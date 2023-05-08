using System;

namespace StarmaidIntegrationComputer.Twitch.Authorization.Models
{
    public class AccessToken
    {
        public string Token { get; set; }
        public string RefreshToken { get; set; }

        private int expiresIn;

        /// <summary>
        /// Number of seconds, since the time the token was created, until it expires.
        /// </summary>
        public int ExpiresIn
        {
            get { return expiresIn; }
            set
            {
                expiresIn = value;
                CreatedAt = DateTime.Now;
                ExpiresAt = CreatedAt.AddSeconds(expiresIn);
            }
        }

        //TODO: Consider storing an enum or something later on?
        public string TokenType { get; set; }
        public string[] Scopes { get; set; }
        public DateTime CreatedAt { get; private set; }
        public DateTime ExpiresAt { get; private set; }

        public AccessToken(string token, string refreshToken, int expiresIn, string tokenType, string[] scopes)
        {
            Token = token;
            RefreshToken = refreshToken;
            ExpiresIn = expiresIn;
            TokenType = tokenType;
            Scopes = scopes;
        }
    }
}
