using DataLayer.DTOs.Auth;
using DataLayer.DTOs.Vocabulary;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ServiceLayer.Vocabulary;
using System.Security.Claims;
using RepositoryLayer.UnitOfWork;

namespace lumina.Controllers
{
    [ApiController]
    [Route("api/vocabulary-lists")]
    [Authorize(Roles = "Staff,Manager")]
    public class VocabularyListsController : ControllerBase
    {
        private readonly IVocabularyListService _vocabularyListService;
        private readonly ILogger<VocabularyListsController> _logger;
        private readonly IUnitOfWork _unitOfWork;

        public VocabularyListsController(IVocabularyListService vocabularyListService, ILogger<VocabularyListsController> logger, IUnitOfWork unitOfWork)
        {
            _vocabularyListService = vocabularyListService;
            _logger = logger;
            _unitOfWork = unitOfWork;
        }

      
        [HttpPost]
        public async Task<IActionResult> CreateList([FromBody] VocabularyListCreateDTO request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
                if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out var creatorUserId))
                {
                    return Unauthorized(new ErrorResponse("Invalid token - User ID could not be determined."));
                }

                var createdList = await _vocabularyListService.CreateListAsync(request, creatorUserId);

                return CreatedAtAction(nameof(GetListById), new { id = createdList.VocabularyListId }, createdList);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while creating a vocabulary list.");
                return StatusCode(500, new ErrorResponse("An internal server error occurred."));
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetLists([FromQuery] string? searchTerm)
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
                if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out var currentUserId))
                {
                    _logger.LogWarning("Invalid token: Claim 'NameIdentifier' not found or invalid.");
                    return Unauthorized(new ErrorResponse("Invalid token - User ID could not be determined."));
                }

                _logger.LogInformation("Getting vocabulary lists for user {UserId}", currentUserId);

                // Lấy role của user hiện tại
                var user = await _unitOfWork.Users.GetUserByIdAsync(currentUserId);
                if (user == null)
                {
                    _logger.LogWarning("User {UserId} not found in database", currentUserId);
                    return Unauthorized(new ErrorResponse("User not found."));
                }

                _logger.LogInformation("User {UserId} has RoleId {RoleId}", currentUserId, user.RoleId);

                // Nếu là Staff (RoleID = 3), chỉ lấy vocabulary lists của chính họ
                if (user.RoleId == 3)
                {
                    _logger.LogInformation("User {UserId} is Staff, filtering vocabulary lists by MakeBy", currentUserId);
                    var lists = await _vocabularyListService.GetListsByUserAsync(currentUserId, searchTerm);
                    _logger.LogInformation("Found {Count} vocabulary lists for Staff {UserId}", lists.Count(), currentUserId);
                    return Ok(lists);
                }
                else
                {
                    _logger.LogInformation("User {UserId} is not Staff (RoleId: {RoleId}), getting all vocabulary lists", currentUserId, user.RoleId);
                    // Manager và Admin có thể xem tất cả vocabulary lists
                    var lists = await _vocabularyListService.GetListsAsync(searchTerm);
                    _logger.LogInformation("Found {Count} total vocabulary lists", lists.Count());
                    
                    // Debug: Log chi tiết từng list
                    foreach (var list in lists)
                    {
                        _logger.LogInformation("List: {Name}, Status: {Status}, MakeBy: {MakeBy}", 
                            list.Name, list.Status, list.MakeByName);
                    }
                    
                    return Ok(lists);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while fetching vocabulary lists.");
                return StatusCode(500, new ErrorResponse("An internal server error occurred."));
            }
        }

        [HttpGet("test")]
        public IActionResult TestEndpoint()
        {
            return Ok(new { message = "VocabularyLists API is working", timestamp = DateTime.UtcNow });
        }

        [HttpGet("{id}", Name = "GetListById")]
        public async Task<IActionResult> GetListById(int id)
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
                if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out var currentUserId))
                {
                    return Unauthorized(new ErrorResponse("Invalid token - User ID could not be determined."));
                }

                // Lấy thông tin user để kiểm tra role
                var user = await _unitOfWork.Users.GetUserByIdAsync(currentUserId);
                if (user == null)
                {
                    return Unauthorized(new ErrorResponse("User not found."));
                }

                // Lấy vocabulary list
                var vocabularyList = await _unitOfWork.VocabularyLists.FindByIdAsync(id);
                if (vocabularyList == null)
                {
                    return NotFound(new ErrorResponse($"Vocabulary list with ID {id} not found."));
                }

                // Nếu là Staff (RoleID = 3), chỉ có thể xem vocabulary list của chính họ
                if (user.RoleId == 3 && vocabularyList.MakeBy != currentUserId)
                {
                    return Forbid("You can only view your own vocabulary lists.");
                }

                // TODO: Implement proper response
                return Ok(new { Message = $"Vocabulary list with ID {id} found" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while fetching vocabulary list with ID {Id}.", id);
                return StatusCode(500, new ErrorResponse("An internal server error occurred."));
            }
        }

        // POST api/vocabulary-lists/{id}/request-approval
        [HttpPost("{id}/request-approval")]
        [Authorize(Roles = "Staff")]
        public async Task<IActionResult> RequestApproval(int id)
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
                if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out var staffUserId))
                {
                    return Unauthorized(new ErrorResponse("Invalid token."));
                }

                // Kiểm tra xem staff có phải là tác giả của vocabulary list không
                var vocabularyList = await _unitOfWork.VocabularyLists.FindByIdAsync(id);
                if (vocabularyList == null)
                {
                    return NotFound(new ErrorResponse($"Vocabulary list with ID {id} not found."));
                }

                // Lấy thông tin user để kiểm tra role
                var user = await _unitOfWork.Users.GetUserByIdAsync(staffUserId);
                if (user == null)
                {
                    return Unauthorized(new ErrorResponse("User not found."));
                }

                // Nếu là Staff (RoleID = 3), chỉ có thể gửi duyệt vocabulary list của chính họ
                if (user.RoleId == 3 && vocabularyList.MakeBy != staffUserId)
                {
                    return Forbid("You can only submit your own vocabulary lists for approval.");
                }

                var ok = await _vocabularyListService.RequestApprovalAsync(id, staffUserId);
                if (!ok)
                {
                    return NotFound(new ErrorResponse($"Vocabulary list with ID {id} not found or cannot be submitted for approval."));
                }
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while requesting approval for vocabulary list with ID {Id}.", id);
                return StatusCode(500, new ErrorResponse("An internal server error occurred."));
            }
        }

        // POST api/vocabulary-lists/{id}/review
        [HttpPost("{id}/review")]
        [Authorize(Roles = "Manager")] // Chỉ Manager mới có quyền này
        public async Task<IActionResult> ReviewList(int id, [FromBody] VocabularyListReviewRequest request)
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
                if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out var managerUserId))
                {
                    return Unauthorized(new ErrorResponse("Invalid token."));
                }

                var ok = await _vocabularyListService.ReviewListAsync(id, request.IsApproved, request.Comment, managerUserId);
                if (!ok)
                {
                    return NotFound(new ErrorResponse($"Vocabulary list with ID {id} not found or is not pending review."));
                }
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while reviewing vocabulary list with ID {Id}.", id);
                return StatusCode(500, new ErrorResponse("An internal server error occurred."));
            }
        }

        // POST api/vocabulary-lists/{id}/send-back
        [HttpPost("{id}/send-back")]
        [Authorize(Roles = "Manager")] // Chỉ Manager mới có quyền này
        public async Task<IActionResult> SendBackToStaff(int id)
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
                if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out var managerUserId))
                {
                    return Unauthorized(new ErrorResponse("Invalid token."));
                }

                var vocabularyList = await _unitOfWork.VocabularyLists.FindByIdAsync(id);
                if (vocabularyList == null)
                {
                    return NotFound(new ErrorResponse($"Vocabulary list with ID {id} not found."));
                }

                if (vocabularyList.Status != "Rejected")
                {
                    return BadRequest(new ErrorResponse("Can only send back rejected vocabulary lists."));
                }

                // Chuyển status từ Rejected về Draft để staff có thể chỉnh sửa
                vocabularyList.Status = "Draft";
                vocabularyList.UpdatedBy = managerUserId;
                vocabularyList.UpdateAt = DateTime.UtcNow;

                await _unitOfWork.VocabularyLists.UpdateAsync(vocabularyList);
                await _unitOfWork.CompleteAsync();

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while sending back vocabulary list with ID {Id}.", id);
                return StatusCode(500, new ErrorResponse("An internal server error occurred."));
            }
        }

        // GET api/vocabulary-lists/public - Lấy vocabulary lists đã được duyệt cho trang Flashcards
        [HttpGet("public")]
        [AllowAnonymous] // Cho phép truy cập không cần đăng nhập
        public async Task<IActionResult> GetPublicVocabularyLists([FromQuery] string? searchTerm)
        {
            try
            {
                _logger.LogInformation("Getting public vocabulary lists for flashcards page");
                
                // Lấy tất cả vocabulary lists có status = "Published" và IsPublic = true
                var publishedLists = await _vocabularyListService.GetPublishedListsAsync(searchTerm);
                
                _logger.LogInformation("Found {Count} published vocabulary lists", publishedLists.Count());
                
                return Ok(publishedLists);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while fetching public vocabulary lists.");
                return StatusCode(500, new ErrorResponse("An internal server error occurred."));
            }
        }
    }

    public class VocabularyListReviewRequest
    {
        public bool IsApproved { get; set; }
        public string? Comment { get; set; }
    }
}