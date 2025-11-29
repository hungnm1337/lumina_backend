using Xunit;
using Moq;
using ServiceLayer;
using RepositoryLayer;
using DataLayer.DTOs.ExamPart;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Lumina.Test.Services
{
    public class GetAllExamPartAsyncUnitTest
    {
        private readonly Mock<IExamPartRepository> _mockExamPartRepository;
        private readonly ExamPartService _service;

        public GetAllExamPartAsyncUnitTest()
        {
            _mockExamPartRepository = new Mock<IExamPartRepository>();

            _service = new ExamPartService(
                _mockExamPartRepository.Object
            );
        }

        #region Test Cases - Repository Returns Data

        [Fact]
        public async Task GetAllExamPartAsync_WhenRepositoryReturnsData_ShouldReturnList()
        {
            // Arrange
            var expectedResult = new List<ExamPartDto>
            {
                new ExamPartDto
                {
                    PartId = 1,
                    PartCode = "P1",
                    Title = "Part 1 - Reading",
                    MaxQuestions = 10,
                    SkillType = "Reading",
                    ExamSetKey = "2024-01"
                },
                new ExamPartDto
                {
                    PartId = 2,
                    PartCode = "P2",
                    Title = "Part 2 - Writing",
                    MaxQuestions = 5,
                    SkillType = "Writing",
                    ExamSetKey = "2024-01"
                },
                new ExamPartDto
                {
                    PartId = 3,
                    PartCode = "P1",
                    Title = "Part 1 - Listening",
                    MaxQuestions = 15,
                    SkillType = "Listening",
                    ExamSetKey = "2024-02"
                }
            };

            _mockExamPartRepository
                .Setup(repo => repo.GetAllExamPartsAsync())
                .ReturnsAsync(expectedResult);

            // Act
            var result = await _service.GetAllExamPartAsync();

            // Assert
            Assert.NotNull(result);
            Assert.Equal(3, result.Count);
            Assert.Equal(expectedResult[0].PartId, result[0].PartId);
            Assert.Equal(expectedResult[0].PartCode, result[0].PartCode);
            Assert.Equal(expectedResult[0].Title, result[0].Title);
            Assert.Equal(expectedResult[0].SkillType, result[0].SkillType);
            Assert.Equal(expectedResult[0].ExamSetKey, result[0].ExamSetKey);
            Assert.Equal(expectedResult[1].PartId, result[1].PartId);
            Assert.Equal(expectedResult[2].PartId, result[2].PartId);

            // Verify repository is called exactly once
            _mockExamPartRepository.Verify(
                repo => repo.GetAllExamPartsAsync(),
                Times.Once
            );
        }

        [Fact]
        public async Task GetAllExamPartAsync_WhenRepositoryReturnsSingleItem_ShouldReturnList()
        {
            // Arrange
            var expectedResult = new List<ExamPartDto>
            {
                new ExamPartDto
                {
                    PartId = 1,
                    PartCode = "P1",
                    Title = "Part 1",
                    MaxQuestions = 10,
                    SkillType = "Reading",
                    ExamSetKey = "2024-01"
                }
            };

            _mockExamPartRepository
                .Setup(repo => repo.GetAllExamPartsAsync())
                .ReturnsAsync(expectedResult);

            // Act
            var result = await _service.GetAllExamPartAsync();

            // Assert
            Assert.NotNull(result);
            Assert.Single(result);
            Assert.Equal(expectedResult[0].PartId, result[0].PartId);
            Assert.Equal(expectedResult[0].PartCode, result[0].PartCode);
            Assert.Equal(expectedResult[0].Title, result[0].Title);

            // Verify repository is called exactly once
            _mockExamPartRepository.Verify(
                repo => repo.GetAllExamPartsAsync(),
                Times.Once
            );
        }

        [Fact]
        public async Task GetAllExamPartAsync_WhenRepositoryReturnsDataWithNullFields_ShouldReturnList()
        {
            // Arrange
            var expectedResult = new List<ExamPartDto>
            {
                new ExamPartDto
                {
                    PartId = 1,
                    PartCode = "P1",
                    Title = "Part 1",
                    MaxQuestions = 10,
                    SkillType = null,
                    ExamSetKey = null
                }
            };

            _mockExamPartRepository
                .Setup(repo => repo.GetAllExamPartsAsync())
                .ReturnsAsync(expectedResult);

            // Act
            var result = await _service.GetAllExamPartAsync();

            // Assert
            Assert.NotNull(result);
            Assert.Single(result);
            Assert.Equal(expectedResult[0].PartId, result[0].PartId);
            Assert.Null(result[0].SkillType);
            Assert.Null(result[0].ExamSetKey);

            // Verify repository is called exactly once
            _mockExamPartRepository.Verify(
                repo => repo.GetAllExamPartsAsync(),
                Times.Once
            );
        }

        #endregion

        #region Test Cases - Repository Returns Empty

        [Fact]
        public async Task GetAllExamPartAsync_WhenRepositoryReturnsEmpty_ShouldReturnEmptyList()
        {
            // Arrange
            var expectedResult = new List<ExamPartDto>();

            _mockExamPartRepository
                .Setup(repo => repo.GetAllExamPartsAsync())
                .ReturnsAsync(expectedResult);

            // Act
            var result = await _service.GetAllExamPartAsync();

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result);

            // Verify repository is called exactly once
            _mockExamPartRepository.Verify(
                repo => repo.GetAllExamPartsAsync(),
                Times.Once
            );
        }

        #endregion

        #region Test Cases - Repository Exception Handling

        [Fact]
        public async Task GetAllExamPartAsync_WhenRepositoryThrowsException_ShouldPropagateException()
        {
            // Arrange
            var exceptionMessage = "Database connection error";

            _mockExamPartRepository
                .Setup(repo => repo.GetAllExamPartsAsync())
                .ThrowsAsync(new Exception(exceptionMessage));

            // Act & Assert
            var exception = await Assert.ThrowsAsync<Exception>(
                async () => await _service.GetAllExamPartAsync()
            );

            Assert.Equal(exceptionMessage, exception.Message);

            // Verify repository is called exactly once
            _mockExamPartRepository.Verify(
                repo => repo.GetAllExamPartsAsync(),
                Times.Once
            );
        }

        #endregion
    }
}

