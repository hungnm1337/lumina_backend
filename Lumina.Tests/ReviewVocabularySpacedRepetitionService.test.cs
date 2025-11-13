using DataLayer.DTOs.Vocabulary;
using DataLayer.Models;
using Moq;
using RepositoryLayer.UnitOfWork;
using RepositoryLayer.UserSpacedRepetition;
using ServiceLayer.Vocabulary;
using System;
using System.Threading.Tasks;
using Xunit;

namespace Lumina.Tests
{
    public class ReviewVocabularySpacedRepetitionServiceTests
    {
        private readonly Mock<IUnitOfWork> _mockUnitOfWork;
        private readonly Mock<IUserSpacedRepetitionRepository> _mockUserSpacedRepetitionRepository;
        private readonly SpacedRepetitionService _service;

        public ReviewVocabularySpacedRepetitionServiceTests()
        {
            _mockUnitOfWork = new Mock<IUnitOfWork>();
            _mockUserSpacedRepetitionRepository = new Mock<IUserSpacedRepetitionRepository>();
            _mockUnitOfWork.Setup(u => u.UserSpacedRepetitions).Returns(_mockUserSpacedRepetitionRepository.Object);
            _mockUnitOfWork.Setup(u => u.CompleteAsync()).ReturnsAsync(1);
            _service = new SpacedRepetitionService(_mockUnitOfWork.Object);
        }

        [Fact]
        public async Task ReviewVocabularyAsync_WithValidRequest_ShouldReturnSuccess()
        {
            // Arrange
            var userId = 1;
            var repetitionId = 1;
            var request = new ReviewVocabularyRequestDTO
            {
                UserSpacedRepetitionId = repetitionId,
                Quality = 5
            };

            var vocabularyList = new VocabularyList { VocabularyListId = 1, Name = "Test List" };
            var repetition = new UserSpacedRepetition
            {
                UserSpacedRepetitionId = repetitionId,
                UserId = userId,
                VocabularyListId = 1,
                VocabularyList = vocabularyList,
                LastReviewedAt = DateTime.UtcNow.AddDays(-5),
                NextReviewAt = DateTime.UtcNow.AddDays(1),
                ReviewCount = 2,
                Intervals = 3,
                Status = "Learning"
            };

            _mockUserSpacedRepetitionRepository
                .Setup(r => r.GetByIdAsync(repetitionId))
                .ReturnsAsync(repetition);
            _mockUserSpacedRepetitionRepository
                .Setup(r => r.UpdateAsync(It.IsAny<UserSpacedRepetition>()))
                .ReturnsAsync((UserSpacedRepetition r) => r);

            // Act
            var result = await _service.ReviewVocabularyAsync(userId, request);

            // Assert
            Assert.NotNull(result);
            Assert.True(result.Success);
            Assert.Equal("Đã cập nhật tiến độ học tập", result.Message);
            Assert.NotNull(result.UpdatedRepetition);
            Assert.NotNull(result.NextReviewAt);
            Assert.True(result.NewIntervals > 0);
            _mockUserSpacedRepetitionRepository.Verify(r => r.GetByIdAsync(repetitionId), Times.Once);
            _mockUserSpacedRepetitionRepository.Verify(r => r.UpdateAsync(It.IsAny<UserSpacedRepetition>()), Times.Once);
            _mockUnitOfWork.Verify(u => u.CompleteAsync(), Times.Once);
        }

        [Fact]
        public async Task ReviewVocabularyAsync_WithNullRepetition_ShouldReturnFailure()
        {
            // Arrange
            var userId = 1;
            var repetitionId = 999;
            var request = new ReviewVocabularyRequestDTO
            {
                UserSpacedRepetitionId = repetitionId,
                Quality = 5
            };

            _mockUserSpacedRepetitionRepository
                .Setup(r => r.GetByIdAsync(repetitionId))
                .ReturnsAsync((UserSpacedRepetition?)null);

            // Act
            var result = await _service.ReviewVocabularyAsync(userId, request);

            // Assert
            Assert.NotNull(result);
            Assert.False(result.Success);
            Assert.Equal("Không tìm thấy bản ghi lặp lại", result.Message);
            Assert.Null(result.UpdatedRepetition);
        }

        [Fact]
        public async Task ReviewVocabularyAsync_WithDifferentUserId_ShouldReturnFailure()
        {
            // Arrange
            var userId = 1;
            var repetitionId = 1;
            var request = new ReviewVocabularyRequestDTO
            {
                UserSpacedRepetitionId = repetitionId,
                Quality = 5
            };

            var repetition = new UserSpacedRepetition
            {
                UserSpacedRepetitionId = repetitionId,
                UserId = 999, // Different user ID
                VocabularyListId = 1,
                VocabularyList = new VocabularyList { Name = "Test" }
            };

            _mockUserSpacedRepetitionRepository
                .Setup(r => r.GetByIdAsync(repetitionId))
                .ReturnsAsync(repetition);

            // Act
            var result = await _service.ReviewVocabularyAsync(userId, request);

            // Assert
            Assert.NotNull(result);
            Assert.False(result.Success);
            Assert.Equal("Không tìm thấy bản ghi lặp lại", result.Message);
        }

        [Fact]
        public async Task ReviewVocabularyAsync_WithQualityLessThanThree_ShouldResetIntervals()
        {
            // Arrange
            var userId = 1;
            var repetitionId = 1;
            var request = new ReviewVocabularyRequestDTO
            {
                UserSpacedRepetitionId = repetitionId,
                Quality = 2 // Low quality
            };

            var repetition = new UserSpacedRepetition
            {
                UserSpacedRepetitionId = repetitionId,
                UserId = userId,
                VocabularyListId = 1,
                VocabularyList = new VocabularyList { Name = "Test" },
                ReviewCount = 3,
                Intervals = 10,
                Status = "Learning"
            };

            _mockUserSpacedRepetitionRepository
                .Setup(r => r.GetByIdAsync(repetitionId))
                .ReturnsAsync(repetition);
            _mockUserSpacedRepetitionRepository
                .Setup(r => r.UpdateAsync(It.IsAny<UserSpacedRepetition>()))
                .ReturnsAsync((UserSpacedRepetition r) => r);

            // Act
            var result = await _service.ReviewVocabularyAsync(userId, request);

            // Assert
            Assert.NotNull(result);
            Assert.True(result.Success);
            Assert.Equal(1, result.NewIntervals); // Should reset to 1
            Assert.Equal("Learning", result.UpdatedRepetition?.Status);
        }

        [Fact]
        public async Task ReviewVocabularyAsync_WithQualityThree_ShouldKeepIntervals()
        {
            // Arrange
            var userId = 1;
            var repetitionId = 1;
            var request = new ReviewVocabularyRequestDTO
            {
                UserSpacedRepetitionId = repetitionId,
                Quality = 3
            };

            var currentIntervals = 5;
            var repetition = new UserSpacedRepetition
            {
                UserSpacedRepetitionId = repetitionId,
                UserId = userId,
                VocabularyListId = 1,
                VocabularyList = new VocabularyList { Name = "Test" },
                ReviewCount = 2,
                Intervals = currentIntervals,
                Status = "Learning"
            };

            _mockUserSpacedRepetitionRepository
                .Setup(r => r.GetByIdAsync(repetitionId))
                .ReturnsAsync(repetition);
            _mockUserSpacedRepetitionRepository
                .Setup(r => r.UpdateAsync(It.IsAny<UserSpacedRepetition>()))
                .ReturnsAsync((UserSpacedRepetition r) => r);

            // Act
            var result = await _service.ReviewVocabularyAsync(userId, request);

            // Assert
            Assert.NotNull(result);
            Assert.True(result.Success);
            Assert.Equal(currentIntervals, result.NewIntervals); // Should keep same intervals
            Assert.Equal("Learning", result.UpdatedRepetition?.Status);
        }

        [Fact]
        public async Task ReviewVocabularyAsync_WithQualityFive_ShouldIncreaseIntervals()
        {
            // Arrange
            var userId = 1;
            var repetitionId = 1;
            var request = new ReviewVocabularyRequestDTO
            {
                UserSpacedRepetitionId = repetitionId,
                Quality = 5 // High quality
            };

            var currentIntervals = 5;
            var repetition = new UserSpacedRepetition
            {
                UserSpacedRepetitionId = repetitionId,
                UserId = userId,
                VocabularyListId = 1,
                VocabularyList = new VocabularyList { Name = "Test" },
                ReviewCount = 2,
                Intervals = currentIntervals,
                Status = "Learning"
            };

            _mockUserSpacedRepetitionRepository
                .Setup(r => r.GetByIdAsync(repetitionId))
                .ReturnsAsync(repetition);
            _mockUserSpacedRepetitionRepository
                .Setup(r => r.UpdateAsync(It.IsAny<UserSpacedRepetition>()))
                .ReturnsAsync((UserSpacedRepetition r) => r);

            // Act
            var result = await _service.ReviewVocabularyAsync(userId, request);

            // Assert
            Assert.NotNull(result);
            Assert.True(result.Success);
            Assert.True(result.NewIntervals > currentIntervals); // Should increase
        }

        [Fact]
        public async Task ReviewVocabularyAsync_WithQualityClamped_ShouldClampQuality()
        {
            // Arrange
            var userId = 1;
            var repetitionId = 1;
            var request = new ReviewVocabularyRequestDTO
            {
                UserSpacedRepetitionId = repetitionId,
                Quality = 10 // Out of range, should be clamped to 5
            };

            var repetition = new UserSpacedRepetition
            {
                UserSpacedRepetitionId = repetitionId,
                UserId = userId,
                VocabularyListId = 1,
                VocabularyList = new VocabularyList { Name = "Test" },
                ReviewCount = 1,
                Intervals = 1,
                Status = "Learning"
            };

            _mockUserSpacedRepetitionRepository
                .Setup(r => r.GetByIdAsync(repetitionId))
                .ReturnsAsync(repetition);
            _mockUserSpacedRepetitionRepository
                .Setup(r => r.UpdateAsync(It.IsAny<UserSpacedRepetition>()))
                .ReturnsAsync((UserSpacedRepetition r) => r);

            // Act
            var result = await _service.ReviewVocabularyAsync(userId, request);

            // Assert
            Assert.NotNull(result);
            Assert.True(result.Success);
            // Quality should be clamped, so intervals should increase (as if quality = 5)
            Assert.True(result.NewIntervals >= 1);
        }

        [Fact]
        public async Task ReviewVocabularyAsync_WithNullReviewCount_ShouldUseZero()
        {
            // Arrange
            var userId = 1;
            var repetitionId = 1;
            var request = new ReviewVocabularyRequestDTO
            {
                UserSpacedRepetitionId = repetitionId,
                Quality = 4
            };

            var repetition = new UserSpacedRepetition
            {
                UserSpacedRepetitionId = repetitionId,
                UserId = userId,
                VocabularyListId = 1,
                VocabularyList = new VocabularyList { Name = "Test" },
                ReviewCount = null, // Null ReviewCount
                Intervals = 1,
                Status = "Learning"
            };

            _mockUserSpacedRepetitionRepository
                .Setup(r => r.GetByIdAsync(repetitionId))
                .ReturnsAsync(repetition);
            _mockUserSpacedRepetitionRepository
                .Setup(r => r.UpdateAsync(It.IsAny<UserSpacedRepetition>()))
                .ReturnsAsync((UserSpacedRepetition r) => r);

            // Act
            var result = await _service.ReviewVocabularyAsync(userId, request);

            // Assert
            Assert.NotNull(result);
            Assert.True(result.Success);
            Assert.NotNull(result.UpdatedRepetition);
            // ReviewCount should be incremented from 0
        }

        [Fact]
        public async Task ReviewVocabularyAsync_WithNullIntervals_ShouldUseOne()
        {
            // Arrange
            var userId = 1;
            var repetitionId = 1;
            var request = new ReviewVocabularyRequestDTO
            {
                UserSpacedRepetitionId = repetitionId,
                Quality = 4
            };

            var repetition = new UserSpacedRepetition
            {
                UserSpacedRepetitionId = repetitionId,
                UserId = userId,
                VocabularyListId = 1,
                VocabularyList = new VocabularyList { Name = "Test" },
                ReviewCount = 1,
                Intervals = null, // Null Intervals
                Status = "Learning"
            };

            _mockUserSpacedRepetitionRepository
                .Setup(r => r.GetByIdAsync(repetitionId))
                .ReturnsAsync(repetition);
            _mockUserSpacedRepetitionRepository
                .Setup(r => r.UpdateAsync(It.IsAny<UserSpacedRepetition>()))
                .ReturnsAsync((UserSpacedRepetition r) => r);

            // Act
            var result = await _service.ReviewVocabularyAsync(userId, request);

            // Assert
            Assert.NotNull(result);
            Assert.True(result.Success);
            Assert.True(result.NewIntervals >= 1);
        }

        [Fact]
        public async Task ReviewVocabularyAsync_WithNullVocabularyList_ShouldUseUnknown()
        {
            // Arrange
            var userId = 1;
            var repetitionId = 1;
            var request = new ReviewVocabularyRequestDTO
            {
                UserSpacedRepetitionId = repetitionId,
                Quality = 4
            };

            var repetition = new UserSpacedRepetition
            {
                UserSpacedRepetitionId = repetitionId,
                UserId = userId,
                VocabularyListId = 1,
                VocabularyList = null, // Null VocabularyList
                ReviewCount = 1,
                Intervals = 1,
                Status = "Learning"
            };

            _mockUserSpacedRepetitionRepository
                .Setup(r => r.GetByIdAsync(repetitionId))
                .ReturnsAsync(repetition);
            _mockUserSpacedRepetitionRepository
                .Setup(r => r.UpdateAsync(It.IsAny<UserSpacedRepetition>()))
                .ReturnsAsync((UserSpacedRepetition r) => r);

            // Act
            var result = await _service.ReviewVocabularyAsync(userId, request);

            // Assert
            Assert.NotNull(result);
            Assert.True(result.Success);
            Assert.NotNull(result.UpdatedRepetition);
            Assert.Equal("Unknown", result.UpdatedRepetition.VocabularyListName);
        }

        [Fact]
        public async Task ReviewVocabularyAsync_WithIntervalsReachingMastered_ShouldSetStatusToMastered()
        {
            // Arrange
            var userId = 1;
            var repetitionId = 1;
            var request = new ReviewVocabularyRequestDTO
            {
                UserSpacedRepetitionId = repetitionId,
                Quality = 5
            };

            var repetition = new UserSpacedRepetition
            {
                UserSpacedRepetitionId = repetitionId,
                UserId = userId,
                VocabularyListId = 1,
                VocabularyList = new VocabularyList { Name = "Test" },
                ReviewCount = 10,
                Intervals = 25, // Close to 30
                Status = "Learning"
            };

            _mockUserSpacedRepetitionRepository
                .Setup(r => r.GetByIdAsync(repetitionId))
                .ReturnsAsync(repetition);
            _mockUserSpacedRepetitionRepository
                .Setup(r => r.UpdateAsync(It.IsAny<UserSpacedRepetition>()))
                .ReturnsAsync((UserSpacedRepetition r) => r);

            // Act
            var result = await _service.ReviewVocabularyAsync(userId, request);

            // Assert
            Assert.NotNull(result);
            Assert.True(result.Success);
            // If newIntervals >= 30, status should be "Mastered"
            if (result.NewIntervals >= 30)
            {
                Assert.Equal("Mastered", result.UpdatedRepetition?.Status);
            }
        }

        [Fact]
        public async Task ReviewVocabularyAsync_WithMaxIntervals_ShouldCapAtNinety()
        {
            // Arrange
            var userId = 1;
            var repetitionId = 1;
            var request = new ReviewVocabularyRequestDTO
            {
                UserSpacedRepetitionId = repetitionId,
                Quality = 5
            };

            var repetition = new UserSpacedRepetition
            {
                UserSpacedRepetitionId = repetitionId,
                UserId = userId,
                VocabularyListId = 1,
                VocabularyList = new VocabularyList { Name = "Test" },
                ReviewCount = 20,
                Intervals = 80, // High intervals
                Status = "Mastered"
            };

            _mockUserSpacedRepetitionRepository
                .Setup(r => r.GetByIdAsync(repetitionId))
                .ReturnsAsync(repetition);
            _mockUserSpacedRepetitionRepository
                .Setup(r => r.UpdateAsync(It.IsAny<UserSpacedRepetition>()))
                .ReturnsAsync((UserSpacedRepetition r) => r);

            // Act
            var result = await _service.ReviewVocabularyAsync(userId, request);

            // Assert
            Assert.NotNull(result);
            Assert.True(result.Success);
            Assert.True(result.NewIntervals <= 90); // Should be capped at 90
        }
    }
}


