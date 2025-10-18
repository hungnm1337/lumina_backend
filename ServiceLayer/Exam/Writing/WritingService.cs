
using DataLayer.DTOs.Exam.Writting;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using System;
using System.Linq;
using System.Threading.Tasks;
using GenerativeAI.Core;
using GenerativeAI;


namespace ServiceLayer.Exam.Writting
{
    public class WritingService : IWritingService
    {
        private readonly IConfiguration _configuration;
        private readonly string _apiKey;

        public WritingService(IConfiguration configuration)
        {
            _configuration = configuration;
            _apiKey = _configuration["Gemini:ApiKey"];
        }

        public async Task<WritingResponseDTO> GetFeedbackFromAI(WritingRequestDTO request)
        {
            try
            {
                // Create the prompt for Gemini
                var prompt = CreatePrompt(request);

                // --- CHANGE 2: SIMPLIFIED API CALL ---
                // NOTE: Using a valid public model name.
                var modelName = "gemini-2.5-flash";

                // Initialize the Gemini model client
                var generativeModel = new GenerativeModel(_apiKey, new ModelParams { Model = modelName });

                // Call the Gemini API and get the response
                var response = await generativeModel.GenerateContentAsync(prompt);

                var responseText = response.Text;
                // --- END CHANGE 2 ---

                if (string.IsNullOrEmpty(responseText))
                {
                    throw new Exception("No response received from Gemini API");
                }

                // Clean up potential markdown formatting from the response
                responseText = responseText.Trim().Replace("```json", "").Replace("```", "");

                // Parse JSON response to WrittingResponseDTO
                var result = JsonConvert.DeserializeObject<WritingResponseDTO>(responseText);

                return result ?? throw new Exception("Failed to deserialize Gemini API response.");
            }
            catch (Exception ex)
            {
                // Return error response
                return new WritingResponseDTO
                {
                    TotalScore = 0,
                    GrammarFeedback = $"Error: {ex.Message}",
                    VocabularyFeedback = $"Error: {ex.Message}",
                    RequiredWordsCheck = $"Error: {ex.Message}",
                    ContentAccuracyFeedback = $"Error: {ex.Message}",
                    CorreededAnswerProposal = $"Error: {ex.Message}"
                };
            }
        }

        private string CreatePrompt(WritingRequestDTO request)
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
4. **RequiredWordsCheck**: Check if the student used the required vocabulary words correctly
5. **ContentAccuracyFeedback**: Evaluation of how well the content addresses the picture caption and task requirements
6. **CorreededAnswerProposal**: A corrected version of the student's answer with improvements, and have to use the required vocabulary words

**Response Format (JSON only, no additional text):**
{{
    ""TotalScore"": [number between 0-5],
    ""GrammarFeedback"": ""[detailed grammar feedback]"",
    ""VocabularyFeedback"": ""[detailed vocabulary feedback]"",
    ""RequiredWordsCheck"": ""[assessment of required words usage]"",
    ""ContentAccuracyFeedback"": ""[content evaluation]"",
    ""CorreededAnswerProposal"": ""[corrected version of the answer]""
}}

Please be constructive and educational in your feedback, helping the student improve their writing skills.";
        }
    }
}