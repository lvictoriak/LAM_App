using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using LAM_App.Data;
using LAM_App.Models;
using Microsoft.EntityFrameworkCore;

namespace LAM_App
{
    public partial class TeachersWindow : Window
    {
        private readonly AppDbContext _context;
        private Teacher? _currentTeacher;

        public TeachersWindow()
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
                    .Include(t => t.Studio)
                    .OrderBy(t => t.FullName ?? "")
                    .ToList();

                var styles = _context.styles
                    .OrderBy(s => s.Name)
                    .ToList();

                dgTeachers.ItemsSource = teachers.Select(t => new
                {
                    t.Id,
                    FullName = t.FullName ?? "",
                    Phone = t.Phone ?? "",
                    t.Age,
                    DanceExperience = t.DanceExperience ?? "",
                    Comment = t.Comment ?? "",
                    t.StudioId,
                    DirectionsText = string.Join("; ", styles
                        .Where(s => s.TeacherId == t.Id)
                        .Select(s => s.Name)
                        .Where(n => !string.IsNullOrWhiteSpace(n))
                        .Distinct())
                }).ToList();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки тренеров:\n{ex.Message}\n\n{ex.InnerException?.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void dgTeachers_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (dgTeachers.SelectedItem == null) return;

            var selectedItem = dgTeachers.SelectedItem;
            var idProperty = selectedItem.GetType().GetProperty("Id");
            if (idProperty?.GetValue(selectedItem) is not int teacherId) return;

            _currentTeacher = _context.teachers
                .Include(t => t.Studio)
                .FirstOrDefault(t => t.Id == teacherId);

            if (_currentTeacher == null) return;

            txtFullName.Text = _currentTeacher.FullName ?? "";
            txtPhone.Text = _currentTeacher.Phone ?? "";
            txtAge.Text = _currentTeacher.Age?.ToString() ?? "";
            txtDanceExperience.Text = _currentTeacher.DanceExperience ?? "";
            txtComment.Text = _currentTeacher.Comment ?? "";

            var directions = _context.styles
                .Where(s => s.TeacherId == _currentTeacher.Id)
                .OrderBy(s => s.Name)
                .Select(s => s.Name)
                .ToList();

            txtDirections.Text = string.Join("; ", directions);
        }

        private void save_btn_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(txtFullName.Text))
                {
                    MessageBox.Show("ФИО обязательно для заполнения", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                int? age = string.IsNullOrWhiteSpace(txtAge.Text) ? null : int.Parse(txtAge.Text);
                bool isNewTeacher = _currentTeacher == null;
                _currentTeacher ??= new Teacher();

                _currentTeacher.FullName = txtFullName.Text.Trim();
                _currentTeacher.Phone = txtPhone.Text.Trim();
                _currentTeacher.Age = age;
                _currentTeacher.DanceExperience = txtDanceExperience.Text.Trim();
                _currentTeacher.Comment = txtComment.Text.Trim();

                if (isNewTeacher)
                {
                    var firstStudioId = _context.studios.Select(s => (int?)s.Id).FirstOrDefault();
                    _currentTeacher.StudioId = firstStudioId;
                    _context.teachers.Add(_currentTeacher);
                }

                _context.SaveChanges();
                var missingDirections = SaveDirections();
                _context.SaveChanges();

                var message = "Тренер успешно сохранен!";
                if (!string.IsNullOrWhiteSpace(missingDirections))
                {
                    message += $"\n\nНе найдены направления: {missingDirections}";
                }

                MessageBox.Show(message, "Успех", MessageBoxButton.OK, MessageBoxImage.Information);

                LoadData();
                ClearFields();
            }
            catch (FormatException)
            {
                MessageBox.Show("Возраст должен быть числом", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка сохранения:\n{ex.Message}\n\n{ex.InnerException?.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private string SaveDirections()
        {
            if (_currentTeacher == null) return "";

            var directionNames = txtDirections.Text
                .Split(new[] { ';', ',' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(d => d.Trim())
                .Where(d => !string.IsNullOrWhiteSpace(d))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            var currentStyles = _context.styles
                .Where(s => s.TeacherId == _currentTeacher.Id)
                .ToList();

            foreach (var style in currentStyles)
            {
                style.TeacherId = null;
            }

            if (!directionNames.Any())
            {
                return "";
            }

            var allStyles = _context.styles.ToList();
            var missingDirections = directionNames.ToList();

            foreach (var directionName in directionNames)
            {
                var style = allStyles.FirstOrDefault(s =>
                    string.Equals(s.Name, directionName, StringComparison.OrdinalIgnoreCase));

                if (style == null) continue;

                style.TeacherId = _currentTeacher.Id;
                missingDirections.RemoveAll(d =>
                    string.Equals(d, directionName, StringComparison.OrdinalIgnoreCase));
            }

            return string.Join("; ", missingDirections);
        }

        private void clear_btn_Click(object sender, RoutedEventArgs e)
        {
            ClearFields();
        }

        private void ClearFields()
        {
            _currentTeacher = null;
            txtFullName.Text = "";
            txtPhone.Text = "";
            txtAge.Text = "";
            txtDirections.Text = "";
            txtDanceExperience.Text = "";
            txtComment.Text = "";
            dgTeachers.SelectedItem = null;
        }

        private void delete_btn_Click(object sender, RoutedEventArgs e)
        {
            if (_currentTeacher == null)
            {
                MessageBox.Show("Выберите тренера для удаления", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var result = MessageBox.Show($"Удалить тренера {_currentTeacher.FullName}?",
                "Подтверждение", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes) return;

            try
            {
                foreach (var style in _context.styles.Where(s => s.TeacherId == _currentTeacher.Id))
                {
                    style.TeacherId = null;
                }

                foreach (var trial in _context.trials.Where(t => t.InstructorId == _currentTeacher.Id))
                {
                    trial.InstructorId = null;
                }

                _context.teachers.Remove(_currentTeacher);
                _context.SaveChanges();

                MessageBox.Show("Тренер удален", "Успех",
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
            txtFullName.Focus();
        }

        private void PhoneValidation(object sender, TextCompositionEventArgs e)
        {
            Regex regex = new Regex("[^0-9+\\-\\s()]+");
            e.Handled = regex.IsMatch(e.Text);
        }

        private void NumberValidation(object sender, TextCompositionEventArgs e)
        {
            Regex regex = new Regex("[^0-9]+");
            e.Handled = regex.IsMatch(e.Text);
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
            var clientWindow = new ClientsWindow();
            clientWindow.Show();
            Close();
        }

        private void teachers_btn_Click(object sender, RoutedEventArgs e)
        {
            menuPopup.IsOpen = false;
            var teacherWindow = new TeachersWindow();
            teacherWindow.Show();
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
