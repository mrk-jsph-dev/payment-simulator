using PaymentSimulator.Api.Contracts;
using PaymentSimulator.Api.Payments;

namespace PaymentSimulator.Api.GraphQL;

[MutationType]
public partial class Mutation
{
    public async Task<PaymentSubmission> SubmitPayment(
        PaymentRequest paymentRequest,
        [Service] PaymentSubmitter submitter,
        CancellationToken cancellationToken)
    {
        var errors = PaymentRequestValidator.Validate(paymentRequest);
        if (errors.Count > 0)
        {
            throw new GraphQLException(errors
                .SelectMany(error => error.Value.Select(message => ErrorBuilder.New()
                    .SetMessage(message)
                    .SetCode("VALIDATION_ERROR")
                    .SetExtension("field", error.Key)
                    .Build()))
                .ToArray());
        }

        return await submitter.SubmitAsync(paymentRequest, cancellationToken);
    }
}
