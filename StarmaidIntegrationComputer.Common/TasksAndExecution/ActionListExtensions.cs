namespace StarmaidIntegrationComputer.Common.TasksAndExecution
{
    public static class ActionListExtensions
    {
        public static void Invoke(this List<Action> list)
        {
            foreach (Action action in list)
            {
                action.Invoke();
            }
        }
    }
}
