// File: GetSuggestedQuestions.test.cs
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
    public class GetSuggestedQuestionsTests
    {
        private readonly Mock<IAIChatService> _mockAiChatService;
        private readonly Mock<ILogger<UserNoteAIChatController>> _mockLogger;
        private readonly UserNoteAIChatController _controller;

        public GetSuggestedQuestionsTests()
        {
            _mockAiChatService = new Mock<IAIChatService>();
            _mockLogger = new Mock<ILogger<UserNoteAIChatController>>();
            _controller = new UserNoteAIChatController(_mockAiChatService.Object, _mockLogger.Object);
        }

        [Fact]
        public async Task GetSuggestedQuestions_KhiRequestHopLeVaCoTitle_TraVeOkVaDanhSachCauHoi()
        {
            // Arrange
            var request = new LessonContentRequestDTO
            {
                LessonContent = "TOEIC is a standardized test of English proficiency...",
                LessonTitle = "Introduction to TOEIC"
            };

            var expectedResponse = new ChatResponseDTO
            {
                Success = true,
                SuggestedQuestions = new List<string>
                {
                    "What is TOEIC?",
                    "How to prepare for TOEIC?",
                    "What is a good TOEIC score?"
                }
            };

            _mockAiChatService
                .Setup(s => s.GenerateSuggestedQuestionsAsync(request.LessonContent, request.LessonTitle))
                .ReturnsAsync(expectedResponse);

            // Act
            var result = await _controller.GetSuggestedQuestions(request);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var response = Assert.IsType<ChatResponseDTO>(okResult.Value);
            Assert.True(response.Success);
            Assert.Equal(3, response.SuggestedQuestions.Count);
            _mockAiChatService.Verify(s => s.GenerateSuggestedQuestionsAsync(request.LessonContent, request.LessonTitle), Times.Once);
        }

        [Fact]
        public async Task GetSuggestedQuestions_KhiRequestHopLeKhongCoTitle_TraVeOkVaDanhSachCauHoi()
        {
            // Arrange
            var request = new LessonContentRequestDTO
            {
                LessonContent = "TOEIC test information...",
                LessonTitle = null
            };

            var expectedResponse = new ChatResponseDTO
            {
                Success = true,
                SuggestedQuestions = new List<string>
                {
                    "What does this lesson cover?",
                    "Can you explain more?"
                }
            };

            _mockAiChatService
                .Setup(s => s.GenerateSuggestedQuestionsAsync(request.LessonContent, null))
                .ReturnsAsync(expectedResponse);

            // Act
            var result = await _controller.GetSuggestedQuestions(request);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var response = Assert.IsType<ChatResponseDTO>(okResult.Value);
            Assert.True(response.Success);
            Assert.Equal(2, response.SuggestedQuestions.Count);
        }

        [Fact]
        public async Task GetSuggestedQuestions_KhiLessonContentRong_TraVeBadRequest()
        {
            // Arrange
            var request = new LessonContentRequestDTO
            {
                LessonContent = "",
                LessonTitle = "TOEIC Intro"
            };

            // Act
            var result = await _controller.GetSuggestedQuestions(request);

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
        public async Task GetSuggestedQuestions_KhiLessonContentWhitespace_TraVeBadRequest(string whitespace)
        {
            // Arrange
            var request = new LessonContentRequestDTO
            {
                LessonContent = whitespace,
                LessonTitle = "TOEIC Intro"
            };

            // Act
            var result = await _controller.GetSuggestedQuestions(request);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            dynamic value = badRequestResult.Value!;
            Assert.Equal("Lesson content cannot be empty.", value.Message);
        }

        [Fact]
        public async Task GetSuggestedQuestions_KhiModelStateInvalid_TraVeBadRequest()
        {
            // Arrange
            _controller.ModelState.AddModelError("LessonContent", "Required");

            // Act
            var result = await _controller.GetSuggestedQuestions(new LessonContentRequestDTO());

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal(400, badRequestResult.StatusCode);
        }

        [Fact]
        public async Task GetSuggestedQuestions_KhiAIServiceThatBai_TraVeInternalError()
        {
            // Arrange
            var request = new LessonContentRequestDTO
            {
                LessonContent = "TOEIC content...",
                LessonTitle = "TOEIC"
            };

            var failedResponse = new ChatResponseDTO
            {
                Success = false,
                SuggestedQuestions = new List<string>()
            };

            _mockAiChatService
                .Setup(s => s.GenerateSuggestedQuestionsAsync(request.LessonContent, request.LessonTitle))
                .ReturnsAsync(failedResponse);

            // Act
            var result = await _controller.GetSuggestedQuestions(request);

            // Assert
            var statusCodeResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(500, statusCodeResult.StatusCode);
            var response = Assert.IsType<ChatResponseDTO>(statusCodeResult.Value!);
            Assert.False(response.Success);
        }

        [Fact]
        public async Task GetSuggestedQuestions_KhiServiceThrowException_TraVeInternalError()
        {
            // Arrange
            var request = new LessonContentRequestDTO
            {
                LessonContent = "TOEIC content...",
                LessonTitle = "TOEIC"
            };

            _mockAiChatService
                .Setup(s => s.GenerateSuggestedQuestionsAsync(request.LessonContent, request.LessonTitle))
                .ThrowsAsync(new Exception("Service error"));

            // Act
            var result = await _controller.GetSuggestedQuestions(request);

            // Assert
            var statusCodeResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(500, statusCodeResult.StatusCode);
            dynamic value = statusCodeResult.Value!;
            Assert.Equal("An unexpected error occurred while generating suggested questions.", 
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
