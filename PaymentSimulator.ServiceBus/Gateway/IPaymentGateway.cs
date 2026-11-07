using PaymentSimulator.ServiceBus.Messages;

namespace PaymentSimulator.ServiceBus.Gateway;

public interface IPaymentGateway
{
    Task<PaymentGatewayResult> ChargeAsync(ChargeCard charge, CancellationToken cancellationToken);
}

public sealed record PaymentGatewayResult(bool IsSuccess, string? FailureReason)
{
    public static PaymentGatewayResult Success() => new(true, null);

    public static PaymentGatewayResult Failure(string reason) => new(false, reason);
}
