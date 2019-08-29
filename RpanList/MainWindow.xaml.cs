using RpanList.Classes;
using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Media;

namespace RpanList
{
    public partial class MainWindow : Window
    {
        BackgroundWorker refresh = new BackgroundWorker();

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
            refresh.DoWork += new DoWorkEventHandler(Refresh_DoWork);
            InitializeComponent();
            parseResponse(RpanApi.grabResponse());
        }

        void parseResponse(ApiResponse response)
        {
            int retryLimit = 3;
            for (int i = 0; i < retryLimit; i++)
            {
                if (response.status == "User ID is not found")
                {
                    // reddit sometimes returns this
                    // it seems to get resolved if you retry a lot
                    // sorry for the awful workaround, but hey, it works
                    retryLimit = 20;
                    System.Threading.Thread.Sleep(150);
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
                        listStreams(response);
                        rpanDown.Visibility = Visibility.Collapsed;
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
            MessageBox.Show(errorMessage);
        }

        void listStreams(ApiResponse response)
        {
            foreach (RpanData data in response.data)
            {
                streams++;
                views = views + data.continuous_watchers;
                StreamList.Children.Add(new StreamView(data));
            }
            Title = "RpanList - " + streams.ToString() + " streams, " + views.ToString() + " viewers";
        }

        private void TbRefresh_MouseLeftButtonUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (!isRefreshing)
            {
                isRefreshing = true;
                refresh.RunWorkerAsync();
            }
        }

        void Refresh_DoWork(object sender, DoWorkEventArgs e)
        {
            Dispatcher.BeginInvoke(new Action(() => { tbRefresh.Text = "Refreshing..."; }));
            Dispatcher.BeginInvoke(new Action(() => { tbRefresh2.Text = "Refreshing..."; }));
            Dispatcher.BeginInvoke(new Action(() => { parseResponse(RpanApi.grabResponse()); }));
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
            }
        }
    }
}
