﻿using System;
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
using StarmaidIntegrationComputer.Common.DataStructures.StarmaidState;
using System.Linq;
using StarmaidIntegrationComputer.Common.Settings;
using StarmaidIntegrationComputer.UdpThalassaControl;
using StarmaidIntegrationComputer.Common.TasksAndExecution;

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
            InjectSetting<DiscordSensitiveSettings>(services, configuration);
            InjectSetting<DiscordSettings>(services, configuration);
            InjectSetting<StreamerProfileSettings>(services, configuration);
            InjectSetting<UdpCommandSettings>(services, configuration);

            //TODO: Pretty sure awake brain knows a better way to load settings than what's in this method.  Sleepy brain does not.
            services.AddScoped<IntegrationComputerCoreCtorArgs>();
            services.AddScoped<TwitchAuthorizationUserTokenFlowHelperCtorArgs>();

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
            services.AddSingleton<StarmaidStateBag>();
            services.AddSingleton<LiveAuthorizationInfo>();
            services.AddSingleton<SoundEffectPlayer>();
            services.AddSingleton<ThalassaToolBuilder>();
            services.AddSingleton<UdpCommandListener>();
            services.AddSingleton<RemoteThalassaControlInterpreter>();
            services.AddSingleton<IUiThreadDispatchInvoker, UiThreadDispatchInvoker>();
            services.AddSingleton<IOpenAiTtsDispatcher, OpenAiTtsDispatcher>();

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
    }
}
