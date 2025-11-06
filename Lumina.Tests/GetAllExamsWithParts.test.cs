// File: GetAllExamsWithParts.test.cs
using Xunit;
using Moq;
using Microsoft.AspNetCore.Mvc;
using lumina.Controllers;
using ServiceLayer.Exam;
using DataLayer.DTOs.ExamPart;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Lumina.Tests
{
    public class GetAllExamsWithPartsTests
    {
        private readonly Mock<IExamService> _mockExamService;
        private readonly ExamController _controller;

        public GetAllExamsWithPartsTests()
        {
            _mockExamService = new Mock<IExamService>();
            _controller = new ExamController(_mockExamService.Object);
        }

        [Fact]
        public async Task GetAllExamsWithParts_KhiCoData_TraVeOkVaDanhSach()
        {
            // Arrange
            var expectedData = new List<ExamGroupBySetKeyDto>
            {
                new ExamGroupBySetKeyDto
                {
                    ExamSetKey = "10-2025",
                    Exams = new List<ExamWithPartsDto>
                    {
                        new ExamWithPartsDto
                        {
                            ExamId = 1,
                            Name = "TOEIC Test 1",
                            IsActive = true,
                            Description = "Test description",
                            Parts = new List<ExamPartBriefDto>()
                        }
                    }
                }
            };

            _mockExamService
                .Setup(s => s.GetExamsGroupedBySetKeyAsync())
                .ReturnsAsync(expectedData);

            // Act
            var result = await _controller.GetAllExamsWithParts();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var returnedData = Assert.IsType<List<ExamGroupBySetKeyDto>>(okResult.Value);
            Assert.Single(returnedData);
            Assert.Equal("10-2025", returnedData[0].ExamSetKey);
            Assert.Single(returnedData[0].Exams);
            Assert.Equal(1, returnedData[0].Exams[0].ExamId);
            _mockExamService.Verify(s => s.GetExamsGroupedBySetKeyAsync(), Times.Once);
        }

        [Fact]
        public async Task GetAllExamsWithParts_KhiDanhSachRong_TraVeOkVaDanhSachRong()
        {
            // Arrange
            var emptyData = new List<ExamGroupBySetKeyDto>();

            _mockExamService
                .Setup(s => s.GetExamsGroupedBySetKeyAsync())
                .ReturnsAsync(emptyData);

            // Act
            var result = await _controller.GetAllExamsWithParts();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var returnedData = Assert.IsType<List<ExamGroupBySetKeyDto>>(okResult.Value);
            Assert.Empty(returnedData);
            _mockExamService.Verify(s => s.GetExamsGroupedBySetKeyAsync(), Times.Once);
        }

        [Fact]
        public async Task GetAllExamsWithParts_KhiCoNhieuExamSetKey_TraVeOkVaDanhSachDayDu()
        {
            // Arrange
            var expectedData = new List<ExamGroupBySetKeyDto>
            {
                new ExamGroupBySetKeyDto
                {
                    ExamSetKey = "10-2025",
                    Exams = new List<ExamWithPartsDto>
                    {
                        new ExamWithPartsDto
                        {
                            ExamId = 1,
                            Name = "TOEIC Test 1",
                            IsActive = true,
                            Description = "Test 1",
                            Parts = new List<ExamPartBriefDto>()
                        }
                    }
                },
                new ExamGroupBySetKeyDto
                {
                    ExamSetKey = "11-2025",
                    Exams = new List<ExamWithPartsDto>
                    {
                        new ExamWithPartsDto
                        {
                            ExamId = 2,
                            Name = "TOEIC Test 2",
                            IsActive = false,
                            Description = "Test 2",
                            Parts = new List<ExamPartBriefDto>()
                        }
                    }
                }
            };

            _mockExamService
                .Setup(s => s.GetExamsGroupedBySetKeyAsync())
                .ReturnsAsync(expectedData);

            // Act
            var result = await _controller.GetAllExamsWithParts();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var returnedData = Assert.IsType<List<ExamGroupBySetKeyDto>>(okResult.Value);
            Assert.Equal(2, returnedData.Count);
            Assert.Equal("10-2025", returnedData[0].ExamSetKey);
            Assert.Equal("11-2025", returnedData[1].ExamSetKey);
            _mockExamService.Verify(s => s.GetExamsGroupedBySetKeyAsync(), Times.Once);
        }

        [Fact]
        public async Task GetAllExamsWithParts_KhiServiceThrowException_ThrowException()
        {
            // Arrange
            _mockExamService
                .Setup(s => s.GetExamsGroupedBySetKeyAsync())
                .ThrowsAsync(new System.Exception("Database connection error"));

            // Act & Assert
            await Assert.ThrowsAsync<System.Exception>(async () =>
                await _controller.GetAllExamsWithParts());

            _mockExamService.Verify(s => s.GetExamsGroupedBySetKeyAsync(), Times.Once);
        }
    }
}
