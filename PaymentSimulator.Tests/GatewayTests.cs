using Microsoft.Extensions.Logging.Abstractions;
using NServiceBus.Testing;
using PaymentSimulator.ServiceBus.Gateway;
using PaymentSimulator.ServiceBus.Messages;
using PaymentSimulator.ServiceBus.Payment;

namespace PaymentSimulator.Tests;

public class GatewayTests
{
    private static readonly SimulatedPaymentGateway Gateway =
        new(NullLogger<SimulatedPaymentGateway>.Instance, TimeSpan.Zero);

    private static ChargeCard Charge(string last4) => new()
    {
        PaymentId = Guid.NewGuid(),
        Amount = 100m,
        Currency = "PHP",
        CardBrand = "Visa",
        CardLast4 = last4
    };

    [Theory]
    [InlineData("4242", true, null)]
    [InlineData(SimulatedPaymentGateway.DeclinedCard, false, "Card declined")]
    [InlineData(SimulatedPaymentGateway.InsufficientFundsCard, false, "Insufficient funds")]
    public async Task Outcome_depends_on_the_test_card(string last4, bool success, string? reason)
    {
        var result = await Gateway.ChargeAsync(Charge(last4), CancellationToken.None);

        Assert.Equal(success, result.IsSuccess);
        Assert.Equal(reason, result.FailureReason);
    }

    [Fact]
    public async Task Charge_handler_replies_with_the_gateway_result()
    {
        var handler = new ChargeCardHandler(Gateway);
        var context = new TestableMessageHandlerContext();
        var charge = Charge(SimulatedPaymentGateway.DeclinedCard);

        await handler.Handle(charge, context);

        var reply = Assert.IsType<ChargeCardResult>(Assert.Single(context.RepliedMessages).Message);
        Assert.Equal(charge.PaymentId, reply.PaymentId);
        Assert.False(reply.IsSuccess);
        Assert.Equal("Card declined", reply.FailureReason);
    }
}
