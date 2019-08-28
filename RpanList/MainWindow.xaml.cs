using Newtonsoft.Json;
using RpanList.Classes;
using System;
using System.ComponentModel;
using System.Net;
using System.Net.Http;
using System.Windows;
using System.Windows.Media;

namespace RpanList
{
    /// <summary>
    /// MainWindow.xaml etkileşim mantığı
    /// </summary>
    public partial class MainWindow : Window
    {
        // Set to true after clicking refresh on RpanDown grid
        bool isRefreshing;
        // Rotation of broken pan in RpanDown grid
        double panRotation = 0;

        int streams = 0;
        int views = 0;

        public MainWindow()
        {
            InitializeComponent();
            parseResponse(grabResponse());
        }

        ApiResponse grabResponse()
        {
            HttpClientHandler handler = new HttpClientHandler()
            {
                AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate
            };
            var client = new HttpClient(handler);
            client.BaseAddress = new Uri("https://strapi.reddit.com");
            HttpResponseMessage response = new HttpResponseMessage();
            try
            {
                response = client.GetAsync("videos/seed/360").Result;
            }
            catch (Exception)
            {
                return new ApiResponse { status = "Couldn't connect" };
            }
            try
            {
                string responseString = response.Content.ReadAsStringAsync().Result;
                return JsonConvert.DeserializeObject<ApiResponse>(responseString);
            }
            catch (Exception)
            {
                return new ApiResponse { status = "JSON died lol" }; // sorry for this
            }
        }

        void parseResponse(ApiResponse response)
        {
            int retryLimit = 3;
            for (int i = 0; i < retryLimit; i++)
            {
                if (response.status == "User ID is not found")
                {
                    // reddit sometimes returns this
                    // it seems to get resolved if you retry a lot, idk why
                    retryLimit = 10;
                    System.Threading.Thread.Sleep(100);
                    continue;
                }
                if (response.status == "success") // api returned with success (duh)
                {
                    tbRefresh.Text = "Click here to refresh";

                    if (response.data.Count == 0) // response contains no streams (usually means that RPAN has ended for today)
                    {
                        if (rpanDown.Visibility == Visibility.Visible && tbRpanDown.Text == "RPAN is down") // if already in RpanError, update the header and rotate the pan
                        {
                            tbRpanDown.Text = "RPAN is still down";
                            panRotation += 5;
                            tbBrokenPan.RenderTransform = new RotateTransform(panRotation);
                            break;
                        }
                        else // if not in RpanDown, reset text and display RpanDown
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
                        break;
                    }

                }
                else if (response.status == "Couldn't connect") throwError("Could not connect to RPAN API.");
                else if (response.status == "JSON died lol") throwError("Could not understand RPAN API response. (Is RPAN fully dead?...)");
                else throwError("RPAN API returned with error: " + response.status);
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
            tbRefresh.Text = "Click here to refresh";
            rpanDown.Visibility = Visibility.Collapsed;
            Title = "RpanList - " + streams.ToString() + " streams, " + views.ToString() + " viewers";
        }

        private void TbRefresh_MouseLeftButtonUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (!isRefreshing)
            {
                isRefreshing = true;
                BackgroundWorker refreshbw = new BackgroundWorker();
                refreshbw.DoWork += new DoWorkEventHandler(_asyncSpeakerThread_DoWork);
                refreshbw.RunWorkerAsync();
            }
        }

        void _asyncSpeakerThread_DoWork(object sender, DoWorkEventArgs e)
        {
            Dispatcher.BeginInvoke(new Action(() => { tbRefresh.Text = "Refreshing..."; }));
            Dispatcher.BeginInvoke(new Action(() => { parseResponse(grabResponse()); }));
        }
    }
}
