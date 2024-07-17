using System.Configuration;
using System.Data;
using System.Windows;

namespace GUI
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private MainViewModel _mainViewModel;

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            var mainWindow = new MainWindow();
            _mainViewModel = new MainViewModel();
            mainWindow.DataContext = _mainViewModel;

            mainWindow.Show();
        }

        protected override void OnExit(ExitEventArgs e)
        {
            _mainViewModel.Dispose();
            
            base.OnExit(e);
        }
    }
}
