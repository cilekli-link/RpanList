using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace RpanList
{
    class FlatButton : Grid
    {
        public event EventHandler Click;

        public FlatButton(string text, double width, string imagePath, Color bgColor)
        {
            Background = new SolidColorBrush(bgColor);
            Height = 20;
            Margin = new Thickness(0, 0, 5, 0);
            Image icon = new Image
            {
                Margin = new Thickness(2, 0, 3, 0),
                Source = new BitmapImage(new Uri(imagePath, UriKind.RelativeOrAbsolute))
            };
            TextBlock txt = new TextBlock
            {
                Foreground = new SolidColorBrush(Colors.White),
                Margin = new Thickness(0, 1, 5, 0),
                Text = text
            };
            WrapPanel btn = new WrapPanel
            {

            };
            Grid clickMask = new Grid
            {
                Background = new SolidColorBrush(Color.FromArgb(0, 0, 0, 0))
            };

            clickMask.MouseUp += ClickMask_MouseUp;

            btn.Children.Add(icon);
            btn.Children.Add(txt);

            Children.Add(btn);
            Children.Add(clickMask);
        }

        private void ClickMask_MouseUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            Click?.Invoke(this, EventArgs.Empty);
        }
    }
}
