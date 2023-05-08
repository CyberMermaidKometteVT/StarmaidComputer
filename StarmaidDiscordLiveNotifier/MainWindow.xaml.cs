using Discord.Webhook;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;

namespace StarmaidDiscordLiveNotifier
{
    public partial class MainWindow : Window
    {
        private readonly string settingsFilePath = "settings.json";
        private Settings settings;
        private readonly Dictionary<string, ulong> roleIds = new Dictionary<string, ulong>();

        private const string twitchAccessTokenAuthenticationUrl = "https://id.twitch.tv/oauth2/token";

        private AccessToken accessToken;

        public MainWindow()
        {
            InitializeComponent();
            LoadSettings();

            CheckLiveStatusPeriodically();
        }

        private async Task AuthenticateToTwitch()
        {
            using (HttpClient? httpClient = new HttpClient())
            {
                Dictionary<string, string> contentToUrlEncode = new Dictionary<string, string> { { "client_id", settings.TwitchClientId },
                    { "client_secret", settings.TwitchClientSecret },
                    { "grant_type", "client_credentials"}
                };
                //string urlEncodedContent = HttpUtility.UrlEncode($"client_id={settings.TwitchClientId}&client_secret={settings.TwitchClientSecret}&grant_type=client_credentials" );
                HttpContent content = new FormUrlEncodedContent(contentToUrlEncode);
                HttpResponseMessage? requestResult = await httpClient.PostAsync(twitchAccessTokenAuthenticationUrl, content);
                string responseContent = await requestResult?.Content?.ReadAsStringAsync();

                if (requestResult?.IsSuccessStatusCode != true)
                {
                    throw new InvalidOperationException($"Failed to authenticate.  Response: {responseContent ?? $"HTTP {requestResult?.StatusCode}"}");
                }

                if (responseContent == null)
                {
                    throw new InvalidOperationException("Unexpected empty response!");
                }
                else
                {
                    accessToken = JsonDocument.Parse(responseContent).Deserialize<AccessToken>(new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
                }
            }
        }

        private async Task<bool> IsTwitchUserLiveAsync()
        {
            if (accessToken == null || accessToken.ExpiresAt < DateTime.Now.AddSeconds(5))
            {
                await AuthenticateToTwitch();
            }

            using (HttpClient? httpClient = new HttpClient())
            {
                httpClient.DefaultRequestHeaders.Add("Client-ID", settings.TwitchClientId);
                httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {accessToken.Token}");



                HttpResponseMessage? response = await httpClient.GetAsync(settings.TwitchApiUrl);

                string? responseContent = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    JsonDocument? json = JsonDocument.Parse(responseContent);
                    JsonElement data = json.RootElement.GetProperty("data");
                    return data.GetArrayLength() > 0;
                }
                else
                {
                    throw new InvalidOperationException($"Failed to retrieve Twitch stream data.  Response: {responseContent ?? $"HTTP {response.StatusCode}"}");
                }
            }
        }

        private async void CheckLiveStatusPeriodically()
        {
            while (true)
            {
                bool isLive = await IsTwitchUserLiveAsync();
                if (isLive)
                {
                    List<string> selectedRoles = GetSelectedRolesFromUser();
                    foreach (string roleName in selectedRoles)
                    {
                        if (roleIds.ContainsKey(roleName))
                        {
                            await SendDiscordMessageAsync(roleIds[roleName]);
                        }
                    }
                }

                await Task.Delay(TimeSpan.FromMinutes(1));
            }
        }

        private List<string> GetSelectedRolesFromUser()
        {
            RoleSelectorWindow? roleSelectorWindow = new RoleSelectorWindow(roleIds.Keys.ToList());
            roleSelectorWindow.ShowDialog();

            return roleSelectorWindow.SelectedRoles;
        }

        private async Task SendDiscordMessageAsync(ulong roleId)
        {
            DiscordWebhookClient? client = new DiscordWebhookClient(settings.DiscordWebhookUrl);


            string message = ($"Hey {GetRoleTags()}, {settings.TwitchApiUsername} is now live on Twitch!");

            await client.SendMessageAsync(message);

            client.Dispose();
        }

        private string GetRoleTags()
        {
            List<string>? roleTags = new List<string>();
            foreach (KeyValuePair<string, ulong> roleId in roleIds)
            {
                if (SelectedRolesListBox.SelectedItems.Contains(roleId.Key))
                {
                    roleTags.Add($"<@&{roleId.Value}>");
                }
            }

            return string.Join(", ", roleTags);
        }

        private void LoadSettings()
        {
            string settingsJson = File.ReadAllText(settingsFilePath);
            settings = JsonSerializer.Deserialize<Settings>(settingsJson);
        }
    }
}