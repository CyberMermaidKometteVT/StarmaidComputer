namespace StarmaidIntegrationComputer.Common.TasksAndExecution
{
    public class ThalassaCommandCallArgument
    {
        public string Name { get; }
        public string SerializedValue { get; }

        public ThalassaCommandCallArgument(string name, string serializedValue)
        {
            Name = name;
            SerializedValue = serializedValue;
        }
    }
}
