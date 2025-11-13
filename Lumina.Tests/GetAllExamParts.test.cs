// File: GetAllExamParts.test.cs
using Xunit;
using Moq;
using Microsoft.AspNetCore.Mvc;
using lumina.Controllers;
using ServiceLayer;
using DataLayer.DTOs.ExamPart;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Lumina.Tests
{
    public class GetAllExamPartsTests
    {
        private readonly Mock<IExamPartService> _mockExamService;
        private readonly ExamPartController _controller;

        public GetAllExamPartsTests()
        {
            _mockExamService = new Mock<IExamPartService>();
            _controller = new ExamPartController(_mockExamService.Object);
        }

        [Fact]
        public async Task GetAllExamParts_KhiCoData_TraVeOkVaDanhSach()
        {
            // Arrange
            var expectedParts = new List<ExamPartDto>
            {
                new ExamPartDto
                {
                    PartId = 1,
                    PartCode = "P1",
                    Title = "Part 1: Pictures",
                    MaxQuestions = 6,
                    SkillType = "Listening",
                    ExamSetKey = "10-2025"
                },
                new ExamPartDto
                {
                    PartId = 2,
                    PartCode = "P2",
                    Title = "Part 2: Question-Response",
                    MaxQuestions = 25,
                    SkillType = "Listening",
                    ExamSetKey = "10-2025"
                }
            };

            _mockExamService
                .Setup(s => s.GetAllExamPartAsync())
                .ReturnsAsync(expectedParts);

            // Act
            var result = await _controller.GetAllExamParts();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var returnedParts = Assert.IsType<List<ExamPartDto>>(okResult.Value);
            Assert.Equal(2, returnedParts.Count);
            Assert.Equal(1, returnedParts[0].PartId);
            Assert.Equal(2, returnedParts[1].PartId);
            Assert.Equal("P1", returnedParts[0].PartCode);
            Assert.Equal("P2", returnedParts[1].PartCode);
            _mockExamService.Verify(s => s.GetAllExamPartAsync(), Times.Once);
        }

        [Fact]
        public async Task GetAllExamParts_KhiDanhSachRong_TraVeOkVaDanhSachRong()
        {
            // Arrange
            var emptyParts = new List<ExamPartDto>();

            _mockExamService
                .Setup(s => s.GetAllExamPartAsync())
                .ReturnsAsync(emptyParts);

            // Act
            var result = await _controller.GetAllExamParts();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var returnedParts = Assert.IsType<List<ExamPartDto>>(okResult.Value);
            Assert.Empty(returnedParts);
            _mockExamService.Verify(s => s.GetAllExamPartAsync(), Times.Once);
        }

        [Fact]
        public async Task GetAllExamParts_KhiServiceThrowException_ThrowException()
        {
            // Arrange
            _mockExamService
                .Setup(s => s.GetAllExamPartAsync())
                .ThrowsAsync(new System.Exception("Database error"));

            // Act & Assert
            await Assert.ThrowsAsync<System.Exception>(async () => 
                await _controller.GetAllExamParts());

            _mockExamService.Verify(s => s.GetAllExamPartAsync(), Times.Once);
        }
    }
}