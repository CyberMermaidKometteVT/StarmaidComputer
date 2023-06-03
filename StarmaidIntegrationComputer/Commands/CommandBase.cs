using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Timers;

using Microsoft.Extensions.Logging;

using StarmaidIntegrationComputer.Common.TasksAndExecution;
using StarmaidIntegrationComputer.Thalassa.SpeechSynthesis;

namespace StarmaidIntegrationComputer.Commands
{
    public abstract class CommandBase
    {
        public int DelayInMilliseconds { get; set; } = 5 * 1000;

        public bool IsRunning { get; private set; } = false;
        public bool IsCompleted { get; private set; } = false;

        public List<Action<CommandBase>> OnCompleteActions { get; private set; } = new List<Action<CommandBase>>();


        private readonly Timer executionTimer;
        private readonly ILogger<CommandBase> logger;
        protected readonly SpeechComputer speechComputer;

        protected CommandBase(ILogger<CommandBase> logger, SpeechComputer speechComputer)
        {
            this.logger = logger;
            this.speechComputer = speechComputer;

            executionTimer = new Timer();
            executionTimer.Interval = DelayInMilliseconds;
            executionTimer.Elapsed += Timer_Elapsed;
        }

        public void Execute()
        {
            if (ValidateState())
            {
                executionTimer.Start();
                IsRunning = true;
            }
        }

        protected virtual bool ValidateState()
        {
            return true;
        }

        public void Abort()
        {
            if (executionTimer.Enabled)
            {
                executionTimer.Stop();
            }
            IsRunning = false;

        }

#warning this really shouldn't work, does it?
        private async void Timer_Elapsed(object? sender, ElapsedEventArgs e)
        {
            //We NEED to stop the timer immediately, or we might execute it multiple times, especially if we hit a breakpoint!
            executionTimer.Stop();
            executionTimer.Dispose();

            try
            {
                await PerformCommand();
                OnCompleteActions.Invoke(this);
            }
            catch (Exception ex)
            {
                string errorSummary = $"Failed to execute command: {ex.Message}.  See log for additional details.";
                logger.LogError(ex, errorSummary);
                speechComputer.Speak(errorSummary);
            }
            finally
            {
                IsRunning = false;
                IsCompleted = true;
            }
        }

        protected abstract Task PerformCommand();


    }
}
