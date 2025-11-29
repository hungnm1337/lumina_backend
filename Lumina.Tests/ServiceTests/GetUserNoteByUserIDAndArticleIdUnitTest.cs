using Xunit;
using Moq;
using ServiceLayer.UserNote;
using RepositoryLayer.UserNote;
using DataLayer.DTOs.UserNote;
using System;
using System.Threading.Tasks;

namespace Lumina.Test.Services
{
    public class GetUserNoteByUserIDAndArticleIdUnitTest
    {
        private readonly Mock<IUserNoteRepository> _mockUserNoteRepository;
        private readonly UserNoteService _service;

        public GetUserNoteByUserIDAndArticleIdUnitTest()
        {
            _mockUserNoteRepository = new Mock<IUserNoteRepository>();

            _service = new UserNoteService(
                _mockUserNoteRepository.Object
            );
        }

        #region Test Cases for Invalid UserId

        [Fact]
        public async Task GetUserNoteByUserIDAndArticleId_WhenUserIdIsNegative_ShouldThrowArgumentException()
        {
            // Arrange
            int userId = -1;
            int articleId = 1;
            int sectionId = 1;

            // Act & Assert
            var exception = await Assert.ThrowsAsync<ArgumentException>(
                async () => await _service.GetUserNoteByUserIDAndArticleId(userId, articleId, sectionId)
            );

            Assert.Equal("userId", exception.ParamName);
            Assert.Contains("User ID must be greater than zero", exception.Message);

            // Verify repository is never called
            _mockUserNoteRepository.Verify(
                repo => repo.GetUserNoteByUserIDAndArticleId(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>()),
                Times.Never
            );
        }

        [Fact]
        public async Task GetUserNoteByUserIDAndArticleId_WhenUserIdIsZero_ShouldThrowArgumentException()
        {
            // Arrange
            int userId = 0;
            int articleId = 1;
            int sectionId = 1;

            // Act & Assert
            var exception = await Assert.ThrowsAsync<ArgumentException>(
                async () => await _service.GetUserNoteByUserIDAndArticleId(userId, articleId, sectionId)
            );

            Assert.Equal("userId", exception.ParamName);
            Assert.Contains("User ID must be greater than zero", exception.Message);

            // Verify repository is never called
            _mockUserNoteRepository.Verify(
                repo => repo.GetUserNoteByUserIDAndArticleId(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>()),
                Times.Never
            );
        }

        #endregion

        #region Test Cases for Invalid ArticleId

        [Fact]
        public async Task GetUserNoteByUserIDAndArticleId_WhenArticleIdIsNegative_ShouldThrowArgumentException()
        {
            // Arrange
            int userId = 1;
            int articleId = -1;
            int sectionId = 1;

            // Act & Assert
            var exception = await Assert.ThrowsAsync<ArgumentException>(
                async () => await _service.GetUserNoteByUserIDAndArticleId(userId, articleId, sectionId)
            );

            Assert.Equal("articleId", exception.ParamName);
            Assert.Contains("Article ID must be greater than zero", exception.Message);

            // Verify repository is never called
            _mockUserNoteRepository.Verify(
                repo => repo.GetUserNoteByUserIDAndArticleId(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>()),
                Times.Never
            );
        }

        [Fact]
        public async Task GetUserNoteByUserIDAndArticleId_WhenArticleIdIsZero_ShouldThrowArgumentException()
        {
            // Arrange
            int userId = 1;
            int articleId = 0;
            int sectionId = 1;

            // Act & Assert
            var exception = await Assert.ThrowsAsync<ArgumentException>(
                async () => await _service.GetUserNoteByUserIDAndArticleId(userId, articleId, sectionId)
            );

            Assert.Equal("articleId", exception.ParamName);
            Assert.Contains("Article ID must be greater than zero", exception.Message);

            // Verify repository is never called
            _mockUserNoteRepository.Verify(
                repo => repo.GetUserNoteByUserIDAndArticleId(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>()),
                Times.Never
            );
        }

        #endregion

        #region Test Cases for Invalid SectionId

        [Fact]
        public async Task GetUserNoteByUserIDAndArticleId_WhenSectionIdIsNegative_ShouldThrowArgumentException()
        {
            // Arrange
            int userId = 1;
            int articleId = 1;
            int sectionId = -1;

            // Act & Assert
            var exception = await Assert.ThrowsAsync<ArgumentException>(
                async () => await _service.GetUserNoteByUserIDAndArticleId(userId, articleId, sectionId)
            );

            Assert.Equal("sectionId", exception.ParamName);
            Assert.Contains("Section ID must be greater than zero", exception.Message);

            // Verify repository is never called
            _mockUserNoteRepository.Verify(
                repo => repo.GetUserNoteByUserIDAndArticleId(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>()),
                Times.Never
            );
        }

        [Fact]
        public async Task GetUserNoteByUserIDAndArticleId_WhenSectionIdIsZero_ShouldThrowArgumentException()
        {
            // Arrange
            int userId = 1;
            int articleId = 1;
            int sectionId = 0;

            // Act & Assert
            var exception = await Assert.ThrowsAsync<ArgumentException>(
                async () => await _service.GetUserNoteByUserIDAndArticleId(userId, articleId, sectionId)
            );

            Assert.Equal("sectionId", exception.ParamName);
            Assert.Contains("Section ID must be greater than zero", exception.Message);

            // Verify repository is never called
            _mockUserNoteRepository.Verify(
                repo => repo.GetUserNoteByUserIDAndArticleId(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>()),
                Times.Never
            );
        }

        #endregion

        #region Test Cases for Valid Parameters

        [Fact]
        public async Task GetUserNoteByUserIDAndArticleId_WhenAllParametersAreValidAndExists_ShouldReturnUserNoteResponseDTO()
        {
            // Arrange
            int userId = 1;
            int articleId = 1;
            int sectionId = 1;
            var expectedResult = new UserNoteResponseDTO
            {
                NoteId = 1,
                UserId = 1,
                User = "Test User",
                ArticleId = 1,
                Article = "Test Article",
                SectionId = 1,
                Section = "Test Section",
                NoteContent = "Test note content",
                CreateAt = DateTime.UtcNow,
                UpdateAt = null
            };

            _mockUserNoteRepository
                .Setup(repo => repo.GetUserNoteByUserIDAndArticleId(userId, articleId, sectionId))
                .ReturnsAsync(expectedResult);

            // Act
            var result = await _service.GetUserNoteByUserIDAndArticleId(userId, articleId, sectionId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(expectedResult.NoteId, result.NoteId);
            Assert.Equal(expectedResult.UserId, result.UserId);
            Assert.Equal(expectedResult.ArticleId, result.ArticleId);
            Assert.Equal(expectedResult.SectionId, result.SectionId);
            Assert.Equal(expectedResult.NoteContent, result.NoteContent);
            Assert.Equal(expectedResult.Article, result.Article);
            Assert.Equal(expectedResult.Section, result.Section);

            // Verify repository is called exactly once with correct parameters
            _mockUserNoteRepository.Verify(
                repo => repo.GetUserNoteByUserIDAndArticleId(userId, articleId, sectionId),
                Times.Once
            );
        }

        [Fact]
        public async Task GetUserNoteByUserIDAndArticleId_WhenAllParametersAreValidButNotFound_ShouldReturnNull()
        {
            // Arrange
            int userId = 1;
            int articleId = 999;
            int sectionId = 999;

            _mockUserNoteRepository
                .Setup(repo => repo.GetUserNoteByUserIDAndArticleId(userId, articleId, sectionId))
                .ReturnsAsync((UserNoteResponseDTO?)null);

            // Act
            var result = await _service.GetUserNoteByUserIDAndArticleId(userId, articleId, sectionId);

            // Assert
            Assert.Null(result);

            // Verify repository is called exactly once
            _mockUserNoteRepository.Verify(
                repo => repo.GetUserNoteByUserIDAndArticleId(userId, articleId, sectionId),
                Times.Once
            );
        }

        [Fact]
        public async Task GetUserNoteByUserIDAndArticleId_WhenAllParametersAreLarge_ShouldReturnUserNoteResponseDTO()
        {
            // Arrange
            int userId = int.MaxValue;
            int articleId = int.MaxValue;
            int sectionId = int.MaxValue;
            var expectedResult = new UserNoteResponseDTO
            {
                NoteId = 1,
                UserId = int.MaxValue,
                User = "Large User ID",
                ArticleId = int.MaxValue,
                Article = "Large Article ID",
                SectionId = int.MaxValue,
                Section = "Large Section ID",
                NoteContent = "Test note content",
                CreateAt = DateTime.UtcNow,
                UpdateAt = DateTime.UtcNow
            };

            _mockUserNoteRepository
                .Setup(repo => repo.GetUserNoteByUserIDAndArticleId(userId, articleId, sectionId))
                .ReturnsAsync(expectedResult);

            // Act
            var result = await _service.GetUserNoteByUserIDAndArticleId(userId, articleId, sectionId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(expectedResult.UserId, result.UserId);
            Assert.Equal(expectedResult.ArticleId, result.ArticleId);
            Assert.Equal(expectedResult.SectionId, result.SectionId);
            Assert.NotNull(result.UpdateAt);

            // Verify repository is called exactly once
            _mockUserNoteRepository.Verify(
                repo => repo.GetUserNoteByUserIDAndArticleId(userId, articleId, sectionId),
                Times.Once
            );
        }

        #endregion

        #region Test Cases - Repository Exception Handling

        [Fact]
        public async Task GetUserNoteByUserIDAndArticleId_WhenRepositoryThrowsException_ShouldPropagateException()
        {
            // Arrange
            int userId = 1;
            int articleId = 1;
            int sectionId = 1;
            var exceptionMessage = "Database connection error";

            _mockUserNoteRepository
                .Setup(repo => repo.GetUserNoteByUserIDAndArticleId(userId, articleId, sectionId))
                .ThrowsAsync(new Exception(exceptionMessage));

            // Act & Assert
            var exception = await Assert.ThrowsAsync<Exception>(
                async () => await _service.GetUserNoteByUserIDAndArticleId(userId, articleId, sectionId)
            );

            Assert.Equal(exceptionMessage, exception.Message);

            // Verify repository is called exactly once
            _mockUserNoteRepository.Verify(
                repo => repo.GetUserNoteByUserIDAndArticleId(userId, articleId, sectionId),
                Times.Once
            );
        }

        #endregion
    }
}

