// File: GetUserNoteByUserIDAndArticleId.test.cs
using Xunit;
using Moq;
using Microsoft.AspNetCore.Mvc;
using lumina.Controllers;
using ServiceLayer.UserNote;
using DataLayer.DTOs.UserNote;
using System;
using System.Threading.Tasks;

#pragma warning disable CS8620 // Argument cannot be used for parameter due to differences in the nullability of reference types.

namespace Lumina.Tests
{
    public class GetUserNoteByUserIDAndArticleIdTests
    {
        private readonly Mock<IUserNoteService> _mockUserNoteService;
        private readonly UserNoteController _controller;

        public GetUserNoteByUserIDAndArticleIdTests()
        {
            _mockUserNoteService = new Mock<IUserNoteService>();
            _controller = new UserNoteController(_mockUserNoteService.Object);
        }

        [Fact]
       public async Task GetUserNoteByUserIDAndArticleId_KhiUserIdMotVaArticleIdMotVaSectionIdMot_TraVeOkVoiUserNote()
       {
           // Arrange
           int userId = 1;
           int articleId = 1;
           int sectionId = 1;
           var expectedUserNote = new UserNoteResponseDTO
           {
               NoteId = 1,
               UserId = userId,
               ArticleId = articleId,
               SectionId = sectionId,
               NoteContent = "Test note"
           };

           _mockUserNoteService
               .Setup(s => s.GetUserNoteByUserIDAndArticleId(userId, articleId, sectionId))
               .ReturnsAsync(expectedUserNote);

           // Act
           var result = await _controller.GetUserNoteByUserIDAndArticleId(userId, articleId, sectionId);

           // Assert
           var okResult = Assert.IsType<OkObjectResult>(result);
           var userNote = Assert.IsType<UserNoteResponseDTO>(okResult.Value);
           Assert.Equal(userId, userNote.UserId);
           Assert.Equal(articleId, userNote.ArticleId);
           Assert.Equal(sectionId, userNote.SectionId);
           _mockUserNoteService.Verify(s => s.GetUserNoteByUserIDAndArticleId(userId, articleId, sectionId), Times.Once);
       }

       [Fact]
       public async Task GetUserNoteByUserIDAndArticleId_KhiUserIdKhongVaArticleIdKhongVaSectionIdKhong_TraVeNotFound()
       {
           // Arrange
           int userId = 0;
           int articleId = 0;
           int sectionId = 0;

           _mockUserNoteService
               .Setup(s => s.GetUserNoteByUserIDAndArticleId(userId, articleId, sectionId))
               .ReturnsAsync(null as UserNoteResponseDTO);

           // Act
           var result = await _controller.GetUserNoteByUserIDAndArticleId(userId, articleId, sectionId);

           // Assert
           var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
           var value = notFoundResult.Value;
           Assert.NotNull(value);
           var messageProperty = value.GetType().GetProperty("Message");
           Assert.NotNull(messageProperty);
           var message = messageProperty.GetValue(value)?.ToString();
           Assert.Equal("User note not found.", message);
           _mockUserNoteService.Verify(s => s.GetUserNoteByUserIDAndArticleId(userId, articleId, sectionId), Times.Once);
       }

       [Fact]
       public async Task GetUserNoteByUserIDAndArticleId_KhiUserIdAmMotVaArticleIdAmMotVaSectionIdAmMot_TraVeNotFound()
       {
           // Arrange
           int userId = -1;
           int articleId = -1;
           int sectionId = -1;

           _mockUserNoteService
               .Setup(s => s.GetUserNoteByUserIDAndArticleId(userId, articleId, sectionId))
               .ReturnsAsync(null as UserNoteResponseDTO);

           // Act
           var result = await _controller.GetUserNoteByUserIDAndArticleId(userId, articleId, sectionId);

           // Assert
           var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
           var value = notFoundResult.Value;
           Assert.NotNull(value);
           var messageProperty = value.GetType().GetProperty("Message");
           Assert.NotNull(messageProperty);
           var message = messageProperty.GetValue(value)?.ToString();
           Assert.Equal("User note not found.", message);
           _mockUserNoteService.Verify(s => s.GetUserNoteByUserIDAndArticleId(userId, articleId, sectionId), Times.Once);
       }

       [Fact]
       public async Task GetUserNoteByUserIDAndArticleId_KhiUserIdKhongTonTai_TraVeNotFound()
       {
           // Arrange
           int userId = 999;
           int articleId = 999;
           int sectionId = 999;

           _mockUserNoteService
               .Setup(s => s.GetUserNoteByUserIDAndArticleId(userId, articleId, sectionId))
               .ReturnsAsync(null as UserNoteResponseDTO);

           // Act
           var result = await _controller.GetUserNoteByUserIDAndArticleId(userId, articleId, sectionId);

           // Assert
           var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
           var value = notFoundResult.Value;
           Assert.NotNull(value);
           var messageProperty = value.GetType().GetProperty("Message");
           Assert.NotNull(messageProperty);
           var message = messageProperty.GetValue(value)?.ToString();
           Assert.Equal("User note not found.", message);
           _mockUserNoteService.Verify(s => s.GetUserNoteByUserIDAndArticleId(userId, articleId, sectionId), Times.Once);
       }

       [Fact]
       public async Task GetUserNoteByUserIDAndArticleId_KhiServiceThrowException_TraVeInternalServerError()
       {
           // Arrange
           int userId = 1;
           int articleId = 1;
           int sectionId = 1;

           _mockUserNoteService
               .Setup(s => s.GetUserNoteByUserIDAndArticleId(userId, articleId, sectionId))
               .ThrowsAsync(new Exception("Database error"));

           // Act
           var result = await _controller.GetUserNoteByUserIDAndArticleId(userId, articleId, sectionId);

            // Assert
            var statusCodeResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(500, statusCodeResult.StatusCode);
            
            var value = statusCodeResult.Value;
            Assert.NotNull(value);
            var messageProperty = value.GetType().GetProperty("Message");
            Assert.NotNull(messageProperty);
            var message = messageProperty.GetValue(value)?.ToString();
            Assert.Equal("An error occurred while retrieving the user note.", message);
        }
    }
}
