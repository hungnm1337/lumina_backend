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
    public class StartAnExamUnitTest
    {
        private readonly Mock<IExamAttemptRepository> _mockExamAttemptRepository;
        private readonly Mock<IUnitOfWork> _mockUnitOfWork;
        private readonly Mock<IStreakService> _mockStreakService;
        private readonly Mock<ILogger<ExamAttemptService>> _mockLogger;
        private readonly ExamAttemptService _service;

        public StartAnExamUnitTest()
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
        public async Task StartAnExam_WhenModelIsNull_ShouldThrowArgumentNullException()
        {
            // Arrange
            ExamAttemptRequestDTO? model = null;

            // Act & Assert
            var exception = await Assert.ThrowsAsync<ArgumentNullException>(
                async () => await _service.StartAnExam(model!)
            );

            Assert.Equal("model", exception.ParamName);
            Assert.Contains("Request DTO cannot be null", exception.Message);

            // Verify repository is never called
            _mockExamAttemptRepository.Verify(
                repo => repo.StartAnExam(It.IsAny<ExamAttemptRequestDTO>()),
                Times.Never
            );
        }

        #endregion

        #region Test Cases for Invalid UserID

        [Fact]
        public async Task StartAnExam_WhenUserIDIsInvalid_ShouldThrowArgumentException()
        {
            // Arrange
            var model = new ExamAttemptRequestDTO
            {
                AttemptID = 0,
                UserID = 0,
                ExamID = 1,
                Status = "InProgress",
                StartTime = DateTime.UtcNow
            };

            // Act & Assert
            var exception = await Assert.ThrowsAsync<ArgumentException>(
                async () => await _service.StartAnExam(model)
            );

            Assert.Equal("UserID", exception.ParamName);
            Assert.Contains("User ID must be greater than zero", exception.Message);

            // Verify repository is never called
            _mockExamAttemptRepository.Verify(
                repo => repo.StartAnExam(It.IsAny<ExamAttemptRequestDTO>()),
                Times.Never
            );
        }

        [Fact]
        public async Task StartAnExam_WhenUserIDIsNegative_ShouldThrowArgumentException()
        {
            // Arrange
            var model = new ExamAttemptRequestDTO
            {
                AttemptID = 0,
                UserID = -1,
                ExamID = 1,
                Status = "InProgress",
                StartTime = DateTime.UtcNow
            };

            // Act & Assert
            var exception = await Assert.ThrowsAsync<ArgumentException>(
                async () => await _service.StartAnExam(model)
            );

            Assert.Equal("UserID", exception.ParamName);
            Assert.Contains("User ID must be greater than zero", exception.Message);

            // Verify repository is never called
            _mockExamAttemptRepository.Verify(
                repo => repo.StartAnExam(It.IsAny<ExamAttemptRequestDTO>()),
                Times.Never
            );
        }

        #endregion

        #region Test Cases for Invalid ExamID

        [Fact]
        public async Task StartAnExam_WhenExamIDIsInvalid_ShouldThrowArgumentException()
        {
            // Arrange
            var model = new ExamAttemptRequestDTO
            {
                AttemptID = 0,
                UserID = 1,
                ExamID = 0,
                Status = "InProgress",
                StartTime = DateTime.UtcNow
            };

            // Act & Assert
            var exception = await Assert.ThrowsAsync<ArgumentException>(
                async () => await _service.StartAnExam(model)
            );

            Assert.Equal("ExamID", exception.ParamName);
            Assert.Contains("Exam ID must be greater than zero", exception.Message);

            // Verify repository is never called
            _mockExamAttemptRepository.Verify(
                repo => repo.StartAnExam(It.IsAny<ExamAttemptRequestDTO>()),
                Times.Never
            );
        }

        [Fact]
        public async Task StartAnExam_WhenExamIDIsNegative_ShouldThrowArgumentException()
        {
            // Arrange
            var model = new ExamAttemptRequestDTO
            {
                AttemptID = 0,
                UserID = 1,
                ExamID = -1,
                Status = "InProgress",
                StartTime = DateTime.UtcNow
            };

            // Act & Assert
            var exception = await Assert.ThrowsAsync<ArgumentException>(
                async () => await _service.StartAnExam(model)
            );

            Assert.Equal("ExamID", exception.ParamName);
            Assert.Contains("Exam ID must be greater than zero", exception.Message);

            // Verify repository is never called
            _mockExamAttemptRepository.Verify(
                repo => repo.StartAnExam(It.IsAny<ExamAttemptRequestDTO>()),
                Times.Never
            );
        }

        #endregion

        #region Test Cases for Invalid Status

        [Fact]
        public async Task StartAnExam_WhenStatusIsNull_ShouldThrowArgumentException()
        {
            // Arrange
            var model = new ExamAttemptRequestDTO
            {
                AttemptID = 0,
                UserID = 1,
                ExamID = 1,
                Status = null!,
                StartTime = DateTime.UtcNow
            };

            // Act & Assert
            var exception = await Assert.ThrowsAsync<ArgumentException>(
                async () => await _service.StartAnExam(model)
            );

            Assert.Equal("Status", exception.ParamName);
            Assert.Contains("Status cannot be empty", exception.Message);

            // Verify repository is never called
            _mockExamAttemptRepository.Verify(
                repo => repo.StartAnExam(It.IsAny<ExamAttemptRequestDTO>()),
                Times.Never
            );
        }

        [Fact]
        public async Task StartAnExam_WhenStatusIsEmpty_ShouldThrowArgumentException()
        {
            // Arrange
            var model = new ExamAttemptRequestDTO
            {
                AttemptID = 0,
                UserID = 1,
                ExamID = 1,
                Status = string.Empty,
                StartTime = DateTime.UtcNow
            };

            // Act & Assert
            var exception = await Assert.ThrowsAsync<ArgumentException>(
                async () => await _service.StartAnExam(model)
            );

            Assert.Equal("Status", exception.ParamName);
            Assert.Contains("Status cannot be empty", exception.Message);

            // Verify repository is never called
            _mockExamAttemptRepository.Verify(
                repo => repo.StartAnExam(It.IsAny<ExamAttemptRequestDTO>()),
                Times.Never
            );
        }

        [Fact]
        public async Task StartAnExam_WhenStatusIsWhitespace_ShouldThrowArgumentException()
        {
            // Arrange
            var model = new ExamAttemptRequestDTO
            {
                AttemptID = 0,
                UserID = 1,
                ExamID = 1,
                Status = "   ",
                StartTime = DateTime.UtcNow
            };

            // Act & Assert
            var exception = await Assert.ThrowsAsync<ArgumentException>(
                async () => await _service.StartAnExam(model)
            );

            Assert.Equal("Status", exception.ParamName);
            Assert.Contains("Status cannot be empty", exception.Message);

            // Verify repository is never called
            _mockExamAttemptRepository.Verify(
                repo => repo.StartAnExam(It.IsAny<ExamAttemptRequestDTO>()),
                Times.Never
            );
        }

        #endregion

        #region Test Cases for Valid Input

        [Fact]
        public async Task StartAnExam_WhenInputIsValid_ShouldCallRepositoryAndReturnResult()
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

            var expectedResult = new ExamAttemptRequestDTO
            {
                AttemptID = 1,
                UserID = 1,
                ExamID = 1,
                Status = "InProgress",
                StartTime = model.StartTime
            };

            _mockExamAttemptRepository
                .Setup(repo => repo.StartAnExam(It.IsAny<ExamAttemptRequestDTO>()))
                .ReturnsAsync(expectedResult);

            // Act
            var result = await _service.StartAnExam(model);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(expectedResult.AttemptID, result.AttemptID);
            Assert.Equal(expectedResult.UserID, result.UserID);
            Assert.Equal(expectedResult.ExamID, result.ExamID);
            Assert.Equal(expectedResult.Status, result.Status);

            // Verify repository is called exactly once with the correct model
            _mockExamAttemptRepository.Verify(
                repo => repo.StartAnExam(It.Is<ExamAttemptRequestDTO>(
                    dto => dto.UserID == model.UserID &&
                           dto.ExamID == model.ExamID &&
                           dto.Status == model.Status
                )),
                Times.Once
            );
        }

        [Fact]
        public async Task StartAnExam_WhenInputIsValidWithOptionalFields_ShouldCallRepositoryAndReturnResult()
        {
            // Arrange
            var model = new ExamAttemptRequestDTO
            {
                AttemptID = 0,
                UserID = 1,
                ExamID = 1,
                ExamPartId = 5,
                Status = "InProgress",
                StartTime = DateTime.UtcNow,
                EndTime = null,
                Score = null
            };

            var expectedResult = new ExamAttemptRequestDTO
            {
                AttemptID = 1,
                UserID = 1,
                ExamID = 1,
                ExamPartId = 5,
                Status = "InProgress",
                StartTime = model.StartTime,
                EndTime = null,
                Score = null
            };

            _mockExamAttemptRepository
                .Setup(repo => repo.StartAnExam(It.IsAny<ExamAttemptRequestDTO>()))
                .ReturnsAsync(expectedResult);

            // Act
            var result = await _service.StartAnExam(model);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(expectedResult.AttemptID, result.AttemptID);
            Assert.Equal(expectedResult.ExamPartId, result.ExamPartId);

            // Verify repository is called exactly once
            _mockExamAttemptRepository.Verify(
                repo => repo.StartAnExam(It.IsAny<ExamAttemptRequestDTO>()),
                Times.Once
            );
        }

        #endregion
    }
}

