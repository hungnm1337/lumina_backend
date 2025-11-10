using Xunit;
using DataLayer.DTOs;
using System;

namespace Lumina.Tests
{
    public class EventDTOTests
    {
        [Fact]
        public void EventDTO_ShouldInitializeWithDefaultValues()
        {
            // Arrange & Act
            var eventDto = new EventDTO();

            // Assert
            Assert.Equal(0, eventDto.EventId);
            Assert.Null(eventDto.EventName);
            Assert.Null(eventDto.Content);
            Assert.Equal(default(DateTime), eventDto.StartDate);
            Assert.Equal(default(DateTime), eventDto.EndDate);
            Assert.Equal(default(DateTime), eventDto.CreateAt);
            Assert.Null(eventDto.UpdateAt);
            Assert.Equal(0, eventDto.CreateBy);
            Assert.Null(eventDto.UpdateBy);
        }

        [Fact]
        public void EventDTO_ShouldSetAndGetEventId()
        {
            // Arrange
            var eventDto = new EventDTO();
            var expectedId = 42;

            // Act
            eventDto.EventId = expectedId;

            // Assert
            Assert.Equal(expectedId, eventDto.EventId);
        }

        [Fact]
        public void EventDTO_ShouldSetAndGetEventName()
        {
            // Arrange
            var eventDto = new EventDTO();
            var expectedName = "Tech Conference 2025";

            // Act
            eventDto.EventName = expectedName;

            // Assert
            Assert.Equal(expectedName, eventDto.EventName);
        }

        [Fact]
        public void EventDTO_ShouldSetAndGetContent()
        {
            // Arrange
            var eventDto = new EventDTO();
            var expectedContent = "Join us for an exciting tech conference featuring industry leaders.";

            // Act
            eventDto.Content = expectedContent;

            // Assert
            Assert.Equal(expectedContent, eventDto.Content);
        }

        [Fact]
        public void EventDTO_ShouldAllowNullContent()
        {
            // Arrange
            var eventDto = new EventDTO
            {
                EventName = "Event without content",
                Content = null
            };

            // Act & Assert
            Assert.Null(eventDto.Content);
        }

        [Fact]
        public void EventDTO_ShouldSetAndGetStartDate()
        {
            // Arrange
            var eventDto = new EventDTO();
            var expectedStartDate = new DateTime(2025, 6, 15, 9, 0, 0);

            // Act
            eventDto.StartDate = expectedStartDate;

            // Assert
            Assert.Equal(expectedStartDate, eventDto.StartDate);
        }

        [Fact]
        public void EventDTO_ShouldSetAndGetEndDate()
        {
            // Arrange
            var eventDto = new EventDTO();
            var expectedEndDate = new DateTime(2025, 6, 17, 18, 0, 0);

            // Act
            eventDto.EndDate = expectedEndDate;

            // Assert
            Assert.Equal(expectedEndDate, eventDto.EndDate);
        }

        [Fact]
        public void EventDTO_ShouldHandleDateRange()
        {
            // Arrange
            var eventDto = new EventDTO
            {
                StartDate = new DateTime(2025, 6, 15, 9, 0, 0),
                EndDate = new DateTime(2025, 6, 17, 18, 0, 0)
            };

            // Act
            var duration = eventDto.EndDate - eventDto.StartDate;

            // Assert
            Assert.True(eventDto.EndDate > eventDto.StartDate);
            Assert.Equal(2.375, duration.TotalDays, 2); // Approximately 2.375 days
        }

        [Fact]
        public void EventDTO_ShouldSetAndGetCreateAt()
        {
            // Arrange
            var eventDto = new EventDTO();
            var expectedCreateAt = DateTime.UtcNow;

            // Act
            eventDto.CreateAt = expectedCreateAt;

            // Assert
            Assert.Equal(expectedCreateAt, eventDto.CreateAt);
        }

        [Fact]
        public void EventDTO_ShouldSetAndGetUpdateAt()
        {
            // Arrange
            var eventDto = new EventDTO();
            var expectedUpdateAt = DateTime.UtcNow;

            // Act
            eventDto.UpdateAt = expectedUpdateAt;

            // Assert
            Assert.Equal(expectedUpdateAt, eventDto.UpdateAt);
        }

        [Fact]
        public void EventDTO_ShouldAllowNullUpdateAt()
        {
            // Arrange
            var eventDto = new EventDTO
            {
                CreateAt = DateTime.UtcNow,
                UpdateAt = null
            };

            // Act & Assert
            Assert.Null(eventDto.UpdateAt);
        }

        [Fact]
        public void EventDTO_ShouldSetAndGetCreateBy()
        {
            // Arrange
            var eventDto = new EventDTO();
            var expectedUserId = 101;

            // Act
            eventDto.CreateBy = expectedUserId;

            // Assert
            Assert.Equal(expectedUserId, eventDto.CreateBy);
        }

        [Fact]
        public void EventDTO_ShouldSetAndGetUpdateBy()
        {
            // Arrange
            var eventDto = new EventDTO();
            var expectedUserId = 202;

            // Act
            eventDto.UpdateBy = expectedUserId;

            // Assert
            Assert.Equal(expectedUserId, eventDto.UpdateBy);
        }

        [Fact]
        public void EventDTO_ShouldAllowNullUpdateBy()
        {
            // Arrange
            var eventDto = new EventDTO
            {
                CreateBy = 101,
                UpdateBy = null
            };

            // Act & Assert
            Assert.Null(eventDto.UpdateBy);
        }

        [Fact]
        public void EventDTO_ShouldHandleCompleteEventCreationScenario()
        {
            // Arrange
            var createdAt = DateTime.UtcNow;
            var startDate = createdAt.AddDays(30);
            var endDate = startDate.AddDays(3);

            // Act
            var eventDto = new EventDTO
            {
                EventId = 1,
                EventName = "Annual Tech Summit 2025",
                Content = "A gathering of innovators and tech enthusiasts.",
                StartDate = startDate,
                EndDate = endDate,
                CreateAt = createdAt,
                UpdateAt = null,
                CreateBy = 15,
                UpdateBy = null
            };

            // Assert
            Assert.Equal(1, eventDto.EventId);
            Assert.Equal("Annual Tech Summit 2025", eventDto.EventName);
            Assert.NotNull(eventDto.Content);
            Assert.True(eventDto.StartDate > eventDto.CreateAt);
            Assert.True(eventDto.EndDate > eventDto.StartDate);
            Assert.Null(eventDto.UpdateAt);
            Assert.Equal(15, eventDto.CreateBy);
            Assert.Null(eventDto.UpdateBy);
        }

        [Fact]
        public void EventDTO_ShouldHandleEventUpdateScenario()
        {
            // Arrange
            var createdAt = new DateTime(2025, 1, 1, 10, 0, 0);
            var updatedAt = new DateTime(2025, 2, 15, 14, 30, 0);

            var eventDto = new EventDTO
            {
                EventId = 5,
                EventName = "Workshop Series",
                Content = "Updated content with new information.",
                StartDate = new DateTime(2025, 6, 1),
                EndDate = new DateTime(2025, 6, 5),
                CreateAt = createdAt,
                UpdateAt = updatedAt,
                CreateBy = 10,
                UpdateBy = 20
            };

            // Act & Assert
            Assert.NotNull(eventDto.UpdateAt);
            Assert.True(eventDto.UpdateAt > eventDto.CreateAt);
            Assert.NotNull(eventDto.UpdateBy);
            Assert.NotEqual(eventDto.CreateBy, eventDto.UpdateBy);
        }

        [Fact]
        public void EventDTO_ShouldHandleLongEventName()
        {
            // Arrange
            var longName = new string('A', 500);
            var eventDto = new EventDTO();

            // Act
            eventDto.EventName = longName;

            // Assert
            Assert.Equal(500, eventDto.EventName.Length);
            Assert.Equal(longName, eventDto.EventName);
        }

        [Fact]
        public void EventDTO_ShouldHandleLongContent()
        {
            // Arrange
            var longContent = new string('B', 5000);
            var eventDto = new EventDTO();

            // Act
            eventDto.Content = longContent;

            // Assert
            Assert.Equal(5000, eventDto.Content.Length);
            Assert.Equal(longContent, eventDto.Content);
        }

        [Fact]
        public void EventDTO_ShouldHandleUnicodeCharactersInEventName()
        {
            // Arrange
            var eventDto = new EventDTO();
            var unicodeName = "Hội thảo Công nghệ 2025 🚀";

            // Act
            eventDto.EventName = unicodeName;

            // Assert
            Assert.Equal(unicodeName, eventDto.EventName);
        }

        [Fact]
        public void EventDTO_ShouldHandleMultilineContent()
        {
            // Arrange
            var eventDto = new EventDTO();
            var multilineContent = @"Line 1: Introduction
Line 2: Agenda
Line 3: Speakers
Line 4: Conclusion";

            // Act
            eventDto.Content = multilineContent;

            // Assert
            Assert.Contains("Line 1", eventDto.Content);
            Assert.Contains("Line 4", eventDto.Content);
        }

        [Fact]
        public void EventDTO_ShouldHandleSingleDayEvent()
        {
            // Arrange
            var eventDate = new DateTime(2025, 7, 20, 9, 0, 0);
            var eventDto = new EventDTO
            {
                StartDate = eventDate,
                EndDate = eventDate.AddHours(8)
            };

            // Act
            var duration = eventDto.EndDate - eventDto.StartDate;

            // Assert
            Assert.Equal(8, duration.TotalHours);
        }

        [Fact]
        public void EventDTO_ShouldHandleMultiDayEvent()
        {
            // Arrange
            var eventDto = new EventDTO
            {
                StartDate = new DateTime(2025, 8, 1, 9, 0, 0),
                EndDate = new DateTime(2025, 8, 7, 18, 0, 0)
            };

            // Act
            var duration = eventDto.EndDate - eventDto.StartDate;

            // Assert
            Assert.True(duration.TotalDays > 6);
        }

        [Fact]
        public void EventDTO_ShouldHandleAuditTrailForNewEvent()
        {
            // Arrange
            var now = DateTime.UtcNow;
            var eventDto = new EventDTO
            {
                CreateAt = now,
                CreateBy = 5,
                UpdateAt = null,
                UpdateBy = null
            };

            // Act & Assert
            Assert.NotEqual(default(DateTime), eventDto.CreateAt);
            Assert.Null(eventDto.UpdateAt);
            Assert.True(eventDto.CreateBy > 0);
            Assert.Null(eventDto.UpdateBy);
        }

        [Fact]
        public void EventDTO_ShouldHandleAuditTrailForUpdatedEvent()
        {
            // Arrange
            var createTime = DateTime.UtcNow.AddDays(-10);
            var updateTime = DateTime.UtcNow;
            var eventDto = new EventDTO
            {
                CreateAt = createTime,
                CreateBy = 5,
                UpdateAt = updateTime,
                UpdateBy = 12
            };

            // Act & Assert
            Assert.NotNull(eventDto.UpdateAt);
            Assert.True(eventDto.UpdateAt > eventDto.CreateAt);
            Assert.NotNull(eventDto.UpdateBy);
        }
    }
}
