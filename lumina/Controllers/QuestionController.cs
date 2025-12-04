using CloudinaryDotNet.Actions;
using DataLayer.DTOs.Prompt;
using DataLayer.DTOs.Questions;
using Microsoft.AspNetCore.Authorization;
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
        [Authorize(Roles = "Staff")]
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
                return BadRequest(new { message = "Chưa chọn file." });

            try
            {
                Console.WriteLine($"[UploadExcel] Starting import for partId={partId}, file={file.FileName}");
                await _importService.ImportQuestionsFromExcelAsync(file, partId);
                Console.WriteLine($"[UploadExcel] Import completed successfully");
                return Ok(new { message = "Import câu hỏi thành công!" });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[UploadExcel] Error: {ex.Message}");
                Console.WriteLine($"[UploadExcel] StackTrace: {ex.StackTrace}");
                return StatusCode(500, new { message = $"Lỗi import câu hỏi: {ex.Message}" });
            }
        }

        [HttpGet("prompt-question-tree-paged")]
        [Authorize(Roles = "Staff")]
        public async Task<IActionResult> GetQuestionByPage(
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
        [Authorize(Roles = "Staff")]
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


        [HttpPost("save-prompts")]
        public async Task<IActionResult> SavePromptsWithQuestionsAndOptions([FromBody] SaveBulkPromptRequest req)
        {
            if (req?.Prompts == null || !req.Prompts.Any())
                return BadRequest("Không có dữ liệu Prompt nào để lưu!");

            if (req.PartId <= 0)
                return BadRequest("PartId không hợp lệ!");

            try
            {
                var newPromptIds = await _questionService.SavePromptsWithQuestionsAndOptionsAsync(req.Prompts, req.PartId);
                return Ok(new
                {
                    success = true,
                    message = $"Đã lưu {newPromptIds.Count} prompt/câu hỏi vào Part {req.PartId}.",
                    promptIds = newPromptIds
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Lỗi khi lưu dữ liệu: {ex.Message}");
            }
        }

        [HttpGet("check-available-slots")]
        public async Task<IActionResult> CheckAvailableSlots([FromQuery] int partId, [FromQuery] int count)
        {
            try
            {
                var available = await _questionService.GetAvailableSlots(partId, count);
                return Ok(new { available, canAdd = true });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message, canAdd = false });
            }
        }

        [HttpDelete("prompt/{promptId}")]
        [Authorize(Roles = "Staff")]
        public async Task<IActionResult> DeletePrompt(int promptId)
        {
            try
            {
                var result = await _questionService.DeletePromptAsync(promptId);
                
                if (!result)
                {
                    return NotFound(new { message = "Không tìm thấy prompt." });
                }

                return Ok(new { message = "Xóa prompt thành công." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
    }
}
