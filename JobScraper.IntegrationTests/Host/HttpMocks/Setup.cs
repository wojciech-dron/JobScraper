using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http;
using RichardSzalay.MockHttp;

#pragma warning disable EXTEXP0001

namespace JobScraper.IntegrationTests.Host.HttpMocks;

public static class Setup
{
    public static IServiceCollection MockAllHttpClients(this IServiceCollection services)
    {
        services.AddSingleton<MockHttpMessageHandler>();
        services.AddTransient<HttpMessageHandlerBuilder, MockHttpMessageHandlerBuilder>();

        return services;
    }

    private sealed class MockHttpMessageHandlerBuilder(IServiceProvider services, MockHttpMessageHandler handler)
        : HttpMessageHandlerBuilder
    {
        public override IList<DelegatingHandler> AdditionalHandlers { get; } = [];
        public override string? Name { get; set; }
        public override HttpMessageHandler PrimaryHandler { get; set; } = handler;
        public override IServiceProvider Services { get; } = services;

        public override HttpMessageHandler Build() => CreateHandlerPipeline(PrimaryHandler, AdditionalHandlers);
    }
}
