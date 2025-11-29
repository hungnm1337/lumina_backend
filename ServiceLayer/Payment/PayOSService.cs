using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace ServiceLayer.Payment
{
    public interface IPayOSService
    {
        Task<PaymentLinkResponse> CreatePaymentLinkAsync(int userId, int packageId, decimal amount);
        Task<PaymentVerificationResult> VerifyWebhookAsync(string signature, string payload);
        string GenerateOrderCode(int userId, int packageId);
    }

    public class PayOSService : IPayOSService
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiKey;
        private readonly string _checksumKey;
        private readonly string _clientId;
        private readonly string _returnUrl;
        private readonly string _cancelUrl;
        private readonly ILogger<PayOSService> _logger;

        public PayOSService(IConfiguration config, IHttpClientFactory httpClientFactory, ILogger<PayOSService> logger)
        {
            _httpClient = httpClientFactory.CreateClient();
            _apiKey = config["PayOS:ApiKey"] ?? throw new ArgumentNullException("PayOS:ApiKey not configured");
            _checksumKey = config["PayOS:ChecksumKey"] ?? throw new ArgumentNullException("PayOS:ChecksumKey not configured");
            _clientId = config["PayOS:ClientId"] ?? throw new ArgumentNullException("PayOS:ClientId not configured");
            _returnUrl = config["PayOS:ReturnUrl"] ?? "https://yourdomain.com/payment/success";
            _cancelUrl = config["PayOS:CancelUrl"] ?? "https://yourdomain.com/payment/cancel";
            _logger = logger;
        }

        public string GenerateOrderCode(int userId, int packageId)
        {
            var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            var shortTimestamp = timestamp % 1000000000;
            return $"{shortTimestamp}{userId:D4}{packageId}";
        }

        public async Task<PaymentLinkResponse> CreatePaymentLinkAsync(int userId, int packageId, decimal amount)
        {
            try
            {
                var orderCode = GenerateOrderCode(userId, packageId);
                var orderCodeLong = long.Parse(orderCode);

                var paymentData = new Dictionary<string, object>
                {
                    { "amount", (int)amount },
                    { "cancelUrl", _cancelUrl },
                    { "description", $"DH {orderCodeLong}" },
                    { "orderCode", orderCodeLong },
                    { "returnUrl", _returnUrl }
                };

                var signature = CreateSignature(paymentData);

                var payload = new
                {
                    orderCode = orderCodeLong,
                    amount = (int)amount,
                    description = $"DH {orderCodeLong}",
                    returnUrl = _returnUrl,
                    cancelUrl = _cancelUrl,
                    signature = signature
                };

                _logger.LogInformation("Sending payment request to PayOS: {Payload}", JsonSerializer.Serialize(payload));

                _httpClient.DefaultRequestHeaders.Clear();
                _httpClient.DefaultRequestHeaders.Add("x-client-id", _clientId);
                _httpClient.DefaultRequestHeaders.Add("x-api-key", _apiKey);

                var response = await _httpClient.PostAsJsonAsync(
                    "https://api-merchant.payos.vn/v2/payment-requests",
                    payload
                );

                var responseContent = await response.Content.ReadAsStringAsync();
                _logger.LogInformation("PayOS API Response: {StatusCode} - {Content}", response.StatusCode, responseContent);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError("PayOS API error: {StatusCode} - {Content}", response.StatusCode, responseContent);
                    throw new Exception($"PayOS API error: {response.StatusCode} - {responseContent}");
                }

                var result = JsonSerializer.Deserialize<PayOSApiResponse>(responseContent, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                _logger.LogInformation("PayOS Response parsed - Code: {Code}, Desc: {Desc}, Data: {Data}", 
                    result?.Code, result?.Desc, result?.Data != null ? "Present" : "Null");

                if (result?.Code != "00")
                {
                    _logger.LogError("PayOS returned error code. Code: {Code}, Desc: {Desc}, Full Response: {Response}", 
                        result?.Code, result?.Desc, responseContent);
                    throw new Exception($"PayOS error: {result?.Desc ?? "Unknown error"}");
                }

                var checkoutUrl = result?.Data?.CheckoutUrl;
                var qrCode = result?.Data?.QrCode;

                _logger.LogInformation("Extracted - CheckoutUrl: {CheckoutUrl}, QrCode length: {QrCodeLength}", 
                    checkoutUrl, qrCode?.Length ?? 0);

                if (result?.Data == null || string.IsNullOrEmpty(checkoutUrl))
                {
                    _logger.LogError("PayOS returned no checkout URL. Full Response: {Response}", responseContent);
                    throw new Exception($"PayOS did not return a valid checkout URL. Response: {responseContent}");
                }

                return new PaymentLinkResponse
                {
                    CheckoutUrl = checkoutUrl,
                    QrCode = qrCode ?? string.Empty,
                    OrderCode = orderCode
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating payment link for user {UserId}, package {PackageId}", userId, packageId);
                throw;
            }
        }

        public async Task<PaymentVerificationResult> VerifyWebhookAsync(string signature, string payload)
        {
            try
            {
                var computedSignature = GenerateSignature(payload);

                if (signature != computedSignature)
                {
                    _logger.LogWarning("Invalid webhook signature received");
                    return new PaymentVerificationResult
                    {
                        IsValid = false,
                        Error = "Invalid signature"
                    };
                }

                var data = JsonSerializer.Deserialize<WebhookPayload>(payload);

                if (data == null)
                {
                    return new PaymentVerificationResult
                    {
                        IsValid = false,
                        Error = "Invalid payload"
                    };
                }

                var orderCodeStr = data.OrderCode;
                if (orderCodeStr.Length >= 5)
                {
                    var userAndPackage = orderCodeStr.Substring(orderCodeStr.Length - 5);
                    var userIdStr = userAndPackage.Substring(0, 4);
                    var packageIdStr = userAndPackage.Substring(4, 1);
                    
                    if (int.TryParse(userIdStr, out int userId) && int.TryParse(packageIdStr, out int packageId))
                    {
                        return new PaymentVerificationResult
                        {
                            IsValid = true,
                            OrderCode = data.OrderCode,
                            Status = data.Status,
                            Amount = data.Amount,
                            UserId = userId,
                            PackageId = packageId
                        };
                    }
                }

                return new PaymentVerificationResult
                {
                    IsValid = false,
                    Error = "Invalid order code format"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error verifying webhook");
                return new PaymentVerificationResult
                {
                    IsValid = false,
                    Error = ex.Message
                };
            }
        }

        private string CreateSignature(Dictionary<string, object> data)
        {
            var sortedData = data.OrderBy(kvp => kvp.Key, StringComparer.Ordinal);
            var dataToSign = string.Join("&", sortedData.Select(kvp => $"{kvp.Key}={kvp.Value}"));
            
            _logger.LogInformation("Data to sign: {DataToSign}", dataToSign);

            using (var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(_checksumKey)))
            {
                var hashBytes = hmac.ComputeHash(Encoding.UTF8.GetBytes(dataToSign));
                return BitConverter.ToString(hashBytes).Replace("-", "").ToLower();
            }
        }

        private string GenerateSignature(object payload)
        {
            var json = JsonSerializer.Serialize(payload, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });
            return GenerateSignature(json);
        }

        private string GenerateSignature(string data)
        {
            using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(_checksumKey));
            var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(data));
            return Convert.ToHexString(hash).ToLower();
        }

        private class PayOSApiResponse
        {
            public string? Code { get; set; }
            public string? Desc { get; set; }
            public PayOSData? Data { get; set; }
            public string? Signature { get; set; }
        }

        private class PayOSData
        {
            public string? Bin { get; set; }
            public string? AccountNumber { get; set; }
            public string? AccountName { get; set; }
            public int? Amount { get; set; }
            public string? Description { get; set; }
            public long? OrderCode { get; set; }
            public string? PaymentLinkId { get; set; }
            public string? Status { get; set; }
            public string? CheckoutUrl { get; set; }
            public string? QrCode { get; set; }
        }
    }
}
