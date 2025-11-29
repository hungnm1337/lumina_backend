using Xunit;
using Moq;
using ServiceLayer.Exam.ExamAttempt;
using RepositoryLayer.Exam.ExamAttempt;
using RepositoryLayer.UnitOfWork;
using ServiceLayer.Streak;
using Microsoft.Extensions.Logging;
using DataLayer.DTOs.UserAnswer;
using System;
using System.Threading.Tasks;

namespace Lumina.Test.Services
{
    public class EndAnExamUnitTest
    {
        private readonly Mock<IExamAttemptRepository> _mockExamAttemptRepository;
        private readonly Mock<IUnitOfWork> _mockUnitOfWork;
        private readonly Mock<IStreakService> _mockStreakService;
        private readonly Mock<ILogger<ExamAttemptService>> _mockLogger;
        private readonly ExamAttemptService _service;

        public EndAnExamUnitTest()
        {
            _mockExamAttemptRepository = new Mock<IExamAttemptRepository>();
            _mockUnitOfWork = new Mock<IUnitOfWork>();
            _mockStreakService = new Mock<IStreakService>();
            _mockLogger = new Mock<ILogger<ExamAttemptService>>();

            _service = new ExamAttemptService(
                _mockExamAttemptRepository.Object,
                _mockUnitOfWork.Object,
                _mockStreakService.Object,
                _mockLogger.Object
            );
        }

        #region Test Cases for Null DTO

        [Fact]
        public async Task EndAnExam_WhenModelIsNull_ShouldThrowArgumentNullException()
        {
            // Arrange
            ExamAttemptRequestDTO? model = null;

            // Act & Assert
            var exception = await Assert.ThrowsAsync<ArgumentNullException>(
                async () => await _service.EndAnExam(model!)
            );

            Assert.Equal("model", exception.ParamName);
            Assert.Contains("Request DTO cannot be null", exception.Message);

            // Verify repository is never called
            _mockExamAttemptRepository.Verify(
                repo => repo.EndAnExam(It.IsAny<ExamAttemptRequestDTO>()),
                Times.Never
            );
        }

        #endregion

        #region Test Cases for Invalid AttemptID

        [Fact]
        public async Task EndAnExam_WhenAttemptIDIsNegative_ShouldThrowArgumentException()
        {
            // Arrange
            var model = new ExamAttemptRequestDTO
            {
                AttemptID = -1,
                UserID = 1,
                ExamID = 1,
                Status = "InProgress",
                StartTime = DateTime.UtcNow
            };

            // Act & Assert
            var exception = await Assert.ThrowsAsync<ArgumentException>(
                async () => await _service.EndAnExam(model)
            );

            Assert.Equal("AttemptID", exception.ParamName);
            Assert.Contains("AttemptID must be non-negative", exception.Message);

            // Verify repository is never called
            _mockExamAttemptRepository.Verify(
                repo => repo.EndAnExam(It.IsAny<ExamAttemptRequestDTO>()),
                Times.Never
            );
        }

        [Fact]
        public async Task EndAnExam_WhenAttemptIDIsZero_ShouldReturnNull()
        {
            // Arrange
            var model = new ExamAttemptRequestDTO
            {
                AttemptID = 0,
                UserID = 1,
                ExamID = 1,
                Status = "InProgress",
                StartTime = DateTime.UtcNow
            };

            // Act
            var result = await _service.EndAnExam(model);

            // Assert
            Assert.Null(result);

            // Verify repository is never called when AttemptID is 0
            _mockExamAttemptRepository.Verify(
                repo => repo.EndAnExam(It.IsAny<ExamAttemptRequestDTO>()),
                Times.Never
            );
        }

        #endregion

        #region Test Cases for Invalid Status

        [Fact]
        public async Task EndAnExam_WhenStatusIsNull_ShouldThrowArgumentException()
        {
            // Arrange
            var model = new ExamAttemptRequestDTO
            {
                AttemptID = 1,
                UserID = 1,
                ExamID = 1,
                Status = null!,
                StartTime = DateTime.UtcNow
            };

            // Act & Assert
            var exception = await Assert.ThrowsAsync<ArgumentException>(
                async () => await _service.EndAnExam(model)
            );

            Assert.Equal("Status", exception.ParamName);
            Assert.Contains("Status cannot be empty", exception.Message);

            // Verify repository is never called
            _mockExamAttemptRepository.Verify(
                repo => repo.EndAnExam(It.IsAny<ExamAttemptRequestDTO>()),
                Times.Never
            );
        }

        [Fact]
        public async Task EndAnExam_WhenStatusIsEmpty_ShouldThrowArgumentException()
        {
            // Arrange
            var model = new ExamAttemptRequestDTO
            {
                AttemptID = 1,
                UserID = 1,
                ExamID = 1,
                Status = string.Empty,
                StartTime = DateTime.UtcNow
            };

            // Act & Assert
            var exception = await Assert.ThrowsAsync<ArgumentException>(
                async () => await _service.EndAnExam(model)
            );

            Assert.Equal("Status", exception.ParamName);
            Assert.Contains("Status cannot be empty", exception.Message);

            // Verify repository is never called
            _mockExamAttemptRepository.Verify(
                repo => repo.EndAnExam(It.IsAny<ExamAttemptRequestDTO>()),
                Times.Never
            );
        }

        [Fact]
        public async Task EndAnExam_WhenStatusIsWhitespace_ShouldThrowArgumentException()
        {
            // Arrange
            var model = new ExamAttemptRequestDTO
            {
                AttemptID = 1,
                UserID = 1,
                ExamID = 1,
                Status = "   ",
                StartTime = DateTime.UtcNow
            };

            // Act & Assert
            var exception = await Assert.ThrowsAsync<ArgumentException>(
                async () => await _service.EndAnExam(model)
            );

            Assert.Equal("Status", exception.ParamName);
            Assert.Contains("Status cannot be empty", exception.Message);

            // Verify repository is never called
            _mockExamAttemptRepository.Verify(
                repo => repo.EndAnExam(It.IsAny<ExamAttemptRequestDTO>()),
                Times.Never
            );
        }

        #endregion

        #region Test Cases for Valid Input

        [Fact]
        public async Task EndAnExam_WhenInputIsValid_ShouldCallRepositoryAndReturnResult()
        {
            // Arrange
            var model = new ExamAttemptRequestDTO
            {
                AttemptID = 1,
                UserID = 1,
                ExamID = 1,
                Status = "Completed",
                StartTime = DateTime.UtcNow,
                EndTime = DateTime.UtcNow,
                Score = 100
            };

            var expectedResult = new ExamAttemptRequestDTO
            {
                AttemptID = 1,
                UserID = 1,
                ExamID = 1,
                Status = "Completed",
                StartTime = model.StartTime,
                EndTime = model.EndTime,
                Score = 100
            };

            _mockExamAttemptRepository
                .Setup(repo => repo.EndAnExam(It.IsAny<ExamAttemptRequestDTO>()))
                .ReturnsAsync(expectedResult);

            // Act
            var result = await _service.EndAnExam(model);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(expectedResult.AttemptID, result.AttemptID);
            Assert.Equal(expectedResult.Status, result.Status);
            Assert.Equal(expectedResult.Score, result.Score);

            // Verify repository is called exactly once with the correct model
            _mockExamAttemptRepository.Verify(
                repo => repo.EndAnExam(It.Is<ExamAttemptRequestDTO>(
                    dto => dto.AttemptID == model.AttemptID &&
                           dto.Status == model.Status
                )),
                Times.Once
            );
        }

        [Fact]
        public async Task EndAnExam_WhenInputIsValidWithMinimalData_ShouldCallRepositoryAndReturnResult()
        {
            // Arrange
            var model = new ExamAttemptRequestDTO
            {
                AttemptID = 1,
                UserID = 1,
                ExamID = 1,
                Status = "InProgress",
                StartTime = DateTime.UtcNow
            };

            var expectedResult = new ExamAttemptRequestDTO
            {
                AttemptID = 1,
                UserID = 1,
                ExamID = 1,
                Status = "InProgress",
                StartTime = model.StartTime
            };

            _mockExamAttemptRepository
                .Setup(repo => repo.EndAnExam(It.IsAny<ExamAttemptRequestDTO>()))
                .ReturnsAsync(expectedResult);

            // Act
            var result = await _service.EndAnExam(model);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(expectedResult.AttemptID, result.AttemptID);
            Assert.Equal(expectedResult.Status, result.Status);

            // Verify repository is called exactly once
            _mockExamAttemptRepository.Verify(
                repo => repo.EndAnExam(It.IsAny<ExamAttemptRequestDTO>()),
                Times.Once
            );
        }

        [Fact]
        public async Task EndAnExam_WhenInputIsValidWithLargeAttemptID_ShouldCallRepositoryAndReturnResult()
        {
            // Arrange
            var model = new ExamAttemptRequestDTO
            {
                AttemptID = int.MaxValue,
                UserID = 1,
                ExamID = 1,
                Status = "Completed",
                StartTime = DateTime.UtcNow
            };

            var expectedResult = new ExamAttemptRequestDTO
            {
                AttemptID = int.MaxValue,
                UserID = 1,
                ExamID = 1,
                Status = "Completed",
                StartTime = model.StartTime
            };

            _mockExamAttemptRepository
                .Setup(repo => repo.EndAnExam(It.IsAny<ExamAttemptRequestDTO>()))
                .ReturnsAsync(expectedResult);

            // Act
            var result = await _service.EndAnExam(model);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(int.MaxValue, result.AttemptID);

            // Verify repository is called exactly once
            _mockExamAttemptRepository.Verify(
                repo => repo.EndAnExam(It.IsAny<ExamAttemptRequestDTO>()),
                Times.Once
            );
        }

        #endregion

        #region Test Cases for Edge Cases - Multiple Validation Failures

        [Fact]
        public async Task EndAnExam_WhenAttemptIDIsNegativeAndStatusIsNull_ShouldThrowArgumentExceptionForAttemptID()
        {
            // Arrange
            // Note: The method checks AttemptID before Status, so it should throw for AttemptID first
            var model = new ExamAttemptRequestDTO
            {
                AttemptID = -1,
                UserID = 1,
                ExamID = 1,
                Status = null!,
                StartTime = DateTime.UtcNow
            };

            // Act & Assert
            var exception = await Assert.ThrowsAsync<ArgumentException>(
                async () => await _service.EndAnExam(model)
            );

            // Should throw for AttemptID first (order of validation)
            Assert.Equal("AttemptID", exception.ParamName);

            // Verify repository is never called
            _mockExamAttemptRepository.Verify(
                repo => repo.EndAnExam(It.IsAny<ExamAttemptRequestDTO>()),
                Times.Never
            );
        }

        [Fact]
        public async Task EndAnExam_WhenAttemptIDIsZeroAndStatusIsEmpty_ShouldReturnNull()
        {
            // Arrange
            // Note: When AttemptID is 0, method returns null immediately without checking Status
            // This is because AttemptID == 0 check happens before Status validation
            var model = new ExamAttemptRequestDTO
            {
                AttemptID = 0,
                UserID = 1,
                ExamID = 1,
                Status = string.Empty,
                StartTime = DateTime.UtcNow
            };

            // Act
            var result = await _service.EndAnExam(model);

            // Assert
            Assert.Null(result);

            // Verify repository is never called
            _mockExamAttemptRepository.Verify(
                repo => repo.EndAnExam(It.IsAny<ExamAttemptRequestDTO>()),
                Times.Never
            );
        }

        #endregion
    }
}

