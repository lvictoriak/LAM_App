using System;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using Microsoft.Win32;

namespace LAM_App
{
    public partial class AdultsTimetableWindow : Window
    {
        private const string AdultsScheduleKey = "timetable_adults";
        private static readonly string ImageStorageFolder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "LAM_App", "Images");

        public AdultsTimetableWindow()
        {
            InitializeComponent();
            InitializeImage(imgAdultsSchedule, AdultsScheduleKey);
        }

        private void menu_btn_Click(object sender, RoutedEventArgs e)
        {
            menuPopup.IsOpen = true;
        }

        private void children_btn_Click(object sender, RoutedEventArgs e)
        {
            var timetableWindow = new TimetableWindow();
            timetableWindow.Show();
            Close();
        }

        private void adults_btn_Click(object sender, RoutedEventArgs e)
        {
        }

        private void loadAdultsSchedule_btn_Click(object sender, RoutedEventArgs e)
        {
            LoadImage(imgAdultsSchedule, AdultsScheduleKey);
        }

        private void deleteAdultsSchedule_btn_Click(object sender, RoutedEventArgs e)
        {
            DeleteImage(imgAdultsSchedule, AdultsScheduleKey);
        }

        private void openAdultsSchedule_btn_Click(object sender, MouseButtonEventArgs e)
        {
            OpenImage(imgAdultsSchedule);
        }

        private void OpenImage(Image sourceImage)
        {
            if (sourceImage.Source == null)
            {
                MessageBox.Show("Сначала загрузите картинку.", "Просмотр", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var viewer = new ImageViewerWindow(sourceImage.Source)
            {
                Owner = this
            };
            viewer.ShowDialog();
        }

        private void InitializeImage(Image target, string key)
        {
            if (File.Exists(GetDeletedMarkerPath(key)))
            {
                target.Source = null;
                return;
            }

            var savedPath = FindSavedImagePath(key);
            if (savedPath != null)
            {
                SetImageSource(target, savedPath);
            }
        }

        private void LoadImage(Image target, string key)
        {
            var dialog = new OpenFileDialog
            {
                Filter = "Изображения|*.png;*.jpg;*.jpeg;*.bmp;*.gif|Все файлы|*.*"
            };

            if (dialog.ShowDialog() != true) return;

            Directory.CreateDirectory(ImageStorageFolder);
            DeleteSavedImages(key);

            var extension = Path.GetExtension(dialog.FileName);
            var targetPath = Path.Combine(ImageStorageFolder, key + extension);
            File.Copy(dialog.FileName, targetPath, true);
            DeleteMarker(key);
            SetImageSource(target, targetPath);
        }

        private void DeleteImage(Image target, string key)
        {
            if (target.Source == null)
            {
                MessageBox.Show("Картинка уже удалена.", "Удаление", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var result = MessageBox.Show("Удалить текущую картинку?", "Подтверждение удаления",
                MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes) return;

            target.Source = null;
            Directory.CreateDirectory(ImageStorageFolder);
            DeleteSavedImages(key);
            File.WriteAllText(GetDeletedMarkerPath(key), "deleted");
        }

        private static void SetImageSource(Image target, string path)
        {
            var image = new BitmapImage();
            image.BeginInit();
            image.CacheOption = BitmapCacheOption.OnLoad;
            image.UriSource = new Uri(path, UriKind.Absolute);
            image.EndInit();
            image.Freeze();
            target.Source = image;
        }

        private static string? FindSavedImagePath(string key)
        {
            if (!Directory.Exists(ImageStorageFolder)) return null;
            return Directory.GetFiles(ImageStorageFolder, key + ".*").FirstOrDefault();
        }

        private static void DeleteSavedImages(string key)
        {
            if (!Directory.Exists(ImageStorageFolder)) return;

            foreach (var path in Directory.GetFiles(ImageStorageFolder, key + ".*"))
            {
                File.Delete(path);
            }
        }

        private static string GetDeletedMarkerPath(string key)
        {
            return Path.Combine(ImageStorageFolder, key + ".deleted");
        }

        private static void DeleteMarker(string key)
        {
            var markerPath = GetDeletedMarkerPath(key);
            if (File.Exists(markerPath))
            {
                File.Delete(markerPath);
            }
        }

        private void Logo_Click(object sender, RoutedEventArgs e)
        {
            var mainWindow = new MainWindow();
            mainWindow.Show();
            Close();
        }

        private void note_btn1_Click(object sender, RoutedEventArgs e)
        {
        }

        private void clients_btn_Click(object sender, RoutedEventArgs e)
        {
            menuPopup.IsOpen = false;
            var clientsWindow = new ClientsWindow();
            clientsWindow.Show();
            Close();
        }

        private void teachers_btn_Click(object sender, RoutedEventArgs e)
        {
            menuPopup.IsOpen = false;
            var teachersWindow = new TeachersWindow();
            teachersWindow.Show();
            Close();
        }

        private void styles_btn_Click(object sender, RoutedEventArgs e)
        {
            menuPopup.IsOpen = false;
            var stylesWindow = new StylesWindow();
            stylesWindow.Show();
            Close();
        }
    }
}
