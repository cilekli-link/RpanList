using Microsoft.Win32;
using RpanList.Classes;
using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using static RpanList.Logger;
using conf = RpanList.Properties.Settings;
using WinForms = System.Windows.Forms;

namespace RpanList
{
    public partial class MainWindow : Window
    {
        DispatcherTimer periodicRefresh = new DispatcherTimer { Interval = TimeSpan.FromSeconds(5) };

        bool isRefreshing;          // Set to true after clicking refresh on RpanDown grid
        bool ignoreRpanDown;        // Set to true after clicking "Continue browsing" on RpanDown grid
        double panRotation = 0;     // Rotation of broken pan in RpanDown grid
        double refreshRotation = 0; // Rotation of Refresh button

        int streams = 0;
        int views = 0;
        int failedAttempts = 0;
        int unreadLogs = 0;

        public MainWindow()
        {
            InitializeComponent();
            periodicRefresh.Tick += PeriodicRefresh_Tick;
            LogEntryAdded += logEntryAdded;
            Log(LogSeverity.Debug, "Application started");
            retrieveSettings(conf.Default);
            Log(LogSeverity.Debug, "Retrieved user settings");
            refresh(true);
            periodicRefresh.Start();
        }

        private void logEntryAdded(LogEntry logEntry)
        {
            if (tabs.SelectedIndex != 2 && (int)logEntry.Severity >= cbLogLevel.SelectedIndex)
            {
                unreadLogs++;
                logHeader.Text = "Log (" + unreadLogs + ")";
            }
            LogView logView = new LogView(logEntry);
            logView.checkSeverity(cbLogLevel.SelectedIndex);
            cbLogLevel.SelectionChanged += logView.LogLevelChanged;
            LogList.Children.Add(logView);
            LogScroller.ScrollToEnd();
        }

        private void PeriodicRefresh_Tick(object sender, EventArgs e)
        {
            if (!isRefreshing)
            {
                refresh(false);
            }
        }

        async Task parseResponse()
        {
            ApiResponse response = await RpanApi.grabResponse();
            if (response.status == "User ID is not found")
            {
                for (int i = 0; i < 20; i++)
                {
                    Log(LogSeverity.Warning, "Could not connect - retrying (" + i + " of 20)");
                    await Task.Delay(200);
                    response = await RpanApi.grabResponse();
                    if (response.status == "User ID is not found")
                    {
                        if (i >= 20)
                        {
                            isRefreshing = false;
                            tbRefresh.Text = "Could not refresh";
                            tbRefresh2.Text = "Refresh";
                            Title = "RpanList - Couldn't connect";
                            throwError("RPAN API returned with error. Please try again.");
                            Log(LogSeverity.Error, "Could not connect, aborted after 20 retries");
                            return;
                        }
                        continue;
                    }
                    else
                    {
                        break;
                    }
                }
            }
            if (response.status == "success")
            {
                tbRefresh.Text = "Refresh";
                tbRefresh2.Text = "Refresh";
                if (response.data.Count == 0) // response contains no streams (usually means that RPAN has ended for today)
                {
                    failedAttempts++;
                    if (failedAttempts == 5)
                    {
                        Log(LogSeverity.Warning, "5 failed refresh attempts: auto-refresh has been paused for now");
                        periodicRefresh.Stop();
                    }
                    Title = "RpanList - RPAN is down";
                    Log(LogSeverity.Warning, "Connected, but RPAN is currently down (no streams)");
                    if (periodicRefresh.IsEnabled)
                    {
                        periodicRefresh.Stop();
                        periodicRefresh.Start();
                    }

                    if (rpanDown.Visibility == Visibility.Visible) // if already in RpanError, update the header and rotate the pan
                    {
                        tbRpanDown.Text = "RPAN is still down";
                        tbRefresh.Text = "Refresh again";
                        panRotation += 5;
                        tbBrokenPan.RenderTransform = new RotateTransform(panRotation);
                    }
                    else if (!ignoreRpanDown) // if not in RpanDown, reset text and display RpanDown
                    {
                        tbRpanDown.Text = "RPAN is down";
                        rpanDown.Visibility = Visibility.Visible;
                    }
                }
                else // there is at least one stream to display
                {
                    failedAttempts = 0;
                    Log(LogSeverity.Info, "Connected, listing streams.");
                    Title = "RpanList - Listing streams...";
                    tbRefresh.Text = "Listing streams...";
                    tbNoStreams.Visibility = Visibility.Hidden;
                    listStreams(response);
                    rpanDown.Visibility = Visibility.Collapsed;
                    if (!periodicRefresh.IsEnabled)
                    {
                        periodicRefresh.Start();
                    }
                }

            }
            else
            {
                Title = "RpanList - Couldn't connect";
                tbRefresh.Text = "Could not refresh";
                Log(LogSeverity.Error, "Failed to connect to RPAN API: status: " + response.status);
            }
            isRefreshing = false;
        }

        void throwError(string errorMessage)
        {
            MessageBox.Show(errorMessage);
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
            Log(LogSeverity.Info, "Listed " + streams + " streams.");
            scroller.ScrollToVerticalOffset(vo);
            scroller.UpdateLayout();
            UpdateLayout();
            Title = "RpanList - " + streams.ToString() + " streams, " + views.ToString() + " viewers";
        }

        private void TbRefresh_MouseLeftButtonUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (!isRefreshing)
            {
                refresh(false);
            }
        }

        async void refresh(bool firstTime)
        {
            isRefreshing = true;
            if (firstTime)
            {
                Log(LogSeverity.Info, "Connecting to RPAN...");
                Title = "RpanList - Connecting...";
            }
            else
            {
                Log(LogSeverity.Info, "Refreshing...");
                Title = "RpanList - Refreshing...";
            }
            tbRefresh.Text = "Refreshing...";
            tbRefresh2.Text = "Refreshing";
            await parseResponse();
        }

        private void TbReturn_MouseLeftButtonUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            Log(LogSeverity.Debug, "Broken pan ignored");
            ignoreRpanDown = true;
            rpanDown.Visibility = Visibility.Hidden;
        }

        private void CbRefresh_MouseLeftButtonUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (!isRefreshing)
            {
                refreshRotation += -30;
                if (refreshRotation <= -360)
                {
                    refreshRotation = 0;
                }
                imRefresh.RenderTransform = new RotateTransform(refreshRotation);

                refresh(false);
            }
        }

        void openBrowse(BrowseType type)
        {
            switch (type)
            {
                case BrowseType.YoutubeDl:
                    OpenFileDialog ofd = new OpenFileDialog
                    {
                        Title = "Open youtube-dl",
                        Filter = "Application (*.exe)|*.exe|All files (*.*)|*.*"
                    };
                    if (ofd.ShowDialog() == true && !string.IsNullOrWhiteSpace(ofd.FileName))
                    {
                        conf.Default.ytdlPath = ofd.FileName;
                        conf.Default.Save();
                        tbYtdlPath.Text = conf.Default.ytdlPath;
                    }
                    break;
                case BrowseType.Downloads:
                    WinForms.FolderBrowserDialog fbd = new WinForms.FolderBrowserDialog
                    {
                        Description = "Please select the folder where downloaded streams will be saved to."
                    };
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
            (wfhRefreshDelay.Child as WinForms.NumericUpDown).Value = s.refreshDelay;
        }

        private void TbSearch_GotFocus(object sender, RoutedEventArgs e)
        {
            if (tbSearch.Text == "Search for streams...")
            {
                tbSearch.Text = "";
            }
        }

        private void TbSearch_LostFocus(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(tbSearch.Text))
            {
                tbSearch.Text = "Search for streams...";
            }
        }

        private void wfhRefreshDelay_ValueChanged(object sender, EventArgs e)
        {
            if (wfhRefreshDelay.Child != null)
            {
                int newDelay = (int)(wfhRefreshDelay.Child as WinForms.NumericUpDown).Value;
                if (periodicRefresh.IsEnabled)
                {
                    periodicRefresh.Stop();
                }

                periodicRefresh.Interval = TimeSpan.FromSeconds(newDelay);
                periodicRefresh.Start();
                conf.Default.refreshDelay = newDelay;
                conf.Default.Save();
            }
        }

        private void TabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (tabs.SelectedIndex == 2)
            {
                unreadLogs = 0;
                logHeader.Text = "Log";
            }
        }

        private void BtnClearLogs_Click(object sender, RoutedEventArgs e)
        {
            LogList.Children.Clear();
        }
    }
}
