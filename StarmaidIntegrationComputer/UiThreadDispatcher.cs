﻿using System;
using System.Windows;

using StarmaidIntegrationComputer.Common.TasksAndExecution;

namespace StarmaidIntegrationComputer
{
    public class UiThreadDispatcher : IUiThreadDispatcher
    {
        public void ExecuteOnUiThread(Action actionToExecute)
        {
            Application.Current.Dispatcher.Invoke(actionToExecute);
        }
    }
}
