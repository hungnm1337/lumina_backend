using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
using ServiceLayer.Payment;
using System.Net;
using System.Text;
using System.Text.Json;

namespace Lumina.Tests.ServiceTests
{
    public class CreatePaymentLinkAsyncUnitTest : IDisposable
    {
        private readonly Mock<IHttpClientFactory> _mockHttpClientFactory;
        private readonly Mock<HttpMessageHandler> _mockHttpMessageHandler;
        private readonly Mock<ILogger<PayOSService>> _mockLogger;
        private readonly IConfiguration _configuration;
        private readonly PayOSService _payOSService;

        public CreatePaymentLinkAsyncUnitTest()
        {
            // Setup Configuration using InMemoryCollection
            var inMemorySettings = new Dictionary<string, string>
            {
                {"PayOS:ApiKey", "test-api-key"},
                {"PayOS:ChecksumKey", "test-checksum-key"},
                {"PayOS:ClientId", "test-client-id"},
                {"PayOS:ReturnUrl", "https://test.com/success"},
                {"PayOS:CancelUrl", "https://test.com/cancel"}
            };

            _configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(inMemorySettings!)
                .Build();

            // Setup HttpClient mocking
            _mockHttpMessageHandler = new Mock<HttpMessageHandler>();
            var httpClient = new HttpClient(_mockHttpMessageHandler.Object);

            _mockHttpClientFactory = new Mock<IHttpClientFactory>();
            _mockHttpClientFactory.Setup(x => x.CreateClient(It.IsAny<string>()))
                .Returns(httpClient);

            // Setup Logger
            _mockLogger = new Mock<ILogger<PayOSService>>();

            // Initialize service
            _payOSService = new PayOSService(_configuration, _mockHttpClientFactory.Object, _mockLogger.Object);
        }

        public void Dispose()
        {
            _mockHttpMessageHandler.Object.Dispose();
        }

        #region Success Scenarios

        [Fact]
        public async Task CreatePaymentLinkAsync_WithValidInputs_ShouldReturnPaymentLinkResponse()
        {
            // Arrange
            int userId = 1;
            int packageId = 2;
            decimal amount = 100000m;

            var apiResponse = new
            {
                code = "00",
                desc = "Success",
                data = new
                {
                    checkoutUrl = "https://payment.payos.vn/checkout/123",
                    qrCode = "data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAAAAUA",
                    orderCode = 123456789,
                    paymentLinkId = "payment-link-123",
                    status = "PENDING"
                },
                signature = "test-signature"
            };

            var responseContent = JsonSerializer.Serialize(apiResponse);
            var httpResponse = new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(responseContent, Encoding.UTF8, "application/json")
            };

            _mockHttpMessageHandler.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(httpResponse);

            // Act
            var result = await _payOSService.CreatePaymentLinkAsync(userId, packageId, amount);

            // Assert
            result.Should().NotBeNull();
            result.CheckoutUrl.Should().Be("https://payment.payos.vn/checkout/123");
            result.QrCode.Should().Be("data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAAAAUA");
            result.OrderCode.Should().NotBeNullOrEmpty();
        }

        [Fact]
        public async Task CreatePaymentLinkAsync_WithValidInputsAndNoQrCode_ShouldReturnEmptyQrCode()
        {
            // Arrange
            var apiResponse = new
            {
                code = "00",
                desc = "Success",
                data = new
                {
                    checkoutUrl = "https://payment.payos.vn/checkout/123",
                    qrCode = (string?)null,
                    orderCode = 123456789
                }
            };

            var responseContent = JsonSerializer.Serialize(apiResponse);
            var httpResponse = new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(responseContent, Encoding.UTF8, "application/json")
            };

            _mockHttpMessageHandler.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(httpResponse);

            // Act
            var result = await _payOSService.CreatePaymentLinkAsync(1, 2, 100000m);

            // Assert
            result.QrCode.Should().Be(string.Empty);
        }

        #endregion

        #region Error Scenarios

        [Fact]
        public async Task CreatePaymentLinkAsync_WhenApiReturnsNonSuccessStatusCode_ShouldThrowException()
        {
            // Arrange
            var httpResponse = new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.BadRequest,
                Content = new StringContent("Bad Request", Encoding.UTF8, "application/json")
            };

            _mockHttpMessageHandler.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(httpResponse);

            // Act
            Func<Task> act = async () => await _payOSService.CreatePaymentLinkAsync(1, 2, 100000m);

            // Assert
            await act.Should().ThrowAsync<Exception>()
                .WithMessage("PayOS API error:*");
        }

        [Fact]
        public async Task CreatePaymentLinkAsync_WhenApiReturnsErrorCode_ShouldThrowException()
        {
            // Arrange
            var apiResponse = new
            {
                code = "01",
                desc = "Invalid signature",
                data = (object?)null
            };

            var responseContent = JsonSerializer.Serialize(apiResponse);
            var httpResponse = new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(responseContent, Encoding.UTF8, "application/json")
            };

            _mockHttpMessageHandler.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(httpResponse);

            // Act
            Func<Task> act = async () => await _payOSService.CreatePaymentLinkAsync(1, 2, 100000m);

            // Assert
            await act.Should().ThrowAsync<Exception>()
                .WithMessage("PayOS error: Invalid signature");
        }

        [Fact]
        public async Task CreatePaymentLinkAsync_WhenApiReturnsNullOrEmptyCheckoutUrl_ShouldThrowException()
        {
            // Arrange - Test c? null data và empty checkoutUrl
            var apiResponse = new
            {
                code = "00",
                desc = "Success",
                data = new
                {
                    checkoutUrl = "",
                    qrCode = "test"
                }
            };

            var responseContent = JsonSerializer.Serialize(apiResponse);
            var httpResponse = new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(responseContent, Encoding.UTF8, "application/json")
            };

            _mockHttpMessageHandler.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(httpResponse);

            // Act
            Func<Task> act = async () => await _payOSService.CreatePaymentLinkAsync(1, 2, 100000m);

            // Assert
            await act.Should().ThrowAsync<Exception>()
                .WithMessage("PayOS did not return a valid checkout URL.*");
        }

        [Fact]
        public async Task CreatePaymentLinkAsync_WhenHttpClientThrowsException_ShouldThrowAndLogError()
        {
            // Arrange
            _mockHttpMessageHandler.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ThrowsAsync(new HttpRequestException("Network error"));

            // Act
            Func<Task> act = async () => await _payOSService.CreatePaymentLinkAsync(1, 2, 100000m);

            // Assert
            await act.Should().ThrowAsync<HttpRequestException>()
                .WithMessage("Network error");

            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => true),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        #endregion
    }
}
