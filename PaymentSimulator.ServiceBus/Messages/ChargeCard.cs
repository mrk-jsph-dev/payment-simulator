namespace PaymentSimulator.ServiceBus.Messages;

/// <summary>Internal command: ask the payment gateway to charge a card.</summary>
public class ChargeCard : ICommand
{
    public Guid PaymentId { get; init; }
    public decimal Amount { get; init; }
    public string Currency { get; init; } = string.Empty;
    public string CardBrand { get; init; } = string.Empty;
    public string CardLast4 { get; init; } = string.Empty;
}

/// <summary>Reply to <see cref="ChargeCard"/> with the gateway's answer.</summary>
public class ChargeCardResult : IMessage
{
    public Guid PaymentId { get; init; }
    public bool IsSuccess { get; init; }
    public string? FailureReason { get; init; }
}
