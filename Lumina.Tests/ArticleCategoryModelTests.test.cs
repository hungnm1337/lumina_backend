using DataLayer.Models;
using System;
using System.Collections.Generic;
using Xunit;

namespace Lumina.Tests
{
    public class ArticleCategoryModelTests
    {
        [Fact]
        public void ArticleCategory_UpdateAt_ShouldSetAndGetUpdateAt()
        {
            // Arrange
            var category = new ArticleCategory
            {
                CategoryId = 1,
                CategoryName = "Technology",
                Description = "Technology category",
                CreatedByUserId = 1,
                CreateAt = DateTime.UtcNow
            };

            var updateTime = DateTime.UtcNow.AddHours(1);

            // Act - Set UpdateAt property
            category.UpdateAt = updateTime;

            // Assert - Access UpdateAt property to ensure coverage
            Assert.NotNull(category.UpdateAt);
            Assert.Equal(updateTime, category.UpdateAt);
        }

        [Fact]
        public void ArticleCategory_UpdateAt_WithNull_ShouldAllowNull()
        {
            // Arrange
            var category = new ArticleCategory
            {
                CategoryId = 1,
                CategoryName = "Technology",
                Description = "Technology category",
                CreatedByUserId = 1,
                CreateAt = DateTime.UtcNow
            };

            // Act - Set UpdateAt to null
            category.UpdateAt = null;

            // Assert - Access UpdateAt property to ensure coverage
            Assert.Null(category.UpdateAt);
        }

        [Fact]
        public void ArticleCategory_CreatedByUser_ShouldSetAndGetCreatedByUser()
        {
            // Arrange
            var category = new ArticleCategory
            {
                CategoryId = 1,
                CategoryName = "Technology",
                Description = "Technology category",
                CreatedByUserId = 1,
                CreateAt = DateTime.UtcNow
            };

            var createdByUser = new User
            {
                UserId = 1,
                Email = "creator@example.com",
                FullName = "Creator User"
            };

            // Act - Set CreatedByUser property
            category.CreatedByUser = createdByUser;

            // Assert - Access CreatedByUser property to ensure coverage
            Assert.NotNull(category.CreatedByUser);
            Assert.Equal(1, category.CreatedByUser.UserId);
            Assert.Equal("creator@example.com", category.CreatedByUser.Email);
            Assert.Equal("Creator User", category.CreatedByUser.FullName);
        }

        [Fact]
        public void ArticleCategory_CreatedByUser_WithNull_ShouldAllowNull()
        {
            // Arrange
            var category = new ArticleCategory
            {
                CategoryId = 1,
                CategoryName = "Technology",
                Description = "Technology category",
                CreatedByUserId = null,
                CreateAt = DateTime.UtcNow
            };

            // Act - Set CreatedByUser to null
            category.CreatedByUser = null;

            // Assert - Access CreatedByUser property to ensure coverage
            Assert.Null(category.CreatedByUser);
            Assert.Null(category.CreatedByUserId);
        }

        [Fact]
        public void ArticleCategory_UpdateAt_PropertyAccess_ShouldCoverProperty()
        {
            // Arrange
            var category = new ArticleCategory
            {
                CategoryId = 1,
                CategoryName = "Technology",
                Description = "Technology category",
                CreatedByUserId = 1,
                CreateAt = DateTime.UtcNow
            };

            var updateTime1 = DateTime.UtcNow.AddHours(1);
            var updateTime2 = DateTime.UtcNow.AddHours(2);

            // Act - Access and modify UpdateAt multiple times
            category.UpdateAt = updateTime1;
            var updateAt1 = category.UpdateAt;

            category.UpdateAt = updateTime2;
            var updateAt2 = category.UpdateAt;

            category.UpdateAt = null;
            var updateAt3 = category.UpdateAt;

            // Assert
            Assert.NotNull(updateAt1);
            Assert.Equal(updateTime1, updateAt1);
            Assert.NotNull(updateAt2);
            Assert.Equal(updateTime2, updateAt2);
            Assert.Null(updateAt3);
        }

        [Fact]
        public void ArticleCategory_CreatedByUser_PropertyAccess_ShouldCoverProperty()
        {
            // Arrange
            var category = new ArticleCategory
            {
                CategoryId = 1,
                CategoryName = "Technology",
                Description = "Technology category",
                CreatedByUserId = 1,
                CreateAt = DateTime.UtcNow
            };

            var user1 = new User { UserId = 1, FullName = "User 1" };
            var user2 = new User { UserId = 2, FullName = "User 2" };

            // Act - Access and modify CreatedByUser multiple times
            category.CreatedByUser = user1;
            var createdBy1 = category.CreatedByUser;

            category.CreatedByUser = user2;
            var createdBy2 = category.CreatedByUser;

            category.CreatedByUser = null;
            var createdBy3 = category.CreatedByUser;

            // Assert
            Assert.NotNull(createdBy1);
            Assert.Equal(1, createdBy1.UserId);
            Assert.NotNull(createdBy2);
            Assert.Equal(2, createdBy2.UserId);
            Assert.Null(createdBy3);
        }

        [Fact]
        public void ArticleCategory_UpdateAt_UsedInCondition_ShouldEvaluateCorrectly()
        {
            // Arrange
            var category = new ArticleCategory
            {
                CategoryId = 1,
                CategoryName = "Technology",
                Description = "Technology category",
                CreatedByUserId = 1,
                CreateAt = DateTime.UtcNow,
                UpdateAt = DateTime.UtcNow.AddHours(1)
            };

            // Act - Use UpdateAt in condition to ensure coverage
            string result;
            if (category.UpdateAt.HasValue)
            {
                result = "Updated";
            }
            else
            {
                result = "Not updated";
            }

            // Assert
            Assert.Equal("Updated", result);
            Assert.NotNull(category.UpdateAt);
        }

        [Fact]
        public void ArticleCategory_UpdateAt_UsedInNullCheck_ShouldWorkCorrectly()
        {
            // Arrange
            var category = new ArticleCategory
            {
                CategoryId = 1,
                CategoryName = "Technology",
                Description = "Technology category",
                CreatedByUserId = 1,
                CreateAt = DateTime.UtcNow,
                UpdateAt = null
            };

            // Act - Use UpdateAt in null check
            var hasUpdate = category.UpdateAt.HasValue;
            var updateTime = category.UpdateAt ?? DateTime.MinValue;

            // Assert
            Assert.False(hasUpdate);
            Assert.Equal(DateTime.MinValue, updateTime);
        }

        [Fact]
        public void ArticleCategory_CreatedByUser_UsedInCondition_ShouldEvaluateCorrectly()
        {
            // Arrange
            var category = new ArticleCategory
            {
                CategoryId = 1,
                CategoryName = "Technology",
                Description = "Technology category",
                CreatedByUserId = 1,
                CreateAt = DateTime.UtcNow
            };

            var createdByUser = new User { UserId = 1, FullName = "Creator" };
            category.CreatedByUser = createdByUser;

            // Act - Use CreatedByUser in condition to ensure coverage
            string result;
            if (category.CreatedByUser != null)
            {
                result = category.CreatedByUser.FullName;
            }
            else
            {
                result = "Unknown";
            }

            // Assert
            Assert.Equal("Creator", result);
        }

        [Fact]
        public void ArticleCategory_CreatedByUser_UsedInNullCheck_ShouldWorkCorrectly()
        {
            // Arrange
            var category = new ArticleCategory
            {
                CategoryId = 1,
                CategoryName = "Technology",
                Description = "Technology category",
                CreatedByUserId = null,
                CreateAt = DateTime.UtcNow,
                CreatedByUser = null
            };

            // Act - Use CreatedByUser in null check
            var hasCreator = category.CreatedByUser != null;
            var creatorName = category.CreatedByUser?.FullName ?? "Unknown";

            // Assert
            Assert.False(hasCreator);
            Assert.Equal("Unknown", creatorName);
        }
    }
}

