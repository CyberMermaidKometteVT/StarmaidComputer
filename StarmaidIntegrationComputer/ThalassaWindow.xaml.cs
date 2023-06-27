using System;
using System.ComponentModel;
using System.Media;
using System.Windows;
using System.Windows.Media;

using Microsoft.Extensions.Logging;

using NAudio.Wave;

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

        AudioFileReader startingListeningReader;
        AudioFileReader stoppingListeningReader;

        WaveOutEvent startingListening = new WaveOutEvent();
        WaveOutEvent stoppingListening = new WaveOutEvent();

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
            startingListeningReader = new AudioFileReader(soundPathSettings.StartingListeningSoundPath);
            startingListening.Init(startingListeningReader);

            stoppingListeningReader = new AudioFileReader(soundPathSettings.StoppingListeningSoundPath);
            stoppingListening.Init(stoppingListeningReader);

            base.OnInitialized(e);
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            base.OnClosing(e);

            if (startingListeningReader != null)
            {
                startingListeningReader.Dispose();
            }

            if (stoppingListeningReader != null)
            {
                stoppingListeningReader.Dispose();
            }

            if (startingListening != null)
            {
                startingListening.Dispose();
            }

            if (stoppingListening != null)
            {
                stoppingListening.Dispose();
            }
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

            Dispatcher.Invoke(PlayStartingListeningFile);
        }
        private void PlayStoppingListening()
        {
            Dispatcher.Invoke(PlayStoppingListeningFile);
        }

        private void PlayStartingListeningFile()
        {

            startingListeningReader.Position = 0;
            startingListening.Play();
        }

        private void PlayStoppingListeningFile()
        {

            stoppingListeningReader.Position = 0;
            stoppingListening.Play();

        }
    }
}
