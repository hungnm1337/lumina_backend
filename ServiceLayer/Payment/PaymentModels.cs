namespace ServiceLayer.Payment
{
    public class PaymentLinkResponse
    {
        public string CheckoutUrl { get; set; } = string.Empty;
        public string QrCode { get; set; } = string.Empty;
        public string OrderCode { get; set; } = string.Empty;
    }

    public class PaymentVerificationResult
    {
        public bool IsValid { get; set; }
        public string? OrderCode { get; set; }
        public string? Status { get; set; }
        public decimal Amount { get; set; }
        public string? Error { get; set; }
        public int UserId { get; set; }
        public int PackageId { get; set; }
    }

    public class WebhookPayload
    {
        public string OrderCode { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string? TransactionDateTime { get; set; }
        public string? Reference { get; set; }
    }

    public class CreatePaymentRequest
    {
        public int PackageId { get; set; }
        public decimal Amount { get; set; }
    }
}
