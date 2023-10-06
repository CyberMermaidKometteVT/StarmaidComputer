using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

using Microsoft.Extensions.Logging;

using OpenAI.Managers;

using StarmaidIntegrationComputer.Common.DataStructures.StarmaidState;
using StarmaidIntegrationComputer.Common.Settings;
using StarmaidIntegrationComputer.Thalassa;
using StarmaidIntegrationComputer.Thalassa.Chat;
using StarmaidIntegrationComputer.Thalassa.Settings;
using StarmaidIntegrationComputer.Thalassa.SpeechSynthesis;
using StarmaidIntegrationComputer.Thalassa.VoiceToText;

namespace StarmaidIntegrationComputer.Chat
{
    /// <summary>
    /// Interaction logic for ChatWindow.xaml
    /// </summary>
    public partial class ChatWindow : Window
    {
        private readonly StarmaidStateBag stateBag;
        private readonly ILogger<ChatComputer> logger;

        private ChatComputer? activeChatComputerUsePropertyOnly;
        public ChatComputer? ActiveChatComputer
        {
            get
            {
                if (activeChatComputerUsePropertyOnly == null)
                {
                    CreateNewChatComputer();
                }
                return activeChatComputerUsePropertyOnly;
            }
            private set { activeChatComputerUsePropertyOnly = value; }
        }

        private readonly IReadOnlyList<Control> controlsForResize;
        private const int defaultFontSize = 12;
        private readonly OpenAISettings openAISettings;
        private readonly SoundEffectPlayer soundEffectPlayer;
        private readonly ThalassaCore thalassaCore;
        private readonly SpeechComputer speechComputer;
        private readonly VoiceListener voiceListener;
        private readonly OpenAIService openAIService;
        private readonly StreamerProfileSettings streamerProfileSettings;
        private readonly ThalassaFunctionBuilder thalassaFunctionBuilder;
        private Action onNewChatComputerUsePropertyOnly = null;

        /// <summary>
        /// Needs to be assigned before creating a new chat computer!
        /// </summary>
        public Action OnNewChatComputer
        {
            get { return onNewChatComputerUsePropertyOnly; }
            set
            {
                onNewChatComputerUsePropertyOnly = value;
                if (value != null)
                {
                    value();
                }
            }
        }

        //TODO: Consider ripping logic out into a custom control, and/or a controller for the Thalassa command strip.
        public ChatWindow(ChatWindowCtorArgs args)
        {
            this.stateBag = args.StateBag;
            this.logger = args.Logger;
            this.openAISettings = args.OpenAISettings;
            this.soundEffectPlayer = args.SoundEffectPlayer;
            this.thalassaCore = args.ThalassaCore;
            this.speechComputer = args.SpeechComputer;
            this.voiceListener = args.VoiceListener;
            this.openAIService = args.OpenAIService;
            this.streamerProfileSettings = args.StreamerProfileSettings;
            this.thalassaFunctionBuilder = args.ThalassaFunctionBuilder;

            AddButtonStateEventHandlers();

            InitializeComponent();


            this.controlsForResize = new List<Control> { ChatbotResponsesRichTextBox, ThalassaLabel, ThalassaListenToggleButton, ThalassaInputOverButton, ThalassaAbortCommandButton, ThalassaShutUpButton, /*AutoscrollCheckBox,*/ ResetConversationButton, UserNameLabel, UserNameTextBox, UserMessageLabel, UserMessageTextBox, SendMessageButton }
            .AsReadOnly();

            CreateNewChatComputer();

            SetAllButtonStates(speechComputer);

            UserNameTextBox.Text = streamerProfileSettings.StreamerName;

            ChatbotResponsesRichTextBox.Document.LineHeight = 1;
            RemoveBlankFirstRichTextBoxLine();
        }

        private void RemoveBlankFirstRichTextBoxLine()
        {
            if ((ChatbotResponsesRichTextBox.Document.Blocks.FirstBlock as Paragraph).Inlines.Count() == 0)
            {
                ChatbotResponsesRichTextBox.Document.Blocks.Remove(ChatbotResponsesRichTextBox.Document.Blocks.FirstBlock);
            }
        }

        private void AddButtonStateEventHandlers()
        {
            speechComputer.SpeechStartingHandlers.Add(OnThalassaSpeechBegun);
            speechComputer.SpeechCompletedHandlers.Add(OnThalassaSpeechOver);

            thalassaCore.StartingListeningHandlers.Add(PlayStartingListening);
            thalassaCore.StoppingListeningHandlers.Add(PlayStoppingListening);

            voiceListener.SessionStartingHandlers.Add(OnSpeechInterpretationBegun);
            voiceListener.SessionCompleteHandlers.Add(OnSpeechInterpretationOver);
        }

        private void CreateNewChatComputer()
        {
            ActiveChatComputer = new ChatComputer(stateBag, openAISettings, logger, openAIService, thalassaFunctionBuilder, streamerProfileSettings);
            ActiveChatComputer.OutputUserMessageHandlers.Add(OnMessageSent);
            ActiveChatComputer.OutputChatbotChattingMessageHandlers.Add(OnMessageReceived);

            if (OnNewChatComputer != null)
            {
                OnNewChatComputer();
            }
        }

        private void SetAllButtonStates(SpeechComputer speechComputer)
        {
            if (speechComputer.IsSpeaking)
            {
                OnThalassaSpeechBegun();
            }
            else
            {
                OnThalassaSpeechOver();
            }

            UpdateStartListeningLabel();
        }


        private void UpdateStartListeningLabel()
        {
            if (thalassaCore.Listening)
            {
                ThalassaListenToggleButton.Content = "S_leep";
            }
            else
            {
                ThalassaListenToggleButton.Content = "_Listen";
            }
        }

        private void PlayStartingListening()
        {
            Dispatcher.Invoke(soundEffectPlayer.PlayStartingListeningFile);
        }

        private void PlayStoppingListening()
        {
            Dispatcher.Invoke(soundEffectPlayer.PlayStoppingListeningFile);
        }

        #region Event handlers
        private void OnSpeechInterpretationBegun()
        {
            Dispatcher.Invoke(() =>
            {
                ThalassaInputOverButton.IsEnabled = true;
                ThalassaInputOverButton.Content = "_Input Over";
            });
        }

        private void OnSpeechInterpretationOver()
        {
            Dispatcher.Invoke(() =>
            {
                ThalassaInputOverButton.IsEnabled = false;
                ThalassaInputOverButton.Content = "(Not Currently Awake)";
            });
        }

        private void OnThalassaSpeechBegun()
        {
            ThalassaShutUpButton.IsEnabled = true;
            ThalassaShutUpButton.Content = "Shut up!";
        }

        private void OnThalassaSpeechOver()
        {
            ThalassaShutUpButton.IsEnabled = false;
            ThalassaShutUpButton.Content = "(Not Talking)";

        }

        private void ThalassaListenToggleButton_Click(object sender, RoutedEventArgs e)
        {

            if (thalassaCore.Listening)
            {
                thalassaCore.StopListening();
            }
            else
            {
                thalassaCore.StartListening();
            }

            UpdateStartListeningLabel();
        }


        private void UserMessageTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            SendMessageButton.IsEnabled = !String.IsNullOrWhiteSpace(UserMessageTextBox.Text);
        }

        private void SendMessageButton_Click(object sender, RoutedEventArgs e)
        {
            SendMessage();
        }

        private void SendMessage()
        {
            //Ignoring the await warning because we don't actually care to wait for the responses here - these messages are just getting sent!
#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
            if (ActiveChatComputer == null)
            {
                CreateNewChatComputer();
            }

            if (String.IsNullOrWhiteSpace(UserNameTextBox.Text))
            {
                ActiveChatComputer.SendChat(streamerProfileSettings.StreamerName, UserMessageTextBox.Text);
            }
            else
            {
                ActiveChatComputer.SendChat(UserNameTextBox.Text, UserMessageTextBox.Text);
            }
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
        }

        private async Task OnMessageSent(string userName, string sentMessage)
        {
            await AppendLabeledText($"{userName}: ", sentMessage, 0.5);
            await ExecuteOnDispatcherThread(UserMessageTextBox.Clear);
        }

        private Task OnMessageReceived(string receivedMessage)
        {
            return AppendLabeledText($"{streamerProfileSettings.AiName}: ", receivedMessage, 1);
        }

        private Task AppendLabeledText(string label, string text, double dividerLineThickness = 0)
        {
            Action append = () =>
            {
                Paragraph paragraph = new Paragraph();

                Span boldSpan = new Span(new Run(label));
                boldSpan.FontWeight = FontWeights.Bold;

                Run textRun = new Run(text);

                paragraph.Inlines.Add(boldSpan);
                paragraph.Inlines.Add(textRun);

                if (dividerLineThickness != 0)
                {
                    Border divider = new Border();
                    divider.BorderThickness = new Thickness(0, dividerLineThickness, 0, 0);
                    divider.BorderBrush = new SolidColorBrush(Colors.Gray);
                    divider.Margin = new Thickness(0, 5, 0, 5);

                    paragraph.Inlines.Add(divider);
                }

                this.ChatbotResponsesRichTextBox.Document.Blocks.Add(paragraph);
            };

            return ExecuteOnDispatcherThread(append);
        }

        private Task ExecuteOnDispatcherThread(Action action)
        {
            if (Dispatcher.Thread == Thread.CurrentThread)
            {
                action();
                return Task.CompletedTask;
            }
            return Dispatcher.InvokeAsync(action).Task;
        }

        private void UserMessageTextBox_KeyUp(object sender, KeyEventArgs e)
        {
            //Only send the message if we're hitting ENTER but not SHIFT-ENTER
            if (e.Key == Key.Enter)
            {
                if (!Keyboard.IsKeyDown(Key.LeftShift) && !Keyboard.IsKeyDown(Key.RightShift))
                {
                    SendMessage();
                }
                else
                {

                    int selectionStart = UserMessageTextBox.SelectionStart;
                    UserMessageTextBox.Text = UserMessageTextBox.Text.Insert(selectionStart, Environment.NewLine);
                    UserMessageTextBox.SelectionStart = selectionStart + 2;
                }
            }
        }

        private void ResetConversation_Click(object sender, RoutedEventArgs e)
        {
            CreateNewChatComputer();
            ChatbotResponsesRichTextBox.Document.Blocks.Clear();
        }

        private void ChatbotResponsesRichTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (AutoscrollCheckBox.IsChecked == true)
            {
                ChatbotResponsesScrollViewer.ScrollToEnd();
            }
        }

        private void Autoscroll_Checked(object sender, RoutedEventArgs e)
        {
            if (this.IsInitialized && AutoscrollCheckBox.IsChecked == true)
            {
                ChatbotResponsesScrollViewer.ScrollToEnd();
            }
        }
        #endregion Event handlers

        private void Window_Closed(object sender, EventArgs e)
        {
            this.thalassaCore.StartingListeningHandlers.Remove(PlayStartingListening);
            this.thalassaCore.StoppingListeningHandlers.Remove(PlayStoppingListening);
        }

        private void ThalassaShutUpButton_Click(object sender, RoutedEventArgs e)
        {
            speechComputer.CancelSpeech();
        }

        private void ThalassaInputOverButton_Click(object sender, RoutedEventArgs e)
        {
            voiceListener.StopListening();
        }

        private void Window_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (Keyboard.Modifiers == ModifierKeys.Control)
            {
                IncrementFormTextScale(e.Delta / 120);
                e.Handled = true;
            }
        }
        private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
        {

            if (Keyboard.Modifiers == ModifierKeys.Control && e.Key == Key.D0)
            {
                ResetFormTextScale();
                e.Handled = true;
            }
        }

        private void IncrementFormTextScale(int numberOfClicks)
        {
            foreach (Control control in controlsForResize)
            {
                if (control.FontSize + numberOfClicks > 0)
                {
                    control.FontSize += numberOfClicks;
                }
            }


            ScaleTransform transform = (AutoscrollCheckBox.RenderTransform as ScaleTransform) ?? new ScaleTransform(1.0, 1.0, 0.5, 0.5);
            transform.ScaleX += numberOfClicks / 10.0;
            transform.ScaleY += numberOfClicks / 10.0;

            AutoscrollCheckBox.RenderTransform = transform;
            AutoscrollCheckBox.RenderTransformOrigin = new Point(1, 0);
        }

        private void ResetFormTextScale()
        {
            foreach (Control control in controlsForResize)
            {
                control.FontSize = defaultFontSize;
            }
            AutoscrollCheckBox.RenderTransform = new ScaleTransform(1.0, 1.0, 0.5, 0.5);

        }
    }
}
