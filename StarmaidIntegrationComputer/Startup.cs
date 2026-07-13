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
using Serilog;
using System.IO;
using Microsoft.Extensions.Configuration;
using StarmaidIntegrationComputer.Common.DataStructures.Audience;
using System.Linq;
using StarmaidIntegrationComputer.Common.Assets;
using StarmaidIntegrationComputer.Common.Settings;
using StarmaidIntegrationComputer.UdpThalassaControl;
using StarmaidIntegrationComputer.Common.TasksAndExecution;
using StarmaidIntegrationComputer.Thalassa.WakeWordProcessor;
using StarmaidIntegrationComputer.Twitch.ExternalApiClients.Pronouns;
using StarmaidIntegrationComputer.Commands;
using TwitchLib.Client;

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
            ThalassaSensitiveSettings thalassaSensitiveSettings = InjectSetting<ThalassaSensitiveSettings>(services, configuration);
            InjectSetting<TwitchSettings>(services, configuration);
            InjectSetting<ThalassaSettings>(services, configuration);
            InjectSetting<ViolaWakeSettings>(services, configuration);
            InjectSetting<SoundPathSettings>(services, configuration);
            InjectSetting<OpenAISettings>(services, configuration);
            InjectSetting<SpeechReplacements>(services, configuration);
            InjectSetting<DiscordSensitiveSettings>(services, configuration);
            InjectSetting<DiscordSettings>(services, configuration);
            InjectSetting<StreamerProfileSettings>(services, configuration);
            InjectSetting<UdpCommandSettings>(services, configuration);

            //TODO: Pretty sure awake brain knows a better way to load settings than what's in this method.  Sleepy brain does not.
            services.AddScoped<TwitchAuthorizationUserTokenFlowHelperCtorArgs>();
            services.AddSingleton<PronounsClient>();

            var scopes = new List<AuthScopes> { AuthScopes.Helix_Channel_Read_Redemptions,
                AuthScopes.Chat_Read,
                AuthScopes.Chat_Edit,
                AuthScopes.Helix_Moderator_Manage_Banned_Users,
                AuthScopes.Helix_Moderator_Read_Followers,
                ////Not yet needed:
                //AuthScopes.Helix_Channel_Read_Redemptions,
                //AuthScopes.Helix_Channel_Manage_Redemptions,

                ////Doesn't exist in current version of TwitchLib:
                //, AuthScopes.Helix_Moderator_Manage_Shoutouts
                //, AuthScopes.Helix_Moderator_Manage_Shield_Mode
                };
            services.AddSingleton<IntegrationComputerMainWindow>();
            services.AddSingleton<TwitchAuthResponseWebserver>();
            services.AddScoped<AuthResponseParsing>();
            services.AddScoped<IntegrationComputerCore>();
            services.AddScoped<TwitchAuthorizationUserTokenFlowHelper>();
            services.AddSingleton<ThalassaCore>();
            services.AddScoped(_ => scopes);
            services.AddScoped<ChatComputer>();
            services.AddScoped<ChatWindowFactory>();
            services.AddScoped<SpeechComputer>();
            services.AddScoped<VoiceToTextManager>();
            services.AddSingleton<TranscriptionSender>();
            services.AddSingleton<VoiceListener>();
            services.AddHttpClient();
            services.AddSingleton<AudienceRegistry>();
            services.AddSingleton<LiveAuthorizationInfo>();
            services.AddSingleton<SoundEffectPlayer>();
            services.AddSingleton<ThalassaToolBuilder>();
            services.AddSingleton<UdpCommandListener>();
            services.AddSingleton<RemoteThalassaControlInterpreter>();
            services.AddSingleton<IUiThreadDispatchInvoker, UiThreadDispatchInvoker>();
            services.AddSingleton<IOpenAiTtsDispatcher, OpenAiTtsDispatcher>();
            services.AddSingleton<IWakeWordProcessorFactory, WakeWordProcessorFactory>();
            services.AddSingleton<AssetDownloader>();
            services.AddSingleton(new TwitchClient());
            services.AddScoped<CommandFactory>();
            services.AddSingleton<PronounLookupService>();

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

            var configurationBuilder = new ConfigurationBuilder()
                .SetBasePath(basePath);

            foreach (string path in GetOrderedJsonFilePaths(sensitivePath, nonconfidentialPath))
            {
                configurationBuilder.AddJsonFile(path, optional: false, reloadOnChange: true);
            }

            return configurationBuilder.Build();
        }

        /// <summary>
        /// Config files live under Config/Sensitive and Config/Nonconfidential, in any number of
        /// subfolders (e.g. Config/Nonconfidential/WakeWord) - grouping related settings files
        /// together without cluttering the top level. Base files load first, then per-environment
        /// overrides (*.&lt;environment&gt;.json), so an override always wins for keys it defines.
        /// </summary>
        private IEnumerable<string> GetOrderedJsonFilePaths(string sensitivePath, string nonconfidentialPath)
        {
            IEnumerable<string> AllJsonFilesUnder(string root) => Directory.GetFiles(root, "*.json", SearchOption.AllDirectories);

            IEnumerable<string> baseFiles = AllJsonFilesUnder(sensitivePath).Where(path => !IsEnvironmentFile(path))
                .Concat(AllJsonFilesUnder(nonconfidentialPath).Where(path => !IsEnvironmentFile(path)));

            IEnumerable<string> environmentOverrideFiles = AllJsonFilesUnder(sensitivePath).Where(IsCurrentEnvironment)
                .Concat(AllJsonFilesUnder(nonconfidentialPath).Where(IsCurrentEnvironment));

            return baseFiles.Concat(environmentOverrideFiles);
        }

        private bool IsEnvironmentFile(string fileName)
        {
            return allEnvironmentNames.Any(environmentName => fileName.EndsWith($".{environmentName}.json"));
        }

        private bool IsCurrentEnvironment(string fileName)
        {
            return fileName.EndsWith($".{currentEnvironmentName}.json");
        }

        //NOTE: reloadOnChange: true on AddJsonFile (above) makes the underlying IConfigurationRoot
        //watch each file and refresh its own in-memory values on change, but InjectSetting below only
        //calls Bind() once at startup into a plain POCO singleton - nothing re-binds it afterward, so
        //edits to config files still require an app restart to take effect. Real hot reload would mean
        //switching these settings classes to the Options pattern (services.Configure<T> + IOptionsMonitor<T>
        //for consumers that need live updates, or a ChangeToken.OnChange callback that re-binds the
        //existing singleton). Deliberately left out of scope for now.
        private T InjectSetting<T>(ServiceCollection services, IConfigurationRoot configuration) where T : class, new()
        {
            T setting = new T();
            configuration.GetSection(typeof(T).Name).Bind(setting);
            services.AddSingleton(setting);

            return setting;
        }
    }
}
