using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using LAM_App.Data;
using LAM_App.Models;
using Microsoft.EntityFrameworkCore;

namespace LAM_App
{
    public partial class MarkAttendanceWindow : Window
    {
        private readonly AppDbContext _context;
        private readonly List<CheckBox> _absentCheckBoxes = new();

        public int SelectedStyleId { get; private set; }
        public DateTime SessionDate { get; private set; }
        public int ChildrenCount { get; private set; }
        public int NewSubscriptionLessons { get; private set; } = 12;
        public int WriteOffCount { get; private set; } = 1;
        public int? SubstituteTeacherId { get; private set; }
        public string? SubstituteTeacherName { get; private set; }
        public List<int> AbsentClientIds { get; private set; } = new();
        public bool IsSaved { get; private set; }

        public MarkAttendanceWindow(AppDbContext context, int? styleId = null)
        {
            InitializeComponent();
            _context = context;

            cbStyle.ItemsSource = _context.styles.OrderBy(s => s.Name).ToList();
            cbSubstituteTeacher.ItemsSource = _context.teachers.OrderBy(t => t.FullName ?? "").ToList();
            dpSessionDate.SelectedDate = DateTime.Today;

            if (styleId.HasValue)
            {
                cbStyle.SelectedValue = styleId.Value;
            }
            else
            {
                cbStyle.SelectedIndex = 0;
            }
        }

        private void cbStyle_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            LoadClients();
        }

        private void LoadClients()
        {
            AbsentListPanel.Children.Clear();
            _absentCheckBoxes.Clear();

            if (cbStyle.SelectedValue == null) return;
            var styleId = Convert.ToInt32(cbStyle.SelectedValue);

            var clients = _context.clients
                .Where(c => c.StyleId == styleId)
                .OrderBy(c => c.ChildSurname ?? "")
                .ThenBy(c => c.ChildName ?? "")
                .AsNoTracking()
                .ToList();

            txtChildrenCount.Text = clients.Count.ToString();

            foreach (var client in clients)
            {
                var checkBox = new CheckBox
                {
                    Content = GetClientName(client),
                    Tag = client.Id,
                    Margin = new Thickness(0, 4, 0, 4)
                };

                _absentCheckBoxes.Add(checkBox);
                AbsentListPanel.Children.Add(checkBox);
            }
        }

        private void save_btn_Click(object sender, RoutedEventArgs e)
        {
            if (cbStyle.SelectedValue == null)
            {
                MessageBox.Show("Выберите направление.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!dpSessionDate.SelectedDate.HasValue)
            {
                MessageBox.Show("Выберите дату занятия.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!int.TryParse(txtChildrenCount.Text, out var childrenCount) || childrenCount < 0)
            {
                MessageBox.Show("Количество детей должно быть числом.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            SelectedStyleId = Convert.ToInt32(cbStyle.SelectedValue);
            SessionDate = DateTime.SpecifyKind(dpSessionDate.SelectedDate.Value.Date, DateTimeKind.Utc);
            ChildrenCount = childrenCount;
            NewSubscriptionLessons = GetComboInt(cbSubscriptionLessons, 12);
            WriteOffCount = GetComboInt(cbWriteOffCount, 1);
            SubstituteTeacherId = cbSubstituteTeacher.SelectedValue == null ? null : Convert.ToInt32(cbSubstituteTeacher.SelectedValue);
            SubstituteTeacherName = cbSubstituteTeacher.Text?.Trim();
            AbsentClientIds = _absentCheckBoxes
                .Where(c => c.IsChecked == true)
                .Select(c => (int)c.Tag)
                .ToList();

            IsSaved = true;
            Close();
        }

        private void cancel_btn_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private static int GetComboInt(ComboBox comboBox, int fallback)
        {
            var text = (comboBox.SelectedItem as ComboBoxItem)?.Content?.ToString();
            return int.TryParse(text, out var result) ? result : fallback;
        }

        private static string GetClientName(Client client)
        {
            return string.Join(" ", new[] { client.ChildSurname, client.ChildName, client.ChildPatronymic }
                .Where(s => !string.IsNullOrWhiteSpace(s)));
        }
    }
}
