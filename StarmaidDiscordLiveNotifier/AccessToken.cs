using System;
using System.Text.Json.Serialization;

namespace StarmaidDiscordLiveNotifier
{
    internal class AccessToken
    {
        [JsonPropertyName("access_token")]
        public string Token { get; set; }

        private int expiresIn;
        [JsonPropertyName("expires_in")]
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

        [JsonPropertyName("token_type")]
        public string TokenType { get; set; }
        public DateTime CreatedAt { get; private set; }
        public DateTime ExpiresAt { get; private set; }

    }
}
