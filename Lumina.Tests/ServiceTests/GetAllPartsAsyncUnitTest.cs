using Xunit;
using Moq;
using ServiceLayer;
using RepositoryLayer;
using DataLayer.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Lumina.Test.Services
{
    public class GetAllPartsAsyncUnitTest
    {
        private readonly Mock<IExamPartRepository> _mockExamPartRepository;
        private readonly ExamPartService _service;

        public GetAllPartsAsyncUnitTest()
        {
            _mockExamPartRepository = new Mock<IExamPartRepository>();

            _service = new ExamPartService(
                _mockExamPartRepository.Object
            );
        }

        #region Test Cases - Repository Returns Data

        [Fact]
        public async Task GetAllPartsAsync_WhenRepositoryReturnsData_ShouldReturnList()
        {
            // Arrange
            var expectedResult = new List<ExamPart>
            {
                new ExamPart
                {
                    PartId = 1,
                    ExamId = 1,
                    PartCode = "P1",
                    Title = "Part 1 - Reading",
                    OrderIndex = 1,
                    MaxQuestions = 10
                },
                new ExamPart
                {
                    PartId = 2,
                    ExamId = 1,
                    PartCode = "P2",
                    Title = "Part 2 - Writing",
                    OrderIndex = 2,
                    MaxQuestions = 5
                },
                new ExamPart
                {
                    PartId = 3,
                    ExamId = 2,
                    PartCode = "P1",
                    Title = "Part 1 - Listening",
                    OrderIndex = 1,
                    MaxQuestions = 15
                }
            };

            _mockExamPartRepository
                .Setup(repo => repo.GetAllPartsAsync())
                .ReturnsAsync(expectedResult);

            // Act
            var result = await _service.GetAllPartsAsync();

            // Assert
            Assert.NotNull(result);
            var resultList = result.ToList();
            Assert.Equal(3, resultList.Count);
            Assert.Equal(expectedResult[0].PartId, resultList[0].PartId);
            Assert.Equal(expectedResult[0].PartCode, resultList[0].PartCode);
            Assert.Equal(expectedResult[0].Title, resultList[0].Title);
            Assert.Equal(expectedResult[1].PartId, resultList[1].PartId);
            Assert.Equal(expectedResult[2].PartId, resultList[2].PartId);

            // Verify repository is called exactly once
            _mockExamPartRepository.Verify(
                repo => repo.GetAllPartsAsync(),
                Times.Once
            );
        }

        [Fact]
        public async Task GetAllPartsAsync_WhenRepositoryReturnsSingleItem_ShouldReturnList()
        {
            // Arrange
            var expectedResult = new List<ExamPart>
            {
                new ExamPart
                {
                    PartId = 1,
                    ExamId = 1,
                    PartCode = "P1",
                    Title = "Part 1",
                    OrderIndex = 1,
                    MaxQuestions = 10
                }
            };

            _mockExamPartRepository
                .Setup(repo => repo.GetAllPartsAsync())
                .ReturnsAsync(expectedResult);

            // Act
            var result = await _service.GetAllPartsAsync();

            // Assert
            Assert.NotNull(result);
            var resultList = result.ToList();
            Assert.Single(resultList);
            Assert.Equal(expectedResult[0].PartId, resultList[0].PartId);
            Assert.Equal(expectedResult[0].PartCode, resultList[0].PartCode);

            // Verify repository is called exactly once
            _mockExamPartRepository.Verify(
                repo => repo.GetAllPartsAsync(),
                Times.Once
            );
        }

        #endregion

        #region Test Cases - Repository Returns Empty

        [Fact]
        public async Task GetAllPartsAsync_WhenRepositoryReturnsEmpty_ShouldReturnEmptyList()
        {
            // Arrange
            var expectedResult = new List<ExamPart>();

            _mockExamPartRepository
                .Setup(repo => repo.GetAllPartsAsync())
                .ReturnsAsync(expectedResult);

            // Act
            var result = await _service.GetAllPartsAsync();

            // Assert
            Assert.NotNull(result);
            var resultList = result.ToList();
            Assert.Empty(resultList);

            // Verify repository is called exactly once
            _mockExamPartRepository.Verify(
                repo => repo.GetAllPartsAsync(),
                Times.Once
            );
        }

        #endregion

        #region Test Cases - Repository Exception Handling

        [Fact]
        public async Task GetAllPartsAsync_WhenRepositoryThrowsException_ShouldPropagateException()
        {
            // Arrange
            var exceptionMessage = "Database connection error";

            _mockExamPartRepository
                .Setup(repo => repo.GetAllPartsAsync())
                .ThrowsAsync(new Exception(exceptionMessage));

            // Act & Assert
            var exception = await Assert.ThrowsAsync<Exception>(
                async () => await _service.GetAllPartsAsync()
            );

            Assert.Equal(exceptionMessage, exception.Message);

            // Verify repository is called exactly once
            _mockExamPartRepository.Verify(
                repo => repo.GetAllPartsAsync(),
                Times.Once
            );
        }

        #endregion
    }
}

