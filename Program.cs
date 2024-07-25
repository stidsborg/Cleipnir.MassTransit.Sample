﻿using Cleipnir.Flows.AspNet;
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
        var bus = host.Services.GetRequiredService<IBus>();

        var order = new Order(
            OrderId: "MK-54321",
            CustomerId: Guid.NewGuid(),
            ProductIds: [Guid.NewGuid()],
            TotalPrice: 120.99M
        );

        await bus.Publish(order);
        
        var orderFlows = host.Services.GetRequiredService<OrderFlows>();
        var controlPanel = await orderFlows.ControlPanel(order.OrderId);
        while (controlPanel is null || controlPanel.Status != Status.Succeeded)
        {
            await Task.Delay(250);
            controlPanel = await orderFlows.ControlPanel(order.OrderId);
        }
        
        System.Console.WriteLine($"Order '{order.OrderId}' processing completed");
        await host.StopAsync();
    }
    
    private static IHostBuilder CreateHostBuilder(string[] args) =>
        Host.CreateDefaultBuilder(args)
            .ConfigureServices((_, services) =>
            {
                services.AddConsumerStubs();
                
                services.AddFlows(c => c
                    .UseInMemoryStore()
                    .RegisterFlowsAutomatically()
                    .IntegrateWithMassTransit()
                );
                
                services.AddMassTransit(x =>
                {
                    x.UsingInMemory((context,cfg) =>
                    {
                        cfg.ConfigureEndpoints(context);
                    });
                });
            });
}
