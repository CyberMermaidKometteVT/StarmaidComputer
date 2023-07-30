using System;
using System.Windows;

using Microsoft.Extensions.DependencyInjection;

namespace StarmaidIntegrationComputer
{

    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private ServiceProvider serviceProvider;

        public App()
        {
            //TODO: Link to better builder inc. settings: https://thecodeblogger.com/2021/05/04/how-to-use-appsettings-json-config-file-with-net-console-applications/ 

            Startup startup = new Startup();

            serviceProvider = startup.ConfigureServices();
        }
        private void OnStartup(object sender, StartupEventArgs a)
        {
            var mainWindow = serviceProvider.GetRequiredService<IntegrationComputerMainWindow>();
            mainWindow.Show();
        }
    }
}
