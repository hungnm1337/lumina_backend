using FluentAssertions;
using Moq;
using RepositoryLayer.Quota;
using ServiceLayer.Quota;

namespace Lumina.Tests.ServiceTests
{
    public class CheckQuotaAsyncUnitTest
    {
        private readonly Mock<IQuotaRepository> _mockQuotaRepo;
        private readonly QuotaService _service;
        private const int FREE_TIER_LIMIT = 20;

        public CheckQuotaAsyncUnitTest()
        {
            _mockQuotaRepo = new Mock<IQuotaRepository>();
            _service = new QuotaService(_mockQuotaRepo.Object);
        }

        #region Premium User

        [Fact]
        public async Task CheckQuotaAsync_WhenUserIsPremium_ShouldAllowUnlimitedAccess()
        {
            // Arrange
            int userId = 1;
            var userStatus = new UserQuotaStatus
            {
                UserId = userId,
                SubscriptionType = "PREMIUM",
                MonthlyReadingAttempts = 100,
                MonthlyListeningAttempts = 50,
                HasActiveSubscription = true
            };

            _mockQuotaRepo.Setup(x => x.GetUserQuotaStatusAsync(userId))
                .ReturnsAsync(userStatus);

            // Act
            var result = await _service.CheckQuotaAsync(userId, "reading");

            // Assert
            result.Should().NotBeNull();
            result.CanAccess.Should().BeTrue();
            result.IsPremium.Should().BeTrue();
            result.RequiresUpgrade.Should().BeFalse();
            result.RemainingAttempts.Should().Be(-1);
            result.SubscriptionType.Should().Be("PREMIUM");

            _mockQuotaRepo.Verify(x => x.GetUserQuotaStatusAsync(userId), Times.Once);
        }

        #endregion

        #region Free User - Reading/Listening Skills

        [Theory]
        [InlineData("reading", 10, 5, true, 10)]   // Còn quota
        [InlineData("READING", 20, 0, false, 0)]   // H?t quota + case-insensitive
        [InlineData("listening", 0, 15, true, 5)]  // Listening còn quota
        public async Task CheckQuotaAsync_WhenFreeUserAccessesFreeSkill_ShouldCheckQuotaLimit(
            string skill, 
            int readingAttempts, 
            int listeningAttempts, 
            bool expectedCanAccess, 
            int expectedRemaining)
        {
            // Arrange
            int userId = 2;
            var userStatus = new UserQuotaStatus
            {
                UserId = userId,
                SubscriptionType = "FREE",
                MonthlyReadingAttempts = readingAttempts,
                MonthlyListeningAttempts = listeningAttempts,
                HasActiveSubscription = false
            };

            _mockQuotaRepo.Setup(x => x.GetUserQuotaStatusAsync(userId))
                .ReturnsAsync(userStatus);

            // Act
            var result = await _service.CheckQuotaAsync(userId, skill);

            // Assert
            result.Should().NotBeNull();
            result.CanAccess.Should().Be(expectedCanAccess);
            result.IsPremium.Should().BeFalse();
            result.RequiresUpgrade.Should().BeFalse();
            result.RemainingAttempts.Should().Be(expectedRemaining);
            result.SubscriptionType.Should().Be("FREE");
        }

        #endregion

        #region Free User - Premium-Only Skills

        [Theory]
        [InlineData("speaking")]
        [InlineData("writing")]
        public async Task CheckQuotaAsync_WhenFreeUserAccessesPremiumSkill_ShouldRequireUpgrade(string skill)
        {
            // Arrange
            int userId = 3;
            var userStatus = new UserQuotaStatus
            {
                UserId = userId,
                SubscriptionType = "FREE",
                MonthlyReadingAttempts = 5,
                MonthlyListeningAttempts = 5,
                HasActiveSubscription = false
            };

            _mockQuotaRepo.Setup(x => x.GetUserQuotaStatusAsync(userId))
                .ReturnsAsync(userStatus);

            // Act
            var result = await _service.CheckQuotaAsync(userId, skill);

            // Assert
            result.Should().NotBeNull();
            result.CanAccess.Should().BeFalse();
            result.IsPremium.Should().BeFalse();
            result.RequiresUpgrade.Should().BeTrue();
            result.RemainingAttempts.Should().Be(0);
            result.SubscriptionType.Should().Be("FREE");
        }

        #endregion

        #region Unknown Skill

        [Fact]
        public async Task CheckQuotaAsync_WhenSkillIsUnknown_ShouldDenyAccess()
        {
            // Arrange
            int userId = 4;
            var userStatus = new UserQuotaStatus
            {
                UserId = userId,
                SubscriptionType = "FREE",
                MonthlyReadingAttempts = 5,
                MonthlyListeningAttempts = 5,
                HasActiveSubscription = false
            };

            _mockQuotaRepo.Setup(x => x.GetUserQuotaStatusAsync(userId))
                .ReturnsAsync(userStatus);

            // Act
            var result = await _service.CheckQuotaAsync(userId, "unknown");

            // Assert
            result.Should().NotBeNull();
            result.CanAccess.Should().BeFalse();
            result.IsPremium.Should().BeFalse();
            result.RequiresUpgrade.Should().BeFalse();
            result.RemainingAttempts.Should().Be(0);
            result.SubscriptionType.Should().Be("FREE");
        }

        #endregion
    }
}
