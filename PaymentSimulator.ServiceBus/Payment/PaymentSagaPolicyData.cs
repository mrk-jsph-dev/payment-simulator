namespace PaymentSimulator.ServiceBus.Payment;

public class PaymentSagaPolicyData : ContainSagaData
{
    public Guid PaymentId { get; set; }
    public PaymentStatus Status { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = string.Empty;
}

public enum PaymentStatus
{
    New = 0,
    Processing = 1,
    Succeeded = 2,
    Failed = 3
}
