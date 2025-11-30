using Xunit;
using Moq;
using ServiceLayer.Leaderboard;
using RepositoryLayer.Leaderboard;
using ServiceLayer.Notification;
using DataLayer.DTOs.Leaderboard;
using DataLayer.Models;
using Lumina.Tests.Helpers;
using System;
using System.Threading.Tasks;

namespace Lumina.Test.Services
{
    public class LeaderboardServiceUpdateTests
    {
        private readonly Mock<ILeaderboardRepository> _mockRepository;
        private readonly Mock<INotificationService> _mockNotificationService;
        private readonly LuminaSystemContext _context;
        private readonly LeaderboardService _service;

        public LeaderboardServiceUpdateTests()
        {
            _mockRepository = new Mock<ILeaderboardRepository>();
            _mockNotificationService = new Mock<INotificationService>();
            _context = InMemoryDbContextHelper.CreateContext();
            _service = new LeaderboardService(_mockRepository.Object, _context, _mockNotificationService.Object);
        }

        [Fact]
        public async Task UpdateAsync_WhenLeaderboardIdIsNegative_ShouldCallRepository()
        {
            // Arrange
            int leaderboardId = -1;
            var dto = new UpdateLeaderboardDTO
            {
                SeasonNumber = 1,
                SeasonName = "Test Season",
                StartDate = DateTime.UtcNow,
                EndDate = DateTime.UtcNow.AddDays(30),
                IsActive = true
            };

            _mockRepository
                .Setup(repo => repo.ExistsSeasonNumberAsync(1, leaderboardId))
                .ReturnsAsync(false);

            _mockRepository
                .Setup(repo => repo.ExistsDateOverlapAsync(dto.StartDate, dto.EndDate, leaderboardId))
                .ReturnsAsync(false);

            _mockRepository
                .Setup(repo => repo.UpdateAsync(It.IsAny<Leaderboard>()))
                .ReturnsAsync(false);

            // Act
            var result = await _service.UpdateAsync(leaderboardId, dto);

            // Assert
            Assert.False(result);

            // Verify repository is called
            _mockRepository.Verify(
                repo => repo.UpdateAsync(It.IsAny<Leaderboard>()),
                Times.Once
            );
        }

        [Fact]
        public async Task UpdateAsync_WhenLeaderboardIdIsZero_ShouldCallRepository()
        {
            // Arrange
            int leaderboardId = 0;
            var dto = new UpdateLeaderboardDTO
            {
                SeasonNumber = 1,
                SeasonName = "Test Season",
                StartDate = DateTime.UtcNow,
                EndDate = DateTime.UtcNow.AddDays(30),
                IsActive = true
            };

            _mockRepository
                .Setup(repo => repo.ExistsSeasonNumberAsync(1, leaderboardId))
                .ReturnsAsync(false);

            _mockRepository
                .Setup(repo => repo.ExistsDateOverlapAsync(dto.StartDate, dto.EndDate, leaderboardId))
                .ReturnsAsync(false);

            _mockRepository
                .Setup(repo => repo.UpdateAsync(It.IsAny<Leaderboard>()))
                .ReturnsAsync(false);

            // Act
            var result = await _service.UpdateAsync(leaderboardId, dto);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task UpdateAsync_WhenDtoIsNull_ShouldThrowArgumentNullException()
        {
            // Arrange
            int leaderboardId = 1;
            UpdateLeaderboardDTO? dto = null;

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentNullException>(
                async () => await _service.UpdateAsync(leaderboardId, dto!)
            );

            // Verify repository is never called
            _mockRepository.Verify(
                repo => repo.UpdateAsync(It.IsAny<Leaderboard>()),
                Times.Never
            );
        }

        [Fact]
        public async Task UpdateAsync_WhenSeasonNumberIsZero_ShouldThrowArgumentException()
        {
            // Arrange
            int leaderboardId = 1;
            var dto = new UpdateLeaderboardDTO
            {
                SeasonNumber = 0,
                SeasonName = "Test Season",
                StartDate = DateTime.UtcNow,
                EndDate = DateTime.UtcNow.AddDays(30),
                IsActive = true
            };

            // Act & Assert
            var exception = await Assert.ThrowsAsync<ArgumentException>(
                async () => await _service.UpdateAsync(leaderboardId, dto)
            );

            Assert.Contains("SeasonNumber must be positive", exception.Message);

            // Verify repository is never called
            _mockRepository.Verify(
                repo => repo.UpdateAsync(It.IsAny<Leaderboard>()),
                Times.Never
            );
        }

        [Fact]
        public async Task UpdateAsync_WhenEndDateIsBeforeStartDate_ShouldThrowArgumentException()
        {
            // Arrange
            int leaderboardId = 1;
            var dto = new UpdateLeaderboardDTO
            {
                SeasonNumber = 1,
                SeasonName = "Test Season",
                StartDate = DateTime.UtcNow.AddDays(30),
                EndDate = DateTime.UtcNow, // EndDate before StartDate
                IsActive = true
            };

            _mockRepository
                .Setup(repo => repo.ExistsSeasonNumberAsync(1, leaderboardId))
                .ReturnsAsync(false);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<ArgumentException>(
                async () => await _service.UpdateAsync(leaderboardId, dto)
            );

            Assert.Contains("EndDate must be after StartDate", exception.Message);

            // Verify repository is never called
            _mockRepository.Verify(
                repo => repo.UpdateAsync(It.IsAny<Leaderboard>()),
                Times.Never
            );
        }

        [Fact]
        public async Task UpdateAsync_WhenSeasonNumberAlreadyExists_ShouldThrowArgumentException()
        {
            // Arrange
            int leaderboardId = 1;
            var dto = new UpdateLeaderboardDTO
            {
                SeasonNumber = 2, // Different season number that exists
                SeasonName = "Test Season",
                StartDate = DateTime.UtcNow,
                EndDate = DateTime.UtcNow.AddDays(30),
                IsActive = true
            };

            _mockRepository
                .Setup(repo => repo.ExistsSeasonNumberAsync(2, leaderboardId))
                .ReturnsAsync(true);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<ArgumentException>(
                async () => await _service.UpdateAsync(leaderboardId, dto)
            );

            Assert.Contains("SeasonNumber already exists", exception.Message);

            // Verify repository is never called
            _mockRepository.Verify(
                repo => repo.UpdateAsync(It.IsAny<Leaderboard>()),
                Times.Never
            );
        }

        [Fact]
        public async Task UpdateAsync_WhenDateRangeOverlaps_ShouldThrowArgumentException()
        {
            // Arrange
            int leaderboardId = 1;
            var dto = new UpdateLeaderboardDTO
            {
                SeasonNumber = 1,
                SeasonName = "Test Season",
                StartDate = DateTime.UtcNow,
                EndDate = DateTime.UtcNow.AddDays(30),
                IsActive = true
            };

            _mockRepository
                .Setup(repo => repo.ExistsSeasonNumberAsync(1, leaderboardId))
                .ReturnsAsync(false);

            _mockRepository
                .Setup(repo => repo.ExistsDateOverlapAsync(dto.StartDate, dto.EndDate, leaderboardId))
                .ReturnsAsync(true);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<ArgumentException>(
                async () => await _service.UpdateAsync(leaderboardId, dto)
            );

            Assert.Contains("Date range overlaps", exception.Message);

            // Verify repository is never called
            _mockRepository.Verify(
                repo => repo.UpdateAsync(It.IsAny<Leaderboard>()),
                Times.Never
            );
        }

        [Fact]
        public async Task UpdateAsync_WhenDtoIsValid_ShouldReturnTrue()
        {
            // Arrange
            int leaderboardId = 1;
            var dto = new UpdateLeaderboardDTO
            {
                SeasonNumber = 1,
                SeasonName = "Updated Season",
                StartDate = DateTime.UtcNow,
                EndDate = DateTime.UtcNow.AddDays(30),
                IsActive = true
            };

            _mockRepository
                .Setup(repo => repo.ExistsSeasonNumberAsync(1, leaderboardId))
                .ReturnsAsync(false);

            _mockRepository
                .Setup(repo => repo.ExistsDateOverlapAsync(dto.StartDate, dto.EndDate, leaderboardId))
                .ReturnsAsync(false);

            _mockRepository
                .Setup(repo => repo.UpdateAsync(It.Is<Leaderboard>(e => 
                    e.LeaderboardId == leaderboardId &&
                    e.SeasonNumber == dto.SeasonNumber &&
                    e.SeasonName == dto.SeasonName &&
                    e.StartDate == dto.StartDate &&
                    e.EndDate == dto.EndDate &&
                    e.IsActive == dto.IsActive &&
                    e.UpdateAt.HasValue)))
                .ReturnsAsync(true);

            // Act
            var result = await _service.UpdateAsync(leaderboardId, dto);

            // Assert
            Assert.True(result);

            // Verify repository is called exactly once
            _mockRepository.Verify(
                repo => repo.ExistsSeasonNumberAsync(1, leaderboardId),
                Times.Once
            );

            _mockRepository.Verify(
                repo => repo.ExistsDateOverlapAsync(dto.StartDate, dto.EndDate, leaderboardId),
                Times.Once
            );

            _mockRepository.Verify(
                repo => repo.UpdateAsync(It.Is<Leaderboard>(e => 
                    e.LeaderboardId == leaderboardId &&
                    e.SeasonNumber == dto.SeasonNumber &&
                    e.SeasonName == dto.SeasonName &&
                    e.StartDate == dto.StartDate &&
                    e.EndDate == dto.EndDate &&
                    e.IsActive == dto.IsActive &&
                    e.UpdateAt.HasValue)),
                Times.Once
            );
        }

        [Fact]
        public async Task UpdateAsync_WhenLeaderboardNotFound_ShouldReturnFalse()
        {
            // Arrange
            int leaderboardId = 999;
            var dto = new UpdateLeaderboardDTO
            {
                SeasonNumber = 1,
                SeasonName = "Test Season",
                StartDate = DateTime.UtcNow,
                EndDate = DateTime.UtcNow.AddDays(30),
                IsActive = true
            };

            _mockRepository
                .Setup(repo => repo.ExistsSeasonNumberAsync(1, leaderboardId))
                .ReturnsAsync(false);

            _mockRepository
                .Setup(repo => repo.ExistsDateOverlapAsync(dto.StartDate, dto.EndDate, leaderboardId))
                .ReturnsAsync(false);

            _mockRepository
                .Setup(repo => repo.UpdateAsync(It.IsAny<Leaderboard>()))
                .ReturnsAsync(false);

            // Act
            var result = await _service.UpdateAsync(leaderboardId, dto);

            // Assert
            Assert.False(result);

            // Verify repository is called
            _mockRepository.Verify(
                repo => repo.UpdateAsync(It.IsAny<Leaderboard>()),
                Times.Once
            );
        }
    }
}
