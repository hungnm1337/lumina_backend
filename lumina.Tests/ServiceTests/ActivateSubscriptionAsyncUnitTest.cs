using DataLayer.Models;
using FluentAssertions;
using Lumina.Tests.Helpers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using ServiceLayer.Subscription;

namespace Lumina.Tests.ServiceTests
{
    public class ActivateSubscriptionAsyncUnitTest : IDisposable
    {
        private readonly LuminaSystemContext _context;
        private readonly Mock<ILogger<SubscriptionService>> _mockLogger;
        private readonly SubscriptionService _service;

        public ActivateSubscriptionAsyncUnitTest()
        {
            _context = InMemoryDbContextHelper.CreateContext();
            _mockLogger = new Mock<ILogger<SubscriptionService>>();
            _service = new SubscriptionService(_context, _mockLogger.Object);
        }

        public void Dispose()
        {
            _context.Database.EnsureDeleted();
            _context.Dispose();
        }

        #region Success Scenarios

        [Fact]
        public async Task ActivateSubscriptionAsync_WithValidPackage_ShouldCreateActiveSubscription()
        {
            // Arrange
            int userId = 1;
            int packageId = 1;
            int paymentId = 1;

            var package = new Package
            {
                PackageId = packageId,
                PackageName = "Premium",
                Price = 100000,
                DurationInDays = 60
            };

            _context.Packages.Add(package);
            await _context.SaveChangesAsync();

            // Act
            var result = await _service.ActivateSubscriptionAsync(userId, packageId, paymentId);

            // Assert
            result.Should().NotBeNull();
            result.UserId.Should().Be(userId);
            result.PackageId.Should().Be(packageId);
            result.PaymentId.Should().Be(paymentId);
            result.Status.Should().Be("Active");
            result.StartTime.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
            result.EndTime.Should().BeCloseTo(DateTime.UtcNow.AddDays(60), TimeSpan.FromSeconds(5));

            var savedSubscription = await _context.Subscriptions.FirstOrDefaultAsync();
            savedSubscription.Should().NotBeNull();
            savedSubscription!.Status.Should().Be("Active");

            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains($"Subscription activated for user {userId}")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        [Fact]
        public async Task ActivateSubscriptionAsync_WhenDurationInDaysIsNull_ShouldUseDefault30Days()
        {
            // Arrange
            int userId = 1;
            int packageId = 1;
            int paymentId = 1;

            var package = new Package
            {
                PackageId = packageId,
                PackageName = "Basic",
                Price = 50000,
                DurationInDays = null // Test null case
            };

            _context.Packages.Add(package);
            await _context.SaveChangesAsync();

            // Act
            var result = await _service.ActivateSubscriptionAsync(userId, packageId, paymentId);

            // Assert
            result.Should().NotBeNull();
            result.EndTime.Should().BeCloseTo(DateTime.UtcNow.AddDays(30), TimeSpan.FromSeconds(5));
        }

        #endregion

        #region Error Scenarios

        [Fact]
        public async Task ActivateSubscriptionAsync_WhenPackageNotFound_ShouldThrowException()
        {
            // Arrange
            int userId = 1;
            int packageId = 999; // Non-existent package
            int paymentId = 1;

            // Act
            Func<Task> act = async () => await _service.ActivateSubscriptionAsync(userId, packageId, paymentId);

            // Assert
            await act.Should().ThrowAsync<Exception>()
                .WithMessage("Package 999 not found");

            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains($"Error activating subscription for user {userId}")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        #endregion
    }
}
