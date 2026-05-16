using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using LAM_App.Data;
using LAM_App.Models;
using Microsoft.EntityFrameworkCore;
using DanceStyle = LAM_App.Models.Style;

namespace LAM_App
{
    public partial class StylesWindow : Window
    {
        private readonly AppDbContext _context;
        private DanceStyle? _currentStyle;

        public StylesWindow()
        {
            InitializeComponent();
            _context = App.DbContext;
            LoadData();
        }

        private void LoadData()
        {
            try
            {
                var teachers = _context.teachers
                    .OrderBy(t => t.FullName ?? "")
                    .ToList();

                var styles = _context.styles
                    .Include(s => s.Studio)
                    .OrderBy(s => s.Id)
                    .ToList();

                dgStyles.ItemsSource = styles.Select(s => new
                {
                    s.Id,
                    Name = s.Name ?? "",
                    ScheduleOptions = s.ScheduleOptions ?? "",
                    TeacherName = teachers.FirstOrDefault(t => t.Id == s.TeacherId)?.FullName ?? "",
                    s.TeacherId,
                    s.StudioId
                }).ToList();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки направлений:\n{ex.Message}\n\n{ex.InnerException?.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void dgStyles_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (dgStyles.SelectedItem == null) return;

            var selectedItem = dgStyles.SelectedItem;
            var idProperty = selectedItem.GetType().GetProperty("Id");
            if (idProperty?.GetValue(selectedItem) is not int styleId) return;

            _currentStyle = _context.styles.FirstOrDefault(s => s.Id == styleId);
            if (_currentStyle == null) return;

            txtName.Text = _currentStyle.Name ?? "";
            txtScheduleOptions.Text = _currentStyle.ScheduleOptions ?? "";

            var teacher = _currentStyle.TeacherId.HasValue
                ? _context.teachers.FirstOrDefault(t => t.Id == _currentStyle.TeacherId.Value)
                : null;

            txtTeachers.Text = teacher?.FullName ?? "";
        }

        private void save_btn_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(txtName.Text))
                {
                    MessageBox.Show("Заполните поле \"Направление\"", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var teacherName = txtTeachers.Text.Trim();
                Teacher? teacher = null;

                if (!string.IsNullOrWhiteSpace(teacherName))
                {
                    teacher = _context.teachers
                        .AsEnumerable()
                        .FirstOrDefault(t => string.Equals(t.FullName?.Trim(), teacherName, StringComparison.OrdinalIgnoreCase));

                    if (teacher == null)
                    {
                        MessageBox.Show("Преподаватель не найден. Введите ФИО так же, как оно записано в окне тренеров.",
                            "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }
                }

                var isNewStyle = _currentStyle == null;
                _currentStyle ??= new DanceStyle();

                _currentStyle.Name = txtName.Text.Trim();
                _currentStyle.ScheduleOptions = txtScheduleOptions.Text.Trim();
                _currentStyle.TeacherId = teacher?.Id;

                if (isNewStyle)
                {
                    var firstStudioId = _context.studios.Select(s => (int?)s.Id).FirstOrDefault();
                    if (!firstStudioId.HasValue)
                    {
                        MessageBox.Show("Сначала добавьте студию в базу данных, иначе направление нельзя сохранить.",
                            "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    _currentStyle.StudioId = firstStudioId.Value;
                    _context.styles.Add(_currentStyle);
                }

                _context.SaveChanges();

                MessageBox.Show("Направление успешно сохранено!", "Успех",
                    MessageBoxButton.OK, MessageBoxImage.Information);

                LoadData();
                ClearFields();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка сохранения:\n{ex.Message}\n\n{ex.InnerException?.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void clear_btn_Click(object sender, RoutedEventArgs e)
        {
            ClearFields();
        }

        private void ClearFields()
        {
            _currentStyle = null;
            txtName.Text = "";
            txtScheduleOptions.Text = "";
            txtTeachers.Text = "";
            dgStyles.SelectedItem = null;
        }

        private void delete_btn_Click(object sender, RoutedEventArgs e)
        {
            if (_currentStyle == null)
            {
                MessageBox.Show("Выберите направление для удаления", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var result = MessageBox.Show($"Удалить направление {_currentStyle.Name}?",
                "Подтверждение", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes) return;

            try
            {
                foreach (var client in _context.clients.Where(c => c.StyleId == _currentStyle.Id))
                {
                    client.StyleId = null;
                }

                foreach (var trial in _context.trials.Where(t => t.StyleId == _currentStyle.Id))
                {
                    trial.StyleId = null;
                }

                foreach (var payment in _context.paymentLogs.Where(p => p.StyleId == _currentStyle.Id))
                {
                    payment.StyleId = null;
                }

                _context.styles.Remove(_currentStyle);
                _context.SaveChanges();

                MessageBox.Show("Направление удалено", "Успех",
                    MessageBoxButton.OK, MessageBoxImage.Information);

                LoadData();
                ClearFields();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка удаления:\n{ex.Message}\n\n{ex.InnerException?.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void update_btn_Click(object sender, RoutedEventArgs e)
        {
            LoadData();
            ClearFields();
        }

        private void BtnAddNew_Click(object sender, RoutedEventArgs e)
        {
            ClearFields();
            txtName.Focus();
        }

        private void Logo_Click(object sender, RoutedEventArgs e)
        {
            var mainWindow = new MainWindow();
            mainWindow.Show();
            Close();
        }

        private void menu_btn_Click(object sender, RoutedEventArgs e)
        {
            menuPopup.IsOpen = true;
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
        }
    }
}
