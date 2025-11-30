using Xunit;
using Moq;
using ServiceLayer.Vocabulary;
using RepositoryLayer.UnitOfWork;
using RepositoryLayer.UserSpacedRepetition;
using DataLayer.Models;
using System;
using System.Threading.Tasks;

namespace Lumina.Test.Services
{
    public class GetByUserAndListAsyncUnitTest
    {
        private readonly Mock<IUnitOfWork> _mockUnitOfWork;
        private readonly Mock<IUserSpacedRepetitionRepository> _mockRepetitionRepository;
        private readonly SpacedRepetitionService _service;

        public GetByUserAndListAsyncUnitTest()
        {
            _mockUnitOfWork = new Mock<IUnitOfWork>();
            _mockRepetitionRepository = new Mock<IUserSpacedRepetitionRepository>();

            _mockUnitOfWork.Setup(u => u.UserSpacedRepetitions).Returns(_mockRepetitionRepository.Object);

            _service = new SpacedRepetitionService(_mockUnitOfWork.Object);
        }

        [Fact]
        public async Task GetByUserAndListAsync_WhenItemExists_ShouldReturnItem()
        {
            // Arrange
            var item = new UserSpacedRepetition
            {
                UserSpacedRepetitionId = 1,
                UserId = 1,
                VocabularyId = 1,
                VocabularyListId = 1,
                NextReviewAt = DateTime.UtcNow.AddDays(1),
                ReviewCount = 1,
                Intervals = 1,
                VocabularyList = new VocabularyList { Name = "Test List" }
            };

            _mockRepetitionRepository
                .Setup(repo => repo.GetByUserAndListAsync(1, 1))
                .ReturnsAsync(item);

            // Act
            var result = await _service.GetByUserAndListAsync(1, 1);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(1, result.UserSpacedRepetitionId);
        }

        [Fact]
        public async Task GetByUserAndListAsync_WhenItemNotFound_ShouldReturnNull()
        {
            // Arrange
            _mockRepetitionRepository
                .Setup(repo => repo.GetByUserAndListAsync(1, 1))
                .ReturnsAsync((UserSpacedRepetition?)null);

            // Act
            var result = await _service.GetByUserAndListAsync(1, 1);

            // Assert
            Assert.Null(result);
        }
    }
}

