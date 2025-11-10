using Xunit;
using RepositoryLayer.Slide;
using DataLayer.Models;
using DataLayer.DTOs;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Lumina.Tests
{
    public class SlideRepositoryTests
    {
        private LuminaSystemContext GetInMemoryContext(string dbName)
        {
            var options = new DbContextOptionsBuilder<LuminaSystemContext>()
                .UseInMemoryDatabase(databaseName: dbName)
                .Options;
            return new LuminaSystemContext(options);
        }

        #region GetAllAsync Tests

        [Fact]
        public async Task GetAllAsync_ShouldReturnAllSlides_WhenNoFiltersProvided()
        {
            // Arrange
            var context = GetInMemoryContext("GetAll_NoFilter");
            var repository = new SlideRepository(context);

            context.Slides.AddRange(
                new DataLayer.Models.Slide
                {
                    SlideId = 1,
                    SlideName = "Slide 1",
                    SlideUrl = "http://example.com/slide1.jpg",
                    IsActive = true,
                    CreateAt = DateTime.UtcNow,
                    CreateBy = 1
                },
                new DataLayer.Models.Slide
                {
                    SlideId = 2,
                    SlideName = "Slide 2",
                    SlideUrl = "http://example.com/slide2.jpg",
                    IsActive = false,
                    CreateAt = DateTime.UtcNow,
                    CreateBy = 1
                }
            );
            await context.SaveChangesAsync();

            // Act
            var result = await repository.GetAllAsync();

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Count);
        }

        [Fact]
        public async Task GetAllAsync_ShouldReturnEmpty_WhenNoSlidesExist()
        {
            // Arrange
            var context = GetInMemoryContext("GetAll_Empty");
            var repository = new SlideRepository(context);

            // Act
            var result = await repository.GetAllAsync();

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result);
        }

        [Fact]
        public async Task GetAllAsync_ShouldFilterByKeywordInSlideName()
        {
            // Arrange
            var context = GetInMemoryContext("GetAll_KeywordName");
            var repository = new SlideRepository(context);

            context.Slides.AddRange(
                new DataLayer.Models.Slide
                {
                    SlideName = "Summer Promotion",
                    SlideUrl = "http://example.com/summer.jpg",
                    IsActive = true,
                    CreateAt = DateTime.UtcNow,
                    CreateBy = 1
                },
                new DataLayer.Models.Slide
                {
                    SlideName = "Winter Sale",
                    SlideUrl = "http://example.com/winter.jpg",
                    IsActive = true,
                    CreateAt = DateTime.UtcNow,
                    CreateBy = 1
                }
            );
            await context.SaveChangesAsync();

            // Act
            var result = await repository.GetAllAsync(keyword: "Summer");

            // Assert
            Assert.NotNull(result);
            Assert.Single(result);
            Assert.Contains("Summer", result[0].SlideName);
        }

        [Fact]
        public async Task GetAllAsync_ShouldFilterByKeywordInSlideUrl()
        {
            // Arrange
            var context = GetInMemoryContext("GetAll_KeywordUrl");
            var repository = new SlideRepository(context);

            context.Slides.AddRange(
                new DataLayer.Models.Slide
                {
                    SlideName = "Banner 1",
                    SlideUrl = "http://cdn.example.com/banner1.jpg",
                    IsActive = true,
                    CreateAt = DateTime.UtcNow,
                    CreateBy = 1
                },
                new DataLayer.Models.Slide
                {
                    SlideName = "Banner 2",
                    SlideUrl = "http://storage.example.com/banner2.jpg",
                    IsActive = true,
                    CreateAt = DateTime.UtcNow,
                    CreateBy = 1
                }
            );
            await context.SaveChangesAsync();

            // Act
            var result = await repository.GetAllAsync(keyword: "cdn");

            // Assert
            Assert.NotNull(result);
            Assert.Single(result);
            Assert.Contains("cdn", result[0].SlideUrl);
        }

        [Fact]
        public async Task GetAllAsync_ShouldFilterByIsActive()
        {
            // Arrange
            var context = GetInMemoryContext("GetAll_IsActive");
            var repository = new SlideRepository(context);

            context.Slides.AddRange(
                new DataLayer.Models.Slide
                {
                    SlideName = "Active Slide",
                    SlideUrl = "http://example.com/active.jpg",
                    IsActive = true,
                    CreateAt = DateTime.UtcNow,
                    CreateBy = 1
                },
                new DataLayer.Models.Slide
                {
                    SlideName = "Inactive Slide",
                    SlideUrl = "http://example.com/inactive.jpg",
                    IsActive = false,
                    CreateAt = DateTime.UtcNow,
                    CreateBy = 1
                }
            );
            await context.SaveChangesAsync();

            // Act
            var result = await repository.GetAllAsync(isActive: true);

            // Assert
            Assert.NotNull(result);
            Assert.Single(result);
            Assert.True(result[0].IsActive);
        }

        [Fact]
        public async Task GetAllAsync_ShouldOrderByCreateAtDescending()
        {
            // Arrange
            var context = GetInMemoryContext("GetAll_OrderBy");
            var repository = new SlideRepository(context);

            var now = DateTime.UtcNow;
            context.Slides.AddRange(
                new DataLayer.Models.Slide
                {
                    SlideName = "Oldest",
                    SlideUrl = "http://example.com/1.jpg",
                    IsActive = true,
                    CreateAt = now.AddDays(-10),
                    CreateBy = 1
                },
                new DataLayer.Models.Slide
                {
                    SlideName = "Newest",
                    SlideUrl = "http://example.com/2.jpg",
                    IsActive = true,
                    CreateAt = now,
                    CreateBy = 1
                },
                new DataLayer.Models.Slide
                {
                    SlideName = "Middle",
                    SlideUrl = "http://example.com/3.jpg",
                    IsActive = true,
                    CreateAt = now.AddDays(-5),
                    CreateBy = 1
                }
            );
            await context.SaveChangesAsync();

            // Act
            var result = await repository.GetAllAsync();

            // Assert
            Assert.NotNull(result);
            Assert.Equal(3, result.Count);
            Assert.Equal("Newest", result[0].SlideName);
            Assert.Equal("Middle", result[1].SlideName);
            Assert.Equal("Oldest", result[2].SlideName);
        }

        [Fact]
        public async Task GetAllAsync_ShouldApplyMultipleFilters()
        {
            // Arrange
            var context = GetInMemoryContext("GetAll_MultipleFilters");
            var repository = new SlideRepository(context);

            context.Slides.AddRange(
                new DataLayer.Models.Slide
                {
                    SlideName = "Active Promo",
                    SlideUrl = "http://example.com/promo.jpg",
                    IsActive = true,
                    CreateAt = DateTime.UtcNow,
                    CreateBy = 1
                },
                new DataLayer.Models.Slide
                {
                    SlideName = "Inactive Promo",
                    SlideUrl = "http://example.com/old-promo.jpg",
                    IsActive = false,
                    CreateAt = DateTime.UtcNow,
                    CreateBy = 1
                },
                new DataLayer.Models.Slide
                {
                    SlideName = "Active Banner",
                    SlideUrl = "http://example.com/banner.jpg",
                    IsActive = true,
                    CreateAt = DateTime.UtcNow,
                    CreateBy = 1
                }
            );
            await context.SaveChangesAsync();

            // Act
            var result = await repository.GetAllAsync(keyword: "Promo", isActive: true);

            // Assert
            Assert.NotNull(result);
            Assert.Single(result);
            Assert.Equal("Active Promo", result[0].SlideName);
            Assert.True(result[0].IsActive);
        }

        #endregion

        #region GetByIdAsync Tests

        [Fact]
        public async Task GetByIdAsync_ShouldReturnSlide_WhenExists()
        {
            // Arrange
            var context = GetInMemoryContext("GetById_Exists");
            var repository = new SlideRepository(context);

            var slide = new DataLayer.Models.Slide
            {
                SlideId = 1,
                SlideName = "Test Slide",
                SlideUrl = "http://example.com/test.jpg",
                IsActive = true,
                CreateAt = DateTime.UtcNow,
                CreateBy = 1
            };
            context.Slides.Add(slide);
            await context.SaveChangesAsync();

            // Act
            var result = await repository.GetByIdAsync(1);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("Test Slide", result.SlideName);
            Assert.Equal("http://example.com/test.jpg", result.SlideUrl);
        }

        [Fact]
        public async Task GetByIdAsync_ShouldReturnNull_WhenNotExists()
        {
            // Arrange
            var context = GetInMemoryContext("GetById_NotExists");
            var repository = new SlideRepository(context);

            // Act
            var result = await repository.GetByIdAsync(999);

            // Assert
            Assert.Null(result);
        }

        #endregion

        #region CreateAsync Tests

        [Fact]
        public async Task CreateAsync_ShouldAddSlideToDatabase()
        {
            // Arrange
            var context = GetInMemoryContext("Create_Success");
            var repository = new SlideRepository(context);

            var newSlideDto = new SlideDTO
            {
                SlideName = "New Slide",
                SlideUrl = "http://example.com/new.jpg",
                IsActive = true,
                CreateBy = 1
            };

            // Act
            var slideId = await repository.CreateAsync(newSlideDto);

            // Assert
            Assert.True(slideId > 0);
            var savedSlide = await context.Slides.FindAsync(slideId);
            Assert.NotNull(savedSlide);
            Assert.Equal("New Slide", savedSlide.SlideName);
            Assert.Equal("http://example.com/new.jpg", savedSlide.SlideUrl);
            Assert.True(savedSlide.IsActive);
        }

        [Fact]
        public async Task CreateAsync_ShouldSetCreateAtToCurrentTime()
        {
            // Arrange
            var context = GetInMemoryContext("Create_CreateAt");
            var repository = new SlideRepository(context);

            var before = DateTime.UtcNow;
            var newSlideDto = new SlideDTO
            {
                SlideName = "Time Test",
                SlideUrl = "http://example.com/time.jpg",
                IsActive = true,
                CreateBy = 1
            };

            // Act
            var slideId = await repository.CreateAsync(newSlideDto);
            var after = DateTime.UtcNow;

            // Assert
            var savedSlide = await context.Slides.FindAsync(slideId);
            Assert.NotNull(savedSlide);
            Assert.True(savedSlide.CreateAt >= before && savedSlide.CreateAt <= after);
        }

        [Fact]
        public async Task CreateAsync_ShouldSetUpdateAtAndUpdateByToNull()
        {
            // Arrange
            var context = GetInMemoryContext("Create_NullUpdate");
            var repository = new SlideRepository(context);

            var newSlideDto = new SlideDTO
            {
                SlideName = "Null Test",
                SlideUrl = "http://example.com/null.jpg",
                IsActive = true,
                CreateBy = 1
            };

            // Act
            var slideId = await repository.CreateAsync(newSlideDto);

            // Assert
            var savedSlide = await context.Slides.FindAsync(slideId);
            Assert.NotNull(savedSlide);
            Assert.Null(savedSlide.UpdateAt);
            Assert.Null(savedSlide.UpdateBy);
        }

        [Fact]
        public async Task CreateAsync_ShouldHandleInactiveSlide()
        {
            // Arrange
            var context = GetInMemoryContext("Create_Inactive");
            var repository = new SlideRepository(context);

            var newSlideDto = new SlideDTO
            {
                SlideName = "Inactive Slide",
                SlideUrl = "http://example.com/inactive.jpg",
                IsActive = false,
                CreateBy = 1
            };

            // Act
            var slideId = await repository.CreateAsync(newSlideDto);

            // Assert
            var savedSlide = await context.Slides.FindAsync(slideId);
            Assert.NotNull(savedSlide);
            Assert.False(savedSlide.IsActive);
        }

        #endregion

        #region UpdateAsync Tests

        [Fact]
        public async Task UpdateAsync_ShouldUpdateSlide_WhenExists()
        {
            // Arrange
            var context = GetInMemoryContext("Update_Success");
            var repository = new SlideRepository(context);

            var existingSlide = new DataLayer.Models.Slide
            {
                SlideId = 1,
                SlideName = "Old Name",
                SlideUrl = "http://example.com/old.jpg",
                IsActive = false,
                CreateAt = DateTime.UtcNow,
                CreateBy = 1
            };
            context.Slides.Add(existingSlide);
            await context.SaveChangesAsync();

            var updateDto = new SlideDTO
            {
                SlideId = 1,
                SlideName = "Updated Name",
                SlideUrl = "http://example.com/updated.jpg",
                IsActive = true,
                UpdateBy = 2
            };

            // Act
            var result = await repository.UpdateAsync(updateDto);

            // Assert
            Assert.True(result);
            var updatedSlide = await context.Slides.FindAsync(1);
            Assert.Equal("Updated Name", updatedSlide?.SlideName);
            Assert.Equal("http://example.com/updated.jpg", updatedSlide?.SlideUrl);
            Assert.True(updatedSlide?.IsActive);
            Assert.Equal(2, updatedSlide?.UpdateBy);
        }

        [Fact]
        public async Task UpdateAsync_ShouldReturnFalse_WhenNotExists()
        {
            // Arrange
            var context = GetInMemoryContext("Update_NotExists");
            var repository = new SlideRepository(context);

            var updateDto = new SlideDTO
            {
                SlideId = 999,
                SlideName = "Ghost Slide",
                SlideUrl = "http://example.com/ghost.jpg",
                IsActive = true
            };

            // Act
            var result = await repository.UpdateAsync(updateDto);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task UpdateAsync_ShouldSetUpdateAtToCurrentTime()
        {
            // Arrange
            var context = GetInMemoryContext("Update_UpdateAt");
            var repository = new SlideRepository(context);

            var existingSlide = new DataLayer.Models.Slide
            {
                SlideName = "Test Slide",
                SlideUrl = "http://example.com/test.jpg",
                IsActive = true,
                CreateAt = DateTime.UtcNow.AddDays(-5),
                CreateBy = 1,
                UpdateAt = null
            };
            context.Slides.Add(existingSlide);
            await context.SaveChangesAsync();

            var before = DateTime.UtcNow;
            var updateDto = new SlideDTO
            {
                SlideId = existingSlide.SlideId,
                SlideName = "Updated Slide",
                SlideUrl = "http://example.com/updated.jpg",
                IsActive = true,
                UpdateBy = 2
            };

            // Act
            await repository.UpdateAsync(updateDto);
            var after = DateTime.UtcNow;

            // Assert
            var updatedSlide = await context.Slides.FindAsync(existingSlide.SlideId);
            Assert.NotNull(updatedSlide?.UpdateAt);
            Assert.True(updatedSlide.UpdateAt >= before && updatedSlide.UpdateAt <= after);
        }

        [Fact]
        public async Task UpdateAsync_ShouldPreserveCreateAtAndCreateBy()
        {
            // Arrange
            var context = GetInMemoryContext("Update_PreserveCreate");
            var repository = new SlideRepository(context);

            var originalCreateAt = DateTime.UtcNow.AddDays(-10);
            var existingSlide = new DataLayer.Models.Slide
            {
                SlideName = "Original",
                SlideUrl = "http://example.com/original.jpg",
                IsActive = true,
                CreateAt = originalCreateAt,
                CreateBy = 1
            };
            context.Slides.Add(existingSlide);
            await context.SaveChangesAsync();

            var updateDto = new SlideDTO
            {
                SlideId = existingSlide.SlideId,
                SlideName = "Updated",
                SlideUrl = "http://example.com/updated.jpg",
                IsActive = false,
                UpdateBy = 2
            };

            // Act
            await repository.UpdateAsync(updateDto);

            // Assert
            var updatedSlide = await context.Slides.FindAsync(existingSlide.SlideId);
            Assert.Equal(originalCreateAt, updatedSlide?.CreateAt);
            Assert.Equal(1, updatedSlide?.CreateBy);
        }

        [Fact]
        public async Task UpdateAsync_ShouldToggleIsActive()
        {
            // Arrange
            var context = GetInMemoryContext("Update_ToggleActive");
            var repository = new SlideRepository(context);

            var existingSlide = new DataLayer.Models.Slide
            {
                SlideName = "Toggle Test",
                SlideUrl = "http://example.com/toggle.jpg",
                IsActive = true,
                CreateAt = DateTime.UtcNow,
                CreateBy = 1
            };
            context.Slides.Add(existingSlide);
            await context.SaveChangesAsync();

            var updateDto = new SlideDTO
            {
                SlideId = existingSlide.SlideId,
                SlideName = "Toggle Test",
                SlideUrl = "http://example.com/toggle.jpg",
                IsActive = false,
                UpdateBy = 1
            };

            // Act
            await repository.UpdateAsync(updateDto);

            // Assert
            var updatedSlide = await context.Slides.FindAsync(existingSlide.SlideId);
            Assert.False(updatedSlide?.IsActive);
        }

        #endregion

        #region DeleteAsync Tests

        [Fact]
        public async Task DeleteAsync_ShouldRemoveSlide_WhenExists()
        {
            // Arrange
            var context = GetInMemoryContext("Delete_Success");
            var repository = new SlideRepository(context);

            var slideToDelete = new DataLayer.Models.Slide
            {
                SlideName = "To Delete",
                SlideUrl = "http://example.com/delete.jpg",
                IsActive = true,
                CreateAt = DateTime.UtcNow,
                CreateBy = 1
            };
            context.Slides.Add(slideToDelete);
            await context.SaveChangesAsync();

            // Act
            var result = await repository.DeleteAsync(slideToDelete.SlideId);

            // Assert
            Assert.True(result);
            var deletedSlide = await context.Slides.FindAsync(slideToDelete.SlideId);
            Assert.Null(deletedSlide);
        }

        [Fact]
        public async Task DeleteAsync_ShouldReturnFalse_WhenNotExists()
        {
            // Arrange
            var context = GetInMemoryContext("Delete_NotExists");
            var repository = new SlideRepository(context);

            // Act
            var result = await repository.DeleteAsync(999);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task DeleteAsync_ShouldNotAffectOtherSlides()
        {
            // Arrange
            var context = GetInMemoryContext("Delete_NoSideEffect");
            var repository = new SlideRepository(context);

            var slide1 = new DataLayer.Models.Slide
            {
                SlideName = "Slide 1",
                SlideUrl = "http://example.com/1.jpg",
                IsActive = true,
                CreateAt = DateTime.UtcNow,
                CreateBy = 1
            };
            var slide2 = new DataLayer.Models.Slide
            {
                SlideName = "Slide 2",
                SlideUrl = "http://example.com/2.jpg",
                IsActive = true,
                CreateAt = DateTime.UtcNow,
                CreateBy = 1
            };
            context.Slides.AddRange(slide1, slide2);
            await context.SaveChangesAsync();

            // Act
            await repository.DeleteAsync(slide1.SlideId);

            // Assert
            var remainingCount = await context.Slides.CountAsync();
            Assert.Equal(1, remainingCount);
            var remainingSlide = await context.Slides.FindAsync(slide2.SlideId);
            Assert.NotNull(remainingSlide);
            Assert.Equal("Slide 2", remainingSlide.SlideName);
        }

        #endregion
    }
}
