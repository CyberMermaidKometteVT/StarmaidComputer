using System;
using System.Windows;
using System.Windows.Media;

using Microsoft.Extensions.Logging;

using StarmaidIntegrationComputer.StarmaidSettings;
using StarmaidIntegrationComputer.Thalassa;

namespace StarmaidIntegrationComputer
{
    /// <summary>
    /// Interaction logic for Thalassa.xaml
    /// </summary>
    public partial class ThalassaWindow : Window
    {
        public Action<string>? DisplayInput { get; set; }
        public ILogger<ThalassaWindow> Logger { get; internal set; }

        MediaPlayer startingListening = new MediaPlayer();
        MediaPlayer stoppingListening = new MediaPlayer();


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

        private readonly ISoundPathSettings soundPathSettings;
        private readonly ThalassaCore core;
        public ThalassaWindow(ISoundPathSettings soundPathSettings, ThalassaCore core)
        {
            this.soundPathSettings = soundPathSettings;
            this.core = core;
            InitializeComponent();

            core.DisplayInput = DisplayInput;
            core.LoggerFactory = this.LoggerFactory;
            core.StartingListeningHandlers.Add(PlayStartingListening);
            core.StoppingListeningHandlers.Add(PlayStoppingListening);

            UpdateStartListeningLabel();
        }

        protected override void OnInitialized(EventArgs e)
        {
            startingListening.Open(new Uri(soundPathSettings.StartingListeningSoundPath, UriKind.RelativeOrAbsolute));
            stoppingListening.Open(new Uri(soundPathSettings.StoppingListeningSoundPath, UriKind.RelativeOrAbsolute));

            base.OnInitialized(e);
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

        private void PlayStartingListening()
        {

            Dispatcher.Invoke(startingListening.Play);
        }
        private void PlayStoppingListening()
        {
            Dispatcher.Invoke(stoppingListening.Play);
        }
    }
}
