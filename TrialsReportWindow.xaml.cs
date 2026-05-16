using System;
using System.Linq;
using System.Windows;
using LAM_App.Models;
using Microsoft.EntityFrameworkCore;

namespace LAM_App
{
    public partial class TrialsReportWindow : Window
    {
        public TrialsReportWindow()
        {
            InitializeComponent();
            dpFrom.SelectedDate = DateTime.Today.AddDays(-30);
            dpTo.SelectedDate = DateTime.Today;
        }

        private void generate_btn_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!dpFrom.SelectedDate.HasValue || !dpTo.SelectedDate.HasValue) return;

                var from = ToUtcDate(dpFrom.SelectedDate.Value);
                var to = ToUtcDate(dpTo.SelectedDate.Value.AddDays(1));

                var records = App.DbContext.trials
                    .Include(r => r.Status)
                    .Where(r => r.TrialDate.HasValue &&
                                r.TrialDate.Value >= from &&
                                r.TrialDate.Value < to)
                    .ToList();

                var statuses = App.DbContext.trialStatuses.ToList();
                int scheduledStatusId = FindStatusId(statuses, "На пробном") ?? 1;
                int attendedStatusId = FindStatusId(statuses, "Пришли") ?? 2;
                int boughtStatusId = FindStatusId(statuses, "Купили") ?? 3;
                int otherStatusId = FindStatusId(statuses, "Другое") ?? 4;

                int scheduledCount = records.Count(r => HasStatus(r, "На пробном", scheduledStatusId));
                int attendedCount = records.Count(r => HasStatus(r, "Пришли", attendedStatusId));
                int boughtCount = records.Count(r => HasStatus(r, "Купили", boughtStatusId));
                int otherCount = records.Count(r => HasStatus(r, "Другое", otherStatusId) ||
                                                    (!HasStatus(r, "На пробном", scheduledStatusId) &&
                                                     !HasStatus(r, "Пришли", attendedStatusId) &&
                                                     !HasStatus(r, "Купили", boughtStatusId)));

                txtTotal.Text = $"Всего записей: {records.Count}";
                txtScheduled.Text = $"На пробном: {scheduledCount}";
                txtAttended.Text = $"Пришли: {attendedCount}";
                txtBought.Text = $"Купили: {boughtCount}";
                txtOther.Text = $"Другое: {otherCount}";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка формирования отчета:\n{ex.Message}\n\n{ex.InnerException?.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void close_btn_Click(object sender, RoutedEventArgs e) => Close();

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

        private static DateTime ToUtcDate(DateTime date)
        {
            return DateTime.SpecifyKind(date.Date, DateTimeKind.Utc);
        }
    }
}
