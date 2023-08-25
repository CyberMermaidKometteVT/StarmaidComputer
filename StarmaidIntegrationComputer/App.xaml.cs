using System;
using System.Threading;
using System.Windows;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using StarmaidIntegrationComputer.Thalassa.SpeechSynthesis;

namespace StarmaidIntegrationComputer
{

    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private ServiceProvider serviceProvider;
        private readonly ILogger<App> logger;

        public App()
        {
            //TODO: Link to better builder inc. settings: https://thecodeblogger.com/2021/05/04/how-to-use-appsettings-json-config-file-with-net-console-applications/ 

            Startup startup = new Startup();

            serviceProvider = startup.ConfigureServices();
            logger = serviceProvider.GetService<ILogger<App>>();
        }
        private void OnStartup(object sender, StartupEventArgs a)
        {
            try
            {
                var mainWindow = serviceProvider.GetRequiredService<IntegrationComputerMainWindow>();
                mainWindow.Show();

                Application.Current.DispatcherUnhandledException += Current_DispatcherUnhandledException;
            }
            catch (Exception ex)
            {
                HandleTopLevelException(ex);
            }
        }

        private void Current_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            HandleTopLevelException(e.Exception);
        }

        private void HandleTopLevelException(Exception ex)
        {
            logger.LogCritical($"UNHANDLED EXCEPTION: {ex.Message}{Environment.NewLine}{ex.StackTrace}");

            Exception? lastException = ex.InnerException;
            while (lastException != null)
            {
                logger.LogCritical($"Inner exception: {ex.Message}{Environment.NewLine}{ex.StackTrace}");
                lastException = lastException.InnerException;
            }

            var speechComputer = serviceProvider.GetService<SpeechComputer>();
            speechComputer.SpeechCompletedHandlers.Add(() => throw ex);
            speechComputer.Speak($"The Starmaid Integration Computer, which runs Thalassa, just crashed. It's a {ex.GetType()}, see the log for more details.");
            while (speechComputer.IsSpeaking)
            {
                Thread.Sleep(1000);
            }

        }
    }
}
