using Microsoft.Extensions.Logging;
using PaymentSimulator.Events;

namespace PaymentSimulator.ServiceBus.Notifications;

/// <summary>Reacts to the outcome of a payment. Emails are simulated with log lines.</summary>
public class CustomerNotificationHandler :
    IHandleMessages<PaymentSuccessEvent>,
    IHandleMessages<PaymentFailedEvent>
{
    private readonly ILogger<CustomerNotificationHandler> _logger;

    public CustomerNotificationHandler(ILogger<CustomerNotificationHandler> logger)
    {
        _logger = logger;
    }

    public Task Handle(PaymentSuccessEvent message, IMessageHandlerContext context)
    {
        _logger.LogInformation(
            "[{PaymentId}] Sending receipt for {Amount} {Currency} (simulated)",
            message.PaymentId,
            message.Amount,
            message.Currency);

        return Task.CompletedTask;
    }

    public Task Handle(PaymentFailedEvent message, IMessageHandlerContext context)
    {
        _logger.LogWarning(
            "[{PaymentId}] Notifying customer of failure: {FailureReason} (simulated)",
            message.PaymentId,
            message.FailureReason);

        return Task.CompletedTask;
    }
}
