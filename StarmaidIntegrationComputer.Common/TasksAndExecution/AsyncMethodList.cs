namespace StarmaidIntegrationComputer.Common.TasksAndExecution
{
    public class AsyncMethodList<T> : List<Func<T, Task>>
    {
        public Task Execute(T argument)
        {
            var runningTasks = this.Select(func => func(argument)).ToArray();

            return Task.WhenAll(runningTasks);
        }
    }
}
