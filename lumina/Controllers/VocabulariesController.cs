using DataLayer.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RepositoryLayer.UnitOfWork;
using ServiceLayer.TextToSpeech; // Thêm using
using System.Security.Claims;
using DataLayer.DTOs.Auth;

namespace lumina.Controllers;

[ApiController]
[Route("api/vocabularies")]
public class VocabulariesController : ControllerBase
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ITextToSpeechService _ttsService; // Thêm service

    public VocabulariesController(IUnitOfWork unitOfWork, ITextToSpeechService ttsService)
    {
        _unitOfWork = unitOfWork;
        _ttsService = ttsService; // Inject service
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

            // Nếu là Staff (RoleID = 3), chỉ lấy vocabulary từ lists của chính họ
            if (user.RoleId == 3)
            {
                // Kiểm tra nếu listId được cung cấp, đảm bảo nó thuộc về user hiện tại
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
                    // Nếu không có listId, lấy tất cả vocabulary từ lists của user
                    var userLists = await _unitOfWork.VocabularyLists.GetByUserAsync(currentUserId, null);
                    var userListIds = userLists.Select(l => l.VocabularyListId).ToList();
                    if (!userListIds.Any())
                    {
                        return Ok(new List<object>());
                    }
                    // TODO: Implement filtering by user's list IDs
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
    [Authorize(Roles = "Staff")]
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

            // Lấy thông tin user để kiểm tra role
            var user = await _unitOfWork.Users.GetUserByIdAsync(currentUserId);
            if (user == null)
            {
                return Unauthorized(new ErrorResponse("User not found."));
            }

            // Nếu là Staff (RoleID = 3), kiểm tra vocabulary list có thuộc về họ không
            if (user.RoleId == 3)
            {
                var vocabularyList = await _unitOfWork.VocabularyLists.FindByIdAsync(req.VocabularyListId);
                if (vocabularyList == null || vocabularyList.MakeBy != currentUserId)
                {
                    return Forbid("You can only add vocabulary to your own lists.");
                }
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
                audioUrl = audioUrl
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
    [Authorize(Roles = "Staff")]
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

            // Lấy thông tin user để kiểm tra role
            var user = await _unitOfWork.Users.GetUserByIdAsync(currentUserId);
            if (user == null)
            {
                return Unauthorized(new ErrorResponse("User not found."));
            }

            var vocab = await _unitOfWork.Vocabularies.GetByIdAsync(id);
            if (vocab == null)
            {
                return NotFound(new { message = "Vocabulary not found" });
            }

            // Nếu là Staff (RoleID = 3), kiểm tra vocabulary có thuộc về list của họ không
            if (user.RoleId == 3)
            {
                var vocabularyList = await _unitOfWork.VocabularyLists.FindByIdAsync(vocab.VocabularyListId);
                if (vocabularyList == null || vocabularyList.MakeBy != currentUserId)
                {
                    return Forbid("You can only update vocabulary in your own lists.");
                }
            }

            vocab.Word = req.Word;
            vocab.TypeOfWord = req.TypeOfWord;
            vocab.Category = req.Category;
            vocab.Definition = req.Definition;
            vocab.Example = req.Example;

            await _unitOfWork.Vocabularies.UpdateAsync(vocab);
            await _unitOfWork.CompleteAsync();

            return Ok(new { message = "Vocabulary updated successfully" });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new ErrorResponse("An internal server error occurred."));
        }
    }

    // DELETE api/vocabularies/{id}
    [HttpDelete("{id}")]
    [Authorize(Roles = "Staff")]
    public async Task<IActionResult> Delete(int id)
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

            var vocab = await _unitOfWork.Vocabularies.GetByIdAsync(id);
            if (vocab == null)
            {
                return NotFound(new { message = "Vocabulary not found" });
            }

            // Nếu là Staff (RoleID = 3), kiểm tra vocabulary có thuộc về list của họ không
            if (user.RoleId == 3)
            {
                var vocabularyList = await _unitOfWork.VocabularyLists.FindByIdAsync(vocab.VocabularyListId);
                if (vocabularyList == null || vocabularyList.MakeBy != currentUserId)
                {
                    return Forbid("You can only delete vocabulary from your own lists.");
                }
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

    // GET api/vocabularies/public/{listId} - Lấy vocabulary words từ published list cho Flashcards
    [HttpGet("public/{listId}")]
    [AllowAnonymous] // Cho phép truy cập không cần đăng nhập
    public async Task<IActionResult> GetPublicVocabularyByList(int listId)
    {
        try
        {
            // Kiểm tra vocabulary list có tồn tại và đã được published không
            var vocabularyList = await _unitOfWork.VocabularyLists.FindByIdAsync(listId);
            if (vocabularyList == null || vocabularyList.IsDeleted == true)
            {
                return NotFound(new { message = "Vocabulary list not found" });
            }

            if (vocabularyList.Status != "Published" || vocabularyList.IsPublic != true)
            {
                return NotFound(new { message = "Vocabulary list is not available for public access" });
            }

            // Lấy tất cả vocabulary words từ list này
            var vocabularies = await _unitOfWork.Vocabularies.GetByListAsync(listId, null);
            
            return Ok(vocabularies.Select(v => new
            {
                id = v.VocabularyId,
                word = v.Word,
                type = v.TypeOfWord,
                category = v.Category,
                definition = v.Definition,
                example = v.Example
            }));
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "An internal server error occurred." });
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







