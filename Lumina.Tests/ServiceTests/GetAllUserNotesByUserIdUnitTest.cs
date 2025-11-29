using Xunit;
using Moq;
using ServiceLayer.UserNote;
using RepositoryLayer.UserNote;
using DataLayer.DTOs.UserNote;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Lumina.Test.Services
{
    public class GetAllUserNotesByUserIdUnitTest
    {
        private readonly Mock<IUserNoteRepository> _mockUserNoteRepository;
        private readonly UserNoteService _service;

        public GetAllUserNotesByUserIdUnitTest()
        {
            _mockUserNoteRepository = new Mock<IUserNoteRepository>();

            _service = new UserNoteService(
                _mockUserNoteRepository.Object
            );
        }

        #region Test Cases for Invalid UserId

        [Fact]
        public async Task GetAllUserNotesByUserId_WhenUserIdIsNegative_ShouldThrowArgumentException()
        {
            // Arrange
            int userId = -1;

            // Act & Assert
            var exception = await Assert.ThrowsAsync<ArgumentException>(
                async () => await _service.GetAllUserNotesByUserId(userId)
            );

            Assert.Equal("userId", exception.ParamName);
            Assert.Contains("User ID must be greater than zero", exception.Message);

            // Verify repository is never called
            _mockUserNoteRepository.Verify(
                repo => repo.GetAllUserNotesByUserId(It.IsAny<int>()),
                Times.Never
            );
        }

        [Fact]
        public async Task GetAllUserNotesByUserId_WhenUserIdIsZero_ShouldThrowArgumentException()
        {
            // Arrange
            int userId = 0;

            // Act & Assert
            var exception = await Assert.ThrowsAsync<ArgumentException>(
                async () => await _service.GetAllUserNotesByUserId(userId)
            );

            Assert.Equal("userId", exception.ParamName);
            Assert.Contains("User ID must be greater than zero", exception.Message);

            // Verify repository is never called
            _mockUserNoteRepository.Verify(
                repo => repo.GetAllUserNotesByUserId(It.IsAny<int>()),
                Times.Never
            );
        }

        #endregion

        #region Test Cases for Valid UserId

        [Fact]
        public async Task GetAllUserNotesByUserId_WhenUserIdIsValidAndHasData_ShouldReturnList()
        {
            // Arrange
            int userId = 1;
            var expectedResult = new List<UserNoteResponseDTO>
            {
                new UserNoteResponseDTO
                {
                    NoteId = 1,
                    UserId = 1,
                    User = "Test User",
                    ArticleId = 1,
                    Article = "Test Article",
                    SectionId = 1,
                    Section = "Test Section",
                    NoteContent = "Test note content 1",
                    CreateAt = DateTime.UtcNow,
                    UpdateAt = null
                },
                new UserNoteResponseDTO
                {
                    NoteId = 2,
                    UserId = 1,
                    User = "Test User",
                    ArticleId = 2,
                    Article = "Test Article 2",
                    SectionId = 2,
                    Section = "Test Section 2",
                    NoteContent = "Test note content 2",
                    CreateAt = DateTime.UtcNow.AddDays(-1),
                    UpdateAt = DateTime.UtcNow
                }
            };

            _mockUserNoteRepository
                .Setup(repo => repo.GetAllUserNotesByUserId(userId))
                .ReturnsAsync(expectedResult);

            // Act
            var result = await _service.GetAllUserNotesByUserId(userId);

            // Assert
            Assert.NotNull(result);
            var resultList = result.ToList();
            Assert.Equal(2, resultList.Count);
            Assert.Equal(expectedResult[0].NoteId, resultList[0].NoteId);
            Assert.Equal(expectedResult[0].NoteContent, resultList[0].NoteContent);
            Assert.Equal(expectedResult[1].NoteId, resultList[1].NoteId);
            Assert.Equal(expectedResult[1].NoteContent, resultList[1].NoteContent);

            // Verify repository is called exactly once with correct userId
            _mockUserNoteRepository.Verify(
                repo => repo.GetAllUserNotesByUserId(userId),
                Times.Once
            );
        }

        [Fact]
        public async Task GetAllUserNotesByUserId_WhenUserIdIsValidButNoData_ShouldReturnEmptyList()
        {
            // Arrange
            int userId = 1;
            var expectedResult = new List<UserNoteResponseDTO>();

            _mockUserNoteRepository
                .Setup(repo => repo.GetAllUserNotesByUserId(userId))
                .ReturnsAsync(expectedResult);

            // Act
            var result = await _service.GetAllUserNotesByUserId(userId);

            // Assert
            Assert.NotNull(result);
            var resultList = result.ToList();
            Assert.Empty(resultList);

            // Verify repository is called exactly once
            _mockUserNoteRepository.Verify(
                repo => repo.GetAllUserNotesByUserId(userId),
                Times.Once
            );
        }

        [Fact]
        public async Task GetAllUserNotesByUserId_WhenUserIdIsLarge_ShouldReturnList()
        {
            // Arrange
            int userId = int.MaxValue;
            var expectedResult = new List<UserNoteResponseDTO>
            {
                new UserNoteResponseDTO
                {
                    NoteId = 999,
                    UserId = int.MaxValue,
                    User = "Large User ID",
                    ArticleId = 1,
                    Article = "Test Article",
                    SectionId = 1,
                    Section = "Test Section",
                    NoteContent = "Test note content",
                    CreateAt = DateTime.UtcNow,
                    UpdateAt = null
                }
            };

            _mockUserNoteRepository
                .Setup(repo => repo.GetAllUserNotesByUserId(userId))
                .ReturnsAsync(expectedResult);

            // Act
            var result = await _service.GetAllUserNotesByUserId(userId);

            // Assert
            Assert.NotNull(result);
            var resultList = result.ToList();
            Assert.Single(resultList);
            Assert.Equal(expectedResult[0].NoteId, resultList[0].NoteId);
            Assert.Equal(expectedResult[0].UserId, resultList[0].UserId);

            // Verify repository is called exactly once
            _mockUserNoteRepository.Verify(
                repo => repo.GetAllUserNotesByUserId(userId),
                Times.Once
            );
        }

        #endregion

        #region Test Cases - Repository Exception Handling

        [Fact]
        public async Task GetAllUserNotesByUserId_WhenRepositoryThrowsException_ShouldPropagateException()
        {
            // Arrange
            int userId = 1;
            var exceptionMessage = "Database connection error";

            _mockUserNoteRepository
                .Setup(repo => repo.GetAllUserNotesByUserId(userId))
                .ThrowsAsync(new Exception(exceptionMessage));

            // Act & Assert
            var exception = await Assert.ThrowsAsync<Exception>(
                async () => await _service.GetAllUserNotesByUserId(userId)
            );

            Assert.Equal(exceptionMessage, exception.Message);

            // Verify repository is called exactly once
            _mockUserNoteRepository.Verify(
                repo => repo.GetAllUserNotesByUserId(userId),
                Times.Once
            );
        }

        #endregion
    }
}

