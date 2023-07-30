using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using StarmaidIntegrationComputer.Twitch.Authorization.Models;

using TwitchLib.Api;
using Microsoft.Extensions.Logging;
using TwitchLib.Api.Core.Enums;
using TwitchLib.Api.Auth;
using Microsoft.Win32;
using System.IO;
using Microsoft.CodeAnalysis;
using StarmaidIntegrationComputer.Common.DataStructures;
using StarmaidIntegrationComputer.StarmaidSettings;

namespace StarmaidIntegrationComputer.Twitch.Authorization
{
    public class TwitchAuthorizationUserTokenFlowHelper
    {
        private readonly TwitchSensitiveSettings twitchSensitiveSettings;
        private readonly ILogger<TwitchAuthorizationUserTokenFlowHelper> logger;
        private TwitchAuthResponseWebserver webserver;

        private readonly List<AuthScopes> scopes;

        //This has a built-in call to the factory because I'm specifically contemplating releasing this class standalone and don't want to expect consumers to know how to inject it
        private TwitchAPI? TwitchApiConnection
        {
            get
            {
                if (twitchApiConnectionOnlyUseProperty == null) twitchApiConnectionOnlyUseProperty = TwitchApiFactory.Build(twitchSensitiveSettings.TwitchClientId, twitchSensitiveSettings.TwitchClientSecret, scopes);
                return twitchApiConnectionOnlyUseProperty;
            }
            set
            {
                twitchApiConnectionOnlyUseProperty = value;
            }
        }
        private TwitchAPI? twitchApiConnectionOnlyUseProperty;


        private Process? authorizationBrowserProcess;

        private string OauthState { get; set; } = InitializeOauthState();

        //Sleepy brain thoughts: what if these were put into some kind of StartInfo or ActionsToSet or some other composed class wrapper for runtime properties to set?  Would that be easier for consumers to understand?
        private readonly bool forceTwitchLoginPrompt;
        private readonly bool logInWithIncognitoBrowser;
        public Action<string> OnAuthorizationProcessFailed { get; set; }
        public Action OnAuthorizationProcessUserCanceled { get; set; }
        public Action<AccessToken> OnAuthorizationProcessSuccessful { get; set; }

        public TwitchAuthorizationUserTokenFlowHelper(TwitchAuthorizationUserTokenFlowHelperCtorArgs ctorArgs)
        {
            this.twitchSensitiveSettings = ctorArgs.TwitchSensitiveSettings;
            this.logger = ctorArgs.Logger;
            this.webserver = ctorArgs.Webserver;
            this.scopes = ctorArgs.Scopes;
            TwitchApiConnection = ctorArgs.TwitchApiConnection;

            forceTwitchLoginPrompt = ctorArgs.TwitchSettings.ForceTwitchLoginPrompt;
            logInWithIncognitoBrowser = ctorArgs.TwitchSettings.LogInWithIncognitoBrowser;

            this.webserver.OnError = AuthorizationServer_Error;
            this.webserver.OnAuthorizationCodeSet = AuthorizationServer_CodeSet;
        }


        private static string InitializeOauthState()
        {
            char[] validCharacters = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ1234567890 -_".ToCharArray();
            byte[] randomBytes = new byte[4 * 100];
            RandomNumberGenerator.Create().GetBytes(randomBytes);

            StringBuilder state = new StringBuilder();
            foreach (byte randomByte in randomBytes)
            {
                state.Append(validCharacters[randomByte % validCharacters.Length]);
            }
            return state.ToString();
        }
        public void PromptForUserAuthorization()
        {
            var authorizationCodeUrl = new TwitchAPI().Auth.GetAuthorizationCodeUrl(twitchSensitiveSettings.RedirectUri, scopes, forceTwitchLoginPrompt, OauthState, twitchSensitiveSettings.TwitchClientId);

            logger.LogInformation("Showing auth browser window.");
            webserver.StartListening(OauthState);

            authorizationBrowserProcess = Process.Start(GetProcessStartInfoForBrowsingPrivately(authorizationCodeUrl));
            authorizationBrowserProcess.Exited += AuthoriationProcess_Exited;
        }

        //NOTE: THIS WHOLE METHOD IS VERY OS-DEPENDENT!
        private ProcessStartInfo GetProcessStartInfoForBrowsingPrivately(string authorizationCodeUrl)
        {
            //looks like "(default)" in RegEdit
            const string defaultRegistryValueName = @"";

            const string defaultBrowserRegistryKeyName = @"Software\Microsoft\Windows\Shell\Associations\UrlAssociations\https\UserChoice";
            const string defaultBrowserRegistryKeyValueName = @"ProgID";

            var processStartInfo = new ProcessStartInfo
            {
                FileName = authorizationCodeUrl,
                UseShellExecute = true
            };

            if (!logInWithIncognitoBrowser)
            {
                processStartInfo.FileName = authorizationCodeUrl;
                return processStartInfo;
            }

            var defaultBrowserProgId = GetValueOfRegistryKey(Registry.CurrentUser, defaultBrowserRegistryKeyName, defaultBrowserRegistryKeyValueName, "default browser progID");

            if (defaultBrowserProgId == String.Empty)
            {
                return processStartInfo;
            }

            string browserExecutionPathKey = defaultBrowserProgId + @"\shell\open\command";

            var browserPathKeyValue = GetValueOfRegistryKey(Registry.ClassesRoot, browserExecutionPathKey, defaultRegistryValueName, "browser path");

            if (browserPathKeyValue == String.Empty)
            {
                return processStartInfo;
            }

            ///allArguments contains the command-line arguments, including at index 0 the full path to the executable.
            var allArguments = GetBrowserArugments(browserPathKeyValue, authorizationCodeUrl);
            processStartInfo.FileName = allArguments[0];
            allArguments.RemoveAt(0);

            foreach (string argument in allArguments)
            {
                processStartInfo.ArgumentList.Add(argument);
            }

            return processStartInfo;
        }

        /// <summary>
        /// Note that the first item in the collection is the full path  to the file.
        /// </summary>
        private List<string> GetBrowserArugments(string defaultBrowserKeyValue, string url)
        {
            var parsedArguments = CommandLineParser.SplitCommandLineIntoArguments(defaultBrowserKeyValue, true);

            List<string> arguments = new List<string>(parsedArguments);

            const string windowsShellFirstArgumentPlaceholder = "%1";
            arguments.ReplaceForAllStringsInList(windowsShellFirstArgumentPlaceholder, url, 1);

            for (int argumentIndex = 1; argumentIndex < arguments.Count; argumentIndex++)
            {
                string argument = arguments[argumentIndex];
                if (argument.Contains(windowsShellFirstArgumentPlaceholder))
                {
                    arguments[argumentIndex] = argument.Replace(windowsShellFirstArgumentPlaceholder, url);
                }
            }
            
            var executableName = Path.GetFileNameWithoutExtension(defaultBrowserKeyValue).ToLower();

            switch (executableName)
            {
                case "firefox":
                    arguments.ReplaceForAllStringsInList("-url", "-private-window");
                    return arguments;
                case "brave":
                case "chrome":
                    arguments.Remove("--single-argument");
                    arguments.Add("-incognito");
                    return arguments;
                case "msedge":
                    //browser = DetectedBrowser.Edge;
                    arguments.Remove("--single-argument");
                    arguments.Add("-inprivate");
                    return arguments;
                case "citrixenterprisebrowser":
                    //I couldn't test this or find any command line arguments for it, but want to remember that this is its value just in case I decide to play with it later.
                    return arguments;
                case "opera":
                case "safari":
                    //These both use the "-private" argument, but as I don't have them installed, I can't test them or test what other arguments Windows wants to give them with the default file association stuff.
                default:
                    logger.LogWarning("Unable to identify default browser, unknown browser in the prog ID, we will not be authenticating with a private window.");
                    return arguments;
            }
        }

        private void ReplaceArgument(string v1, string v2)
        {
            throw new NotImplementedException();
        }

        //private string GetValueOfRegistryKey(RegistryKey topLevelKey, string keyPath, string valueName, string errorDescription)
        //{
        //}

        //private string GetValueOfRegistryKeyFromClassesRoot(string keyPath, string valueName, string errorDescription)
        //{
        //    using RegistryKey registryKey = Registry.ClassesRoot.OpenSubKey(keyPath);
        //    return GetValueOfRegistryKey(registryKey, valueName, errorDescription);
        //}

        //private string GetValueOfRegistryKeyFromCurrentUser(string keyPath, string valueName, string errorDescription)
        //{
        //    using RegistryKey registryKey = Registry.CurrentUser.OpenSubKey(keyPath);
        //    return GetValueOfRegistryKey(registryKey, valueName, errorDescription);
        //}


        private string GetValueOfRegistryKey(RegistryKey topLevelRegistryKey, string keyPath, string valueName, string errorDescription)
        {
            using RegistryKey? registryKey = topLevelRegistryKey.OpenSubKey(keyPath);
            if (registryKey == null)
            {
                logger.LogWarning($"Unable to identify default browser, no {errorDescription} registry KEY found, we will not be authenticating with a private window.");
                return String.Empty;
            }
            object deaultBrowserKeyDefaultValue = registryKey.GetValue(valueName);
            if (deaultBrowserKeyDefaultValue == null)
            {
                logger.LogWarning("Unable to identify default browser, no {errorDescription} registry VALUE, we will not be authenticating with a private window.");
                return String.Empty;
            }
            return deaultBrowserKeyDefaultValue.ToString();
        }

        #region Authorization event handlers
        private void AuthoriationProcess_Exited(object? sender, EventArgs e)
        {
            logger.LogTrace("Browser process closed before getting a response.");

            AuthorizationProcess_Canceled();
        }

        private void AuthorizationProcess_Canceled()
        {
            logger.LogInformation("Authorization of Twitch user was canceled.  Not continuing to try to listen to Twitch authorization.");

            if (OnAuthorizationProcessUserCanceled != null) OnAuthorizationProcessUserCanceled();
            DisposeAuthorizationBrowserProcess();
        }

        private void AuthorizationServer_Error(string errorCode)
        {
            logger.LogInformation($"Error authorizing Twitch user, not continuing to try to listen to Twitch authorization.  Error code: {errorCode}");
            if (OnAuthorizationProcessFailed != null) OnAuthorizationProcessFailed(errorCode);
            DisposeAuthorizationBrowserProcess();
        }

        private void AuthorizationServer_CodeSet(AuthorizationCode authorizationCode)
        {
            logger.LogInformation("Authorization of Twitch user completed, code set.  Not continuing to try to listen to Twitch authorization.");

            DisposeAuthorizationBrowserProcess();

            FinishAuthenticatingThenStartListening(authorizationCode);
        }
        #endregion Authorization event handlers


        private void FinishAuthenticatingThenStartListening(AuthorizationCode authorizationCode)
        {

            Task<AuthCodeResponse> getAccessTokenApiCallTask = TwitchApiConnection.Auth.GetAccessTokenFromCodeAsync(authorizationCode.Code, twitchSensitiveSettings.TwitchClientSecret, twitchSensitiveSettings.RedirectUri, twitchSensitiveSettings.TwitchClientId);


            getAccessTokenApiCallTask.ContinueWith(SetAccessTokenOnGetAccessTokenContinue);
        }
        private void SetAccessTokenOnGetAccessTokenContinue(Task<AuthCodeResponse> responseTask)
        {
            if (responseTask.Exception != null)
            {
                //TODO: Add a recursive exception logger D:<
                var exceptionType = responseTask.Exception.GetType().Name;
                logger.LogError($"An error occurred in the task trying to get the access token from the authorization code.  The {exceptionType} error was as follows: {responseTask.Exception.Message}\r\nStack trace:\r\n{responseTask.Exception.StackTrace}");
                if (OnAuthorizationProcessFailed != null) OnAuthorizationProcessFailed(exceptionType);
                return;
            }

            AccessToken accessToken = GetAccessToken(responseTask.Result);

            if (OnAuthorizationProcessSuccessful == null)
            {
                throw new InvalidOperationException($"{GetType().Name} requires {nameof(OnAuthorizationProcessSuccessful)} to be set in order to allow");
            }

            OnAuthorizationProcessSuccessful(accessToken);
        }


        private void DisposeAuthorizationBrowserProcess()
        {
            if (authorizationBrowserProcess != null)
            {
                authorizationBrowserProcess.Exited -= AuthoriationProcess_Exited;
                authorizationBrowserProcess.Dispose();
                authorizationBrowserProcess = null;
            }
        }

        public AccessToken GetAccessToken(AuthCodeResponse response)
        {
            return new AccessToken(
                 response.AccessToken,
                 response.RefreshToken,
                 response.ExpiresIn,
                 response.TokenType,
                 response.Scopes
                 );
        }

        public AccessToken GetAccessToken(RefreshResponse response)
        {
            //TODO: Find a better place for this to live!
            const string CODE_TOKEN_TYPE = "code";
            return new AccessToken(
                 response.AccessToken,
                 response.RefreshToken,
                 response.ExpiresIn,
                 CODE_TOKEN_TYPE,
                 response.Scopes
                 );
        }
    }
}
