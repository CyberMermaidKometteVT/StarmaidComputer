using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace StarmaidIntegrationComputer.Thalassa.Tests
{
    internal sealed class FakeHttpMessageHandler : HttpMessageHandler
    {
        public string Content { get; set; } = string.Empty;
        public HttpStatusCode StatusCode { get; set; } = HttpStatusCode.OK;

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            HttpResponseMessage response = new(StatusCode)
            {
                Content = new StringContent(Content)
            };

            return Task.FromResult(response);
        }
    }
}
