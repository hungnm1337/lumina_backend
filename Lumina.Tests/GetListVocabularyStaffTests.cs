using DataLayer.DTOs.Auth;
using DataLayer.DTOs.Vocabulary;
using DataLayer.Models;
using lumina.Controllers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using RepositoryLayer.UnitOfWork;
using RepositoryLayer.User;
using ServiceLayer.TextToSpeech;
using System.Security.Claims;

namespace Lumina.Tests
{
    public class GetListVocabularyStaffTests
    {
        private readonly Mock<IUnitOfWork> _mockUnitOfWork;
        private readonly Mock<ITextToSpeechService> _mockTtsService;
        private readonly Mock<ILogger<VocabulariesController>> _mockLogger;
        private readonly VocabulariesController _controller;

        private readonly Mock<IUserRepository> _mockUserRepository;
        private readonly Mock<IVocabularyRepository> _mockVocabularyRepository;
        private readonly Mock<IVocabularyListRepository> _mockVocabularyListRepository;

        public GetListVocabularyStaffTests()
        {
            _mockUnitOfWork = new Mock<IUnitOfWork>();
            _mockTtsService = new Mock<ITextToSpeechService>();
            _mockLogger = new Mock<ILogger<VocabulariesController>>();
            _mockUserRepository = new Mock<IUserRepository>();
            _mockVocabularyRepository = new Mock<IVocabularyRepository>();
            _mockVocabularyListRepository = new Mock<IVocabularyListRepository>();

            // Setup mock repositories
            _mockUnitOfWork.Setup(u => u.Users).Returns(_mockUserRepository.Object);
            _mockUnitOfWork.Setup(u => u.Vocabularies).Returns(_mockVocabularyRepository.Object);
            _mockUnitOfWork.Setup(u => u.VocabularyLists).Returns(_mockVocabularyListRepository.Object);

            _controller = new VocabulariesController(_mockUnitOfWork.Object, _mockTtsService.Object);
        }

        #region Happy Path Tests

        [Fact]
        public async Task GetList_WithValidStaffUserAndOwnListId_ShouldReturn200OK()
        {
            // Arrange
            var userId = 1;
            var listId = 5;
            var staffUser = new User { UserId = userId, RoleId = 3 };
            var vocabularyList = new VocabularyList { VocabularyListId = listId, MakeBy = userId };
            var vocabularies = new List<Vocabulary>
            {
                new Vocabulary
                {
                    VocabularyId = 1,
                    VocabularyListId = listId,
                    Word = "Hello",
                    TypeOfWord = "noun",
                    Category = "greeting",
                    Definition = "Xin chào",
                    Example = "Hello world"
                }
            };

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, userId.ToString())
            };
            var identity = new ClaimsIdentity(claims, "TestAuthType");
            var principal = new ClaimsPrincipal(identity);
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = principal }
            };

            _mockUserRepository.Setup(u => u.GetUserByIdAsync(userId))
                .ReturnsAsync(staffUser);
            _mockVocabularyListRepository.Setup(u => u.FindByIdAsync(listId))
                .ReturnsAsync(vocabularyList);
            _mockVocabularyRepository.Setup(u => u.GetByListAsync(listId, null))
                .ReturnsAsync(vocabularies);

            // Act
            var result = await _controller.GetList(listId, null);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(StatusCodes.Status200OK, okResult.StatusCode);
            var response = okResult.Value as dynamic;
            Assert.NotNull(response);
        }

        [Fact]
        public async Task GetList_WithValidStaffUserWithoutListId_ShouldReturn200OK()
        {
            // Arrange
            var userId = 1;
            var staffUser = new User { UserId = userId, RoleId = 3 };
            var userLists = new List<VocabularyListDTO>
            {
                new VocabularyListDTO { VocabularyListId = 1, Name = "List 1" },
                new VocabularyListDTO { VocabularyListId = 2, Name = "List 2" }
            };
            var vocabularies = new List<Vocabulary>
            {
                new Vocabulary
                {
                    VocabularyId = 1,
                    VocabularyListId = 1,
                    Word = "Test",
                    TypeOfWord = "noun",
                    Definition = "Test definition"
                }
            };

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, userId.ToString())
            };
            var identity = new ClaimsIdentity(claims, "TestAuthType");
            var principal = new ClaimsPrincipal(identity);
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = principal }
            };

            _mockUserRepository.Setup(u => u.GetUserByIdAsync(userId))
                .ReturnsAsync(staffUser);
            _mockVocabularyListRepository.Setup(u => u.GetByUserAsync(userId, null))
                .ReturnsAsync(userLists);
            _mockVocabularyRepository.Setup(u => u.GetByListAsync(null, null))
                .ReturnsAsync(vocabularies);

            // Act
            var result = await _controller.GetList(null, null);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(StatusCodes.Status200OK, okResult.StatusCode);
        }

        [Fact]
        public async Task GetList_WithValidStaffUserWithoutListIdAndNoUserLists_ShouldReturnEmptyList()
        {
            // Arrange
            var userId = 1;
            var staffUser = new User { UserId = userId, RoleId = 3 };
            var userLists = new List<VocabularyListDTO>();

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, userId.ToString())
            };
            var identity = new ClaimsIdentity(claims, "TestAuthType");
            var principal = new ClaimsPrincipal(identity);
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = principal }
            };

            _mockUserRepository.Setup(u => u.GetUserByIdAsync(userId))
                .ReturnsAsync(staffUser);
            _mockVocabularyListRepository.Setup(u => u.GetByUserAsync(userId, null))
                .ReturnsAsync(userLists);

            // Act
            var result = await _controller.GetList(null, null);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(StatusCodes.Status200OK, okResult.StatusCode);
            var response = okResult.Value as IEnumerable<object>;
            Assert.Empty(response);
        }

        [Fact]
        public async Task GetList_WithNonStaffUser_ShouldReturn200OK()
        {
            // Arrange
            var userId = 1;
            var managerUser = new User { UserId = userId, RoleId = 2 }; // Manager, not Staff
            var vocabularies = new List<Vocabulary>
            {
                new Vocabulary
                {
                    VocabularyId = 1,
                    VocabularyListId = 1,
                    Word = "Test",
                    TypeOfWord = "noun",
                    Definition = "Test definition"
                }
            };

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, userId.ToString())
            };
            var identity = new ClaimsIdentity(claims, "TestAuthType");
            var principal = new ClaimsPrincipal(identity);
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = principal }
            };

            _mockUserRepository.Setup(u => u.GetUserByIdAsync(userId))
                .ReturnsAsync(managerUser);
            _mockVocabularyRepository.Setup(u => u.GetByListAsync(null, null))
                .ReturnsAsync(vocabularies);

            // Act
            var result = await _controller.GetList(null, null);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(StatusCodes.Status200OK, okResult.StatusCode);
        }

        #endregion

        #region Authorization Tests

        [Fact]
        public async Task GetList_WithNullUserIdClaim_ShouldReturn401Unauthorized()
        {
            // Arrange
            var identity = new ClaimsIdentity(); // No claims
            var principal = new ClaimsPrincipal(identity);
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = principal }
            };

            // Act
            var result = await _controller.GetList(null, null);

            // Assert
            var unauthorizedResult = Assert.IsType<UnauthorizedObjectResult>(result);
            Assert.Equal(StatusCodes.Status401Unauthorized, unauthorizedResult.StatusCode);
            var errorResponse = Assert.IsType<ErrorResponse>(unauthorizedResult.Value);
            Assert.Equal("Invalid token - User ID could not be determined.", errorResponse.Error);
        }

        [Fact]
        public async Task GetList_WithInvalidUserIdClaim_ShouldReturn401Unauthorized()
        {
            // Arrange
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, "invalid-number")
            };
            var identity = new ClaimsIdentity(claims, "TestAuthType");
            var principal = new ClaimsPrincipal(identity);
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = principal }
            };

            // Act
            var result = await _controller.GetList(null, null);

            // Assert
            var unauthorizedResult = Assert.IsType<UnauthorizedObjectResult>(result);
            Assert.Equal(StatusCodes.Status401Unauthorized, unauthorizedResult.StatusCode);
            var errorResponse = Assert.IsType<ErrorResponse>(unauthorizedResult.Value);
            Assert.Equal("Invalid token - User ID could not be determined.", errorResponse.Error);
        }

        [Fact]
        public async Task GetList_WithUserNotFound_ShouldReturn401Unauthorized()
        {
            // Arrange
            var userId = 1;
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, userId.ToString())
            };
            var identity = new ClaimsIdentity(claims, "TestAuthType");
            var principal = new ClaimsPrincipal(identity);
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = principal }
            };

            _mockUserRepository.Setup(u => u.GetUserByIdAsync(userId))
                .ReturnsAsync((User?)null);

            // Act
            var result = await _controller.GetList(null, null);

            // Assert
            var unauthorizedResult = Assert.IsType<UnauthorizedObjectResult>(result);
            Assert.Equal(StatusCodes.Status401Unauthorized, unauthorizedResult.StatusCode);
            var errorResponse = Assert.IsType<ErrorResponse>(unauthorizedResult.Value);
            Assert.Equal("User not found.", errorResponse.Error);
        }

        #endregion

        #region Authorization For Staff Tests

        [Fact]
        public async Task GetList_WithStaffUserAndInvalidListId_ShouldReturn403Forbid()
        {
            // Arrange
            var userId = 1;
            var otherUserId = 2;
            var listId = 5;
            var staffUser = new User { UserId = userId, RoleId = 3 };
            var vocabularyList = new VocabularyList { VocabularyListId = listId, MakeBy = otherUserId }; // Belongs to other user

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, userId.ToString())
            };
            var identity = new ClaimsIdentity(claims, "TestAuthType");
            var principal = new ClaimsPrincipal(identity);
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = principal }
            };

            _mockUserRepository.Setup(u => u.GetUserByIdAsync(userId))
                .ReturnsAsync(staffUser);
            _mockVocabularyListRepository.Setup(u => u.FindByIdAsync(listId))
                .ReturnsAsync(vocabularyList);

            // Act
            var result = await _controller.GetList(listId, null);

            // Assert
            var forbidResult = Assert.IsType<ForbidResult>(result);
        }

        [Fact]
        public async Task GetList_WithStaffUserAndListNotFound_ShouldReturn403Forbid()
        {
            // Arrange
            var userId = 1;
            var listId = 5;
            var staffUser = new User { UserId = userId, RoleId = 3 };

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, userId.ToString())
            };
            var identity = new ClaimsIdentity(claims, "TestAuthType");
            var principal = new ClaimsPrincipal(identity);
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = principal }
            };

            _mockUserRepository.Setup(u => u.GetUserByIdAsync(userId))
                .ReturnsAsync(staffUser);
            _mockVocabularyListRepository.Setup(u => u.FindByIdAsync(listId))
                .ReturnsAsync((VocabularyList?)null);

            // Act
            var result = await _controller.GetList(listId, null);

            // Assert
            var forbidResult = Assert.IsType<ForbidResult>(result);
        }

        #endregion

        #region Exception Handling Tests

        [Fact]
        public async Task GetList_WhenServiceThrowsException_ShouldReturn500InternalServerError()
        {
            // Arrange
            var userId = 1;
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, userId.ToString())
            };
            var identity = new ClaimsIdentity(claims, "TestAuthType");
            var principal = new ClaimsPrincipal(identity);
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = principal }
            };

            _mockUserRepository.Setup(u => u.GetUserByIdAsync(userId))
                .ThrowsAsync(new Exception("Test exception"));

            // Act
            var result = await _controller.GetList(null, null);

            // Assert
            var statusCodeResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(StatusCodes.Status500InternalServerError, statusCodeResult.StatusCode);
            var errorResponse = Assert.IsType<ErrorResponse>(statusCodeResult.Value);
            Assert.Equal("An internal server error occurred.", errorResponse.Error);
        }

        #endregion
    }
}

