using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Controls;

using Microsoft.Extensions.Logging;

using Serilog;
using Serilog.Events;

using StarmaidIntegrationComputer.Chat;
using StarmaidIntegrationComputer.Commands;
using StarmaidIntegrationComputer.Common.Settings;
using StarmaidIntegrationComputer.Logging;
using StarmaidIntegrationComputer.Thalassa;
using StarmaidIntegrationComputer.Thalassa.SpeechSynthesis;

namespace StarmaidIntegrationComputer
{
    /// <summary>
    /// Interaction logic for IntegrationComputerMainWindow.xaml
    /// </summary>
    public partial class IntegrationComputerMainWindow : Window
    {
        private readonly IntegrationComputerCore core;
        private readonly ILoggerFactory loggerFactory;
        private readonly ChatWindowFactory chatWindowFactory;
        private readonly ThalassaCore thalassaCore;
        private readonly StreamerProfileSettings streamerProfileSettings;
        private readonly SpeechComputer speechComputer;
        private readonly WindowLogSinkController windowLogSinkController;
        LoggerConfiguration loggerConfiguration;

        private bool isPopulatingWindowLogLevelComboBox;
        public List<ChatWindow> chatWindows { get; private set; } = new List<ChatWindow>();

        ScrollViewer outputScrollViewer;


        public IntegrationComputerMainWindow(ILoggerFactory loggerFactory, IntegrationComputerCore core, LoggerConfiguration loggerConfiguration, ChatWindowFactory chatWindowFactory, ThalassaCore thalassaCore, StreamerProfileSettings streamerProfileSettings, SpeechComputer speechComputer, WindowLogSinkController windowLogSinkController)
        {
            this.loggerFactory = loggerFactory;
            this.core = core;
            this.loggerConfiguration = loggerConfiguration;
            this.chatWindowFactory = chatWindowFactory;
            this.thalassaCore = thalassaCore;
            this.streamerProfileSettings = streamerProfileSettings;
            this.speechComputer = speechComputer;
            this.windowLogSinkController = windowLogSinkController;
            this.core.OutputToMainWindow = AppendOutput;
            this.core.UpdateIsRunningVisuals = SetToggleButtonContent;

            Loaded += IntegrationComputerMainWindow_Loaded;


            InitializeComponent();
            InitializeLogging();
            SetToggleButtonContent();

            this.thalassaCore.LoggerFactory = loggerFactory;


            //This should realy live somewhere else
            thalassaCore.SpeechInterpreted = OnSpeechInterpreted;
        }

        private void InitializeLogging()
        {
            windowLogSinkController.AttachTo(OutputRichTextBox);

            PopulateWindowLogLevelComboBox();
        }

        private void PopulateWindowLogLevelComboBox()
        {
            //Assigning ItemsSource and SelectedItem both raise SelectionChanged, which would push the
            //level straight back into the controller it was just read from - harmless, but it would also
            //fire before the window has finished initializing.
            isPopulatingWindowLogLevelComboBox = true;

            WindowLogLevelComboBox.ItemsSource = windowLogSinkController.SelectableLevels;
            WindowLogLevelComboBox.SelectedItem = windowLogSinkController.CurrentLevel;

            isPopulatingWindowLogLevelComboBox = false;
        }

        private void WindowLogLevelComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (isPopulatingWindowLogLevelComboBox)
            {
                return;
            }

            LogEventLevel selectedLevel = (LogEventLevel)WindowLogLevelComboBox.SelectedItem;

            windowLogSinkController.CurrentLevel = selectedLevel;

            loggerFactory.CreateLogger<IntegrationComputerMainWindow>()
                .LogInformation($"Log detail for this window is now {selectedLevel}. The log file's level is unchanged.");
        }

        private async void IntegrationComputerMainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            thalassaCore.DisplayInput = DispatchAppendText;
            thalassaCore.AbortCommandIssued = AbortAllExecutingCommands;
            await core.EnactIsRunning();
            outputScrollViewer = (ScrollViewer)FindName("OutputScrollViewer");

            core.OnLoaded();
        }

        private void DispatchAppendText(string text)
        {
            Dispatcher.Invoke(() => OutputRichTextBox.AppendText(text));
        }

        private void SetToggleButtonContent()
        {
            Action behaviorToExecute = () => ToggleRunningButton.Content = core.IsRunning ? "Twitch Disconnect" : "Twitch Connect";

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


            Action behaviorToExecute = () =>
            {
                double verticalOffset = outputScrollViewer.VerticalOffset;
                double extentHeight = outputScrollViewer.ExtentHeight;
                double viewportHeight = outputScrollViewer.ViewportHeight;

                bool wasScrolledToBottom = verticalOffset + viewportHeight >= extentHeight;


                OutputRichTextBox.AppendText($"\n{newContent}");

                if (wasScrolledToBottom)
                {
                    OutputRichTextBox.ScrollToEnd();
                }
            };

            if (OutputRichTextBox.Dispatcher.Thread == Thread.CurrentThread)
            {
                behaviorToExecute();
            }
            else
            {
                OutputRichTextBox.Dispatcher.Invoke(behaviorToExecute);
            }
        }

        private void ClearOutputButton_Click(object sender, RoutedEventArgs e)
        {
            OutputRichTextBox.Document.Blocks.Clear();
        }

        private void SpawnNewTestChatWindowButton_Click(object sender, RoutedEventArgs e)
        {
            ChatWindow activeChatWindow = chatWindowFactory.CreateNew();

            activeChatWindow.OnNewChatComputer += () =>
            {
                core.ActiveChatComputer = activeChatWindow.ActiveChatComputer;
                activeChatWindow.Closed += (sender, args) => { chatWindows.Remove(activeChatWindow); };
                activeChatWindow.OnAbortingRunningCommand = AbortAllExecutingCommands;

                core.OnCommandListChanged = commandListCount => activeChatWindow.RunningCommandCountChanged(commandListCount);
                core.OutputToStreamer = activeChatWindow.OutputToStreamer;
            };

            chatWindows.Add(activeChatWindow);

            activeChatWindow.Show();
        }

        private void AbortAllExecutingCommands()
        {
            CommandBase[] removalQueue = new CommandBase[core.ExecutingCommands.Count()];
            core.ExecutingCommands.CopyTo(removalQueue);

            foreach (CommandBase command in removalQueue)
            {
                command.Abort();
            }

            if (removalQueue.Length > 0)
            {
                speechComputer.Speak($"Aborted {removalQueue.Length} commands.");
            }
            else
            {
                speechComputer.Speak($"No commands to abort.");
            }
        }

        private void OnSpeechInterpreted(string interpretedSpeech)
        {
            if (chatWindows.Any())
            {
                chatWindows.First().ActiveChatComputer.SendChat(streamerProfileSettings.StreamerName, $"(spoken) - {interpretedSpeech}");
            }
        }

        private void OutputRichTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (Autoscroll.IsChecked == true)
            {
                OutputScrollViewer.ScrollToEnd();
            }
        }

        private void Autoscroll_Checked(object sender, RoutedEventArgs e)
        {
            if (this.IsInitialized && Autoscroll.IsChecked == true)
            {
                OutputScrollViewer.ScrollToEnd();
            }
        }
    }
}
