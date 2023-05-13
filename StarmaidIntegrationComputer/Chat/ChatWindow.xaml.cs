using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

using Microsoft.Extensions.Logging;

using OpenAI_API;

namespace StarmaidIntegrationComputer.Chat
{
    /// <summary>
    /// Interaction logic for ChatWindow.xaml
    /// </summary>
    public partial class ChatWindow : Window
    {
        private readonly OpenAIAPI api;
        private readonly ILogger<ChatComputer> logger;
        private ChatComputer? chatComputer;
        private readonly string jailbreakMessage;

        public ChatWindow(OpenAIAPI api, ILogger<ChatComputer> logger, string jailbreakMessage)
        {
            this.api = api;
            this.logger = logger;
            this.jailbreakMessage = jailbreakMessage;
            InitializeComponent();
            CreateNewChatComputer();
        }

        private void CreateNewChatComputer()
        {
            chatComputer = new ChatComputer(api, jailbreakMessage, logger);
            chatComputer.OutputUserMessageHandlers.Add(OnMessageSent);
            chatComputer.OutputChatbotResponseHandlers.Add(OnMessageReceived);
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
            if (chatComputer != null)
            {
                CreateNewChatComputer();
            }

            if (String.IsNullOrWhiteSpace(UserNameTextBox.Text))
            {
                chatComputer.SendChat(UserMessageTextBox.Text);
            }
            else
            {
                chatComputer.SendChat(UserNameTextBox.Text, UserMessageTextBox.Text);
            }
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
        }

        //I'm worried it's a bad idea to make this async, since it involves thread I/O
        private Task OnMessageSent(string sentMessage)
        {
            sentMessage += Environment.NewLine;
            if (Dispatcher.Thread == Thread.CurrentThread)
            {
                ChatbotResponsesRichTextBox.AppendText(sentMessage);
                UserMessageTextBox.Clear();
                return Task.CompletedTask;
            }
            else
            {
                return Dispatcher.InvokeAsync(() =>
                {
                    ChatbotResponsesRichTextBox.AppendText(sentMessage);
                    UserMessageTextBox.Clear();
                }).Task;
            }
        }

        private Task OnMessageReceived(string receivedMessage)
        {
            receivedMessage += Environment.NewLine;
            if (Dispatcher.Thread == Thread.CurrentThread)
            {
                ChatbotResponsesRichTextBox.AppendText(receivedMessage);
                return Task.CompletedTask;
            }
            else
            {
                return Dispatcher.InvokeAsync(() => ChatbotResponsesRichTextBox.AppendText(receivedMessage)).Task;
            }
        }

        private void UserMessageTextBox_KeyUp(object sender, KeyEventArgs e)
        {
            //Only send the message if we're hitting ENTER but not SHIFT-ENTER
            if (e.Key == Key.Enter && !Keyboard.IsKeyDown(Key.LeftShift) && !Keyboard.IsKeyDown(Key.RightShift))
            {
                SendMessage();
            }
        }

        private void ResetConversation_Click(object sender, RoutedEventArgs e)
        {
            CreateNewChatComputer();
            ChatbotResponsesRichTextBox.Document.Blocks.Clear();
        }
    }
}
