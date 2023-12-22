using System.Net;

namespace StarmaidIntegrationComputer.StarmaidSettings
{
    internal class WebSocketSettings
    {
        public bool UseWebSockets { get; set; }
        public int Port { get; set; }
        public IPAddress IpAddress { get; set; }
    }
}
