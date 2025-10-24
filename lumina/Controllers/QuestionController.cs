using DataLayer.DTOs.Passage;
using DataLayer.DTOs.Prompt;
using DataLayer.DTOs.Questions;
using Microsoft.AspNetCore.Mvc;
using ServiceLayer.Import;
using ServiceLayer.Questions;

namespace lumina.Controllers
{
    
    [ApiController]
    [Route("api/[controller]")]
    public class QuestionController : ControllerBase
    {

        private readonly IQuestionService _questionService;

        private readonly IImportService _importService;

        public QuestionController(IQuestionService questionService, IImportService importService)
        {
            _questionService = questionService;
            _importService = importService;
        }


        [HttpPost("prompt-with-questions")]
        public async Task<IActionResult> CreatePromptWithQuestions([FromBody] CreatePromptWithQuestionsDTO dto)
        {
            if (dto == null)
                return BadRequest("Dữ liệu không hợp lệ.");

            try
            {
                var promptId = await _questionService.CreatePromptWithQuestionsAsync(dto);
                return Ok(new { PromptId = promptId });
            }
            catch (Exception ex)
            {
                // Kiểm tra thông điệp lỗi để trả về status code phù hợp
                if (ex.Message.Contains("đã đủ") || ex.Message.Contains("Không còn slot"))
                {
                    // Trả về BadRequest và thông báo rõ hơn cho user
                    return BadRequest(new { error = ex.Message });
                }

                // Ngoài ra trả lỗi 500
                return StatusCode(500, $"Lỗi khi tạo prompt và câu hỏi: {ex.Message}");
            }
        }


        [HttpPost("upload-excel")]
        public async Task<IActionResult> UploadExcel(IFormFile file, [FromQuery] int partId)
        {
            if (file == null || file.Length == 0)
                return BadRequest("Chưa chọn file.");

            try
            {
                await _importService.ImportQuestionsFromExcelAsync(file, partId);
                return Ok("Import câu hỏi thành công!");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Lỗi import câu hỏi: {ex.Message}");
            }
        }

        [HttpGet("passage-question-tree-paged")]
        public async Task<IActionResult> GetPaged(
    [FromQuery] int page = 1,
    [FromQuery] int size = 10,
    [FromQuery] int? partId = null)
        {
            var (items, totalPages) = await _questionService.GetPromptsPagedAsync(page, size, partId);
            return Ok(new
            {
                Items = items,
                TotalPages = totalPages
            });
        }

        [HttpPut("edit-passage")]
        public async Task<IActionResult> EditPassage([FromBody] PromptEditDto dto)
        {
            if (dto == null || dto.PromptId <= 0)
                return BadRequest("Dữ liệu không hợp lệ");

            var result = await _questionService.EditPromptWithQuestionsAsync(dto);

            if (!result)
                return NotFound("Prompt không tồn tại");

            return Ok(new { message = "Cập nhật thành công" });
        }


        [HttpPost("add-question")]
        public async Task<IActionResult> Add([FromBody] QuestionCrudDto dto)
        {
            var id = await _questionService.AddQuestionAsync(dto);
            return Ok(new { questionId = id });
        }

        [HttpPut("edit-question")]
        public async Task<IActionResult> Update([FromBody] QuestionCrudDto dto)
        {
            var ok = await _questionService.UpdateQuestionAsync(dto);
            if (!ok) return NotFound("Không tồn tại question!");
            return Ok(new { message = "Đã cập nhật!" });
        }

        [HttpDelete("delete-question/{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var ok = await _questionService.DeleteQuestionAsync(id);
                if (!ok)
                    return NotFound(new { message = "Không tồn tại question!" });
                return Ok(new { message = "Đã xóa!" });
            }
            catch (Exception ex)
            {
                // Trả về JSON message cho FE dễ xử lý
                return BadRequest(new { message = ex.Message });
            }
        }



        [HttpGet("statistics")]
        public async Task<IActionResult> GetStatistics()
        {
            var data = await _questionService.GetStatisticsAsync();
            return Ok(data);
        }


    }
}
