using DataLayer.DTOs.AI;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using ServiceLayer.UserNoteAI;
using System;
using System.Threading.Tasks;

namespace lumina.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UserNoteAIChatController : ControllerBase
    {
        private readonly IAIChatService _aiChatService;
        private readonly ILogger<UserNoteAIChatController> _logger;

        public UserNoteAIChatController(IAIChatService aiChatService, ILogger<UserNoteAIChatController> logger)
        {
            _aiChatService = aiChatService;
            _logger = logger;
        }

        /// <summary>
        /// Ask a question about lesson content
        /// </summary>
        /// <param name="request">Chat request with user question and lesson content</param>
        /// <returns>AI response to the question</returns>
        [HttpPost("ask-question")]
        [ProducesResponseType(typeof(ChatResponseDTO), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> AskQuestion([FromBody] ChatRequestDTO request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                if (string.IsNullOrWhiteSpace(request.UserQuestion))
                {
                    return BadRequest(new { Message = "User question cannot be empty." });
                }

                if (string.IsNullOrWhiteSpace(request.LessonContent))
                {
                    return BadRequest(new { Message = "Lesson content cannot be empty." });
                }

                var result = await _aiChatService.AskQuestionAsync(request);

                if (!result.Success)
                {
                    return StatusCode(StatusCodes.Status500InternalServerError, result);
                }

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while processing chat question");
                return StatusCode(StatusCodes.Status500InternalServerError, 
                    new { Message = "An unexpected error occurred while processing your question." });
            }
        }

        /// <summary>
        /// Continue conversation with context
        /// </summary>
        /// <param name="request">Chat request with conversation history</param>
        /// <returns>AI response with updated conversation history</returns>
        [HttpPost("continue-conversation")]
        [ProducesResponseType(typeof(ChatConversationResponseDTO), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> ContinueConversation([FromBody] ChatRequestDTO request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                if (string.IsNullOrWhiteSpace(request.UserQuestion))
                {
                    return BadRequest(new { Message = "User question cannot be empty." });
                }

                if (string.IsNullOrWhiteSpace(request.LessonContent))
                {
                    return BadRequest(new { Message = "Lesson content cannot be empty." });
                }

                var result = await _aiChatService.ContinueConversationAsync(request);

                if (!result.CurrentResponse.Success)
                {
                    return StatusCode(StatusCodes.Status500InternalServerError, result);
                }

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while continuing conversation");
                return StatusCode(StatusCodes.Status500InternalServerError, 
                    new { Message = "An unexpected error occurred while continuing the conversation." });
            }
        }

        /// <summary>
        /// Generate suggested questions based on lesson content
        /// </summary>
        /// <param name="request">Request with lesson content and optional title</param>
        /// <returns>List of suggested questions</returns>
        [HttpPost("suggested-questions")]
        [ProducesResponseType(typeof(ChatResponseDTO), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetSuggestedQuestions([FromBody] LessonContentRequestDTO request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                if (string.IsNullOrWhiteSpace(request.LessonContent))
                {
                    return BadRequest(new { Message = "Lesson content cannot be empty." });
                }

                var result = await _aiChatService.GenerateSuggestedQuestionsAsync(
                    request.LessonContent, 
                    request.LessonTitle);

                if (!result.Success)
                {
                    return StatusCode(StatusCodes.Status500InternalServerError, result);
                }

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while generating suggested questions");
                return StatusCode(StatusCodes.Status500InternalServerError, 
                    new { Message = "An unexpected error occurred while generating suggested questions." });
            }
        }

        /// <summary>
        /// Get detailed explanation of a concept
        /// </summary>
        /// <param name="request">Request with concept and lesson context</param>
        /// <returns>Detailed explanation of the concept</returns>
        [HttpPost("explain-concept")]
        [ProducesResponseType(typeof(ChatResponseDTO), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> ExplainConcept([FromBody] ConceptExplanationRequestDTO request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                if (string.IsNullOrWhiteSpace(request.Concept))
                {
                    return BadRequest(new { Message = "Concept cannot be empty." });
                }

                if (string.IsNullOrWhiteSpace(request.LessonContext))
                {
                    return BadRequest(new { Message = "Lesson context cannot be empty." });
                }

                var result = await _aiChatService.ExplainConceptAsync(
                    request.Concept, 
                    request.LessonContext);

                if (!result.Success)
                {
                    return StatusCode(StatusCodes.Status500InternalServerError, result);
                }

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while explaining concept");
                return StatusCode(StatusCodes.Status500InternalServerError, 
                    new { Message = "An unexpected error occurred while explaining the concept." });
            }
        }

        /// <summary>
        /// Quick ask - simplified endpoint for single questions
        /// </summary>
        /// <param name="request">Simple question request</param>
        /// <returns>AI response</returns>
        [HttpPost("quick-ask")]
        [ProducesResponseType(typeof(ChatResponseDTO), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> QuickAsk([FromBody] QuickAskRequestDTO request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var chatRequest = new ChatRequestDTO
                {
                    UserQuestion = request.Question,
                    LessonContent = request.Context,
                    UserId = request.UserId
                };

                var result = await _aiChatService.AskQuestionAsync(chatRequest);

                if (!result.Success)
                {
                    return StatusCode(StatusCodes.Status500InternalServerError, result);
                }

                return Ok(new 
                { 
                    Answer = result.Answer,
                    SuggestedQuestions = result.SuggestedQuestions,
                    Success = result.Success
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred in quick ask");
                return StatusCode(StatusCodes.Status500InternalServerError, 
                    new { Message = "An unexpected error occurred." });
            }
        }
    }

    #region Supporting DTOs

    /// <summary>
    /// DTO for lesson content request
    /// </summary>
    public class LessonContentRequestDTO
    {
        public string LessonContent { get; set; } = string.Empty;
        public string? LessonTitle { get; set; }
    }

    /// <summary>
    /// DTO for concept explanation request
    /// </summary>
    public class ConceptExplanationRequestDTO
    {
        public string Concept { get; set; } = string.Empty;
        public string LessonContext { get; set; } = string.Empty;
    }

    /// <summary>
    /// DTO for quick ask request
    /// </summary>
    public class QuickAskRequestDTO
    {
        public string Question { get; set; } = string.Empty;
        public string Context { get; set; } = string.Empty;
        public int? UserId { get; set; }
    }

    #endregion
}
