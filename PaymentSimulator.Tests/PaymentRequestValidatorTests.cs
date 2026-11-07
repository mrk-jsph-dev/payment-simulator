using PaymentSimulator.Api.Contracts;
using PaymentSimulator.Api.Payments;

namespace PaymentSimulator.Tests;

public class PaymentRequestValidatorTests
{
    private static PaymentRequest Valid() => new()
    {
        PaymentId = Guid.NewGuid(),
        Amount = 199.99m,
        Currency = "PHP",
        PaymentMethod = new PaymentMethodDto
        {
            Type = "CreditCard",
            CardBrand = "Visa",
            CardLast4 = "4242"
        },
        Customer = new CustomerDto { CustomerId = "CUST-12345", Email = "jane.doe@example.com" },
        Reference = "ORDER-67890"
    };

    [Fact]
    public void Valid_request_has_no_errors()
    {
        Assert.Empty(PaymentRequestValidator.Validate(Valid()));
    }

    [Fact]
    public void Empty_payment_id_is_rejected()
    {
        var request = Valid();
        request.PaymentId = Guid.Empty;

        Assert.Contains("PaymentId", PaymentRequestValidator.Validate(request).Keys);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public void Amount_must_be_positive(decimal amount)
    {
        var request = Valid();
        request.Amount = amount;

        Assert.Contains("Amount", PaymentRequestValidator.Validate(request).Keys);
    }

    [Theory]
    [InlineData("")]
    [InlineData("PH")]
    [InlineData("PESO")]
    [InlineData("P1P")]
    public void Currency_must_be_a_three_letter_code(string currency)
    {
        var request = Valid();
        request.Currency = currency;

        Assert.Contains("Currency", PaymentRequestValidator.Validate(request).Keys);
    }

    [Theory]
    [InlineData("424")]
    [InlineData("42a2")]
    public void Card_last4_must_be_four_digits(string last4)
    {
        var request = Valid();
        request.PaymentMethod.CardLast4 = last4;

        Assert.Contains("PaymentMethod.CardLast4", PaymentRequestValidator.Validate(request).Keys);
    }
}
