using RpanList.Classes;
using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace RpanList
{
    class StreamView : Grid
    {
        public event DownloadClickedEventHandler DownloadClicked;
        public delegate void DownloadClickedEventHandler(StreamView sender, RpanData stream);

        string lastSearchTerm;

        public RpanData rpanData;

        FlatButton openRpan = new FlatButton("Open in RPAN", 80, @"Icons\open.png", Color.FromRgb(0, 50, 255));
        FlatButton openRpanMedia = new FlatButton("Open in r/pan_media", 137, @"Icons\link.png", Color.FromRgb(255, 100, 0));
        FlatButton download = new FlatButton("Download", 50, @"Icons\download.png", Color.FromRgb(0, 150, 255));

        public void SearchTermChanged(object sender, TextChangedEventArgs e)
        {
            TextBox tb = sender as TextBox;
            if (!string.IsNullOrWhiteSpace(tb.Text) && tb.Text != "Search for streams...")
            {
                lastSearchTerm = tb.Text;
                checkSearchTerm(lastSearchTerm);
            }
            else
            {
                lastSearchTerm = "";
                Visibility = Visibility.Visible;
            }
        }

        void checkSearchTerm(string searchTerm)
        {
            string term = searchTerm.ToLowerInvariant();
            string title = rpanData.post.title.ToLowerInvariant();
            string user = rpanData.post.authorInfo.name.ToLowerInvariant();
            if (title.Contains(term) || user.Contains(term))
            {
                Visibility = Visibility.Visible;
            }
            else
            {
                Visibility = Visibility.Collapsed;
            }
        }

        public StreamView(RpanData data, string currentSearchTerm)
        {
            rpanData = data;
            #region UI elements
            Background = new SolidColorBrush(Color.FromRgb(24, 24, 24));
            Height = 80;
            Margin = new Thickness(10, 5, 10, 0);
            MouseEnter += StreamView_MouseEnter;
            MouseLeave += StreamView_MouseLeave;
            Image img = new Image
            {
                Width = 70,
                HorizontalAlignment = HorizontalAlignment.Left,
                Margin = new Thickness(5)
            };

            StackPanel info = new StackPanel
            {
                Margin = new Thickness(70, 5, 90, 5)
            };

            WrapPanel userAndViews = new WrapPanel
            {
                Margin = new Thickness(0, 0, 0, -5)
            };

            TextBlock username = new TextBlock
            {
                Text = "u/" + data.post.authorInfo.name,
                Foreground = new SolidColorBrush(Color.FromRgb(0, 200, 255)),
                Height = 18,
                Margin = new Thickness(0, 0, 5, 0),
            };

            TextBlock viewers = new TextBlock
            {
                Text = data.continuous_watchers + " viewers - " + data.unique_watchers + " unique watchers",
                Foreground = new SolidColorBrush(Color.FromRgb(200, 200, 200)),
                Height = 18
            };

            TextBlock title = new TextBlock
            {
                FontFamily = new FontFamily("Segoe UI"),
                Text = System.Globalization.CultureInfo.InvariantCulture.TextInfo.ToTitleCase(data.post.title.ToLowerInvariant()),
                Foreground = new SolidColorBrush(Colors.White),
                Height = 25,
                Margin = new Thickness(0, 0, 0, -5),
                FontSize = 17,
                //FontWeight = FontWeights.Bold,
            };

            WrapPanel buttons = new WrapPanel();

            TextBlock messages = new TextBlock
            {
                Text = Math.Floor(double.Parse(data.post.commentCount)) + " chat messages",
                Foreground = new SolidColorBrush(Color.FromRgb(200, 200, 200)),
                Height = 18
            };


            openRpan.Visibility = Visibility.Hidden;
            openRpanMedia.Visibility = Visibility.Hidden;
            download.Visibility = Visibility.Hidden;

            openRpan.Click += (s, a) => { Process.Start("https://reddit.com" + data.share_link); };
            openRpanMedia.Click += (s, a) => { Process.Start("https://reddit.com" + data.post.permalink); };
            download.Click += (s, a) => { DownloadClicked?.Invoke(this, data); };

            StackPanel votes = new StackPanel
            {
                Width = 80,
                HorizontalAlignment = HorizontalAlignment.Right,
            };

            TextBlock netVotes = new TextBlock
            {
                Foreground = new SolidColorBrush(Color.FromRgb(255, 90, 0)),
                Text = (data.upvotes - data.downvotes).ToString(),
                FontWeight = FontWeights.Bold,
                FontSize = 24,
                TextAlignment = TextAlignment.Center,
                Margin = new Thickness(0, 10, 0, -5)
            };

            TextBlock upvotes = new TextBlock
            {
                Foreground = new SolidColorBrush(Color.FromRgb(255, 90, 0)),
                Text = data.upvotes.ToString(),
                FontSize = 14,
                TextAlignment = TextAlignment.Center,
                Margin = new Thickness(0, 0, 0, -3)
            };

            TextBlock downvotes = new TextBlock
            {
                Foreground = new SolidColorBrush(Color.FromRgb(0, 90, 255)),
                Text = data.downvotes.ToString(),
                FontSize = 14,
                TextAlignment = TextAlignment.Center
            };
            #endregion

            if (data.downvotes > data.upvotes)
            {
                netVotes.Foreground = new SolidColorBrush(Color.FromRgb(0, 90, 255));
            }

            BitmapImage bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.UriSource = new Uri(data.stream.thumbnail, UriKind.Absolute);
            bitmap.EndInit();
            img.Source = bitmap;

            userAndViews.Children.Add(username);
            userAndViews.Children.Add(messages);

            buttons.Children.Add(openRpan);
            buttons.Children.Add(openRpanMedia);
            buttons.Children.Add(download);

            info.Children.Add(userAndViews);
            info.Children.Add(title);
            info.Children.Add(viewers);
            info.Children.Add(buttons);

            votes.Children.Add(netVotes);
            votes.Children.Add(upvotes);
            votes.Children.Add(downvotes);

            Children.Add(img);
            Children.Add(info);
            Children.Add(votes);

            if (!string.IsNullOrWhiteSpace(currentSearchTerm))
            {
                lastSearchTerm = currentSearchTerm;
                checkSearchTerm(currentSearchTerm);
            }
        }

        private void StreamView_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
        {
            Background = new SolidColorBrush(Color.FromRgb(24, 24, 24));
            openRpan.Visibility = Visibility.Hidden;
            openRpanMedia.Visibility = Visibility.Hidden;
            download.Visibility = Visibility.Hidden;
        }

        private void StreamView_MouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
        {
            Background = new SolidColorBrush(Color.FromRgb(48, 48, 48));
            openRpan.Visibility = Visibility.Visible;
            openRpanMedia.Visibility = Visibility.Visible;
            download.Visibility = Visibility.Visible;
        }
    }
}
