using System;
using System.Threading.Tasks;
using ServiceLayer.Exam.Writting;
using Xunit;

namespace Lumina.Test.Services
{
    public class GenerateContentAsyncUnitTest
    {
        private const string TestApiKey = "test-api-key";

        [Fact]
        public async Task GenerateContentAsync_WhenPromptIsNull_ShouldThrowArgumentNullException()
        {
            // Arrange
            var service = new GenerativeAIService(TestApiKey);

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentNullException>(() => service.GenerateContentAsync(null!));
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        public async Task GenerateContentAsync_WhenPromptIsEmptyOrWhitespace_ShouldThrowArgumentException(string prompt)
        {
            // Arrange
            var service = new GenerativeAIService(TestApiKey);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<ArgumentException>(() => service.GenerateContentAsync(prompt));
            Assert.Equal("prompt", exception.ParamName);
        }

        [Fact]
        public async Task GenerateContentAsync_WhenPromptIsValid_ShouldReturnTestResponseInDebug()
        {
            // Arrange
            var service = new GenerativeAIService(TestApiKey);
            var prompt = "valid prompt";

            // Act
            var result = await service.GenerateContentAsync(prompt);

            // Assert
            Assert.NotNull(result);
            Assert.Equal($"Test response for prompt: {prompt}", result);
        }
    }
}


