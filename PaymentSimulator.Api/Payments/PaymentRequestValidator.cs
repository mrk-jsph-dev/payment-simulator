using PaymentSimulator.Api.Contracts;

namespace PaymentSimulator.Api.Payments;

/// <summary>
/// Business rules for a payment request, shared by the REST and GraphQL entry points.
/// Missing fields are already rejected by ASP.NET model validation and the GraphQL schema.
/// </summary>
public static class PaymentRequestValidator
{
    public static Dictionary<string, string[]> Validate(PaymentRequest request)
    {
        var errors = new Dictionary<string, string[]>();

        if (request.PaymentId == Guid.Empty)
        {
            errors[nameof(request.PaymentId)] = ["PaymentId must not be empty."];
        }

        if (request.Amount <= 0)
        {
            errors[nameof(request.Amount)] = ["Amount must be greater than zero."];
        }

        if (string.IsNullOrWhiteSpace(request.Currency)
            || request.Currency.Length != 3
            || !request.Currency.All(char.IsAsciiLetter))
        {
            errors[nameof(request.Currency)] = ["Currency must be a 3-letter code, such as PHP."];
        }

        var last4 = request.PaymentMethod?.CardLast4;
        if (last4 is null || last4.Length != 4 || !last4.All(char.IsAsciiDigit))
        {
            errors["PaymentMethod.CardLast4"] = ["CardLast4 must be exactly 4 digits."];
        }

        return errors;
    }
}
