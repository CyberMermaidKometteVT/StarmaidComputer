using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;

using StarmaidIntegrationComputer.StarmaidSettings;
using StarmaidIntegrationComputer.Thalassa.SpeechSynthesis;
using StarmaidIntegrationComputer.Twitch;
using StarmaidIntegrationComputer.Twitch.ExternalApiClients.Pronouns;

using TwitchLib.Api;
using TwitchLib.Api.Helix.Models.Channels.GetChannelFollowers;

namespace StarmaidIntegrationComputer.Commands.Twitch
{
    internal class SayLastFollwersCommand : TwitchCommandBase
    {
        private int count;
        private readonly PronounLookupService pronounLookupService;
        List<string> followers { get; } = new List<string>();

        public SayLastFollwersCommand(ILogger<CommandBase> logger, SpeechComputer speechComputer, TwitchSensitiveSettings twitchSensitiveSettings, LiveAuthorizationInfo liveAuthorizationInfo, TwitchAPI twitchApi, PronounLookupService pronounLookupService, int count)
            : base(logger, speechComputer, Enums.TwitchStateToValidate.Api, liveAuthorizationInfo, twitchSensitiveSettings, twitchApi)
        {
            this.count = count;
            this.pronounLookupService = pronounLookupService;
        }

        protected override async Task PerformCommandAsync()
        {
            try
            {
                await GetStateFromTargetChannel();

                List<string> formattedFollowers = new List<string>();
                foreach (string followerName in followers)
                {
                    string pronounDisplay = await pronounLookupService.GetPronounLabelOrEmptyString(followerName);
                    formattedFollowers.Add($"{followerName}{pronounDisplay}");
                }

                string followerNames;
                if (formattedFollowers.Count > 1)
                {
                    string allButLast = string.Join(", ", formattedFollowers.Take(formattedFollowers.Count - 1));
                    followerNames = $"{allButLast}, and {formattedFollowers.Last()}";
                }
                else
                {
                    followerNames = string.Join(", ", formattedFollowers);
                }

                string output = $"The following {followers.Count} most recent followers are: {followerNames}";

                speechComputer.Speak(output);
                CompletedText = $"{this.GetType().Name} completed - {output}";
            }
            catch (InvalidOperationException)
            {
                speechComputer.Speak("Failed to execute command - no follower object was returned!");
            }
        }

        private async Task GetStateFromTargetChannel()
        {
            GetChannelFollowersResponse? getFollowersResponse = await twitchApi.Helix.Channels.GetChannelFollowersAsync(liveAuthorizationInfo.StreamerBroadcasterId, first: count, accessToken: liveAuthorizationInfo.AccessToken.Token);

            var testGetChannelInformationResponse = await twitchApi.Helix.Channels.GetChannelInformationAsync(liveAuthorizationInfo.StreamerBroadcasterId, accessToken: liveAuthorizationInfo.AccessToken.Token);

            if (getFollowersResponse == null)
            {
                throw new InvalidOperationException("Could not load follower list! No error reported, the return was just null.");
            }

            followers.AddRange(getFollowersResponse.Data.Select(follower => follower.UserName));
        }
    }
}
