using System.Collections.Generic;
using System.Windows.Controls;

using Microsoft.Extensions.Logging;

using Serilog;
using Serilog.Core;
using Serilog.Events;

using StarmaidIntegrationComputer.Common.Settings;

namespace StarmaidIntegrationComputer.Logging
{
    /// <summary>
    /// Owns the Serilog sink that writes to the main window's output pane, and the level switch that
    /// filters it. The sink can't be built in Startup, because the RichTextBox it writes to doesn't
    /// exist until the window has run InitializeComponent - so the window calls <see cref="AttachTo"/>
    /// once its controls exist, and otherwise deals only in the levels it gets from here, rather than
    /// composing loggers or reading settings itself.
    /// Public because it appears in the public constructor of IntegrationComputerMainWindow - C# requires
    /// that even within the same assembly.
    /// </summary>
    public class WindowLogSinkController
    {
        private readonly ILoggerFactory loggerFactory;
        private readonly LoggingLevelSwitch levelSwitch;
        private readonly string? unrecognizedConfiguredLevel;

        private bool hasAttached;

        public WindowLogSinkController(LoggingSettings loggingSettings, ILoggerFactory loggerFactory)
        {
            this.loggerFactory = loggerFactory;

            //An unset window level means "however much detail the file is getting" - the split only
            //matters once someone deliberately wants the pane quieter (or louder) than the file.
            MinimumLogLevelResolver.TryResolveMinimumLevel(loggingSettings.MinimumLogLevel, MinimumLogLevelResolver.DefaultMinimumLevel, out LogEventLevel fileMinimumLevel);

            bool windowLevelWasRecognized = MinimumLogLevelResolver.TryResolveMinimumLevel(loggingSettings.WindowMinimumLogLevel, fileMinimumLevel, out LogEventLevel windowMinimumLevel);

            if (!windowLevelWasRecognized)
            {
                unrecognizedConfiguredLevel = loggingSettings.WindowMinimumLogLevel;
            }

            levelSwitch = new LoggingLevelSwitch(windowMinimumLevel);
        }

        public IReadOnlyList<LogEventLevel> SelectableLevels => MinimumLogLevelResolver.SelectableLevels;

        /// <summary>
        /// Changing this takes effect on the next log message - Serilog reads the switch per event, so
        /// nothing needs rebuilding and the log file's own level is unaffected.
        /// </summary>
        public LogEventLevel CurrentLevel
        {
            get => levelSwitch.MinimumLevel;
            set => levelSwitch.MinimumLevel = value;
        }

        public void AttachTo(RichTextBox outputRichTextBox)
        {
            if (hasAttached)
            {
                return;
            }

            LoggerConfiguration windowLoggerConfiguration = new LoggerConfiguration();
            windowLoggerConfiguration.MinimumLevel.ControlledBy(levelSwitch);
            windowLoggerConfiguration.WriteTo.RichTextBox(outputRichTextBox);

            loggerFactory.AddSerilog(windowLoggerConfiguration.CreateLogger(), true);
            hasAttached = true;

            //Warned here rather than in the constructor, because until the sink exists there's nowhere
            //on screen for the warning to appear - and this is a warning about what's on screen.
            if (unrecognizedConfiguredLevel != null)
            {
                loggerFactory.CreateLogger<WindowLogSinkController>()
                    .LogWarning($"LoggingSettings.WindowMinimumLogLevel value '{unrecognizedConfiguredLevel}' isn't a recognized log level - falling back to {CurrentLevel}. Valid values are Verbose, Debug, Information, Warning, Error and Fatal.");
            }
        }
    }
}
