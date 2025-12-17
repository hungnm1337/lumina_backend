using Xunit;
using Moq;
using ServiceLayer.Vocabulary;
using RepositoryLayer.UnitOfWork;
using DataLayer.Models;
using System;
using System.Threading.Tasks;

namespace Lumina.Test.Services
{
    public class ReviewListAsyncUnitTest
    {
        private readonly Mock<IUnitOfWork> _mockUnitOfWork;
        private readonly Mock<IVocabularyListRepository> _mockVocabularyListRepository;
        private readonly VocabularyListService _service;

        public ReviewListAsyncUnitTest()
        {
            _mockUnitOfWork = new Mock<IUnitOfWork>();
            _mockVocabularyListRepository = new Mock<IVocabularyListRepository>();

            _mockUnitOfWork.Setup(u => u.VocabularyLists).Returns(_mockVocabularyListRepository.Object);

            _service = new VocabularyListService(_mockUnitOfWork.Object);
        }

        [Fact]
        public async Task ReviewListAsync_WhenListNotFound_ShouldReturnFalse()
        {
            // Arrange
            _mockVocabularyListRepository
                .Setup(repo => repo.FindByIdAsync(1))
                .ReturnsAsync((VocabularyList?)null);

            // Act
            var result = await _service.ReviewListAsync(1, true, null, 1);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task ReviewListAsync_WhenStatusIsNotPending_ShouldReturnFalse()
        {
            // Arrange
            var list = new VocabularyList
            {
                VocabularyListId = 1,
                Status = "Draft"
            };

            _mockVocabularyListRepository
                .Setup(repo => repo.FindByIdAsync(1))
                .ReturnsAsync(list);

            // Act
            var result = await _service.ReviewListAsync(1, true, null, 1);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task ReviewListAsync_WhenApproved_ShouldUpdateToPublished()
        {
            // Arrange
            var list = new VocabularyList
            {
                VocabularyListId = 1,
                Status = "Pending",
                IsPublic = false,
                RejectionReason = "Previous rejection"
            };

            _mockVocabularyListRepository
                .Setup(repo => repo.FindByIdAsync(1))
                .ReturnsAsync(list);

            _mockVocabularyListRepository
                .Setup(repo => repo.UpdateAsync(It.IsAny<VocabularyList>()))
                .ReturnsAsync((VocabularyList l) => l);

            _mockUnitOfWork
                .Setup(u => u.CompleteAsync())
                .ReturnsAsync(1);

            // Act
            var result = await _service.ReviewListAsync(1, true, null, 1);

            // Assert
            Assert.True(result);
            Assert.Equal("Published", list.Status);
            Assert.True(list.IsPublic);
            Assert.Null(list.RejectionReason);
            Assert.Equal(1, list.UpdatedBy);
            Assert.NotNull(list.UpdateAt);

            _mockVocabularyListRepository.Verify(repo => repo.UpdateAsync(list), Times.Once);
            _mockUnitOfWork.Verify(u => u.CompleteAsync(), Times.Once);
        }

        [Fact]
        public async Task ReviewListAsync_WhenRejected_ShouldUpdateToRejected()
        {
            // Arrange
            var list = new VocabularyList
            {
                VocabularyListId = 1,
                Status = "Pending",
                IsPublic = false
            };

            _mockVocabularyListRepository
                .Setup(repo => repo.FindByIdAsync(1))
                .ReturnsAsync(list);

            _mockVocabularyListRepository
                .Setup(repo => repo.UpdateAsync(It.IsAny<VocabularyList>()))
                .ReturnsAsync((VocabularyList l) => l);

            _mockUnitOfWork
                .Setup(u => u.CompleteAsync())
                .ReturnsAsync(1);

            // Act
            var result = await _service.ReviewListAsync(1, false, "Not suitable", 1);

            // Assert
            Assert.True(result);
            Assert.Equal("Rejected", list.Status);
            Assert.False(list.IsPublic);
            Assert.Equal("Not suitable", list.RejectionReason);
            Assert.Equal(1, list.UpdatedBy);
            Assert.NotNull(list.UpdateAt);

            _mockVocabularyListRepository.Verify(repo => repo.UpdateAsync(list), Times.Once);
            _mockUnitOfWork.Verify(u => u.CompleteAsync(), Times.Once);
        }
    }
}



















