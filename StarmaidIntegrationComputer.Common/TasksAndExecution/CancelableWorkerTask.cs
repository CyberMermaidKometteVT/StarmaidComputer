namespace StarmaidIntegrationComputer.Common.TasksAndExecution
{
    public class CancellableWorkerTask<T> where T : class
    {
        public Task Task { get; set; }
        public bool IsWorkComplete { get; set; } = false;
        public T? WorkOutput { get; set; } = null;
        public CancellationTokenSource CancellationTokenSource { get; } = new CancellationTokenSource();

        public CancellableWorkerTask(Action action)
        {
            Task = Task.Run(action, CancellationTokenSource.Token);
        }
    }
}
