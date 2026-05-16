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
    public partial class ClientsWindow : Window
    {
        private readonly AppDbContext _context;
        private Client? _currentClient;
        private DataGrid? _selectedDataGrid;

        public ClientsWindow()
        {
            InitializeComponent();
            _context = App.DbContext;
            try
            {
                LoadData();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки клиентов: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadData()
        {
            var clients = _context.clients
                .Include(c => c.Style)
                .OrderBy(c => c.Style != null ? c.Style.Name : "")
                .ThenBy(c => c.ChildSurname ?? "")
                .ToList();

            GroupsItemsControl.ItemsSource = clients
                .GroupBy(c => c.StyleId)
                .OrderBy(g => g.First().Style?.Name ?? "")
                .Select(g => new
                {
                    GroupName = g.First().Style?.Name ?? "Без группы",
                    GroupSchedule = g.First().Style?.ScheduleOptions ?? "",
                    Clients = g.ToList()
                })
                .ToList();

            cbGroup.ItemsSource = _context.styles.ToList();
        }

        private void DataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sender is DataGrid dataGrid && dataGrid.SelectedItem is Client selectedClient)
            {
                if (_selectedDataGrid != null && _selectedDataGrid != dataGrid)
                {
                    _selectedDataGrid.SelectedItem = null;
                }

                _selectedDataGrid = dataGrid;
                _currentClient = selectedClient;

                txtChildSurname.Text = selectedClient.ChildSurname ?? "";
                txtChildName.Text = selectedClient.ChildName ?? "";
                txtChildPatronymic.Text = selectedClient.ChildPatronymic ?? "";
                txtParentName.Text = selectedClient.ParentName ?? "";
                txtPhone.Text = selectedClient.ParentPhone ?? "";
                txtAge.Text = selectedClient.Age?.ToString();
                dpBirthDate.SelectedDate = selectedClient.BirthDate;
                cbGroup.SelectedValue = selectedClient.StyleId;
                txtShift.Text = selectedClient.Shift ?? "";
                txtComment.Text = selectedClient.Comment ?? "";
            }
        }

        private void DataGrid_LoadingRow(object sender, DataGridRowEventArgs e)
        {
            e.Row.Header = e.Row.GetIndex() + 1;
        }

        private void save_btn_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(txtChildSurname.Text) ||
                    string.IsNullOrWhiteSpace(txtChildName.Text) ||
                    string.IsNullOrWhiteSpace(txtParentName.Text) ||
                    string.IsNullOrWhiteSpace(txtPhone.Text))
                {
                    MessageBox.Show("Заполните обязательные поля: Фамилия, Имя ребенка, ФИО родителя, Телефон",
                        "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (_currentClient == null)
                {
                    _currentClient = new Client();
                    _context.clients.Add(_currentClient);
                }

                _currentClient.ChildSurname = txtChildSurname.Text.Trim();
                _currentClient.ChildName = txtChildName.Text.Trim();
                _currentClient.ChildPatronymic = txtChildPatronymic.Text.Trim();
                _currentClient.ParentName = txtParentName.Text.Trim();
                _currentClient.ParentPhone = txtPhone.Text.Trim();
                _currentClient.Age = string.IsNullOrEmpty(txtAge.Text) ? (int?)null : int.Parse(txtAge.Text);
                _currentClient.BirthDate = dpBirthDate.SelectedDate;
                _currentClient.StyleId = cbGroup.SelectedValue as int?;
                _currentClient.Shift = txtShift.Text.Trim();
                _currentClient.Comment = txtComment.Text.Trim();

                _context.SaveChanges();

                MessageBox.Show("Клиент успешно сохранен!", "Успех",
                    MessageBoxButton.OK, MessageBoxImage.Information);

                LoadData();
                ClearFields();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка сохранения: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void clear_btn_Click(object sender, RoutedEventArgs e)
        {
            ClearFields();
        }

        private void ClearFields()
        {
            _currentClient = null;
            txtChildSurname.Text = "";
            txtChildName.Text = "";
            txtChildPatronymic.Text = "";
            txtParentName.Text = "";
            txtPhone.Text = "";
            txtAge.Text = "";
            dpBirthDate.SelectedDate = null;
            cbGroup.SelectedIndex = -1;
            txtShift.Text = "";
            txtComment.Text = "";

            if (_selectedDataGrid != null)
            {
                _selectedDataGrid.SelectedItem = null;
                _selectedDataGrid = null;
            }
        }

        private void delete_btn_Click(object sender, RoutedEventArgs e)
        {
            if (_currentClient == null)
            {
                MessageBox.Show("Выберите клиента для удаления", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var result = MessageBox.Show($"Удалить клиента {_currentClient.ChildSurname} {_currentClient.ChildName}?",
                "Подтверждение", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    _context.clients.Remove(_currentClient);
                    _context.SaveChanges();

                    MessageBox.Show("Клиент удален", "Успех",
                        MessageBoxButton.OK, MessageBoxImage.Information);

                    LoadData();
                    ClearFields();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка удаления: {ex.Message}", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
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
            txtChildSurname.Focus();
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
