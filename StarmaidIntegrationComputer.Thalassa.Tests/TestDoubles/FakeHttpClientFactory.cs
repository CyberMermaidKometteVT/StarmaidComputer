using System.Net;
using System.Net.Http;

namespace StarmaidIntegrationComputer.Thalassa.Tests
{
    internal sealed class FakeHttpClientFactory : IHttpClientFactory
    {
        public string Content { get; set; } = string.Empty;
        public HttpStatusCode StatusCode { get; set; } = HttpStatusCode.OK;

        public HttpClient CreateClient(string name) => new(new FakeHttpMessageHandler { Content = Content, StatusCode = StatusCode });
    }
}
