using DataLayer.DTOs;
using lumina.Controllers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using ServiceLayer.TextToSpeech;
using ServiceLayer.UploadFile;
using System;
using System.IO;
using System.Threading.Tasks;
using Xunit;

namespace Lumina.Tests
{
    public class UploadControllerTests
    {
        private readonly Mock<IUploadService> _mockUploadService;
        private readonly Mock<ITextToSpeechService> _mockTextToSpeechService;
        private readonly UploadController _controller;

        public UploadControllerTests()
        {
            _mockUploadService = new Mock<IUploadService>();
            _mockTextToSpeechService = new Mock<ITextToSpeechService>();
            _controller = new UploadController(_mockUploadService.Object, _mockTextToSpeechService.Object);
        }

        #region UploadFile Tests (3 test cases)

        [Fact]
        public async Task UploadFile_ValidFile_ReturnsOkWithUploadResult()
        {
            // Arrange
            var fileMock = new Mock<IFormFile>();
            var content = "fake file content";
            var fileName = "test.jpg";
            var ms = new MemoryStream();
            var writer = new StreamWriter(ms);
            writer.Write(content);
            writer.Flush();
            ms.Position = 0;

            fileMock.Setup(f => f.OpenReadStream()).Returns(ms);
            fileMock.Setup(f => f.FileName).Returns(fileName);
            fileMock.Setup(f => f.Length).Returns(ms.Length);
            fileMock.Setup(f => f.ContentType).Returns("image/jpeg");

            var expectedResult = new UploadResultDTO
            {
                Url = "https://cloudinary.com/test.jpg",
                PublicId = "music_app/images/test_123"
            };

            _mockUploadService.Setup(s => s.UploadFileAsync(It.IsAny<IFormFile>()))
                .ReturnsAsync(expectedResult);

            // Act
            var result = await _controller.UploadFile(fileMock.Object);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var uploadResult = Assert.IsType<UploadResultDTO>(okResult.Value);
            Assert.Equal(expectedResult.Url, uploadResult.Url);
            Assert.Equal(expectedResult.PublicId, uploadResult.PublicId);
            _mockUploadService.Verify(s => s.UploadFileAsync(It.IsAny<IFormFile>()), Times.Once);
        }

        [Fact]
        public async Task UploadFile_NullFile_ReturnsBadRequest()
        {
            // Arrange
            IFormFile file = null;

            // Act
            var result = await _controller.UploadFile(file);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal("Không có file nào được chọn.", badRequestResult.Value);
        }

        [Fact]
        public async Task UploadFile_ServiceThrowsException_ReturnsInternalServerError()
        {
            // Arrange
            var fileMock = new Mock<IFormFile>();
            fileMock.Setup(f => f.Length).Returns(100);
            
            _mockUploadService.Setup(s => s.UploadFileAsync(It.IsAny<IFormFile>()))
                .ThrowsAsync(new Exception("Upload failed"));

            // Act
            var result = await _controller.UploadFile(fileMock.Object);

            // Assert
            var statusCodeResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(500, statusCodeResult.StatusCode);
            Assert.Contains("Upload failed", statusCodeResult.Value.ToString());
        }

        #endregion

        #region UploadFromUrl Tests (3 test cases)

        [Fact]
        public async Task UploadFromUrl_ValidUrl_ReturnsOkWithUploadResult()
        {
            // Arrange
            var request = new UploadFromUrlRequest
            {
                FileUrl = "https://example.com/image.jpg"
            };

            var expectedResult = new UploadResultDTO
            {
                Url = "https://cloudinary.com/uploaded.jpg",
                PublicId = "music_app/images_123"
            };

            _mockUploadService.Setup(s => s.UploadFromUrlAsync(request.FileUrl))
                .ReturnsAsync(expectedResult);

            // Act
            var result = await _controller.UploadFromUrl(request);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var uploadResult = Assert.IsType<UploadResultDTO>(okResult.Value);
            Assert.Equal(expectedResult.Url, uploadResult.Url);
            Assert.Equal(expectedResult.PublicId, uploadResult.PublicId);
            _mockUploadService.Verify(s => s.UploadFromUrlAsync(request.FileUrl), Times.Once);
        }

        [Fact]
        public async Task UploadFromUrl_NullOrEmptyUrl_ReturnsBadRequest()
        {
            // Arrange - Test both null request and empty URL
            UploadFromUrlRequest nullRequest = null;
            var emptyUrlRequest = new UploadFromUrlRequest { FileUrl = "" };

            // Act
            var resultNull = await _controller.UploadFromUrl(nullRequest);
            var resultEmpty = await _controller.UploadFromUrl(emptyUrlRequest);

            // Assert
            var badRequestNull = Assert.IsType<BadRequestObjectResult>(resultNull);
            Assert.Equal("URL không hợp lệ.", badRequestNull.Value);

            var badRequestEmpty = Assert.IsType<BadRequestObjectResult>(resultEmpty);
            Assert.Equal("URL không hợp lệ.", badRequestEmpty.Value);
        }

        [Fact]
        public async Task UploadFromUrl_ServiceThrowsException_ReturnsInternalServerError()
        {
            // Arrange
            var request = new UploadFromUrlRequest
            {
                FileUrl = "https://example.com/invalid.jpg"
            };

            _mockUploadService.Setup(s => s.UploadFromUrlAsync(request.FileUrl))
                .ThrowsAsync(new Exception("Invalid URL format"));

            // Act
            var result = await _controller.UploadFromUrl(request);

            // Assert
            var statusCodeResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(500, statusCodeResult.StatusCode);
            Assert.Contains("Invalid URL format", statusCodeResult.Value.ToString());
        }

        #endregion

        #region GenerateQuestionAudioAsync Tests (3 test cases)

        [Fact]
        public async Task GenerateQuestionAudioAsync_ValidText_ReturnsOkWithUploadResult()
        {
            // Arrange
            var request = new TextToSpeechRequest
            {
                Text = "Hello, this is a test audio."
            };

            var expectedResult = new UploadResultDTO
            {
                Url = "https://cloudinary.com/audio/tts_123.mp3",
                PublicId = "lumina/audio/tts_123"
            };

            _mockTextToSpeechService.Setup(s => s.GenerateAudioAsync(request.Text, It.IsAny<string>()))
                .ReturnsAsync(expectedResult);

            // Act
            var result = await _controller.GenerateQuestionAudioAsync(request);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var uploadResult = Assert.IsType<UploadResultDTO>(okResult.Value);
            Assert.Equal(expectedResult.Url, uploadResult.Url);
            Assert.Equal(expectedResult.PublicId, uploadResult.PublicId);
            _mockTextToSpeechService.Verify(s => s.GenerateAudioAsync(request.Text, It.IsAny<string>()), Times.Once);
        }

        [Fact]
        public async Task GenerateQuestionAudioAsync_NullOrEmptyText_ReturnsBadRequest()
        {
            // Arrange - Test both null request and empty text
            TextToSpeechRequest nullRequest = null;
            var emptyTextRequest = new TextToSpeechRequest { Text = "" };

            // Act
            var resultNull = await _controller.GenerateQuestionAudioAsync(nullRequest);
            var resultEmpty = await _controller.GenerateQuestionAudioAsync(emptyTextRequest);

            // Assert
            var badRequestNull = Assert.IsType<BadRequestObjectResult>(resultNull);
            Assert.Equal("Text không được để trống", badRequestNull.Value);

            var badRequestEmpty = Assert.IsType<BadRequestObjectResult>(resultEmpty);
            Assert.Equal("Text không được để trống", badRequestEmpty.Value);
        }

        [Fact]
        public async Task GenerateQuestionAudioAsync_ServiceThrowsException_ReturnsInternalServerError()
        {
            // Arrange
            var request = new TextToSpeechRequest
            {
                Text = "Test audio generation"
            };

            _mockTextToSpeechService.Setup(s => s.GenerateAudioAsync(request.Text, It.IsAny<string>()))
                .ThrowsAsync(new Exception("Azure Speech API error"));

            // Act
            var result = await _controller.GenerateQuestionAudioAsync(request);

            // Assert
            var statusCodeResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(500, statusCodeResult.StatusCode);
            Assert.Contains("Azure Speech API error", statusCodeResult.Value.ToString());
        }

        #endregion
    }
}