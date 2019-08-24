using Newtonsoft.Json;
using RpanList.Classes;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

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
            string s;
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
                catch (Exception e)
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
