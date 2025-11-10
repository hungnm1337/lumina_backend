// File: QuickAsk.test.cs
using Xunit;
using Moq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using lumina.Controllers;
using ServiceLayer.UserNoteAI;
using DataLayer.DTOs.AI;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Lumina.Tests
{
    public class QuickAskTests
    {
        private readonly Mock<IAIChatService> _mockAiChatService;
        private readonly Mock<ILogger<UserNoteAIChatController>> _mockLogger;
        private readonly UserNoteAIChatController _controller;

        public QuickAskTests()
        {
            _mockAiChatService = new Mock<IAIChatService>();
            _mockLogger = new Mock<ILogger<UserNoteAIChatController>>();
            _controller = new UserNoteAIChatController(_mockAiChatService.Object, _mockLogger.Object);
        }

        [Fact]
        public async Task QuickAsk_KhiRequestHopLeVaCoUserId_TraVeOkVaResponse()
        {
            // Arrange
            var request = new QuickAskRequestDTO
            {
                Question = "What is TOEIC?",
                Context = "TOEIC test information...",
                UserId = 123
            };

            var expectedResponse = new ChatResponseDTO
            {
                Success = true,
                Answer = "TOEIC is a standardized test of English proficiency.",
                SuggestedQuestions = new List<string>
                {
                    "What is a good TOEIC score?",
                    "How to prepare for TOEIC?"
                }
            };

            _mockAiChatService
                .Setup(s => s.AskQuestionAsync(It.Is<ChatRequestDTO>(r => 
                    r.UserQuestion == request.Question && 
                    r.LessonContent == request.Context && 
                    r.UserId == request.UserId)))
                .ReturnsAsync(expectedResponse);

            // Act
            var result = await _controller.QuickAsk(request);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            dynamic value = okResult.Value!;
            Assert.Equal("TOEIC is a standardized test of English proficiency.", value.Answer);
            Assert.Equal(2, ((List<string>)value.SuggestedQuestions).Count);
            Assert.True(value.Success);
        }

        [Fact]
        public async Task QuickAsk_KhiRequestHopLeKhongCoUserId_TraVeOkVaResponse()
        {
            // Arrange
            var request = new QuickAskRequestDTO
            {
                Question = "What is TOEIC?",
                Context = "TOEIC test information...",
                UserId = null
            };

            var expectedResponse = new ChatResponseDTO
            {
                Success = true,
                Answer = "TOEIC is a test.",
                SuggestedQuestions = new List<string> { "Tell me more?" }
            };

            _mockAiChatService
                .Setup(s => s.AskQuestionAsync(It.Is<ChatRequestDTO>(r => 
                    r.UserQuestion == request.Question && 
                    r.LessonContent == request.Context && 
                    r.UserId == null)))
                .ReturnsAsync(expectedResponse);

            // Act
            var result = await _controller.QuickAsk(request);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            dynamic value = okResult.Value!;
            Assert.Equal("TOEIC is a test.", value.Answer);
            Assert.Single((List<string>)value.SuggestedQuestions);
            Assert.True(value.Success);
        }

        [Fact]
        public async Task QuickAsk_KhiModelStateInvalid_TraVeBadRequest()
        {
            // Arrange
            _controller.ModelState.AddModelError("Question", "Required");

            // Act
            var result = await _controller.QuickAsk(new QuickAskRequestDTO());

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal(400, badRequestResult.StatusCode);
        }

        [Fact]
        public async Task QuickAsk_KhiAIServiceThatBai_TraVeInternalError()
        {
            // Arrange
            var request = new QuickAskRequestDTO
            {
                Question = "What is TOEIC?",
                Context = "TOEIC content...",
                UserId = 1
            };

            var failedResponse = new ChatResponseDTO
            {
                Success = false,
                Answer = "Error processing request"
            };

            _mockAiChatService
                .Setup(s => s.AskQuestionAsync(It.IsAny<ChatRequestDTO>()))
                .ReturnsAsync(failedResponse);

            // Act
            var result = await _controller.QuickAsk(request);

            // Assert
            var statusCodeResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(500, statusCodeResult.StatusCode);
            var response = Assert.IsType<ChatResponseDTO>(statusCodeResult.Value!);
            Assert.False(response.Success);
        }

        [Fact]
        public async Task QuickAsk_KhiServiceThrowException_TraVeInternalError()
        {
            // Arrange
            var request = new QuickAskRequestDTO
            {
                Question = "What is TOEIC?",
                Context = "TOEIC content...",
                UserId = 1
            };

            _mockAiChatService
                .Setup(s => s.AskQuestionAsync(It.IsAny<ChatRequestDTO>()))
                .ThrowsAsync(new Exception("Service error"));

            // Act
            var result = await _controller.QuickAsk(request);

            // Assert
            var statusCodeResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(500, statusCodeResult.StatusCode);
            dynamic value = statusCodeResult.Value!;
            Assert.Equal("An unexpected error occurred.", value.Message);
            _mockLogger.Verify(x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => true),
                It.IsAny<Exception>(),
                It.Is<Func<It.IsAnyType, Exception?, string>>((v, t) => true)), 
                Times.Once);
        }

        [Fact]
        public async Task QuickAsk_KhiRequestVoiEmptyStrings_TraVeOkNeuServiceThanhCong()
        {
            // Arrange
            var request = new QuickAskRequestDTO
            {
                Question = "",
                Context = "",
                UserId = null
            };

            var expectedResponse = new ChatResponseDTO
            {
                Success = true,
                Answer = "Response",
                SuggestedQuestions = new List<string>()
            };

            _mockAiChatService
                .Setup(s => s.AskQuestionAsync(It.IsAny<ChatRequestDTO>()))
                .ReturnsAsync(expectedResponse);

            // Act
            var result = await _controller.QuickAsk(request);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            dynamic value = okResult.Value!;
            Assert.True(value.Success);
        }
    }
}
