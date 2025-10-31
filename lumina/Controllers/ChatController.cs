using DataLayer.DTOs.Chat;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ServiceLayer.Chat;
using System.Security.Claims;

namespace lumina.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class ChatController : ControllerBase
    {
        private readonly IChatService _chatService;

        public ChatController(IChatService chatService)
        {
            _chatService = chatService;
        }

        [HttpPost("ask")]
        public async Task<ActionResult<ChatResponseDTO>> AskQuestion([FromBody] ChatRequestDTO request)
        {
            if (request == null || string.IsNullOrEmpty(request.Message))
            {
                return BadRequest("Invalid request data.");
            }

            try
            {
                // Lấy UserId từ token
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
                if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out var currentUserId))
                {
                    return Unauthorized("Invalid token - User ID could not be determined.");
                }

                request.UserId = currentUserId;
                var response = await _chatService.ProcessMessage(request);
                return Ok(response);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        [HttpPost("save-vocabularies")]
        public async Task<ActionResult<SaveVocabularyResponseDTO>> SaveVocabularies([FromBody] SaveVocabularyRequestDTO request)
        {
            if (request == null || string.IsNullOrEmpty(request.FolderName) || request.Vocabularies == null || !request.Vocabularies.Any())
            {
                return BadRequest("Invalid request data.");
            }

            try
            {
                // Lấy UserId từ token
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
                if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out var currentUserId))
                {
                    return Unauthorized("Invalid token - User ID could not be determined.");
                }

                request.UserId = currentUserId;
                var response = await _chatService.SaveGeneratedVocabularies(request);
                return Ok(response);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }
    }
}
