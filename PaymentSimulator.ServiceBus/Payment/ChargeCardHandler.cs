using PaymentSimulator.ServiceBus.Gateway;
using PaymentSimulator.ServiceBus.Messages;

namespace PaymentSimulator.ServiceBus.Payment;

public class ChargeCardHandler : IHandleMessages<ChargeCard>
{
    private readonly IPaymentGateway _gateway;

    public ChargeCardHandler(IPaymentGateway gateway)
    {
        _gateway = gateway;
    }

    public async Task Handle(ChargeCard message, IMessageHandlerContext context)
    {
        var result = await _gateway.ChargeAsync(message, context.CancellationToken);

        await context.Reply(new ChargeCardResult
        {
            PaymentId = message.PaymentId,
            IsSuccess = result.IsSuccess,
            FailureReason = result.FailureReason
        });
    }
}
