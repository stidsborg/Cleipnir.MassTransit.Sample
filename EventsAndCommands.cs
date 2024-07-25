namespace Cleipnir.Flows.MassTransit.Sample;

public record OrderConfirmationEmailSent(string OrderId, Guid CustomerId);

public record ReserveFunds(string OrderId, decimal Amount, Guid TransactionId, Guid CustomerId);
public record FundsReserved(string OrderId);

public record ShipProducts(string OrderId, Guid CustomerId, IEnumerable<Guid> ProductIds);
public record CancelShipment(string OrderId);
public record ProductsShipped(string OrderId, string TrackAndTraceNumber);

public record ProductsShipmentFailed(string OrderId);

public record SendOrderConfirmationEmail(string OrderId, Guid CustomerId, string TrackAndTraceNumber);

public record CaptureFunds(string OrderId, Guid CustomerId, Guid TransactionId);
public record FundsCaptured(string OrderId);
public record FundsCaptureFailed(string OrderId);
public record CancelFundsReservation(string OrderId, Guid TransactionId);
public record FundsReservationFailed(string OrderId);