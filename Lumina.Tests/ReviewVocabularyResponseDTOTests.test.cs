using DataLayer.DTOs.Vocabulary;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace Lumina.Tests
{
    public class ReviewVocabularyResponseDTOTests
    {
        [Fact]
        public void ReviewVocabularyResponseDTO_WithDefaultValues_ShouldHaveDefaultValues()
        {
            // Arrange & Act
            var dto = new ReviewVocabularyResponseDTO();

            // Assert
            Assert.False(dto.Success);
            Assert.Equal(string.Empty, dto.Message);
            Assert.Null(dto.UpdatedRepetition);
            Assert.Null(dto.NextReviewAt);
            Assert.Equal(0, dto.NewIntervals);
        }

        [Fact]
        public void ReviewVocabularyResponseDTO_WithSuccess_ShouldSetAndGet()
        {
            // Arrange
            var dto = new ReviewVocabularyResponseDTO
            {
                Success = true
            };

            // Act & Assert
            Assert.True(dto.Success);
        }

        [Fact]
        public void ReviewVocabularyResponseDTO_WithMessage_ShouldSetAndGet()
        {
            // Arrange
            var dto = new ReviewVocabularyResponseDTO
            {
                Message = "Review completed successfully"
            };

            // Act & Assert
            Assert.Equal("Review completed successfully", dto.Message);
        }

        [Fact]
        public void ReviewVocabularyResponseDTO_WithUpdatedRepetition_ShouldSetAndGet()
        {
            // Arrange
            var repetition = new SpacedRepetitionDTO
            {
                UserSpacedRepetitionId = 1,
                UserId = 100,
                VocabularyListId = 50
            };
            var dto = new ReviewVocabularyResponseDTO
            {
                UpdatedRepetition = repetition
            };

            // Act & Assert
            Assert.NotNull(dto.UpdatedRepetition);
            Assert.Equal(1, dto.UpdatedRepetition.UserSpacedRepetitionId);
            Assert.Equal(100, dto.UpdatedRepetition.UserId);
            Assert.Equal(50, dto.UpdatedRepetition.VocabularyListId);
        }

        [Fact]
        public void ReviewVocabularyResponseDTO_WithNextReviewAt_ShouldSetAndGet()
        {
            // Arrange
            var nextReview = DateTime.UtcNow.AddDays(7);
            var dto = new ReviewVocabularyResponseDTO
            {
                NextReviewAt = nextReview
            };

            // Act & Assert
            Assert.NotNull(dto.NextReviewAt);
            Assert.Equal(nextReview.Date, dto.NextReviewAt.Value.Date);
        }

        [Fact]
        public void ReviewVocabularyResponseDTO_WithNewIntervals_ShouldSetAndGet()
        {
            // Arrange
            var dto = new ReviewVocabularyResponseDTO
            {
                NewIntervals = 14
            };

            // Act & Assert
            Assert.Equal(14, dto.NewIntervals);
        }

        [Fact]
        public void ReviewVocabularyResponseDTO_WithAllProperties_ShouldSetAndGetAll()
        {
            // Arrange
            var repetition = new SpacedRepetitionDTO { UserSpacedRepetitionId = 1 };
            var nextReview = DateTime.UtcNow.AddDays(10);
            var dto = new ReviewVocabularyResponseDTO
            {
                Success = true,
                Message = "Success",
                UpdatedRepetition = repetition,
                NextReviewAt = nextReview,
                NewIntervals = 20
            };

            // Act & Assert
            Assert.True(dto.Success);
            Assert.Equal("Success", dto.Message);
            Assert.NotNull(dto.UpdatedRepetition);
            Assert.NotNull(dto.NextReviewAt);
            Assert.Equal(20, dto.NewIntervals);
        }

        [Fact]
        public void ReviewVocabularyResponseDTO_WithNullUpdatedRepetition_ShouldAllowNull()
        {
            // Arrange
            var dto = new ReviewVocabularyResponseDTO
            {
                Success = false,
                Message = "Failed",
                UpdatedRepetition = null
            };

            // Act & Assert
            Assert.False(dto.Success);
            Assert.Null(dto.UpdatedRepetition);
        }

        [Fact]
        public void ReviewVocabularyResponseDTO_WithNullNextReviewAt_ShouldAllowNull()
        {
            // Arrange
            var dto = new ReviewVocabularyResponseDTO
            {
                Success = false,
                NextReviewAt = null
            };

            // Act & Assert
            Assert.Null(dto.NextReviewAt);
        }

        [Fact]
        public void ReviewVocabularyResponseDTO_CanChangeProperties_ShouldUpdateCorrectly()
        {
            // Arrange
            var dto = new ReviewVocabularyResponseDTO
            {
                Success = false,
                Message = "Initial",
                NewIntervals = 5
            };

            // Act
            dto.Success = true;
            dto.Message = "Updated";
            dto.NewIntervals = 10;

            // Assert
            Assert.True(dto.Success);
            Assert.Equal("Updated", dto.Message);
            Assert.Equal(10, dto.NewIntervals);
        }

        [Fact]
        public void ReviewVocabularyResponseDTO_PropertyAccessMultipleTimes_ShouldCoverProperty()
        {
            // Arrange
            var dto = new ReviewVocabularyResponseDTO();

            // Act - Access properties multiple times
            var success1 = dto.Success;
            var message1 = dto.Message;
            var intervals1 = dto.NewIntervals;

            dto.Success = true;
            dto.Message = "Test";
            dto.NewIntervals = 7;

            var success2 = dto.Success;
            var message2 = dto.Message;
            var intervals2 = dto.NewIntervals;

            // Assert
            Assert.False(success1);
            Assert.Equal(string.Empty, message1);
            Assert.Equal(0, intervals1);
            Assert.True(success2);
            Assert.Equal("Test", message2);
            Assert.Equal(7, intervals2);
        }

        [Fact]
        public void ReviewVocabularyResponseDTO_UsedInCondition_ShouldEvaluateCorrectly()
        {
            // Arrange
            var dto = new ReviewVocabularyResponseDTO
            {
                Success = true,
                Message = "Success"
            };

            // Act - Use Success in condition to ensure coverage
            string result;
            if (dto.Success)
            {
                result = dto.Message;
            }
            else
            {
                result = "Failed";
            }

            // Assert
            Assert.Equal("Success", result);
        }

        [Fact]
        public void ReviewVocabularyResponseDTO_UsedInCollection_ShouldWorkCorrectly()
        {
            // Arrange
            var responses = new List<ReviewVocabularyResponseDTO>
            {
                new ReviewVocabularyResponseDTO { Success = true, Message = "Success 1", NewIntervals = 5 },
                new ReviewVocabularyResponseDTO { Success = false, Message = "Failed", NewIntervals = 0 },
                new ReviewVocabularyResponseDTO { Success = true, Message = "Success 2", NewIntervals = 10 }
            };

            // Act - Use properties in LINQ queries to ensure coverage
            var successful = responses.Where(r => r.Success).ToList();
            var failed = responses.Where(r => !r.Success).ToList();
            var allIntervals = responses.Select(r => r.NewIntervals).ToList();
            var allMessages = responses.Select(r => r.Message).ToList();

            // Assert
            Assert.Equal(2, successful.Count);
            Assert.Single(failed);
            Assert.Equal(3, allIntervals.Count);
            Assert.Equal(3, allMessages.Count);
            Assert.Contains(5, allIntervals);
            Assert.Contains(10, allIntervals);
        }
    }
}


