// File: GetExamParts.test.cs
using Xunit;
using Moq;
using Microsoft.AspNetCore.Mvc;
using lumina.Controllers;
using DataLayer.Models;
using System.Collections.Generic;
using System.Threading.Tasks;
using System;
using ServiceLayer;

namespace Lumina.Tests
{
    public class GetExamPartsTests
    {
        private readonly Mock<IExamPartService> _mockExamService;
        private readonly ExamPartController _controller;

        public GetExamPartsTests()
        {
            _mockExamService = new Mock<IExamPartService>();
            _controller = new ExamPartController(_mockExamService.Object);
        }

        [Fact]
        public async Task GetExamParts_KhiCoData_TraVeOkVaDanhSach()
        {
            // Arrange
            var expectedParts = new List<ExamPart>
            {
                new ExamPart
                {
                    PartId = 1,
                    Title = "Part 1: Picture Description",
                    PartCode = "P1",
                    ExamId = 1,
                    OrderIndex = 1
                },
                new ExamPart
                {
                    PartId = 2,
                    Title = "Part 2: Question-Response",
                    PartCode = "P2",
                    ExamId = 1,
                    OrderIndex = 2
                }
            };

            _mockExamService
                .Setup(s => s.GetAllPartsAsync())
                .ReturnsAsync(expectedParts);

            // Act
            var result = await _controller.GetExamParts();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var returnedParts = Assert.IsType<List<ExamPart>>(okResult.Value);
            Assert.Equal(2, returnedParts.Count);
            Assert.Equal(1, returnedParts[0].PartId);
            Assert.Equal(2, returnedParts[1].PartId);
            _mockExamService.Verify(s => s.GetAllPartsAsync(), Times.Once);
        }

        [Fact]
        public async Task GetExamParts_KhiDanhSachRong_TraVeOkVaDanhSachRong()
        {
            // Arrange
            var emptyParts = new List<ExamPart>();

            _mockExamService
                .Setup(s => s.GetAllPartsAsync())
                .ReturnsAsync(emptyParts);

            // Act
            var result = await _controller.GetExamParts();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var returnedParts = Assert.IsType<List<ExamPart>>(okResult.Value);
            Assert.Empty(returnedParts);
            _mockExamService.Verify(s => s.GetAllPartsAsync(), Times.Once);
        }

        [Fact]
        public async Task GetExamParts_KhiServiceThrowException_TraVe500()
        {
            // Arrange
            string errorMessage = "Database connection error";
            _mockExamService
                .Setup(s => s.GetAllPartsAsync())
                .ThrowsAsync(new Exception(errorMessage));

            // Act
            var result = await _controller.GetExamParts();

            // Assert
            var statusCodeResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(500, statusCodeResult.StatusCode);
            Assert.Equal($"Lỗi khi lấy dữ liệu Part: {errorMessage}", statusCodeResult.Value);
            _mockExamService.Verify(s => s.GetAllPartsAsync(), Times.Once);
        }
    }
}