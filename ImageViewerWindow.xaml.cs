using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace LAM_App
{
    public partial class ImageViewerWindow : Window
    {
        public ImageViewerWindow(ImageSource imageSource)
        {
            InitializeComponent();
            viewerImage.Source = imageSource;
        }

        private void zoomOut_btn_Click(object sender, RoutedEventArgs e)
        {
            zoomSlider.Value = System.Math.Max(zoomSlider.Minimum, zoomSlider.Value - 0.1);
        }

        private void zoomIn_btn_Click(object sender, RoutedEventArgs e)
        {
            zoomSlider.Value = System.Math.Min(zoomSlider.Maximum, zoomSlider.Value + 0.1);
        }

        private void resetZoom_btn_Click(object sender, RoutedEventArgs e)
        {
            zoomSlider.Value = 1;
        }

        private void close_btn_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void viewerScroll_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if ((Keyboard.Modifiers & ModifierKeys.Control) != ModifierKeys.Control) return;

            zoomSlider.Value = e.Delta > 0
                ? System.Math.Min(zoomSlider.Maximum, zoomSlider.Value + 0.1)
                : System.Math.Max(zoomSlider.Minimum, zoomSlider.Value - 0.1);
            e.Handled = true;
        }
    }
}
