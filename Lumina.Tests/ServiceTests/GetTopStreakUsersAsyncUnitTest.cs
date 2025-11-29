using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DataLayer.DTOs.Streak;
using DataLayer.Models;
using Lumina.Tests.Helpers;
using Microsoft.Extensions.Logging;
using Moq;
using RepositoryLayer.Streak;
using ServiceLayer.Streak;
using Xunit;

namespace Lumina.Tests.ServiceTests
{
    public class GetTopStreakUsersAsyncUnitTest
    {
        private static (StreakService Service, LuminaSystemContext Context, Mock<ILogger<StreakService>> LoggerMock, Mock<IStreakRepository> RepoMock) CreateService()
        {
            var context = InMemoryDbContextHelper.CreateContext(Guid.NewGuid().ToString());
            var loggerMock = new Mock<ILogger<StreakService>>();
            var repoMock = new Mock<IStreakRepository>(MockBehavior.Strict);
            var service = new StreakService(context, loggerMock.Object, repoMock.Object);

            return (service, context, loggerMock, repoMock);
        }

        private static async Task DisposeContextAsync(LuminaSystemContext context)
        {
            await context.Database.EnsureDeletedAsync();
            await context.DisposeAsync();
        }

        [Fact]
        public async Task GetTopStreakUsersAsync_ValidTopN_DelegatesToRepository()
        {
            // Arrange
            var (service, context, loggerMock, repoMock) = CreateService();
            try
            {
                var expectedUsers = new List<StreakUserDTO>
                {
                    new StreakUserDTO { UserId = 1, FullName = "User 1", CurrentStreak = 100 },
                    new StreakUserDTO { UserId = 2, FullName = "User 2", CurrentStreak = 50 }
                };

                repoMock
                    .Setup(r => r.GetTopStreakUsersAsync(10))
                    .ReturnsAsync(expectedUsers);

                // Act
                var result = await service.GetTopStreakUsersAsync(10);

                // Assert
                Assert.NotNull(result);
                Assert.Equal(2, result.Count);
                Assert.Equal(1, result[0].UserId);
                Assert.Equal(100, result[0].CurrentStreak);

                repoMock.Verify(r => r.GetTopStreakUsersAsync(10), Times.Once);
            }
            finally
            {
                await DisposeContextAsync(context);
            }
        }

        [Theory]
        [InlineData(0)] // Zero topN
        [InlineData(10)] // Repository returns empty
        public async Task GetTopStreakUsersAsync_EmptyResult_ReturnsEmptyList(int topN)
        {
            // Arrange
            var (service, context, loggerMock, repoMock) = CreateService();
            try
            {
                repoMock
                    .Setup(r => r.GetTopStreakUsersAsync(topN))
                    .ReturnsAsync(new List<StreakUserDTO>());

                // Act
                var result = await service.GetTopStreakUsersAsync(topN);

                // Assert
                Assert.NotNull(result);
                Assert.Empty(result);

                repoMock.Verify(r => r.GetTopStreakUsersAsync(topN), Times.Once);
            }
            finally
            {
                await DisposeContextAsync(context);
            }
        }
    }
}

