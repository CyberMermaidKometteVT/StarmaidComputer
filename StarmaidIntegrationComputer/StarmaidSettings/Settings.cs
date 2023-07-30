using System.Collections.Generic;

namespace StarmaidIntegrationComputer.StarmaidSettings
{
    //TODO: Consider breaking this out into interfaces to fullfil the Liskov Substitution OOP design principle.
    public class Settings
    {
        public string DiscordWebhookUrl { get; set; }
        //There is a cleaner way to do the bearer token!  See https://github.com/OkGoDoIt/OpenAI-API-dotnet#authentication
        public string JailbreakMessage { get; set; }
        public List<Role> Roles { get; set; }
    }
}
