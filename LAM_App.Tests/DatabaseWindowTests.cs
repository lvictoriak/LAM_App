using System.Windows.Media;
using System.Windows.Controls;
using LAM_App.Models;
using Microsoft.EntityFrameworkCore;

namespace LAM_App.Tests;

public class DatabaseWindowTests
{
    [Fact]
    public void Attendance_AddMarks_creates_present_absent_medical_and_clear_marks()
    {
        WpfTest.Run(() =>
        {
            using var db = new TestDb();
            var style = db.AddStyle();
            var client = db.AddClient(style.Id, "Иванов", "Иван");
            var session = AddSession(db, style.Id, new DateTime(2026, 5, 1));
            var window = new AttendanceWindow();

            ReflectionHelper.InvokePrivateInstance(window, "AddMarks", session.Id, client.Id, "present", 2);
            ReflectionHelper.InvokePrivateInstance(window, "AddMarks", session.Id, client.Id, "absent", 1);
            ReflectionHelper.InvokePrivateInstance(window, "AddMarks", session.Id, client.Id, "medical", 2);
            ReflectionHelper.InvokePrivateInstance(window, "AddMarks", session.Id, client.Id, "clear", 2);
            db.Context.SaveChanges();

            Assert.Equal(4, db.Context.attendanceMarks.Count());
            Assert.Equal(2, db.Context.attendanceMarks.Count(m => !m.IsAbsent && !m.IsMedicalExcused));
            Assert.Single(db.Context.attendanceMarks.Where(m => m.IsAbsent && !m.IsMedicalExcused));
            Assert.Single(db.Context.attendanceMarks.Where(m => m.IsMedicalExcused));
            window.Close();
        });
    }

    [Fact]
    public void Attendance_UpdateSessionChildrenCount_counts_unique_present_children_from_database()
    {
        WpfTest.Run(() =>
        {
            using var db = new TestDb();
            var style = db.AddStyle();
            var first = db.AddClient(style.Id, "Иванов", "Иван");
            var second = db.AddClient(style.Id, "Петров", "Петр");
            var session = AddSession(db, style.Id, new DateTime(2026, 5, 1));
            db.Context.attendanceMarks.AddRange(
                new AttendanceMark { SessionId = session.Id, ClientId = first.Id },
                new AttendanceMark { SessionId = session.Id, ClientId = first.Id },
                new AttendanceMark { SessionId = session.Id, ClientId = second.Id, IsAbsent = true });
            db.Context.SaveChanges();
            var window = new AttendanceWindow();

            ReflectionHelper.InvokePrivateInstance(window, "UpdateSessionChildrenCount", session.Id);
            db.Context.SaveChanges();

            Assert.Equal(1, db.Context.attendanceSessions.Single(s => s.Id == session.Id).ChildrenCount);
            window.Close();
        });
    }

    [Fact]
    public void Attendance_RecalculateSubscriptions_assigns_lesson_numbers_and_creates_new_subscription()
    {
        WpfTest.Run(() =>
        {
            using var db = new TestDb();
            var style = db.AddStyle();
            var client = db.AddClient(style.Id, "Иванов", "Иван");
            db.Context.attendanceSubscriptions.Add(new AttendanceSubscription
            {
                ClientId = client.Id,
                StyleId = style.Id,
                TotalLessons = 2,
                StartDate = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            });
            db.Context.SaveChanges();

            var first = AddSession(db, style.Id, new DateTime(2026, 5, 1));
            var second = AddSession(db, style.Id, new DateTime(2026, 5, 8));
            var third = AddSession(db, style.Id, new DateTime(2026, 5, 15));
            db.Context.attendanceMarks.AddRange(
                new AttendanceMark { SessionId = first.Id, ClientId = client.Id },
                new AttendanceMark { SessionId = second.Id, ClientId = client.Id },
                new AttendanceMark { SessionId = third.Id, ClientId = client.Id });
            db.Context.SaveChanges();
            var window = new AttendanceWindow();

            ReflectionHelper.InvokePrivateInstance(window, "RecalculateSubscriptions", style.Id, 2);

            var subscriptions = db.Context.attendanceSubscriptions.OrderBy(s => s.Id).ToList();
            var numbers = db.Context.attendanceMarks
                .Include(m => m.Session)
                .OrderBy(m => m.Session!.SessionDate)
                .Select(m => m.LessonNumber)
                .ToList();

            Assert.Equal(2, subscriptions.Count);
            Assert.Equal(2, subscriptions[0].UsedLessons);
            Assert.Equal(1, subscriptions[1].UsedLessons);
            Assert.Equal(new int?[] { 1, 2, 1 }, numbers);
            Assert.NotNull(subscriptions[0].FinishedAt);
            window.Close();
        });
    }

    [Fact]
    public void Attendance_GetSubscriptionForMark_reuses_available_subscription_or_creates_new_one()
    {
        WpfTest.Run(() =>
        {
            using var db = new TestDb();
            var style = db.AddStyle();
            var client = db.AddClient(style.Id, "Иванов", "Иван");
            var existing = new AttendanceSubscription
            {
                ClientId = client.Id,
                StyleId = style.Id,
                TotalLessons = 1,
                UsedLessons = 0
            };
            db.Context.attendanceSubscriptions.Add(existing);
            db.Context.SaveChanges();
            var subscriptions = db.Context.attendanceSubscriptions.ToList();
            var index = 0;
            var window = new AttendanceWindow();

            var reused = ReflectionHelper.InvokePrivateInstance<AttendanceSubscription>(
                window,
                "GetSubscriptionForMark",
                subscriptions,
                client.Id,
                style.Id,
                12,
                index);
            subscriptions[0].UsedLessons = 1;
            index = 0;
            var created = ReflectionHelper.InvokePrivateInstance<AttendanceSubscription>(
                window,
                "GetSubscriptionForMark",
                subscriptions,
                client.Id,
                style.Id,
                12,
                index);

            Assert.Equal(existing.Id, reused.Id);
            Assert.NotEqual(existing.Id, created.Id);
            Assert.Equal(12, created.TotalLessons);
            window.Close();
        });
    }

    [Fact]
    public void Attendance_GetCellBackground_returns_medical_unpaid_last_lesson_and_white()
    {
        WpfTest.Run(() =>
        {
            using var db = new TestDb();
            var window = new AttendanceWindow();

            var medical = ReflectionHelper.InvokePrivateInstance<Brush>(
                window,
                "GetCellBackground",
                new List<AttendanceMark> { new() { IsMedicalExcused = true } });
            var unpaid = ReflectionHelper.InvokePrivateInstance<Brush>(
                window,
                "GetCellBackground",
                new List<AttendanceMark> { new() { Subscription = new AttendanceSubscription { IsPaid = false } } });
            var last = ReflectionHelper.InvokePrivateInstance<Brush>(
                window,
                "GetCellBackground",
                new List<AttendanceMark> { new() { LessonNumber = 12, Subscription = new AttendanceSubscription { IsPaid = true, TotalLessons = 12 } } });
            var regular = ReflectionHelper.InvokePrivateInstance<Brush>(
                window,
                "GetCellBackground",
                new List<AttendanceMark>());

            Assert.NotSame(medical, unpaid);
            Assert.Same(Brushes.Red, unpaid);
            Assert.Same(Brushes.Orange, last);
            Assert.Same(Brushes.White, regular);
            window.Close();
        });
    }

    [Fact]
    public void Attendance_SelectClientDebt_selects_unpaid_subscription_for_client()
    {
        WpfTest.Run(() =>
        {
            using var db = new TestDb();
            var style = db.AddStyle();
            var client = db.AddClient(style.Id, "Иванов", "Иван");
            var subscription = new AttendanceSubscription
            {
                ClientId = client.Id,
                StyleId = style.Id,
                TotalLessons = 12,
                UsedLessons = 1,
                IsPaid = false
            };
            db.Context.attendanceSubscriptions.Add(subscription);
            db.Context.SaveChanges();
            var window = new AttendanceWindow();

            ReflectionHelper.InvokePrivateInstance(window, "LoadPaymentSelectors");
            ReflectionHelper.InvokePrivateInstance(window, "SelectClientDebt", client.Id);

            Assert.Equal(subscription.Id, Named<ComboBox>(window, "cbUnpaidSubscriptions").SelectedValue);
            window.Close();
        });
    }

    [Fact]
    public void Payments_LoadDebtors_returns_only_unpaid_used_subscriptions()
    {
        WpfTest.Run(() =>
        {
            using var db = new TestDb();
            var style = db.AddStyle();
            var anna = db.AddClient(style.Id, "Абрамова", "Анна");
            var ivan = db.AddClient(style.Id, "Иванов", "Иван");
            db.Context.attendanceSubscriptions.AddRange(
                new AttendanceSubscription { ClientId = ivan.Id, StyleId = style.Id, TotalLessons = 12, UsedLessons = 3, IsPaid = false },
                new AttendanceSubscription { ClientId = anna.Id, StyleId = style.Id, TotalLessons = 8, UsedLessons = 1, IsPaid = false },
                new AttendanceSubscription { ClientId = anna.Id, StyleId = style.Id, TotalLessons = 8, UsedLessons = 0, IsPaid = false },
                new AttendanceSubscription { ClientId = ivan.Id, StyleId = style.Id, TotalLessons = 12, UsedLessons = 5, IsPaid = true });
            db.Context.SaveChanges();
            var window = new PaymentsWindow();

            ReflectionHelper.InvokePrivateInstance(window, "LoadDebtors");

            var debtors = Named<DataGrid>(window, "dgDebtors").ItemsSource!.Cast<object>().ToList();
            Assert.Equal(2, debtors.Count);
            Assert.Equal("Абрамова Анна", GetProperty<string>(debtors[0], "ChildName"));
            Assert.Equal("Иванов Иван", GetProperty<string>(debtors[1], "ChildName"));
            window.Close();
        });
    }

    [Fact]
    public void Payments_LoadData_orders_payments_and_loads_selectors()
    {
        WpfTest.Run(() =>
        {
            using var db = new TestDb();
            var style = db.AddStyle();
            db.Context.paymentTypes.Add(new PaymentType { Name = "Наличные" });
            db.Context.incomes.Add(new IncomeCategory { Name = "Абонемент" });
            db.Context.paymentLogs.AddRange(
                new PaymentLog { PaymentDate = new DateTime(2026, 5, 1), Income = 100, StyleId = style.Id },
                new PaymentLog { PaymentDate = new DateTime(2026, 5, 2), Expense = 50, StyleId = style.Id });
            db.Context.SaveChanges();
            var window = new PaymentsWindow();

            ReflectionHelper.InvokePrivateInstance(window, "LoadData");

            var payments = Named<DataGrid>(window, "dgPayments").ItemsSource!.Cast<object>().ToList();
            Assert.Equal(2, payments.Count);
            Assert.Equal(new DateTime(2026, 5, 2), GetProperty<DateTime>(payments[0], "PaymentDate"));
            Assert.Single(Named<ComboBox>(window, "cbPaymentType").Items);
            Assert.Single(Named<ComboBox>(window, "cbCategory").Items);
            Assert.Single(Named<ComboBox>(window, "cbStyle").Items);
            window.Close();
        });
    }

    [Fact]
    public void PaymentReport_GenerateReport_calculates_income_expense_and_net_profit()
    {
        WpfTest.Run(() =>
        {
            using var db = new TestDb();
            db.Context.paymentLogs.AddRange(
                new PaymentLog { PaymentDate = new DateTime(2026, 5, 1), Income = 100, Expense = 20 },
                new PaymentLog { PaymentDate = new DateTime(2026, 5, 2), Income = 50 },
                new PaymentLog { PaymentDate = new DateTime(2026, 6, 1), Income = 999 });
            db.Context.SaveChanges();
            var window = new PaymentReportWindow();
            Named<DatePicker>(window, "dpFromDate").SelectedDate = new DateTime(2026, 5, 1);
            Named<DatePicker>(window, "dpToDate").SelectedDate = new DateTime(2026, 5, 31);

            ReflectionHelper.InvokePrivateInstance(window, "BtnGenerateReport_Click", window, new System.Windows.RoutedEventArgs());

            Assert.Contains("150", Named<TextBlock>(window, "txtTotalIncome").Text);
            Assert.Contains("20", Named<TextBlock>(window, "txtTotalExpense").Text);
            Assert.Contains("130", Named<TextBlock>(window, "txtNetProfit").Text);
            Assert.Same(Brushes.Green, Named<TextBlock>(window, "txtNetProfit").Foreground);
            window.Close();
        });
    }

    [Fact]
    public void PaymentReport_GenerateReport_marks_negative_profit_red()
    {
        WpfTest.Run(() =>
        {
            using var db = new TestDb();
            db.Context.paymentLogs.Add(new PaymentLog { PaymentDate = new DateTime(2026, 5, 1), Income = 10, Expense = 30 });
            db.Context.SaveChanges();
            var window = new PaymentReportWindow();
            Named<DatePicker>(window, "dpFromDate").SelectedDate = new DateTime(2026, 5, 1);
            Named<DatePicker>(window, "dpToDate").SelectedDate = new DateTime(2026, 5, 1);

            ReflectionHelper.InvokePrivateInstance(window, "BtnGenerateReport_Click", window, new System.Windows.RoutedEventArgs());

            Assert.Contains("-20", Named<TextBlock>(window, "txtNetProfit").Text);
            Assert.Same(Brushes.Red, Named<TextBlock>(window, "txtNetProfit").Foreground);
            window.Close();
        });
    }

    [Fact]
    public void TrialsReport_GenerateReport_counts_records_by_status_and_period()
    {
        WpfTest.Run(() =>
        {
            using var db = new TestDb();
            var style = db.AddStyle();
            var scheduled = AddStatus(db, "scheduled");
            var attended = AddStatus(db, "attended");
            var bought = AddStatus(db, "bought");
            var other = AddStatus(db, "other");
            var unknown = AddStatus(db, "unknown");
            db.Context.trials.AddRange(
                new TrialRecord { StatusId = scheduled.Id, StyleId = style.Id, TrialDate = new DateTime(2026, 5, 1, 0, 0, 0, DateTimeKind.Utc) },
                new TrialRecord { StatusId = attended.Id, StyleId = style.Id, TrialDate = new DateTime(2026, 5, 2, 0, 0, 0, DateTimeKind.Utc) },
                new TrialRecord { StatusId = bought.Id, StyleId = style.Id, TrialDate = new DateTime(2026, 5, 3, 0, 0, 0, DateTimeKind.Utc) },
                new TrialRecord { StatusId = other.Id, StyleId = style.Id, TrialDate = new DateTime(2026, 5, 4, 0, 0, 0, DateTimeKind.Utc) },
                new TrialRecord { StatusId = unknown.Id, StyleId = style.Id, TrialDate = new DateTime(2026, 5, 5, 0, 0, 0, DateTimeKind.Utc) },
                new TrialRecord { StatusId = scheduled.Id, StyleId = style.Id, TrialDate = new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc) },
                new TrialRecord { StatusId = attended.Id, StyleId = style.Id });
            db.Context.SaveChanges();
            var window = new TrialsReportWindow();
            Named<DatePicker>(window, "dpFrom").SelectedDate = new DateTime(2026, 5, 1);
            Named<DatePicker>(window, "dpTo").SelectedDate = new DateTime(2026, 5, 31);

            ReflectionHelper.InvokePrivateInstance(window, "generate_btn_Click", window, new System.Windows.RoutedEventArgs());

            Assert.Contains("5", Named<TextBlock>(window, "txtTotal").Text);
            Assert.Contains("1", Named<TextBlock>(window, "txtScheduled").Text);
            Assert.Contains("1", Named<TextBlock>(window, "txtAttended").Text);
            Assert.Contains("1", Named<TextBlock>(window, "txtBought").Text);
            Assert.Contains("2", Named<TextBlock>(window, "txtOther").Text);
            window.Close();
        });
    }

    [Fact]
    public void TrialsReport_GenerateReport_returns_when_dates_are_missing()
    {
        WpfTest.Run(() =>
        {
            using var db = new TestDb();
            var window = new TrialsReportWindow();
            Named<DatePicker>(window, "dpFrom").SelectedDate = null;

            ReflectionHelper.InvokePrivateInstance(window, "generate_btn_Click", window, new System.Windows.RoutedEventArgs());

            Assert.Contains("0", Named<TextBlock>(window, "txtTotal").Text);
            window.Close();
        });
    }

    [Fact]
    public void Trials_LoadData_groups_records_by_status()
    {
        WpfTest.Run(() =>
        {
            using var db = new TestDb();
            var style = db.AddStyle();
            var scheduled = AddStatus(db, "На пробном");
            var attended = AddStatus(db, "Пришли");
            var bought = AddStatus(db, "Купили");
            db.Context.trials.AddRange(
                new TrialRecord { StatusId = scheduled.Id, StyleId = style.Id, ChildName = "А" },
                new TrialRecord { StatusId = attended.Id, StyleId = style.Id, ChildName = "Б" },
                new TrialRecord { StatusId = bought.Id, StyleId = style.Id, ChildName = "В" },
                new TrialRecord { Status = new TrialStatus { Name = "Неизвестно" }, StyleId = style.Id, ChildName = "Г" });
            db.Context.SaveChanges();
            var window = new TrialsWindow();

            ReflectionHelper.InvokePrivateInstance(window, "LoadData");

            Assert.Single(Named<ListBox>(window, "lbScheduled").Items);
            Assert.Single(Named<ListBox>(window, "lbAttended").Items);
            Assert.Single(Named<ListBox>(window, "lbBought").Items);
            Assert.Single(Named<ListBox>(window, "lbOther").Items);
            window.Close();
        });
    }

    [Fact]
    public void Clients_LoadData_groups_clients_by_style_and_loads_styles()
    {
        WpfTest.Run(() =>
        {
            using var db = new TestDb();
            var first = db.AddStyle("дети 3-4");
            var second = db.AddStyle("дети 5-6");
            db.AddClient(first.Id, "Иванов", "Иван");
            db.AddClient(second.Id, "Петров", "Петр");
            var window = new ClientsWindow();

            ReflectionHelper.InvokePrivateInstance(window, "LoadData");

            Assert.Equal(2, Named<ItemsControl>(window, "GroupsItemsControl").Items.Count);
            Assert.Equal(2, Named<ComboBox>(window, "cbGroup").Items.Count);
            window.Close();
        });
    }

    [Fact]
    public void Teachers_LoadData_shows_direction_text_for_teacher()
    {
        WpfTest.Run(() =>
        {
            using var db = new TestDb();
            var teacher = db.AddTeacher("Мария");
            db.AddStyle("джаз", teacher.Id);
            db.AddStyle("балет", teacher.Id);
            var window = new TeachersWindow();

            ReflectionHelper.InvokePrivateInstance(window, "LoadData");

            var rows = Named<DataGrid>(window, "dgTeachers").ItemsSource!.Cast<object>().ToList();
            Assert.Single(rows);
            var directions = GetProperty<string>(rows[0], "DirectionsText");
            Assert.Contains("джаз", directions);
            Assert.Contains("балет", directions);
            window.Close();
        });
    }

    [Fact]
    public void Styles_LoadData_shows_teacher_name_for_style()
    {
        WpfTest.Run(() =>
        {
            using var db = new TestDb();
            var teacher = db.AddTeacher("Мария");
            db.AddStyle("джаз", teacher.Id);
            var window = new StylesWindow();

            ReflectionHelper.InvokePrivateInstance(window, "LoadData");

            var rows = Named<DataGrid>(window, "dgStyles").ItemsSource!.Cast<object>().ToList();
            Assert.Single(rows);
            Assert.Equal("Мария", GetProperty<string>(rows[0], "TeacherName"));
            window.Close();
        });
    }

    [Fact]
    public void MedicalCertificate_constructor_loads_clients_for_style()
    {
        WpfTest.Run(() =>
        {
            using var db = new TestDb();
            var firstStyle = db.AddStyle("дети 3-4");
            var secondStyle = db.AddStyle("дети 5-6");
            db.AddClient(firstStyle.Id, "Иванов", "Иван");
            db.AddClient(secondStyle.Id, "Петров", "Петр");

            var window = new MedicalCertificateWindow(db.Context, firstStyle.Id);

            Assert.Single(Named<ComboBox>(window, "cbClient").Items);
            Assert.Equal(0, Named<ComboBox>(window, "cbClient").SelectedIndex);
            Assert.Equal(DateTime.Today, Named<DatePicker>(window, "dpFrom").SelectedDate);
            Assert.Equal(DateTime.Today, Named<DatePicker>(window, "dpTo").SelectedDate);
            window.Close();
        });
    }

    [Fact]
    public void MedicalCertificate_save_sets_client_dates_and_saved_flag()
    {
        WpfTest.Run(() =>
        {
            using var db = new TestDb();
            var style = db.AddStyle();
            var client = db.AddClient(style.Id, "Иванов", "Иван");
            var window = new MedicalCertificateWindow(db.Context, style.Id);
            Named<ComboBox>(window, "cbClient").SelectedValue = client.Id;
            Named<DatePicker>(window, "dpFrom").SelectedDate = new DateTime(2026, 5, 1);
            Named<DatePicker>(window, "dpTo").SelectedDate = new DateTime(2026, 5, 10);

            ReflectionHelper.InvokePrivateInstance(window, "save_btn_Click", window, new System.Windows.RoutedEventArgs());

            Assert.True(window.IsSaved);
            Assert.Equal(client.Id, window.ClientId);
            Assert.Equal(DateTimeKind.Utc, window.DateFrom.Kind);
            Assert.Equal(DateTimeKind.Utc, window.DateTo.Kind);
        });
    }

    [Fact]
    public void Payments_ClearFields_resets_form_state()
    {
        WpfTest.Run(() =>
        {
            using var db = new TestDb();
            var window = new PaymentsWindow();
            Named<DatePicker>(window, "dpPaymentDate").SelectedDate = new DateTime(2026, 5, 1);
            Named<TextBox>(window, "txtIncome").Text = "100";
            Named<TextBox>(window, "txtExpense").Text = "10";
            Named<TextBox>(window, "txtContractor").Text = "Клиент";
            Named<TextBox>(window, "txtComment").Text = "Комментарий";

            ReflectionHelper.InvokePrivateInstance(window, "ClearFields");

            Assert.Equal(DateTime.Today, Named<DatePicker>(window, "dpPaymentDate").SelectedDate);
            Assert.Equal("", Named<TextBox>(window, "txtIncome").Text);
            Assert.Equal("", Named<TextBox>(window, "txtExpense").Text);
            Assert.Equal("", Named<TextBox>(window, "txtContractor").Text);
            Assert.Equal("", Named<TextBox>(window, "txtComment").Text);
            window.Close();
        });
    }

    [Fact]
    public void Login_TextChanged_enables_button_and_login_sets_success()
    {
        WpfTest.Run(() =>
        {
            var window = new LoginWindow();
            var txtPassword = Named<PasswordBox>(window, "txtPassword");
            var btnLogin = Named<Button>(window, "btnLogin");
            txtPassword.Password = "secret";

            ReflectionHelper.InvokePrivateInstance(window, "TxtPassword_TextChanged", txtPassword, new System.Windows.RoutedEventArgs());
            ReflectionHelper.InvokePrivateInstance(window, "BtnLogin_Click", btnLogin, new System.Windows.RoutedEventArgs());

            Assert.True(btnLogin.IsEnabled);
            Assert.True(window.IsSuccess);
            Assert.Equal("secret", window.EnteredPassword);
        });
    }

    private static AttendanceSession AddSession(TestDb db, int styleId, DateTime date)
    {
        var session = new AttendanceSession
        {
            StyleId = styleId,
            SessionDate = DateTime.SpecifyKind(date, DateTimeKind.Utc),
            CreatedAt = DateTime.UtcNow
        };
        db.Context.attendanceSessions.Add(session);
        db.Context.SaveChanges();
        return session;
    }

    private static TrialStatus AddStatus(TestDb db, string name)
    {
        var status = new TrialStatus { Name = name };
        db.Context.trialStatuses.Add(status);
        db.Context.SaveChanges();
        return status;
    }

    private static T GetProperty<T>(object target, string propertyName)
    {
        return (T)target.GetType().GetProperty(propertyName)!.GetValue(target)!;
    }

    private static T Named<T>(object window, string name)
    {
        return ReflectionHelper.GetPrivateField<T>(window, name);
    }
}
