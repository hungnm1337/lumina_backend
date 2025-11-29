using DataLayer.DTOs.AIGeneratedExam;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using ServiceLayer.ExamGenerationAI;
using ServiceLayer.ExamGenerationAI.Mappers;

namespace lumina.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ExamGenerationAIController : ControllerBase
    {
        private readonly IExamGenerationAIService _aiService;
        private readonly IAIExamMapper _mapper;

        public ExamGenerationAIController(IExamGenerationAIService aiService, IAIExamMapper aIExamMapper)
        {
            _aiService = aiService;
            _mapper = aIExamMapper;
        }

       

        [HttpPost("smart-chat")]
        public async Task<IActionResult> SmartChat([FromBody] CreateExamRequest request)
        {
            Console.WriteLine($"[SmartChat] Received: {request.UserRequest}");

            try
            {
                // ===== Bước 1: Phát hiện intent =====
                var intent = await _aiService.DetectIntentAsync(request.UserRequest);
                Console.WriteLine($"[SmartChat] Intent: IsExam={intent.IsExamRequest}, Reason={intent.Explanation}");

                // ===== Bước 2: Nếu là yêu cầu tạo đề thi =====
                if (intent.IsExamRequest)
                {
                    var (partNumber, quantity, topic) = await _aiService.ParseUserRequestAsync(request.UserRequest);
                    var aiExamDto = await _aiService.GenerateExamAsync(partNumber, quantity, topic);
                    var saveDto = _mapper.MapAIGeneratedToCreatePrompts(aiExamDto);

                    return Ok(new
                    {
                        type = "exam",
                        message = $"Đã tạo {aiExamDto.Prompts.Count} prompts với {saveDto.Sum(p => p.Questions.Count)} câu hỏi về {topic ?? "chủ đề chung"}",
                        data = saveDto,
                        examInfo = new
                        {
                            examTitle = aiExamDto.ExamExamTitle,
                            skill = aiExamDto.Skill,
                            partLabel = aiExamDto.PartLabel,
                            promptCount = aiExamDto.Prompts.Count,
                            totalQuestions = saveDto.Sum(p => p.Questions.Count)
                        }
                    });
                }

                // ===== Bước 3: Chat thông thường =====
                var response = await ExecuteWithRetryAsync(() => _aiService.GeneralChatAsync(request.UserRequest));

                return Ok(new
                {
                    type = "chat",
                    message = response
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[SmartChat] Error: {ex}");

                // Nếu Gemini API quá tải hoặc unavailable
                if (ex.Message.Contains("503") || ex.Message.Contains("UNAVAILABLE") || ex.Message.Contains("overloaded"))
                {
                    return StatusCode(503, new
                    {
                        success = false,
                        message = "Gemini API hiện đang quá tải, vui lòng thử lại sau.",
                        detail = ex.Message
                    });
                }

                // Nếu lỗi đến từ request sai định dạng
                if (ex is ArgumentException || ex is FormatException)
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "Yêu cầu không hợp lệ. Vui lòng kiểm tra lại nội dung đầu vào.",
                        detail = ex.Message
                    });
                }

                // Lỗi khác (mặc định 500)
                return StatusCode(500, new
                {
                    success = false,
                    message = "Lỗi hệ thống, vui lòng thử lại sau.",
                    detail = ex.Message
                });
            }
        }


        // === Hàm hỗ trợ retry tự động khi Gemini quá tải ===
        private async Task<T> ExecuteWithRetryAsync<T>(Func<Task<T>> action, int maxRetries = 2, int delayMs = 2000)
        {
            for (int attempt = 0; attempt <= maxRetries; attempt++)
            {
                try
                {
                    return await action();
                }
                catch (Exception ex)
                {
                    if (attempt < maxRetries &&
                        (ex.Message.Contains("503") || ex.Message.Contains("UNAVAILABLE") || ex.Message.Contains("overloaded")))
                    {
                        Console.WriteLine($"[SmartChat] Gemini overloaded, retrying... ({attempt + 1}/{maxRetries})");
                        await Task.Delay(delayMs);
                        continue;
                    }

                    throw;
                }
            }

            throw new Exception("Gemini API vẫn quá tải sau khi thử lại.");
        }



        // Endpoint để Staff gửi yêu cầu tạo đề thi
        [HttpPost("generate-exam")]
        public async Task<IActionResult> GenerateExam([FromBody] CreateExamRequest request)
        {
            try
            {
                var (partNumber, quantity, topic) = await _aiService.ParseUserRequestAsync(request.UserRequest);

                var aiExamDto = await _aiService.GenerateExamAsync(partNumber, quantity, topic);

                Console.WriteLine("aiExamDto " + aiExamDto.Prompts.Count());

                var saveDto = _mapper.MapAIGeneratedToCreatePrompts(aiExamDto);

                Console.WriteLine("saveDto " + saveDto);

                // await _repository.SaveAsync(saveDto); // Gọi repo lưu nếu cần

                return Ok(saveDto);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

    }
}
