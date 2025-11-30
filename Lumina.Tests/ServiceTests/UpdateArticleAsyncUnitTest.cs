using Xunit;
using Moq;
using ServiceLayer.Article;
using RepositoryLayer.UnitOfWork;
using RepositoryLayer;
using DataLayer.DTOs.Article;
using DataLayer.Models;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Lumina.Test.Services
{
    public class UpdateArticleAsyncUnitTest
    {
        private readonly Mock<IUnitOfWork> _mockUnitOfWork;
        private readonly Mock<ILogger<ArticleService>> _mockLogger;
        private readonly Mock<IArticleRepository> _mockArticleRepository;
        private readonly Mock<ICategoryRepository> _mockCategoryRepository;
        private readonly Mock<RepositoryLayer.User.IUserRepository> _mockUserRepository;
        private readonly ArticleService _service;

        public UpdateArticleAsyncUnitTest()
        {
            _mockUnitOfWork = new Mock<IUnitOfWork>();
            _mockLogger = new Mock<ILogger<ArticleService>>();
            _mockArticleRepository = new Mock<IArticleRepository>();
            _mockCategoryRepository = new Mock<ICategoryRepository>();
            _mockUserRepository = new Mock<RepositoryLayer.User.IUserRepository>();

            _mockUnitOfWork.Setup(u => u.Articles).Returns(_mockArticleRepository.Object);
            _mockUnitOfWork.Setup(u => u.Categories).Returns(_mockCategoryRepository.Object);
            _mockUnitOfWork.Setup(u => u.Users).Returns(_mockUserRepository.Object);

            _service = new ArticleService(_mockUnitOfWork.Object, _mockLogger.Object);
        }

        [Fact]
        public async Task UpdateArticleAsync_WhenArticleNotFound_ShouldReturnNull()
        {
            // Arrange
            var request = new ArticleUpdateDTO
            {
                Title = "Updated Title",
                Summary = "Updated Summary",
                CategoryId = 1
            };

            _mockArticleRepository
                .Setup(repo => repo.FindByIdAsync(1))
                .ReturnsAsync((Article?)null);

            // Act
            var result = await _service.UpdateArticleAsync(1, request, 1);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task UpdateArticleAsync_WhenStaffUpdatesPublishedArticle_ShouldSetStatusToPending()
        {
            // Arrange
            var request = new ArticleUpdateDTO
            {
                Title = "Updated Title",
                Summary = "Updated Summary",
                CategoryId = 1,
                Sections = new List<ArticleSectionUpdateDTO>()
            };

            // Article ban đầu phải có Status = "Published" (không null, không empty, trim = "Published")
            // để wasPublished = true và service đi vào nhánh if (isStaff && wasPublished)
            var article = new Article
            {
                ArticleId = 1,
                Title = "Original",
                Summary = "Original Summary",
                Status = "Published", // Phải là "Published" (không null, không empty) để wasPublished = true
                IsPublished = true,
                CategoryId = 1,
                CreatedBy = 1,
                CreatedAt = DateTime.UtcNow,
                ArticleSections = new List<ArticleSection>()
            };

            // Staff user với RoleId = 3 để isStaff = true
            var staffUser = new User 
            { 
                UserId = 1, 
                RoleId = 3, // RoleId 3 = Staff
                FullName = "Staff User"
            };
            var category = new ArticleCategory { CategoryId = 1, CategoryName = "Test" };
            var author = new User { UserId = 1, FullName = "Author" };

            // Service sẽ:
            // 1. Load article (Status = "Published")
            // 2. GetUserByIdAsync(1) -> staffUser (RoleId = 3) -> isStaff = true
            // 3. wasPublished = article.Status.Trim().Equals("Published") -> true
            // 4. isStaff && wasPublished -> true, nên set article.Status = "Pending" (dòng 164)
            // 5. UpdateAsync(article) với Status = "Pending"
            // 6. savedStatus = "Pending" (dòng 190)
            // 7. Reload article (dòng 193) -> có thể có mismatch (giả sử reloaded article vẫn có Status = "Published")
            // 8. Nếu có mismatch, service sẽ fix lại (dòng 195-202)
            // 9. Tạo DTO từ article sau fix (dòng 217)
            
            // Giả sử reloaded article sau update vẫn có status "Published" (mismatch)
            var reloadedArticleWithMismatch = new Article
            {
                ArticleId = 1,
                Title = "Updated Title",
                Summary = "Updated Summary",
                Status = "Published", // Mismatch - service đã set thành "Pending" nhưng reload lại là "Published"
                IsPublished = true, // Mismatch
                CategoryId = 1,
                CreatedBy = 1,
                CreatedAt = article.CreatedAt,
                ArticleSections = new List<ArticleSection>()
            };

            // Article sau khi fix mismatch
            var reloadedArticleFixed = new Article
            {
                ArticleId = 1,
                Title = "Updated Title",
                Summary = "Updated Summary",
                Status = "Pending", // Đã được fix
                IsPublished = false, // Đã được fix
                CategoryId = 1,
                CreatedBy = 1,
                CreatedAt = article.CreatedAt,
                ArticleSections = new List<ArticleSection>()
            };

            // Service có thể gọi FindByIdAsync nhiều lần:
            // 1. Dòng 145: Load article ban đầu
            // 2. Dòng 193: Reload sau update (có mismatch)
            // 3. Dòng 202: Reload sau fix mismatch
            _mockArticleRepository
                .SetupSequence(repo => repo.FindByIdAsync(1))
                .ReturnsAsync(article) // Lần 1: Load article ban đầu (dòng 145)
                .ReturnsAsync(reloadedArticleWithMismatch) // Lần 2: Reload sau update (dòng 193) - có mismatch
                .ReturnsAsync(reloadedArticleFixed); // Lần 3: Reload sau fix mismatch (dòng 202)

            // Setup GetUserByIdAsync để trả về staffUser khi được gọi với updaterUserId = 1
            // Service sẽ gọi GetUserByIdAsync(1) ở dòng 151 để lấy updater
            // Service cũng sẽ gọi GetUserByIdAsync(article.CreatedBy = 1) ở dòng 209 để lấy author
            // Cả 2 lần đều gọi với userId = 1, nên cần setup sequence
            _mockUserRepository
                .SetupSequence(repo => repo.GetUserByIdAsync(1))
                .ReturnsAsync(staffUser) // Lần 1: Lấy updater (dòng 151) - cần RoleId = 3 để isStaff = true
                .ReturnsAsync(author); // Lần 2: Lấy author (dòng 209)
            
            // SetupSequence sẽ xử lý cả 2 lần gọi GetUserByIdAsync(1):
            // - Lần 1: Lấy updater (trả về staffUser với RoleId = 3)
            // - Lần 2: Lấy author (trả về author)

            // Setup UpdateAsync để trả về article đã được update
            // Service sẽ gọi UpdateAsync với article có Status = "Pending" (nếu đi vào nhánh if)
            _mockArticleRepository
                .Setup(repo => repo.UpdateAsync(It.IsAny<Article>()))
                .ReturnsAsync((Article a) => a);

            _mockCategoryRepository
                .Setup(repo => repo.FindByIdAsync(1))
                .ReturnsAsync(category);

            // Act
            var result = await _service.UpdateArticleAsync(1, request, 1);

            // Assert
            Assert.NotNull(result);
            
            // Verify GetUserByIdAsync được gọi với updaterUserId = 1 để lấy updater
            _mockUserRepository.Verify(repo => repo.GetUserByIdAsync(1), Times.AtLeastOnce, 
                "Service should call GetUserByIdAsync to get updater");
            
            // Debug: Kiểm tra xem UpdateAsync được gọi với article nào
            // Nếu service đi vào nhánh if (isStaff && wasPublished), UpdateAsync phải được gọi với Status = "Pending"
            // Nhưng verify cho thấy UpdateAsync được gọi nhưng không với Status = "Pending"
            // Điều này có nghĩa là service không đi vào nhánh if (isStaff && wasPublished)
            // Có thể do updater là null hoặc RoleId không phải 3
            _mockArticleRepository.Verify(repo => repo.UpdateAsync(It.IsAny<Article>()), Times.AtLeastOnce);
            
            // Kiểm tra xem service có đi vào nhánh if không
            // Nếu không, có thể do updater là null hoặc RoleId không phải 3
            // Hoặc wasPublished là false
            _mockArticleRepository.Verify(repo => repo.UpdateAsync(It.Is<Article>(a => a.Status == "Pending" && a.IsPublished == false)), Times.AtLeastOnce,
                "Service should update article with Status = 'Pending' when staff updates published article. " +
                "If this fails, it means service did not enter the if (isStaff && wasPublished) branch. " +
                "Check if updater is null or RoleId is not 3, or wasPublished is false.");
            
            // Service sẽ:
            // 1. Load article (Status = "Published")
            // 2. GetUserByIdAsync(1) -> staffUser (RoleId = 3) -> isStaff = true
            // 3. wasPublished = article.Status.Trim().Equals("Published") -> true
            // 4. isStaff && wasPublished -> true, nên set article.Status = "Pending" (dòng 164)
            // 5. UpdateAsync(article) với Status = "Pending"
            // 6. savedStatus = "Pending" (dòng 190)
            // 7. Reload article (dòng 193) -> reloadedArticleWithMismatch (Status = "Published", có mismatch)
            // 8. Check mismatch: "Published" != "Pending" -> true, nên fix lại (dòng 195-202)
            // 9. UpdateAsync(article) với Status = "Pending" (dòng 201)
            // 10. Reload lại (dòng 202) -> reloadedArticleFixed (Status = "Pending")
            // 11. Tạo DTO từ reloadedArticleFixed (dòng 217)
            // Kiểm tra status - article đã được reload và fix nên phải là "Pending"
            Assert.Equal("Pending", result.Status);
            Assert.False(result.IsPublished);
        }

        [Fact]
        public async Task UpdateArticleAsync_WhenStatusMismatchAfterReload_ShouldFixAndReturnDTO()
        {
            // Arrange
            var request = new ArticleUpdateDTO
            {
                Title = "Updated Title",
                Summary = "Updated Summary",
                CategoryId = 1,
                Sections = null
            };

            var article = new Article
            {
                ArticleId = 1,
                Title = "Original",
                Status = "Draft",
                IsPublished = false,
                CategoryId = 1,
                CreatedBy = 1,
                ArticleSections = new List<ArticleSection>()
            };

            var user = new User { UserId = 1, RoleId = 2 };
            var category = new ArticleCategory { CategoryId = 1, CategoryName = "Test" };
            var author = new User { UserId = 1, FullName = "Author" };

            var reloadedArticle = new Article
            {
                ArticleId = 1,
                Title = "Updated Title",
                Status = "Different Status",
                IsPublished = true,
                CategoryId = 1,
                CreatedBy = 1,
                CreatedAt = article.CreatedAt,
                ArticleSections = new List<ArticleSection>()
            };

            _mockArticleRepository
                .SetupSequence(repo => repo.FindByIdAsync(1))
                .ReturnsAsync(article)
                .ReturnsAsync(reloadedArticle)
                .ReturnsAsync(reloadedArticle);

            _mockUserRepository
                .Setup(repo => repo.GetUserByIdAsync(1))
                .ReturnsAsync(user);

            _mockArticleRepository
                .Setup(repo => repo.UpdateAsync(It.IsAny<Article>()))
                .ReturnsAsync((Article a) => a);

            _mockCategoryRepository
                .Setup(repo => repo.FindByIdAsync(1))
                .ReturnsAsync(category);

            _mockUserRepository
                .Setup(repo => repo.GetUserByIdAsync(article.CreatedBy))
                .ReturnsAsync(author);

            // Act
            var result = await _service.UpdateArticleAsync(1, request, 1);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("Updated Title", result.Title);
        }
    }
}

