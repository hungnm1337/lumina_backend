using DataLayer.DTOs.Exam.Speaking;
using DataLayer.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using RepositoryLayer.UnitOfWork;
using ServiceLayer.Exam.Speaking;
using ServiceLayer.Speech;
using System.Security.Claims;

namespace Lumina.Tests
{
    public class SpeakingControllerSubmitAnswerTests
    {
        private readonly Mock<ISpeakingScoringService> _mockSpeakingScoringService;
        private readonly Mock<IUnitOfWork> _mockUnitOfWork;
        private readonly Mock<IAzureSpeechService> _mockAzureSpeechService;
        private readonly SpeakingController _controller;

        public SpeakingControllerSubmitAnswerTests()
        {
            _mockSpeakingScoringService = new Mock<ISpeakingScoringService>();
            _mockUnitOfWork = new Mock<IUnitOfWork>();
            _mockAzureSpeechService = new Mock<IAzureSpeechService>();
            _controller = new SpeakingController(
                _mockSpeakingScoringService.Object,
                _mockUnitOfWork.Object,
                _mockAzureSpeechService.Object
            );
        }

        #region Happy Path Tests

        [Fact]
        public async Task SubmitAnswer_WithValidRequestAndAttemptId_ShouldReturn200OK()
        {
            // Arrange
            var userId = 1;
            SetupUserClaims(userId);

            var audioFile = CreateMockFormFile();
            var request = new SubmitSpeakingAnswerRequest
            {
                Audio = audioFile,
                QuestionId = 10,
                AttemptId = 5
            };

            var existingAttempt = new ExamAttempt
            {
                AttemptID = 5,
                UserID = userId,
                ExamID = 1,
                Status = "In Progress"
            };

            var expectedResult = new SpeakingScoringResultDTO
            {
                Transcript = "Hello world",
                SavedAudioUrl = "https://cloudinary.com/audio.wav"
            };

            _mockUnitOfWork.Setup(u => u.ExamAttemptsGeneric.GetAsync(
                It.IsAny<System.Linq.Expressions.Expression<System.Func<ExamAttempt, bool>>>(),
                It.IsAny<string>()
            )).ReturnsAsync(existingAttempt);

            _mockSpeakingScoringService.Setup(s => s.ProcessAndScoreAnswerAsync(
                It.IsAny<IFormFile>(),
                It.IsAny<int>(),
                It.IsAny<int>()
            )).ReturnsAsync(expectedResult);

            // Act
            var result = await _controller.SubmitAnswer(request);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(StatusCodes.Status200OK, okResult.StatusCode);
            
            var actualResult = Assert.IsType<SpeakingScoringResultDTO>(okResult.Value);
            Assert.Equal(expectedResult.Transcript, actualResult.Transcript);
        }

        #endregion

        #region Audio Validation Tests

        [Fact]
        public async Task SubmitAnswer_WithNullAudio_ShouldReturn400BadRequest()
        {
            // Arrange
            var request = new SubmitSpeakingAnswerRequest
            {
                Audio = null,
                QuestionId = 10,
                AttemptId = 5
            };

            // Act
            var result = await _controller.SubmitAnswer(request);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal(StatusCodes.Status400BadRequest, badRequestResult.StatusCode);
            Assert.Equal("Audio file is required.", badRequestResult.Value);
        }

        [Fact]
        public async Task SubmitAnswer_WithEmptyAudio_ShouldReturn400BadRequest()
        {
            // Arrange
            var emptyAudioFile = CreateMockFormFile(0); // 0 length
            var request = new SubmitSpeakingAnswerRequest
            {
                Audio = emptyAudioFile,
                QuestionId = 10,
                AttemptId = 5
            };

            // Act
            var result = await _controller.SubmitAnswer(request);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal(StatusCodes.Status400BadRequest, badRequestResult.StatusCode);
            Assert.Equal("Audio file is required.", badRequestResult.Value);
        }

        #endregion

        #region Authorization Tests

        [Fact]
        public async Task SubmitAnswer_WithValidUserClaims_ShouldCallService()
        {
            // Arrange
            var userId = 1;
            SetupUserClaims(userId);

            var audioFile = CreateMockFormFile();
            var request = new SubmitSpeakingAnswerRequest
            {
                Audio = audioFile,
                QuestionId = 10,
                AttemptId = 5
            };

            var existingAttempt = new ExamAttempt
            {
                AttemptID = 5,
                UserID = userId,
                ExamID = 1,
                Status = "In Progress"
            };

            var expectedResult = new SpeakingScoringResultDTO
            {
                Transcript = "Test transcript",
                SavedAudioUrl = "https://cloudinary.com/audio.wav"
            };

            _mockUnitOfWork.Setup(u => u.ExamAttemptsGeneric.GetAsync(
                It.IsAny<System.Linq.Expressions.Expression<System.Func<ExamAttempt, bool>>>(),
                It.IsAny<string>()
            )).ReturnsAsync(existingAttempt);

            _mockSpeakingScoringService.Setup(s => s.ProcessAndScoreAnswerAsync(
                It.IsAny<IFormFile>(),
                It.IsAny<int>(),
                It.IsAny<int>()
            )).ReturnsAsync(expectedResult);

            // Act
            var result = await _controller.SubmitAnswer(request);

            // Assert
            _mockSpeakingScoringService.Verify(s => s.ProcessAndScoreAnswerAsync(
                It.IsAny<IFormFile>(),
                request.QuestionId,
                request.AttemptId
            ), Times.Once);
        }

        #endregion

        #region AttemptId Validation Tests

        [Fact]
        public async Task SubmitAnswer_WithNonExistentAttemptId_ShouldReturn404NotFound()
        {
            // Arrange
            var userId = 1;
            SetupUserClaims(userId);

            var audioFile = CreateMockFormFile();
            var request = new SubmitSpeakingAnswerRequest
            {
                Audio = audioFile,
                QuestionId = 10,
                AttemptId = 999
            };

            _mockUnitOfWork.Setup(u => u.ExamAttemptsGeneric.GetAsync(
                It.IsAny<System.Linq.Expressions.Expression<System.Func<ExamAttempt, bool>>>(),
                It.IsAny<string>()
            )).ReturnsAsync((ExamAttempt)null);

            // Act
            var result = await _controller.SubmitAnswer(request);

            // Assert
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
            Assert.Equal(StatusCodes.Status404NotFound, notFoundResult.StatusCode);
            Assert.Contains("not found", notFoundResult.Value.ToString());
        }

        [Fact]
        public async Task SubmitAnswer_WithAttemptIdBelongingToAnotherUser_ShouldReturn403Forbidden()
        {
            // Arrange
            var userId = 1;
            SetupUserClaims(userId);

            var audioFile = CreateMockFormFile();
            var request = new SubmitSpeakingAnswerRequest
            {
                Audio = audioFile,
                QuestionId = 10,
                AttemptId = 5
            };

            var attemptBelongingToAnotherUser = new ExamAttempt
            {
                AttemptID = 5,
                UserID = 999, // Different user
                ExamID = 1,
                Status = "In Progress"
            };

            _mockUnitOfWork.Setup(u => u.ExamAttemptsGeneric.GetAsync(
                It.IsAny<System.Linq.Expressions.Expression<System.Func<ExamAttempt, bool>>>(),
                It.IsAny<string>()
            )).ReturnsAsync(attemptBelongingToAnotherUser);

            // Act
            var result = await _controller.SubmitAnswer(request);

            // Assert
            var forbidResult = Assert.IsType<ForbidResult>(result);
        }

        #endregion

        #region Exception Handling Tests

        [Fact]
        public async Task SubmitAnswer_WhenServiceThrowsException_ShouldReturn500InternalServerError()
        {
            // Arrange
            var userId = 1;
            SetupUserClaims(userId);

            var audioFile = CreateMockFormFile();
            var request = new SubmitSpeakingAnswerRequest
            {
                Audio = audioFile,
                QuestionId = 10,
                AttemptId = 5
            };

            var existingAttempt = new ExamAttempt
            {
                AttemptID = 5,
                UserID = userId,
                ExamID = 1,
                Status = "In Progress"
            };

            _mockUnitOfWork.Setup(u => u.ExamAttemptsGeneric.GetAsync(
                It.IsAny<System.Linq.Expressions.Expression<System.Func<ExamAttempt, bool>>>(),
                It.IsAny<string>()
            )).ReturnsAsync(existingAttempt);

            _mockSpeakingScoringService.Setup(s => s.ProcessAndScoreAnswerAsync(
                It.IsAny<IFormFile>(),
                It.IsAny<int>(),
                It.IsAny<int>()
            )).ThrowsAsync(new Exception("Audio processing failed"));

            // Act
            var result = await _controller.SubmitAnswer(request);

            // Assert
            var statusCodeResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(StatusCodes.Status500InternalServerError, statusCodeResult.StatusCode);
            Assert.Contains("Internal server error", statusCodeResult.Value.ToString());
        }

        #endregion

        #region Helper Methods

        private void SetupUserClaims(int userId)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, userId.ToString())
            };
            var identity = new ClaimsIdentity(claims, "TestAuthType");
            var claimsPrincipal = new ClaimsPrincipal(identity);
            
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = claimsPrincipal }
            };
        }

        private void SetupUserClaims(string userId)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, userId)
            };
            var identity = new ClaimsIdentity(claims, "TestAuthType");
            var claimsPrincipal = new ClaimsPrincipal(identity);
            
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = claimsPrincipal }
            };
        }

        private IFormFile CreateMockFormFile(long length = 1024)
        {
            var content = new byte[length];
            var stream = new MemoryStream(content);
            var mockFile = new Mock<IFormFile>();
            
            mockFile.Setup(f => f.Length).Returns(length);
            mockFile.Setup(f => f.OpenReadStream()).Returns(stream);
            mockFile.Setup(f => f.FileName).Returns("test-audio.wav");
            mockFile.Setup(f => f.ContentType).Returns("audio/wav");
            
            return mockFile.Object;
        }

        #endregion
    }
}
