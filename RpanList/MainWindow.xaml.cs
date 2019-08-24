using Newtonsoft.Json;
using RpanList.Classes;
using System;
using System.Net;
using System.Net.Http;
using System.Windows;

namespace RpanList
{
    /// <summary>
    /// MainWindow.xaml etkileşim mantığı
    /// </summary>
    public partial class MainWindow : Window
    {
        int streams = 0;
        int views = 0;
        public MainWindow()
        {
            InitializeComponent();
            ApiResponse apiResponse;
            HttpClientHandler handler = new HttpClientHandler()
            {
                AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate
            };
            using (var client = new HttpClient(handler))
            {
                client.BaseAddress = new Uri("https://strapi.reddit.com");
                HttpResponseMessage response = new HttpResponseMessage();
                try
                {
                    response = client.GetAsync("videos/seed/360").Result;
                }
                catch (Exception)
                {
                    MessageBox.Show("Could not connect to RPAN API.");
                    return;
                }

                string str = response.Content.ReadAsStringAsync().Result;
                apiResponse = JsonConvert.DeserializeObject<ApiResponse>(str);

                if (apiResponse.status == "success")
                {
                    if (apiResponse.data.Count == 0)
                    {
                        // API returned success, but there's no data
                        MessageBox.Show("RPAN is currently down, come back tomorrow.");
                        return;
                    }
                    else
                    {
                        foreach (RpanData data in apiResponse.data)
                        {
                            streams++;
                            views = views + data.continuous_watchers;
                            StreamList.Children.Add(new StreamView(data));
                        }
                        status.Text = "RPAN: " + streams.ToString() + " streams, " + views.ToString() + " viewers";
                    }
                }
                else
                {
                    MessageBox.Show("RPAN API returned with error: " + apiResponse.status);
                    return;
                }
            }
        }
    }
}
