using Microsoft.Extensions.Logging.Abstractions;
using NServiceBus.Testing;
using PaymentSimulator.Events;
using PaymentSimulator.ServiceBus.Messages;
using PaymentSimulator.ServiceBus.Payment;

namespace PaymentSimulator.Tests;

public class PaymentSagaPolicyTests
{
    private static readonly Guid PaymentId = Guid.Parse("3fa85f64-5717-4562-b3fc-2c963f66afe8");

    private static PaymentSagaPolicy CreateSaga(PaymentStatus status = PaymentStatus.New) =>
        new(NullLogger<PaymentSagaPolicy>.Instance)
        {
            Data = new PaymentSagaPolicyData
            {
                PaymentId = PaymentId,
                Status = status,
                Amount = 199.99m,
                Currency = "PHP"
            }
        };

    private static PaymentReceivedEvent Received() => new()
    {
        PaymentId = PaymentId,
        Amount = 199.99m,
        Currency = "PHP",
        PaymentType = "CreditCard",
        CardBrand = "Visa",
        CardLast4 = "4242",
        CustomerId = "CUST-12345",
        Reference = "ORDER-67890"
    };

    [Fact]
    public async Task New_payment_asks_the_gateway_to_charge_the_card()
    {
        var saga = CreateSaga();
        var context = new TestableMessageHandlerContext();

        await saga.Handle(Received(), context);

        var charge = Assert.IsType<ChargeCard>(Assert.Single(context.SentMessages).Message);
        Assert.Equal(PaymentId, charge.PaymentId);
        Assert.Equal("4242", charge.CardLast4);
        Assert.Equal(PaymentStatus.Processing, saga.Data.Status);
    }

    [Theory]
    [InlineData(PaymentStatus.Processing)]
    [InlineData(PaymentStatus.Succeeded)]
    [InlineData(PaymentStatus.Failed)]
    public async Task Duplicate_request_is_skipped_whatever_the_status(PaymentStatus status)
    {
        var saga = CreateSaga(status);
        var context = new TestableMessageHandlerContext();

        await saga.Handle(Received(), context);

        Assert.Empty(context.SentMessages);
        Assert.Empty(context.PublishedMessages);
        Assert.Equal(status, saga.Data.Status);
    }

    [Fact]
    public async Task Successful_charge_publishes_success_and_waits_out_the_duplicate_window()
    {
        var saga = CreateSaga(PaymentStatus.Processing);
        var context = new TestableMessageHandlerContext();

        await saga.Handle(new ChargeCardResult { PaymentId = PaymentId, IsSuccess = true }, context);

        var published = Assert.IsType<PaymentSuccessEvent>(
            Assert.Single(context.PublishedMessages).Message);
        Assert.Equal(199.99m, published.Amount);
        Assert.Equal(PaymentStatus.Succeeded, saga.Data.Status);
        Assert.Single(context.TimeoutMessages);
        Assert.False(saga.Completed);
    }

    [Fact]
    public async Task Failed_charge_publishes_failure_with_the_gateway_reason()
    {
        var saga = CreateSaga(PaymentStatus.Processing);
        var context = new TestableMessageHandlerContext();

        await saga.Handle(
            new ChargeCardResult
            {
                PaymentId = PaymentId,
                IsSuccess = false,
                FailureReason = "Insufficient funds"
            },
            context);

        var published = Assert.IsType<PaymentFailedEvent>(
            Assert.Single(context.PublishedMessages).Message);
        Assert.Equal("Insufficient funds", published.FailureReason);
        Assert.Equal(PaymentStatus.Failed, saga.Data.Status);
        Assert.Single(context.TimeoutMessages);
    }

    [Fact]
    public async Task Late_duplicate_gateway_result_is_ignored()
    {
        var saga = CreateSaga(PaymentStatus.Succeeded);
        var context = new TestableMessageHandlerContext();

        await saga.Handle(new ChargeCardResult { PaymentId = PaymentId, IsSuccess = false }, context);

        Assert.Empty(context.PublishedMessages);
        Assert.Equal(PaymentStatus.Succeeded, saga.Data.Status);
    }

    [Fact]
    public async Task Saga_completes_when_the_duplicate_window_is_over()
    {
        var saga = CreateSaga(PaymentStatus.Failed);

        await saga.Timeout(new DuplicateWindowExpired(), new TestableMessageHandlerContext());

        Assert.True(saga.Completed);
    }
}
