using DataLayer.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RepositoryLayer.UnitOfWork;
using ServiceLayer.TextToSpeech; 
using System.Security.Claims;
using DataLayer.DTOs.Auth;

namespace lumina.Controllers;

[ApiController]
[Route("api/vocabularies")]
public class VocabulariesController : ControllerBase
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ITextToSpeechService _ttsService; 

    public VocabulariesController(IUnitOfWork unitOfWork, ITextToSpeechService ttsService)
    {
        _unitOfWork = unitOfWork;
        _ttsService = ttsService; 
    }

  
    [HttpGet]
    [Authorize(Roles = "Staff")]
    public async Task<IActionResult> GetList([FromQuery] int? listId, [FromQuery] string? search)
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

            if (user.RoleId == 3)
            {
             
                if (listId.HasValue)
                {
                    var vocabularyList = await _unitOfWork.VocabularyLists.FindByIdAsync(listId.Value);
                    if (vocabularyList == null || vocabularyList.MakeBy != currentUserId)
                    {
                        return Forbid("You can only access your own vocabulary lists.");
                    }
                }
                else
                {
                  
                    var userLists = await _unitOfWork.VocabularyLists.GetByUserAsync(currentUserId, null);
                    var userListIds = userLists.Select(l => l.VocabularyListId).ToList();
                    if (!userListIds.Any())
                    {
                        return Ok(new List<object>());
                    }
              
                }
            }

            var items = await _unitOfWork.Vocabularies.GetByListAsync(listId, search);
            return Ok(items.Select(v => new
            {
                id = v.VocabularyId,
                listId = v.VocabularyListId,
                word = v.Word,
                type = v.TypeOfWord,
                category = v.Category,
                definition = v.Definition,
                example = v.Example,
            }));
        }
        catch (Exception ex)
        {
            return StatusCode(500, new ErrorResponse("An internal server error occurred."));
        }
    }

    public sealed class CreateVocabularyRequest
    {
        public int VocabularyListId { get; set; }
        public string Word { get; set; } = string.Empty;
        public string TypeOfWord { get; set; } = string.Empty; // noun, verb, adj...
        public string? Category { get; set; }
        public string Definition { get; set; } = string.Empty;
        public string? Example { get; set; }
        public bool GenerateAudio { get; set; } = true; // Thêm option để tạo audio
    }

    // POST api/vocabularies
    [HttpPost]
    [Authorize] // Cho phép cả Student và Staff
    public async Task<IActionResult> Create([FromBody] CreateVocabularyRequest req)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out var currentUserId))
            {
                return Unauthorized(new ErrorResponse("Invalid token - User ID could not be determined."));
            }

            // Kiểm tra vocabulary list có tồn tại không
            var vocabularyList = await _unitOfWork.VocabularyLists.FindByIdAsync(req.VocabularyListId);
            if (vocabularyList == null)
            {
                return NotFound(new ErrorResponse("Vocabulary list not found."));
            }

            string? audioUrl = null;

            // Tạo audio nếu được yêu cầu
            if (req.GenerateAudio && !string.IsNullOrEmpty(req.Word))
            {
                try
                {
                    var audioResult = await _ttsService.GenerateAudioAsync(req.Word);
                    audioUrl = audioResult.Url;
                }
                catch (Exception ex)
                {
                    // Log lỗi nhưng vẫn tạo vocabulary
                    Console.WriteLine($"Lỗi tạo audio: {ex.Message}");
                }
            }

            // Kiểm tra user hiện tại
            var user = await _unitOfWork.Users.GetUserByIdAsync(currentUserId);
            if (user == null)
            {
                return Unauthorized(new ErrorResponse("User not found."));
            }

            bool isStaff = user.RoleId == 3; // RoleId 3 = Staff
            var wasPublished = !string.IsNullOrEmpty(vocabularyList.Status) && 
                              vocabularyList.Status.Trim().Equals("Published", StringComparison.OrdinalIgnoreCase);

            // Nếu là Staff và list đã được published, chuyển về pending để manager duyệt lại
            if (isStaff && wasPublished)
            {
                vocabularyList.Status = "Pending";
                vocabularyList.IsPublic = false;
                vocabularyList.UpdateAt = DateTime.UtcNow;
                await _unitOfWork.VocabularyLists.UpdateAsync(vocabularyList);
                // Đảm bảo thay đổi được lưu trước khi tiếp tục
                await _unitOfWork.CompleteAsync();
            }

            var vocab = new Vocabulary
            {
                VocabularyListId = req.VocabularyListId,
                Word = req.Word,
                TypeOfWord = req.TypeOfWord,
                Category = req.Category,
                Definition = req.Definition,
                Example = req.Example,
               
                IsDeleted = false
            };
            await _unitOfWork.Vocabularies.AddAsync(vocab);
            await _unitOfWork.CompleteAsync();

            return CreatedAtAction(nameof(GetList), new { listId = req.VocabularyListId }, new { 
                id = vocab.VocabularyId,
                audioUrl = audioUrl,
                statusChanged = isStaff && wasPublished // Thông báo cho frontend biết status đã thay đổi
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new ErrorResponse("An internal server error occurred."));
        }
    }

    // Thêm endpoint để tạo audio cho từ vựng hiện có
    [HttpPost("{id}/generate-audio")]
    [Authorize(Roles = "Staff")]
    public async Task<IActionResult> GenerateAudio(int id)
    {
        try
        {
            var vocab = await _unitOfWork.Vocabularies.GetByIdAsync(id);
            if (vocab == null)
            {
                return NotFound(new { message = "Vocabulary not found" });
            }

            // Kiểm tra từ vựng có rỗng không
            if (string.IsNullOrWhiteSpace(vocab.Word))
            {
                return BadRequest(new { message = "Vocabulary word is empty" });
            }

            // Tạo audio trực tiếp
            var audioResult = await _ttsService.GenerateAudioAsync(vocab.Word);
         
            
            await _unitOfWork.Vocabularies.UpdateAsync(vocab);
            await _unitOfWork.CompleteAsync();

            return Ok(new { 
                message = "Audio generated successfully",
                audioUrl = audioResult.Url
            });
        }
        catch (Exception ex)
        {
            // Log chi tiết lỗi
            Console.WriteLine($"Error in GenerateAudio endpoint: {ex}");
            return StatusCode(500, new { message = $"Lỗi tạo audio: {ex.Message}" });
        }
    }

    // GET api/vocabularies/{id}
    [HttpGet("{id}")]
    [Authorize(Roles = "Staff")]
    public async Task<IActionResult> GetById(int id)
    {
        var vocab = await _unitOfWork.Vocabularies.GetByIdAsync(id);
        if (vocab == null)
        {
            return NotFound(new { message = "Vocabulary not found" });
        }

        return Ok(new
        {
            id = vocab.VocabularyId,
            listId = vocab.VocabularyListId,
            word = vocab.Word,
            type = vocab.TypeOfWord,
            category = vocab.Category,
            definition = vocab.Definition,
            example = vocab.Example
        });
    }

    // PUT api/vocabularies/{id}
    [HttpPut("{id}")]
    [Authorize] // Cho phép cả Student và Staff
    public async Task<IActionResult> Update(int id, [FromBody] UpdateVocabularyRequest req)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out var currentUserId))
            {
                return Unauthorized(new ErrorResponse("Invalid token - User ID could not be determined."));
            }

            var vocab = await _unitOfWork.Vocabularies.GetByIdAsync(id);
            if (vocab == null)
            {
                return NotFound(new { message = "Vocabulary not found" });
            }

            // Kiểm tra vocabulary list
            var vocabularyList = await _unitOfWork.VocabularyLists.FindByIdAsync(vocab.VocabularyListId);
            if (vocabularyList == null)
            {
                return NotFound(new ErrorResponse("Vocabulary list not found."));
            }

            // Kiểm tra user hiện tại
            var user = await _unitOfWork.Users.GetUserByIdAsync(currentUserId);
            if (user == null)
            {
                return Unauthorized(new ErrorResponse("User not found."));
            }

            bool isStaff = user.RoleId == 3; // RoleId 3 = Staff
            var wasPublished = !string.IsNullOrEmpty(vocabularyList.Status) && 
                              vocabularyList.Status.Trim().Equals("Published", StringComparison.OrdinalIgnoreCase);

            // Nếu là Staff và list đã được published, chuyển về pending để manager duyệt lại
            if (isStaff && wasPublished)
            {
                vocabularyList.Status = "Pending";
                vocabularyList.IsPublic = false;
                vocabularyList.UpdateAt = DateTime.UtcNow;
                await _unitOfWork.VocabularyLists.UpdateAsync(vocabularyList);
                // Đảm bảo thay đổi được lưu trước khi tiếp tục
                await _unitOfWork.CompleteAsync();
            }

            vocab.Word = req.Word;
            vocab.TypeOfWord = req.TypeOfWord;
            vocab.Category = req.Category;
            vocab.Definition = req.Definition;
            vocab.Example = req.Example;

            await _unitOfWork.Vocabularies.UpdateAsync(vocab);
            await _unitOfWork.CompleteAsync();

            return Ok(new { 
                message = "Vocabulary updated successfully",
                statusChanged = isStaff && wasPublished // Thông báo cho frontend biết status đã thay đổi
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new ErrorResponse("An internal server error occurred."));
        }
    }

    // DELETE api/vocabularies/{id}
    [HttpDelete("{id}")]
    [Authorize] // Cho phép cả Student và Staff
    public async Task<IActionResult> Delete(int id)
    {
        try
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out var currentUserId))
            {
                return Unauthorized(new ErrorResponse("Invalid token - User ID could not be determined."));
            }

            var vocab = await _unitOfWork.Vocabularies.GetByIdAsync(id);
            if (vocab == null)
            {
                return NotFound(new { message = "Vocabulary not found" });
            }

            await _unitOfWork.Vocabularies.DeleteAsync(id);
            await _unitOfWork.CompleteAsync();

            return Ok(new { message = "Vocabulary deleted successfully" });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new ErrorResponse("An internal server error occurred."));
        }
    }

    // GET api/vocabularies/search
    [HttpGet("search")]
    [Authorize(Roles = "Staff")]
    public async Task<IActionResult> Search([FromQuery] string term, [FromQuery] int? listId)
    {
        if (string.IsNullOrWhiteSpace(term))
        {
            return BadRequest(new { message = "Search term is required" });
        }

        var results = await _unitOfWork.Vocabularies.SearchAsync(term, listId);
        return Ok(results.Select(v => new
        {
            id = v.VocabularyId,
            listId = v.VocabularyListId,
            word = v.Word,
            type = v.TypeOfWord,
            category = v.Category,
            definition = v.Definition,
            example = v.Example
        }));
    }

    // GET api/vocabularies/by-type/{type}
    [HttpGet("by-type/{type}")]
    [Authorize(Roles = "Staff")]
    public async Task<IActionResult> GetByType(string type)
    {
        var results = await _unitOfWork.Vocabularies.GetByTypeAsync(type);
        return Ok(results.Select(v => new
        {
            id = v.VocabularyId,
            listId = v.VocabularyListId,
            word = v.Word,
            type = v.TypeOfWord,
            category = v.Category,
            definition = v.Definition,
            example = v.Example
        }));
    }

    // GET api/vocabularies/by-category/{category}
    [HttpGet("by-category/{category}")]
    [Authorize(Roles = "Staff")]
    public async Task<IActionResult> GetByCategory(string category)
    {
        var results = await _unitOfWork.Vocabularies.GetByCategoryAsync(category);
        return Ok(results.Select(v => new
        {
            id = v.VocabularyId,
            listId = v.VocabularyListId,
            word = v.Word,
            type = v.TypeOfWord,
            category = v.Category,
            definition = v.Definition,
            example = v.Example
        }));
    }

    // GET api/vocabularies/categories
    [HttpGet("categories")]
    [Authorize(Roles = "Staff")]
    public async Task<IActionResult> GetCategories()
    {
        var categories = await _unitOfWork.Vocabularies.GetDistinctCategoriesAsync();
        return Ok(categories);
    }

    // GET api/vocabularies/stats
    [HttpGet("stats")]
    [Authorize(Roles = "Staff")]
    public async Task<IActionResult> GetStats()
    {
        var counts = await _unitOfWork.Vocabularies.GetCountsByListAsync();
        var totalCount = await _unitOfWork.Vocabularies.GetTotalCountAsync();
        
        return Ok(new
        {
            totalCount = totalCount,
            countsByList = counts.Select(kv => new { listId = kv.Key, total = kv.Value })
        });
    }

    // GET api/vocabularies/student-list - Endpoint cho Student và Staff để lấy vocabulary
    [HttpGet("student-list")]
    [Authorize]
    public async Task<IActionResult> GetStudentList([FromQuery] int? listId, [FromQuery] string? search)
    {
        try
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out var currentUserId))
            {
                return Unauthorized(new ErrorResponse("Invalid token - User ID could not be determined."));
            }

            // Kiểm tra vocabulary list có tồn tại không (nếu có listId)
            if (listId.HasValue)
            {
                var vocabularyList = await _unitOfWork.VocabularyLists.FindByIdAsync(listId.Value);
                if (vocabularyList == null)
                {
                    return NotFound(new ErrorResponse("Vocabulary list not found."));
                }
            }

            var items = await _unitOfWork.Vocabularies.GetByListAsync(listId, search);
            return Ok(items.Select(v => new
            {
                id = v.VocabularyId,
                listId = v.VocabularyListId,
                word = v.Word,
                type = v.TypeOfWord,
                category = v.Category,
                definition = v.Definition,
                example = v.Example,
                //audioUrl = v.AudioUrl
            }));
        }
        catch (Exception ex)
        {
            return StatusCode(500, new ErrorResponse("An internal server error occurred."));
        }
    }

    // GET api/vocabularies/public/{listId} - Endpoint công khai để lấy vocabulary từ list đã được publish
    [HttpGet("public/{listId}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetPublicVocabulary(int listId)
    {
        try
        {
            var vocabularyList = await _unitOfWork.VocabularyLists.FindByIdAsync(listId);
            if (vocabularyList == null || vocabularyList.Status != "Published")
            {
                return NotFound(new ErrorResponse("Vocabulary list not found or not published."));
            }

            var items = await _unitOfWork.Vocabularies.GetByListAsync(listId, null);
            return Ok(items.Select(v => new
            {
                id = v.VocabularyId,
                word = v.Word,
                definition = v.Definition,
                category = v.Category,
                example = v.Example,
                audioUrl = (string?)null // Vocabulary model không có AudioUrl, để null cho frontend xử lý
            }));
        }
        catch (Exception ex)
        {
            return StatusCode(500, new ErrorResponse("An internal server error occurred."));
        }
    }

    public sealed class UpdateVocabularyRequest
    {
        public string Word { get; set; } = string.Empty;
        public string TypeOfWord { get; set; } = string.Empty;
        public string? Category { get; set; }
        public string Definition { get; set; } = string.Empty;
        public string? Example { get; set; }
    }
}







