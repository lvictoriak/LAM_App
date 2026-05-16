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
    public partial class PaymentsWindow : Window
    {
        private readonly AppDbContext _context;
        private PaymentLog _currentPayment;

        public PaymentsWindow()
        {
            InitializeComponent();
            _context = App.DbContext;
            LoadData();
        }

        private void LoadData()
        {
            var payments = _context.paymentLogs
                .Select(p => new
                {
                    p.PaymentId,
                    p.PaymentDate,
                    p.Income,
                    p.Expense,
                    PaymentTypeName = p.PaymentType != null ? p.PaymentType.Name : "",
                    CategoryName = p.IncomeCategory != null ? p.IncomeCategory.Name : "",
                    StyleName = p.Style != null ? p.Style.Name : "",
                    p.Contractor,
                    p.Comment
                })
                .OrderByDescending(p => p.PaymentDate)
                .ToList();

            dgPayments.ItemsSource = payments;

            //загрузка справочников
            cbPaymentType.ItemsSource = _context.paymentTypes.ToList();
            cbCategory.ItemsSource = _context.incomes.ToList();
            cbStyle.ItemsSource = _context.styles.ToList();

            LoadDebtors();
        }

        private void LoadDebtors()
        {
            var debtors = _context.attendanceSubscriptions
                .Include(s => s.Client)
                .Include(s => s.Style)
                .Where(s => !s.IsPaid && s.UsedLessons > 0)
                .OrderBy(s => s.Client!.ChildSurname)
                .ThenBy(s => s.Client!.ChildName)
                .AsEnumerable()
                .Select(s => new
                {
                    ChildName = string.Join(" ", new[] { s.Client?.ChildSurname, s.Client?.ChildName }
                        .Where(v => !string.IsNullOrWhiteSpace(v))),
                    StyleName = s.Style?.Name ?? "",
                    SubscriptionInfo = $"{s.TotalLessons} занятий, использовано {s.UsedLessons}"
                })
                .ToList();

            dgDebtors.ItemsSource = debtors;
        }

        private void NumberValidation(object sender, TextCompositionEventArgs e)
        {
            Regex regex = new Regex("[^0-9.]+");
            e.Handled = regex.IsMatch(e.Text);
        }

        //выбор строки в таблице
        private void dgPayments_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (dgPayments.SelectedItem == null) return;

            var selectedItem = dgPayments.SelectedItem;
            int paymentId = ((dynamic)selectedItem).PaymentId;

            var selectedPayment = _context.paymentLogs
                .FirstOrDefault(p => p.PaymentId == paymentId);

            if (selectedPayment != null)
            {
                _currentPayment = selectedPayment;
                dpPaymentDate.SelectedDate = selectedPayment.PaymentDate;
                txtIncome.Text = selectedPayment.Income?.ToString();
                txtExpense.Text = selectedPayment.Expense?.ToString();
                cbPaymentType.SelectedValue = selectedPayment.PaymentTypeId;
                cbCategory.SelectedValue = selectedPayment.CategoryId;
                cbStyle.SelectedValue = selectedPayment.StyleId;
                txtContractor.Text = selectedPayment.Contractor;
                txtComment.Text = selectedPayment.Comment;
            }
        }

        private void clear_btn_Click(object sender, RoutedEventArgs e)
        {
            ClearFields();
        }

        private void ClearFields()
        {
            dpPaymentDate.SelectedDate = DateTime.Today;
            txtIncome.Text = "";
            txtExpense.Text = "";
            cbPaymentType.SelectedIndex = -1;
            cbCategory.SelectedIndex = -1;
            cbStyle.SelectedIndex = -1;
            txtContractor.Text = "";
            txtComment.Text = "";
            _currentPayment = null;
            dgPayments.SelectedItem = null;
        }

        private void save_btn_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (cbPaymentType.SelectedValue == null || cbCategory.SelectedValue == null)
                {
                    MessageBox.Show("Обязательно выберите 'Вид оплаты' и 'Статью'!", "Ошибка ввода", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (_currentPayment == null)
                {
                    // Создание новой записи
                    _currentPayment = new PaymentLog();
                    _context.paymentLogs.Add(_currentPayment);
                }

                var date = dpPaymentDate.SelectedDate ?? DateTime.Today;
                _currentPayment.PaymentDate = DateTime.SpecifyKind(date, DateTimeKind.Unspecified);
                _currentPayment.Income = string.IsNullOrEmpty(txtIncome.Text) ? (decimal?)null : decimal.Parse(txtIncome.Text);
                _currentPayment.Expense = string.IsNullOrEmpty(txtExpense.Text) ? (decimal?)null : decimal.Parse(txtExpense.Text);
                _currentPayment.PaymentTypeId = cbPaymentType.SelectedValue != null ? Convert.ToInt32(cbPaymentType.SelectedValue) : (int?)null;
                _currentPayment.CategoryId = cbCategory.SelectedValue != null ? Convert.ToInt32(cbCategory.SelectedValue) : (int?)null;
                _currentPayment.StyleId = cbStyle.SelectedValue != null ? Convert.ToInt32(cbStyle.SelectedValue) : (int?)null;
                _currentPayment.Contractor = txtContractor.Text;
                _currentPayment.Comment = txtComment.Text;
                _currentPayment.ExtraInfo = "";

                _context.SaveChanges();
                MessageBox.Show("Данные успешно сохранены", "Успех",
                    MessageBoxButton.OK, MessageBoxImage.Information);

                LoadData();
                ClearFields();
            }
            catch (Exception ex)
            {
                string errorMsg = ex.Message;
                if (ex.InnerException != null)
                {
                    errorMsg += "\n\nПричина (SQL): " + ex.InnerException.Message;
                }
                MessageBox.Show(errorMsg, "Ошибка сохранения", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void delete_btn_Click(object sender, RoutedEventArgs e)
        {
            if (_currentPayment == null)
            {
                MessageBox.Show("Выберите запись для удаления", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var result = MessageBox.Show("Вы уверены, что хотите удалить запись?", "Подтверждение",
                MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    _context.paymentLogs.Remove(_currentPayment);
                    _context.SaveChanges();
                    MessageBox.Show("Запись удалена", "Успех",
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

        //обновление
        private void update_btn_Click(object sender, RoutedEventArgs e)
        {
            LoadData();
            ClearFields();
        }

        //Отчет
        private void report_btn_Click(object sender, RoutedEventArgs e)
        {
            var reportWindow = new PaymentReportWindow();
            reportWindow.ShowDialog();
        }

        private void Logo_Click(object sender, RoutedEventArgs e)
        {
            var mainWindow = new MainWindow();
            mainWindow.Show();
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
