namespace Cleipnir.Flows.MassTransit.Sample;

public record Order(string OrderId, Guid CustomerId, IEnumerable<Guid> ProductIds, decimal TotalPrice);