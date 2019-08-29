using Microsoft.Win32;
using RpanList.Classes;
using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Media;
using WinForms = System.Windows.Forms;
using conf = RpanList.Properties.Settings;
using System.Windows.Threading;
using System.Threading.Tasks;

namespace RpanList
{
    public partial class MainWindow : Window
    {
        //BackgroundWorker refresh = new BackgroundWorker();
        DispatcherTimer periodicRefresh = new DispatcherTimer { Interval = TimeSpan.FromSeconds(5) };

        // Set to true after clicking refresh on RpanDown grid
        bool isRefreshing;

        // Set to true after clicking "Continue browsing" on RpanDown grid
        bool ignoreRpanDown;

        // Rotation of broken pan in RpanDown grid
        double panRotation = 0;
        // Rotation of Refresh button
        double refreshRotation = 0;

        int streams = 0;
        int views = 0;

        public MainWindow()
        {
            InitializeComponent();
            periodicRefresh.Tick += PeriodicRefresh_Tick;
            retrieveSettings(conf.Default);
            parseResponse();
        }

        private void PeriodicRefresh_Tick(object sender, EventArgs e)
        {
            refresh();
        }

        async void parseResponse()
        {
            ApiResponse response = RpanApi.grabResponse();
            int retryLimit = 3;
            for (int i = 0; i < retryLimit; i++)
            {
                if (response.status == "User ID is not found")
                {
                    // reddit sometimes returns this
                    // it seems to get resolved if you retry a lot
                    // sorry for the awful workaround, but hey, it works
                    retryLimit = 20;
                    await Task.Delay(200);
                    if (i + 1 >= retryLimit)
                    {
                        isRefreshing = false;
                        tbRefresh.Text = "Could not refresh";
                        throwError("RPAN API returned with error. Please try again.");
                        return;
                    }
                    else
                    {
                        continue;
                    }
                }
                if (response.status == "success")
                {
                    tbRefresh.Text = "Refresh";

                    if (response.data.Count == 0) // response contains no streams (usually means that RPAN has ended for today)
                    {
                        if (rpanDown.Visibility == Visibility.Visible) // if already in RpanError, update the header and rotate the pan
                        {
                            tbRpanDown.Text = "RPAN is still down";
                            tbRefresh.Text = "Refresh again";
                            panRotation += 5;
                            tbBrokenPan.RenderTransform = new RotateTransform(panRotation);
                            break;
                        }
                        else if (!ignoreRpanDown) // if not in RpanDown, reset text and display RpanDown
                        {
                            tbRpanDown.Text = "RPAN is down";
                            rpanDown.Visibility = Visibility.Visible;
                            break;
                        }
                    }
                    else // there is at least one stream to display
                    {
                        tbRefresh.Text = "Listing streams...";
                        tbRefresh2.Text = "Refresh";
                        listStreams(response);
                        rpanDown.Visibility = Visibility.Collapsed;
                        if (!periodicRefresh.IsEnabled) periodicRefresh.Start();
                        break;
                    }

                }
                else if (response.status == "Couldn't connect")
                {
                    throwError("Could not connect to RPAN API.");
                }
                else if (response.status == "Couldn't parse JSON")
                {
                    throwError("Could not understand RPAN API response.");
                }
                else
                {
                    throwError("RPAN API returned with error: " + response.status);
                }

                tbRefresh.Text = "Could not refresh";
            }
            // user can click tbRefresh again
            isRefreshing = false;
        }

        void throwError(string errorMessage)
        {
            System.Windows.MessageBox.Show(errorMessage);
        }

        void listStreams(ApiResponse response)
        {
            double vo = scroller.VerticalOffset;
            StreamList.Children.Clear();
            streams = 0;
            views = 0;
            foreach (RpanData data in response.data)
            {
                streams++;
                views = views + data.continuous_watchers;
                StreamView sw = new StreamView(data);
                tbSearch.TextChanged += sw.SearchTermChanged;
                StreamList.Children.Add(sw);
            }
            scroller.ScrollToVerticalOffset(vo);
            scroller.UpdateLayout();
            UpdateLayout();
            Title = "RpanList - " + streams.ToString() + " streams, " + views.ToString() + " viewers";
        }

        private void TbRefresh_MouseLeftButtonUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (!isRefreshing)
            {
                isRefreshing = true;
                refresh();
            }
        }

        async void refresh()
        {
           tbRefresh.Text = "Refreshing...";
           tbRefresh2.Text = "Refreshing";
           parseResponse();
        }

        private void TbReturn_MouseLeftButtonUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            ignoreRpanDown = true;
            rpanDown.Visibility = Visibility.Hidden;
        }

        private void CbRefresh_MouseLeftButtonUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (!isRefreshing)
            {
                isRefreshing = true;
                refreshRotation += -30;
                if (refreshRotation <= -360)
                {
                    refreshRotation = 0;
                }
                imRefresh.RenderTransform = new RotateTransform(refreshRotation);

                refresh();
            }
        }

        void openBrowse(BrowseType type)
        {
            switch (type)
            {
                case BrowseType.YoutubeDl:
                    Microsoft.Win32.OpenFileDialog ofd = new Microsoft.Win32.OpenFileDialog();
                    ofd.Title = "Open youtube-dl";
                    ofd.Filter = "Application (*.exe)|*.exe|All files (*.*)|*.*";
                    if (ofd.ShowDialog() == true && !string.IsNullOrWhiteSpace(ofd.FileName))
                    {
                        conf.Default.ytdlPath = ofd.FileName;
                        conf.Default.Save();
                        tbYtdlPath.Text = conf.Default.ytdlPath;
                    }
                    break;
                case BrowseType.Downloads:
                    FolderBrowserDialog fbd = new FolderBrowserDialog();
                    fbd.Description = "Please select the folder where downloaded streams will be saved to.";
                    if (fbd.ShowDialog() == WinForms.DialogResult.OK && !string.IsNullOrWhiteSpace(fbd.SelectedPath))
                    {
                        conf.Default.downloadDir = fbd.SelectedPath;
                        conf.Default.Save();
                        tbDownloadDir.Text = conf.Default.downloadDir;
                    }
                    break;
                default:
                    break;
            }
        }

        enum BrowseType
        {
            YoutubeDl,
            Downloads
        }

        private void BtnBrowseYtdl_Click(object sender, RoutedEventArgs e)
        {
            openBrowse(BrowseType.YoutubeDl);
        }

        private void BtnBrowseDownDir_Click(object sender, RoutedEventArgs e)
        {
            openBrowse(BrowseType.Downloads);
        }

        private void TbYtdlPath_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            conf.Default.ytdlPath = tbYtdlPath.Text;
            conf.Default.Save();
        }

        private void TbDownloadDir_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            conf.Default.downloadDir = tbDownloadDir.Text;
            conf.Default.Save();
        }

        private void TbCloseSettings_MouseLeftButtonUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            settingsGrid.Visibility = Visibility.Collapsed;
        }

        private void CbSettings_MouseLeftButtonUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            settingsGrid.Visibility = Visibility.Visible;
        }

        void retrieveSettings(conf s)
        {
            tbYtdlPath.Text = s.ytdlPath;
            tbDownloadDir.Text = s.downloadDir;
        }

        private void TbSearch_GotFocus(object sender, RoutedEventArgs e)
        {
            if (tbSearch.Text == "Search for streams...") tbSearch.Text = "";
        }

        private void TbSearch_LostFocus(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(tbSearch.Text)) tbSearch.Text = "Search for streams...";
        }
    }
}
