using DataLayer.Models;
using FluentAssertions;
using Lumina.Tests.Helpers;
using Microsoft.Extensions.Logging;
using Moq;
using ServiceLayer.Subscription;

namespace Lumina.Tests.ServiceTests
{
    public class GetActiveSubscriptionAsyncUnitTest : IDisposable
    {
        private readonly LuminaSystemContext _context;
        private readonly Mock<ILogger<SubscriptionService>> _mockLogger;
        private readonly SubscriptionService _service;

        public GetActiveSubscriptionAsyncUnitTest()
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
        public async Task GetActiveSubscriptionAsync_WhenActiveSubscriptionExists_ShouldReturnSubscription()
        {
            // Arrange
            int userId = 1;
            var activeSubscription = new DataLayer.Models.Subscription
            {
                SubscriptionId = 1,
                UserId = userId,
                PackageId = 1,
                PaymentId = 1,
                Status = "Active",
                StartTime = DateTime.UtcNow.AddDays(-10),
                EndTime = DateTime.UtcNow.AddDays(20)
            };

            _context.Subscriptions.Add(activeSubscription);
            await _context.SaveChangesAsync();

            // Act
            var result = await _service.GetActiveSubscriptionAsync(userId);

            // Assert
            result.Should().NotBeNull();
            result!.SubscriptionId.Should().Be(1);
            result.UserId.Should().Be(userId);
            result.Status.Should().Be("Active");
            result.EndTime.Should().BeAfter(DateTime.UtcNow);
        }

        [Fact]
        public async Task GetActiveSubscriptionAsync_WhenMultipleActiveSubscriptions_ShouldReturnLatestEndTime()
        {
            // Arrange
            int userId = 1;
            
            var subscription1 = new DataLayer.Models.Subscription
            {
                SubscriptionId = 1,
                UserId = userId,
                PackageId = 1,
                PaymentId = 1,
                Status = "Active",
                StartTime = DateTime.UtcNow.AddDays(-30),
                EndTime = DateTime.UtcNow.AddDays(10) // G?n h?t h?n h?n
            };

            var subscription2 = new DataLayer.Models.Subscription
            {
                SubscriptionId = 2,
                UserId = userId,
                PackageId = 2,
                PaymentId = 2,
                Status = "Active",
                StartTime = DateTime.UtcNow.AddDays(-5),
                EndTime = DateTime.UtcNow.AddDays(25) // Còn lâu h?n - nên tr? v? cái này
            };

            _context.Subscriptions.AddRange(subscription1, subscription2);
            await _context.SaveChangesAsync();

            // Act
            var result = await _service.GetActiveSubscriptionAsync(userId);

            // Assert
            result.Should().NotBeNull();
            result!.SubscriptionId.Should().Be(2);
            result.EndTime.Should().Be(subscription2.EndTime);
        }

        #endregion

        #region No Subscription Scenarios

        [Fact]
        public async Task GetActiveSubscriptionAsync_WhenNoSubscriptionExists_ShouldReturnNull()
        {
            // Arrange
            int userId = 999;

            // Act
            var result = await _service.GetActiveSubscriptionAsync(userId);

            // Assert
            result.Should().BeNull();
        }

        [Fact]
        public async Task GetActiveSubscriptionAsync_WhenSubscriptionExpired_ShouldReturnNull()
        {
            // Arrange
            int userId = 1;
            var expiredSubscription = new DataLayer.Models.Subscription
            {
                SubscriptionId = 1,
                UserId = userId,
                PackageId = 1,
                PaymentId = 1,
                Status = "Active",
                StartTime = DateTime.UtcNow.AddDays(-60),
                EndTime = DateTime.UtcNow.AddDays(-10) // ?ã h?t h?n
            };

            _context.Subscriptions.Add(expiredSubscription);
            await _context.SaveChangesAsync();

            // Act
            var result = await _service.GetActiveSubscriptionAsync(userId);

            // Assert
            result.Should().BeNull();
        }

        #endregion
    }
}
