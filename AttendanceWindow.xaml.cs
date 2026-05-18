using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using LAM_App.Data;
using LAM_App.Models;
using Microsoft.EntityFrameworkCore;
using DanceStyle = LAM_App.Models.Style;

namespace LAM_App
{
    public partial class AttendanceWindow : Window
    {
        private readonly AppDbContext _context;
        private readonly Brush _headerBrush = new SolidColorBrush(Color.FromRgb(68, 136, 198));
        private readonly Brush _subHeaderBrush = new SolidColorBrush(Color.FromRgb(198, 126, 166));
        private readonly Brush _totalBrush = new SolidColorBrush(Color.FromRgb(248, 205, 205));
        private readonly Brush _lastLessonBrush = Brushes.Orange;
        private readonly Brush _unpaidBrush = Brushes.Red;
        private readonly Brush _selectedHeaderBrush = new SolidColorBrush(Color.FromRgb(255, 210, 0));
        private readonly Brush _medicalBrush = new SolidColorBrush(Color.FromRgb(222, 240, 255));
        private readonly Brush _whiteBrush = Brushes.White;
        private int? _selectedSessionId;
        private double _lastJournalViewportWidth;

        public AttendanceWindow()
        {
            InitializeComponent();
            _context = App.DbContext;
            dpMonth.SelectedDate = DateTime.Today;
            LoadSelectors();
            Loaded += (_, _) => LoadJournal();
        }

        private void LoadSelectors()
        {
            cbStyle.ItemsSource = _context.styles.OrderBy(s => s.Name).ToList();
            cbStyle.SelectedIndex = cbStyle.Items.Count > 0 ? 0 : -1;
            LoadPaymentSelectors();
        }

        private void LoadPaymentSelectors()
        {
            cbUnpaidSubscriptions.ItemsSource = _context.attendanceSubscriptions
                .Include(s => s.Client)
                .Include(s => s.Style)
                .Where(s => !s.IsPaid && s.UsedLessons > 0)
                .OrderBy(s => s.Client!.ChildSurname)
                .ThenBy(s => s.Client!.ChildName)
                .AsEnumerable()
                .Select(s => new SubscriptionListItem(
                    s.Id,
                    $"{GetClientName(s.Client)} - {s.Style?.Name} ({s.TotalLessons} занятий, использовано {s.UsedLessons})"))
                .ToList();

            cbPayments.ItemsSource = _context.paymentLogs
                .OrderByDescending(p => p.PaymentDate)
                .Take(30)
                .AsEnumerable()
                .Select(p => new PaymentListItem(
                    p.PaymentId,
                    $"{p.PaymentDate:dd.MM.yyyy} - {p.Contractor} - {p.Income}"))
                .ToList();
        }

        private void LoadJournal()
        {
            AttendanceGrid.Children.Clear();
            AttendanceGrid.RowDefinitions.Clear();
            AttendanceGrid.ColumnDefinitions.Clear();
            deleteColumn_btn.IsEnabled = _selectedSessionId.HasValue;

            if (cbStyle.SelectedValue == null || !dpMonth.SelectedDate.HasValue) return;

            var styleId = Convert.ToInt32(cbStyle.SelectedValue);
            var selectedStyle = cbStyle.SelectedItem as DanceStyle;
            var monthStart = DateTime.SpecifyKind(new DateTime(dpMonth.SelectedDate.Value.Year, dpMonth.SelectedDate.Value.Month, 1), DateTimeKind.Utc);
            var monthEnd = monthStart.AddMonths(1);

            var clients = _context.clients
                .Where(c => c.StyleId == styleId)
                .OrderBy(c => c.ChildSurname ?? "")
                .ThenBy(c => c.ChildName ?? "")
                .ToList();

            var sessions = _context.attendanceSessions
                .Include(s => s.Marks)
                    .ThenInclude(m => m.Subscription)
                .Where(s => s.StyleId == styleId && s.SessionDate >= monthStart && s.SessionDate < monthEnd)
                .OrderBy(s => s.SessionDate)
                .ThenBy(s => s.Id)
                .ToList();

            if (_selectedSessionId.HasValue && sessions.All(s => s.Id != _selectedSessionId.Value))
            {
                _selectedSessionId = null;
                deleteColumn_btn.IsEnabled = false;
            }

            var displayedSessionColumns = GetDisplayedSessionColumns(sessions.Count);

            BuildGridStructure(clients.Count, displayedSessionColumns);
            AddCell(0, 0, "", _headerBrush, 42, FontWeights.Bold);
            AddCell(0, 1, selectedStyle?.Name ?? "Направление", _subHeaderBrush, 22, FontWeights.Normal);
            AddCell(1, 0, monthStart.ToString("MMMM").ToUpperInvariant(), _headerBrush, 42, FontWeights.Bold, displayedSessionColumns);

            for (var i = 0; i < sessions.Count; i++)
            {
                AddSessionHeaderCell(i + 1, sessions[i]);
            }

            for (var rowIndex = 0; rowIndex < clients.Count; rowIndex++)
            {
                var client = clients[rowIndex];
                var row = rowIndex + 2;
                AddClientCell(row, rowIndex + 1, client);

                for (var colIndex = 0; colIndex < sessions.Count; colIndex++)
                {
                    var session = sessions[colIndex];
                    var marks = session.Marks.Where(m => m.ClientId == client.Id).OrderBy(m => m.Id).ToList();
                    AddEditableMarkCell(colIndex + 1, row, session, client, marks);
                }
            }

            var totalRow = clients.Count + 2;
            AddCell(0, totalRow, "всего", _totalBrush, 20, FontWeights.Normal);
            for (var colIndex = 0; colIndex < sessions.Count; colIndex++)
            {
                AddCell(colIndex + 1, totalRow, CountPresentChildren(sessions[colIndex].Marks).ToString(), _totalBrush, 20, FontWeights.Normal);
            }

            var teacherRow = totalRow + 1;
            AddCell(0, teacherRow, "", _whiteBrush, 18, FontWeights.Normal);
            for (var colIndex = 0; colIndex < sessions.Count; colIndex++)
            {
                AddCell(colIndex + 1, teacherRow, sessions[colIndex].SubstituteTeacherName ?? "", _whiteBrush, 18, FontWeights.Normal);
            }
        }

        private void BuildGridStructure(int clientsCount, int sessionsCount)
        {
            AttendanceGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(340) });
            for (var i = 0; i < sessionsCount; i++)
            {
                AttendanceGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(54) });
            }

            AttendanceGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(58) });
            AttendanceGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(38) });

            for (var i = 0; i < clientsCount; i++)
            {
                AttendanceGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(34) });
            }

            AttendanceGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(36) });
            AttendanceGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(36) });
        }

        private int GetDisplayedSessionColumns(int sessionsCount)
        {
            const double clientColumnWidth = 340;
            const double sessionColumnWidth = 54;
            const double gridHorizontalMargin = 20;

            var viewportWidth = AttendanceScrollViewer?.ViewportWidth ?? 0;
            if (viewportWidth <= 0)
            {
                viewportWidth = AttendanceScrollViewer?.ActualWidth ?? 0;
            }

            var availableWidth = Math.Max(0, viewportWidth - gridHorizontalMargin - clientColumnWidth);
            var columnsToFillViewport = (int)Math.Ceiling(availableWidth / sessionColumnWidth);

            return Math.Max(1, Math.Max(sessionsCount, columnsToFillViewport));
        }

        private void AddClientCell(int row, int number, Client client)
        {
            var border = CreateBorder(_whiteBrush);
            border.Tag = client.Id;
            border.MouseLeftButtonUp += (_, _) => SelectClientDebt(client.Id);

            var panel = new DockPanel();
            panel.Children.Add(new TextBlock
            {
                Text = number.ToString(),
                Width = 44,
                TextAlignment = TextAlignment.Right,
                Margin = new Thickness(0, 0, 10, 0),
                FontSize = 20
            });
            panel.Children.Add(new TextBlock
            {
                Text = GetClientName(client),
                FontSize = 20,
                TextTrimming = TextTrimming.CharacterEllipsis
            });

            border.Child = panel;
            Grid.SetColumn(border, 0);
            Grid.SetRow(border, row);
            AttendanceGrid.Children.Add(border);
        }

        private void AddSessionHeaderCell(int column, AttendanceSession session)
        {
            var background = _selectedSessionId == session.Id ? _selectedHeaderBrush : _subHeaderBrush;
            var border = CreateBorder(background);
            border.Cursor = Cursors.Hand;
            border.ToolTip = "Выбрать столбец";
            border.MouseLeftButtonUp += (_, _) =>
            {
                _selectedSessionId = session.Id;
                deleteColumn_btn.IsEnabled = true;
                LoadJournal();
            };

            border.Child = new TextBlock
            {
                Text = session.SessionDate.Day.ToString(),
                FontSize = 22,
                TextAlignment = TextAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center
            };

            Grid.SetColumn(border, column);
            Grid.SetRow(border, 1);
            AttendanceGrid.Children.Add(border);
        }

        private void AddEditableMarkCell(int column, int row, AttendanceSession session, Client client, List<AttendanceMark> marks)
        {
            var border = CreateBorder(GetCellBackground(marks));
            border.Cursor = Cursors.Hand;
            border.ToolTip = "Редактировать отметку";
            border.MouseLeftButtonUp += (_, _) => EditCell(session, client, marks);
            border.Child = new TextBlock
            {
                Text = GetCellText(marks),
                FontSize = 20,
                TextAlignment = TextAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center,
                TextWrapping = TextWrapping.Wrap
            };

            Grid.SetColumn(border, column);
            Grid.SetRow(border, row);
            AttendanceGrid.Children.Add(border);
        }

        private void AddCell(int column, int row, string text, Brush background, double fontSize, FontWeight fontWeight, int columnSpan = 1)
        {
            var border = CreateBorder(background);
            border.Child = new TextBlock
            {
                Text = text,
                FontSize = fontSize,
                FontWeight = fontWeight,
                TextAlignment = TextAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center,
                TextWrapping = TextWrapping.Wrap
            };

            Grid.SetColumn(border, column);
            Grid.SetRow(border, row);
            Grid.SetColumnSpan(border, columnSpan);
            AttendanceGrid.Children.Add(border);
        }

        private static Border CreateBorder(Brush background)
        {
            return new Border
            {
                BorderBrush = Brushes.LightGray,
                BorderThickness = new Thickness(0.5),
                Background = background,
                Padding = new Thickness(4)
            };
        }

        private static string GetCellText(List<AttendanceMark> marks)
        {
            if (marks.Any(m => m.IsMedicalExcused)) return "нб";
            if (marks.Any(m => m.IsAbsent)) return "н";

            var numbers = marks
                .Where(m => m.LessonNumber.HasValue)
                .OrderBy(m => m.Id)
                .Select(m => m.LessonNumber!.Value.ToString())
                .ToList();

            return string.Join(",", numbers);
        }

        private Brush GetCellBackground(List<AttendanceMark> marks)
        {
            if (marks.Any(m => m.IsMedicalExcused)) return _medicalBrush;

            var subscriptionMarks = marks.Where(m => m.Subscription != null).ToList();
            if (subscriptionMarks.Any(m => m.Subscription!.IsPaid == false)) return _unpaidBrush;
            if (subscriptionMarks.Any(m => m.LessonNumber == m.Subscription!.TotalLessons)) return _lastLessonBrush;
            return _whiteBrush;
        }

        private static int CountPresentChildren(IEnumerable<AttendanceMark> marks)
        {
            return marks
                .Where(m => !m.IsAbsent)
                .Select(m => m.ClientId)
                .Distinct()
                .Count();
        }

        private void UpdateSessionChildrenCount(int sessionId)
        {
            var session = _context.attendanceSessions.FirstOrDefault(s => s.Id == sessionId);
            if (session == null) return;

            session.ChildrenCount = _context.attendanceMarks
                .Where(m => m.SessionId == sessionId && !m.IsAbsent)
                .Select(m => m.ClientId)
                .Distinct()
                .Count();
        }

        private void SelectClientDebt(int clientId)
        {
            var unpaid = ((IEnumerable<SubscriptionListItem>?)cbUnpaidSubscriptions.ItemsSource)?
                .FirstOrDefault(item =>
                    _context.attendanceSubscriptions.Any(s => s.Id == item.Id && s.ClientId == clientId && !s.IsPaid));

            if (unpaid != null)
            {
                cbUnpaidSubscriptions.SelectedValue = unpaid.Id;
            }
        }

        private void MarkAttendance(MarkAttendanceWindow form)
        {
            var clients = _context.clients
                .Where(c => c.StyleId == form.SelectedStyleId)
                .OrderBy(c => c.ChildSurname ?? "")
                .ThenBy(c => c.ChildName ?? "")
                .ToList();

            var session = new AttendanceSession
            {
                StyleId = form.SelectedStyleId,
                SessionDate = form.SessionDate,
                ChildrenCount = form.ChildrenCount,
                SubstituteTeacherId = form.SubstituteTeacherId,
                SubstituteTeacherName = form.SubstituteTeacherName,
                CreatedAt = DateTime.UtcNow
            };

            _context.attendanceSessions.Add(session);
            _context.SaveChanges();

            foreach (var client in clients)
            {
                var isAbsent = form.AbsentClientIds.Contains(client.Id);
                AddMarks(session.Id, client.Id, isAbsent ? "absent" : "present", form.WriteOffCount);
            }

            _context.SaveChanges();
            UpdateSessionChildrenCount(session.Id);
            _context.SaveChanges();
            RecalculateSubscriptions(form.SelectedStyleId, form.NewSubscriptionLessons);
        }

        private void AddMarks(int sessionId, int clientId, string markType, int writeOffCount)
        {
            if (markType == "clear") return;

            var count = markType == "medical" ? 1 : Math.Clamp(writeOffCount, 1, 2);
            for (var i = 0; i < count; i++)
            {
                _context.attendanceMarks.Add(new AttendanceMark
                {
                    SessionId = sessionId,
                    ClientId = clientId,
                    IsAbsent = markType is "absent" or "medical",
                    IsMedicalExcused = markType == "medical"
                });
            }
        }

        private void EditCell(AttendanceSession session, Client client, List<AttendanceMark> marks)
        {
            var title = $"{GetClientName(client)} - {session.SessionDate:dd.MM.yyyy}";
            var form = new EditAttendanceMarkWindow(title, GetCurrentMarkType(marks), Math.Max(1, Math.Min(2, marks.Count)))
            {
                Owner = this
            };
            form.ShowDialog();

            if (!form.IsSaved) return;

            try
            {
                var actualMarks = _context.attendanceMarks
                    .Where(m => m.SessionId == session.Id && m.ClientId == client.Id)
                    .ToList();

                _context.attendanceMarks.RemoveRange(actualMarks);
                AddMarks(session.Id, client.Id, form.MarkType, form.WriteOffCount);
                _context.SaveChanges();
                UpdateSessionChildrenCount(session.Id);
                RecalculateSubscriptions(session.StyleId);
                LoadPaymentSelectors();
                LoadJournal();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка редактирования отметки:\n{ex.Message}\n\n{ex.InnerException?.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private static string GetCurrentMarkType(List<AttendanceMark> marks)
        {
            if (marks.Count == 0) return "clear";
            if (marks.Any(m => m.IsMedicalExcused)) return "medical";
            if (marks.Any(m => m.IsAbsent)) return "absent";
            return "present";
        }

        private void RecalculateSubscriptions(int styleId, int defaultSubscriptionLessons = 12)
        {
            var clientIds = _context.clients
                .Where(c => c.StyleId == styleId)
                .Select(c => c.Id)
                .ToList();

            foreach (var clientId in clientIds)
            {
                RecalculateClientSubscriptions(clientId, styleId, defaultSubscriptionLessons);
            }

            _context.SaveChanges();
        }

        private void RecalculateClientSubscriptions(int clientId, int styleId, int defaultSubscriptionLessons)
        {
            var subscriptions = _context.attendanceSubscriptions
                .Where(s => s.ClientId == clientId && s.StyleId == styleId)
                .OrderBy(s => s.StartDate)
                .ThenBy(s => s.CreatedAt)
                .ThenBy(s => s.Id)
                .ToList();

            foreach (var subscription in subscriptions)
            {
                subscription.UsedLessons = 0;
                subscription.FinishedAt = null;
            }

            var marks = _context.attendanceMarks
                .Include(m => m.Session)
                .Where(m => m.ClientId == clientId && m.Session != null && m.Session.StyleId == styleId)
                .OrderBy(m => m.Session!.SessionDate)
                .ThenBy(m => m.SessionId)
                .ThenBy(m => m.Id)
                .ToList();

            foreach (var mark in marks)
            {
                mark.SubscriptionId = null;
                mark.LessonNumber = null;
            }

            var subscriptionIndex = 0;
            foreach (var mark in marks.Where(m => !m.IsMedicalExcused))
            {
                var subscription = GetSubscriptionForMark(subscriptions, clientId, styleId, defaultSubscriptionLessons, ref subscriptionIndex);
                subscription.UsedLessons += 1;

                if (subscription.UsedLessons == 1 && mark.Session != null)
                {
                    subscription.StartDate = mark.Session.SessionDate;
                }

                if (subscription.UsedLessons >= subscription.TotalLessons && mark.Session != null)
                {
                    subscription.FinishedAt = mark.Session.SessionDate;
                }

                mark.SubscriptionId = subscription.Id;
                mark.LessonNumber = subscription.UsedLessons;
            }
        }

        private AttendanceSubscription GetSubscriptionForMark(
            List<AttendanceSubscription> subscriptions,
            int clientId,
            int styleId,
            int defaultSubscriptionLessons,
            ref int subscriptionIndex)
        {
            while (subscriptionIndex < subscriptions.Count && subscriptions[subscriptionIndex].UsedLessons >= subscriptions[subscriptionIndex].TotalLessons)
            {
                subscriptionIndex++;
            }

            if (subscriptionIndex < subscriptions.Count)
            {
                return subscriptions[subscriptionIndex];
            }

            var subscription = new AttendanceSubscription
            {
                ClientId = clientId,
                StyleId = styleId,
                TotalLessons = defaultSubscriptionLessons,
                UsedLessons = 0,
                StartDate = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow,
                IsPaid = false
            };

            _context.attendanceSubscriptions.Add(subscription);
            _context.SaveChanges();
            subscriptions.Add(subscription);
            return subscription;
        }

        private void ApplyMedicalCertificate(MedicalCertificateWindow form)
        {
            var sessions = _context.attendanceSessions
                .Where(s => s.StyleId == Convert.ToInt32(cbStyle.SelectedValue)
                    && s.SessionDate >= form.DateFrom
                    && s.SessionDate <= form.DateTo)
                .OrderBy(s => s.SessionDate)
                .ThenBy(s => s.Id)
                .ToList();

            foreach (var session in sessions)
            {
                var absentMarks = _context.attendanceMarks
                    .Where(m => m.SessionId == session.Id
                        && m.ClientId == form.ClientId
                        && m.IsAbsent)
                    .ToList();

                foreach (var mark in absentMarks)
                {
                    mark.IsMedicalExcused = true;
                    mark.SubscriptionId = null;
                    mark.LessonNumber = null;
                }
            }

            _context.SaveChanges();
            RecalculateSubscriptions(Convert.ToInt32(cbStyle.SelectedValue));
        }

        private void markChildren_btn_Click(object sender, RoutedEventArgs e)
        {
            var styleId = cbStyle.SelectedValue == null ? null : (int?)Convert.ToInt32(cbStyle.SelectedValue);
            var form = new MarkAttendanceWindow(_context, styleId)
            {
                Owner = this
            };
            form.ShowDialog();

            if (!form.IsSaved) return;

            try
            {
                MarkAttendance(form);
                cbStyle.SelectedValue = form.SelectedStyleId;
                dpMonth.SelectedDate = form.SessionDate;
                LoadPaymentSelectors();
                LoadJournal();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка отметки посещения:\n{ex.Message}\n\n{ex.InnerException?.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void certificate_btn_Click(object sender, RoutedEventArgs e)
        {
            if (cbStyle.SelectedValue == null)
            {
                MessageBox.Show("Сначала выберите направление.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var form = new MedicalCertificateWindow(_context, Convert.ToInt32(cbStyle.SelectedValue))
            {
                Owner = this
            };
            form.ShowDialog();

            if (!form.IsSaved) return;

            try
            {
                ApplyMedicalCertificate(form);
                LoadPaymentSelectors();
                LoadJournal();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка отметки справки:\n{ex.Message}\n\n{ex.InnerException?.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void deleteColumn_btn_Click(object sender, RoutedEventArgs e)
        {
            if (!_selectedSessionId.HasValue) return;

            var session = _context.attendanceSessions
                .Include(s => s.Marks)
                .FirstOrDefault(s => s.Id == _selectedSessionId.Value);

            if (session == null)
            {
                _selectedSessionId = null;
                LoadJournal();
                return;
            }

            var answer = MessageBox.Show(
                $"Удалить столбец занятия {session.SessionDate:dd.MM.yyyy}? Все отметки в этом столбце будут удалены.",
                "Подтверждение удаления",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (answer != MessageBoxResult.Yes) return;

            var styleId = session.StyleId;
            _context.attendanceMarks.RemoveRange(session.Marks);
            _context.attendanceSessions.Remove(session);
            _context.SaveChanges();

            _selectedSessionId = null;
            RecalculateSubscriptions(styleId);
            LoadPaymentSelectors();
            LoadJournal();
        }

        private void markPaid_btn_Click(object sender, RoutedEventArgs e)
        {
            if (cbUnpaidSubscriptions.SelectedValue == null || cbPayments.SelectedValue == null)
            {
                MessageBox.Show("Выберите абонемент и оплату.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var subscriptionId = Convert.ToInt32(cbUnpaidSubscriptions.SelectedValue);
            var paymentId = Convert.ToInt32(cbPayments.SelectedValue);
            var subscription = _context.attendanceSubscriptions.FirstOrDefault(s => s.Id == subscriptionId);
            if (subscription == null) return;

            subscription.IsPaid = true;
            subscription.PaymentId = paymentId;
            _context.SaveChanges();

            LoadPaymentSelectors();
            LoadJournal();
        }

        private void cbStyle_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            _selectedSessionId = null;
            LoadJournal();
        }

        private void dpMonth_SelectedDateChanged(object sender, SelectionChangedEventArgs e)
        {
            _selectedSessionId = null;
            LoadJournal();
        }

        private void AttendanceScrollViewer_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (Math.Abs(e.NewSize.Width - _lastJournalViewportWidth) < 1) return;

            _lastJournalViewportWidth = e.NewSize.Width;
            LoadJournal();
        }

        private void Logo_Click(object sender, RoutedEventArgs e)
        {
            var mainWindow = new MainWindow();
            mainWindow.Show();
            Close();
        }

        private void menu_btn_Click(object sender, RoutedEventArgs e) => menuPopup.IsOpen = true;

        private void clients_btn_Click(object sender, RoutedEventArgs e)
        {
            menuPopup.IsOpen = false;
            var window = new ClientsWindow();
            window.Show();
            Close();
        }

        private void payment_btn_Click(object sender, RoutedEventArgs e)
        {
            menuPopup.IsOpen = false;
            var window = new PaymentsWindow();
            window.Show();
            Close();
        }

        private void trials_btn_Click(object sender, RoutedEventArgs e)
        {
            menuPopup.IsOpen = false;
            var window = new TrialsWindow();
            window.Show();
            Close();
        }

        private static string GetClientName(Client? client)
        {
            if (client == null) return "";
            return string.Join(" ", new[] { client.ChildSurname, client.ChildName, client.ChildPatronymic }
                .Where(s => !string.IsNullOrWhiteSpace(s)));
        }

        private record SubscriptionListItem(int Id, string DisplayName);
        private record PaymentListItem(int PaymentId, string DisplayName);
    }
}
