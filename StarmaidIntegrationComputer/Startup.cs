using System;
using System.Collections.Generic;

using Microsoft.Extensions.DependencyInjection;
using StarmaidIntegrationComputer.Chat;
using StarmaidIntegrationComputer.StarmaidSettings;
using StarmaidIntegrationComputer.Thalassa.Chat;
using StarmaidIntegrationComputer.Thalassa.Settings;
using StarmaidIntegrationComputer.Thalassa.SpeechSynthesis;
using StarmaidIntegrationComputer.Thalassa.VoiceToText;
using StarmaidIntegrationComputer.Thalassa;
using StarmaidIntegrationComputer.Twitch.Authorization;
using StarmaidIntegrationComputer.Twitch;
using TwitchLib.Api.Core.Enums;
using OpenAI_API;
using Serilog;
using System.IO;
using System.Windows;
using Microsoft.Extensions.Configuration;
using StarmaidIntegrationComputer.Common.DataStructures.StarmaidState;
using System.Linq;
using Microsoft.Extensions.Hosting;

namespace StarmaidIntegrationComputer
{
    internal class Startup
    {

        private const string configFolder = "Config";
        private const string configSubfolderNonconfidential = "Nonconfidential";
        private const string configSubfolderSensitive = "Sensitive";

        private string[] allEnvironmentNames = { environmentNameLocal };
        private string currentEnvironmentName = environmentNameLocal;

        private const string environmentNameLocal = "local";


        public ServiceProvider ConfigureServices()
        {
            ServiceCollection services = new ServiceCollection();
            services.AddLogging(loggingBuilder =>
            {
                LoggerConfiguration fileLoggerConfiguration = new LoggerConfiguration();
                fileLoggerConfiguration.WriteTo.File($"Log\\StarmaidComputer-{DateTime.Now.ToString("yyyy-MM-dd_HH_mm_ss")}.log");
                Serilog.Core.Logger? fileLogger = fileLoggerConfiguration.CreateLogger();
                loggingBuilder.AddSerilog(fileLogger);

                services.AddSingleton(fileLoggerConfiguration);

            });

            IConfigurationRoot configuration = this.LoadConfiguration();

            TwitchSensitiveSettings twitchSensitiveSettings = InjectSetting<TwitchSensitiveSettings>(services, configuration);
            OpenAISensitiveSettings openAiSensitiveSettings = InjectSetting<OpenAISensitiveSettings>(services, configuration);
            InjectSetting<TwitchSettings>(services, configuration);
            InjectSetting<ThalassaSettings>(services, configuration);
            InjectSetting<SoundPathSettings>(services, configuration);
            InjectSetting<OpenAISettings>(services, configuration);
            InjectSetting<SpeechReplacements>(services, configuration);
            var one = InjectSetting<DiscordSensitiveSettings>(services, configuration);
            var two = InjectSetting<DiscordSettings>(services, configuration);

            //TODO: Pretty sure awake brain knows a better way to load settings than what's in this method.  Sleepy brain does not.
            services.AddScoped<IntegrationComputerCoreCtorArgs>();
            services.AddScoped<TwitchAuthorizationUserTokenFlowHelperCtorArgs>();

            var scopes = new List<AuthScopes> { AuthScopes.Helix_Channel_Read_Redemptions, AuthScopes.Chat_Read, AuthScopes.Chat_Edit
                //, AuthScopes.Helix_Moderator_Manage_Shoutouts
                };
            services.AddSingleton<IntegrationComputerMainWindow>();
            services.AddSingleton<TwitchAuthResponseWebserver>();
            services.AddScoped<AuthResponseParsing>();
            services.AddScoped<IntegrationComputerCore>();
            services.AddScoped<TwitchAuthorizationUserTokenFlowHelper>();
            services.AddSingleton<ThalassaCore>();
            services.AddScoped(_ => scopes);
            services.AddScoped(_ => new OpenAIAPI(openAiSensitiveSettings.OpenAIBearerToken));
            services.AddScoped<ChatComputer>();
            services.AddScoped<ChatWindowFactory>();
            services.AddScoped<SpeechComputer>();
            services.AddScoped<VoiceToTextManager>();
            services.AddSingleton<TranscriptionSender>();
            services.AddSingleton<VoiceListener>();
            services.AddHttpClient<TranscriptionSender>();
            services.AddSingleton<StarmaidStateBag>();
            services.AddSingleton<LiveAuthorizationInfo>();
            services.AddSingleton<SoundEffectPlayer>();

            services.AddScoped(_ =>
                TwitchApiFactory.Build(twitchSensitiveSettings.TwitchClientId, twitchSensitiveSettings.TwitchClientSecret, scopes)
                );

            return services.BuildServiceProvider();
        }

        private IConfigurationRoot LoadConfiguration()
        {
            var basePath = Path.Combine(Directory.GetCurrentDirectory(), configFolder);
            var sensitivePath = Path.Combine(basePath, configSubfolderSensitive);
            var nonconfidentialPath = Path.Combine(basePath, configSubfolderNonconfidential);

            List<string> allJsonFilePaths = new List<string>();

            allJsonFilePaths.AddRange(Directory.GetFiles(sensitivePath).Where(path => !IsEnvironmentFile(path)));
            allJsonFilePaths.AddRange(Directory.GetFiles(nonconfidentialPath).Where(path => !IsEnvironmentFile(path)));

            allJsonFilePaths.AddRange(Directory.GetFiles(sensitivePath).Where(IsCurrentEnvironment));
            allJsonFilePaths.AddRange(Directory.GetFiles(nonconfidentialPath).Where(IsCurrentEnvironment));

            var configurationBuilder = new ConfigurationBuilder()
                .SetBasePath(basePath);

            allJsonFilePaths.ForEach(path => configurationBuilder.AddJsonFile(path, false, true));

            var configuration = configurationBuilder.Build();

            return configuration;
        }

        private bool IsEnvironmentFile(string fileName)
        {
            return allEnvironmentNames.Any(environmentName => fileName.EndsWith($".{environmentName}.json"));
        }

        private bool IsCurrentEnvironment(string fileName)
{
            return fileName.EndsWith($".{currentEnvironmentName}.json");
        }


        private T InjectSetting<T>(ServiceCollection services, IConfigurationRoot configuration) where T : class, new()
        {
            T setting = new T();
            configuration.GetSection(typeof(T).Name).Bind(setting);
            services.AddSingleton(setting);

            return setting;
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


    }
}
