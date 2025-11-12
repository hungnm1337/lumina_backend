// File: ExplainConcept.test.cs
using Xunit;
using Moq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using lumina.Controllers;
using ServiceLayer.UserNoteAI;
using DataLayer.DTOs.AI;
using System;
using System.Threading.Tasks;

namespace Lumina.Tests
{
    public class ExplainConceptTests
    {
        private readonly Mock<IAIChatService> _mockAiChatService;
        private readonly Mock<ILogger<UserNoteAIChatController>> _mockLogger;
        private readonly UserNoteAIChatController _controller;

        public ExplainConceptTests()
        {
            _mockAiChatService = new Mock<IAIChatService>();
            _mockLogger = new Mock<ILogger<UserNoteAIChatController>>();
            _controller = new UserNoteAIChatController(_mockAiChatService.Object, _mockLogger.Object);
        }

        [Fact]
        public async Task ExplainConcept_KhiRequestHopLe_TraVeOkVaGiaiThich()
        {
            // Arrange
            var request = new ConceptExplanationRequestDTO
            {
                Concept = "Present Perfect Tense",
                LessonContext = "English grammar lesson about tenses..."
            };

            var expectedResponse = new ChatResponseDTO
            {
                Success = true,
                Answer = "Present Perfect Tense is used to describe actions that happened at an unspecified time...",
                ConfidenceScore = 95
            };

            _mockAiChatService
                .Setup(s => s.ExplainConceptAsync(request.Concept, request.LessonContext))
                .ReturnsAsync(expectedResponse);

            // Act
            var result = await _controller.ExplainConcept(request);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var response = Assert.IsType<ChatResponseDTO>(okResult.Value);
            Assert.True(response.Success);
            Assert.Equal("Present Perfect Tense is used to describe actions that happened at an unspecified time...", response.Answer);
            Assert.Equal(95, response.ConfidenceScore);
            _mockAiChatService.Verify(s => s.ExplainConceptAsync(request.Concept, request.LessonContext), Times.Once);
        }

        [Fact]
        public async Task ExplainConcept_KhiConceptRong_TraVeBadRequest()
        {
            // Arrange
            var request = new ConceptExplanationRequestDTO
            {
                Concept = "",
                LessonContext = "Some context..."
            };

            // Act
            var result = await _controller.ExplainConcept(request);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            dynamic value = badRequestResult.Value!;
            Assert.Equal("Concept cannot be empty.", value.Message);
        }

        [Fact]
        public async Task ExplainConcept_KhiLessonContextRong_TraVeBadRequest()
        {
            // Arrange
            var request = new ConceptExplanationRequestDTO
            {
                Concept = "Present Perfect",
                LessonContext = ""
            };

            // Act
            var result = await _controller.ExplainConcept(request);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            dynamic value = badRequestResult.Value!;
            Assert.Equal("Lesson context cannot be empty.", value.Message);
        }

        [Theory]
        [InlineData(" ")]  // space
        [InlineData("\t")] // tab
        [InlineData("\n")] // newline
        [InlineData("\r")] // carriage return
        public async Task ExplainConcept_KhiConceptWhitespace_TraVeBadRequest(string whitespace)
        {
            // Arrange
            var request = new ConceptExplanationRequestDTO
            {
                Concept = whitespace,
                LessonContext = "Some context..."
            };

            // Act
            var result = await _controller.ExplainConcept(request);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            dynamic value = badRequestResult.Value!;
            Assert.Equal("Concept cannot be empty.", value.Message);
        }

        [Theory]
        [InlineData(" ")]  // space
        [InlineData("\t")] // tab
        [InlineData("\n")] // newline
        [InlineData("\r")] // carriage return
        public async Task ExplainConcept_KhiLessonContextWhitespace_TraVeBadRequest(string whitespace)
        {
            // Arrange
            var request = new ConceptExplanationRequestDTO
            {
                Concept = "Present Perfect",
                LessonContext = whitespace
            };

            // Act
            var result = await _controller.ExplainConcept(request);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            dynamic value = badRequestResult.Value!;
            Assert.Equal("Lesson context cannot be empty.", value.Message);
        }

        [Fact]
        public async Task ExplainConcept_KhiModelStateInvalid_TraVeBadRequest()
        {
            // Arrange
            _controller.ModelState.AddModelError("Concept", "Required");

            // Act
            var result = await _controller.ExplainConcept(new ConceptExplanationRequestDTO());

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal(400, badRequestResult.StatusCode);
        }

        [Fact]
        public async Task ExplainConcept_KhiAIServiceThatBai_TraVeInternalError()
        {
            // Arrange
            var request = new ConceptExplanationRequestDTO
            {
                Concept = "Present Perfect",
                LessonContext = "Grammar lesson..."
            };

            var failedResponse = new ChatResponseDTO
            {
                Success = false,
                Answer = "Error processing concept explanation"
            };

            _mockAiChatService
                .Setup(s => s.ExplainConceptAsync(request.Concept, request.LessonContext))
                .ReturnsAsync(failedResponse);

            // Act
            var result = await _controller.ExplainConcept(request);

            // Assert
            var statusCodeResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(500, statusCodeResult.StatusCode);
            var response = Assert.IsType<ChatResponseDTO>(statusCodeResult.Value!);
            Assert.False(response.Success);
        }

        [Fact]
        public async Task ExplainConcept_KhiServiceThrowException_TraVeInternalError()
        {
            // Arrange
            var request = new ConceptExplanationRequestDTO
            {
                Concept = "Present Perfect",
                LessonContext = "Grammar lesson..."
            };

            _mockAiChatService
                .Setup(s => s.ExplainConceptAsync(request.Concept, request.LessonContext))
                .ThrowsAsync(new Exception("Service error"));

            // Act
            var result = await _controller.ExplainConcept(request);

            // Assert
            var statusCodeResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(500, statusCodeResult.StatusCode);
            dynamic value = statusCodeResult.Value!;
            Assert.Equal("An unexpected error occurred while explaining the concept.", 
                value.Message);
            _mockLogger.Verify(x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => true),
                It.IsAny<Exception>(),
                It.Is<Func<It.IsAnyType, Exception?, string>>((v, t) => true)), 
                Times.Once);
        }
    }
}
