using System;
using System.IO;
using System.Net;

using Microsoft.Extensions.Logging;

using StarmaidIntegrationComputer.StarmaidSettings;
using StarmaidIntegrationComputer.Twitch.Authorization.Exceptions;
using StarmaidIntegrationComputer.Twitch.Authorization.Models;

namespace StarmaidIntegrationComputer.Twitch.Authorization
{
    public class TwitchAuthResponseWebserver
    {
        //private readonly Settings settings;
        private readonly AuthResponseParsing authResponseParser;
        private readonly ILogger<TwitchAuthResponseWebserver> logger;

        public Action<AuthorizationCode>? OnAuthorizationCodeSet { get; set; }
        public Action<string>? OnError { get; set; }

        HttpListener httpListener = new HttpListener();


        public TwitchAuthResponseWebserver(Settings settings, AuthResponseParsing authResponseParser, ILogger<TwitchAuthResponseWebserver> logger)
        {
            //this.settings = settings;
            this.authResponseParser = authResponseParser;
            this.logger = logger;

            httpListener.Prefixes.Add(settings.RedirectUri);
        }
        public void StartListening(string oauthState)
        {
            httpListener.Start();
            httpListener.BeginGetContext(new AsyncCallback(HttpRequestReceived), oauthState);
        }

        private void HttpRequestReceived(IAsyncResult result)
        {
            logger.LogInformation($"{this.GetType().Name}: HTTP request received");


            if (result.AsyncState == null)
            {
                //TODO: Is this the best kind of exception for this?
                throw new NullReferenceException("Async state of the HTTP request recieved did not include the OAuth state used to protect against CSRF attacks.");
            }

            string state = result.AsyncState.ToString();

            HttpListenerContext context = httpListener.EndGetContext(result);

            HttpListenerRequest request = context.Request;

            AuthorizationCode? authorizationCode = null;
            try
            {
                authorizationCode = authResponseParser.InterpretBrowserUri(request.Url);

                if (authorizationCode?.State != state)
                {
                    throw new TwitchInvalidStateException(authorizationCode?.State, state);
                }

            }
            catch (InvalidOperationException)
            {
                if (OnError != null) OnError("Unknown error code");
            }
            catch (TwitchAuthorizationFailedException ex)
            {
                if (OnError != null) OnError(ex.ErrorCode);
            }
            catch (TwitchInvalidStateException ex)
            {
                if (OnError != null) OnError(ex.Message);
            }

            if (authorizationCode == null)
            {
                logger.LogInformation("No authorization token was successfully interpreted from the query string parameters.");
            }
            else
            {
                string scopesSubstring = authorizationCode.Scopes != null ? "[" + string.Join(", ", authorizationCode.Scopes) + "]" : "... No scopes found!";

                logger.LogInformation($"Authorization code loaded for scopes {scopesSubstring}");

                if (OnAuthorizationCodeSet == null)
                {
                    throw new InvalidOperationException($"{nameof(OnAuthorizationCodeSet)} event handler not set!");
                }
                OnAuthorizationCodeSet(authorizationCode);

                var response = context.Response;
                const string responseString = "<html><body><script type='text/javascript'>window.close();</script>The window can now be closed, if it isn't automatically.</body></html>";
                byte[] contentBytes = System.Text.Encoding.UTF8.GetBytes(responseString);

                response.ContentLength64 = contentBytes.Length;
                response.StatusCode = (int)HttpStatusCode.OK;
                response.StatusDescription = "Status OK";
                Stream output = response.OutputStream;
                output.Write(contentBytes, 0, contentBytes.Length);
                //output.Close();

                httpListener.Stop();
            }
        }
    }
}
