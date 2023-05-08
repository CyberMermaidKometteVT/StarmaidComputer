using System;
using System.Text;
using System.Web;

using Microsoft.Extensions.Logging;

using StarmaidIntegrationComputer.Twitch.Authorization.Exceptions;
using StarmaidIntegrationComputer.Twitch.Authorization.Models;

namespace StarmaidIntegrationComputer.Twitch
{
    public class AuthResponseParsing
    {
        private ILogger<AuthResponseParsing>? Logger { get; set; }

        public AuthResponseParsing(ILogger<AuthResponseParsing> logger)
        {
            Logger = logger;
        }

        public AuthorizationCode? InterpretBrowserUri(Uri uri)
        {
            var queryParameters = HttpUtility.ParseQueryString(uri.Query);
            if (queryParameters == null)
            {
                Logger?.LogWarning($"Null query parameters parsed after trying {nameof(InterpretBrowserUri)}.  No authorization code set.  We shouldn't realy have gotten here though, this was a call to our redirect URI!");
                return null;
            }
            string? code = queryParameters["code"];
            string[]? scopes = queryParameters["scope"]?.Split(" ");
            string? state = queryParameters["state"];

            if (code == null || scopes == null || state == null)
            {
                HandleIssueInRedirect(queryParameters, code, scopes);
                return null;
            }

            return new AuthorizationCode(code, scopes, state);
        }

        private void HandleIssueInRedirect(System.Collections.Specialized.NameValueCollection queryParameters, string? code, string[]? scopes)
        {
            string? errorCode = queryParameters["error"];
            string? errorDescription = queryParameters["error_description"];
            string? state = queryParameters["state"];

            if (errorCode == null)
            {
                StringBuilder nullPartsDescriptionBuilder = new StringBuilder();
                if (code == null) nullPartsDescriptionBuilder.Append("code");
                if (scopes == null)
                {
                    if (nullPartsDescriptionBuilder.Length != 0) nullPartsDescriptionBuilder.Append(" and ");
                    nullPartsDescriptionBuilder.Append("scopes");
                }
                if (state == null)
                {
                    if (nullPartsDescriptionBuilder.Length != 0) nullPartsDescriptionBuilder.Append(" and ");
                    nullPartsDescriptionBuilder.Append("state");
                }


                string nullPartsDescription = nullPartsDescriptionBuilder.ToString();

                string message = $"Some of the parsed query parameters are null after trying {nameof(InterpretBrowserUri)}.  We also have no error.  No authorization code set.  We shouldn't realy have gotten here though, this was a call to our redirect URI!  The null parts are {nullPartsDescriptionBuilder}.";
                Logger?.LogWarning(message);

                throw new InvalidOperationException(message);
            }
            else //there's an error!
            {
                string error = $"Error trying to get authorization code.  Error code ({errorCode}): {errorDescription}";
                Logger?.LogError(error);

                throw new TwitchAuthorizationFailedException(errorCode, errorDescription);
            }
        }
    }
}
