using DataLayer.DTOs.Quota;
using FluentAssertions;
using Moq;
using RepositoryLayer.Quota;
using ServiceLayer.Quota;

namespace Lumina.Tests.ServiceTests
{
    public class GetRemainingQuotaAsyncUnitTest
    {
        private readonly Mock<IQuotaRepository> _mockQuotaRepo;
        private readonly QuotaService _service;
        private const int FREE_TIER_LIMIT = 20;

        public GetRemainingQuotaAsyncUnitTest()
        {
            _mockQuotaRepo = new Mock<IQuotaRepository>();
            _service = new QuotaService(_mockQuotaRepo.Object);
        }

        #region Premium User Scenarios

        [Fact]
        public async Task GetRemainingQuotaAsync_WhenUserIsPremium_ShouldReturnUnlimitedQuota()
        {
            // Arrange
            int userId = 1;
            var userStatus = new UserQuotaStatus
            {
                UserId = userId,
                SubscriptionType = "PREMIUM",
                MonthlyReadingAttempts = 50,
                MonthlyListeningAttempts = 30,
                HasActiveSubscription = true
            };

            _mockQuotaRepo.Setup(x => x.GetUserQuotaStatusAsync(userId))
                .ReturnsAsync(userStatus);

            // Act
            var result = await _service.GetRemainingQuotaAsync(userId);

            // Assert
            result.Should().NotBeNull();
            result.IsPremium.Should().BeTrue();
            result.ReadingRemaining.Should().Be(-1);
            result.ListeningRemaining.Should().Be(-1);
            result.ReadingLimit.Should().Be(-1);
            result.ListeningLimit.Should().Be(-1);
            result.ReadingUsed.Should().Be(50);
            result.ListeningUsed.Should().Be(30);

            _mockQuotaRepo.Verify(x => x.GetUserQuotaStatusAsync(userId), Times.Once);
        }

        #endregion

        #region Free User Scenarios

        [Fact]
        public async Task GetRemainingQuotaAsync_WhenFreeUserHasRemainingQuota_ShouldReturnCorrectRemaining()
        {
            // Arrange
            int userId = 2;
            var userStatus = new UserQuotaStatus
            {
                UserId = userId,
                SubscriptionType = "FREE",
                MonthlyReadingAttempts = 5,
                MonthlyListeningAttempts = 10,
                HasActiveSubscription = false
            };

            _mockQuotaRepo.Setup(x => x.GetUserQuotaStatusAsync(userId))
                .ReturnsAsync(userStatus);

            // Act
            var result = await _service.GetRemainingQuotaAsync(userId);

            // Assert
            result.Should().NotBeNull();
            result.IsPremium.Should().BeFalse();
            result.ReadingRemaining.Should().Be(15); // 20 - 5
            result.ListeningRemaining.Should().Be(10); // 20 - 10
            result.ReadingLimit.Should().Be(FREE_TIER_LIMIT);
            result.ListeningLimit.Should().Be(FREE_TIER_LIMIT);
            result.ReadingUsed.Should().Be(5);
            result.ListeningUsed.Should().Be(10);
        }

        [Fact]
        public async Task GetRemainingQuotaAsync_WhenFreeUserExceedsQuota_ShouldReturnZeroRemaining()
        {
            // Arrange
            int userId = 3;
            var userStatus = new UserQuotaStatus
            {
                UserId = userId,
                SubscriptionType = "FREE",
                MonthlyReadingAttempts = 25, // V??t quá limit
                MonthlyListeningAttempts = 20, // ?úng limit
                HasActiveSubscription = false
            };

            _mockQuotaRepo.Setup(x => x.GetUserQuotaStatusAsync(userId))
                .ReturnsAsync(userStatus);

            // Act
            var result = await _service.GetRemainingQuotaAsync(userId);

            // Assert
            result.Should().NotBeNull();
            result.IsPremium.Should().BeFalse();
            result.ReadingRemaining.Should().Be(0); // Math.Max(0, 20 - 25)
            result.ListeningRemaining.Should().Be(0); // Math.Max(0, 20 - 20)
            result.ReadingUsed.Should().Be(25);
            result.ListeningUsed.Should().Be(20);
        }

        #endregion
    }
}
