using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace LAM_App
{
    public partial class EditAttendanceMarkWindow : Window
    {
        public string MarkType { get; private set; } = "present";
        public int WriteOffCount { get; private set; } = 1;
        public bool IsSaved { get; private set; }

        public EditAttendanceMarkWindow(string title, string currentType, int currentWriteOffCount)
        {
            InitializeComponent();
            txtTitle.Text = title;
            SelectComboByTag(cbMarkType, currentType);
            SelectComboByText(cbWriteOffCount, currentWriteOffCount.ToString());
        }

        private void save_btn_Click(object sender, RoutedEventArgs e)
        {
            MarkType = (cbMarkType.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "present";
            var text = (cbWriteOffCount.SelectedItem as ComboBoxItem)?.Content?.ToString();
            WriteOffCount = int.TryParse(text, out var count) ? count : 1;
            IsSaved = true;
            Close();
        }

        private void cancel_btn_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private static void SelectComboByTag(ComboBox comboBox, string tag)
        {
            comboBox.SelectedItem = comboBox.Items
                .OfType<ComboBoxItem>()
                .FirstOrDefault(item => item.Tag?.ToString() == tag);

            comboBox.SelectedIndex = comboBox.SelectedIndex < 0 ? 0 : comboBox.SelectedIndex;
        }

        private static void SelectComboByText(ComboBox comboBox, string text)
        {
            comboBox.SelectedItem = comboBox.Items
                .OfType<ComboBoxItem>()
                .FirstOrDefault(item => item.Content?.ToString() == text);

            comboBox.SelectedIndex = comboBox.SelectedIndex < 0 ? 0 : comboBox.SelectedIndex;
        }
    }
}
