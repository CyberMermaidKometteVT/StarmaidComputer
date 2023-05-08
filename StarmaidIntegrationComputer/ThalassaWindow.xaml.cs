using System;
using System.Windows;

using Microsoft.Extensions.Logging;

using Thalassa;

namespace StarmaidIntegrationComputer
{
    /// <summary>
    /// Interaction logic for Thalassa.xaml
    /// </summary>
    public partial class ThalassaWindow : Window
    {
        public Action<string>? DisplayInput { get; set; }
        public ILogger<ThalassaWindow> Logger { get; internal set; }

        private ILoggerFactory loggerFactory;
        public ILoggerFactory LoggerFactory
        {
            get { return loggerFactory; }
            set
            {
                loggerFactory = value;
                Logger = loggerFactory.CreateLogger<ThalassaWindow>();
                core.LoggerFactory = this.LoggerFactory;
            }
        }

        private ThalassaCore core;
        public ThalassaWindow(ThalassaCore core)
        {
            this.core = core;
            InitializeComponent();

            core.DisplayInput = DisplayInput;
            core.LoggerFactory = this.LoggerFactory;

            UpdateStartListeningLabel();
        }

       

        private void btnStartListening_Click(object sender, RoutedEventArgs e)
        {
            if (core.Listening)
            {
                core.StopListening();
            }
            else
            {
                core.StartListening();
            }

            UpdateStartListeningLabel();
        }

        private void UpdateStartListeningLabel()
        {
            if (core.Listening)
            {
                btnStartListening.Content = "&Stop Listening";
            }
            else
            {
                btnStartListening.Content = "&Start Listening";
            }
        }
    }
}
