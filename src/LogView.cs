using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;

namespace RpanList
{
    class LogView : Grid
    {
        LogEntry entry;
        public void LogLevelChanged(object sender, EventArgs e)
        {
            checkSeverity((sender as ComboBox).SelectedIndex);
        }

        public void checkSeverity(int severity)
        {
            if (severity <= (int)entry.Severity)
            {
                Visibility = Visibility.Visible;
            }
            else
            {
                Visibility = Visibility.Collapsed;
            }
        }
        public LogView(LogEntry item)
        {
            entry = item;
            Height = 18;
            Viewbox icon = new Viewbox
            {
                Width = 18,
                Height = 18,
                HorizontalAlignment = HorizontalAlignment.Left
            };
            TextBlock content = new TextBlock
            {
                HorizontalAlignment = HorizontalAlignment.Left,
                Margin = new Thickness(22, 0, 0, 0)
            };
            TextBlock timestamp = new TextBlock
            {
                HorizontalAlignment = HorizontalAlignment.Right,
                Margin = new Thickness(0, 0, 6, 0)
            };
            string imageName = "";
            switch (item.Severity)
            {
                case LogSeverity.Debug:
                    imageName = "Debug";
                    icon.Opacity = 0.5;
                    content.Opacity = 0.5;
                    timestamp.Opacity = 0.5;
                    break;
                case LogSeverity.Info:
                    imageName = "Info";
                    break;
                case LogSeverity.Warning:
                    imageName = "Warning";
                    break;
                case LogSeverity.Error:
                    imageName = "Error";
                    break;
            }
            //icon.Source = new BitmapImage(new Uri(@"/RpanList;component/Icons/" + imageName + ".png", UriKind.Relative));
            icon.Child = (Canvas)FindResource("icn" + imageName);
            content.Text = item.Content;
            timestamp.Text = item.Timestamp.ToString();

            Children.Add(icon);
            Children.Add(content);
            Children.Add(timestamp);
        }

        // Alternative constructor
        public LogView(LogType type, LogSeverity severity, string content)
            : this(new LogEntry(type, severity, content)) { }
    }
}
