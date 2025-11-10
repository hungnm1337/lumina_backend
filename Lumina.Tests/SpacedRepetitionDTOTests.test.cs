using DataLayer.DTOs.Vocabulary;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace Lumina.Tests
{
    public class SpacedRepetitionDTOTests
    {
        [Fact]
        public void SpacedRepetitionDTO_WithDefaultValues_ShouldHaveDefaultValues()
        {
            // Arrange & Act
            var dto = new SpacedRepetitionDTO();

            // Assert
            Assert.Equal(0, dto.UserSpacedRepetitionId);
            Assert.Equal(0, dto.UserId);
            Assert.Equal(0, dto.VocabularyListId);
            Assert.Equal(string.Empty, dto.VocabularyListName);
            Assert.Equal(default(DateTime), dto.LastReviewedAt);
            Assert.Null(dto.NextReviewAt);
            Assert.Equal(0, dto.ReviewCount);
            Assert.Equal(0, dto.Intervals);
            Assert.Equal(string.Empty, dto.Status);
            Assert.False(dto.IsDue);
            Assert.Equal(0, dto.DaysUntilReview);
        }

        [Fact]
        public void SpacedRepetitionDTO_WithAllProperties_ShouldSetAndGetAll()
        {
            // Arrange
            var lastReviewed = DateTime.UtcNow.AddDays(-5);
            var nextReview = DateTime.UtcNow.AddDays(2);
            var dto = new SpacedRepetitionDTO
            {
                UserSpacedRepetitionId = 1,
                UserId = 100,
                VocabularyListId = 50,
                VocabularyListName = "Test List",
                LastReviewedAt = lastReviewed,
                NextReviewAt = nextReview,
                ReviewCount = 5,
                Intervals = 7,
                Status = "Active",
                IsDue = true,
                DaysUntilReview = 2
            };

            // Act & Assert
            Assert.Equal(1, dto.UserSpacedRepetitionId);
            Assert.Equal(100, dto.UserId);
            Assert.Equal(50, dto.VocabularyListId);
            Assert.Equal("Test List", dto.VocabularyListName);
            Assert.Equal(lastReviewed.Date, dto.LastReviewedAt.Date);
            Assert.NotNull(dto.NextReviewAt);
            Assert.Equal(nextReview.Date, dto.NextReviewAt.Value.Date);
            Assert.Equal(5, dto.ReviewCount);
            Assert.Equal(7, dto.Intervals);
            Assert.Equal("Active", dto.Status);
            Assert.True(dto.IsDue);
            Assert.Equal(2, dto.DaysUntilReview);
        }

        [Fact]
        public void SpacedRepetitionDTO_WithNullNextReviewAt_ShouldAllowNull()
        {
            // Arrange
            var dto = new SpacedRepetitionDTO
            {
                NextReviewAt = null
            };

            // Act & Assert
            Assert.Null(dto.NextReviewAt);
        }

        [Fact]
        public void SpacedRepetitionDTO_CanChangeProperties_ShouldUpdateCorrectly()
        {
            // Arrange
            var dto = new SpacedRepetitionDTO
            {
                UserId = 1,
                ReviewCount = 3,
                IsDue = false
            };

            // Act
            dto.UserId = 2;
            dto.ReviewCount = 5;
            dto.IsDue = true;

            // Assert
            Assert.Equal(2, dto.UserId);
            Assert.Equal(5, dto.ReviewCount);
            Assert.True(dto.IsDue);
        }

        [Fact]
        public void SpacedRepetitionDTO_PropertyAccessMultipleTimes_ShouldCoverProperty()
        {
            // Arrange
            var dto = new SpacedRepetitionDTO();

            // Act - Access all properties multiple times
            var id1 = dto.UserSpacedRepetitionId;
            var userId1 = dto.UserId;
            var listId1 = dto.VocabularyListId;
            var name1 = dto.VocabularyListName;
            var count1 = dto.ReviewCount;
            var intervals1 = dto.Intervals;
            var status1 = dto.Status;
            var isDue1 = dto.IsDue;
            var days1 = dto.DaysUntilReview;

            dto.UserSpacedRepetitionId = 10;
            dto.UserId = 20;
            dto.VocabularyListId = 30;
            dto.VocabularyListName = "Updated";
            dto.ReviewCount = 7;
            dto.Intervals = 14;
            dto.Status = "Updated";
            dto.IsDue = true;
            dto.DaysUntilReview = 5;

            var id2 = dto.UserSpacedRepetitionId;
            var userId2 = dto.UserId;
            var listId2 = dto.VocabularyListId;
            var name2 = dto.VocabularyListName;
            var count2 = dto.ReviewCount;
            var intervals2 = dto.Intervals;
            var status2 = dto.Status;
            var isDue2 = dto.IsDue;
            var days2 = dto.DaysUntilReview;

            // Assert
            Assert.Equal(0, id1);
            Assert.Equal(0, userId1);
            Assert.Equal(string.Empty, name1);
            Assert.Equal(0, count1);
            Assert.False(isDue1);

            Assert.Equal(10, id2);
            Assert.Equal(20, userId2);
            Assert.Equal("Updated", name2);
            Assert.Equal(7, count2);
            Assert.True(isDue2);
        }

        [Fact]
        public void SpacedRepetitionDTO_UsedInCondition_ShouldEvaluateCorrectly()
        {
            // Arrange
            var dto = new SpacedRepetitionDTO
            {
                IsDue = true,
                Status = "Active"
            };

            // Act - Use properties in conditions to ensure coverage
            string result;
            if (dto.IsDue)
            {
                result = dto.Status;
            }
            else
            {
                result = "Not due";
            }

            // Assert
            Assert.Equal("Active", result);
        }

        [Fact]
        public void SpacedRepetitionDTO_UsedInCollection_ShouldWorkCorrectly()
        {
            // Arrange
            var dtos = new List<SpacedRepetitionDTO>
            {
                new SpacedRepetitionDTO { UserId = 1, IsDue = true, ReviewCount = 5 },
                new SpacedRepetitionDTO { UserId = 2, IsDue = false, ReviewCount = 3 },
                new SpacedRepetitionDTO { UserId = 3, IsDue = true, ReviewCount = 7 }
            };

            // Act - Use properties in LINQ queries to ensure coverage
            var dueItems = dtos.Where(d => d.IsDue).ToList();
            var notDueItems = dtos.Where(d => !d.IsDue).ToList();
            var allUserIds = dtos.Select(d => d.UserId).ToList();
            var allReviewCounts = dtos.Select(d => d.ReviewCount).ToList();
            var allStatuses = dtos.Select(d => d.Status).ToList();

            // Assert
            Assert.Equal(2, dueItems.Count);
            Assert.Single(notDueItems);
            Assert.Equal(3, allUserIds.Count);
            Assert.Equal(3, allReviewCounts.Count);
            Assert.Equal(3, allStatuses.Count);
        }

        [Fact]
        public void SpacedRepetitionDTO_WithDateTimeProperties_ShouldHandleDates()
        {
            // Arrange
            var lastReviewed = DateTime.UtcNow.AddDays(-10);
            var nextReview = DateTime.UtcNow.AddDays(5);
            var dto = new SpacedRepetitionDTO
            {
                LastReviewedAt = lastReviewed,
                NextReviewAt = nextReview
            };

            // Act & Assert
            Assert.Equal(lastReviewed.Date, dto.LastReviewedAt.Date);
            Assert.NotNull(dto.NextReviewAt);
            Assert.Equal(nextReview.Date, dto.NextReviewAt.Value.Date);
        }

        [Fact]
        public void SpacedRepetitionDTO_AllPropertiesAccess_ShouldCoverAllProperties()
        {
            // Arrange & Act
            var dto = new SpacedRepetitionDTO
            {
                UserSpacedRepetitionId = 1,
                UserId = 2,
                VocabularyListId = 3,
                VocabularyListName = "Test",
                LastReviewedAt = DateTime.UtcNow,
                NextReviewAt = DateTime.UtcNow.AddDays(1),
                ReviewCount = 4,
                Intervals = 5,
                Status = "Test",
                IsDue = true,
                DaysUntilReview = 6
            };

            // Access all properties to ensure coverage
            var id = dto.UserSpacedRepetitionId;
            var userId = dto.UserId;
            var listId = dto.VocabularyListId;
            var name = dto.VocabularyListName;
            var lastReviewed = dto.LastReviewedAt;
            var nextReview = dto.NextReviewAt;
            var count = dto.ReviewCount;
            var intervals = dto.Intervals;
            var status = dto.Status;
            var isDue = dto.IsDue;
            var days = dto.DaysUntilReview;

            // Assert
            Assert.Equal(1, id);
            Assert.Equal(2, userId);
            Assert.Equal(3, listId);
            Assert.Equal("Test", name);
            Assert.NotNull(nextReview);
            Assert.Equal(4, count);
            Assert.Equal(5, intervals);
            Assert.Equal("Test", status);
            Assert.True(isDue);
            Assert.Equal(6, days);
        }
    }
}

