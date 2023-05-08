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
using StarmaidIntegrationComputer.StarmaidSettings;

namespace StarmaidIntegrationComputer.Twitch.Authorization
{
    public class TwitchAuthorizationUserTokenFlowHelper
    {
        private readonly Settings settings;
        private readonly ILogger<TwitchAuthorizationUserTokenFlowHelper> logger;
        private TwitchAuthResponseWebserver webserver;

        private readonly List<AuthScopes> scopes;

        //This has a built-in call to the factory because I'm specifically contemplating releasing this class standalone and don't want to expect consumers to know how to inject it
        private TwitchAPI? TwitchApiConnection
        {
            get
            {
                if (twitchApiConnectionOnlyUseProperty == null) twitchApiConnectionOnlyUseProperty = TwitchApiFactory.Build(settings.TwitchClientId, settings.TwitchClientSecret, scopes);
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
        public bool ForceTwitchLoginPrompt { get; set; } = false;
        public Action<string> OnAuthorizationProcessFailed { get; set; }
        public Action OnAuthorizationProcessUserCanceled { get; set; }
        public Action<AccessToken> OnAuthorizationProcessSuccessful { get; set; }

        public TwitchAuthorizationUserTokenFlowHelper(Settings settings, ILogger<TwitchAuthorizationUserTokenFlowHelper> logger, List<AuthScopes> scopes, TwitchAuthResponseWebserver webserver, TwitchAPI? twitchApiConnection)
        {
            this.settings = settings;
            this.logger = logger;
            this.webserver = webserver;
            this.scopes = scopes;
            TwitchApiConnection = twitchApiConnection;


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
            var authorizationCodeUrl = new TwitchAPI().Auth.GetAuthorizationCodeUrl(settings.RedirectUri, scopes, ForceTwitchLoginPrompt, OauthState, settings.TwitchClientId);


            logger.LogInformation("Showing auth browser window.");
            webserver.StartListening(OauthState);
            authorizationBrowserProcess = Process.Start(new ProcessStartInfo { FileName = authorizationCodeUrl, UseShellExecute = true });
            authorizationBrowserProcess.Exited += AuthoriationProcess_Exited;
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

            Task<AuthCodeResponse> getAccessTokenApiCallTask = TwitchApiConnection.Auth.GetAccessTokenFromCodeAsync(authorizationCode.Code, settings.TwitchClientSecret, settings.RedirectUri, settings.TwitchClientId);


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
