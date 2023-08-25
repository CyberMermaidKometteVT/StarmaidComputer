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

        public static void Invoke<T>(this List<Action<T>> list, T argument)
        {
            foreach (Action<T> action in list)
            {
                action.Invoke(argument);
            }
        }
    }
}
