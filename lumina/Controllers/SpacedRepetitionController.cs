using DataLayer.DTOs.Vocabulary;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ServiceLayer.Vocabulary;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace lumina.Controllers
{
    [ApiController]
    [Route("api/spaced-repetition")]
    [Authorize]
    public class SpacedRepetitionController : ControllerBase
    {
        private readonly ISpacedRepetitionService _spacedRepetitionService;

        public SpacedRepetitionController(ISpacedRepetitionService spacedRepetitionService)
        {
            _spacedRepetitionService = spacedRepetitionService;
        }

        // GET api/spaced-repetition/due
        [HttpGet("due")]
        public async Task<IActionResult> GetDueForReview()
        {
            var userId = GetCurrentUserId();
            if (userId == null)
            {
                return Unauthorized(new { message = "User not authenticated" });
            }

            var dueItems = await _spacedRepetitionService.GetDueForReviewAsync(userId.Value);
            return Ok(dueItems);
        }

        // GET api/spaced-repetition/all
        [HttpGet("all")]
        public async Task<IActionResult> GetAllRepetitions()
        {
            var userId = GetCurrentUserId();
            if (userId == null)
            {
                return Unauthorized(new { message = "User not authenticated" });
            }

            var items = await _spacedRepetitionService.GetUserRepetitionsAsync(userId.Value);
            return Ok(items);
        }

        // POST api/spaced-repetition/review
        [HttpPost("review")]
        public async Task<IActionResult> ReviewVocabulary([FromBody] ReviewVocabularyRequestDTO request)
        {
            var userId = GetCurrentUserId();
            if (userId == null)
            {
                return Unauthorized(new { message = "User not authenticated" });
            }

            if (request.Quality < 0 || request.Quality > 5)
            {
                return BadRequest(new { message = "Quality phải từ 0 đến 5" });
            }

            var result = await _spacedRepetitionService.ReviewVocabularyAsync(userId.Value, request);
            
            if (!result.Success)
            {
                return NotFound(new { message = result.Message });
            }

            return Ok(result);
        }

        // GET api/spaced-repetition/by-list/{listId}
        [HttpGet("by-list/{listId}")]
        public async Task<IActionResult> GetByList(int listId)
        {
            var userId = GetCurrentUserId();
            if (userId == null)
            {
                return Unauthorized(new { message = "User not authenticated" });
            }

            var item = await _spacedRepetitionService.GetByUserAndListAsync(userId.Value, listId);
            
            if (item == null)
            {
                return NotFound(new { message = "Không tìm thấy bản ghi lặp lại" });
            }

            return Ok(item);
        }

        // POST api/spaced-repetition/create/{listId}
        [HttpPost("create/{listId}")]
        public async Task<IActionResult> CreateRepetition(int listId)
        {
            var userId = GetCurrentUserId();
            if (userId == null)
            {
                return Unauthorized(new { message = "User not authenticated" });
            }

            try
            {
                var result = await _spacedRepetitionService.CreateRepetitionAsync(userId.Value, listId);
                return Ok(result);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }
        }

        private int? GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out var userId))
            {
                return null;
            }
            return userId;
        }
    }
}

