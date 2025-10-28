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
            try
            {
                Console.WriteLine($"[SmartChat] Received: {request.UserRequest}");

                // Bước 1: Phát hiện intent
                var intent = await _aiService.DetectIntentAsync(request.UserRequest);
                Console.WriteLine($"[SmartChat] Intent: IsExam={intent.IsExamRequest}, Reason={intent.Explanation}");

                if (intent.IsExamRequest)
                {
                    // ===== XỬ LÝ TẠO ĐỀ THI =====
                    var (partNumber, quantity, topic) = await _aiService.ParseUserRequestAsync(request.UserRequest);
                    var aiExamDto = await _aiService.GenerateExamAsync(partNumber, quantity, topic);
                    var saveDto = _mapper.MapAIGeneratedToCreatePrompts(aiExamDto);
                    Console.WriteLine("Test" + saveDto);

                    return Ok(new
                    {
                        type = "exam",
                        message = $"✅ Đã tạo {aiExamDto.Prompts.Count} prompts với {saveDto.Sum(p => p.Questions.Count)} câu hỏi về {topic ?? "chủ đề chung"}",
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
                else
                {
                    // ===== XỬ LÝ CHAT TỰ DO =====
                    var response = await _aiService.GeneralChatAsync(request.UserRequest);

                    return Ok(new
                    {
                        type = "chat",
                        message = response
                    });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[SmartChat] Error: {ex.Message}");
                return BadRequest(new
                {
                    success = false,
                    message = ex.Message,
                    detail = ex.InnerException?.Message
                });
            }
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
