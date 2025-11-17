using lumina.Controllers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using RepositoryLayer.UnitOfWork;
using ServiceLayer.Vocabulary;
using Xunit;

namespace Lumina.Tests
{
    public class TestEndpointVocabularyListsControllerTests
    {
        private readonly Mock<IVocabularyListService> _mockVocabularyListService;
        private readonly Mock<ILogger<VocabularyListsController>> _mockLogger;
        private readonly Mock<IUnitOfWork> _mockUnitOfWork;
        private readonly VocabularyListsController _controller;

        public TestEndpointVocabularyListsControllerTests()
        {
            _mockVocabularyListService = new Mock<IVocabularyListService>();
            _mockLogger = new Mock<ILogger<VocabularyListsController>>();
            _mockUnitOfWork = new Mock<IUnitOfWork>();
            _controller = new VocabularyListsController(_mockVocabularyListService.Object, _mockLogger.Object, _mockUnitOfWork.Object);
        }

        [Fact]
        public void TestEndpoint_ShouldReturnOk()
        {
            // Act
            var result = _controller.TestEndpoint();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.NotNull(okResult.Value);
            // Verify the response contains expected properties
            var resultType = okResult.Value.GetType();
            var messageProperty = resultType.GetProperty("message");
            var timestampProperty = resultType.GetProperty("timestamp");
            Assert.NotNull(messageProperty);
            Assert.NotNull(timestampProperty);
        }
    }
}











