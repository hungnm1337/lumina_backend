using Xunit;
using Moq;
using Microsoft.AspNetCore.SignalR;
using ServiceLayer.Hubs;
using System.Security.Claims;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using System.Reflection;

namespace Lumina.Test.Services
{
    public class NotificationHubUnitTest
    {
        private readonly Mock<HubCallerContext> _mockContext;
        private readonly Mock<IGroupManager> _mockGroups;
        private readonly NotificationHub _hub;

        public NotificationHubUnitTest()
        {
            _mockContext = new Mock<HubCallerContext>();
            _mockGroups = new Mock<IGroupManager>();
            _hub = new NotificationHub();
            ClearUserConnections(); // Clear connections before each test
        }

        private void SetHubContext(HubCallerContext context, IGroupManager groups)
        {
            // Use reflection to set Context and Groups properties
            var contextProperty = typeof(Hub).GetProperty("Context", BindingFlags.Public | BindingFlags.Instance);
            var groupsProperty = typeof(Hub).GetProperty("Groups", BindingFlags.Public | BindingFlags.Instance);
            
            contextProperty?.SetValue(_hub, context);
            groupsProperty?.SetValue(_hub, groups);
        }

        private void ClearUserConnections()
        {
            // Use reflection to clear the static dictionary
            var field = typeof(NotificationHub).GetField("_userConnections", BindingFlags.NonPublic | BindingFlags.Static);
            if (field?.GetValue(null) is Dictionary<int, string> dictionary)
            {
                dictionary.Clear();
            }
        }

        #region OnConnectedAsync Tests

        [Fact]
        public async Task OnConnectedAsync_WhenUserIdIsValid_ShouldAddToConnectionsAndGroup()
        {
            // Arrange
            int userId = 1;
            string connectionId = "connection-123";
            var claim = new Claim(ClaimTypes.NameIdentifier, userId.ToString());
            var claims = new List<Claim> { claim };
            var identity = new ClaimsIdentity(claims, "TestAuth");
            var principal = new ClaimsPrincipal(identity);

            _mockContext.Setup(c => c.User).Returns(principal);
            _mockContext.Setup(c => c.ConnectionId).Returns(connectionId);
            _mockGroups.Setup(g => g.AddToGroupAsync(connectionId, "AllUsers", default))
                .Returns(Task.CompletedTask);

            SetHubContext(_mockContext.Object, _mockGroups.Object);

            // Act
            await _hub.OnConnectedAsync();

            // Assert
            _mockGroups.Verify(
                g => g.AddToGroupAsync(connectionId, "AllUsers", default),
                Times.Once
            );

            var storedConnectionId = NotificationHub.GetConnectionId(userId);
            Assert.Equal(connectionId, storedConnectionId);
        }

        [Fact]
        public async Task OnConnectedAsync_WhenUserIsNull_ShouldNotAddToConnections()
        {
            // Arrange
            string connectionId = "connection-123";
            _mockContext.Setup(c => c.User).Returns((ClaimsPrincipal?)null);
            _mockContext.Setup(c => c.ConnectionId).Returns(connectionId);

            SetHubContext(_mockContext.Object, _mockGroups.Object);

            // Act
            await _hub.OnConnectedAsync();

            // Assert
            _mockGroups.Verify(
                g => g.AddToGroupAsync(It.IsAny<string>(), It.IsAny<string>(), default),
                Times.Never
            );
        }

        [Fact]
        public async Task OnConnectedAsync_WhenUserIdClaimIsMissing_ShouldNotAddToConnections()
        {
            // Arrange
            string connectionId = "connection-123";
            var claims = new List<Claim>();
            var identity = new ClaimsIdentity(claims, "TestAuth");
            var principal = new ClaimsPrincipal(identity);

            _mockContext.Setup(c => c.User).Returns(principal);
            _mockContext.Setup(c => c.ConnectionId).Returns(connectionId);

            SetHubContext(_mockContext.Object, _mockGroups.Object);

            // Act
            await _hub.OnConnectedAsync();

            // Assert
            _mockGroups.Verify(
                g => g.AddToGroupAsync(It.IsAny<string>(), It.IsAny<string>(), default),
                Times.Never
            );
        }

        [Fact]
        public async Task OnConnectedAsync_WhenUserIdClaimIsInvalid_ShouldNotAddToConnections()
        {
            // Arrange
            string connectionId = "connection-123";
            var claim = new Claim(ClaimTypes.NameIdentifier, "invalid-number");
            var claims = new List<Claim> { claim };
            var identity = new ClaimsIdentity(claims, "TestAuth");
            var principal = new ClaimsPrincipal(identity);

            _mockContext.Setup(c => c.User).Returns(principal);
            _mockContext.Setup(c => c.ConnectionId).Returns(connectionId);

            SetHubContext(_mockContext.Object, _mockGroups.Object);

            // Act
            await _hub.OnConnectedAsync();

            // Assert
            _mockGroups.Verify(
                g => g.AddToGroupAsync(It.IsAny<string>(), It.IsAny<string>(), default),
                Times.Never
            );
        }

        [Fact]
        public async Task OnConnectedAsync_WhenUserIdClaimIsEmpty_ShouldNotAddToConnections()
        {
            // Arrange
            string connectionId = "connection-123";
            var claim = new Claim(ClaimTypes.NameIdentifier, string.Empty);
            var claims = new List<Claim> { claim };
            var identity = new ClaimsIdentity(claims, "TestAuth");
            var principal = new ClaimsPrincipal(identity);

            _mockContext.Setup(c => c.User).Returns(principal);
            _mockContext.Setup(c => c.ConnectionId).Returns(connectionId);

            SetHubContext(_mockContext.Object, _mockGroups.Object);

            // Act
            await _hub.OnConnectedAsync();

            // Assert
            _mockGroups.Verify(
                g => g.AddToGroupAsync(It.IsAny<string>(), It.IsAny<string>(), default),
                Times.Never
            );
        }

        [Fact]
        public async Task OnConnectedAsync_WhenMultipleUsersConnect_ShouldStoreAllConnections()
        {
            // Arrange
            int userId1 = 1;
            int userId2 = 2;
            string connectionId1 = "connection-123";
            string connectionId2 = "connection-456";

            // First user connects
            var claim1 = new Claim(ClaimTypes.NameIdentifier, userId1.ToString());
            var claims1 = new List<Claim> { claim1 };
            var identity1 = new ClaimsIdentity(claims1, "TestAuth");
            var principal1 = new ClaimsPrincipal(identity1);

            _mockContext.Setup(c => c.User).Returns(principal1);
            _mockContext.Setup(c => c.ConnectionId).Returns(connectionId1);
            _mockGroups.Setup(g => g.AddToGroupAsync(connectionId1, "AllUsers", default))
                .Returns(Task.CompletedTask);

            SetHubContext(_mockContext.Object, _mockGroups.Object);

            // Act - First connection
            await _hub.OnConnectedAsync();

            // Second user connects
            var claim2 = new Claim(ClaimTypes.NameIdentifier, userId2.ToString());
            var claims2 = new List<Claim> { claim2 };
            var identity2 = new ClaimsIdentity(claims2, "TestAuth");
            var principal2 = new ClaimsPrincipal(identity2);

            _mockContext.Setup(c => c.User).Returns(principal2);
            _mockContext.Setup(c => c.ConnectionId).Returns(connectionId2);
            _mockGroups.Setup(g => g.AddToGroupAsync(connectionId2, "AllUsers", default))
                .Returns(Task.CompletedTask);

            SetHubContext(_mockContext.Object, _mockGroups.Object);

            // Act - Second connection
            await _hub.OnConnectedAsync();

            // Assert
            var storedConnectionId1 = NotificationHub.GetConnectionId(userId1);
            var storedConnectionId2 = NotificationHub.GetConnectionId(userId2);
            Assert.Equal(connectionId1, storedConnectionId1);
            Assert.Equal(connectionId2, storedConnectionId2);

            var connectedUserIds = NotificationHub.GetConnectedUserIds().ToList();
            Assert.Contains(userId1, connectedUserIds);
            Assert.Contains(userId2, connectedUserIds);
        }

        #endregion

        #region OnDisconnectedAsync Tests

        [Fact]
        public async Task OnDisconnectedAsync_WhenUserIdIsValid_ShouldRemoveFromConnections()
        {
            // Arrange
            int userId = 1;
            string connectionId = "connection-123";
            
            // First connect the user
            var claim = new Claim(ClaimTypes.NameIdentifier, userId.ToString());
            var claims = new List<Claim> { claim };
            var identity = new ClaimsIdentity(claims, "TestAuth");
            var principal = new ClaimsPrincipal(identity);

            _mockContext.Setup(c => c.User).Returns(principal);
            _mockContext.Setup(c => c.ConnectionId).Returns(connectionId);
            _mockGroups.Setup(g => g.AddToGroupAsync(connectionId, "AllUsers", default))
                .Returns(Task.CompletedTask);

            SetHubContext(_mockContext.Object, _mockGroups.Object);
            await _hub.OnConnectedAsync();

            // Verify user is connected
            Assert.NotNull(NotificationHub.GetConnectionId(userId));

            // Act - Disconnect
            SetHubContext(_mockContext.Object, _mockGroups.Object);
            await _hub.OnDisconnectedAsync(null);

            // Assert
            var storedConnectionId = NotificationHub.GetConnectionId(userId);
            Assert.Null(storedConnectionId);
        }

        [Fact]
        public async Task OnDisconnectedAsync_WhenUserIsNull_ShouldNotRemoveFromConnections()
        {
            // Arrange
            int userId = 1;
            string connectionId = "connection-123";
            
            // First connect the user
            var claim = new Claim(ClaimTypes.NameIdentifier, userId.ToString());
            var claims = new List<Claim> { claim };
            var identity = new ClaimsIdentity(claims, "TestAuth");
            var principal = new ClaimsPrincipal(identity);

            _mockContext.Setup(c => c.User).Returns(principal);
            _mockContext.Setup(c => c.ConnectionId).Returns(connectionId);
            _mockGroups.Setup(g => g.AddToGroupAsync(connectionId, "AllUsers", default))
                .Returns(Task.CompletedTask);

            SetHubContext(_mockContext.Object, _mockGroups.Object);
            await _hub.OnConnectedAsync();

            // Setup for disconnect - user is null
            _mockContext.Setup(c => c.User).Returns((ClaimsPrincipal?)null);
            SetHubContext(_mockContext.Object, _mockGroups.Object);

            // Act
            await _hub.OnDisconnectedAsync(null);

            // Assert - User should still be connected because user was null during disconnect
            var storedConnectionId = NotificationHub.GetConnectionId(userId);
            Assert.NotNull(storedConnectionId);
        }

        [Fact]
        public async Task OnDisconnectedAsync_WhenUserIdClaimIsMissing_ShouldNotRemoveFromConnections()
        {
            // Arrange
            int userId = 1;
            string connectionId = "connection-123";
            
            // First connect the user
            var claim = new Claim(ClaimTypes.NameIdentifier, userId.ToString());
            var claims = new List<Claim> { claim };
            var identity = new ClaimsIdentity(claims, "TestAuth");
            var principal = new ClaimsPrincipal(identity);

            _mockContext.Setup(c => c.User).Returns(principal);
            _mockContext.Setup(c => c.ConnectionId).Returns(connectionId);
            _mockGroups.Setup(g => g.AddToGroupAsync(connectionId, "AllUsers", default))
                .Returns(Task.CompletedTask);

            SetHubContext(_mockContext.Object, _mockGroups.Object);
            await _hub.OnConnectedAsync();

            // Setup for disconnect - no userId claim
            var emptyClaims = new List<Claim>();
            var emptyIdentity = new ClaimsIdentity(emptyClaims, "TestAuth");
            var emptyPrincipal = new ClaimsPrincipal(emptyIdentity);
            _mockContext.Setup(c => c.User).Returns(emptyPrincipal);
            SetHubContext(_mockContext.Object, _mockGroups.Object);

            // Act
            await _hub.OnDisconnectedAsync(null);

            // Assert - User should still be connected
            var storedConnectionId = NotificationHub.GetConnectionId(userId);
            Assert.NotNull(storedConnectionId);
        }

        [Fact]
        public async Task OnDisconnectedAsync_WhenUserIdClaimIsInvalid_ShouldNotRemoveFromConnections()
        {
            // Arrange
            int userId = 1;
            string connectionId = "connection-123";
            
            // First connect the user
            var claim = new Claim(ClaimTypes.NameIdentifier, userId.ToString());
            var claims = new List<Claim> { claim };
            var identity = new ClaimsIdentity(claims, "TestAuth");
            var principal = new ClaimsPrincipal(identity);

            _mockContext.Setup(c => c.User).Returns(principal);
            _mockContext.Setup(c => c.ConnectionId).Returns(connectionId);
            _mockGroups.Setup(g => g.AddToGroupAsync(connectionId, "AllUsers", default))
                .Returns(Task.CompletedTask);

            SetHubContext(_mockContext.Object, _mockGroups.Object);
            await _hub.OnConnectedAsync();

            // Setup for disconnect - invalid userId claim
            var invalidClaim = new Claim(ClaimTypes.NameIdentifier, "invalid-number");
            var invalidClaims = new List<Claim> { invalidClaim };
            var invalidIdentity = new ClaimsIdentity(invalidClaims, "TestAuth");
            var invalidPrincipal = new ClaimsPrincipal(invalidIdentity);
            _mockContext.Setup(c => c.User).Returns(invalidPrincipal);
            SetHubContext(_mockContext.Object, _mockGroups.Object);

            // Act
            await _hub.OnDisconnectedAsync(null);

            // Assert - User should still be connected
            var storedConnectionId = NotificationHub.GetConnectionId(userId);
            Assert.NotNull(storedConnectionId);
        }

        [Fact]
        public async Task OnDisconnectedAsync_WhenExceptionIsProvided_ShouldStillRemoveFromConnections()
        {
            // Arrange
            int userId = 1;
            string connectionId = "connection-123";
            var exception = new Exception("Connection error");
            
            // First connect the user
            var claim = new Claim(ClaimTypes.NameIdentifier, userId.ToString());
            var claims = new List<Claim> { claim };
            var identity = new ClaimsIdentity(claims, "TestAuth");
            var principal = new ClaimsPrincipal(identity);

            _mockContext.Setup(c => c.User).Returns(principal);
            _mockContext.Setup(c => c.ConnectionId).Returns(connectionId);
            _mockGroups.Setup(g => g.AddToGroupAsync(connectionId, "AllUsers", default))
                .Returns(Task.CompletedTask);

            SetHubContext(_mockContext.Object, _mockGroups.Object);
            await _hub.OnConnectedAsync();

            // Act - Disconnect with exception
            SetHubContext(_mockContext.Object, _mockGroups.Object);
            await _hub.OnDisconnectedAsync(exception);

            // Assert
            var storedConnectionId = NotificationHub.GetConnectionId(userId);
            Assert.Null(storedConnectionId);
        }

        [Fact]
        public async Task OnDisconnectedAsync_WhenUserNotInConnections_ShouldNotThrow()
        {
            // Arrange
            int userId = 999; // User that was never connected
            var claim = new Claim(ClaimTypes.NameIdentifier, userId.ToString());
            var claims = new List<Claim> { claim };
            var identity = new ClaimsIdentity(claims, "TestAuth");
            var principal = new ClaimsPrincipal(identity);

            _mockContext.Setup(c => c.User).Returns(principal);
            SetHubContext(_mockContext.Object, _mockGroups.Object);

            // Act & Assert - Should not throw
            await _hub.OnDisconnectedAsync(null);
            
            var storedConnectionId = NotificationHub.GetConnectionId(userId);
            Assert.Null(storedConnectionId);
        }

        #endregion

        #region GetConnectionId Tests

        [Fact]
        public async Task GetConnectionId_WhenUserIdExists_ShouldReturnConnectionId()
        {
            // Arrange
            int userId = 1;
            string connectionId = "connection-123";
            
            // Connect user first
            var claim = new Claim(ClaimTypes.NameIdentifier, userId.ToString());
            var claims = new List<Claim> { claim };
            var identity = new ClaimsIdentity(claims, "TestAuth");
            var principal = new ClaimsPrincipal(identity);

            _mockContext.Setup(c => c.User).Returns(principal);
            _mockContext.Setup(c => c.ConnectionId).Returns(connectionId);
            _mockGroups.Setup(g => g.AddToGroupAsync(connectionId, "AllUsers", default))
                .Returns(Task.CompletedTask);

            SetHubContext(_mockContext.Object, _mockGroups.Object);
            await _hub.OnConnectedAsync();

            // Act
            var result = NotificationHub.GetConnectionId(userId);

            // Assert
            Assert.Equal(connectionId, result);
        }

        [Fact]
        public void GetConnectionId_WhenUserIdDoesNotExist_ShouldReturnNull()
        {
            // Arrange
            int userId = 999;

            // Act
            var result = NotificationHub.GetConnectionId(userId);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public void GetConnectionId_WhenUserIdIsZero_ShouldReturnNull()
        {
            // Arrange
            int userId = 0;

            // Act
            var result = NotificationHub.GetConnectionId(userId);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public void GetConnectionId_WhenUserIdIsNegative_ShouldReturnNull()
        {
            // Arrange
            int userId = -1;

            // Act
            var result = NotificationHub.GetConnectionId(userId);

            // Assert
            Assert.Null(result);
        }

        #endregion

        #region GetConnectedUserIds Tests

        [Fact]
        public void GetConnectedUserIds_WhenNoUsersConnected_ShouldReturnEmpty()
        {
            // Arrange
            ClearUserConnections();

            // Act
            var result = NotificationHub.GetConnectedUserIds();

            // Assert
            Assert.Empty(result);
        }

        [Fact]
        public async Task GetConnectedUserIds_WhenOneUserConnected_ShouldReturnOneUserId()
        {
            // Arrange
            int userId = 1;
            string connectionId = "connection-123";
            
            var claim = new Claim(ClaimTypes.NameIdentifier, userId.ToString());
            var claims = new List<Claim> { claim };
            var identity = new ClaimsIdentity(claims, "TestAuth");
            var principal = new ClaimsPrincipal(identity);

            _mockContext.Setup(c => c.User).Returns(principal);
            _mockContext.Setup(c => c.ConnectionId).Returns(connectionId);
            _mockGroups.Setup(g => g.AddToGroupAsync(connectionId, "AllUsers", default))
                .Returns(Task.CompletedTask);

            SetHubContext(_mockContext.Object, _mockGroups.Object);
            await _hub.OnConnectedAsync();

            // Act
            var result = NotificationHub.GetConnectedUserIds().ToList();

            // Assert
            Assert.Single(result);
            Assert.Contains(userId, result);
        }

        [Fact]
        public async Task GetConnectedUserIds_WhenMultipleUsersConnected_ShouldReturnAllUserIds()
        {
            // Arrange
            int userId1 = 1;
            int userId2 = 2;
            int userId3 = 3;
            string connectionId1 = "connection-123";
            string connectionId2 = "connection-456";
            string connectionId3 = "connection-789";

            // Connect first user
            var claim1 = new Claim(ClaimTypes.NameIdentifier, userId1.ToString());
            var claims1 = new List<Claim> { claim1 };
            var identity1 = new ClaimsIdentity(claims1, "TestAuth");
            var principal1 = new ClaimsPrincipal(identity1);

            _mockContext.Setup(c => c.User).Returns(principal1);
            _mockContext.Setup(c => c.ConnectionId).Returns(connectionId1);
            _mockGroups.Setup(g => g.AddToGroupAsync(connectionId1, "AllUsers", default))
                .Returns(Task.CompletedTask);
            SetHubContext(_mockContext.Object, _mockGroups.Object);
            await _hub.OnConnectedAsync();

            // Connect second user
            var claim2 = new Claim(ClaimTypes.NameIdentifier, userId2.ToString());
            var claims2 = new List<Claim> { claim2 };
            var identity2 = new ClaimsIdentity(claims2, "TestAuth");
            var principal2 = new ClaimsPrincipal(identity2);

            _mockContext.Setup(c => c.User).Returns(principal2);
            _mockContext.Setup(c => c.ConnectionId).Returns(connectionId2);
            _mockGroups.Setup(g => g.AddToGroupAsync(connectionId2, "AllUsers", default))
                .Returns(Task.CompletedTask);
            SetHubContext(_mockContext.Object, _mockGroups.Object);
            await _hub.OnConnectedAsync();

            // Connect third user
            var claim3 = new Claim(ClaimTypes.NameIdentifier, userId3.ToString());
            var claims3 = new List<Claim> { claim3 };
            var identity3 = new ClaimsIdentity(claims3, "TestAuth");
            var principal3 = new ClaimsPrincipal(identity3);

            _mockContext.Setup(c => c.User).Returns(principal3);
            _mockContext.Setup(c => c.ConnectionId).Returns(connectionId3);
            _mockGroups.Setup(g => g.AddToGroupAsync(connectionId3, "AllUsers", default))
                .Returns(Task.CompletedTask);
            SetHubContext(_mockContext.Object, _mockGroups.Object);
            await _hub.OnConnectedAsync();

            // Act
            var result = NotificationHub.GetConnectedUserIds().ToList();

            // Assert
            Assert.Equal(3, result.Count);
            Assert.Contains(userId1, result);
            Assert.Contains(userId2, result);
            Assert.Contains(userId3, result);
        }

        [Fact]
        public async Task GetConnectedUserIds_WhenUserDisconnects_ShouldNotIncludeDisconnectedUser()
        {
            // Arrange
            int userId1 = 1;
            int userId2 = 2;
            string connectionId1 = "connection-123";
            string connectionId2 = "connection-456";

            // Connect first user
            var claim1 = new Claim(ClaimTypes.NameIdentifier, userId1.ToString());
            var claims1 = new List<Claim> { claim1 };
            var identity1 = new ClaimsIdentity(claims1, "TestAuth");
            var principal1 = new ClaimsPrincipal(identity1);

            _mockContext.Setup(c => c.User).Returns(principal1);
            _mockContext.Setup(c => c.ConnectionId).Returns(connectionId1);
            _mockGroups.Setup(g => g.AddToGroupAsync(connectionId1, "AllUsers", default))
                .Returns(Task.CompletedTask);
            SetHubContext(_mockContext.Object, _mockGroups.Object);
            await _hub.OnConnectedAsync();

            // Connect second user
            var claim2 = new Claim(ClaimTypes.NameIdentifier, userId2.ToString());
            var claims2 = new List<Claim> { claim2 };
            var identity2 = new ClaimsIdentity(claims2, "TestAuth");
            var principal2 = new ClaimsPrincipal(identity2);

            _mockContext.Setup(c => c.User).Returns(principal2);
            _mockContext.Setup(c => c.ConnectionId).Returns(connectionId2);
            _mockGroups.Setup(g => g.AddToGroupAsync(connectionId2, "AllUsers", default))
                .Returns(Task.CompletedTask);
            SetHubContext(_mockContext.Object, _mockGroups.Object);
            await _hub.OnConnectedAsync();

            // Disconnect first user
            _mockContext.Setup(c => c.User).Returns(principal1);
            SetHubContext(_mockContext.Object, _mockGroups.Object);
            await _hub.OnDisconnectedAsync(null);

            // Act
            var result = NotificationHub.GetConnectedUserIds().ToList();

            // Assert
            Assert.Single(result);
            Assert.Contains(userId2, result);
            Assert.DoesNotContain(userId1, result);
        }

        #endregion
    }
}

