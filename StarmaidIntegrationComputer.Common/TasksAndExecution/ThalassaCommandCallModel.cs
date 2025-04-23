namespace StarmaidIntegrationComputer.Common.TasksAndExecution
{
    public class ThalassaCommandCallModel
    {
        public string Name { get; set; }
        public List<ThalassaCommandCallArgument> Arguments { get; } = new List<ThalassaCommandCallArgument>();

        public ThalassaCommandCallModel(string name, List<ThalassaCommandCallArgument> arguments = null)
        {
            Name = name;
            Arguments = arguments ??= new List<ThalassaCommandCallArgument>();
        }
    }
}
