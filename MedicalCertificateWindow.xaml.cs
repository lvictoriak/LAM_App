using System;
using System.Linq;
using System.Windows;
using LAM_App.Data;

namespace LAM_App
{
    public partial class MedicalCertificateWindow : Window
    {
        private readonly AppDbContext _context;
        private readonly int _styleId;

        public int ClientId { get; private set; }
        public DateTime DateFrom { get; private set; }
        public DateTime DateTo { get; private set; }
        public bool IsSaved { get; private set; }

        public MedicalCertificateWindow(AppDbContext context, int styleId)
        {
            InitializeComponent();
            _context = context;
            _styleId = styleId;

            cbClient.ItemsSource = _context.clients
                .Where(c => c.StyleId == _styleId)
                .OrderBy(c => c.ChildSurname ?? "")
                .ThenBy(c => c.ChildName ?? "")
                .AsEnumerable()
                .Select(c => new ClientListItem(
                    c.Id,
                    string.Join(" ", new[] { c.ChildSurname, c.ChildName, c.ChildPatronymic }
                        .Where(s => !string.IsNullOrWhiteSpace(s)))))
                .ToList();

            cbClient.SelectedIndex = cbClient.Items.Count > 0 ? 0 : -1;
            dpFrom.SelectedDate = DateTime.Today;
            dpTo.SelectedDate = DateTime.Today;
        }

        private void save_btn_Click(object sender, RoutedEventArgs e)
        {
            if (cbClient.SelectedValue == null)
            {
                MessageBox.Show("Выберите ребенка.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!dpFrom.SelectedDate.HasValue || !dpTo.SelectedDate.HasValue)
            {
                MessageBox.Show("Укажите две даты справки.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var from = dpFrom.SelectedDate.Value.Date;
            var to = dpTo.SelectedDate.Value.Date;
            if (from > to)
            {
                MessageBox.Show("Дата начала справки не может быть позже даты окончания.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            ClientId = Convert.ToInt32(cbClient.SelectedValue);
            DateFrom = DateTime.SpecifyKind(from, DateTimeKind.Utc);
            DateTo = DateTime.SpecifyKind(to, DateTimeKind.Utc);
            IsSaved = true;
            Close();
        }

        private void cancel_btn_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private record ClientListItem(int Id, string DisplayName);
    }
}
