using System.Windows;

namespace LAM_App
{
    public partial class LoginWindow : Window
    {
        public string EnteredPassword { get; private set; }
        public bool IsSuccess { get; private set; }

        public LoginWindow()
        {
            InitializeComponent();
        }

        private void TxtPassword_TextChanged(object sender, RoutedEventArgs e)
        {
            btnLogin.IsEnabled = !string.IsNullOrEmpty(txtPassword.Password);
        }

        private void BtnLogin_Click(object sender, RoutedEventArgs e)
        {
            EnteredPassword = txtPassword.Password;
            IsSuccess = true;
            this.Close();
        }
    }
}