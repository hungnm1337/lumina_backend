using DataLayer.DTOs.Auth;
using DataLayer.DTOs.Vocabulary;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ServiceLayer.Vocabulary;
using System.Security.Claims;

namespace lumina.Controllers
{
    [ApiController]
    [Route("api/vocabulary-lists")]
    [Authorize(Roles = "Staff")]
    public class VocabularyListsController : ControllerBase
    {
        private readonly IVocabularyListService _vocabularyListService;
        private readonly ILogger<VocabularyListsController> _logger;

        public VocabularyListsController(IVocabularyListService vocabularyListService, ILogger<VocabularyListsController> logger)
        {
            _vocabularyListService = vocabularyListService;
            _logger = logger;
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
                var lists = await _vocabularyListService.GetListsAsync(searchTerm);
                return Ok(lists);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while fetching vocabulary lists.");
                return StatusCode(500, new ErrorResponse("An internal server error occurred."));
            }
        }

        [HttpGet("{id}", Name = "GetListById")]
        public IActionResult GetListById(int id)
        {
            return Ok(new { Message = $"Placeholder for getting list with ID {id}" });
        }
    }
}