namespace StarmaidIntegrationComputer.Twitch.Authorization.Models
{
    /// <summary>
    /// This is the token that's an intermediary between the Twitch login, and our later ACCESS token.
    /// This is ONLY used (so far by this code, no idea if it ever does more) to GET AN ACCESS TOKEN.
    /// </summary>
    public class AuthorizationCode
    {
        public string Code { get; private set; }
        public string[] Scopes { get; private set; }
        public string State { get; private set; }

        public AuthorizationCode(string authorizationCode, string[] scopes, string state)
        {
            Code = authorizationCode;
            Scopes = scopes;
            State = state;
        }
    }
}
