using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using LAM_App.Data;
using LAM_App.Models;
using Microsoft.EntityFrameworkCore;

namespace LAM_App
{
    public partial class TrialsWindow : Window
    {
        private readonly AppDbContext _context;
        private TrialRecord? _currentRecord;

        public TrialsWindow()
        {
            InitializeComponent();
            _context = App.DbContext;
            LoadData();
        }

        private void LoadData()
        {
            try
            {
                if (_context == null)
                {
                    MessageBox.Show("База данных не подключена!", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                var allRecords = _context.trials
                    .Include(r => r.Status)
                    .Include(r => r.Style)
                    .Include(r => r.Instructor)
                    .ToList();
                var statuses = _context.trialStatuses.ToList();
                int scheduledStatusId = FindStatusId(statuses, "На пробном") ?? 1;
                int attendedStatusId = FindStatusId(statuses, "Пришли") ?? 2;
                int boughtStatusId = FindStatusId(statuses, "Купили") ?? 3;
                int otherStatusId = FindStatusId(statuses, "Другое") ?? 4;

                txtGroup.ItemsSource = _context.styles.OrderBy(s => s.Name).ToList();

                lbScheduled.ItemsSource = allRecords.Where(r => HasStatus(r, "На пробном", scheduledStatusId)).ToList();
                lbAttended.ItemsSource = allRecords.Where(r => HasStatus(r, "Пришли", attendedStatusId)).ToList();
                lbBought.ItemsSource = allRecords.Where(r => HasStatus(r, "Купили", boughtStatusId)).ToList();
                lbOther.ItemsSource = allRecords
                    .Where(r => HasStatus(r, "Другое", otherStatusId) ||
                                (!HasStatus(r, "На пробном", scheduledStatusId) &&
                                 !HasStatus(r, "Пришли", attendedStatusId) &&
                                 !HasStatus(r, "Купили", boughtStatusId)))
                    .ToList();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки данных:\n{ex.Message}\n\n{ex.InnerException?.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var listBox = sender as ListBox;
            if (listBox == null) return;
            if (listBox.SelectedItem == null) return;

            _currentRecord = listBox.SelectedItem as TrialRecord;
            if (_currentRecord == null) return;

            SelectStatus(_currentRecord.Status?.Name);
            txtParentName.Text = _currentRecord.ParentName ?? "";
            txtPhone.Text = _currentRecord.ParentPhone ?? "";
            txtChildName.Text = _currentRecord.ChildName ?? "";
            txtAge.Text = _currentRecord.ChildAge?.ToString();
            txtGroup.SelectedValue = _currentRecord.StyleId;
            txtGroup.Text = _currentRecord.Style?.Name ?? "";
            dpDate.SelectedDate = _currentRecord.TrialDate;
            txtComment.Text = _currentRecord.Comment ?? "";
        }

        private void save_btn_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(txtParentName.Text) ||
                    string.IsNullOrWhiteSpace(txtPhone.Text) ||
                    string.IsNullOrWhiteSpace(txtChildName.Text))
                {
                    MessageBox.Show("Заполните обязательные поля: ФИО родителя, телефон, ФИО ребенка",
                        "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var statusName = GetSelectedStatusName();
                var selectedStatus = GetOrCreateTrialStatus(statusName);
                if (selectedStatus == null)
                {
                    MessageBox.Show("Выберите корректный статус пробного занятия", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var selectedStyleId = txtGroup.SelectedValue as int?;
                var styleName = txtGroup.Text.Trim();
                if (selectedStyleId == null && string.IsNullOrWhiteSpace(styleName))
                {
                    MessageBox.Show("Укажите направление пробного занятия",
                        "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var style = selectedStyleId == null
                    ? _context.styles.FirstOrDefault(s => s.Name == styleName)
                    : _context.styles.FirstOrDefault(s => s.Id == selectedStyleId);
                if (style == null)
                {
                    MessageBox.Show("Направление не найдено. Проверьте название направления.",
                        "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                int? childAge = string.IsNullOrWhiteSpace(txtAge.Text) ? null : int.Parse(txtAge.Text);
                bool isNewRecord = _currentRecord == null;
                _currentRecord ??= new TrialRecord();

                _currentRecord.StatusId = selectedStatus.Id;
                _currentRecord.ParentName = txtParentName.Text.Trim();
                _currentRecord.ParentPhone = txtPhone.Text.Trim();
                _currentRecord.ChildName = txtChildName.Text.Trim();
                _currentRecord.ChildAge = childAge;

                var selectedDate = dpDate.SelectedDate ?? DateTime.Today;
                _currentRecord.TrialDate = ToUtcDate(selectedDate);
                _currentRecord.RecordDate = ToUtcDate(_currentRecord.RecordDate ?? DateTime.UtcNow);
                _currentRecord.Comment = txtComment.Text.Trim();
                _currentRecord.StyleId = style.Id;
                _currentRecord.InstructorId = style.TeacherId;

                if (isNewRecord)
                {
                    _context.trials.Add(_currentRecord);
                }

                _context.SaveChanges();
                MessageBox.Show("Сохранено!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);

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

        private void clear_btn_Click(object sender, RoutedEventArgs e) => ClearFields();

        private void ClearFields()
        {
            _currentRecord = null;
            lbScheduled.SelectedItem = null;
            lbAttended.SelectedItem = null;
            lbBought.SelectedItem = null;
            lbOther.SelectedItem = null;

            cbStatus.SelectedIndex = 0;
            txtParentName.Text = "";
            txtPhone.Text = "";
            txtChildName.Text = "";
            txtAge.Text = "";
            txtGroup.SelectedIndex = -1;
            txtGroup.Text = "";
            dpDate.SelectedDate = DateTime.Today;
            txtComment.Text = "";
        }

        private void delete_btn_Click(object sender, RoutedEventArgs e)
        {
            if (_currentRecord != null)
            {
                var res = MessageBox.Show("Удалить запись?", "Подтверждение", MessageBoxButton.YesNo);
                if (res == MessageBoxResult.Yes)
                {
                    try
                    {
                        _context.trials.Remove(_currentRecord);
                        _context.SaveChanges();
                        LoadData();
                        ClearFields();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Ошибка удаления:\n{ex.Message}\n\n{ex.InnerException?.Message}",
                            "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
        }

        private void update_btn_Click(object sender, RoutedEventArgs e) => LoadData();

        private void SelectStatus(string? statusName)
        {
            foreach (ComboBoxItem item in cbStatus.Items)
            {
                if (NormalizeStatus(item.Content as string) == NormalizeStatus(statusName))
                {
                    cbStatus.SelectedItem = item;
                    return;
                }
            }

            cbStatus.SelectedIndex = 0;
        }

        private static int? FindStatusId(System.Collections.Generic.IEnumerable<TrialStatus> statuses, string statusName)
        {
            return statuses
                .FirstOrDefault(s => NormalizeStatus(s.Name) == NormalizeStatus(statusName))
                ?.Id;
        }

        private static bool HasStatus(TrialRecord record, string statusName, int statusId)
        {
            return record.StatusId == statusId ||
                   NormalizeStatus(record.Status?.Name) == NormalizeStatus(statusName);
        }

        private static string NormalizeStatus(string? statusName)
        {
            return (statusName ?? "").Trim().ToLowerInvariant();
        }

        private string? GetSelectedStatusName()
        {
            if (cbStatus.SelectedItem is ComboBoxItem item)
            {
                return item.Content?.ToString();
            }

            return cbStatus.Text;
        }

        private TrialStatus? GetOrCreateTrialStatus(string? statusName)
        {
            var normalizedStatus = NormalizeStatus(statusName);
            if (string.IsNullOrWhiteSpace(normalizedStatus)) return null;

            var selectedStatus = _context.trialStatuses
                .ToList()
                .FirstOrDefault(s => NormalizeStatus(s.Name) == normalizedStatus);

            if (selectedStatus != null) return selectedStatus;

            selectedStatus = new TrialStatus
            {
                Name = statusName!.Trim()
            };

            _context.trialStatuses.Add(selectedStatus);
            _context.SaveChanges();
            return selectedStatus;
        }

        private static DateTime ToUtcDate(DateTime date)
        {
            return DateTime.SpecifyKind(date.Date, DateTimeKind.Utc);
        }

        private void report_btn_Click(object sender, RoutedEventArgs e)
        {
            var reportWindow = new TrialsReportWindow();
            reportWindow.ShowDialog();
        }
        private void Logo_Click(object sender, RoutedEventArgs e) 
        { 
            var w = new MainWindow(); 
            w.Show(); 
            this.Close(); 
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
            this.Close();
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
