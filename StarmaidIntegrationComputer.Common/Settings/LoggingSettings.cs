namespace StarmaidIntegrationComputer.Common.Settings
{
    /// <summary>
    /// Controls how much detail reaches each of the two log destinations. Diagnostics that fire many
    /// times a second - notably the wake word processor's per-buffer confidence scores - log at Debug,
    /// so they stay out of the way until one of these levels is lowered to ask for them.
    /// </summary>
    public class LoggingSettings
    {
        /// <summary>
        /// Minimum level written to the log file. One of Serilog's level names, from most detail to
        /// least: Verbose, Debug, Information, Warning, Error, Fatal. Blank falls back to Information;
        /// an unrecognized value also falls back to Information, but logs a warning at startup rather
        /// than failing silently.
        /// </summary>
        public string? MinimumLogLevel { get; set; }

        /// <summary>
        /// Minimum level shown in the main window's output pane, which can be changed while running
        /// via the dropdown above that pane - this setting is only the starting value. Kept separate
        /// from <see cref="MinimumLogLevel"/> so the file can be recording Debug detail without the
        /// on-screen pane being flooded by it. Blank falls back to <see cref="MinimumLogLevel"/>.
        /// </summary>
        public string? WindowMinimumLogLevel { get; set; }
    }
}
