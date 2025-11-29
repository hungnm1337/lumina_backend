using Xunit;
using Moq;
using ServiceLayer.Exam;
using RepositoryLayer.Exam;
using Lumina.Tests.Helpers;
using System;
using System.Threading.Tasks;
using DataLayer.Models;

namespace Lumina.Test.Services
{
    public class ToggleExamStatusAsyncUnitTest
    {
        private readonly Mock<IExamRepository> _mockExamRepository;
        private readonly LuminaSystemContext _context;
        private readonly ExamService _service;

        public ToggleExamStatusAsyncUnitTest()
        {
            _mockExamRepository = new Mock<IExamRepository>();
            _context = InMemoryDbContextHelper.CreateContext();

            _service = new ExamService(
                _mockExamRepository.Object,
                _context
            );
        }

        #region Test Cases - Invalid ID

        [Theory]
        [InlineData(-1)]
        public async Task ToggleExamStatusAsync_WhenExamIdIsInvalid_ShouldCallRepository(int examId)
        {
            // Arrange
            _mockExamRepository
                .Setup(repo => repo.ToggleExamStatusAsync(examId))
                .ReturnsAsync(false);

            // Act
            var result = await _service.ToggleExamStatusAsync(examId);

            // Assert
            Assert.False(result);

            // Verify repository is called exactly once
            _mockExamRepository.Verify(
                repo => repo.ToggleExamStatusAsync(examId),
                Times.Once
            );
        }

        #endregion

        #region Test Cases - Valid ID (Success)

        [Fact]
        public async Task ToggleExamStatusAsync_WhenExamIdIsValidAndTogglesSuccessfully_ShouldReturnTrue()
        {
            // Arrange
            int examId = 1;

            _mockExamRepository
                .Setup(repo => repo.ToggleExamStatusAsync(examId))
                .ReturnsAsync(true);

            // Act
            var result = await _service.ToggleExamStatusAsync(examId);

            // Assert
            Assert.True(result);

            // Verify repository is called exactly once
            _mockExamRepository.Verify(
                repo => repo.ToggleExamStatusAsync(examId),
                Times.Once
            );
        }

        [Theory]
        [InlineData(999, false)] // Exam not found
        public async Task ToggleExamStatusAsync_WhenExamIdIsValidButReturnsFalse_ShouldReturnFalse(int examId, bool expectedResult)
        {
            // Arrange
            _mockExamRepository
                .Setup(repo => repo.ToggleExamStatusAsync(examId))
                .ReturnsAsync(expectedResult);

            // Act
            var result = await _service.ToggleExamStatusAsync(examId);

            // Assert
            Assert.False(result);

            // Verify repository is called exactly once
            _mockExamRepository.Verify(
                repo => repo.ToggleExamStatusAsync(examId),
                Times.Once
            );
        }

        #endregion

        #region Test Cases - Repository Exception Handling

        [Fact]
        public async Task ToggleExamStatusAsync_WhenRepositoryThrowsException_ShouldPropagateException()
        {
            // Arrange
            int examId = 1;
            var exceptionMessage = "Database connection error";

            _mockExamRepository
                .Setup(repo => repo.ToggleExamStatusAsync(examId))
                .ThrowsAsync(new Exception(exceptionMessage));

            // Act & Assert
            var exception = await Assert.ThrowsAsync<Exception>(
                async () => await _service.ToggleExamStatusAsync(examId)
            );

            Assert.Equal(exceptionMessage, exception.Message);

            // Verify repository is called exactly once
            _mockExamRepository.Verify(
                repo => repo.ToggleExamStatusAsync(examId),
                Times.Once
            );
        }

        #endregion
    }
}
