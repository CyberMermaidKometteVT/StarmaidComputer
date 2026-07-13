using System;
using System.Net.Http;

namespace StarmaidIntegrationComputer.Thalassa.Tests
{
    internal sealed class ThrowingHttpClientFactory : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => throw new InvalidOperationException("No download should have been attempted.");
    }
}
