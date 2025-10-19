using Microsoft.AspNetCore.Mvc;
using ServiceLayer.Exam;
using System.Collections.Generic;
using System.Threading.Tasks;
using DataLayer.DTOs.Exam;
using Microsoft.AspNetCore.Authorization;

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

        // GET: api/exam
        // Lấy danh sách tất cả các bài thi (không bao gồm các phần Part)
        // Có thể filter theo examType và partCode
        [HttpGet]
        public async Task<ActionResult<List<ExamDTO>>> GetAllExams(
            [FromQuery] string? examType = null, 
            [FromQuery] string? partCode = null)
        {
            var exams = await _examService.GetAllExams(examType, partCode);
            return Ok(exams);
        }

        // GET: api/exam/{examId}
        // Lấy chi tiết bài thi theo examId bao gồm các phần Part bên trong
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

        // GET: api/exam/part/{partId}
        // Lấy thông tin chi tiết part cùng các câu hỏi của part
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
        [Authorize]
        public async Task<IActionResult> CloneFormat(
    [FromQuery] string fromExamSetKey,
    [FromQuery] string toExamSetKey)
        {
            // lấy user id từ token
            var userIdClaim = User.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier");
            if (userIdClaim == null)
                return Unauthorized("Không tìm thấy thông tin người dùng trong token");

            var userId = int.Parse(userIdClaim.Value);

            var result = await _examService.CreateExamFormatAsync(fromExamSetKey, toExamSetKey, userId);

            if (result)
                return Ok(new { message = "Clone thành công!" });
            else
                return NotFound("ExamSetKey nguồn không tồn tại hoặc không có dữ liệu.");
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

    }
}
