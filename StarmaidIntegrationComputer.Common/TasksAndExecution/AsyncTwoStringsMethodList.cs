namespace StarmaidIntegrationComputer.Common.TasksAndExecution
{
    //Worried about race conditions and stuff in this, especially on the form; maybe I shouldn't make it async?
    public class AsyncTwoStringsMethodList : List<Func<string, string, Task>>
    {
        public Task Execute(string value1, string value2)
        {
            var runningTasks = this.Select(func => func(value1, value2)).ToArray();

            return Task.WhenAll(runningTasks);
        }
    }
}
