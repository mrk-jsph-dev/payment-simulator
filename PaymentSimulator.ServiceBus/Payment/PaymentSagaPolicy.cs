using Microsoft.Extensions.Logging;
using PaymentSimulator.Events;
using PaymentSimulator.ServiceBus.Messages;

namespace PaymentSimulator.ServiceBus.Payment;

/// <summary>
/// Orchestrates one payment, keyed by <see cref="PaymentReceivedEvent.PaymentId"/>.
/// The saga only makes decisions and keeps state; the call to the payment gateway
/// happens in <see cref="ChargeCardHandler"/>, which replies with the result.
/// </summary>
public class PaymentSagaPolicy : Saga<PaymentSagaPolicyData>,
    IAmStartedByMessages<PaymentReceivedEvent>,
    IHandleMessages<ChargeCardResult>,
    IHandleTimeouts<DuplicateWindowExpired>
{
    /// <summary>
    /// How long a finished payment is remembered, so a repeated request with the same
    /// PaymentId is skipped instead of charged again.
    /// </summary>
    public static readonly TimeSpan DuplicateWindow = TimeSpan.FromHours(24);

    private readonly ILogger<PaymentSagaPolicy> _logger;

    public PaymentSagaPolicy(ILogger<PaymentSagaPolicy> logger)
    {
        _logger = logger;
    }

    protected override void ConfigureHowToFindSaga(SagaPropertyMapper<PaymentSagaPolicyData> mapper)
    {
        mapper.MapSaga(sagaData => sagaData.PaymentId)
            .ToMessage<PaymentReceivedEvent>(message => message.PaymentId)
            .ToMessage<ChargeCardResult>(message => message.PaymentId);
    }

    public async Task Handle(PaymentReceivedEvent message, IMessageHandlerContext context)
    {
        if (Data.Status != PaymentStatus.New)
        {
            _logger.LogWarning(
                "[{PaymentId}] Duplicate request skipped (idempotent), payment is already {Status}",
                message.PaymentId,
                Data.Status);

            return;
        }

        Data.Status = PaymentStatus.Processing;
        Data.Amount = message.Amount;
        Data.Currency = message.Currency;

        _logger.LogInformation("[{PaymentId}] Payment received, charging card", message.PaymentId);

        await context.SendLocal(new ChargeCard
        {
            PaymentId = message.PaymentId,
            Amount = message.Amount,
            Currency = message.Currency,
            CardBrand = message.CardBrand,
            CardLast4 = message.CardLast4
        });
    }

    public async Task Handle(ChargeCardResult message, IMessageHandlerContext context)
    {
        if (Data.Status != PaymentStatus.Processing)
        {
            _logger.LogWarning(
                "[{PaymentId}] Gateway result ignored, payment is already {Status}",
                message.PaymentId,
                Data.Status);

            return;
        }

        if (message.IsSuccess)
        {
            Data.Status = PaymentStatus.Succeeded;
            _logger.LogInformation("[{PaymentId}] Payment succeeded", message.PaymentId);

            await context.Publish(new PaymentSuccessEvent
            {
                PaymentId = message.PaymentId,
                Amount = Data.Amount,
                Currency = Data.Currency
            });
        }
        else
        {
            Data.Status = PaymentStatus.Failed;
            _logger.LogWarning(
                "[{PaymentId}] Payment failed: {Reason}",
                message.PaymentId,
                message.FailureReason);

            await context.Publish(new PaymentFailedEvent
            {
                PaymentId = message.PaymentId,
                Amount = Data.Amount,
                Currency = Data.Currency,
                FailureReason = message.FailureReason ?? "Unknown failure"
            });
        }

        // Keep the saga for a while so duplicates are still detected, then end it.
        await RequestTimeout<DuplicateWindowExpired>(context, DuplicateWindow);
    }

    public Task Timeout(DuplicateWindowExpired state, IMessageHandlerContext context)
    {
        _logger.LogInformation(
            "[{PaymentId}] Duplicate window over, saga completed ({Status})",
            Data.PaymentId,
            Data.Status);

        MarkAsComplete();

        return Task.CompletedTask;
    }
}

public class DuplicateWindowExpired
{
}
