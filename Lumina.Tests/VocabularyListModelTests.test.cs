using DataLayer.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace Lumina.Tests
{
    public class VocabularyListModelTests
    {
        [Fact]
        public void VocabularyList_UpdatedBy_ShouldSetAndGet()
        {
            // Arrange
            var vocabularyList = new VocabularyList
            {
                VocabularyListId = 1,
                Name = "Test List",
                MakeBy = 1,
                CreateAt = DateTime.UtcNow
            };

            // Act - Set UpdatedBy property
            vocabularyList.UpdatedBy = 2;

            // Assert - Access UpdatedBy property to ensure coverage
            Assert.Equal(2, vocabularyList.UpdatedBy);
        }

        [Fact]
        public void VocabularyList_UpdatedBy_WithNull_ShouldAllowNull()
        {
            // Arrange
            var vocabularyList = new VocabularyList
            {
                VocabularyListId = 1,
                Name = "Test List",
                MakeBy = 1,
                CreateAt = DateTime.UtcNow
            };

            // Act - Set UpdatedBy to null
            vocabularyList.UpdatedBy = null;

            // Assert - Access UpdatedBy property to ensure coverage
            Assert.Null(vocabularyList.UpdatedBy);
        }

        [Fact]
        public void VocabularyList_UpdatedBy_PropertyAccessMultipleTimes_ShouldCoverProperty()
        {
            // Arrange
            var vocabularyList = new VocabularyList
            {
                VocabularyListId = 1,
                Name = "Test List",
                MakeBy = 1,
                CreateAt = DateTime.UtcNow
            };

            // Act - Access UpdatedBy property multiple times
            var updatedBy1 = vocabularyList.UpdatedBy;

            vocabularyList.UpdatedBy = 10;
            var updatedBy2 = vocabularyList.UpdatedBy;

            vocabularyList.UpdatedBy = 20;
            var updatedBy3 = vocabularyList.UpdatedBy;

            vocabularyList.UpdatedBy = null;
            var updatedBy4 = vocabularyList.UpdatedBy;

            // Assert
            Assert.Null(updatedBy1);
            Assert.Equal(10, updatedBy2);
            Assert.Equal(20, updatedBy3);
            Assert.Null(updatedBy4);
        }

        [Fact]
        public void VocabularyList_UpdatedByNavigation_ShouldSetAndGet()
        {
            // Arrange
            var vocabularyList = new VocabularyList
            {
                VocabularyListId = 1,
                Name = "Test List",
                MakeBy = 1,
                CreateAt = DateTime.UtcNow
            };

            var updatedByUser = new User
            {
                UserId = 2,
                Email = "updater@example.com",
                FullName = "Updater User"
            };

            // Act - Set UpdatedByNavigation property
            vocabularyList.UpdatedByNavigation = updatedByUser;
            vocabularyList.UpdatedBy = 2;

            // Assert - Access UpdatedByNavigation property to ensure coverage
            Assert.NotNull(vocabularyList.UpdatedByNavigation);
            Assert.Equal(2, vocabularyList.UpdatedByNavigation.UserId);
            Assert.Equal("updater@example.com", vocabularyList.UpdatedByNavigation.Email);
            Assert.Equal("Updater User", vocabularyList.UpdatedByNavigation.FullName);
        }

        [Fact]
        public void VocabularyList_UpdatedByNavigation_WithNull_ShouldAllowNull()
        {
            // Arrange
            var vocabularyList = new VocabularyList
            {
                VocabularyListId = 1,
                Name = "Test List",
                MakeBy = 1,
                CreateAt = DateTime.UtcNow,
                UpdatedBy = null
            };

            // Act - Set UpdatedByNavigation to null
            vocabularyList.UpdatedByNavigation = null;

            // Assert - Access UpdatedByNavigation property to ensure coverage
            Assert.Null(vocabularyList.UpdatedByNavigation);
            Assert.Null(vocabularyList.UpdatedBy);
        }

        [Fact]
        public void VocabularyList_UpdatedByNavigation_PropertyAccessMultipleTimes_ShouldCoverProperty()
        {
            // Arrange
            var vocabularyList = new VocabularyList
            {
                VocabularyListId = 1,
                Name = "Test List",
                MakeBy = 1,
                CreateAt = DateTime.UtcNow
            };

            var user1 = new User { UserId = 2, FullName = "User 1" };
            var user2 = new User { UserId = 3, FullName = "User 2" };

            // Act - Access and modify UpdatedByNavigation multiple times
            vocabularyList.UpdatedByNavigation = user1;
            var nav1 = vocabularyList.UpdatedByNavigation;

            vocabularyList.UpdatedByNavigation = user2;
            var nav2 = vocabularyList.UpdatedByNavigation;

            vocabularyList.UpdatedByNavigation = null;
            var nav3 = vocabularyList.UpdatedByNavigation;

            // Assert
            Assert.NotNull(nav1);
            Assert.Equal(2, nav1.UserId);
            Assert.NotNull(nav2);
            Assert.Equal(3, nav2.UserId);
            Assert.Null(nav3);
        }

        [Fact]
        public void VocabularyList_UpdatedByNavigation_UsedInCondition_ShouldEvaluateCorrectly()
        {
            // Arrange
            var vocabularyList = new VocabularyList
            {
                VocabularyListId = 1,
                Name = "Test List",
                MakeBy = 1,
                CreateAt = DateTime.UtcNow
            };

            var updatedByUser = new User { UserId = 2, FullName = "Updater" };
            vocabularyList.UpdatedByNavigation = updatedByUser;

            // Act - Use UpdatedByNavigation in condition to ensure coverage
            string result;
            if (vocabularyList.UpdatedByNavigation != null)
            {
                result = vocabularyList.UpdatedByNavigation.FullName;
            }
            else
            {
                result = "No updater";
            }

            // Assert
            Assert.Equal("Updater", result);
        }

        [Fact]
        public void VocabularyList_UpdatedByNavigation_UsedInNullCheck_ShouldWorkCorrectly()
        {
            // Arrange
            var vocabularyList = new VocabularyList
            {
                VocabularyListId = 1,
                Name = "Test List",
                MakeBy = 1,
                CreateAt = DateTime.UtcNow,
                UpdatedByNavigation = null
            };

            // Act - Use UpdatedByNavigation in null check
            var hasUpdater = vocabularyList.UpdatedByNavigation != null;
            var updaterName = vocabularyList.UpdatedByNavigation?.FullName ?? "Unknown";

            // Assert
            Assert.False(hasUpdater);
            Assert.Equal("Unknown", updaterName);
        }

        [Fact]
        public void VocabularyList_UpdatedBy_UsedInCondition_ShouldEvaluateCorrectly()
        {
            // Arrange
            var vocabularyList = new VocabularyList
            {
                VocabularyListId = 1,
                Name = "Test List",
                MakeBy = 1,
                CreateAt = DateTime.UtcNow,
                UpdatedBy = 5
            };

            // Act - Use UpdatedBy in condition to ensure coverage
            string result;
            if (vocabularyList.UpdatedBy.HasValue)
            {
                result = $"Updated by user {vocabularyList.UpdatedBy.Value}";
            }
            else
            {
                result = "Not updated";
            }

            // Assert
            Assert.Equal("Updated by user 5", result);
        }

        [Fact]
        public void VocabularyList_UpdatedBy_UsedInNullCheck_ShouldWorkCorrectly()
        {
            // Arrange
            var vocabularyList = new VocabularyList
            {
                VocabularyListId = 1,
                Name = "Test List",
                MakeBy = 1,
                CreateAt = DateTime.UtcNow,
                UpdatedBy = null
            };

            // Act - Use UpdatedBy in null check
            var hasUpdatedBy = vocabularyList.UpdatedBy.HasValue;
            var updatedByValue = vocabularyList.UpdatedBy ?? 0;

            // Assert
            Assert.False(hasUpdatedBy);
            Assert.Equal(0, updatedByValue);
        }

        [Fact]
        public void VocabularyList_UpdatedByAndUpdatedByNavigation_Together_ShouldWorkCorrectly()
        {
            // Arrange
            var vocabularyList = new VocabularyList
            {
                VocabularyListId = 1,
                Name = "Test List",
                MakeBy = 1,
                CreateAt = DateTime.UtcNow
            };

            var updatedByUser = new User { UserId = 10, FullName = "Test Updater" };

            // Act - Set both UpdatedBy and UpdatedByNavigation
            vocabularyList.UpdatedBy = 10;
            vocabularyList.UpdatedByNavigation = updatedByUser;

            // Assert - Access both properties to ensure coverage
            Assert.Equal(10, vocabularyList.UpdatedBy);
            Assert.NotNull(vocabularyList.UpdatedByNavigation);
            Assert.Equal(10, vocabularyList.UpdatedByNavigation.UserId);
            Assert.Equal("Test Updater", vocabularyList.UpdatedByNavigation.FullName);
        }

        [Fact]
        public void VocabularyList_AllPropertiesAccess_ShouldCoverAllProperties()
        {
            // Arrange & Act
            var vocabularyList = new VocabularyList
            {
                VocabularyListId = 1,
                Name = "Test",
                MakeBy = 1,
                CreateAt = DateTime.UtcNow,
                UpdatedBy = 2,
                UpdatedByNavigation = new User { UserId = 2, FullName = "Test" }
            };

            // Access UpdatedBy and UpdatedByNavigation multiple times to ensure coverage
            var updatedBy1 = vocabularyList.UpdatedBy;
            var nav1 = vocabularyList.UpdatedByNavigation;

            var updatedBy2 = vocabularyList.UpdatedBy;
            var nav2 = vocabularyList.UpdatedByNavigation;

            // Assert
            Assert.Equal(2, updatedBy1);
            Assert.NotNull(nav1);
            Assert.Equal(2, updatedBy2);
            Assert.NotNull(nav2);
        }
    }
}

