using Xunit;
using Moq;
using ServiceLayer.Vocabulary;
using RepositoryLayer.UnitOfWork;
using RepositoryLayer.UserSpacedRepetition;
using DataLayer.DTOs.Vocabulary;
using DataLayer.Models;
using System;
using System.Threading.Tasks;

namespace Lumina.Test.Services
{
    public class ReviewVocabularyAsyncUnitTest
    {
        private readonly Mock<IUnitOfWork> _mockUnitOfWork;
        private readonly Mock<IUserSpacedRepetitionRepository> _mockRepetitionRepository;
        private readonly Mock<IVocabularyListRepository> _mockVocabularyListRepository;
        private readonly SpacedRepetitionService _service;

        public ReviewVocabularyAsyncUnitTest()
        {
            _mockUnitOfWork = new Mock<IUnitOfWork>();
            _mockRepetitionRepository = new Mock<IUserSpacedRepetitionRepository>();
            _mockVocabularyListRepository = new Mock<IVocabularyListRepository>();

            _mockUnitOfWork.Setup(u => u.UserSpacedRepetitions).Returns(_mockRepetitionRepository.Object);
            _mockUnitOfWork.Setup(u => u.VocabularyLists).Returns(_mockVocabularyListRepository.Object);

            _service = new SpacedRepetitionService(_mockUnitOfWork.Object);
        }

        [Fact]
        public async Task ReviewVocabularyAsync_WhenRequestIsNull_ShouldReturnFailure()
        {
            // Act
            var result = await _service.ReviewVocabularyAsync(1, null!);

            // Assert
            Assert.NotNull(result);
            Assert.False(result.Success);
            Assert.Contains("cannot be null", result.Message);
        }

        [Fact]
        public async Task ReviewVocabularyAsync_WhenUserSpacedRepetitionIdNotFound_ShouldReturnFailure()
        {
            // Arrange
            var request = new ReviewVocabularyRequestDTO
            {
                UserSpacedRepetitionId = 1,
                Quality = 4
            };

            _mockRepetitionRepository
                .Setup(repo => repo.GetByIdAsync(1))
                .ReturnsAsync((UserSpacedRepetition?)null);

            // Act
            var result = await _service.ReviewVocabularyAsync(1, request);

            // Assert
            Assert.False(result.Success);
            Assert.Contains("Không tìm thấy", result.Message);
        }

        [Fact]
        public async Task ReviewVocabularyAsync_WhenUserSpacedRepetitionIdMismatch_ShouldReturnFailure()
        {
            // Arrange
            var request = new ReviewVocabularyRequestDTO
            {
                UserSpacedRepetitionId = 1,
                Quality = 4
            };

            var repetition = new UserSpacedRepetition
            {
                UserSpacedRepetitionId = 1,
                UserId = 2, // Different user
                VocabularyList = new VocabularyList { Name = "Test" }
            };

            _mockRepetitionRepository
                .Setup(repo => repo.GetByIdAsync(1))
                .ReturnsAsync(repetition);

            // Act
            var result = await _service.ReviewVocabularyAsync(1, request);

            // Assert
            Assert.False(result.Success);
        }

        [Fact]
        public async Task ReviewVocabularyAsync_WhenVocabularyIdProvided_ShouldCreateOrUpdate()
        {
            // Arrange
            var request = new ReviewVocabularyRequestDTO
            {
                VocabularyId = 1,
                VocabularyListId = 1,
                Quality = 4
            };

            var vocabularyList = new VocabularyList
            {
                VocabularyListId = 1,
                Name = "Test List"
            };

            _mockRepetitionRepository
                .Setup(repo => repo.GetByUserAndVocabularyAsync(1, 1))
                .ReturnsAsync((UserSpacedRepetition?)null);

            _mockVocabularyListRepository
                .Setup(repo => repo.FindByIdAsync(1))
                .ReturnsAsync(vocabularyList);

            _mockRepetitionRepository
                .Setup(repo => repo.AddAsync(It.IsAny<UserSpacedRepetition>()))
                .ReturnsAsync((UserSpacedRepetition r) => r);

            _mockRepetitionRepository
                .Setup(repo => repo.UpdateAsync(It.IsAny<UserSpacedRepetition>()))
                .ReturnsAsync((UserSpacedRepetition r) => r);

            _mockUnitOfWork
                .Setup(u => u.CompleteAsync())
                .ReturnsAsync(1);

            // Act
            var result = await _service.ReviewVocabularyAsync(1, request);

            // Assert
            Assert.True(result.Success);
        }

        [Fact]
        public async Task ReviewVocabularyAsync_WhenMissingFields_ShouldReturnFailure()
        {
            // Arrange
            var request = new ReviewVocabularyRequestDTO
            {
                Quality = 4
            };

            // Act
            var result = await _service.ReviewVocabularyAsync(1, request);

            // Assert
            Assert.False(result.Success);
            Assert.Contains("Thiếu thông tin", result.Message);
        }

        [Fact]
        public async Task ReviewVocabularyAsync_WhenQualityIsLow_ShouldResetIntervals()
        {
            // Arrange
            var request = new ReviewVocabularyRequestDTO
            {
                UserSpacedRepetitionId = 1,
                Quality = 2
            };

            var repetition = new UserSpacedRepetition
            {
                UserSpacedRepetitionId = 1,
                UserId = 1,
                ReviewCount = 1,
                Intervals = 5,
                VocabularyList = new VocabularyList { Name = "Test" }
            };

            _mockRepetitionRepository
                .Setup(repo => repo.GetByIdAsync(1))
                .ReturnsAsync(repetition);

            _mockRepetitionRepository
                .Setup(repo => repo.UpdateAsync(It.IsAny<UserSpacedRepetition>()))
                .ReturnsAsync((UserSpacedRepetition r) => r);

            _mockUnitOfWork
                .Setup(u => u.CompleteAsync())
                .ReturnsAsync(1);

            // Act
            var result = await _service.ReviewVocabularyAsync(1, request);

            // Assert
            Assert.True(result.Success);
            Assert.Equal(1, repetition.Intervals);
        }

        [Fact]
        public async Task ReviewVocabularyAsync_WhenQualityExceedsMax_ShouldClamp()
        {
            // Arrange
            var request = new ReviewVocabularyRequestDTO
            {
                UserSpacedRepetitionId = 1,
                Quality = 10 // Exceeds max of 5
            };

            var repetition = new UserSpacedRepetition
            {
                UserSpacedRepetitionId = 1,
                UserId = 1,
                ReviewCount = 2,
                Intervals = 1,
                VocabularyList = new VocabularyList { Name = "Test" }
            };

            _mockRepetitionRepository
                .Setup(repo => repo.GetByIdAsync(1))
                .ReturnsAsync(repetition);

            _mockRepetitionRepository
                .Setup(repo => repo.UpdateAsync(It.IsAny<UserSpacedRepetition>()))
                .ReturnsAsync((UserSpacedRepetition r) => r);

            _mockUnitOfWork
                .Setup(u => u.CompleteAsync())
                .ReturnsAsync(1);

            // Act
            var result = await _service.ReviewVocabularyAsync(1, request);

            // Assert
            Assert.True(result.Success);
        }

        [Fact]
        public async Task ReviewVocabularyAsync_WhenQualityIs3AndReviewCountIs1_ShouldSetIntervalsTo6()
        {
            // Arrange
            var request = new ReviewVocabularyRequestDTO
            {
                UserSpacedRepetitionId = 1,
                Quality = 3
            };

            var repetition = new UserSpacedRepetition
            {
                UserSpacedRepetitionId = 1,
                UserId = 1,
                ReviewCount = 1,
                Intervals = 1,
                VocabularyList = new VocabularyList { Name = "Test" }
            };

            _mockRepetitionRepository
                .Setup(repo => repo.GetByIdAsync(1))
                .ReturnsAsync(repetition);

            _mockRepetitionRepository
                .Setup(repo => repo.UpdateAsync(It.IsAny<UserSpacedRepetition>()))
                .ReturnsAsync((UserSpacedRepetition r) => r);

            _mockUnitOfWork
                .Setup(u => u.CompleteAsync())
                .ReturnsAsync(1);

            // Act
            var result = await _service.ReviewVocabularyAsync(1, request);

            // Assert
            Assert.True(result.Success);
            Assert.Equal(6, result.NewIntervals);
        }
    }
}

