using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;

using StarmaidIntegrationComputer.Common.DataStructures.StarmaidState;
using StarmaidIntegrationComputer.StarmaidSettings;
using StarmaidIntegrationComputer.Thalassa.SpeechSynthesis;
using StarmaidIntegrationComputer.Twitch;

using TwitchLib.Api;
using TwitchLib.Api.Helix.Models.Channels.GetChannelFollowers;
using TwitchLib.Api.Helix.Models.Users.GetUserFollows;
using TwitchLib.Client;

namespace StarmaidIntegrationComputer.Commands.Twitch
{
    internal class SayLastFollwersCommand : TwitchCommandBase
    {
        private int count;
        List<string> followers { get; } = new List<string>();
        public SayLastFollwersCommand(ILogger<CommandBase> logger, SpeechComputer speechComputer, TwitchSensitiveSettings twitchSensitiveSettings, LiveAuthorizationInfo liveAuthorizationInfo, TwitchAPI twitchApi, int count)
            : base(logger, speechComputer, Enums.TwitchStateToValidate.Api, liveAuthorizationInfo, twitchSensitiveSettings, twitchApi)
        {
            this.count = count;
        }

        protected override async Task PerformCommandAsync()
        {
            try
            {
                await GetStateFromTargetChannel();

                string followerNames;
                if (followers.Count() > 1)
                {
                    followerNames = string.Join(",", followers.Take(followers.Count - 2));
                    followerNames = $"{followerNames}, and {followers.Last()}";
                }
                else
                {
                    followerNames = string.Join(",", followers);
                }

                speechComputer.Speak($"The following {followers.Count} most recent followers are: { followerNames }");
            }
            catch (InvalidOperationException)
            {
                speechComputer.Speak("Failed to execute command - no follower object was returned!");
            }
        }

        private async Task GetStateFromTargetChannel()
        {
            GetChannelFollowersResponse? getFollowersResponse = await twitchApi.Helix.Channels.GetChannelFollowersAsync(liveAuthorizationInfo.StreamerBroadcasterId, accessToken: liveAuthorizationInfo.AccessToken.Token);


            var testGetChannelInformationResponse = await twitchApi.Helix.Channels.GetChannelInformationAsync(liveAuthorizationInfo.StreamerBroadcasterId, accessToken: liveAuthorizationInfo.AccessToken.Token);

            if (getFollowersResponse == null)
            {
                throw new InvalidOperationException("Could not load follower list! No error reported, the return was just null.");
            }

            followers.AddRange(getFollowersResponse.Data.Select(follower => follower.UserName));
        }

    }
}
