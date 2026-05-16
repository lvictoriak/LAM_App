using System;
using System.Linq;
using System.Windows;
using LAM_App.Data;

namespace LAM_App
{
    public partial class PaymentReportWindow : Window
    {
        private readonly AppDbContext _context;

        public PaymentReportWindow()
        {
            InitializeComponent();
            _context = App.DbContext;
            dpFromDate.SelectedDate = DateTime.Today.AddDays(-30);
            dpToDate.SelectedDate = DateTime.Today;
        }

        private void BtnGenerateReport_Click(object sender, RoutedEventArgs e)
        {
            if (!dpFromDate.SelectedDate.HasValue || !dpToDate.SelectedDate.HasValue)
            {
                MessageBox.Show("Выберите даты периода", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var fromDate = dpFromDate.SelectedDate.Value.Date;
            var toDate = dpToDate.SelectedDate.Value.Date.AddDays(1);

            var payments = _context.paymentLogs
                .Where(p => p.PaymentDate >= fromDate && p.PaymentDate < toDate)
                .ToList();

            decimal totalIncome = payments.Sum(p => p.Income ?? 0);
            decimal totalExpense = payments.Sum(p => p.Expense ?? 0);
            decimal netProfit = totalIncome - totalExpense;

            txtTotalIncome.Text = $"{totalIncome:F2} ₽";
            txtTotalExpense.Text = $"{totalExpense:F2} ₽";
            txtNetProfit.Text = $"{netProfit:F2} ₽";

            if (netProfit >= 0)
                txtNetProfit.Foreground = System.Windows.Media.Brushes.Green;
            else
                txtNetProfit.Foreground = System.Windows.Media.Brushes.Red;
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}