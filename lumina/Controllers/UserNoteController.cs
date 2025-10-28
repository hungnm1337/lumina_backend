using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using ServiceLayer.UserNote;

namespace lumina.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UserNoteController : ControllerBase
    {
        private readonly IUserNoteService _userNoteService;

        public UserNoteController(IUserNoteService userNoteService)
        {
            _userNoteService = userNoteService;
        }

        [HttpPost("upsert")]
        public async Task<IActionResult> UpsertUserNote([FromBody] DataLayer.DTOs.UserNote.UserNoteRequestDTO userNoteRequestDTO)
        {
            try
            {
                bool result = await _userNoteService.UpsertUserNote(userNoteRequestDTO);

                if (result)
                {
                    return Ok(new { Message = "User note upserted successfully." });
                }
                else
                {
                    return BadRequest(new { Message = "Could not save the user note. The item may not exist or data is invalid." });
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = "An internal server error occurred." });
            }
        }

        [HttpGet("note/{userNoteId}")]
        public async Task<IActionResult> GetUserNoteByID(int userNoteId)
        {
            try
            {
                var userNote = await _userNoteService.GetUserNoteByID(userNoteId);
                if (userNote != null)
                {
                    return Ok(userNote);
                }
                else
                {
                    return NotFound(new { Message = "User note not found." });
                }
            }
            catch
            {
                return StatusCode(500, new { Message = "An error occurred while retrieving the user note." });
            }
        }

        [HttpGet("user/{userId}/notes")]
        public async Task<IActionResult> GetAllUserNotesByUserId(int userId)
        {
            try
            {
                var userNotes = await _userNoteService.GetAllUserNotesByUserId(userId);
                return Ok(userNotes);
            }
            catch
            {
                return StatusCode(500, new { Message = "An error occurred while retrieving user notes." });
            }
        }
    }
}
