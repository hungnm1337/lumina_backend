// File: AskQuestion.test.cs
using Xunit;
using Moq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using lumina.Controllers;
using ServiceLayer.UserNoteAI;
using DataLayer.DTOs.AI;
using System.Collections.Generic;
using System.Threading.Tasks;
using System;

namespace Lumina.Tests
{
    public class AskQuestionTests
    {
        private readonly Mock<IAIChatService> _mockAiChatService;
        private readonly Mock<ILogger<UserNoteAIChatController>> _mockLogger;
        private readonly UserNoteAIChatController _controller;

        public AskQuestionTests()
        {
            _mockAiChatService = new Mock<IAIChatService>();
            _mockLogger = new Mock<ILogger<UserNoteAIChatController>>();
            _controller = new UserNoteAIChatController(_mockAiChatService.Object, _mockLogger.Object);
        }

        [Fact]
        public async Task AskQuestion_KhiRequestHopLe_TraVeOkVaResponse()
        {
            // Arrange
            var request = new ChatRequestDTO
            {
                UserQuestion = "What is TOEIC?",
                LessonContent = "TOEIC is a standardized test..."
            };

            var expectedResponse = new ChatResponseDTO
            {
                Answer = "TOEIC is a standardized test...",
                ConfidenceScore = 95,
                Success = true,
                SuggestedQuestions = new List<string> { "What is TOEIC score range?" }
            };

            _mockAiChatService
                .Setup(s => s.AskQuestionAsync(request))
                .ReturnsAsync(expectedResponse);

            // Act
            var result = await _controller.AskQuestion(request);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var response = Assert.IsType<ChatResponseDTO>(okResult.Value);
            Assert.True(response.Success);
            Assert.Equal("TOEIC is a standardized test...", response.Answer);
            Assert.Equal(95, response.ConfidenceScore);
            Assert.Single(response.SuggestedQuestions);
        }

        [Fact]
        public async Task AskQuestion_KhiModelStateInvalid_TraVeBadRequest()
        {
            // Arrange
            _controller.ModelState.AddModelError("UserQuestion", "Required");

            // Act
            var result = await _controller.AskQuestion(new ChatRequestDTO());

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal(400, badRequestResult.StatusCode);
        }

        [Fact]
            public async Task AskQuestion_KhiUserQuestionRong_TraVeBadRequest()
        {
            // Arrange
            var request = new ChatRequestDTO
            {
                UserQuestion = "",
                LessonContent = "Some content"
            };

            // Act
            var result = await _controller.AskQuestion(request);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            var value = badRequestResult.Value;
            Assert.NotNull(value);
            
            var messageProperty = value.GetType().GetProperty("Message");
            Assert.NotNull(messageProperty);
            var message = messageProperty.GetValue(value)?.ToString();
            Assert.Equal("User question cannot be empty.", message);
        }

        [Fact]
            public async Task AskQuestion_KhiLessonContentRong_TraVeBadRequest()
        {
            // Arrange
            var request = new ChatRequestDTO
            {
                UserQuestion = "What is TOEIC?",
                LessonContent = ""
            };

            // Act
            var result = await _controller.AskQuestion(request);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            var value = badRequestResult.Value;
            Assert.NotNull(value);
            
            var messageProperty = value.GetType().GetProperty("Message");
            Assert.NotNull(messageProperty);
            var message = messageProperty.GetValue(value)?.ToString();
            Assert.Equal("Lesson content cannot be empty.", message);
        }

        [Fact]
        public async Task AskQuestion_KhiAIServiceThatBai_TraVeInternalError()
        {
            // Arrange
            var request = new ChatRequestDTO
            {
                UserQuestion = "What is TOEIC?",
                LessonContent = "Some content"
            };

            var failedResponse = new ChatResponseDTO
            {
                Success = false,
                Answer = "Error processing request"
            };

            _mockAiChatService
                .Setup(s => s.AskQuestionAsync(request))
                .ReturnsAsync(failedResponse);

            // Act
            var result = await _controller.AskQuestion(request);

            // Assert
            var statusCodeResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(500, statusCodeResult.StatusCode);
            var response = Assert.IsType<ChatResponseDTO>(statusCodeResult.Value!);
            Assert.False(response.Success);
        }

        [Fact]
        public async Task AskQuestion_KhiServiceThrowException_TraVeInternalError()
        {
            // Arrange
            var request = new ChatRequestDTO
            {
                UserQuestion = "What is TOEIC?",
                LessonContent = "Some content"
            };

            _mockAiChatService
                .Setup(s => s.AskQuestionAsync(request))
                .ThrowsAsync(new Exception("Service error"));

            // Act
            var result = await _controller.AskQuestion(request);

            // Assert
            var statusCodeResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(500, statusCodeResult.StatusCode);
            
            var value = statusCodeResult.Value;
            Assert.NotNull(value);
            
            var messageProperty = value.GetType().GetProperty("Message");
            Assert.NotNull(messageProperty);
            var message = messageProperty.GetValue(value)?.ToString();
            Assert.Equal("An unexpected error occurred while processing your question.", message);
            
            _mockLogger.Verify(x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => true),
                It.IsAny<Exception>(),
                It.Is<Func<It.IsAnyType, Exception?, string>>((v, t) => true)), 
                Times.Once);
        }

        [Theory]
        [InlineData(" ")]  // space
        [InlineData("\t")] // tab
        [InlineData("\n")] // newline
        [InlineData("\r")] // carriage return
            public async Task AskQuestion_KhiUserQuestionWhitespace_TraVeBadRequest(string whitespace)
        {
            // Arrange
            var request = new ChatRequestDTO
            {
                UserQuestion = whitespace,
                LessonContent = "Some content"
            };

            // Act
            var result = await _controller.AskQuestion(request);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            var value = badRequestResult.Value;
            Assert.NotNull(value);
            
            var messageProperty = value.GetType().GetProperty("Message");
            Assert.NotNull(messageProperty);
            var message = messageProperty.GetValue(value)?.ToString();
            Assert.Equal("User question cannot be empty.", message);
        }

        [Theory]
        [InlineData(" ")]  // space
        [InlineData("\t")] // tab
        [InlineData("\n")] // newline
        [InlineData("\r")] // carriage return
            public async Task AskQuestion_KhiLessonContentWhitespace_TraVeBadRequest(string whitespace)
        {
            // Arrange
            var request = new ChatRequestDTO
            {
                UserQuestion = "What is TOEIC?",
                LessonContent = whitespace
            };

            // Act
            var result = await _controller.AskQuestion(request);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            var value = badRequestResult.Value;
            Assert.NotNull(value);
            
            var messageProperty = value.GetType().GetProperty("Message");
            Assert.NotNull(messageProperty);
            var message = messageProperty.GetValue(value)?.ToString();
            Assert.Equal("Lesson content cannot be empty.", message);
        }
    }
}