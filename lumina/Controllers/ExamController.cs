using Microsoft.AspNetCore.Mvc;
using ServiceLayer.Exam;
using System.Collections.Generic;
using System.Threading.Tasks;
using DataLayer.DTOs;

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
        [HttpGet]
        public async Task<ActionResult<List<ExamDTO>>> GetAllExams()
        {
            var exams = await _examService.GetAllExams();
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
    }
}
