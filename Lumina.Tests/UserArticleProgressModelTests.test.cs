using DataLayer.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace Lumina.Tests
{
    public class UserArticleProgressModelTests
    {
        [Fact]
        public void UserArticleProgress_WithDefaultValues_ShouldHaveDefaultValues()
        {
            // Arrange
            var progress = new UserArticleProgress();

            // Assert
            Assert.Equal(0, progress.ProgressId);
            Assert.Equal(0, progress.UserId);
            Assert.Equal(0, progress.ArticleId);
            Assert.Null(progress.ProgressPercent);
            Assert.Null(progress.Status);
            Assert.Equal(default(DateTime), progress.LastAccessedAt);
            Assert.Null(progress.CompletedAt);
        }

        [Fact]
        public void UserArticleProgress_ProgressId_ShouldSetAndGet()
        {
            // Arrange
            var progress = new UserArticleProgress();

            // Act
            progress.ProgressId = 1;
            var progressId = progress.ProgressId;

            // Assert
            Assert.Equal(1, progressId);
        }

        [Fact]
        public void UserArticleProgress_UserId_ShouldSetAndGet()
        {
            // Arrange
            var progress = new UserArticleProgress();

            // Act
            progress.UserId = 10;
            var userId = progress.UserId;

            // Assert
            Assert.Equal(10, userId);
        }

        [Fact]
        public void UserArticleProgress_ArticleId_ShouldSetAndGet()
        {
            // Arrange
            var progress = new UserArticleProgress();

            // Act
            progress.ArticleId = 5;
            var articleId = progress.ArticleId;

            // Assert
            Assert.Equal(5, articleId);
        }

        [Fact]
        public void UserArticleProgress_ProgressPercent_ShouldSetAndGet()
        {
            // Arrange
            var progress = new UserArticleProgress();

            // Act
            progress.ProgressPercent = 50;
            var progressPercent = progress.ProgressPercent;

            // Assert
            Assert.Equal(50, progressPercent);
        }

        [Fact]
        public void UserArticleProgress_ProgressPercent_ShouldAllowNull()
        {
            // Arrange
            var progress = new UserArticleProgress();

            // Act
            progress.ProgressPercent = null;
            var progressPercent = progress.ProgressPercent;

            // Assert
            Assert.Null(progressPercent);
        }

        [Fact]
        public void UserArticleProgress_Status_ShouldSetAndGet()
        {
            // Arrange
            var progress = new UserArticleProgress();

            // Act
            progress.Status = "In Progress";
            var status = progress.Status;

            // Assert
            Assert.Equal("In Progress", status);
        }

        [Fact]
        public void UserArticleProgress_Status_ShouldAllowNull()
        {
            // Arrange
            var progress = new UserArticleProgress();

            // Act
            progress.Status = null;
            var status = progress.Status;

            // Assert
            Assert.Null(status);
        }

        [Fact]
        public void UserArticleProgress_LastAccessedAt_ShouldSetAndGet()
        {
            // Arrange
            var progress = new UserArticleProgress();
            var accessTime = DateTime.UtcNow;

            // Act
            progress.LastAccessedAt = accessTime;
            var lastAccessedAt = progress.LastAccessedAt;

            // Assert
            Assert.Equal(accessTime, lastAccessedAt);
        }

        [Fact]
        public void UserArticleProgress_CompletedAt_ShouldSetAndGet()
        {
            // Arrange
            var progress = new UserArticleProgress();
            var completedTime = DateTime.UtcNow;

            // Act
            progress.CompletedAt = completedTime;
            var completedAt = progress.CompletedAt;

            // Assert
            Assert.Equal(completedTime, completedAt);
        }

        [Fact]
        public void UserArticleProgress_CompletedAt_ShouldAllowNull()
        {
            // Arrange
            var progress = new UserArticleProgress();

            // Act
            progress.CompletedAt = null;
            var completedAt = progress.CompletedAt;

            // Assert
            Assert.Null(completedAt);
        }

        [Fact]
        public void UserArticleProgress_Article_ShouldSetAndGet()
        {
            // Arrange
            var progress = new UserArticleProgress();
            var article = new Article
            {
                ArticleId = 1,
                Title = "Test Article",
                Summary = "Test Summary"
            };

            // Act
            progress.Article = article;
            var articleProperty = progress.Article;

            // Assert
            Assert.NotNull(articleProperty);
            Assert.Equal(1, articleProperty.ArticleId);
            Assert.Equal("Test Article", articleProperty.Title);
        }

        [Fact]
        public void UserArticleProgress_User_ShouldSetAndGet()
        {
            // Arrange
            var progress = new UserArticleProgress();
            var user = new User
            {
                UserId = 1,
                FullName = "Test User",
                Email = "test@example.com"
            };

            // Act
            progress.User = user;
            var userProperty = progress.User;

            // Assert
            Assert.NotNull(userProperty);
            Assert.Equal(1, userProperty.UserId);
            Assert.Equal("Test User", userProperty.FullName);
        }

        [Fact]
        public void UserArticleProgress_PropertyAccessMultipleTimes_ShouldCoverProperty()
        {
            // Arrange
            var progress = new UserArticleProgress();

            // Act - Access properties multiple times
            var progressId1 = progress.ProgressId;
            progress.ProgressId = 10;
            var progressId2 = progress.ProgressId;

            var userId1 = progress.UserId;
            progress.UserId = 20;
            var userId2 = progress.UserId;

            var articleId1 = progress.ArticleId;
            progress.ArticleId = 30;
            var articleId2 = progress.ArticleId;

            var progressPercent1 = progress.ProgressPercent;
            progress.ProgressPercent = 75;
            var progressPercent2 = progress.ProgressPercent;

            var status1 = progress.Status;
            progress.Status = "Completed";
            var status2 = progress.Status;

            var lastAccessedAt1 = progress.LastAccessedAt;
            var newAccessTime = DateTime.UtcNow;
            progress.LastAccessedAt = newAccessTime;
            var lastAccessedAt2 = progress.LastAccessedAt;

            var completedAt1 = progress.CompletedAt;
            var newCompletedTime = DateTime.UtcNow;
            progress.CompletedAt = newCompletedTime;
            var completedAt2 = progress.CompletedAt;

            // Assert
            Assert.Equal(0, progressId1);
            Assert.Equal(10, progressId2);
            Assert.Equal(0, userId1);
            Assert.Equal(20, userId2);
            Assert.Equal(0, articleId1);
            Assert.Equal(30, articleId2);
            Assert.Null(progressPercent1);
            Assert.Equal(75, progressPercent2);
            Assert.Null(status1);
            Assert.Equal("Completed", status2);
            Assert.Equal(default(DateTime), lastAccessedAt1);
            Assert.Equal(newAccessTime, lastAccessedAt2);
            Assert.Null(completedAt1);
            Assert.Equal(newCompletedTime, completedAt2);
        }

        [Fact]
        public void UserArticleProgress_WithAllPropertiesSet_ShouldSetAndGetAllProperties()
        {
            // Arrange
            var article = new Article { ArticleId = 1, Title = "Test Article" };
            var user = new User { UserId = 1, FullName = "Test User" };
            var accessTime = DateTime.UtcNow;
            var completedTime = DateTime.UtcNow.AddHours(1);

            // Act
            var progress = new UserArticleProgress
            {
                ProgressId = 1,
                UserId = 1,
                ArticleId = 1,
                ProgressPercent = 100,
                Status = "Completed",
                LastAccessedAt = accessTime,
                CompletedAt = completedTime,
                Article = article,
                User = user
            };

            // Assert
            Assert.Equal(1, progress.ProgressId);
            Assert.Equal(1, progress.UserId);
            Assert.Equal(1, progress.ArticleId);
            Assert.Equal(100, progress.ProgressPercent);
            Assert.Equal("Completed", progress.Status);
            Assert.Equal(accessTime, progress.LastAccessedAt);
            Assert.Equal(completedTime, progress.CompletedAt);
            Assert.NotNull(progress.Article);
            Assert.NotNull(progress.User);
        }

        [Fact]
        public void UserArticleProgress_UsedInCondition_ShouldEvaluateCorrectly()
        {
            // Arrange
            var progress = new UserArticleProgress
            {
                ProgressPercent = 50,
                Status = "In Progress",
                CompletedAt = null
            };

            // Act - Use properties in conditions to ensure coverage
            var isInProgress = progress.ProgressPercent < 100;
            var hasStatus = !string.IsNullOrEmpty(progress.Status);
            var isCompleted = progress.CompletedAt.HasValue;

            // Assert
            Assert.True(isInProgress);
            Assert.True(hasStatus);
            Assert.False(isCompleted);
        }

        [Fact]
        public void UserArticleProgress_UsedInCollection_ShouldWorkCorrectly()
        {
            // Arrange
            var progresses = new List<UserArticleProgress>
            {
                new UserArticleProgress { ProgressId = 1, UserId = 1, ArticleId = 1, ProgressPercent = 50, Status = "In Progress" },
                new UserArticleProgress { ProgressId = 2, UserId = 2, ArticleId = 2, ProgressPercent = 100, Status = "Completed", CompletedAt = DateTime.UtcNow },
                new UserArticleProgress { ProgressId = 3, UserId = 3, ArticleId = 3, ProgressPercent = null, Status = null }
            };

            // Act - Use properties in LINQ queries to ensure coverage
            var completedProgresses = progresses.Where(p => p.ProgressPercent == 100).ToList();
            var inProgressItems = progresses.Where(p => p.ProgressPercent.HasValue && p.ProgressPercent < 100).ToList();
            var progressIds = progresses.Select(p => p.ProgressId).ToList();
            var userIds = progresses.Select(p => p.UserId).ToList();
            var articleIds = progresses.Select(p => p.ArticleId).ToList();
            var statuses = progresses.Select(p => p.Status).ToList();
            var completedAts = progresses.Select(p => p.CompletedAt).ToList();

            // Assert
            Assert.Single(completedProgresses);
            Assert.Single(inProgressItems); // Only item with ProgressPercent = 50
            Assert.Equal(3, progressIds.Count);
            Assert.Equal(3, userIds.Count);
            Assert.Equal(3, articleIds.Count);
            Assert.Equal(3, statuses.Count);
            Assert.Equal(3, completedAts.Count);
        }

        [Fact]
        public void UserArticleProgress_ArticleNavigation_PropertyAccessMultipleTimes_ShouldCoverProperty()
        {
            // Arrange
            var progress = new UserArticleProgress();
            var article1 = new Article { ArticleId = 1, Title = "Article 1" };
            var article2 = new Article { ArticleId = 2, Title = "Article 2" };

            // Act & Assert
            progress.Article = article1;
            var article1Property = progress.Article;
            Assert.Equal(1, article1Property.ArticleId);

            progress.Article = article2;
            var article2Property = progress.Article;
            Assert.Equal(2, article2Property.ArticleId);
        }

        [Fact]
        public void UserArticleProgress_UserNavigation_PropertyAccessMultipleTimes_ShouldCoverProperty()
        {
            // Arrange
            var progress = new UserArticleProgress();
            var user1 = new User { UserId = 1, FullName = "User 1" };
            var user2 = new User { UserId = 2, FullName = "User 2" };

            // Act & Assert
            progress.User = user1;
            var user1Property = progress.User;
            Assert.Equal(1, user1Property.UserId);

            progress.User = user2;
            var user2Property = progress.User;
            Assert.Equal(2, user2Property.UserId);
        }

        [Fact]
        public void UserArticleProgress_AllPropertiesAccess_ShouldCoverAllProperties()
        {
            // Arrange & Act
            var article = new Article { ArticleId = 1, Title = "Test Article" };
            var user = new User { UserId = 1, FullName = "Test User" };
            var progress = new UserArticleProgress
            {
                ProgressId = 1,
                UserId = 1,
                ArticleId = 1,
                ProgressPercent = 100,
                Status = "Completed",
                LastAccessedAt = DateTime.UtcNow,
                CompletedAt = DateTime.UtcNow,
                Article = article,
                User = user
            };

            // Access all properties
            var pId = progress.ProgressId;
            var uId = progress.UserId;
            var aId = progress.ArticleId;
            var pPercent = progress.ProgressPercent;
            var status = progress.Status;
            var lastAccess = progress.LastAccessedAt;
            var completed = progress.CompletedAt;
            var articleNav = progress.Article;
            var userNav = progress.User;

            // Assert (just to ensure properties are accessed)
            Assert.Equal(1, pId);
            Assert.Equal(1, uId);
            Assert.Equal(1, aId);
            Assert.Equal(100, pPercent);
            Assert.Equal("Completed", status);
            Assert.NotNull(lastAccess);
            Assert.NotNull(completed);
            Assert.NotNull(articleNav);
            Assert.NotNull(userNav);
        }

        [Fact]
        public void UserArticleProgress_ProgressPercent_WithDifferentValues_ShouldHandleCorrectly()
        {
            // Arrange
            var progress = new UserArticleProgress();

            // Act & Assert
            progress.ProgressPercent = 0;
            Assert.Equal(0, progress.ProgressPercent);

            progress.ProgressPercent = 50;
            Assert.Equal(50, progress.ProgressPercent);

            progress.ProgressPercent = 100;
            Assert.Equal(100, progress.ProgressPercent);

            progress.ProgressPercent = null;
            Assert.Null(progress.ProgressPercent);
        }

        [Fact]
        public void UserArticleProgress_Status_WithDifferentValues_ShouldHandleCorrectly()
        {
            // Arrange
            var progress = new UserArticleProgress();

            // Act & Assert
            progress.Status = "Not Started";
            Assert.Equal("Not Started", progress.Status);

            progress.Status = "In Progress";
            Assert.Equal("In Progress", progress.Status);

            progress.Status = "Completed";
            Assert.Equal("Completed", progress.Status);

            progress.Status = "";
            Assert.Equal("", progress.Status);

            progress.Status = null;
            Assert.Null(progress.Status);
        }

        [Fact]
        public void UserArticleProgress_DateTimeProperties_WithDifferentValues_ShouldHandleCorrectly()
        {
            // Arrange
            var progress = new UserArticleProgress();
            var time1 = DateTime.UtcNow.AddDays(-1);
            var time2 = DateTime.UtcNow;
            var time3 = DateTime.UtcNow.AddDays(1);

            // Act & Assert
            progress.LastAccessedAt = time1;
            Assert.Equal(time1, progress.LastAccessedAt);

            progress.LastAccessedAt = time2;
            Assert.Equal(time2, progress.LastAccessedAt);

            progress.CompletedAt = time3;
            Assert.Equal(time3, progress.CompletedAt);

            progress.CompletedAt = null;
            Assert.Null(progress.CompletedAt);
        }

        [Fact]
        public void UserArticleProgress_UsedInNullCheck_ShouldWorkCorrectly()
        {
            // Arrange
            var progress = new UserArticleProgress
            {
                ProgressPercent = null,
                Status = null,
                CompletedAt = null
            };

            // Act - Use null-conditional operators to ensure coverage
            var progressPercentValue = progress.ProgressPercent;
            var statusValue = progress.Status;
            var completedAtValue = progress.CompletedAt?.ToString();

            // Assert
            Assert.Null(progressPercentValue);
            Assert.Null(statusValue);
            Assert.Null(completedAtValue);

            // Act - Set values and check again
            progress.ProgressPercent = 50;
            progress.Status = "Test";
            progress.CompletedAt = DateTime.UtcNow;

            progressPercentValue = progress.ProgressPercent;
            statusValue = progress.Status;
            completedAtValue = progress.CompletedAt?.ToString();

            // Assert
            Assert.NotNull(progressPercentValue);
            Assert.NotNull(statusValue);
            Assert.NotNull(completedAtValue);
        }
    }
}

