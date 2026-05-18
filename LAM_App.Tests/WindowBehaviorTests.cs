using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using LAM_App.Models;

namespace LAM_App.Tests;

public class WindowBehaviorTests
{
    [Fact]
    public void MainWindow_constructor_and_menu_handler_work_without_navigation()
    {
        WpfTest.Run(() =>
        {
            var window = new MainWindow();

            ReflectionHelper.InvokePrivateInstance(window, "menu_btn_Click", window, new System.Windows.RoutedEventArgs());

            Assert.NotNull(Named<System.Windows.Controls.Primitives.Popup>(window, "menuPopup"));
            window.Close();
        });
    }

    [Fact]
    public void ImageViewer_constructor_and_zoom_buttons_update_slider()
    {
        WpfTest.Run(() =>
        {
            var image = new WriteableBitmap(1, 1, 96, 96, PixelFormats.Bgra32, null);
            var window = new ImageViewerWindow(image);
            var slider = Named<Slider>(window, "zoomSlider");
            var viewerImage = Named<Image>(window, "viewerImage");

            Assert.Same(image, viewerImage.Source);
            Assert.Equal(1, slider.Value);

            ReflectionHelper.InvokePrivateInstance(window, "zoomIn_btn_Click", window, new System.Windows.RoutedEventArgs());
            Assert.Equal(1.1, slider.Value, 3);

            ReflectionHelper.InvokePrivateInstance(window, "zoomOut_btn_Click", window, new System.Windows.RoutedEventArgs());
            Assert.Equal(1, slider.Value, 3);

            slider.Value = 3;
            ReflectionHelper.InvokePrivateInstance(window, "resetZoom_btn_Click", window, new System.Windows.RoutedEventArgs());
            Assert.Equal(1, slider.Value);

            slider.Value = slider.Maximum;
            ReflectionHelper.InvokePrivateInstance(window, "zoomIn_btn_Click", window, new System.Windows.RoutedEventArgs());
            Assert.Equal(slider.Maximum, slider.Value);

            slider.Value = slider.Minimum;
            ReflectionHelper.InvokePrivateInstance(window, "zoomOut_btn_Click", window, new System.Windows.RoutedEventArgs());
            Assert.Equal(slider.Minimum, slider.Value);
            window.Close();
        });
    }

    [Fact]
    public void EditAttendance_constructor_and_save_set_selected_values()
    {
        WpfTest.Run(() =>
        {
            var window = new EditAttendanceMarkWindow("Ребенок", "absent", 2);

            Assert.Equal("Ребенок", Named<TextBlock>(window, "txtTitle").Text);
            Assert.Equal("absent", ((ComboBoxItem)Named<ComboBox>(window, "cbMarkType").SelectedItem).Tag);
            Assert.Equal("2", ((ComboBoxItem)Named<ComboBox>(window, "cbWriteOffCount").SelectedItem).Content);

            ReflectionHelper.InvokePrivateInstance(window, "save_btn_Click", window, new System.Windows.RoutedEventArgs());

            Assert.True(window.IsSaved);
            Assert.Equal("absent", window.MarkType);
            Assert.Equal(2, window.WriteOffCount);
        });
    }

    [Fact]
    public void MarkAttendance_constructor_loads_clients_and_save_collects_absent_ids()
    {
        WpfTest.Run(() =>
        {
            using var db = new TestDb();
            var style = db.AddStyle();
            var first = db.AddClient(style.Id, "Иванов", "Иван");
            db.AddClient(style.Id, "Петров", "Петр");
            var teacher = db.AddTeacher("Замена");
            var window = new MarkAttendanceWindow(db.Context, style.Id);
            var absentPanel = Named<StackPanel>(window, "AbsentListPanel");
            var firstAbsent = absentPanel.Children.OfType<CheckBox>().First();
            firstAbsent.IsChecked = true;
            Named<DatePicker>(window, "dpSessionDate").SelectedDate = new DateTime(2026, 5, 17);
            Named<TextBox>(window, "txtChildrenCount").Text = "2";
            Named<ComboBox>(window, "cbSubstituteTeacher").SelectedValue = teacher.Id;
            Named<ComboBox>(window, "cbSubstituteTeacher").Text = teacher.FullName;

            ReflectionHelper.InvokePrivateInstance(window, "save_btn_Click", window, new System.Windows.RoutedEventArgs());

            Assert.True(window.IsSaved);
            Assert.Equal(style.Id, window.SelectedStyleId);
            Assert.Equal(DateTimeKind.Utc, window.SessionDate.Kind);
            Assert.Equal(2, window.ChildrenCount);
            Assert.Equal(12, window.NewSubscriptionLessons);
            Assert.Equal(1, window.WriteOffCount);
            Assert.Equal(teacher.Id, window.SubstituteTeacherId);
            Assert.Equal(teacher.FullName, window.SubstituteTeacherName);
            Assert.Contains(first.Id, window.AbsentClientIds);
        });
    }

    [Fact]
    public void Trials_ListBoxSelectionChanged_and_ClearFields_update_form()
    {
        WpfTest.Run(() =>
        {
            using var db = new TestDb();
            var style = db.AddStyle();
            var status = new TrialStatus { Name = "Пришли" };
            db.Context.trialStatuses.Add(status);
            db.Context.SaveChanges();
            var record = new TrialRecord
            {
                StatusId = status.Id,
                Status = status,
                StyleId = style.Id,
                Style = style,
                ParentName = "Родитель",
                ParentPhone = "123",
                ChildName = "Ребенок",
                ChildAge = 7,
                TrialDate = new DateTime(2026, 5, 17),
                Comment = "Комментарий"
            };
            var window = new TrialsWindow();
            var listBox = Named<ListBox>(window, "lbAttended");
            listBox.ItemsSource = new[] { record };
            listBox.SelectedItem = record;

            ReflectionHelper.InvokePrivateInstance(window, "ListBox_SelectionChanged", listBox, new SelectionChangedEventArgs(ListBox.SelectionChangedEvent, new List<object>(), new List<object> { record }));

            Assert.Equal("Родитель", Named<TextBox>(window, "txtParentName").Text);
            Assert.Equal("123", Named<TextBox>(window, "txtPhone").Text);
            Assert.Equal("Ребенок", Named<TextBox>(window, "txtChildName").Text);
            Assert.Equal("7", Named<TextBox>(window, "txtAge").Text);
            Assert.Equal("Комментарий", Named<TextBox>(window, "txtComment").Text);

            ReflectionHelper.InvokePrivateInstance(window, "ClearFields");
            Assert.Equal("", Named<TextBox>(window, "txtParentName").Text);
            Assert.Equal("", Named<TextBox>(window, "txtPhone").Text);
            Assert.Equal("", Named<TextBox>(window, "txtChildName").Text);
            window.Close();
        });
    }

    [Fact]
    public void Clients_ClearFields_resets_form()
    {
        WpfTest.Run(() =>
        {
            using var db = new TestDb();
            var window = new ClientsWindow();
            Named<TextBox>(window, "txtChildSurname").Text = "Иванов";
            Named<TextBox>(window, "txtChildName").Text = "Иван";
            Named<TextBox>(window, "txtParentName").Text = "Родитель";
            Named<TextBox>(window, "txtPhone").Text = "123";
            Named<TextBox>(window, "txtAge").Text = "7";
            Named<DatePicker>(window, "dpBirthDate").SelectedDate = new DateTime(2020, 1, 1);

            ReflectionHelper.InvokePrivateInstance(window, "ClearFields");

            Assert.Equal("", Named<TextBox>(window, "txtChildSurname").Text);
            Assert.Equal("", Named<TextBox>(window, "txtChildName").Text);
            Assert.Equal("", Named<TextBox>(window, "txtParentName").Text);
            Assert.Equal("", Named<TextBox>(window, "txtPhone").Text);
            Assert.Null(Named<DatePicker>(window, "dpBirthDate").SelectedDate);
            window.Close();
        });
    }

    [Fact]
    public void Clients_SelectionChanged_Update_AddNew_and_Menu_update_form_state()
    {
        WpfTest.Run(() =>
        {
            using var db = new TestDb();
            var style = db.AddStyle("group");
            var client = db.AddClient(style.Id, "Ivanov", "Ivan");
            client.ChildPatronymic = "Ivanovich";
            client.ParentName = "Parent";
            client.ParentPhone = "123";
            client.Age = 7;
            client.BirthDate = new DateTime(2019, 1, 1);
            client.Shift = "first";
            client.Comment = "comment";
            db.Context.SaveChanges();
            var window = new ClientsWindow();
            var grid = new DataGrid { ItemsSource = new[] { client } };
            grid.SelectedItem = client;

            ReflectionHelper.InvokePrivateInstance(window, "DataGrid_SelectionChanged", grid, new SelectionChangedEventArgs(DataGrid.SelectionChangedEvent, new List<object>(), new List<object> { client }));

            Assert.Equal("Ivanov", Named<TextBox>(window, "txtChildSurname").Text);
            Assert.Equal("Ivan", Named<TextBox>(window, "txtChildName").Text);
            Assert.Equal("Ivanovich", Named<TextBox>(window, "txtChildPatronymic").Text);
            Assert.Equal("Parent", Named<TextBox>(window, "txtParentName").Text);
            Assert.Equal("123", Named<TextBox>(window, "txtPhone").Text);
            Assert.Equal("7", Named<TextBox>(window, "txtAge").Text);
            Assert.Equal(new DateTime(2019, 1, 1), Named<DatePicker>(window, "dpBirthDate").SelectedDate);
            Assert.Equal(style.Id, Named<ComboBox>(window, "cbGroup").SelectedValue);
            Assert.Equal("first", Named<TextBox>(window, "txtShift").Text);
            Assert.Equal("comment", Named<TextBox>(window, "txtComment").Text);

            ReflectionHelper.InvokePrivateInstance(window, "update_btn_Click", window, new System.Windows.RoutedEventArgs());
            Assert.Equal("", Named<TextBox>(window, "txtChildSurname").Text);

            Named<TextBox>(window, "txtChildSurname").Text = "dirty";
            ReflectionHelper.InvokePrivateInstance(window, "BtnAddNew_Click", window, new System.Windows.RoutedEventArgs());
            Assert.Equal("", Named<TextBox>(window, "txtChildSurname").Text);

            ReflectionHelper.InvokePrivateInstance(window, "menu_btn_Click", window, new System.Windows.RoutedEventArgs());
            Assert.NotNull(Named<System.Windows.Controls.Primitives.Popup>(window, "menuPopup"));
            window.Close();
        });
    }

    [Fact]
    public void Teachers_SaveDirections_assigns_existing_styles_and_reports_missing()
    {
        WpfTest.Run(() =>
        {
            using var db = new TestDb();
            var teacher = db.AddTeacher("Мария");
            var style = db.AddStyle("джаз");
            var window = new TeachersWindow();
            ReflectionHelper.SetPrivateField(window, "_currentTeacher", teacher);
            Named<TextBox>(window, "txtDirections").Text = "джаз; неизвестное";

            var missing = ReflectionHelper.InvokePrivateInstance<string>(window, "SaveDirections");

            Assert.Equal("неизвестное", missing);
            Assert.Equal(teacher.Id, db.Context.styles.Single(s => s.Id == style.Id).TeacherId);
            window.Close();
        });
    }

    [Fact]
    public void Teachers_ClearFields_resets_form()
    {
        WpfTest.Run(() =>
        {
            using var db = new TestDb();
            var window = new TeachersWindow();
            Named<TextBox>(window, "txtFullName").Text = "Мария";
            Named<TextBox>(window, "txtPhone").Text = "123";
            Named<TextBox>(window, "txtAge").Text = "20";
            Named<TextBox>(window, "txtDirections").Text = "джаз";

            ReflectionHelper.InvokePrivateInstance(window, "ClearFields");

            Assert.Equal("", Named<TextBox>(window, "txtFullName").Text);
            Assert.Equal("", Named<TextBox>(window, "txtPhone").Text);
            Assert.Equal("", Named<TextBox>(window, "txtAge").Text);
            Assert.Equal("", Named<TextBox>(window, "txtDirections").Text);
            window.Close();
        });
    }

    [Fact]
    public void Teachers_SelectionChanged_Update_AddNew_and_Menu_update_form_state()
    {
        WpfTest.Run(() =>
        {
            using var db = new TestDb();
            var teacher = db.AddTeacher("Maria");
            teacher.Phone = "123";
            teacher.Age = 25;
            teacher.DanceExperience = "10 years";
            teacher.Comment = "comment";
            db.AddStyle("jazz", teacher.Id);
            db.Context.SaveChanges();
            var window = new TeachersWindow();
            var grid = Named<DataGrid>(window, "dgTeachers");
            var row = grid.ItemsSource!.Cast<object>().Single(r => (int)r.GetType().GetProperty("Id")!.GetValue(r)! == teacher.Id);
            grid.SelectedItem = row;

            ReflectionHelper.InvokePrivateInstance(window, "dgTeachers_SelectionChanged", grid, new SelectionChangedEventArgs(DataGrid.SelectionChangedEvent, new List<object>(), new List<object> { row }));

            Assert.Equal("Maria", Named<TextBox>(window, "txtFullName").Text);
            Assert.Equal("123", Named<TextBox>(window, "txtPhone").Text);
            Assert.Equal("25", Named<TextBox>(window, "txtAge").Text);
            Assert.Equal("10 years", Named<TextBox>(window, "txtDanceExperience").Text);
            Assert.Equal("comment", Named<TextBox>(window, "txtComment").Text);
            Assert.Equal("jazz", Named<TextBox>(window, "txtDirections").Text);

            ReflectionHelper.InvokePrivateInstance(window, "update_btn_Click", window, new System.Windows.RoutedEventArgs());
            Assert.Equal("", Named<TextBox>(window, "txtFullName").Text);

            Named<TextBox>(window, "txtFullName").Text = "dirty";
            ReflectionHelper.InvokePrivateInstance(window, "BtnAddNew_Click", window, new System.Windows.RoutedEventArgs());
            Assert.Equal("", Named<TextBox>(window, "txtFullName").Text);

            ReflectionHelper.InvokePrivateInstance(window, "menu_btn_Click", window, new System.Windows.RoutedEventArgs());
            Assert.NotNull(Named<System.Windows.Controls.Primitives.Popup>(window, "menuPopup"));
            window.Close();
        });
    }

    [Fact]
    public void Styles_SelectionChanged_and_ClearFields_update_form()
    {
        WpfTest.Run(() =>
        {
            using var db = new TestDb();
            var teacher = db.AddTeacher("Мария");
            var style = db.AddStyle("джаз", teacher.Id);
            var window = new StylesWindow();
            var grid = Named<DataGrid>(window, "dgStyles");
            var row = grid.ItemsSource!.Cast<object>().Single(r => (int)r.GetType().GetProperty("Id")!.GetValue(r)! == style.Id);
            grid.SelectedItem = row;

            ReflectionHelper.InvokePrivateInstance(window, "dgStyles_SelectionChanged", grid, new SelectionChangedEventArgs(DataGrid.SelectionChangedEvent, new List<object>(), new List<object> { row }));

            Assert.Equal("джаз", Named<TextBox>(window, "txtName").Text);
            Assert.Equal("Мария", Named<TextBox>(window, "txtTeachers").Text);

            ReflectionHelper.InvokePrivateInstance(window, "ClearFields");
            Assert.Equal("", Named<TextBox>(window, "txtName").Text);
            Assert.Equal("", Named<TextBox>(window, "txtTeachers").Text);
            window.Close();
        });
    }

    [Fact]
    public void Styles_Update_AddNew_and_Menu_update_form_state()
    {
        WpfTest.Run(() =>
        {
            using var db = new TestDb();
            db.AddStyle("jazz");
            var window = new StylesWindow();

            Named<TextBox>(window, "txtName").Text = "dirty";
            ReflectionHelper.InvokePrivateInstance(window, "update_btn_Click", window, new System.Windows.RoutedEventArgs());
            Assert.Equal("", Named<TextBox>(window, "txtName").Text);

            Named<TextBox>(window, "txtName").Text = "dirty";
            ReflectionHelper.InvokePrivateInstance(window, "BtnAddNew_Click", window, new System.Windows.RoutedEventArgs());
            Assert.Equal("", Named<TextBox>(window, "txtName").Text);

            ReflectionHelper.InvokePrivateInstance(window, "menu_btn_Click", window, new System.Windows.RoutedEventArgs());
            Assert.NotNull(Named<System.Windows.Controls.Primitives.Popup>(window, "menuPopup"));
            window.Close();
        });
    }

    [Fact]
    public void PriceAndTimetable_constructors_initialize_without_saved_images()
    {
        WpfTest.Run(() =>
        {
            var timetable = new TimetableWindow();
            var price = new PriceWindow();
            var adultsTimetable = new AdultsTimetableWindow();
            var adultsPrice = new AdultsPriceWindow();

            Assert.NotNull(timetable);
            Assert.NotNull(price);
            Assert.NotNull(adultsTimetable);
            Assert.NotNull(adultsPrice);

            timetable.Close();
            price.Close();
            adultsTimetable.Close();
            adultsPrice.Close();
        });
    }

    private static T Named<T>(object window, string name)
    {
        return ReflectionHelper.GetPrivateField<T>(window, name);
    }
}
