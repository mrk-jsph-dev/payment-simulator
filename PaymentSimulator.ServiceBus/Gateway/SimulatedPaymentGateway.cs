using Microsoft.Extensions.Logging;
using PaymentSimulator.ServiceBus.Messages;

namespace PaymentSimulator.ServiceBus.Gateway;

/// <summary>
/// A stand-in for a card gateway. The outcome depends on the card's last four digits,
/// in the style of Stripe's test cards, so every result can be reproduced on purpose:
/// <list type="bullet">
///   <item><c>0002</c>: declined</item>
///   <item><c>9995</c>: insufficient funds</item>
///   <item>anything else: success</item>
/// </list>
/// </summary>
public class SimulatedPaymentGateway : IPaymentGateway
{
    public const string DeclinedCard = "0002";
    public const string InsufficientFundsCard = "9995";

    private readonly ILogger<SimulatedPaymentGateway> _logger;
    private readonly TimeSpan _latency;

    public SimulatedPaymentGateway(ILogger<SimulatedPaymentGateway> logger)
        : this(logger, TimeSpan.FromSeconds(1))
    {
    }

    public SimulatedPaymentGateway(ILogger<SimulatedPaymentGateway> logger, TimeSpan latency)
    {
        _logger = logger;
        _latency = latency;
    }

    public async Task<PaymentGatewayResult> ChargeAsync(
        ChargeCard charge,
        CancellationToken cancellationToken)
    {
        if (_latency > TimeSpan.Zero)
        {
            // Simulated network delay
            await Task.Delay(_latency, cancellationToken);
        }

        var result = charge.CardLast4 switch
        {
            DeclinedCard => PaymentGatewayResult.Failure("Card declined"),
            InsufficientFundsCard => PaymentGatewayResult.Failure("Insufficient funds"),
            _ => PaymentGatewayResult.Success()
        };

        if (result.IsSuccess)
        {
            _logger.LogInformation(
                "[{PaymentId}] Gateway (simulated) SUCCESS for {Amount} {Currency} ({CardBrand} ****{Last4})",
                charge.PaymentId,
                charge.Amount,
                charge.Currency,
                charge.CardBrand,
                charge.CardLast4);
        }
        else
        {
            _logger.LogWarning(
                "[{PaymentId}] Gateway (simulated) FAILED: {Reason}",
                charge.PaymentId,
                result.FailureReason);
        }

        return result;
    }
}
