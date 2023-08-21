namespace StarmaidIntegrationComputer.Common.TasksAndExecution
{
    //Worried about race conditions and stuff in this, especially on the form; maybe I shouldn't make it async?
    public class AsyncStringMethodList : AsyncMethodList<string>
    {
    }
}
