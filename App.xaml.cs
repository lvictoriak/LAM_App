using System.Windows;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using LAM_App.Data;
using System.IO;

namespace LAM_App
{
    public partial class App : Application
    {
        public static AppDbContext DbContext { get; private set; }

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            var loginWindow = new LoginWindow();
            loginWindow.ShowDialog();

            if (!loginWindow.IsSuccess)//если не получили верный пароль, выходим
            {
                Shutdown();
                return;
            }

            string userPassword = loginWindow.EnteredPassword;

            var builder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);

            IConfiguration config = builder.Build();

            string templateConnection = config.GetConnectionString("DefaultConnection");
            //заменяем плейсхолдер на введенный пароль
            string realConnection = templateConnection.Replace("PLACEHOLDER_PASSWORD", userPassword);

            var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
            optionsBuilder.UseNpgsql(realConnection);

            try
            {
                DbContext = new AppDbContext(optionsBuilder.Options);

                //Проверка связи
                DbContext.Database.CanConnect();
                DbContext.EnsureAttendanceSchema();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка подключения к БД: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                Shutdown();
                return;
            }

            var mainWindow = new MainWindow();
            ShutdownMode = ShutdownMode.OnLastWindowClose;
            mainWindow.Show();
        }
    }
}
