using Microsoft.AspNetCore.Mvc;
using ServiceLayer.Exam;
using System.Collections.Generic;
using System.Threading.Tasks;
using DataLayer.DTOs.Exam;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace lumina.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ExamController : ControllerBase
    {
        private readonly IExamService _examService;

        public ExamController(IExamService examService)
        {
            _examService = examService;
        }

        [HttpGet]
        public async Task<ActionResult<List<ExamDTO>>> GetAllExams(
            [FromQuery] string? examType = null, 
            [FromQuery] string? partCode = null)
        {
            var exams = await _examService.GetAllExams(examType, partCode);
            return Ok(exams);
        }

        [HttpGet("{examId:int}")]
        public async Task<ActionResult<ExamDTO>> GetExamDetailAndPart(int examId)
        {
            var exam = await _examService.GetExamDetailAndExamPartByExamID(examId);
            if (exam == null)
            {
                return NotFound($"Exam with ID {examId} not found.");
            }
            return Ok(exam);
        }

        [HttpGet("part/{partId:int}")]
        public async Task<ActionResult<ExamPartDTO>> GetExamPartDetailAndQuestion(int partId)
        {
            var part = await _examService.GetExamPartDetailAndQuestionByExamPartID(partId);
            if (part == null)
            {
                return NotFound($"Exam part with ID {partId} not found.");
            }
            return Ok(part);
        }

        [HttpPost("CreateExam")]
        [Authorize(Roles = "Staff")]
        public async Task<IActionResult> CreateExams(
            [FromQuery] string toExamSetKey)
        {
            const string fromExamSetKey = "10-2025";

            var userIdClaim = User.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier");
            if (userIdClaim == null)
                return Unauthorized(new { message = "Không tìm thấy thông tin người dùng trong token" });

            var userId = int.Parse(userIdClaim.Value);

            var result = await _examService.CreateExamFormatAsync(fromExamSetKey, toExamSetKey, userId);

            if (result)
                return Ok(new { message = "Tạo bài Exam thành công!" });
            else
                return BadRequest(new { message = $"Bài Exams tháng {toExamSetKey} đã tồn tại rồi" });
        }

        [HttpGet("all-with-parts")]
        public async Task<IActionResult> GetAllExamsWithParts()
        {
            var data = await _examService.GetExamsGroupedBySetKeyAsync();
            return Ok(data);
        }

        [HttpPost("toggle-status")]
        public async Task<IActionResult> ToggleExamStatus(int examId)
        {
            var success = await _examService.ToggleExamStatusAsync(examId);
            if (!success)
                return BadRequest(new { message = "Không đủ câu hỏi. Không thể mở khóa bài thi!" });
            return Ok(new { message = "Đổi trạng thái thành công." });
        }

        [HttpGet("completion-status")]
        [Authorize]
        public async Task<ActionResult<List<ExamCompletionStatusDTO>>> GetUserExamCompletionStatuses()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null)
                return Unauthorized(new { message = "Không tìm thấy thông tin người dùng" });

            var userId = int.Parse(userIdClaim.Value);
            var statuses = await _examService.GetUserExamCompletionStatusesAsync(userId);
            return Ok(statuses);
        }

        [HttpGet("{examId:int}/completion-status")]
        [Authorize]
        public async Task<ActionResult<ExamCompletionStatusDTO>> GetExamCompletionStatus(int examId)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null)
                return Unauthorized(new { message = "Không tìm thấy thông tin người dùng" });

            var userId = int.Parse(userIdClaim.Value);
            var status = await _examService.GetExamCompletionStatusAsync(userId, examId);
            return Ok(status);
        }

        [HttpGet("{examId:int}/parts/completion-status")]
        [Authorize]
        public async Task<ActionResult<List<PartCompletionStatusDTO>>> GetPartCompletionStatus(int examId)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null)
                return Unauthorized(new { message = "Không tìm thấy thông tin người dùng" });

            var userId = int.Parse(userIdClaim.Value);
            var statuses = await _examService.GetPartCompletionStatusAsync(userId, examId);
            return Ok(statuses);
        }
    }
}
