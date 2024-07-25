using Cleipnir.ResilientFunctions.Domain;
using Cleipnir.ResilientFunctions.Reactive.Extensions;
using MassTransit;

namespace Cleipnir.Flows.MassTransit.Sample;

public class OrderFlow(IBus bus) : Flow<Order>,
    ISubscription<ConsumeContext<FundsReserved>>,
    ISubscription<ConsumeContext<ProductsShipped>>,
    ISubscription<ConsumeContext<FundsCaptured>>,
    ISubscription<ConsumeContext<OrderConfirmationEmailSent>>
{
    #region Routing
    public static RoutingInfo Correlate(Order order) => Route.To(order.OrderId);
    public static RoutingInfo Correlate(ConsumeContext<FundsReserved> msg) => Route.To(msg.Message.OrderId);
    public static RoutingInfo Correlate(ConsumeContext<ProductsShipped> msg) => Route.To(msg.Message.OrderId);
    public static RoutingInfo Correlate(ConsumeContext<FundsCaptured> msg) => Route.To(msg.Message.OrderId);
    public static RoutingInfo Correlate(ConsumeContext<OrderConfirmationEmailSent> msg) => Route.To(msg.Message.OrderId);
    #endregion
    
    public override async Task Run(Order order)
    {
        var transactionId = await Effect.Capture("TransactionId", Guid.NewGuid);

        await ReserveFunds(order, transactionId);
        var success = await Messages
            .OfTypes<FundsReserved, FundsReservationFailed>()
            .Select(e => e.AsObject() is FundsReserved)
            .First();

        if (!success)
            throw new Exception("Funds reservation failed");

        await ShipProducts(order);
        var trackAndTraceNumber = await Messages
            .OfTypes<ProductsShipped, ProductsShipmentFailed>()
            .Select(e => 
                e.Match(s => s.TrackAndTraceNumber, s => default(string))
            )
            .First();

        if (trackAndTraceNumber is null)
        {
            await CancelReservation(order, transactionId);
            throw new Exception("Shipment failed");
        }
        
        await CaptureFunds(order, transactionId);
        success = await Messages
            .OfTypes<FundsCaptured, FundsCaptureFailed>()
            .Select(e => e.AsObject() is FundsCaptured)
            .First();
        if (!success)
        {
            await CancelShipment(order);
            await CancelReservation(order, transactionId);
            throw new Exception("Funds capture failed");
        }

        await SendOrderConfirmationEmail(order, trackAndTraceNumber);
        await Messages.FirstOfType<OrderConfirmationEmailSent>();
    }

    #region MessagePublishers

    private Task ReserveFunds(Order order, Guid transactionId)
        => Effect.Capture(
            "ReserveFunds",
            () => bus.Publish(new ReserveFunds(order.OrderId, order.TotalPrice, transactionId, order.CustomerId))
        );

    private Task CancelReservation(Order order, Guid transactionId)
        => Effect.Capture(
            "CancelReservation",
            () => bus.Publish(new CancelFundsReservation(order.OrderId, transactionId))
        );

    private Task ShipProducts(Order order)
        => Effect.Capture(
            "ShipProducts",
            () => bus.Publish(new ShipProducts(order.OrderId, order.CustomerId, order.ProductIds))
        );
    
    private Task CancelShipment(Order order)
        => Effect.Capture(
            "CancelShipment",
            () => bus.Publish(new ShipProducts(order.OrderId, order.CustomerId, order.ProductIds))
        );
    
    private Task CaptureFunds(Order order, Guid transactionId)
        => Effect.Capture(
            "CaptureFunds",
            () => bus.Publish(new CaptureFunds(order.OrderId, order.CustomerId, transactionId))
        );

    private Task SendOrderConfirmationEmail(Order order, string trackAndTraceNumber)
        => Effect.Capture(
            "SendOrderConfirmationEmail",
            () => bus.Publish(new SendOrderConfirmationEmail(order.OrderId, order.CustomerId, trackAndTraceNumber))
        );

    #endregion
}
