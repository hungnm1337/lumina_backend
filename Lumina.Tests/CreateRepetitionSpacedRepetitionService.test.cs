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
    public class CreateRepetitionSpacedRepetitionServiceTests
        {
        private readonly Mock<IUnitOfWork> _mockUnitOfWork;
        private readonly Mock<IUserSpacedRepetitionRepository> _mockUserSpacedRepetitionRepository;
        private readonly Mock<IVocabularyListRepository> _mockVocabularyListRepository;
        private readonly SpacedRepetitionService _service;

        public CreateRepetitionSpacedRepetitionServiceTests()
        {
            _mockUnitOfWork = new Mock<IUnitOfWork>();
            _mockUserSpacedRepetitionRepository = new Mock<IUserSpacedRepetitionRepository>();
            _mockVocabularyListRepository = new Mock<IVocabularyListRepository>();
            _mockUnitOfWork.Setup(u => u.UserSpacedRepetitions).Returns(_mockUserSpacedRepetitionRepository.Object);
            _mockUnitOfWork.Setup(u => u.VocabularyLists).Returns(_mockVocabularyListRepository.Object);
            _mockUnitOfWork.Setup(u => u.CompleteAsync()).ReturnsAsync(1);
            _service = new SpacedRepetitionService(_mockUnitOfWork.Object);
        }

        [Fact]
        public async Task CreateRepetitionAsync_WithValidIds_ShouldCreateNewRepetition()
        {
            // Arrange
            var userId = 1;
            var vocabularyListId = 1;
            var vocabularyList = new VocabularyList
            {
                VocabularyListId = vocabularyListId,
                Name = "Test List"
            };

            _mockUserSpacedRepetitionRepository
                .Setup(r => r.ExistsAsync(userId, vocabularyListId))
                .ReturnsAsync(false);
            _mockVocabularyListRepository
                .Setup(r => r.FindByIdAsync(vocabularyListId))
                .ReturnsAsync(vocabularyList);
            _mockUserSpacedRepetitionRepository
                .Setup(r => r.AddAsync(It.IsAny<UserSpacedRepetition>()))
                .ReturnsAsync((UserSpacedRepetition r) => 
                {
                    r.UserSpacedRepetitionId = 1; // Simulate EF Core assigning ID
                    return r;
                });

            // Act
            var result = await _service.CreateRepetitionAsync(userId, vocabularyListId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(userId, result.UserId);
            Assert.Equal(vocabularyListId, result.VocabularyListId);
            Assert.Equal("Test List", result.VocabularyListName);
            Assert.Equal(0, result.ReviewCount);
            Assert.Equal(1, result.Intervals);
            Assert.Equal("New", result.Status);
            Assert.False(result.IsDue);
            Assert.Equal(1, result.DaysUntilReview);
            _mockUserSpacedRepetitionRepository.Verify(r => r.ExistsAsync(userId, vocabularyListId), Times.Once);
            _mockVocabularyListRepository.Verify(r => r.FindByIdAsync(vocabularyListId), Times.Once);
            _mockUserSpacedRepetitionRepository.Verify(r => r.AddAsync(It.IsAny<UserSpacedRepetition>()), Times.Once);
            _mockUnitOfWork.Verify(u => u.CompleteAsync(), Times.Once);
        }

        [Fact]
        public async Task CreateRepetitionAsync_WithExistingRepetition_ShouldReturnExisting()
        {
            // Arrange
            var userId = 1;
            var vocabularyListId = 1;
            var now = DateTime.UtcNow;
            var vocabularyList = new VocabularyList
            {
                VocabularyListId = vocabularyListId,
                Name = "Test List"
            };
            var existingItem = new UserSpacedRepetition
            {
                UserSpacedRepetitionId = 1,
                UserId = userId,
                VocabularyListId = vocabularyListId,
                VocabularyList = vocabularyList,
                LastReviewedAt = now,
                NextReviewAt = now.AddDays(1),
                ReviewCount = 1,
                Intervals = 1,
                Status = "Learning"
            };

            _mockUserSpacedRepetitionRepository
                .Setup(r => r.ExistsAsync(userId, vocabularyListId))
                .ReturnsAsync(true);
            _mockUserSpacedRepetitionRepository
                .Setup(r => r.GetByUserAndListAsync(userId, vocabularyListId))
                .ReturnsAsync(existingItem);

            // Act
            var result = await _service.CreateRepetitionAsync(userId, vocabularyListId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(1, result.UserSpacedRepetitionId);
            Assert.Equal("Test List", result.VocabularyListName);
            _mockUserSpacedRepetitionRepository.Verify(r => r.ExistsAsync(userId, vocabularyListId), Times.Once);
            _mockUserSpacedRepetitionRepository.Verify(r => r.GetByUserAndListAsync(userId, vocabularyListId), Times.Once);
            _mockUserSpacedRepetitionRepository.Verify(r => r.AddAsync(It.IsAny<UserSpacedRepetition>()), Times.Never);
        }

        [Fact]
        public async Task CreateRepetitionAsync_WithExistingButNullReturn_ShouldThrowException()
        {
            // Arrange
            var userId = 1;
            var vocabularyListId = 1;

            _mockUserSpacedRepetitionRepository
                .Setup(r => r.ExistsAsync(userId, vocabularyListId))
                .ReturnsAsync(true);
            _mockUserSpacedRepetitionRepository
                .Setup(r => r.GetByUserAndListAsync(userId, vocabularyListId))
                .ReturnsAsync((UserSpacedRepetition?)null);

            // Act & Assert
            await Assert.ThrowsAsync<Exception>(async () =>
            {
                await _service.CreateRepetitionAsync(userId, vocabularyListId);
            });
        }

        [Fact]
        public async Task CreateRepetitionAsync_WithNonExistentVocabularyList_ShouldThrowKeyNotFoundException()
        {
            // Arrange
            var userId = 1;
            var vocabularyListId = 999;

            _mockUserSpacedRepetitionRepository
                .Setup(r => r.ExistsAsync(userId, vocabularyListId))
                .ReturnsAsync(false);
            _mockVocabularyListRepository
                .Setup(r => r.FindByIdAsync(vocabularyListId))
                .ReturnsAsync((VocabularyList?)null);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<KeyNotFoundException>(async () =>
            {
                await _service.CreateRepetitionAsync(userId, vocabularyListId);
            });

            Assert.Equal("Vocabulary list not found.", exception.Message);
            _mockUserSpacedRepetitionRepository.Verify(r => r.AddAsync(It.IsAny<UserSpacedRepetition>()), Times.Never);
        }

        [Fact]
        public async Task CreateRepetitionAsync_ShouldSetCorrectInitialValues()
        {
            // Arrange
            var userId = 1;
            var vocabularyListId = 1;
            var vocabularyList = new VocabularyList
            {
                VocabularyListId = vocabularyListId,
                Name = "Test List"
            };

            _mockUserSpacedRepetitionRepository
                .Setup(r => r.ExistsAsync(userId, vocabularyListId))
                .ReturnsAsync(false);
            _mockVocabularyListRepository
                .Setup(r => r.FindByIdAsync(vocabularyListId))
                .ReturnsAsync(vocabularyList);
            _mockUserSpacedRepetitionRepository
                .Setup(r => r.AddAsync(It.IsAny<UserSpacedRepetition>()))
                .ReturnsAsync((UserSpacedRepetition r) => 
                {
                    r.UserSpacedRepetitionId = 1;
                    return r;
                });

            // Act
            var result = await _service.CreateRepetitionAsync(userId, vocabularyListId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(0, result.ReviewCount);
            Assert.Equal(1, result.Intervals);
            Assert.Equal("New", result.Status);
            Assert.False(result.IsDue);
            Assert.Equal(1, result.DaysUntilReview);
        }
    }
}


