using Microsoft.AspNetCore.Mvc;
using PaymentSimulator.Api.Contracts;
using PaymentSimulator.Api.Payments;

namespace PaymentSimulator.API.Controllers;

[Route("api/payment")]
[ApiController]
public class PaymentController : ControllerBase
{
    private readonly PaymentSubmitter _submitter;

    public PaymentController(PaymentSubmitter submitter)
    {
        _submitter = submitter;
    }

    [HttpPost("submit")]
    [ProducesResponseType<PaymentSubmission>(StatusCodes.Status202Accepted)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> SubmitPaymentAsync(
        [FromBody] PaymentRequest paymentRequest,
        CancellationToken cancellationToken)
    {
        var errors = PaymentRequestValidator.Validate(paymentRequest);
        if (errors.Count > 0)
        {
            return ValidationProblem(new ValidationProblemDetails(errors));
        }

        var submission = await _submitter.SubmitAsync(paymentRequest, cancellationToken);

        return Accepted(submission);
    }
}
