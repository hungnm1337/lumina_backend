using Xunit;
using Moq;
using ServiceLayer.Leaderboard;
using RepositoryLayer.Leaderboard;
using DataLayer.DTOs;
using DataLayer.DTOs.Leaderboard;
using DataLayer.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace Lumina.Tests
{
    public class LeaderboardServiceTests
    {
        private LuminaSystemContext GetInMemoryContext()
        {
            var options = new DbContextOptionsBuilder<LuminaSystemContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            return new LuminaSystemContext(options);
        }

        #region GetAllPaginatedAsync Tests

        [Fact]
        public async Task GetAllPaginatedAsync_ShouldCallRepository()
        {
            // Arrange
            var mockRepo = new Mock<ILeaderboardRepository>();
            var context = GetInMemoryContext();
            mockRepo.Setup(r => r.GetAllPaginatedAsync(null, 1, 10))
                .ReturnsAsync(new PaginatedResultDTO<LeaderboardDTO>
                {
                    Items = new List<LeaderboardDTO>(),
                    Total = 0,
                    Page = 1,
                    PageSize = 10
                });
            var service = new LeaderboardService(mockRepo.Object, context);

            // Act
            await service.GetAllPaginatedAsync(page: 1, pageSize: 10);

            // Assert
            mockRepo.Verify(r => r.GetAllPaginatedAsync(null, 1, 10), Times.Once);
        }

        #endregion

        #region GetAllAsync Tests

        [Fact]
        public async Task GetAllAsync_ShouldCallRepository()
        {
            // Arrange
            var mockRepo = new Mock<ILeaderboardRepository>();
            var context = GetInMemoryContext();
            mockRepo.Setup(r => r.GetAllAsync(null))
                .ReturnsAsync(new List<LeaderboardDTO>());
            var service = new LeaderboardService(mockRepo.Object, GetInMemoryContext());

            // Act
            await service.GetAllAsync();

            // Assert
            mockRepo.Verify(r => r.GetAllAsync(null), Times.Once);
        }

        [Fact]
        public async Task GetAllAsync_ShouldFilterByIsActive()
        {
            // Arrange
            var mockRepo = new Mock<ILeaderboardRepository>();
            mockRepo.Setup(r => r.GetAllAsync(true))
                .ReturnsAsync(new List<LeaderboardDTO>());
            var service = new LeaderboardService(mockRepo.Object, GetInMemoryContext());

            // Act
            await service.GetAllAsync(isActive: true);

            // Assert
            mockRepo.Verify(r => r.GetAllAsync(true), Times.Once);
        }

        #endregion

        #region GetByIdAsync Tests

        [Fact]
        public async Task GetByIdAsync_ShouldCallRepository()
        {
            // Arrange
            var mockRepo = new Mock<ILeaderboardRepository>();
            mockRepo.Setup(r => r.GetByIdAsync(1))
                .ReturnsAsync(new LeaderboardDTO { LeaderboardId = 1 });
            var service = new LeaderboardService(mockRepo.Object, GetInMemoryContext());

            // Act
            await service.GetByIdAsync(1);

            // Assert
            mockRepo.Verify(r => r.GetByIdAsync(1), Times.Once);
        }

        #endregion

        #region GetCurrentAsync Tests

        [Fact]
        public async Task GetCurrentAsync_ShouldCallRepository()
        {
            // Arrange
            var mockRepo = new Mock<ILeaderboardRepository>();
            mockRepo.Setup(r => r.GetCurrentAsync())
                .ReturnsAsync(new LeaderboardDTO { LeaderboardId = 1 });
            var service = new LeaderboardService(mockRepo.Object, GetInMemoryContext());

            // Act
            await service.GetCurrentAsync();

            // Assert
            mockRepo.Verify(r => r.GetCurrentAsync(), Times.Once);
        }

        #endregion

        #region CreateAsync Tests

        [Fact]
        public async Task CreateAsync_ShouldThrowException_WhenSeasonNumberIsZero()
        {
            // Arrange
            var mockRepo = new Mock<ILeaderboardRepository>();
            var service = new LeaderboardService(mockRepo.Object, GetInMemoryContext());

            var dto = new CreateLeaderboardDTO
            {
                SeasonName = "Test",
                SeasonNumber = 0,
                IsActive = false
            };

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() => service.CreateAsync(dto));
        }

        [Fact]
        public async Task CreateAsync_ShouldThrowException_WhenSeasonNumberIsNegative()
        {
            // Arrange
            var mockRepo = new Mock<ILeaderboardRepository>();
            var service = new LeaderboardService(mockRepo.Object, GetInMemoryContext());

            var dto = new CreateLeaderboardDTO
            {
                SeasonName = "Test",
                SeasonNumber = -1,
                IsActive = false
            };

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() => service.CreateAsync(dto));
        }

        [Fact]
        public async Task CreateAsync_ShouldThrowException_WhenEndDateBeforeStartDate()
        {
            // Arrange
            var mockRepo = new Mock<ILeaderboardRepository>();
            var service = new LeaderboardService(mockRepo.Object, GetInMemoryContext());

            var dto = new CreateLeaderboardDTO
            {
                SeasonName = "Test",
                SeasonNumber = 1,
                StartDate = new DateTime(2025, 12, 31),
                EndDate = new DateTime(2025, 1, 1),
                IsActive = false
            };

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() => service.CreateAsync(dto));
        }

        [Fact]
        public async Task CreateAsync_ShouldThrowException_WhenSeasonNumberExists()
        {
            // Arrange
            var mockRepo = new Mock<ILeaderboardRepository>();
            mockRepo.Setup(r => r.ExistsSeasonNumberAsync(5, null))
                .ReturnsAsync(true);
            var service = new LeaderboardService(mockRepo.Object, GetInMemoryContext());

            var dto = new CreateLeaderboardDTO
            {
                SeasonName = "Test",
                SeasonNumber = 5,
                IsActive = false
            };

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() => service.CreateAsync(dto));
        }

        [Fact]
        public async Task CreateAsync_ShouldThrowException_WhenDateRangeOverlaps()
        {
            // Arrange
            var mockRepo = new Mock<ILeaderboardRepository>();
            mockRepo.Setup(r => r.ExistsSeasonNumberAsync(1, null))
                .ReturnsAsync(false);
            mockRepo.Setup(r => r.ExistsDateOverlapAsync(It.IsAny<DateTime?>(), It.IsAny<DateTime?>(), null))
                .ReturnsAsync(true);
            var service = new LeaderboardService(mockRepo.Object, GetInMemoryContext());

            var dto = new CreateLeaderboardDTO
            {
                SeasonName = "Test",
                SeasonNumber = 1,
                StartDate = new DateTime(2025, 6, 1),
                EndDate = new DateTime(2025, 6, 30),
                IsActive = false
            };

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() => service.CreateAsync(dto));
        }

        [Fact]
        public async Task CreateAsync_ShouldCallRepository_WhenValidationPasses()
        {
            // Arrange
            var mockRepo = new Mock<ILeaderboardRepository>();
            mockRepo.Setup(r => r.ExistsSeasonNumberAsync(1, null))
                .ReturnsAsync(false);
            mockRepo.Setup(r => r.ExistsDateOverlapAsync(It.IsAny<DateTime?>(), It.IsAny<DateTime?>(), null))
                .ReturnsAsync(false);
            mockRepo.Setup(r => r.CreateAsync(It.IsAny<DataLayer.Models.Leaderboard>()))
                .ReturnsAsync(10);
            var service = new LeaderboardService(mockRepo.Object, GetInMemoryContext());

            var dto = new CreateLeaderboardDTO
            {
                SeasonName = "Summer Season",
                SeasonNumber = 1,
                StartDate = new DateTime(2025, 6, 1),
                EndDate = new DateTime(2025, 8, 31),
                IsActive = true
            };

            // Act
            var result = await service.CreateAsync(dto);

            // Assert
            Assert.Equal(10, result);
            mockRepo.Verify(r => r.CreateAsync(It.Is<DataLayer.Models.Leaderboard>(l =>
                l.SeasonName == "Summer Season" &&
                l.SeasonNumber == 1 &&
                l.IsActive == true
            )), Times.Once);
        }

        #endregion

        #region UpdateAsync Tests

        [Fact]
        public async Task UpdateAsync_ShouldThrowException_WhenSeasonNumberInvalid()
        {
            // Arrange
            var mockRepo = new Mock<ILeaderboardRepository>();
            var service = new LeaderboardService(mockRepo.Object, GetInMemoryContext());

            var dto = new UpdateLeaderboardDTO
            {
                SeasonName = "Test",
                SeasonNumber = -1
            };

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() => service.UpdateAsync(1, dto));
        }

        [Fact]
        public async Task UpdateAsync_ShouldThrowException_WhenSeasonNumberExistsForOtherSeason()
        {
            // Arrange
            var mockRepo = new Mock<ILeaderboardRepository>();
            mockRepo.Setup(r => r.ExistsSeasonNumberAsync(5, 1))
                .ReturnsAsync(true);
            var service = new LeaderboardService(mockRepo.Object, GetInMemoryContext());

            var dto = new UpdateLeaderboardDTO
            {
                SeasonName = "Test",
                SeasonNumber = 5
            };

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() => service.UpdateAsync(1, dto));
        }

        [Fact]
        public async Task UpdateAsync_ShouldCallRepository_WhenValidationPasses()
        {
            // Arrange
            var mockRepo = new Mock<ILeaderboardRepository>();
            mockRepo.Setup(r => r.ExistsSeasonNumberAsync(2, 5))
                .ReturnsAsync(false);
            mockRepo.Setup(r => r.ExistsDateOverlapAsync(It.IsAny<DateTime?>(), It.IsAny<DateTime?>(), 5))
                .ReturnsAsync(false);
            mockRepo.Setup(r => r.UpdateAsync(It.IsAny<DataLayer.Models.Leaderboard>()))
                .ReturnsAsync(true);
            var service = new LeaderboardService(mockRepo.Object, GetInMemoryContext());

            var dto = new UpdateLeaderboardDTO
            {
                SeasonName = "Updated Season",
                SeasonNumber = 2,
                StartDate = new DateTime(2025, 6, 1),
                EndDate = new DateTime(2025, 8, 31),
                IsActive = false
            };

            // Act
            var result = await service.UpdateAsync(5, dto);

            // Assert
            Assert.True(result);
            mockRepo.Verify(r => r.UpdateAsync(It.Is<DataLayer.Models.Leaderboard>(l =>
                l.LeaderboardId == 5 &&
                l.SeasonName == "Updated Season" &&
                l.SeasonNumber == 2
            )), Times.Once);
        }

        #endregion

        #region DeleteAsync Tests

        [Fact]
        public async Task DeleteAsync_ShouldCallRepository()
        {
            // Arrange
            var mockRepo = new Mock<ILeaderboardRepository>();
            mockRepo.Setup(r => r.DeleteAsync(1))
                .ReturnsAsync(true);
            var service = new LeaderboardService(mockRepo.Object, GetInMemoryContext());

            // Act
            var result = await service.DeleteAsync(1);

            // Assert
            Assert.True(result);
            mockRepo.Verify(r => r.DeleteAsync(1), Times.Once);
        }

        #endregion

        #region SetCurrentAsync Tests

        [Fact]
        public async Task SetCurrentAsync_ShouldCallRepository()
        {
            // Arrange
            var mockRepo = new Mock<ILeaderboardRepository>();
            mockRepo.Setup(r => r.SetCurrentAsync(3))
                .ReturnsAsync(true);
            var service = new LeaderboardService(mockRepo.Object, GetInMemoryContext());

            // Act
            var result = await service.SetCurrentAsync(3);

            // Assert
            Assert.True(result);
            mockRepo.Verify(r => r.SetCurrentAsync(3), Times.Once);
        }

        #endregion

        #region GetSeasonRankingAsync Tests

        [Fact]
        public async Task GetSeasonRankingAsync_ShouldCallRepository_WithDefaultTop()
        {
            // Arrange
            var mockRepo = new Mock<ILeaderboardRepository>();
            mockRepo.Setup(r => r.GetSeasonRankingAsync(1, 100))
                .ReturnsAsync(new List<LeaderboardRankDTO>());
            var service = new LeaderboardService(mockRepo.Object, GetInMemoryContext());

            // Act
            await service.GetSeasonRankingAsync(1);

            // Assert
            mockRepo.Verify(r => r.GetSeasonRankingAsync(1, 100), Times.Once);
        }

        [Fact]
        public async Task GetSeasonRankingAsync_ShouldCallRepository_WithCustomTop()
        {
            // Arrange
            var mockRepo = new Mock<ILeaderboardRepository>();
            mockRepo.Setup(r => r.GetSeasonRankingAsync(1, 50))
                .ReturnsAsync(new List<LeaderboardRankDTO>());
            var service = new LeaderboardService(mockRepo.Object, GetInMemoryContext());

            // Act
            await service.GetSeasonRankingAsync(1, top: 50);

            // Assert
            mockRepo.Verify(r => r.GetSeasonRankingAsync(1, 50), Times.Once);
        }

        #endregion

        #region RecalculateSeasonScoresAsync Tests

        [Fact]
        public async Task RecalculateSeasonScoresAsync_ShouldCallRepository()
        {
            // Arrange
            var mockRepo = new Mock<ILeaderboardRepository>();
            mockRepo.Setup(r => r.RecalculateSeasonScoresAsync(1))
                .ReturnsAsync(10);
            var service = new LeaderboardService(mockRepo.Object, GetInMemoryContext());

            // Act
            var result = await service.RecalculateSeasonScoresAsync(1);

            // Assert
            Assert.Equal(10, result);
            mockRepo.Verify(r => r.RecalculateSeasonScoresAsync(1), Times.Once);
        }

        #endregion

        #region ResetSeasonAsync Tests

        [Fact]
        public async Task ResetSeasonAsync_ShouldCallRepository_WithArchiveTrue()
        {
            // Arrange
            var mockRepo = new Mock<ILeaderboardRepository>();
            mockRepo.Setup(r => r.ResetSeasonScoresAsync(1, true))
                .ReturnsAsync(5);
            var service = new LeaderboardService(mockRepo.Object, GetInMemoryContext());

            // Act
            var result = await service.ResetSeasonAsync(1, archiveScores: true);

            // Assert
            Assert.Equal(5, result);
            mockRepo.Verify(r => r.ResetSeasonScoresAsync(1, true), Times.Once);
        }

        [Fact]
        public async Task ResetSeasonAsync_ShouldCallRepository_WithArchiveFalse()
        {
            // Arrange
            var mockRepo = new Mock<ILeaderboardRepository>();
            mockRepo.Setup(r => r.ResetSeasonScoresAsync(1, false))
                .ReturnsAsync(5);
            var service = new LeaderboardService(mockRepo.Object, GetInMemoryContext());

            // Act
            var result = await service.ResetSeasonAsync(1, archiveScores: false);

            // Assert
            Assert.Equal(5, result);
            mockRepo.Verify(r => r.ResetSeasonScoresAsync(1, false), Times.Once);
        }

        #endregion

        #region GetUserStatsAsync Tests

        [Fact]
        public async Task GetUserStatsAsync_ShouldUseProvidedLeaderboardId()
        {
            // Arrange
            var mockRepo = new Mock<ILeaderboardRepository>();
            mockRepo.Setup(r => r.GetUserSeasonStatsAsync(10, 5))
                .ReturnsAsync(new UserSeasonStatsDTO { UserId = 10 });
            var service = new LeaderboardService(mockRepo.Object, GetInMemoryContext());

            // Act
            await service.GetUserStatsAsync(userId: 10, leaderboardId: 5);

            // Assert
            mockRepo.Verify(r => r.GetUserSeasonStatsAsync(10, 5), Times.Once);
            mockRepo.Verify(r => r.GetCurrentAsync(), Times.Never);
        }

        [Fact]
        public async Task GetUserStatsAsync_ShouldUseCurrentSeason_WhenLeaderboardIdNotProvided()
        {
            // Arrange
            var mockRepo = new Mock<ILeaderboardRepository>();
            mockRepo.Setup(r => r.GetCurrentAsync())
                .ReturnsAsync(new LeaderboardDTO { LeaderboardId = 3 });
            mockRepo.Setup(r => r.GetUserSeasonStatsAsync(10, 3))
                .ReturnsAsync(new UserSeasonStatsDTO { UserId = 10 });
            var service = new LeaderboardService(mockRepo.Object, GetInMemoryContext());

            // Act
            await service.GetUserStatsAsync(userId: 10);

            // Assert
            mockRepo.Verify(r => r.GetCurrentAsync(), Times.Once);
            mockRepo.Verify(r => r.GetUserSeasonStatsAsync(10, 3), Times.Once);
        }

        [Fact]
        public async Task GetUserStatsAsync_ShouldReturnNull_WhenNoCurrentSeason()
        {
            // Arrange
            var mockRepo = new Mock<ILeaderboardRepository>();
            mockRepo.Setup(r => r.GetCurrentAsync())
                .ReturnsAsync((LeaderboardDTO?)null);
            var service = new LeaderboardService(mockRepo.Object, GetInMemoryContext());

            // Act
            var result = await service.GetUserStatsAsync(userId: 10);

            // Assert
            Assert.Null(result);
            mockRepo.Verify(r => r.GetUserSeasonStatsAsync(It.IsAny<int>(), It.IsAny<int>()), Times.Never);
        }

        #endregion

        #region GetUserTOEICCalculationAsync Tests

        [Fact]
        public async Task GetUserTOEICCalculationAsync_ShouldUseProvidedLeaderboardId()
        {
            // Arrange
            var mockRepo = new Mock<ILeaderboardRepository>();
            mockRepo.Setup(r => r.GetUserTOEICCalculationAsync(10, 5))
                .ReturnsAsync(new TOEICScoreCalculationDTO { UserId = 10 });
            var service = new LeaderboardService(mockRepo.Object, GetInMemoryContext());

            // Act
            await service.GetUserTOEICCalculationAsync(userId: 10, leaderboardId: 5);

            // Assert
            mockRepo.Verify(r => r.GetUserTOEICCalculationAsync(10, 5), Times.Once);
            mockRepo.Verify(r => r.GetCurrentAsync(), Times.Never);
        }

        [Fact]
        public async Task GetUserTOEICCalculationAsync_ShouldUseCurrentSeason_WhenLeaderboardIdNotProvided()
        {
            // Arrange
            var mockRepo = new Mock<ILeaderboardRepository>();
            mockRepo.Setup(r => r.GetCurrentAsync())
                .ReturnsAsync(new LeaderboardDTO { LeaderboardId = 3 });
            mockRepo.Setup(r => r.GetUserTOEICCalculationAsync(10, 3))
                .ReturnsAsync(new TOEICScoreCalculationDTO { UserId = 10 });
            var service = new LeaderboardService(mockRepo.Object, GetInMemoryContext());

            // Act
            await service.GetUserTOEICCalculationAsync(userId: 10);

            // Assert
            mockRepo.Verify(r => r.GetCurrentAsync(), Times.Once);
            mockRepo.Verify(r => r.GetUserTOEICCalculationAsync(10, 3), Times.Once);
        }

        [Fact]
        public async Task GetUserTOEICCalculationAsync_ShouldReturnNull_WhenNoCurrentSeason()
        {
            // Arrange
            var mockRepo = new Mock<ILeaderboardRepository>();
            mockRepo.Setup(r => r.GetCurrentAsync())
                .ReturnsAsync((LeaderboardDTO?)null);
            var service = new LeaderboardService(mockRepo.Object, GetInMemoryContext());

            // Act
            var result = await service.GetUserTOEICCalculationAsync(userId: 10);

            // Assert
            Assert.Null(result);
            mockRepo.Verify(r => r.GetUserTOEICCalculationAsync(It.IsAny<int>(), It.IsAny<int>()), Times.Never);
        }

        #endregion

        #region GetUserRankAsync Tests

        [Fact]
        public async Task GetUserRankAsync_ShouldUseProvidedLeaderboardId()
        {
            // Arrange
            var mockRepo = new Mock<ILeaderboardRepository>();
            mockRepo.Setup(r => r.GetUserRankInSeasonAsync(10, 5))
                .ReturnsAsync(3);
            var service = new LeaderboardService(mockRepo.Object, GetInMemoryContext());

            // Act
            var result = await service.GetUserRankAsync(userId: 10, leaderboardId: 5);

            // Assert
            Assert.Equal(3, result);
            mockRepo.Verify(r => r.GetUserRankInSeasonAsync(10, 5), Times.Once);
            mockRepo.Verify(r => r.GetCurrentAsync(), Times.Never);
        }

        [Fact]
        public async Task GetUserRankAsync_ShouldUseCurrentSeason_WhenLeaderboardIdNotProvided()
        {
            // Arrange
            var mockRepo = new Mock<ILeaderboardRepository>();
            mockRepo.Setup(r => r.GetCurrentAsync())
                .ReturnsAsync(new LeaderboardDTO { LeaderboardId = 3 });
            mockRepo.Setup(r => r.GetUserRankInSeasonAsync(10, 3))
                .ReturnsAsync(1);
            var service = new LeaderboardService(mockRepo.Object, GetInMemoryContext());

            // Act
            var result = await service.GetUserRankAsync(userId: 10);

            // Assert
            Assert.Equal(1, result);
            mockRepo.Verify(r => r.GetCurrentAsync(), Times.Once);
            mockRepo.Verify(r => r.GetUserRankInSeasonAsync(10, 3), Times.Once);
        }

        [Fact]
        public async Task GetUserRankAsync_ShouldReturnZero_WhenNoCurrentSeason()
        {
            // Arrange
            var mockRepo = new Mock<ILeaderboardRepository>();
            mockRepo.Setup(r => r.GetCurrentAsync())
                .ReturnsAsync((LeaderboardDTO?)null);
            var service = new LeaderboardService(mockRepo.Object, GetInMemoryContext());

            // Act
            var result = await service.GetUserRankAsync(userId: 10);

            // Assert
            Assert.Equal(0, result);
            mockRepo.Verify(r => r.GetUserRankInSeasonAsync(It.IsAny<int>(), It.IsAny<int>()), Times.Never);
        }

        #endregion

        #region AutoManageSeasonsAsync Tests

        [Fact]
        public async Task AutoManageSeasonsAsync_ShouldCallBothAutoMethods()
        {
            // Arrange
            var mockRepo = new Mock<ILeaderboardRepository>();
            mockRepo.Setup(r => r.AutoActivateSeasonAsync())
                .Returns(Task.CompletedTask);
            mockRepo.Setup(r => r.AutoEndSeasonAsync())
                .Returns(Task.CompletedTask);
            var service = new LeaderboardService(mockRepo.Object, GetInMemoryContext());

            // Act
            await service.AutoManageSeasonsAsync();

            // Assert
            mockRepo.Verify(r => r.AutoActivateSeasonAsync(), Times.Once);
            mockRepo.Verify(r => r.AutoEndSeasonAsync(), Times.Once);
        }

        #endregion
    }
}
