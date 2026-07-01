using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;

using Newtonsoft.Json;

using Polly;
using Polly.CircuitBreaker;

using StarmaidIntegrationComputer.Common.DataStructures.Pronouns;
using StarmaidIntegrationComputer.Twitch.ExternalApiClients.Pronouns.DataStructures.DTOs;

namespace StarmaidIntegrationComputer.Twitch.ExternalApiClients.Pronouns
{
    public class PronounsClient
    {
        private readonly ILogger<PronounsClient> logger;
        private readonly HttpClient httpClient = new HttpClient();

        // Trips when 50% of requests fail (min 3 requests in a 30s window), stays open for 60s.
        // Only 5xx responses count as failures; 404 (user has no pronouns) passes through.
        private readonly ResiliencePipeline<HttpResponseMessage> pipeline;

        public IReadOnlyDictionary<string, PronounInformation> PronounTable { get; private set; }

        public PronounsClient(ILogger<PronounsClient> logger)
        {
            this.logger = logger;
            pipeline = new ResiliencePipelineBuilder<HttpResponseMessage>()
                .AddCircuitBreaker(new CircuitBreakerStrategyOptions<HttpResponseMessage>
                {
                    FailureRatio = 0.5,
                    MinimumThroughput = 3,
                    SamplingDuration = TimeSpan.FromSeconds(30),
                    BreakDuration = TimeSpan.FromSeconds(60),
                    ShouldHandle = new PredicateBuilder<HttpResponseMessage>()
                        .HandleResult(r => (int)r.StatusCode >= 500)
                })
                .Build();
        }

        public async Task PopulatePronounsLookupTable()
        {
            HttpResponseMessage? response = await ExecuteRequest(
                "https://api.pronouns.alejo.io/v1/pronouns",
                nameof(PopulatePronounsLookupTable));
            if (response == null)
                return;

            var responseContent = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                logger.LogError("{Class}.{Method}: Failed to populate pronoun lookup table. Status: {StatusCode}, Body: {Body}",
                    nameof(PronounsClient), nameof(PopulatePronounsLookupTable), response.StatusCode, responseContent);
                return;
            }

            PronounTable = JsonConvert.DeserializeObject<Dictionary<string, PronounInformation>>(responseContent).ToFrozenDictionary();
        }

        public async Task<UserAndPronouns?> GetUserAndPronouns(string username)
        {
            if (PronounTable == null)
            {
                await PopulatePronounsLookupTable();
                if (PronounTable == null)
                {
                    logger.LogWarning("{Class}.{Method}: Pronoun lookup table unavailable; cannot look up pronouns for '{Username}'.",
                        nameof(PronounsClient), nameof(GetUserAndPronouns), username);
                    return null;
                }
            }

            string userUrl = $"https://api.pronouns.alejo.io/v1/users/{Uri.EscapeDataString(username)}";
            HttpResponseMessage? response = await ExecuteRequest(userUrl, nameof(GetUserAndPronouns));
            if (response == null)
                return null;

            if (response.StatusCode == HttpStatusCode.NotFound)
                return new UserAndPronouns(username, null, null);

            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning("{Class}.{Method}: Unexpected status {StatusCode} looking up pronouns for '{Username}'.",
                    nameof(PronounsClient), nameof(GetUserAndPronouns), response.StatusCode, username);
                return null;
            }

            var responseContent = await response.Content.ReadAsStringAsync();
            var userAndPronounIds = JsonConvert.DeserializeObject<UserAndPronounIds>(responseContent);

            PronounInformation? firstPronoun = null;
            if (userAndPronounIds.Pronoun_Id != null && !PronounTable.TryGetValue(userAndPronounIds.Pronoun_Id, out firstPronoun))
            {
                logger.LogWarning("{Class}.{Method}: Pronoun ID '{PronounId}' for user '{Username}' was not found in the pronoun lookup table.",
                    nameof(PronounsClient), nameof(GetUserAndPronouns), userAndPronounIds.Pronoun_Id, username);
            }

            PronounInformation? secondPronoun = null;
            if (userAndPronounIds.Alt_Pronoun_id != null && !PronounTable.TryGetValue(userAndPronounIds.Alt_Pronoun_id, out secondPronoun))
            {
                logger.LogWarning("{Class}.{Method}: Alt pronoun ID '{PronounId}' for user '{Username}' was not found in the pronoun lookup table.",
                    nameof(PronounsClient), nameof(GetUserAndPronouns), userAndPronounIds.Alt_Pronoun_id, username);
            }

            return new UserAndPronouns(username, firstPronoun, secondPronoun);
        }

        private async Task<HttpResponseMessage?> ExecuteRequest(string url, string callerName)
        {
            try
            {
                return await pipeline.ExecuteAsync(async ct => await httpClient.GetAsync(url, ct));
            }
            catch (BrokenCircuitException ex)
            {
                logger.LogWarning("{Class}.{Method}: Circuit breaker is open; skipping request. Can retry after {RetryAfter}.",
                    nameof(PronounsClient), callerName, ex.RetryAfter);
                return null;
            }
        }
    }
}
