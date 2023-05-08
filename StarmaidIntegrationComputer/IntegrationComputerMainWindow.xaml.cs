using System;
using System.Threading;
using System.Windows;

using Microsoft.Extensions.Logging;

using Serilog;

using StarmaidIntegrationComputer.StarmaidSettings;

using TwitchLib.Client;
using TwitchLib.PubSub;


namespace StarmaidIntegrationComputer
{
    /// <summary>
    /// Interaction logic for IntegrationComputerMainWindow.xaml
    /// </summary>
    public partial class IntegrationComputerMainWindow : Window
    {
        private Settings settings;
        private readonly IntegrationComputerCore core;
        private readonly ILoggerFactory loggerFactory;


        LoggerConfiguration defaultLoggerConfiguration;

        #region Not API related
        ThalassaWindow thalassaForm;

        #endregion Not API related

        public IntegrationComputerMainWindow(ILoggerFactory loggerFactory, Settings settings, IntegrationComputerCore core, ThalassaWindow thalassaForm)
        {
            this.loggerFactory = loggerFactory;
            this.settings = settings;
            this.core = core;
            this.thalassaForm = thalassaForm;

            this.core.Output = AppendOutput;
            this.core.UpdateIsRunningVisuals = SetToggleButtonContent;

            Loaded += IntegrationComputerMainWindow_Loaded;


            InitializeComponent();
            InitializeLogging();
            InitializeThalassaForm();
            SetToggleButtonContent();
        }


        private void InitializeLogging()
        {
            defaultLoggerConfiguration = new LoggerConfiguration();
            defaultLoggerConfiguration.WriteTo.RichTextBox(OutputRichTextBox);
            Serilog.Core.Logger? serilogLogger = defaultLoggerConfiguration.CreateLogger();
            defaultLoggerConfiguration.MinimumLevel.Verbose();


            loggerFactory.AddSerilog(serilogLogger, true);

            loggerFactory.CreateLogger<IntegrationComputerMainWindow>();
            loggerFactory.CreateLogger<TwitchPubSub>();
            loggerFactory.CreateLogger<TwitchClient>();
        }

        private void InitializeThalassaForm()
        {
            thalassaForm.DisplayInput = OutputRichTextBox.AppendText;
            thalassaForm.LoggerFactory = loggerFactory;
        }

        private async void IntegrationComputerMainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            await core.EnactIsRunning();
        }

        private void SetToggleButtonContent()
        {
            Action behaviorToExecute = () => ToggleRunningButton.Content = core.IsRunning ? "Stop Running" : "Start Running";

            if (ToggleRunningButton.Dispatcher.Thread == Thread.CurrentThread)
            {
                behaviorToExecute();
            }
            else
            {
                ToggleRunningButton.Dispatcher.Invoke(behaviorToExecute);
            }
        }

        private async void ToggleRunningButton_Click(object sender, RoutedEventArgs e)
        {
            await core.ToggleRunning();
        }

        private void AppendOutput(string newContent)
        {
            Action behaviorToExecute = () => OutputRichTextBox.AppendText($"\n{newContent}");

            if (OutputRichTextBox.Dispatcher.Thread == Thread.CurrentThread)
            {
                behaviorToExecute();
            }
            else
            {
                OutputRichTextBox.Dispatcher.Invoke(behaviorToExecute);
            }
        }

        private void Thalassa_Click(object sender, RoutedEventArgs e)
        {
            if (thalassaForm.IsVisible)
            {
                thalassaForm.Hide();
            }
            else
            {
                thalassaForm.Show();
            }
        }

        private void ClearOutputButton_Click(object sender, RoutedEventArgs e)
        {
            OutputRichTextBox.Document.Blocks.Clear();
        }
    }
}
