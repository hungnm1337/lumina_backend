// File: ContinueConversation.test.cs
using Xunit;
using Moq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using lumina.Controllers;
using ServiceLayer.UserNoteAI;
using DataLayer.DTOs.AI;
using System;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace Lumina.Tests
{
    public class ContinueConversationTests
    {
        private readonly Mock<IAIChatService> _mockAiChatService;
        private readonly Mock<ILogger<UserNoteAIChatController>> _mockLogger;
        private readonly UserNoteAIChatController _controller;

        public ContinueConversationTests()
        {
            _mockAiChatService = new Mock<IAIChatService>();
            _mockLogger = new Mock<ILogger<UserNoteAIChatController>>();
            _controller = new UserNoteAIChatController(_mockAiChatService.Object, _mockLogger.Object);
        }

        [Fact]
        public async Task ContinueConversation_KhiRequestHopLe_TraVeOkVaResponse()
        {
            // Arrange
            var request = new ChatRequestDTO
            {
                UserQuestion = "Tell me more about TOEIC?",
                LessonContent = "TOEIC test content..."
            };

            var expectedResponse = new ChatConversationResponseDTO
            {
                CurrentResponse = new ChatResponseDTO 
                { 
                    Answer = "More details about TOEIC...",
                    Success = true 
                },
                ConversationHistory = new List<ChatMessageDTO> 
                { 
                    new ChatMessageDTO { Role = "user", Content = "Tell me more about TOEIC?" },
                    new ChatMessageDTO { Role = "assistant", Content = "More details about TOEIC..." }
                }
            };

            _mockAiChatService
                .Setup(s => s.ContinueConversationAsync(request))
                .ReturnsAsync(expectedResponse);

            // Act
            var result = await _controller.ContinueConversation(request);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var response = Assert.IsType<ChatConversationResponseDTO>(okResult.Value);
            Assert.True(response.CurrentResponse.Success);
            Assert.Equal("More details about TOEIC...", response.CurrentResponse.Answer);
            Assert.Equal(2, response.ConversationHistory.Count);
        }

        [Fact]
        public async Task ContinueConversation_KhiUserQuestionRong_TraVeBadRequest()
        {
            // Arrange
            var request = new ChatRequestDTO
            {
                UserQuestion = "",
                LessonContent = "Some content"
            };

            // Act
            var result = await _controller.ContinueConversation(request);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            dynamic value = badRequestResult.Value!;
            Assert.Equal("User question cannot be empty.", value.Message);
        }

        [Fact]
        public async Task ContinueConversation_KhiLessonContentRong_TraVeBadRequest()
        {
            // Arrange
            var request = new ChatRequestDTO
            {
                UserQuestion = "What is TOEIC?",
                LessonContent = ""
            };

            // Act
            var result = await _controller.ContinueConversation(request);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            dynamic value = badRequestResult.Value!;
            Assert.Equal("Lesson content cannot be empty.", value.Message);
        }

        [Theory]
        [InlineData(" ")]  // space
        [InlineData("\t")] // tab
        [InlineData("\n")] // newline
        [InlineData("\r")] // carriage return
        public async Task ContinueConversation_KhiUserQuestionWhitespace_TraVeBadRequest(string whitespace)
        {
            // Arrange
            var request = new ChatRequestDTO
            {
                UserQuestion = whitespace,
                LessonContent = "Some content"
            };

            // Act
            var result = await _controller.ContinueConversation(request);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            dynamic value = badRequestResult.Value!;
            Assert.Equal("User question cannot be empty.", value.Message);
        }

        [Fact]
        public async Task ContinueConversation_KhiAIServiceThatBai_TraVeInternalError()
        {
            // Arrange
            var request = new ChatRequestDTO
            {
                UserQuestion = "Tell me more about TOEIC?",
                LessonContent = "Some content"
            };

            var failedResponse = new ChatConversationResponseDTO
            {
                CurrentResponse = new ChatResponseDTO
                {
                    Success = false,
                    Answer = "Error processing request"
                }
            };

            _mockAiChatService
                .Setup(s => s.ContinueConversationAsync(request))
                .ReturnsAsync(failedResponse);

            // Act
            var result = await _controller.ContinueConversation(request);

            // Assert
            var statusCodeResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(500, statusCodeResult.StatusCode);
            var response = Assert.IsType<ChatConversationResponseDTO>(statusCodeResult.Value!);
            Assert.False(response.CurrentResponse.Success);
        }

        [Fact]
        public async Task ContinueConversation_KhiServiceThrowException_TraVeInternalError()
        {
            // Arrange
            var request = new ChatRequestDTO
            {
                UserQuestion = "Tell me more about TOEIC?",
                LessonContent = "Some content"
            };

            _mockAiChatService
                .Setup(s => s.ContinueConversationAsync(request))
                .ThrowsAsync(new Exception("Service error"));

            // Act
            var result = await _controller.ContinueConversation(request);

            // Assert
            var statusCodeResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(500, statusCodeResult.StatusCode);
            dynamic value = statusCodeResult.Value!;
            Assert.Equal("An unexpected error occurred while continuing the conversation.", 
                value.Message);
            _mockLogger.Verify(x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => true),
                It.IsAny<Exception>(),
                It.Is<Func<It.IsAnyType, Exception?, string>>((v, t) => true)), 
                Times.Once);
        }

        [Fact]
        public async Task ContinueConversation_KhiModelStateInvalid_TraVeBadRequest()
        {
            // Arrange
            _controller.ModelState.AddModelError("UserQuestion", "Required");

            // Act
            var result = await _controller.ContinueConversation(new ChatRequestDTO());

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal(400, badRequestResult.StatusCode);
        }
    }
}