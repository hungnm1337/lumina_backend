using DataLayer.Models;
using Moq;
using RepositoryLayer.UnitOfWork;
using ServiceLayer.Vocabulary;
using System;
using System.Threading.Tasks;
using Xunit;

namespace Lumina.Tests
{
    public class RequestApprovalVocabularyListServiceTests
    {
        private readonly Mock<IUnitOfWork> _mockUnitOfWork;
        private readonly Mock<IVocabularyListRepository> _mockVocabularyListRepository;
        private readonly VocabularyListService _service;

        public RequestApprovalVocabularyListServiceTests()
        {
            _mockUnitOfWork = new Mock<IUnitOfWork>();
            _mockVocabularyListRepository = new Mock<IVocabularyListRepository>();
            _mockUnitOfWork.Setup(u => u.VocabularyLists).Returns(_mockVocabularyListRepository.Object);
            _mockUnitOfWork.Setup(u => u.CompleteAsync()).ReturnsAsync(1);
            _service = new VocabularyListService(_mockUnitOfWork.Object);
        }

        [Fact]
        public async Task RequestApprovalAsync_WithDraftStatus_ShouldReturnTrue()
        {
            // Arrange
            var listId = 1;
            var staffUserId = 2;
            var vocabularyList = new VocabularyList
            {
                VocabularyListId = listId,
                Name = "Draft List",
                Status = "Draft",
                IsPublic = false,
                MakeBy = 1,
                CreateAt = DateTime.UtcNow,
                IsDeleted = false
            };

            _mockVocabularyListRepository
                .Setup(r => r.FindByIdAsync(listId))
                .ReturnsAsync(vocabularyList);
            _mockVocabularyListRepository
                .Setup(r => r.UpdateAsync(It.IsAny<VocabularyList>()))
                .ReturnsAsync((VocabularyList l) => l);

            // Act
            var result = await _service.RequestApprovalAsync(listId, staffUserId);

            // Assert
            Assert.True(result);
            Assert.Equal("Pending", vocabularyList.Status);
            Assert.False(vocabularyList.IsPublic);
            Assert.Equal(staffUserId, vocabularyList.UpdatedBy);
            Assert.NotNull(vocabularyList.UpdateAt);
            _mockVocabularyListRepository.Verify(r => r.FindByIdAsync(listId), Times.Once);
            _mockVocabularyListRepository.Verify(r => r.UpdateAsync(It.IsAny<VocabularyList>()), Times.Once);
            _mockUnitOfWork.Verify(u => u.CompleteAsync(), Times.Once);
        }

        [Fact]
        public async Task RequestApprovalAsync_WithRejectedStatus_ShouldReturnTrue()
        {
            // Arrange
            var listId = 1;
            var staffUserId = 2;
            var vocabularyList = new VocabularyList
            {
                VocabularyListId = listId,
                Name = "Rejected List",
                Status = "Rejected",
                IsPublic = false,
                MakeBy = 1,
                CreateAt = DateTime.UtcNow,
                IsDeleted = false
            };

            _mockVocabularyListRepository
                .Setup(r => r.FindByIdAsync(listId))
                .ReturnsAsync(vocabularyList);
            _mockVocabularyListRepository
                .Setup(r => r.UpdateAsync(It.IsAny<VocabularyList>()))
                .ReturnsAsync((VocabularyList l) => l);

            // Act
            var result = await _service.RequestApprovalAsync(listId, staffUserId);

            // Assert
            Assert.True(result);
            Assert.Equal("Pending", vocabularyList.Status);
            _mockVocabularyListRepository.Verify(r => r.FindByIdAsync(listId), Times.Once);
            _mockVocabularyListRepository.Verify(r => r.UpdateAsync(It.IsAny<VocabularyList>()), Times.Once);
        }

        [Fact]
        public async Task RequestApprovalAsync_WithPublishedStatus_ShouldReturnFalse()
        {
            // Arrange
            var listId = 1;
            var staffUserId = 2;
            var vocabularyList = new VocabularyList
            {
                VocabularyListId = listId,
                Name = "Published List",
                Status = "Published",
                IsPublic = true
            };

            _mockVocabularyListRepository
                .Setup(r => r.FindByIdAsync(listId))
                .ReturnsAsync(vocabularyList);

            // Act
            var result = await _service.RequestApprovalAsync(listId, staffUserId);

            // Assert
            Assert.False(result);
            Assert.Equal("Published", vocabularyList.Status);
            _mockVocabularyListRepository.Verify(r => r.FindByIdAsync(listId), Times.Once);
            _mockVocabularyListRepository.Verify(r => r.UpdateAsync(It.IsAny<VocabularyList>()), Times.Never);
        }

        [Fact]
        public async Task RequestApprovalAsync_WithPendingStatus_ShouldReturnFalse()
        {
            // Arrange
            var listId = 1;
            var staffUserId = 2;
            var vocabularyList = new VocabularyList
            {
                VocabularyListId = listId,
                Name = "Pending List",
                Status = "Pending"
            };

            _mockVocabularyListRepository
                .Setup(r => r.FindByIdAsync(listId))
                .ReturnsAsync(vocabularyList);

            // Act
            var result = await _service.RequestApprovalAsync(listId, staffUserId);

            // Assert
            Assert.False(result);
            _mockVocabularyListRepository.Verify(r => r.FindByIdAsync(listId), Times.Once);
            _mockVocabularyListRepository.Verify(r => r.UpdateAsync(It.IsAny<VocabularyList>()), Times.Never);
        }

        [Fact]
        public async Task RequestApprovalAsync_WithNullVocabularyList_ShouldReturnFalse()
        {
            // Arrange
            var listId = 999;
            var staffUserId = 2;

            _mockVocabularyListRepository
                .Setup(r => r.FindByIdAsync(listId))
                .ReturnsAsync((VocabularyList?)null);

            // Act
            var result = await _service.RequestApprovalAsync(listId, staffUserId);

            // Assert
            Assert.False(result);
            _mockVocabularyListRepository.Verify(r => r.FindByIdAsync(listId), Times.Once);
            _mockVocabularyListRepository.Verify(r => r.UpdateAsync(It.IsAny<VocabularyList>()), Times.Never);
        }

        [Fact]
        public async Task RequestApprovalAsync_ShouldSetUpdatedByAndUpdateAt()
        {
            // Arrange
            var listId = 1;
            var staffUserId = 2;
            var vocabularyList = new VocabularyList
            {
                VocabularyListId = listId,
                Name = "Draft List",
                Status = "Draft",
                IsPublic = false,
                MakeBy = 1,
                CreateAt = DateTime.UtcNow,
                IsDeleted = false,
                UpdatedBy = null,
                UpdateAt = null
            };

            _mockVocabularyListRepository
                .Setup(r => r.FindByIdAsync(listId))
                .ReturnsAsync(vocabularyList);
            _mockVocabularyListRepository
                .Setup(r => r.UpdateAsync(It.IsAny<VocabularyList>()))
                .ReturnsAsync((VocabularyList l) => l);

            // Act
            var result = await _service.RequestApprovalAsync(listId, staffUserId);

            // Assert
            Assert.True(result);
            Assert.Equal(staffUserId, vocabularyList.UpdatedBy);
            Assert.NotNull(vocabularyList.UpdateAt);
        }
    }
}

