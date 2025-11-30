using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using ServiceLayer.Payment;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Lumina.Tests.ServiceTests
{
    public class VerifyWebhookAsyncUnitTest
    {
        private readonly Mock<ILogger<PayOSService>> _mockLogger;
        private readonly Mock<IHttpClientFactory> _mockHttpClientFactory;

        public VerifyWebhookAsyncUnitTest()
        {
            _mockLogger = new Mock<ILogger<PayOSService>>();
            _mockHttpClientFactory = new Mock<IHttpClientFactory>();
        }

        #region Invalid Signature Tests

        [Fact]
        public async Task VerifyWebhookAsync_WithInvalidSignature_ShouldReturnInvalidResult()
        {
            // Arrange
            var configuration = CreateConfiguration("test-checksum-key");
            var service = new PayOSService(configuration, _mockHttpClientFactory.Object, _mockLogger.Object);

            var payload = JsonSerializer.Serialize(new
            {
                OrderCode ="12345678900011",
                Status ="PAID",
                Amount =100000
            });

            var invalidSignature = "invalid_signature_value";

            // Act
            var result = await service.VerifyWebhookAsync(invalidSignature, payload);

            // Assert
            result.Should().NotBeNull();
            result.IsValid.Should().BeFalse();
            result.Error.Should().Be("Invalid signature");
        }

        [Fact]
        public async Task VerifyWebhookAsync_WithEmptySignature_ShouldReturnInvalidResult()
        {
            // Arrange
            var configuration = CreateConfiguration("test-checksum-key");
            var service = new PayOSService(configuration, _mockHttpClientFactory.Object, _mockLogger.Object);

            var payload = JsonSerializer.Serialize(new
            {
                OrderCode ="12345678900011",
                Status ="PAID",
                Amount =100000
            });

            // Act
            var result = await service.VerifyWebhookAsync("", payload);

            // Assert
            result.Should().NotBeNull();
            result.IsValid.Should().BeFalse();
            result.Error.Should().Be("Invalid signature");
        }

        #endregion

        #region Invalid Payload Tests

        [Fact]
        public async Task VerifyWebhookAsync_WithInvalidJsonPayload_ShouldReturnInvalidResult()
        {
            // Arrange
            var checksumKey = "test-checksum-key";
            var configuration = CreateConfiguration(checksumKey);
            var service = new PayOSService(configuration, _mockHttpClientFactory.Object, _mockLogger.Object);

            var invalidPayload = "{ invalid json }";
            var signature = GenerateSignature(invalidPayload, checksumKey);

            // Act
            var result = await service.VerifyWebhookAsync(signature, invalidPayload);

            // Assert
            result.Should().NotBeNull();
            result.IsValid.Should().BeFalse();
            result.Error.Should().NotBeNullOrEmpty();
        }

        [Fact]
        public async Task VerifyWebhookAsync_WithNullPayloadData_ShouldReturnInvalidResult()
        {
            // Arrange
            var checksumKey = "test-checksum-key";
            var configuration = CreateConfiguration(checksumKey);
            var service = new PayOSService(configuration, _mockHttpClientFactory.Object, _mockLogger.Object);

            var payload = "null";
            var signature = GenerateSignature(payload, checksumKey);

            // Act
            var result = await service.VerifyWebhookAsync(signature, payload);

            // Assert
            result.Should().NotBeNull();
            result.IsValid.Should().BeFalse();
            result.Error.Should().Be("Invalid payload");
        }

        #endregion

        #region Valid Webhook Tests

        [Fact]
        public async Task VerifyWebhookAsync_WithValidWebhook_ShouldReturnValidResult()
        {
            // Arrange
            var checksumKey = "test-checksum-key";
            var configuration = CreateConfiguration(checksumKey);
            var service = new PayOSService(configuration, _mockHttpClientFactory.Object, _mockLogger.Object);

            // OrderCode format: {timestamp}{userId:D4}{packageId}
            // Example: "12345678900051" -> last 5 chars = "00051" -> userId=0005, packageId=1
            var webhookData = new
            {
                OrderCode = "12345678900051",
                Status = "PAID",
                Amount = 100000m
            };

            var payload = JsonSerializer.Serialize(webhookData);
            var signature = GenerateSignature(payload, checksumKey);

            // Act
            var result = await service.VerifyWebhookAsync(signature, payload);

            // Assert
            result.Should().NotBeNull();
            result.IsValid.Should().BeTrue();
            result.OrderCode.Should().Be("12345678900051");
            result.Status.Should().Be("PAID");
            result.Amount.Should().Be(100000m);
            result.UserId.Should().Be(5);
            result.PackageId.Should().Be(1);
            result.Error.Should().BeNullOrEmpty();
        }

        [Fact]
        public async Task VerifyWebhookAsync_WithDifferentUserIdAndPackageId_ShouldExtractCorrectly()
        {
            // Arrange
            var checksumKey = "test-checksum-key";
            var configuration = CreateConfiguration(checksumKey);
            var service = new PayOSService(configuration, _mockHttpClientFactory.Object, _mockLogger.Object);

            // OrderCode: last 5 chars = "12343" -> userId=1234, packageId=3
            var webhookData = new
            {
                OrderCode = "98765432112343",
                Status = "PAID",
                Amount = 250000m
            };

            var payload = JsonSerializer.Serialize(webhookData);
            var signature = GenerateSignature(payload, checksumKey);

            // Act
            var result = await service.VerifyWebhookAsync(signature, payload);

            // Assert
            result.Should().NotBeNull();
            result.IsValid.Should().BeTrue();
            result.UserId.Should().Be(1234);
            result.PackageId.Should().Be(3);
        }

        [Fact]
        public async Task VerifyWebhookAsync_WithZeroPaddedUserId_ShouldParseCorrectly()
        {
            // Arrange
            var checksumKey = "test-checksum-key";
            var configuration = CreateConfiguration(checksumKey);
            var service = new PayOSService(configuration, _mockHttpClientFactory.Object, _mockLogger.Object);

            // OrderCode: last 5 chars = "00012" -> userId=0001, packageId=2
            var webhookData = new
            {
                OrderCode = "55555555500012",
                Status = "PAID",
                Amount = 150000m
            };

            var payload = JsonSerializer.Serialize(webhookData);
            var signature = GenerateSignature(payload, checksumKey);

            // Act
            var result = await service.VerifyWebhookAsync(signature, payload);

            // Assert
            result.Should().NotBeNull();
            result.IsValid.Should().BeTrue();
            result.UserId.Should().Be(1);
            result.PackageId.Should().Be(2);
        }

        #endregion

        #region Invalid OrderCode Format Tests

        [Fact]
        public async Task VerifyWebhookAsync_WithShortOrderCode_ShouldReturnInvalidResult()
        {
            // Arrange
            var checksumKey = "test-checksum-key";
            var configuration = CreateConfiguration(checksumKey);
            var service = new PayOSService(configuration, _mockHttpClientFactory.Object, _mockLogger.Object);

            // OrderCode too short (less than 5 characters)
            var webhookData = new
            {
                OrderCode ="1234",
                Status ="PAID",
                Amount =100000m
            };

            var payload = JsonSerializer.Serialize(webhookData, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });
            var signature = GenerateSignature(payload, checksumKey);

            // Act
            var result = await service.VerifyWebhookAsync(signature, payload);

            // Assert
            result.Should().NotBeNull();
            result.IsValid.Should().BeFalse();
            result.Error.Should().Be("Invalid order code format");
        }

        [Fact]
        public async Task VerifyWebhookAsync_WithNonNumericUserId_ShouldReturnInvalidResult()
        {
            // Arrange
            var checksumKey = "test-checksum-key";
            var configuration = CreateConfiguration(checksumKey);
            var service = new PayOSService(configuration, _mockHttpClientFactory.Object, _mockLogger.Object);

            // OrderCode: last 5 chars = "ABCD1" -> userId=ABCD (non-numeric)
            var webhookData = new
            {
                OrderCode ="1234567890ABCD1",
                Status ="PAID",
                Amount =100000m
            };

            var payload = JsonSerializer.Serialize(webhookData, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });
            var signature = GenerateSignature(payload, checksumKey);

            // Act
            var result = await service.VerifyWebhookAsync(signature, payload);

            // Assert
            result.Should().NotBeNull();
            result.IsValid.Should().BeFalse();
            result.Error.Should().Be("Invalid order code format");
        }

        [Fact]
        public async Task VerifyWebhookAsync_WithNonNumericPackageId_ShouldReturnInvalidResult()
        {
            // Arrange
            var checksumKey = "test-checksum-key";
            var configuration = CreateConfiguration(checksumKey);
            var service = new PayOSService(configuration, _mockHttpClientFactory.Object, _mockLogger.Object);

            // OrderCode: last 5 chars = "0001X" -> packageId=X (non-numeric)
            var webhookData = new
            {
                OrderCode ="12345678900001X",
                Status ="PAID",
                Amount =100000m
            };

            var payload = JsonSerializer.Serialize(webhookData, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });
            var signature = GenerateSignature(payload, checksumKey);

            // Act
            var result = await service.VerifyWebhookAsync(signature, payload);

            // Assert
            result.Should().NotBeNull();
            result.IsValid.Should().BeFalse();
            result.Error.Should().Be("Invalid order code format");
        }

        #endregion

        #region Helper Methods

        private IConfiguration CreateConfiguration(string checksumKey)
        {
            var inMemorySettings = new Dictionary<string, string>
            {
                {"PayOS:ApiKey", "test-api-key"},
                {"PayOS:ChecksumKey", checksumKey},
                {"PayOS:ClientId", "test-client-id"},
                {"PayOS:ReturnUrl", "https://test.com/success"},
                {"PayOS:CancelUrl", "https://test.com/cancel"}
            };

            return new ConfigurationBuilder()
                .AddInMemoryCollection(inMemorySettings!)
                .Build();
        }

        private string GenerateSignature(string data, string checksumKey)
        {
            using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(checksumKey));
            var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(data));
            return Convert.ToHexString(hash).ToLower();
        }

        #endregion
    }
}
