using Newtonsoft.Json;

namespace StarmaidIntegrationComputer.Twitch.ExternalApiClients.Pronouns.DataStructures.DTOs
{
    public class UserAndPronounIds
    {
        public string Channel_Id { get; }
        public string Channel_Login { get; }
        public string Pronoun_Id { get; }
        public string Alt_Pronoun_id { get; }

        [JsonConstructor]
        public UserAndPronounIds(string channel_Id, string channel_Login, string pronoun_Id, string alt_Pronoun_id)
        {
            Channel_Id = channel_Id;
            Channel_Login = channel_Login;
            Pronoun_Id = pronoun_Id;
            Alt_Pronoun_id = alt_Pronoun_id;
        }
    }
}
