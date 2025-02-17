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
        private readonly OpenAISensitiveSettings openAISensitiveSettings;
        private readonly StreamerProfileSettings streamerProfileSettings;
        private readonly ThalassaToolBuilder thalassaFunctionBuilder;
        private Action? onNewChatComputerUsePropertyOnly = null;

        public Action? OnAbortingRunningCommand = null;

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
        public void RunningCommandCountChanged(int executingCommandCount)
        {
            Dispatcher.Invoke(() =>
            {
                if (executingCommandCount == 0)
                {
                    ThalassaAbortCommandButton.IsEnabled = false;
                    ThalassaAbortCommandButton.Content = "(No _Command Pending)";
                }
                else
                {
                    ThalassaAbortCommandButton.IsEnabled = true;
                    ThalassaAbortCommandButton.Content = $"Abort {executingCommandCount} _Commands)";
                }
            });
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
            this.openAISensitiveSettings = args.OpenAISensitiveSettings;
            this.streamerProfileSettings = args.StreamerProfileSettings;
            this.thalassaFunctionBuilder = args.ThalassaFunctionBuilder;

            AddButtonStateEventHandlers();

            InitializeComponent();

            this.controlsForResize = new List<Control> { ChatbotResponsesRichTextBox, ThalassaLabel, ThalassaListenToggleButton, ThalassaInputOverButton, ThalassaAbortCommandButton, ThalassaShutUpButton, /*AutoscrollCheckBox,*/ ResetConversationButton, UserNameLabel, UserNameTextBox, UserMessageLabel, UserMessageTextBox, SendMessageButton, WasNotTalkingToYouButton }
            .AsReadOnly();

            CreateNewChatComputer();

            SetAllButtonStates();

            UserNameTextBox.Text = streamerProfileSettings.StreamerName;

            ChatbotResponsesRichTextBox.Document.LineHeight = 1;
            RemoveBlankFirstRichTextBoxLine();
        }

        public void OutputToStreamer(string message)
        {
            ExecuteOnDispatcherThread(() => ChatbotResponsesRichTextBox.AppendText(message));
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
            thalassaCore.StoppingListeningHandlers.Add(OnStoppingListening);
            thalassaCore.IsListeningChangedHandlers.Add(UpdateStartListeningLabel);

            voiceListener.SessionStartingHandlers.Add(OnSpeechInterpretationBegun);
            voiceListener.SessionCompleteHandlers.Add(OnSpeechInterpretationOver);
        }

        private void CreateNewChatComputer()
        {
            ActiveChatComputer = new ChatComputer(stateBag, openAISettings, logger, openAISensitiveSettings, thalassaFunctionBuilder, streamerProfileSettings);
            ActiveChatComputer.OutputUserMessageHandlers.Add(OnMessageSent);
            ActiveChatComputer.OutputChatbotChattingMessageHandlers.Add(OnMessageReceived);

            if (OnNewChatComputer != null)
            {
                OnNewChatComputer();
            }
        }

        private void SetAllButtonStates()
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
            ExecuteOnDispatcherThread(() =>
            {

                if (thalassaCore.IsListening)
                {
                    ThalassaListenToggleButton.Content = "S_leep";
                }
                else
                {
                    ThalassaListenToggleButton.Content = "_Listen";
                }
            });
        }

        private void PlayStartingListening()
        {
            Dispatcher.Invoke(soundEffectPlayer.PlayStartingListeningFile);
        }

        private void OnStoppingListening()
        {
            PlayStoppingListening();

            OnSpeechInterpretationOver();
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

                WasNotTalkingToYouButton.IsEnabled = true;
                WasNotTalkingToYouButton.Content = "Not _Talking To You";
            });
        }

        private void OnSpeechInterpretationOver()
        {
            Dispatcher.Invoke(() =>
            {
                const string notCurrentlyAwakeText = "(Not Currently Awake)";
                ThalassaInputOverButton.IsEnabled = false;
                ThalassaInputOverButton.Content = notCurrentlyAwakeText;

                WasNotTalkingToYouButton.IsEnabled = false;
                WasNotTalkingToYouButton.Content = notCurrentlyAwakeText;
            });
        }

        private void OnThalassaSpeechBegun()
        {
            ExecuteOnDispatcherThread(() =>
            {
                ThalassaShutUpButton.IsEnabled = true;
                ThalassaShutUpButton.Content = "Shu_t up!";
            });
        }

        private void OnThalassaSpeechOver()
        {
            ExecuteOnDispatcherThread(() =>
            {
                ThalassaShutUpButton.IsEnabled = false;
                ThalassaShutUpButton.Content = "(Not Talking)";
            });

        }

        private void ThalassaListenToggleButton_Click(object sender, RoutedEventArgs e)
        {

            if (thalassaCore.IsListening)
            {
                thalassaCore.StopListening();
            }
            else
            {
                thalassaCore.StartListening();
            }
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

        private void EvaluateScrollbarForMessageTextBox()
        {
            double formHeight = this.ActualHeight;

            if (this.ActualHeight > formHeight * 0.5)
            {
                UserMessageScrollViewer.VerticalScrollBarVisibility = ScrollBarVisibility.Visible;
            }
            else
            {
                UserMessageScrollViewer.VerticalScrollBarVisibility = ScrollBarVisibility.Hidden;
            }
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
            EvaluateScrollbarForMessageTextBox();


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
            this.thalassaCore.StoppingListeningHandlers.Remove(OnStoppingListening);
        }

        private void ThalassaShutUpButton_Click(object sender, RoutedEventArgs e)
        {
            thalassaCore.CancelSpeech();
        }

        private void ThalassaInputOverButton_Click(object sender, RoutedEventArgs e)
        {
            thalassaCore.ConcludeCurrentListening();

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

            if (e.Key == Key.Enter)
            {
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
            AutoscrollCheckBox.RenderTransformOrigin = new System.Windows.Point(1, 0);
        }

        private void ResetFormTextScale()
        {
            foreach (Control control in controlsForResize)
            {
                control.FontSize = defaultFontSize;
            }
            AutoscrollCheckBox.RenderTransform = new ScaleTransform(1.0, 1.0, 0.5, 0.5);

        }

        private void ThalassaWasNotTalkingToYouButton_Click(object sender, RoutedEventArgs e)
        {
            thalassaCore.AbortCurrentListening();
        }

        private void ThalassaAbortCommandButton_Click(object sender, RoutedEventArgs e)
        {
            if (OnAbortingRunningCommand != null)
            {
                OnAbortingRunningCommand();
            }
        }

        private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            EvaluateScrollbarForMessageTextBox();

            //These calculations are garbage, the inside viewer really should not have the exact same actual height as its containing grid
            //  But we ball! Komette's tired. Maybe at some later date I'll make this less silly.
            //  Also, this doesn't follow good WPF principles, which should be using data binding.
            //  Thalassa suggests that I use binding with a converter for the MaxHeight of the viewer.
            OuterGrid.RowDefinitions[3].MaxHeight = this.ActualHeight / 2.0;
            UserMessageScrollViewer.MaxHeight = this.ActualHeight / 2.0;
        }
    }
}
