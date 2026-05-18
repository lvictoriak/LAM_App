using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using LAM_App.Models;

namespace LAM_App
{
    public partial class MainWindow : Window
    {
        private Note? _currentNote;

        public MainWindow()
        {
            InitializeComponent();
            dpNoteDate.SelectedDate = DateTime.Today;
            LoadNotes();
        }

        private void LoadNotes()
        {
            try
            {
                if (App.DbContext == null) return;

                dgNotes.ItemsSource = App.DbContext.notes
                    .OrderByDescending(n => n.NoteDate)
                    .ThenByDescending(n => n.Id)
                    .ToList();
            }
            catch
            {
                dgNotes.ItemsSource = new List<Note>();
            }
        }

        private void dgNotes_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (dgNotes.SelectedItem is not Note selectedNote) return;

            _currentNote = selectedNote;
            dpNoteDate.SelectedDate = selectedNote.NoteDate;
            SelectNoteStatus(selectedNote.Status);
            txtNoteComment.Text = selectedNote.Comment;
        }

        private void saveNote_btn_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (App.DbContext == null) return;

                var comment = txtNoteComment.Text.Trim();
                if (string.IsNullOrWhiteSpace(comment))
                {
                    MessageBox.Show("Введите текст заметки", "Заметки", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                _currentNote ??= new Note
                {
                    CreatedAt = DateTime.UtcNow
                };

                if (_currentNote.Id == 0)
                {
                    App.DbContext.notes.Add(_currentNote);
                }

                _currentNote.NoteDate = dpNoteDate.SelectedDate ?? DateTime.Today;
                _currentNote.Status = GetSelectedNoteStatus();
                _currentNote.Comment = comment;

                App.DbContext.SaveChanges();
                ClearNoteFields();
                LoadNotes();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Не удалось сохранить заметку: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void clearNote_btn_Click(object sender, RoutedEventArgs e)
        {
            ClearNoteFields();
        }

        private void deleteNote_btn_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (App.DbContext == null || _currentNote == null) return;

                App.DbContext.notes.Remove(_currentNote);
                App.DbContext.SaveChanges();
                ClearNoteFields();
                LoadNotes();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Не удалось удалить заметку: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ClearNoteFields()
        {
            _currentNote = null;
            dgNotes.SelectedItem = null;
            dpNoteDate.SelectedDate = DateTime.Today;
            cbNoteStatus.SelectedIndex = 0;
            txtNoteComment.Text = "";
        }

        private string GetSelectedNoteStatus()
        {
            return (cbNoteStatus.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Задача";
        }

        private void SelectNoteStatus(string status)
        {
            foreach (ComboBoxItem item in cbNoteStatus.Items)
            {
                if (string.Equals(item.Content?.ToString(), status, StringComparison.OrdinalIgnoreCase))
                {
                    cbNoteStatus.SelectedItem = item;
                    return;
                }
            }

            cbNoteStatus.SelectedIndex = 0;
        }

        private void menu_btn_Click(object sender, RoutedEventArgs e)
        {
            menuPopup.IsOpen = true;
        }

        private void payment_btn_Click(object sender, RoutedEventArgs e)
        {
            var paymentWindow = new PaymentsWindow();
            paymentWindow.Show();
            this.Close();
        }

        private void price_btn_Click(object sender, RoutedEventArgs e)
        {
            var priceWindow = new PriceWindow();
            priceWindow.Show();
            this.Close();
        }

        private void timetable_btn_Click(object sender, RoutedEventArgs e)
        {
            var timetableWindow = new TimetableWindow();
            timetableWindow.Show();
            this.Close();
        }

        private void trial_btn_Click(object sender, RoutedEventArgs e)
        {
            var trialWindow = new TrialsWindow();
            trialWindow.Show();
            this.Close();
        }

        private void attendance_btn_Click(object sender, RoutedEventArgs e)
        {
            var attendanceWindow = new AttendanceWindow();
            attendanceWindow.Show();
            this.Close();
        }

        private void clients_btn_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                menuPopup.IsOpen = false;
                var clientsWindow = new ClientsWindow();
                clientsWindow.Show();
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Не удалось открыть окно клиентов: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void teachers_btn_Click(object sender, RoutedEventArgs e)
        {
            menuPopup.IsOpen = false;
            var teachersWindow = new TeachersWindow();
            teachersWindow.Show();
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
