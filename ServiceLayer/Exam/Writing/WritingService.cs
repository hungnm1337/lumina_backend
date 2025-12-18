
using DataLayer.DTOs.Exam.Speaking;
using DataLayer.DTOs.Exam.Writting;
using DataLayer.DTOs.UserAnswer;
using DataLayer.Models;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using RepositoryLayer.Exam.Writting;
using RepositoryLayer.UnitOfWork;
using System;
using System.Linq;
using System.Threading.Tasks;


namespace ServiceLayer.Exam.Writting
{
    public class WritingService : IWritingService
    {
        private readonly IConfiguration _configuration;
        private readonly IWrittingRepository _writtingRepository;
        private readonly IGenerativeAIService _generativeAIService;
        private readonly IUnitOfWork _unitOfWork;

        public WritingService(IConfiguration configuration, IWrittingRepository writtingRepository, IGenerativeAIService generativeAIService, IUnitOfWork unitOfWork)
        {
            _configuration = configuration;
            _writtingRepository = writtingRepository;
            _generativeAIService = generativeAIService;
            _unitOfWork = unitOfWork;
        }

        
        public async Task<AttemptValidationResult> ValidateAttemptAsync(int attemptId, int userId)
        {
            var attempt = await _unitOfWork.ExamAttemptsGeneric
                .GetAsync(a => a.AttemptID == attemptId);

            if (attempt == null)
            {
                return new AttemptValidationResult
                {
                    IsValid = false,
                    ErrorType = AttemptErrorType.NotFound,
                    ErrorMessage = $"ExamAttempt {attemptId} not found."
                };
            }

            if (attempt.UserID != userId)
            {
                return new AttemptValidationResult
                {
                    IsValid = false,
                    ErrorType = AttemptErrorType.Forbidden,
                    ErrorMessage = "User does not own this attempt."
                };
            }

            return new AttemptValidationResult
            {
                IsValid = true,
                AttemptId = attemptId,
                ErrorType = AttemptErrorType.None
            };
        }

        public async Task<bool> SaveWritingAnswer(WritingAnswerRequestDTO writingAnswerRequestDTO)
        {
            if (writingAnswerRequestDTO == null)
                throw new ArgumentNullException(nameof(writingAnswerRequestDTO), "Request cannot be null.");

            if (writingAnswerRequestDTO.AttemptID <= 0)
                throw new ArgumentException("Attempt ID must be greater than zero.", nameof(writingAnswerRequestDTO.AttemptID));

            if (writingAnswerRequestDTO.QuestionId <= 0)
                throw new ArgumentException("Question ID must be greater than zero.", nameof(writingAnswerRequestDTO.QuestionId));

            if (string.IsNullOrWhiteSpace(writingAnswerRequestDTO.UserAnswerContent))
                throw new ArgumentException("User Answer Content cannot be empty.", nameof(writingAnswerRequestDTO.UserAnswerContent));

            try
            {
                return await _writtingRepository.SaveWritingAnswer(writingAnswerRequestDTO);
            }
            catch (Exception)
            {
                return false;
            }
        }

        public async Task<WritingResponseDTO> GetFeedbackP1FromAI(WritingRequestP1DTO request)
        {
            try
            {
                var prompt = CreatePromptP1(request);

                var responseText = await _generativeAIService.GenerateContentAsync(prompt);

                if (string.IsNullOrEmpty(responseText))
                {
                    throw new Exception("No response received from Gemini API");
                }

                responseText = responseText.Trim().Replace("```json", "").Replace("```", "");

                var result = JsonConvert.DeserializeObject<WritingResponseDTO>(responseText);

                return result ?? throw new Exception("Failed to deserialize Gemini API response.");
            }
            catch (Exception ex)
            {
                return new WritingResponseDTO
                {
                    TotalScore = 0,
                    GrammarFeedback = $"Error: {ex.Message}",
                    VocabularyFeedback = $"Error: {ex.Message}",
                    ContentAccuracyFeedback = $"Error: {ex.Message}",
                    CorreededAnswerProposal = $"Error: {ex.Message}"
                };
            }
        }

        public async Task<WritingResponseDTO> GetFeedbackP23FromAI(WritingRequestP23DTO request)
        {
            try
            {
                var prompt = CreatePromptP23(request);

                var responseText = await _generativeAIService.GenerateContentAsync(prompt);

                if (string.IsNullOrEmpty(responseText))
                {
                    throw new Exception("No response received from Gemini API");
                }

                responseText = responseText.Trim().Replace("```json", "").Replace("```", "");

                var result = JsonConvert.DeserializeObject<WritingResponseDTO>(responseText);

                return result ?? throw new Exception("Failed to deserialize Gemini API response.");
            }
            catch (Exception ex)
            {
                return new WritingResponseDTO
                {
                    TotalScore = 0,
                    GrammarFeedback = $"Error: {ex.Message}",
                    VocabularyFeedback = $"Error: {ex.Message}",
                    ContentAccuracyFeedback = $"Error: {ex.Message}",
                    CorreededAnswerProposal = $"Error: {ex.Message}"
                };
            }
        }

        private string CreatePromptP1(WritingRequestP1DTO request)
        {
            // This method remains the same
            return $@"
You are an expert TOEIC writing evaluator. Please evaluate the following writing task and provide detailed feedback.

**Task Context:**
Picture Caption: {request.PictureCaption}
Vocabulary Requirements: {request.VocabularyRequest}

**Student's Answer:**
{request.UserAnswer}

**Instructions:**
Please evaluate the student's writing based on the following criteria and return your response as a JSON object with the exact structure below:

1. **TotalScore** (0-5): Overall score based on all criteria
2. **GrammarFeedback**: Detailed feedback on grammar, sentence structure, and language accuracy
3. **VocabularyFeedback**: Assessment of vocabulary usage, word choice, and appropriateness
4. **ContentAccuracyFeedback**: Evaluation of how well the content addresses the picture caption and task requirements
5. **CorreededAnswerProposal**: A corrected version of the student's answer with improvements, and have to use the required vocabulary words

**Response Format (JSON only, no additional text):**
{{
    ""TotalScore"": [number between 0-5],
    ""GrammarFeedback"": ""[detailed grammar feedback]"",
    ""VocabularyFeedback"": ""[detailed vocabulary feedback]"",
    ""ContentAccuracyFeedback"": ""[content evaluation]"",
    ""CorreededAnswerProposal"": ""[corrected version of the answer]""
}}

Please be constructive and educational in your feedback, helping the student improve their writing skills.";
        }


        private string CreatePromptP23(WritingRequestP23DTO request)
        {
            // Phần mở đầu chung cho cả hai P arts
            string basePreamble = $@"
You are an expert TOEIC writing evaluator. Please evaluate the following writing task and provide detailed feedback.

**Task Context:**
Part Number: {request.PartNumber}
Prompt: {request.Prompt}

**Student's Answer:**
{request.UserAnswer}

**Instructions:**
Please evaluate the student's writing based on the following criteria and return your response as a JSON object with the exact structure below:
";

            // Tiêu chí cụ thể cho Part 2 (Viết Email)
            string part2Criteria = @"
1. **TotalScore** (0-5): Overall score. (Note: Official TOEIC scale for Part 2 is 0-4; please map your evaluation to the 0-5 scale).
2. **GrammarFeedback**: Detailed feedback on grammar, sentence structure, punctuation, and language accuracy.
3. **VocabularyFeedback**: Assessment of vocabulary usage, word choice, and appropriateness for a professional written request (e.g., email).
4. **ContentAccuracyFeedback**: Evaluation of task completion and organization. Does the response address *all points* and questions from the prompt? Is the tone appropriate? Is the email well-organized (e.g., greeting, body paragraphs, closing)?
5. **CorreededAnswerProposal**: A corrected, improved version of the student's email response that clearly addresses all task points and sounds natural. This response MUST be formatted with appropriate line breaks (newlines) and paragraph breaks, suitable for display with white-space: pre-wrap. It should not be a single continuous block of text.";

            // Tiêu chí cụ thể cho Part 3 (Viết Luận)
            string part3Criteria = @"
1. **TotalScore** (0-5): Overall score based on official TOEIC essay criteria.
2. **GrammarFeedback**: Detailed feedback on grammar accuracy, sentence variety, and the use of complex structures.
3. **VocabularyFeedback**: Assessment of vocabulary range, precision, and appropriate academic/formal word choice for an opinion essay.
4. **ContentAccuracyFeedback**: Evaluation of organization and content. Does the essay have a clear thesis statement that responds to the prompt? Are the supporting arguments relevant, well-explained, and supported by specific reasons or examples? Is the essay well-structured (introduction, body paragraphs, conclusion)?
5. **CorreededAnswerProposal**: A corrected, improved version of the student's opinion essay, modeling strong structure, clear arguments, and supporting details.
";

            string jsonFormat = @"
**Response Format (JSON only, no additional text):**
{{
    ""TotalScore"": [number between 0-5],
    ""GrammarFeedback"": ""[detailed grammar feedback]"",
    ""VocabularyFeedback"": ""[detailed vocabulary feedback]"",
    ""ContentAccuracyFeedback"": ""[content evaluation]"",
    ""CorreededAnswerProposal"": ""[corrected version of the answer]""
}}

Please be constructive and educational in your feedback, helping the student improve their writing skills.";

            // Logic để chọn tiêu chí chấm điểm
            string specificCriteria;
            if (request.PartNumber == 2)
            {
                specificCriteria = part2Criteria;
            }
            else if (request.PartNumber == 3)
            {
                specificCriteria = part3Criteria;
            }
            else
            {
                throw new ArgumentException($"Invalid Part Number for this method. Expected 2 or 3, but received: {request.PartNumber}", nameof(request.PartNumber));
            }

            return basePreamble + specificCriteria + jsonFormat;
        }

    }
}