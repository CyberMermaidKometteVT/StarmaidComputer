namespace StarmaidIntegrationComputer.Thalassa.WakeWordProcessor
{
    public interface IWakeWordProcessorFactory
    {
        WakeWordProcessorBase Processor { get; }
    }
}