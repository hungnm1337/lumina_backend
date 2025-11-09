using DataLayer.Models;
using Microsoft.EntityFrameworkCore;

namespace Lumina.Tests.Helpers
{
    public static class InMemoryDbContextHelper
    {
        public static LuminaSystemContext CreateContext(string? databaseName = null)
        {
            var options = new DbContextOptionsBuilder<LuminaSystemContext>()
                .UseInMemoryDatabase(databaseName ?? Guid.NewGuid().ToString())
                .ConfigureWarnings(warnings => warnings.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning))
                .Options;

            return new LuminaSystemContext(options);
        }

        public static async Task SeedVocabularyDataAsync(LuminaSystemContext context)
        {
            // Create a test role first
            var testRole = new Role
            {
                RoleId = 3,
                RoleName = "Staff"
            };
            context.Roles.Add(testRole);

            // Create a test user
            var testUser = new User
            {
                UserId = 1,
                Email = "test@example.com",
                FullName = "Test User",
                RoleId = 3
            };
            context.Users.Add(testUser);

            // Create vocabulary lists
            var list1 = new VocabularyList
            {
                VocabularyListId = 1,
                Name = "Test List 1",
                MakeBy = 1,
                CreateAt = DateTime.UtcNow,
                IsDeleted = false,
                IsPublic = true,
                Status = "Published"
            };

            var list2 = new VocabularyList
            {
                VocabularyListId = 2,
                Name = "Test List 2",
                MakeBy = 1,
                CreateAt = DateTime.UtcNow,
                IsDeleted = false,
                IsPublic = false,
                Status = "Draft"
            };

            context.VocabularyLists.AddRange(list1, list2);

            // Create vocabularies
            var vocabularies = new List<Vocabulary>
            {
                new Vocabulary
                {
                    VocabularyId = 1,
                    VocabularyListId = 1,
                    Word = "Hello",
                    Definition = "Xin chào",
                    TypeOfWord = "noun",
                    Category = "greeting",
                    Example = "Hello, how are you?",
                    IsDeleted = false
                },
                new Vocabulary
                {
                    VocabularyId = 2,
                    VocabularyListId = 1,
                    Word = "World",
                    Definition = "Thế giới",
                    TypeOfWord = "noun",
                    Category = "general",
                    Example = "Hello world",
                    IsDeleted = false
                },
                new Vocabulary
                {
                    VocabularyId = 3,
                    VocabularyListId = 1,
                    Word = "Beautiful",
                    Definition = "Đẹp",
                    TypeOfWord = "adjective",
                    Category = "appearance",
                    Example = "Beautiful day",
                    IsDeleted = false
                },
                new Vocabulary
                {
                    VocabularyId = 4,
                    VocabularyListId = 2,
                    Word = "Run",
                    Definition = "Chạy",
                    TypeOfWord = "verb",
                    Category = "action",
                    Example = "Run fast",
                    IsDeleted = false
                },
                new Vocabulary
                {
                    VocabularyId = 5,
                    VocabularyListId = 1,
                    Word = "Test",
                    Definition = "Kiểm tra",
                    TypeOfWord = "noun",
                    Category = null,
                    Example = null,
                    IsDeleted = false
                },
                new Vocabulary
                {
                    VocabularyId = 6,
                    VocabularyListId = 1,
                    Word = "Deleted",
                    Definition = "Đã xóa",
                    TypeOfWord = "noun",
                    Category = "test",
                    Example = "Deleted item",
                    IsDeleted = true // Soft deleted
                }
            };

            context.Vocabularies.AddRange(vocabularies);
            await context.SaveChangesAsync();
        }

        public static async Task SeedVocabularyListDataAsync(LuminaSystemContext context)
        {
            // Create roles
            var staffRole = new Role
            {
                RoleId = 3,
                RoleName = "Staff"
            };
            var studentRole = new Role
            {
                RoleId = 2,
                RoleName = "Student"
            };
            context.Roles.AddRange(staffRole, studentRole);

            // Create users
            var staffUser = new User
            {
                UserId = 1,
                Email = "staff@example.com",
                FullName = "Staff User",
                RoleId = 3
            };
            var studentUser = new User
            {
                UserId = 2,
                Email = "student@example.com",
                FullName = "Student User",
                RoleId = 2
            };
            var studentUser2 = new User
            {
                UserId = 3,
                Email = "student2@example.com",
                FullName = "Student User 2",
                RoleId = 2
            };
            context.Users.AddRange(staffUser, studentUser, studentUser2);

            // Create vocabulary lists
            var list1 = new VocabularyList
            {
                VocabularyListId = 1,
                Name = "Published Public List",
                MakeBy = 1, // Staff
                CreateAt = DateTime.UtcNow.AddDays(-5),
                IsDeleted = false,
                IsPublic = true,
                Status = "Published",
                RejectionReason = null
            };

            var list2 = new VocabularyList
            {
                VocabularyListId = 2,
                Name = "Draft List",
                MakeBy = 2, // Student
                CreateAt = DateTime.UtcNow.AddDays(-3),
                IsDeleted = false,
                IsPublic = false,
                Status = "Draft",
                RejectionReason = null
            };

            var list3 = new VocabularyList
            {
                VocabularyListId = 3,
                Name = "Published Private List",
                MakeBy = 2, // Student
                CreateAt = DateTime.UtcNow.AddDays(-2),
                IsDeleted = false,
                IsPublic = false,
                Status = "Published",
                RejectionReason = null
            };

            var list4 = new VocabularyList
            {
                VocabularyListId = 4,
                Name = "Rejected List",
                MakeBy = 2, // Student
                CreateAt = DateTime.UtcNow.AddDays(-4),
                IsDeleted = false,
                IsPublic = true,
                Status = "Rejected",
                RejectionReason = "Inappropriate content"
            };

            var list5 = new VocabularyList
            {
                VocabularyListId = 5,
                Name = "Deleted List",
                MakeBy = 2, // Student
                CreateAt = DateTime.UtcNow.AddDays(-1),
                IsDeleted = true,
                IsPublic = false,
                Status = "Draft",
                RejectionReason = null
            };

            var list6 = new VocabularyList
            {
                VocabularyListId = 6,
                Name = "Staff Published List",
                MakeBy = 1, // Staff
                CreateAt = DateTime.UtcNow.AddDays(-1),
                IsDeleted = false,
                IsPublic = true,
                Status = "Published",
                RejectionReason = null
            };

            var list7 = new VocabularyList
            {
                VocabularyListId = 7,
                Name = "Another Published List",
                MakeBy = 3, // Student 2
                CreateAt = DateTime.UtcNow,
                IsDeleted = false,
                IsPublic = true,
                Status = "Published",
                RejectionReason = null
            };

            context.VocabularyLists.AddRange(list1, list2, list3, list4, list5, list6, list7);

            // Create vocabularies for lists
            var vocabularies = new List<Vocabulary>
            {
                new Vocabulary
                {
                    VocabularyId = 1,
                    VocabularyListId = 1,
                    Word = "Hello",
                    Definition = "Xin chào",
                    TypeOfWord = "noun",
                    Category = "greeting",
                    Example = "Hello, how are you?",
                    IsDeleted = false
                },
                new Vocabulary
                {
                    VocabularyId = 2,
                    VocabularyListId = 1,
                    Word = "World",
                    Definition = "Thế giới",
                    TypeOfWord = "noun",
                    Category = "general",
                    Example = "Hello world",
                    IsDeleted = false
                },
                new Vocabulary
                {
                    VocabularyId = 3,
                    VocabularyListId = 2,
                    Word = "Run",
                    Definition = "Chạy",
                    TypeOfWord = "verb",
                    Category = "action",
                    Example = "Run fast",
                    IsDeleted = false
                },
                new Vocabulary
                {
                    VocabularyId = 4,
                    VocabularyListId = 3,
                    Word = "Test",
                    Definition = "Kiểm tra",
                    TypeOfWord = "noun",
                    Category = null,
                    Example = null,
                    IsDeleted = false
                },
                new Vocabulary
                {
                    VocabularyId = 5,
                    VocabularyListId = 1,
                    Word = "Deleted",
                    Definition = "Đã xóa",
                    TypeOfWord = "noun",
                    Category = "test",
                    Example = "Deleted item",
                    IsDeleted = true // Soft deleted
                },
                new Vocabulary
                {
                    VocabularyId = 6,
                    VocabularyListId = 6,
                    Word = "StaffWord",
                    Definition = "Staff word",
                    TypeOfWord = "noun",
                    Category = "test",
                    Example = "Staff example",
                    IsDeleted = false
                }
            };

            context.Vocabularies.AddRange(vocabularies);
            await context.SaveChangesAsync();
        }

        public static async Task SeedArticleDataAsync(LuminaSystemContext context)
        {
            // Create roles
            var staffRole = new Role
            {
                RoleId = 3,
                RoleName = "Staff"
            };
            var managerRole = new Role
            {
                RoleId = 2,
                RoleName = "Manager"
            };
            context.Roles.AddRange(staffRole, managerRole);

            // Create users
            var staffUser = new User
            {
                UserId = 1,
                Email = "staff@example.com",
                FullName = "Staff User",
                RoleId = 3
            };
            var managerUser = new User
            {
                UserId = 2,
                Email = "manager@example.com",
                FullName = "Manager User",
                RoleId = 2
            };
            var studentUser = new User
            {
                UserId = 3,
                Email = "student@example.com",
                FullName = "Student User",
                RoleId = 2
            };
            context.Users.AddRange(staffUser, managerUser, studentUser);

            // Create categories
            var category1 = new ArticleCategory
            {
                CategoryId = 1,
                CategoryName = "Technology",
                Description = "Technology articles",
                CreatedByUserId = 1,
                CreateAt = DateTime.UtcNow
            };
            var category2 = new ArticleCategory
            {
                CategoryId = 2,
                CategoryName = "Education",
                Description = "Education articles",
                CreatedByUserId = 1,
                CreateAt = DateTime.UtcNow
            };
            context.ArticleCategories.AddRange(category1, category2);

            // Create articles
            var article1 = new Article
            {
                ArticleId = 1,
                Title = "Published Article",
                Summary = "This is a published article",
                CategoryId = 1,
                CreatedBy = 1,
                CreatedAt = DateTime.UtcNow.AddDays(-5),
                IsPublished = true,
                Status = "Published",
                RejectionReason = null
            };
            var article2 = new Article
            {
                ArticleId = 2,
                Title = "Draft Article",
                Summary = "This is a draft article",
                CategoryId = 1,
                CreatedBy = 1,
                CreatedAt = DateTime.UtcNow.AddDays(-3),
                IsPublished = false,
                Status = "Draft",
                RejectionReason = null
            };
            var article3 = new Article
            {
                ArticleId = 3,
                Title = "Pending Article",
                Summary = "This is a pending article",
                CategoryId = 2,
                CreatedBy = 1,
                CreatedAt = DateTime.UtcNow.AddDays(-2),
                IsPublished = false,
                Status = "Pending",
                RejectionReason = null
            };
            var article4 = new Article
            {
                ArticleId = 4,
                Title = "Rejected Article",
                Summary = "This is a rejected article",
                CategoryId = 1,
                CreatedBy = 1,
                CreatedAt = DateTime.UtcNow.AddDays(-4),
                IsPublished = false,
                Status = "Rejected",
                RejectionReason = "Inappropriate content"
            };
            var article5 = new Article
            {
                ArticleId = 5,
                Title = "Another Published Article",
                Summary = "Another published article",
                CategoryId = 2,
                CreatedBy = 3,
                CreatedAt = DateTime.UtcNow,
                IsPublished = true,
                Status = "Published",
                RejectionReason = null
            };
            context.Articles.AddRange(article1, article2, article3, article4, article5);

            // Create article sections
            var section1 = new ArticleSection
            {
                SectionId = 1,
                ArticleId = 1,
                SectionTitle = "Introduction",
                SectionContent = "This is the introduction section",
                OrderIndex = 1
            };
            var section2 = new ArticleSection
            {
                SectionId = 2,
                ArticleId = 1,
                SectionTitle = "Main Content",
                SectionContent = "This is the main content section",
                OrderIndex = 2
            };
            var section3 = new ArticleSection
            {
                SectionId = 3,
                ArticleId = 2,
                SectionTitle = "Draft Section",
                SectionContent = "This is a draft section",
                OrderIndex = 1
            };
            context.ArticleSections.AddRange(section1, section2, section3);

            await context.SaveChangesAsync();
        }
    }
}

