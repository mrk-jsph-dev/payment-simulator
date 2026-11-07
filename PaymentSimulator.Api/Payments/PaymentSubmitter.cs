using PaymentSimulator.Api.Contracts;
using PaymentSimulator.Events;

namespace PaymentSimulator.Api.Payments;

/// <summary>Accepts a valid payment request and hands it to the message bus.</summary>
public class PaymentSubmitter
{
    private readonly IMessageSession _messageSession;
    private readonly ILogger<PaymentSubmitter> _logger;

    public PaymentSubmitter(IMessageSession messageSession, ILogger<PaymentSubmitter> logger)
    {
        _messageSession = messageSession;
        _logger = logger;
    }

    public async Task<PaymentSubmission> SubmitAsync(
        PaymentRequest request,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("[{PaymentId}] Payment saved to DATABASE (simulated)...", request.PaymentId);

        await _messageSession.Publish(new PaymentReceivedEvent
        {
            PaymentId = request.PaymentId,
            Amount = request.Amount,
            Currency = request.Currency.ToUpperInvariant(),
            PaymentType = request.PaymentMethod.Type,
            CardLast4 = request.PaymentMethod.CardLast4,
            CardBrand = request.PaymentMethod.CardBrand,
            CustomerId = request.Customer.CustomerId,
            Reference = request.Reference
        }, cancellationToken);

        return new PaymentSubmission(request.PaymentId, "Accepted");
    }
}

/// <summary>
/// The request was accepted for processing. The outcome arrives later as an event.
/// </summary>
public record PaymentSubmission(Guid PaymentId, string Status);
