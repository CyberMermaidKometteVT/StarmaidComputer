using System;
using System.Collections.Generic;

using Microsoft.Extensions.Logging;

using Serilog.Events;

namespace StarmaidIntegrationComputer.Logging
{
    /// <summary>
    /// Turns the log level strings in LoggingSettings into the level types the logging stack needs.
    /// </summary>
    internal static class MinimumLogLevelResolver
    {
        public const LogEventLevel DefaultMinimumLevel = LogEventLevel.Information;

        /// <summary>
        /// The levels offered in the main window's log level dropdown, most detail first.
        /// </summary>
        public static IReadOnlyList<LogEventLevel> SelectableLevels { get; } = new[]
        {
            LogEventLevel.Verbose,
            LogEventLevel.Debug,
            LogEventLevel.Information,
            LogEventLevel.Warning,
            LogEventLevel.Error,
            LogEventLevel.Fatal
        };

        /// <summary>
        /// Returns false only when a value was supplied and wasn't recognized, so the caller can warn
        /// about a typo'd level. A blank value is a normal "just use the fallback" and returns true.
        /// </summary>
        public static bool TryResolveMinimumLevel(string? configuredLevel, LogEventLevel fallbackLevel, out LogEventLevel minimumLevel)
        {
            minimumLevel = fallbackLevel;

            if (string.IsNullOrWhiteSpace(configuredLevel))
            {
                return true;
            }

            if (Enum.TryParse(configuredLevel, ignoreCase: true, out LogEventLevel parsedLevel))
            {
                minimumLevel = parsedLevel;
                return true;
            }

            return false;
        }

        public static LogLevel ToMicrosoftLogLevel(LogEventLevel serilogLevel)
        {
            switch (serilogLevel)
            {
                case LogEventLevel.Verbose:
                    return LogLevel.Trace;
                case LogEventLevel.Debug:
                    return LogLevel.Debug;
                case LogEventLevel.Information:
                    return LogLevel.Information;
                case LogEventLevel.Warning:
                    return LogLevel.Warning;
                case LogEventLevel.Error:
                    return LogLevel.Error;
                case LogEventLevel.Fatal:
                    return LogLevel.Critical;
                default:
                    return LogLevel.Information;
            }
        }
    }
}
