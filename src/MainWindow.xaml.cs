using Microsoft.Win32;
using RpanList.Classes;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shell;
using System.Windows.Threading;
using static RpanList.Logger;
using conf = RpanList.Properties.Settings;
using WinForms = System.Windows.Forms;

namespace RpanList
{
    public partial class MainWindow : Window
    {
        Dictionary<string, Download> downloads = new Dictionary<string, Download>();
        Dictionary<string, RpanData> streams = new Dictionary<string, RpanData>();
        ApiResponse response;

        DispatcherTimer periodicRefresh = new DispatcherTimer { Interval = TimeSpan.FromSeconds(5) };

        bool isRefreshing;          // Set to true after clicking refresh on RpanDown grid
        bool ignoreRpanDown;        // Set to true after clicking "Continue browsing" on RpanDown grid
        double panRotation = 0;     // Rotation of broken pan in RpanDown grid
        double refreshRotation = 0; // Rotation of Refresh button

        int streamCount = 0;
        int viewCount = 0;
        int failedAttempts = 0;
        int unreadLogs = 0;

        public MainWindow()
        {
            InitializeComponent();
            periodicRefresh.Tick += PeriodicRefresh_Tick;
            LogEntryAdded += logEntryAdded;
            Log(LogSeverity.Debug, "Application started");
            retrieveSettings(conf.Default);
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
            if (cbAutoscrollLog.IsChecked == true)
            {
                LogScroller.ScrollToEnd();
            }
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
            response = await RpanApi.grabResponse();

            if (response.status == "User ID is not found")
            // reddit sometimes returns this. idk why, but it gets resolved if you retry a lot.
            // that's what this for loop does
            {
                for (int i = 0; i < 20; i++)
                {
                    Log(LogSeverity.Warning, "Could not connect - retrying (" + i + " of 20)");
                    await Task.Delay(4000); // let the API rest (?)
                    response = await RpanApi.grabResponse();
                    if (response.status == "User ID is not found")
                    {
                        if (i >= 19)
                        {
                            isRefreshing = false;
                            tbRpanDownRefresh.Text = "Could not refresh";
                            setTitle("RpanList - Couldn't connect");
                            Log(LogSeverity.Error, "Could not connect, aborted after 20 retries");
                            return;
                        }
                        else // retry attempts didn't reach limit yet
                        {
                            continue;
                        }
                    }
                    else // if response is something else
                    {
                        break;
                    }
                }
            }
            if (response.status == "success")
            {
                tbRpanDownRefresh.Text = "Refresh";
                if (response.data.Count == 0) // response contains no streams (usually means that RPAN has ended for today)
                {
                    setTitle("RpanList - RPAN is down");
                    Log(LogSeverity.Warning, "Connected, but RPAN is currently down (no streams)");
                    if (conf.Default.pauseRefreshIfDown)
                    {
                        if (periodicRefresh.IsEnabled)
                        {
                            periodicRefresh.Stop();
                            Log(LogSeverity.Warning, "RPAN is down, auto-refresh paused");
                        }
                    }
                    if (rpanDown.Visibility == Visibility.Visible) // if already in RpanError, update the header and rotate the pan
                    {
                        tbRpanDown.Text = "RPAN is still down";
                        tbRpanDownRefresh.Text = "Refresh again";
                        panRotation += 5;
                        tbBrokenPan.RenderTransform = new RotateTransform(panRotation);
                    }
                    else if (!ignoreRpanDown && conf.Default.showBrokenPan) // if not in RpanDown, reset text and display RpanDown
                    {
                        tbRpanDown.Text = "RPAN is down";
                        rpanDown.Visibility = Visibility.Visible;
                    }
                    checkFailedAttempts();
                }
                else // there is at least one stream to display
                {
                    failedAttempts = 0;
                    //Log(LogSeverity.Info, "Connected, listing streams.");
                    setTitle("RpanList - Listing streams...");
                    tbRpanDownRefresh.Text = "Listing streams...";
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
                checkFailedAttempts();
                setTitle("RpanList - Couldn't connect");
                tbRpanDownRefresh.Text = "Could not refresh";
                Log(LogSeverity.Error, "Failed to connect to RPAN API: status: " + response.status);
            }
            isRefreshing = false;
        }

        void listStreams(ApiResponse response)
        {
            StreamView sw;
            double vo = scroller.VerticalOffset;

            sortList(response);
            if (cbSortDescending.IsChecked == true) response.data.Reverse();
            StreamList.Children.Clear();
            streamCount = 0;
            viewCount = 0;
            foreach (RpanData data in response.data)
            {
                streamCount++;
                viewCount += data.continuous_watchers;
                if (!string.IsNullOrWhiteSpace(tbSearch.Text) && tbSearch.Text != "Search for streams...")
                {
                    sw = new StreamView(data, tbSearch.Text);
                }
                else
                {
                    sw = new StreamView(data, "");
                }
                sw.DownloadClicked += Sw_DownloadClicked;
                tbSearch.TextChanged += sw.SearchTermChanged;
                StreamList.Children.Add(sw);
            }
            Log(LogSeverity.Info, "Listed " + streamCount + " streams.");
            scroller.ScrollToVerticalOffset(vo);
            scroller.UpdateLayout();
            UpdateLayout();
            setTitle("RpanList - " + streamCount + " streams, " + viewCount + " viewers");
            updateRecents(response);
        }

        private void Sw_DownloadClicked(StreamView sender, RpanData data)
        {
            Download dl = new Download(data);
            dl.DownloadEnded += Dl_DownloadEnded;
            downloads.Add(data.stream.stream_id, dl);
            tabs.Items.Add(dl.tab);
        }

        private void Dl_DownloadEnded(object sender, EventArgs e)
        {
            downloads.Remove((sender as Download).data.stream.stream_id);
        }

        private void updateRecents(ApiResponse response)
        {
            Dictionary<string, RpanData> newDict = new Dictionary<string, RpanData>();
            foreach (RpanData data in response.data)
            {
                if (!newDict.ContainsKey(data.stream.stream_id))
                {
                    newDict.Add(data.stream.stream_id, data);
                }
                if (streams.ContainsKey(data.stream.stream_id))
                {
                    streams[data.stream.stream_id] = data;
                }
            }
            // Streams from newDict that aren't in the streams dictionary
            Dictionary<string, RpanData> newStreams = newDict
                .Where(kvp => !streams.ContainsKey(kvp.Key))
                .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

            // Streams from newDict that already are in the streams directory
            Dictionary<string, RpanData> oldStreams = newDict
                .Where(kvp => streams.ContainsKey(kvp.Key))
                .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

            // Streams from the stream directory that aren't in newDict
            Dictionary<string, RpanData> goneStreams = streams
                .Where(kvp => !newDict.ContainsKey(kvp.Key))
                .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

            foreach (KeyValuePair<string, RpanData> stream in newStreams)
            {
                streams.Add(stream.Key, stream.Value);
                Log(LogSeverity.Debug, "New stream: " + stream.Value.post.title);
            }

            foreach (KeyValuePair<string, RpanData> stream in goneStreams)
            {
                streams.Remove(stream.Key);
                Log(LogSeverity.Debug, "Stream ended: " + stream.Value.post.title);
                //tbEndedNoStreams.Visibility = Visibility.Collapsed;
                //if (tbEndedSearch.Text != "Search for streams...")
                //{
                //    endedStreamList.Children.Add(new StreamView(stream.Value, tbEndedSearch.Text));
                //}
                //else
                //{
                    endedStreamList.Children.Add(new StreamView(stream.Value, ""));
                //}
            }


        }

        private void TbRefresh_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (!isRefreshing)
            {
                refresh(false);
            }
        }

        void checkFailedAttempts()
        {
            if (periodicRefresh.IsEnabled)
            {
                failedAttempts++;
                if (failedAttempts == conf.Default.maxRefreshAttempts)
                {
                    Log(LogSeverity.Warning, conf.Default.maxRefreshAttempts + " failed refresh attempt(s): auto-refresh has been paused for now");
                    periodicRefresh.Stop();
                }
            }
        }
        async void refresh(bool firstTime)
        {
            isRefreshing = true;

            if (firstTime) // Connecting for the first time?
            {
                Log(LogSeverity.Info, "Connecting to RPAN...");
                setTitle("RpanList - Connecting...");
                tbToolbarRefresh.Text = "Connecting";
            }
            else
            {
                Log(LogSeverity.Info, "Refreshing...");
                setTitle("RpanList - Refreshing...");
                tbToolbarRefresh.Text = "Refreshing";
                tbRpanDownRefresh.Text = "Refreshing...";
            }
            await parseResponse();
            // tbRpanDownRefresh.Text doesn't get changed here, because it can also be "Refresh again" or "Could not refresh"
            tbToolbarRefresh.Text = "Refresh";
        }

        private void TbReturn_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            Log(LogSeverity.Debug, "Broken pan ignored");
            ignoreRpanDown = true;
            rpanDown.Visibility = Visibility.Hidden;
        }

        private void CbRefresh_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
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
                        tbYtdlPath.Text = ofd.FileName;
                        conf.Default.ytdlPath = ofd.FileName;
                    }
                    break;
                case BrowseType.Downloads:
                    WinForms.FolderBrowserDialog fbd = new WinForms.FolderBrowserDialog
                    {
                        Description = "Please select the folder where downloaded streams will be saved to."
                    };
                    if (fbd.ShowDialog() == WinForms.DialogResult.OK && !string.IsNullOrWhiteSpace(fbd.SelectedPath))
                    {
                        tbDownloadDir.Text = fbd.SelectedPath;
                        conf.Default.downloadDir = fbd.SelectedPath;
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

        private void CbSettings_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (settingsGrid.Visibility == Visibility.Visible)
            {
                conf.Default.Save();
                settingsGrid.Visibility = Visibility.Collapsed;
            }
            else
            {
                settingsGrid.Visibility = Visibility.Visible;
            }
        }

        void retrieveSettings(conf s)
        {
            (wfhRefreshDelay.Child as WinForms.NumericUpDown).Value = s.refreshDelay;
            (wfhMaxRefreshAttempts.Child as WinForms.NumericUpDown).Value = s.maxRefreshAttempts;

            if (s.borderlessWindow)
            {
                WindowStyle = WindowStyle.None;
                if (WindowState == WindowState.Maximized) MainGrid.Margin = new Thickness(6);
                WindowChrome.SetWindowChrome(this, new WindowChrome { CaptionHeight = 0, ResizeBorderThickness = new Thickness(5) });
                BorderlessStuff.Visibility = Visibility.Visible;
            }
            else
            {
                WindowStyle = WindowStyle.SingleBorderWindow;
                WindowChrome.SetWindowChrome(this, null);
                BorderlessStuff.Visibility = Visibility.Collapsed;
            }

            Log(LogSeverity.Debug, "Retrieved user settings");
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
            }
        }

        private void wfhMaxRefreshAttempts_ValueChanged(object sender, EventArgs e)
        {
            if (wfhMaxRefreshAttempts.Child != null)
            {
                int newValue = (int)(wfhMaxRefreshAttempts.Child as WinForms.NumericUpDown).Value;
                conf.Default.maxRefreshAttempts = newValue;
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

        private void VbMinimize_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        private void VbClose_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            Close();
        }

        private void VbMaximize_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (WindowState == WindowState.Normal)
            {
                WindowState = WindowState.Maximized;
            }
            else
            {
                WindowState = WindowState.Normal;
            }
        }

        void setTitle(string newTitle)
        {
            Title = newTitle;
            tbTitle.Text = newTitle;
        }

        private void CbBorderless_Checked(object sender, RoutedEventArgs e)
        {
            if (cbBorderless != null && BorderlessStuff != null)
            {
                if (cbBorderless.IsChecked == true)
                {
                    WindowStyle = WindowStyle.None;
                    if (WindowState == WindowState.Maximized) MainGrid.Margin = new Thickness(6);
                    WindowChrome.SetWindowChrome(this, new WindowChrome { CaptionHeight = 0, ResizeBorderThickness = new Thickness(5) });
                    BorderlessStuff.Visibility = Visibility.Visible;
                }
                else
                {
                    WindowStyle = WindowStyle.SingleBorderWindow;
                    MainGrid.Margin = new Thickness(0);
                    WindowChrome.SetWindowChrome(this, null);
                    BorderlessStuff.Visibility = Visibility.Collapsed;
                }
            }
        }

        private void Tabs_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                DragMove();
            }
        }

        private void Window_StateChanged(object sender, EventArgs e)
        {
            if (cbBorderless.IsChecked == true)
            {
                if (WindowState == WindowState.Maximized)
                {
                    MainGrid.Margin = new Thickness(6);
                }
                else
                {
                    MainGrid.Margin = new Thickness(0);
                }
            }
        }

        private void CbSortBy_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (response?.data?.Count > 0)
            {
                listStreams(response);
            }
        }

        private void CbSortDescending_Checked(object sender, RoutedEventArgs e)
        {
            if (response?.data?.Count > 0)
            {
                listStreams(response);
            }
        }

        void sortList(ApiResponse response)
        {
            switch (cbSortBy.SelectedIndex)
            {
                case 0:
                    response.data.Sort((x, y) => x.rank.CompareTo(y.rank));
                    break;
                case 1:
                    response.data.Sort((x, y) => x.post.title.CompareTo(y.post.title));
                    break;
                case 2:
                    response.data.Sort((x, y) => x.upvotes.CompareTo(y.upvotes));
                    break;
                case 3:
                    response.data.Sort((x, y) => x.downvotes.CompareTo(y.downvotes));
                    break;
                case 4:
                    response.data.Sort((x, y) => (x.upvotes - x.downvotes).CompareTo((y.upvotes - y.downvotes)));
                    break;
                case 5:
                    response.data.Sort((x, y) => x.continuous_watchers.CompareTo(y.continuous_watchers));
                    break;
                case 6:
                    response.data.Sort((x, y) => x.unique_watchers.CompareTo(y.unique_watchers));
                    break;
                default:
                    break;
            }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            foreach (Download dl in downloads.Values)
            {
                dl.EndDownload();
            }
        }
    }
}
