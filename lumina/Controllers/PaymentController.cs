using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ServiceLayer.Payment;
using ServiceLayer.Subscription;
using System.Security.Claims;
using DataLayer.Models;

namespace lumina.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PaymentController : ControllerBase
    {
        private readonly IPayOSService _payOSService;
        private readonly ISubscriptionService _subscriptionService;
        private readonly LuminaSystemContext _context;
        private readonly ILogger<PaymentController> _logger;

        public PaymentController(
            IPayOSService payOSService,
            ISubscriptionService subscriptionService,
            LuminaSystemContext context,
            ILogger<PaymentController> logger)
        {
            _payOSService = payOSService;
            _subscriptionService = subscriptionService;
            _context = context;
            _logger = logger;
        }

        [HttpPost("create-link")]
        [Authorize]
        public async Task<IActionResult> CreatePaymentLink([FromBody] CreatePaymentRequest request)
        {
            try
            {
                var userIdClaim = User.FindFirst("UserId")?.Value ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
                {
                    return Unauthorized(new { message = "User not authenticated" });
                }

                var package = await _context.Packages.FindAsync(request.PackageId);
                if (package == null)
                {
                    return NotFound(new { message = "Package not found" });
                }

                if (package.Price.HasValue && request.Amount != package.Price.Value)
                {
                    return BadRequest(new { message = "Amount does not match package price" });
                }

                var link = await _payOSService.CreatePaymentLinkAsync(userId, request.PackageId, request.Amount);

                return Ok(new
                {
                    checkoutUrl = link.CheckoutUrl,
                    qrCode = link.QrCode,
                    orderCode = link.OrderCode
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating payment link");
                return StatusCode(500, new { message = "Error creating payment link", error = ex.Message });
            }
        }

        [HttpPost("webhook")]
        [AllowAnonymous]
        public async Task<IActionResult> HandleWebhook([FromBody] System.Text.Json.JsonElement body)
        {
            try
            {
                var bodyString = body.ToString();
                _logger.LogInformation("Received PayOS webhook: {WebhookBody}", bodyString);

                // PayOS webhook structure: { code, desc, data: {...}, signature }
                if (!body.TryGetProperty("data", out var data))
                {
                    _logger.LogWarning("PayOS Webhook received without 'data' property.");
                    return Ok(new { message = "Webhook received but no data property" });
                }

                var code = body.TryGetProperty("code", out var codeElement) ? codeElement.GetString() : null;
                if (code != "00")
                {
                    _logger.LogInformation("Webhook ignored because code is not '00'. Code: {Code}", code);
                    return Ok(new { message = "Webhook ignored - code not 00" });
                }

                if (!data.TryGetProperty("orderCode", out var orderCodeElement))
                {
                    _logger.LogWarning("Webhook 'data' property does not contain 'orderCode'.");
                    return Ok(new { message = "No orderCode in data" });
                }

                var orderCode = orderCodeElement.GetInt64();
                _logger.LogInformation("Processing payment for orderCode: {OrderCode}", orderCode);

                // Extract userId and packageId from orderCode
                var orderCodeStr = orderCode.ToString();
                if (orderCodeStr.Length >= 5)
                {
                    var userAndPackage = orderCodeStr.Substring(orderCodeStr.Length - 5);
                    var userIdStr = userAndPackage.Substring(0, 4);
                    var packageIdStr = userAndPackage.Substring(4, 1);

                    if (int.TryParse(userIdStr, out int userId) && int.TryParse(packageIdStr, out int packageId))
                    {
                        _logger.LogInformation("Extracted userId: {UserId}, packageId: {PackageId}", userId, packageId);

                        // Get amount from webhook data
                        var amount = data.TryGetProperty("amount", out var amountElement) ? amountElement.GetDecimal() : 0;

                        // Create payment record
                        var payment = new DataLayer.Models.Payment
                        {
                            UserId = userId,
                            PackageId = packageId,
                            Amount = amount,
                            PaymentGatewayTransactionId = orderCode.ToString(),
                            Status = "Success",
                            CreatedAt = DateTime.UtcNow
                        };

                        _context.Payments.Add(payment);
                        await _context.SaveChangesAsync();

                        // Activate subscription
                        await _subscriptionService.ActivateSubscriptionAsync(
                            userId,
                            packageId,
                            payment.PaymentId
                        );

                        _logger.LogInformation("Subscription activated successfully for user {UserId}", userId);
                        
                        return Ok(new { message = "Webhook processed successfully" });
                    }
                }

                _logger.LogWarning("Could not extract userId/packageId from orderCode: {OrderCode}", orderCode);
                return Ok(new { message = "Invalid orderCode format" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing webhook");
                return StatusCode(500, new { message = "Error processing webhook", error = ex.Message });
            }
        }
        

        [HttpGet("subscription-status")]
        [Authorize]
        public async Task<IActionResult> GetSubscriptionStatus()
        {
            try
            {
                var userIdClaim = User.FindFirst("UserId")?.Value ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
                {
                    return Unauthorized(new { message = "User not authenticated" });
                }

                var subscription = await _subscriptionService.GetActiveSubscriptionAsync(userId);

                if (subscription == null)
                {
                    return Ok(new
                    {
                        hasActiveSubscription = false,
                        subscriptionType = "FREE"
                    });
                }

                return Ok(new
                {
                    hasActiveSubscription = true,
                    subscriptionType = "PREMIUM",
                    startDate = subscription.StartTime,
                    endDate = subscription.EndTime,
                    packageId = subscription.PackageId
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting subscription status");
                return StatusCode(500, new { message = "Error getting subscription status", error = ex.Message });
            }
        }
    }
}
