using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace LAM_App
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void menu_btn_Click(object sender, RoutedEventArgs e)
        {
            menuPopup.IsOpen = true;
        }

        private void payment_btn_Click(object sender, RoutedEventArgs e)
        {
            var paymentWindow = new PaymentsWindow();
            paymentWindow.Show();
            this.Close();
        }

        private void price_btn_Click(object sender, RoutedEventArgs e)
        {
            var priceWindow = new PriceWindow();
            priceWindow.Show();
            this.Close();
        }

        private void timetable_btn_Click(object sender, RoutedEventArgs e)
        {
            var timetableWindow = new TimetableWindow();
            timetableWindow.Show();
            this.Close();
        }

        private void trial_btn_Click(object sender, RoutedEventArgs e)
        {
            var trialWindow = new TrialsWindow();
            trialWindow.Show();
            this.Close();
        }

        private void attendance_btn_Click(object sender, RoutedEventArgs e)
        {
            var attendanceWindow = new AttendanceWindow();
            attendanceWindow.Show();
            this.Close();
        }

        private void note_btn1_Click(object sender, RoutedEventArgs e)
        {

        }
        private void clients_btn_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                menuPopup.IsOpen = false;
                var clientsWindow = new ClientsWindow();
                clientsWindow.Show();
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Не удалось открыть окно клиентов: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void teachers_btn_Click(object sender, RoutedEventArgs e)
        {
            menuPopup.IsOpen = false;
            var teachersWindow = new TeachersWindow();
            teachersWindow.Show();
            this.Close();
        }
        private void styles_btn_Click(object sender, RoutedEventArgs e)
        {
            menuPopup.IsOpen = false;
            var stylesWindow = new StylesWindow();
            stylesWindow.Show();
            this.Close();
        }
    }
}
