namespace StarmaidIntegrationComputer.Common.TasksAndExecution
{
    public interface IUiThreadDispatchInvoker
    {
        void ExecuteOnUiThread(Action actionToExecute);
    }
}