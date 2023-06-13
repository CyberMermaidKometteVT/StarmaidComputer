using System;

namespace StarmaidIntegrationComputer.Helpers
{
    internal static class TmiSentTsHelpers
    {
        /// <summary>
        /// Parses a <paramref name="tmiSentTs"/>, which exists on many events received from Twitch.
        /// It's a Unix timestamp. 
        /// </summary>
        /// <param name="tmiSentTs">A Unix timestamp, present in many Twitch event payloads.</param>
        /// <returns>The UTC <see cref="DateTime"/> representing that moment.</returns>
        internal static DateTime ParseOrNow(string tmiSentTs)
        {
            long tmiSentTsLong;
            bool couldParseTimestamp = long.TryParse(tmiSentTs, out tmiSentTsLong);
            DateTime result = DateTime.Now;
            if (couldParseTimestamp)
            {
                result = DateTimeOffset.FromUnixTimeSeconds(tmiSentTsLong).UtcDateTime;
            }

            return result;

        }

    }
}
