using Xunit;
using Moq;
using RepositoryLayer.Event;
using DataLayer.Models;
using DataLayer.DTOs;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Lumina.Tests
{
    public class EventRepositoryTests
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
        public async Task GetAllAsync_ShouldReturnAllEvents_WhenNoFiltersProvided()
        {
            // Arrange
            var context = GetInMemoryContext("GetAllAsync_NoFilters");
            var repository = new EventRepository(context);

            var user1 = new User { UserId = 1, FullName = "User One", Email = "user1@test.com" };
            var user2 = new User { UserId = 2, FullName = "User Two", Email = "user2@test.com" };
            context.Users.AddRange(user1, user2);

            var event1 = new DataLayer.Models.Event
            {
                EventId = 1,
                EventName = "Event 1",
                StartDate = new DateTime(2025, 6, 1),
                EndDate = new DateTime(2025, 6, 5),
                CreateAt = DateTime.UtcNow,
                CreateBy = 1
            };
            var event2 = new DataLayer.Models.Event
            {
                EventId = 2,
                EventName = "Event 2",
                StartDate = new DateTime(2025, 7, 1),
                EndDate = new DateTime(2025, 7, 5),
                CreateAt = DateTime.UtcNow,
                CreateBy = 2
            };
            context.Events.AddRange(event1, event2);
            await context.SaveChangesAsync();

            // Act
            var result = await repository.GetAllAsync();

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Count);
            Assert.Contains(result, e => e.EventName == "Event 1");
            Assert.Contains(result, e => e.EventName == "Event 2");
        }

        [Fact]
        public async Task GetAllAsync_ShouldReturnEmpty_WhenNoEventsExist()
        {
            // Arrange
            var context = GetInMemoryContext("GetAllAsync_Empty");
            var repository = new EventRepository(context);

            // Act
            var result = await repository.GetAllAsync();

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result);
        }

        [Fact]
        public async Task GetAllAsync_ShouldFilterByFromDate()
        {
            // Arrange
            var context = GetInMemoryContext("GetAllAsync_FromDate");
            var repository = new EventRepository(context);

            var user = new User { UserId = 1, FullName = "User One", Email = "user1@test.com" };
            context.Users.Add(user);

            var event1 = new DataLayer.Models.Event
            {
                EventId = 1,
                EventName = "Past Event",
                StartDate = new DateTime(2025, 5, 1),
                EndDate = new DateTime(2025, 5, 5),
                CreateAt = DateTime.UtcNow,
                CreateBy = 1
            };
            var event2 = new DataLayer.Models.Event
            {
                EventId = 2,
                EventName = "Future Event",
                StartDate = new DateTime(2025, 7, 1),
                EndDate = new DateTime(2025, 7, 5),
                CreateAt = DateTime.UtcNow,
                CreateBy = 1
            };
            context.Events.AddRange(event1, event2);
            await context.SaveChangesAsync();

            // Act
            var result = await repository.GetAllAsync(from: new DateTime(2025, 6, 1));

            // Assert
            Assert.NotNull(result);
            Assert.Single(result);
            Assert.Equal("Future Event", result[0].EventName);
        }

        [Fact]
        public async Task GetAllAsync_ShouldFilterByToDate()
        {
            // Arrange
            var context = GetInMemoryContext("GetAllAsync_ToDate");
            var repository = new EventRepository(context);

            var user = new User { UserId = 1, FullName = "User One", Email = "user1@test.com" };
            context.Users.Add(user);

            var event1 = new DataLayer.Models.Event
            {
                EventId = 1,
                EventName = "Early Event",
                StartDate = new DateTime(2025, 5, 1),
                EndDate = new DateTime(2025, 5, 5),
                CreateAt = DateTime.UtcNow,
                CreateBy = 1
            };
            var event2 = new DataLayer.Models.Event
            {
                EventId = 2,
                EventName = "Late Event",
                StartDate = new DateTime(2025, 7, 1),
                EndDate = new DateTime(2025, 7, 31),
                CreateAt = DateTime.UtcNow,
                CreateBy = 1
            };
            context.Events.AddRange(event1, event2);
            await context.SaveChangesAsync();

            // Act
            var result = await repository.GetAllAsync(to: new DateTime(2025, 6, 1));

            // Assert
            Assert.NotNull(result);
            Assert.Single(result);
            Assert.Equal("Early Event", result[0].EventName);
        }

        [Fact]
        public async Task GetAllAsync_ShouldFilterByKeywordInEventName()
        {
            // Arrange
            var context = GetInMemoryContext("GetAllAsync_KeywordName");
            var repository = new EventRepository(context);

            var user = new User { UserId = 1, FullName = "User One", Email = "user1@test.com" };
            context.Users.Add(user);

            var event1 = new DataLayer.Models.Event
            {
                EventId = 1,
                EventName = "Tech Conference",
                StartDate = new DateTime(2025, 6, 1),
                EndDate = new DateTime(2025, 6, 5),
                CreateAt = DateTime.UtcNow,
                CreateBy = 1
            };
            var event2 = new DataLayer.Models.Event
            {
                EventId = 2,
                EventName = "Music Festival",
                StartDate = new DateTime(2025, 7, 1),
                EndDate = new DateTime(2025, 7, 5),
                CreateAt = DateTime.UtcNow,
                CreateBy = 1
            };
            context.Events.AddRange(event1, event2);
            await context.SaveChangesAsync();

            // Act
            var result = await repository.GetAllAsync(keyword: "Tech");

            // Assert
            Assert.NotNull(result);
            Assert.Single(result);
            Assert.Equal("Tech Conference", result[0].EventName);
        }

        [Fact]
        public async Task GetAllAsync_ShouldFilterByKeywordInContent()
        {
            // Arrange
            var context = GetInMemoryContext("GetAllAsync_KeywordContent");
            var repository = new EventRepository(context);

            var user = new User { UserId = 1, FullName = "User One", Email = "user1@test.com" };
            context.Users.Add(user);

            var event1 = new DataLayer.Models.Event
            {
                EventId = 1,
                EventName = "Event 1",
                Content = "This event focuses on innovation",
                StartDate = new DateTime(2025, 6, 1),
                EndDate = new DateTime(2025, 6, 5),
                CreateAt = DateTime.UtcNow,
                CreateBy = 1
            };
            var event2 = new DataLayer.Models.Event
            {
                EventId = 2,
                EventName = "Event 2",
                Content = "This is a general event",
                StartDate = new DateTime(2025, 7, 1),
                EndDate = new DateTime(2025, 7, 5),
                CreateAt = DateTime.UtcNow,
                CreateBy = 1
            };
            context.Events.AddRange(event1, event2);
            await context.SaveChangesAsync();

            // Act
            var result = await repository.GetAllAsync(keyword: "innovation");

            // Assert
            Assert.NotNull(result);
            Assert.Single(result);
            Assert.Equal("Event 1", result[0].EventName);
        }

        [Fact]
        public async Task GetAllAsync_ShouldOrderByStartDateDescending()
        {
            // Arrange
            var context = GetInMemoryContext("GetAllAsync_OrderBy");
            var repository = new EventRepository(context);

            var user = new User { UserId = 1, FullName = "User One", Email = "user1@test.com" };
            context.Users.Add(user);

            var event1 = new DataLayer.Models.Event
            {
                EventId = 1,
                EventName = "Event 1",
                StartDate = new DateTime(2025, 5, 1),
                EndDate = new DateTime(2025, 5, 5),
                CreateAt = DateTime.UtcNow,
                CreateBy = 1
            };
            var event2 = new DataLayer.Models.Event
            {
                EventId = 2,
                EventName = "Event 2",
                StartDate = new DateTime(2025, 7, 1),
                EndDate = new DateTime(2025, 7, 5),
                CreateAt = DateTime.UtcNow,
                CreateBy = 1
            };
            var event3 = new DataLayer.Models.Event
            {
                EventId = 3,
                EventName = "Event 3",
                StartDate = new DateTime(2025, 6, 1),
                EndDate = new DateTime(2025, 6, 5),
                CreateAt = DateTime.UtcNow,
                CreateBy = 1
            };
            context.Events.AddRange(event1, event2, event3);
            await context.SaveChangesAsync();

            // Act
            var result = await repository.GetAllAsync();

            // Assert
            Assert.NotNull(result);
            Assert.Equal(3, result.Count);
            Assert.Equal("Event 2", result[0].EventName); // Latest start date first
            Assert.Equal("Event 3", result[1].EventName);
            Assert.Equal("Event 1", result[2].EventName);
        }

        #endregion

        #region GetAllPaginatedAsync Tests

        [Fact]
        public async Task GetAllPaginatedAsync_ShouldReturnFirstPage()
        {
            // Arrange
            var context = GetInMemoryContext("GetAllPaginated_FirstPage");
            var repository = new EventRepository(context);

            var user = new User { UserId = 1, FullName = "User One", Email = "user1@test.com" };
            context.Users.Add(user);

            for (int i = 1; i <= 15; i++)
            {
                context.Events.Add(new DataLayer.Models.Event
                {
                    EventId = i,
                    EventName = $"Event {i}",
                    StartDate = new DateTime(2025, 6, i),
                    EndDate = new DateTime(2025, 6, i + 1),
                    CreateAt = DateTime.UtcNow,
                    CreateBy = 1
                });
            }
            await context.SaveChangesAsync();

            // Act
            var result = await repository.GetAllPaginatedAsync(page: 1, pageSize: 10);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(10, result.Items.Count);
            Assert.Equal(15, result.Total);
            Assert.Equal(1, result.Page);
            Assert.Equal(10, result.PageSize);
            Assert.Equal(2, result.TotalPages);
            Assert.True(result.HasNext);
            Assert.False(result.HasPrevious);
        }

        [Fact]
        public async Task GetAllPaginatedAsync_ShouldReturnSecondPage()
        {
            // Arrange
            var context = GetInMemoryContext("GetAllPaginated_SecondPage");
            var repository = new EventRepository(context);

            var user = new User { UserId = 1, FullName = "User One", Email = "user1@test.com" };
            context.Users.Add(user);

            for (int i = 1; i <= 15; i++)
            {
                context.Events.Add(new DataLayer.Models.Event
                {
                    EventId = i,
                    EventName = $"Event {i}",
                    StartDate = new DateTime(2025, 6, i),
                    EndDate = new DateTime(2025, 6, i + 1),
                    CreateAt = DateTime.UtcNow,
                    CreateBy = 1
                });
            }
            await context.SaveChangesAsync();

            // Act
            var result = await repository.GetAllPaginatedAsync(page: 2, pageSize: 10);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(5, result.Items.Count);
            Assert.Equal(15, result.Total);
            Assert.Equal(2, result.Page);
            Assert.False(result.HasNext);
            Assert.True(result.HasPrevious);
        }

        [Fact]
        public async Task GetAllPaginatedAsync_ShouldApplyFiltersAndPagination()
        {
            // Arrange
            var context = GetInMemoryContext("GetAllPaginated_Filters");
            var repository = new EventRepository(context);

            var user = new User { UserId = 1, FullName = "User One", Email = "user1@test.com" };
            context.Users.Add(user);

            for (int i = 1; i <= 20; i++)
            {
                context.Events.Add(new DataLayer.Models.Event
                {
                    EventId = i,
                    EventName = i % 2 == 0 ? $"Tech Event {i}" : $"Other Event {i}",
                    StartDate = new DateTime(2025, 6, i),
                    EndDate = new DateTime(2025, 6, i + 1),
                    CreateAt = DateTime.UtcNow,
                    CreateBy = 1
                });
            }
            await context.SaveChangesAsync();

            // Act
            var result = await repository.GetAllPaginatedAsync(keyword: "Tech", page: 1, pageSize: 5);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(5, result.Items.Count);
            Assert.Equal(10, result.Total); // 10 "Tech" events
            Assert.Equal(2, result.TotalPages);
            Assert.All(result.Items, item => Assert.Contains("Tech", item.EventName));
        }

        #endregion

        #region GetByIdAsync Tests

        [Fact]
        public async Task GetByIdAsync_ShouldReturnEvent_WhenEventExists()
        {
            // Arrange
            var context = GetInMemoryContext("GetById_Success");
            var repository = new EventRepository(context);

            var user = new User { UserId = 1, FullName = "User One", Email = "user1@test.com" };
            context.Users.Add(user);

            var event1 = new DataLayer.Models.Event
            {
                EventId = 1,
                EventName = "Test Event",
                Content = "Test Content",
                StartDate = new DateTime(2025, 6, 1),
                EndDate = new DateTime(2025, 6, 5),
                CreateAt = DateTime.UtcNow,
                CreateBy = 1
            };
            context.Events.Add(event1);
            await context.SaveChangesAsync();

            // Act
            var result = await repository.GetByIdAsync(1);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(1, result.EventId);
            Assert.Equal("Test Event", result.EventName);
            Assert.Equal("Test Content", result.Content);
        }

        [Fact]
        public async Task GetByIdAsync_ShouldReturnNull_WhenEventDoesNotExist()
        {
            // Arrange
            var context = GetInMemoryContext("GetById_NotFound");
            var repository = new EventRepository(context);

            // Act
            var result = await repository.GetByIdAsync(999);

            // Assert
            Assert.Null(result);
        }

        #endregion

        #region CreateAsync Tests

        [Fact]
        public async Task CreateAsync_ShouldAddEventToDatabase()
        {
            // Arrange
            var context = GetInMemoryContext("Create_Success");
            var repository = new EventRepository(context);

            var user = new User { UserId = 1, FullName = "User One", Email = "user1@test.com" };
            context.Users.Add(user);
            await context.SaveChangesAsync();

            var newEvent = new DataLayer.Models.Event
            {
                EventName = "New Event",
                Content = "New Content",
                StartDate = new DateTime(2025, 6, 1),
                EndDate = new DateTime(2025, 6, 5),
                CreateAt = DateTime.UtcNow,
                CreateBy = 1
            };

            // Act
            var eventId = await repository.CreateAsync(newEvent);

            // Assert
            Assert.True(eventId > 0);
            var savedEvent = await context.Events.FindAsync(eventId);
            Assert.NotNull(savedEvent);
            Assert.Equal("New Event", savedEvent.EventName);
        }

        [Fact]
        public async Task CreateAsync_ShouldReturnGeneratedEventId()
        {
            // Arrange
            var context = GetInMemoryContext("Create_ReturnId");
            var repository = new EventRepository(context);

            var user = new User { UserId = 1, FullName = "User One", Email = "user1@test.com" };
            context.Users.Add(user);
            await context.SaveChangesAsync();

            var newEvent = new DataLayer.Models.Event
            {
                EventName = "Test Event",
                StartDate = new DateTime(2025, 6, 1),
                EndDate = new DateTime(2025, 6, 5),
                CreateAt = DateTime.UtcNow,
                CreateBy = 1
            };

            // Act
            var eventId = await repository.CreateAsync(newEvent);

            // Assert
            Assert.Equal(newEvent.EventId, eventId);
        }

        #endregion

        #region UpdateAsync Tests

        [Fact]
        public async Task UpdateAsync_ShouldUpdateEvent_WhenEventExists()
        {
            // Arrange
            var context = GetInMemoryContext("Update_Success");
            var repository = new EventRepository(context);

            var user = new User { UserId = 1, FullName = "User One", Email = "user1@test.com" };
            context.Users.Add(user);

            var existingEvent = new DataLayer.Models.Event
            {
                EventId = 1,
                EventName = "Original Name",
                Content = "Original Content",
                StartDate = new DateTime(2025, 6, 1),
                EndDate = new DateTime(2025, 6, 5),
                CreateAt = DateTime.UtcNow,
                CreateBy = 1
            };
            context.Events.Add(existingEvent);
            await context.SaveChangesAsync();

            // Detach to simulate a new context
            context.Entry(existingEvent).State = EntityState.Detached;

            var updatedEvent = new DataLayer.Models.Event
            {
                EventId = 1,
                EventName = "Updated Name",
                Content = "Updated Content",
                StartDate = new DateTime(2025, 7, 1),
                EndDate = new DateTime(2025, 7, 5),
                UpdateAt = DateTime.UtcNow,
                UpdateBy = 1
            };

            // Act
            var result = await repository.UpdateAsync(updatedEvent);

            // Assert
            Assert.True(result);
            var savedEvent = await context.Events.FindAsync(1);
            Assert.Equal("Updated Name", savedEvent.EventName);
            Assert.Equal("Updated Content", savedEvent.Content);
            Assert.Equal(new DateTime(2025, 7, 1), savedEvent.StartDate);
        }

        [Fact]
        public async Task UpdateAsync_ShouldReturnFalse_WhenEventDoesNotExist()
        {
            // Arrange
            var context = GetInMemoryContext("Update_NotFound");
            var repository = new EventRepository(context);

            var updatedEvent = new DataLayer.Models.Event
            {
                EventId = 999,
                EventName = "Non-existent Event",
                StartDate = new DateTime(2025, 6, 1),
                EndDate = new DateTime(2025, 6, 5)
            };

            // Act
            var result = await repository.UpdateAsync(updatedEvent);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task UpdateAsync_ShouldPreserveCreateAtAndCreateBy()
        {
            // Arrange
            var context = GetInMemoryContext("Update_PreserveAudit");
            var repository = new EventRepository(context);

            var user1 = new User { UserId = 1, FullName = "User One", Email = "user1@test.com" };
            var user2 = new User { UserId = 2, FullName = "User Two", Email = "user2@test.com" };
            context.Users.AddRange(user1, user2);

            var originalCreateAt = new DateTime(2025, 1, 1);
            var existingEvent = new DataLayer.Models.Event
            {
                EventId = 1,
                EventName = "Original Name",
                StartDate = new DateTime(2025, 6, 1),
                EndDate = new DateTime(2025, 6, 5),
                CreateAt = originalCreateAt,
                CreateBy = 1
            };
            context.Events.Add(existingEvent);
            await context.SaveChangesAsync();

            context.Entry(existingEvent).State = EntityState.Detached;

            var updatedEvent = new DataLayer.Models.Event
            {
                EventId = 1,
                EventName = "Updated Name",
                StartDate = new DateTime(2025, 7, 1),
                EndDate = new DateTime(2025, 7, 5),
                UpdateAt = DateTime.UtcNow,
                UpdateBy = 2
            };

            // Act
            var result = await repository.UpdateAsync(updatedEvent);

            // Assert
            Assert.True(result);
            var savedEvent = await context.Events.FindAsync(1);
            Assert.Equal(originalCreateAt, savedEvent.CreateAt); // Preserved
            Assert.Equal(1, savedEvent.CreateBy); // Preserved
            Assert.NotNull(savedEvent.UpdateAt);
            Assert.Equal(2, savedEvent.UpdateBy);
        }

        #endregion

        #region DeleteAsync Tests

        [Fact]
        public async Task DeleteAsync_ShouldRemoveEvent_WhenEventExists()
        {
            // Arrange
            var context = GetInMemoryContext("Delete_Success");
            var repository = new EventRepository(context);

            var user = new User { UserId = 1, FullName = "User One", Email = "user1@test.com" };
            context.Users.Add(user);

            var existingEvent = new DataLayer.Models.Event
            {
                EventId = 1,
                EventName = "Event to Delete",
                StartDate = new DateTime(2025, 6, 1),
                EndDate = new DateTime(2025, 6, 5),
                CreateAt = DateTime.UtcNow,
                CreateBy = 1
            };
            context.Events.Add(existingEvent);
            await context.SaveChangesAsync();

            // Act
            var result = await repository.DeleteAsync(1);

            // Assert
            Assert.True(result);
            var deletedEvent = await context.Events.FindAsync(1);
            Assert.Null(deletedEvent);
        }

        [Fact]
        public async Task DeleteAsync_ShouldReturnFalse_WhenEventDoesNotExist()
        {
            // Arrange
            var context = GetInMemoryContext("Delete_NotFound");
            var repository = new EventRepository(context);

            // Act
            var result = await repository.DeleteAsync(999);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task DeleteAsync_ShouldNotAffectOtherEvents()
        {
            // Arrange
            var context = GetInMemoryContext("Delete_NoSideEffect");
            var repository = new EventRepository(context);

            var user = new User { UserId = 1, FullName = "User One", Email = "user1@test.com" };
            context.Users.Add(user);

            var event1 = new DataLayer.Models.Event
            {
                EventId = 1,
                EventName = "Event 1",
                StartDate = new DateTime(2025, 6, 1),
                EndDate = new DateTime(2025, 6, 5),
                CreateAt = DateTime.UtcNow,
                CreateBy = 1
            };
            var event2 = new DataLayer.Models.Event
            {
                EventId = 2,
                EventName = "Event 2",
                StartDate = new DateTime(2025, 7, 1),
                EndDate = new DateTime(2025, 7, 5),
                CreateAt = DateTime.UtcNow,
                CreateBy = 1
            };
            context.Events.AddRange(event1, event2);
            await context.SaveChangesAsync();

            // Act
            var result = await repository.DeleteAsync(1);

            // Assert
            Assert.True(result);
            Assert.Equal(1, await context.Events.CountAsync());
            var remainingEvent = await context.Events.FindAsync(2);
            Assert.NotNull(remainingEvent);
            Assert.Equal("Event 2", remainingEvent.EventName);
        }

        #endregion
    }
}
