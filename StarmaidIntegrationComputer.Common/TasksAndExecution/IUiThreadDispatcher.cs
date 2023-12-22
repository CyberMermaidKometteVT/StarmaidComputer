namespace StarmaidIntegrationComputer.Common.TasksAndExecution
{
    public interface IUiThreadDispatcher
    {
        void ExecuteOnUiThread(Action actionToExecute);
    }
}