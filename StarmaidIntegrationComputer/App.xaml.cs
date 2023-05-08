﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Windows;

using Microsoft.Extensions.DependencyInjection;

using Serilog;

using StarmaidIntegrationComputer.StarmaidSettings;
using StarmaidIntegrationComputer.Twitch;
using StarmaidIntegrationComputer.Twitch.Authorization;

using Thalassa;

using TwitchLib.Api.Core.Enums;

namespace StarmaidIntegrationComputer
{

    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private ServiceProvider serviceProvider;
        private readonly string settingsFilePath = "settings.json";

        public App()
        {
            ServiceCollection services = new ServiceCollection();
            ConfigureServices(services);
            serviceProvider = services.BuildServiceProvider();
        }

        private void ConfigureServices(ServiceCollection services)
        {
            services.AddLogging(loggingBuilder =>
            {
                loggingBuilder.AddSerilog();
            });

            //TODO: Pretty sure awake brain knows a better way to load settings than what's in this method.  Sleepy brain does not.
            var settings = LoadSettings();
            var scopes = new List<AuthScopes> { AuthScopes.Helix_Channel_Read_Redemptions, AuthScopes.Chat_Read, AuthScopes.Chat_Edit};
            services.AddSingleton<IntegrationComputerMainWindow>();
            services.AddSingleton(settings);
            services.AddSingleton<TwitchAuthResponseWebserver>();
            services.AddScoped<AuthResponseParsing>();
            services.AddScoped<IntegrationComputerCore>();
            services.AddScoped<TwitchAuthorizationUserTokenFlowHelper>();
            services.AddScoped(_ => TwitchApiFactory.Build(settings.TwitchClientId, settings.TwitchClientSecret, scopes));
            services.AddSingleton<ThalassaCore>();
            services.AddScoped<ThalassaWindow>();

            services.AddScoped(_ => scopes);

            serviceProvider = services.BuildServiceProvider();
        }

        private Settings LoadSettings()
        {
            string settingsJson = File.ReadAllText(settingsFilePath);
            JsonSettings? parsedSettings = null;
            try
            {
                parsedSettings = JsonSerializer.Deserialize<JsonSettings>(settingsJson);
            }
            catch (Exception)
            {
                MessageBox.Show($"Error in your settings file!");
            }

            if (parsedSettings == null)
            {
                throw new InvalidOperationException("Failed to load settings file! 😟");
            }

            return ConvertSettings(parsedSettings);
        }

        private Settings ConvertSettings(JsonSettings parsedSettings)
        {
            Settings settings = new Settings
            {
                DiscordWebhookUrl = parsedSettings.DiscordWebhookUrl,
                RedirectUri = parsedSettings.RedirectUri,
                Roles = parsedSettings.Roles,
                RunOnStartup = parsedSettings.RunOnStartup,
                TwitchApiUrl = parsedSettings.TwitchApiUrl,
                TwitchClientId = parsedSettings.TwitchClientId,
                TwitchClientSecret = parsedSettings.TwitchClientSecret,
                TwitchApiUsername = parsedSettings.TwitchApiUsername,
                TwitchChatbotChannelName = parsedSettings.TwitchChatbotChannelName,
                TwitchChatbotUsername = parsedSettings.TwitchChatbotUsername
            };



            settings.ChatCommandIdentifier = ParseCharFromStringInJson(parsedSettings.ChatCommandIdentifier, nameof(parsedSettings.ChatCommandIdentifier)) ?? settings.ChatCommandIdentifier;

            settings.WhisperCommandIdentifier = ParseCharFromStringInJson(parsedSettings.WhisperCommandIdentifier, nameof(parsedSettings.WhisperCommandIdentifier)) ?? settings.WhisperCommandIdentifier;

            return settings;
        }

        /// <summary>
        /// This will exit the application if the value in question is too long to be a char, but if the value is missing altogether, will simply return <paramref name="defaultValue"/>.
        /// </summary>
        private char? ParseCharFromStringInJson(string source, string parsedName, char? defaultValue = null)
        {
            if (source == null || source.Length == 0)
            {
                return defaultValue;
            }

            bool identifierParsedSuccessfully = Char.TryParse(source, out char parsedChar);

            if (!identifierParsedSuccessfully)
            {
                MessageBox.Show($"Failed to parse {parsedName} in your settings file, it should be a one-character string like '!'.");
                Application.Current.Shutdown();

                //This is returning a value, but we ARE shutting down.  The default value is used to keep things from raising exceptions before shutdown.  Presumably the default value should be a reasonable default to keep operating temporarily.
                return defaultValue;
            }

            return parsedChar;
        }


        private void OnStartup(object sender, StartupEventArgs a)
        {
            var mainWindow = serviceProvider.GetRequiredService<IntegrationComputerMainWindow>();
            mainWindow.Show();
        }
    }
}
