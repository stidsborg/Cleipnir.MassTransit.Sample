using Cleipnir.Flows.AspNet;
using Cleipnir.Flows.MassTransit.Sample.Other;
using Cleipnir.ResilientFunctions.Domain;
using MassTransit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Cleipnir.Flows.MassTransit.Sample;

internal static class Program
{
    public static async Task Main(string[] args)
    {
        var host = await CreateHostBuilder([]).StartAsync();
        var orderFlows = host.Services.GetRequiredService<OrderFlows>();
        
        var order = new Order(
            OrderId: "MK-54321",
            CustomerId: Guid.NewGuid(),
            ProductIds: [Guid.NewGuid()],
            TotalPrice: 120.99M
        );

        await orderFlows.Schedule(order.OrderId, order);
        
        var controlPanel = await orderFlows.ControlPanel(order.OrderId);
        while (controlPanel is null || controlPanel.Status != Status.Succeeded)
        {
            await Task.Delay(250);
            controlPanel = await orderFlows.ControlPanel(order.OrderId);
        }
        
        Console.WriteLine($"Order '{order.OrderId}' processing completed");
        await host.StopAsync();
    }
    
    private static IHostBuilder CreateHostBuilder(string[] args) =>
        Host.CreateDefaultBuilder(args)
            .ConfigureServices((_, services) =>
            {
                services.AddFlows(c => c
                    .UseInMemoryStore()
                    .RegisterFlowsAutomatically()
                    .WithOptions(new Options(messagesDefaultMaxWaitForCompletion: TimeSpan.FromMinutes(1)))
                );
                
                services.AddMassTransit(x =>
                {
                    x.AddConsumers(typeof(Program).Assembly);
                    x.UsingInMemory((context,cfg) =>
                    {
                        cfg.ConfigureEndpoints(context);
                    });
                });
            });
}
