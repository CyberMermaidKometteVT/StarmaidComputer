using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Controls;

using Microsoft.Extensions.Logging;

using Serilog;

using StarmaidIntegrationComputer.Chat;
using StarmaidIntegrationComputer.Thalassa;

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
        LoggerConfiguration loggerConfiguration;

        ThalassaWindow thalassaForm;
        public List<ChatWindow> chatWindows { get; private set; } = new List<ChatWindow>();

        ScrollViewer outputScrollViewer;



        public IntegrationComputerMainWindow(ILoggerFactory loggerFactory, IntegrationComputerCore core, ThalassaWindow thalassaForm, LoggerConfiguration loggerConfiguration, ChatWindowFactory chatWindowFactory, ThalassaCore thalassaCore)
        {
            this.loggerFactory = loggerFactory;
            this.core = core;
            this.thalassaForm = thalassaForm;
            this.loggerConfiguration = loggerConfiguration;
            this.chatWindowFactory = chatWindowFactory;
            this.thalassaCore = thalassaCore;
            this.core.Output = AppendOutput;
            this.core.UpdateIsRunningVisuals = SetToggleButtonContent;

            Loaded += IntegrationComputerMainWindow_Loaded;


            InitializeComponent();
            InitializeLogging();
            InitializeThalassaForm();
            SetToggleButtonContent();

            //This should realy live somewhere else
            thalassaCore.SpeechInterpreted = OnSpeechInterpreted;
        }

        private void InitializeLogging()
        {
            loggerConfiguration.WriteTo.RichTextBox(OutputRichTextBox);


            LoggerConfiguration textBoxLoggerConfiguration = new LoggerConfiguration();
            textBoxLoggerConfiguration.WriteTo.RichTextBox(OutputRichTextBox);

            Serilog.Core.Logger? serilogLogger = textBoxLoggerConfiguration.CreateLogger();

            loggerFactory.AddSerilog(serilogLogger, true);
        }

        private void InitializeThalassaForm()
        {
            thalassaForm.DisplayInput = OutputRichTextBox.AppendText;
            thalassaForm.LoggerFactory = loggerFactory;
        }

        private async void IntegrationComputerMainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            await core.EnactIsRunning();
            outputScrollViewer = (ScrollViewer)FindName("OutputScrollViewer");
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

        private void SpawnNewTestChatWindowButton_Click(object sender, RoutedEventArgs e)
        {
            var activeChatWindow = chatWindowFactory.CreateNew();

            activeChatWindow.OnNewChatComputer += () =>
            {
                core.ActiveChatComputer = activeChatWindow.ActiveChatComputer;
                activeChatWindow.Closed += (sender, args) => { chatWindows.Remove(activeChatWindow); };
            };

            chatWindows.Add(activeChatWindow);

            activeChatWindow.Show();
        }


        private void OnSpeechInterpreted(string interpretedSpeech)
        {
            if (chatWindows.Any())
            {
                chatWindows.First().ActiveChatComputer.SendChat("Komette", $"(spoken) - {interpretedSpeech}");
            }
        }

        private void OutputRichTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (Autoscroll.IsChecked == true)
            {
                OutputScrollViewer.ScrollToEnd();
            }
        }
    }
}
