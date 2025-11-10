using DataLayer.Models;
using System;
using System.Collections.Generic;
using Xunit;

namespace Lumina.Tests
{
    public class ArticleModelTests
    {
        [Fact]
        public void Article_UpdatedByNavigation_ShouldSetAndGetUpdatedByNavigation()
        {
            // Arrange
            var article = new Article
            {
                ArticleId = 1,
                Title = "Test Article",
                Summary = "Test Summary",
                CategoryId = 1,
                CreatedBy = 1,
                CreatedAt = DateTime.UtcNow
            };

            var updatedByUser = new User
            {
                UserId = 2,
                Email = "updater@example.com",
                FullName = "Updater User"
            };

            // Act - Set UpdatedByNavigation property
            article.UpdatedByNavigation = updatedByUser;
            article.UpdatedBy = 2;

            // Assert - Access UpdatedByNavigation property to ensure coverage
            Assert.NotNull(article.UpdatedByNavigation);
            Assert.Equal(2, article.UpdatedByNavigation.UserId);
            Assert.Equal("updater@example.com", article.UpdatedByNavigation.Email);
            Assert.Equal("Updater User", article.UpdatedByNavigation.FullName);
            Assert.Equal(2, article.UpdatedBy);
        }

        [Fact]
        public void Article_UpdatedByNavigation_WithNull_ShouldAllowNull()
        {
            // Arrange
            var article = new Article
            {
                ArticleId = 1,
                Title = "Test Article",
                Summary = "Test Summary",
                CategoryId = 1,
                CreatedBy = 1,
                CreatedAt = DateTime.UtcNow,
                UpdatedBy = null
            };

            // Act - Set UpdatedByNavigation to null
            article.UpdatedByNavigation = null;

            // Assert - Access UpdatedByNavigation property to ensure coverage
            Assert.Null(article.UpdatedByNavigation);
            Assert.Null(article.UpdatedBy);
        }

        [Fact]
        public void Article_UpdatedByNavigation_PropertyAccess_ShouldCoverProperty()
        {
            // Arrange
            var article = new Article
            {
                ArticleId = 1,
                Title = "Test Article",
                Summary = "Test Summary",
                CategoryId = 1,
                CreatedBy = 1,
                CreatedAt = DateTime.UtcNow
            };

            var user1 = new User { UserId = 2, FullName = "User 1" };
            var user2 = new User { UserId = 3, FullName = "User 2" };

            // Act - Access and modify UpdatedByNavigation multiple times
            article.UpdatedByNavigation = user1;
            var nav1 = article.UpdatedByNavigation;

            article.UpdatedByNavigation = user2;
            var nav2 = article.UpdatedByNavigation;

            article.UpdatedByNavigation = null;
            var nav3 = article.UpdatedByNavigation;

            // Assert
            Assert.NotNull(nav1);
            Assert.Equal(2, nav1.UserId);
            Assert.NotNull(nav2);
            Assert.Equal(3, nav2.UserId);
            Assert.Null(nav3);
        }

        [Fact]
        public void Article_UpdatedByNavigation_UsedInCondition_ShouldEvaluateCorrectly()
        {
            // Arrange
            var article = new Article
            {
                ArticleId = 1,
                Title = "Test Article",
                Summary = "Test Summary",
                CategoryId = 1,
                CreatedBy = 1,
                CreatedAt = DateTime.UtcNow
            };

            var updatedByUser = new User { UserId = 2, FullName = "Updater" };
            article.UpdatedByNavigation = updatedByUser;

            // Act - Use UpdatedByNavigation in condition to ensure coverage
            string result;
            if (article.UpdatedByNavigation != null)
            {
                result = article.UpdatedByNavigation.FullName;
            }
            else
            {
                result = "No updater";
            }

            // Assert
            Assert.Equal("Updater", result);
        }

        [Fact]
        public void Article_UpdatedByNavigation_UsedInNullCheck_ShouldWorkCorrectly()
        {
            // Arrange
            var article = new Article
            {
                ArticleId = 1,
                Title = "Test Article",
                Summary = "Test Summary",
                CategoryId = 1,
                CreatedBy = 1,
                CreatedAt = DateTime.UtcNow,
                UpdatedByNavigation = null
            };

            // Act - Use UpdatedByNavigation in null check
            var hasUpdater = article.UpdatedByNavigation != null;
            var updaterName = article.UpdatedByNavigation?.FullName ?? "Unknown";

            // Assert
            Assert.False(hasUpdater);
            Assert.Equal("Unknown", updaterName);
        }
    }
}

