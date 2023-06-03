namespace StarmaidIntegrationComputer.Common.TasksAndExecution
{
    //Worried about race conditions and stuff in this, especially on the form; maybe I shouldn't make it async?
    public class AsyncStringMethodList : List<Func<string, Task>>
    {
        public Task Execute(string message)
        {
            var runningTasks = this.Select(func => func(message)).ToArray();

            return Task.WhenAll(runningTasks);
        }
    }
}
