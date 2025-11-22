using DataLayer.DTOs.Exam.Writting;
using lumina.Controllers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using ServiceLayer.Exam.Writting;

namespace Lumina.Tests
{
    public class GetFeedbackP23FromAITests
    {
        private readonly Mock<IWritingService> _mockWritingService;
        private readonly Mock<ILogger<WritingController>> _mockLogger;
        private readonly WritingController _controller;

        public GetFeedbackP23FromAITests()
        {
            _mockWritingService = new Mock<IWritingService>();
            _mockLogger = new Mock<ILogger<WritingController>>();
            _controller = new WritingController(_mockWritingService.Object, _mockLogger.Object);
        }

        [Theory]
        [InlineData(2, "Write an email to your friend", "Dear John, How are you?", 85)]
        [InlineData(3, "Write an essay about technology", "Technology is important in modern life.", 75)]
        public async Task GetFeedbackP23FromAI_VoiDuLieuHopLe_TraVe200OK(int partNumber, string prompt, string userAnswer, int expectedScore)
        {
            // Arrange
            var request = new WritingRequestP23DTO
            {
                PartNumber = partNumber,
                Prompt = prompt,
                UserAnswer = userAnswer
            };

            var expectedResponse = new WritingResponseDTO
            {
                TotalScore = expectedScore,
                GrammarFeedback = "Good grammar usage",
                VocabularyFeedback = "Good vocabulary",
                ContentAccuracyFeedback = "Content is accurate",
                CorreededAnswerProposal = userAnswer
            };

            _mockWritingService.Setup(s => s.GetFeedbackP23FromAI(It.IsAny<WritingRequestP23DTO>()))
                .ReturnsAsync(expectedResponse);

            // Act
            var result = await _controller.GetFeedbackP23FromAI(request);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(StatusCodes.Status200OK, okResult.StatusCode);
            var response = Assert.IsType<WritingResponseDTO>(okResult.Value);
            Assert.Equal(expectedScore, response.TotalScore);
            _mockWritingService.Verify(s => s.GetFeedbackP23FromAI(It.IsAny<WritingRequestP23DTO>()), Times.Once);
        }

        [Fact]
        public async Task GetFeedbackP23FromAI_KhiModelStateKhongHopLe_TraVe400BadRequest()
        {
            // Arrange
            var request = new WritingRequestP23DTO
            {
                PartNumber = 2,
                Prompt = "Write an email",
                UserAnswer = "This is my answer"
            };

            _controller.ModelState.AddModelError("PartNumber", "Part number is required");

            // Act
            var result = await _controller.GetFeedbackP23FromAI(request);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal(StatusCodes.Status400BadRequest, badRequestResult.StatusCode);
            _mockWritingService.Verify(s => s.GetFeedbackP23FromAI(It.IsAny<WritingRequestP23DTO>()), Times.Never);
        }

        [Theory]
        [InlineData(null, "Valid prompt", "UserAnswer cannot be empty.")]
        [InlineData("", "Valid prompt", "UserAnswer cannot be empty.")]
        [InlineData("   ", "Valid prompt", "UserAnswer cannot be empty.")]
        [InlineData("Valid answer", null, "Prompt cannot be empty.")]
        [InlineData("Valid answer", "", "Prompt cannot be empty.")]
        [InlineData("Valid answer", "   ", "Prompt cannot be empty.")]
        public async Task GetFeedbackP23FromAI_KhiDuLieuKhongHopLe_TraVe400BadRequest(string userAnswer, string prompt, string expectedMessage)
        {
            // Arrange
            var request = new WritingRequestP23DTO
            {
                PartNumber = 2,
                Prompt = prompt!,
                UserAnswer = userAnswer!
            };

            // Act
            var result = await _controller.GetFeedbackP23FromAI(request);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal(StatusCodes.Status400BadRequest, badRequestResult.StatusCode);
            var response = badRequestResult.Value;
            var messageProperty = response!.GetType().GetProperty("Message");
            Assert.Equal(expectedMessage, messageProperty!.GetValue(response));
            _mockWritingService.Verify(s => s.GetFeedbackP23FromAI(It.IsAny<WritingRequestP23DTO>()), Times.Never);
        }

        [Fact]
        public async Task GetFeedbackP23FromAI_KhiServiceThrowException_TraVe500VaLogError()
        {
            // Arrange
            var request = new WritingRequestP23DTO
            {
                PartNumber = 2,
                Prompt = "Write an email",
                UserAnswer = "This is my answer"
            };

            _mockWritingService.Setup(s => s.GetFeedbackP23FromAI(It.IsAny<WritingRequestP23DTO>()))
                .ThrowsAsync(new Exception("Service error"));

            // Act
            var result = await _controller.GetFeedbackP23FromAI(request);

            // Assert
            var statusCodeResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(StatusCodes.Status500InternalServerError, statusCodeResult.StatusCode);
            var response = statusCodeResult.Value;
            var messageProperty = response!.GetType().GetProperty("Message");
            Assert.Equal("An unexpected error occurred while getting AI feedback.", messageProperty!.GetValue(response));
            
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => true),
                    It.IsAny<Exception?>(),
                    It.Is<Func<It.IsAnyType, Exception?, string>>((v, t) => true)),
                Times.Once);
        }

        // ==================== BOUNDARY VALUE TEST CASES ====================

        [Theory]
        [InlineData(45, "too short", "Technology is good.")]  // Short essay - Low score
        [InlineData(50, "grammar errors", "We must to protect environment.")]  // Grammar errors - Medium score
        [InlineData(90, "Excellent", "Working from home offers flexibility and efficiency. It enables better work-life balance.")]  // Good essay - High score
        public async Task GetFeedbackP23FromAI_VoiBienGioiHan_TraVeScoreTuongUng(int expectedScore, string expectedFeedbackKeyword, string userAnswer)
        {
            // Arrange
            var request = new WritingRequestP23DTO
            {
                PartNumber = 3,
                Prompt = "Discuss your topic",
                UserAnswer = userAnswer
            };

            var expectedResponse = new WritingResponseDTO
            {
                TotalScore = expectedScore,
                GrammarFeedback = expectedFeedbackKeyword,
                VocabularyFeedback = "Feedback",
                ContentAccuracyFeedback = expectedFeedbackKeyword,
                CorreededAnswerProposal = userAnswer
            };

            _mockWritingService.Setup(s => s.GetFeedbackP23FromAI(It.IsAny<WritingRequestP23DTO>()))
                .ReturnsAsync(expectedResponse);

            // Act
            var result = await _controller.GetFeedbackP23FromAI(request);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var response = Assert.IsType<WritingResponseDTO>(okResult.Value);
            Assert.Equal(expectedScore, response.TotalScore);
            Assert.Contains(expectedFeedbackKeyword, response.GrammarFeedback + response.ContentAccuracyFeedback);
        }
    }
}
