using MassTransit.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Cleipnir.Flows.MassTransit.Sample.Other;

public static class Setup
{
    public static void AddConsumerStubs(this IServiceCollection services)
    {
        services.RegisterConsumer<EmailServiceStub>();
        services.RegisterConsumer<LogisticsServiceStub>();
        services.RegisterConsumer<PaymentProviderStub>();
    }
}