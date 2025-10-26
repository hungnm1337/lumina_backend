using DataLayer.DTOs;
using DataLayer.DTOs.AIGeneratedExam;
using DataLayer.DTOs.Questions;
using DataLayer.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using ServiceLayer.AI.Prompt;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace ServiceLayer.ExamGenerationAI
{
    public class ExamGenerationAIService : IExamGenerationAIService
    {
        private readonly HttpClient _httpClient;
        private readonly GeminiOptions _options;

        public ExamGenerationAIService(IOptions<GeminiOptions> options, HttpClient httpClient)
        {
            _options = options.Value;
            _httpClient = httpClient;
        }

        // --- Gọi Gemini API ---
        public async Task<string> GenerateResponseAsync(string prompt)
        {
            var url = $"{_options.BaseUrl}/{_options.Model}:generateContent?key={_options.ApiKey}";

            var requestBody = new
            {
                contents = new[]
                {
                    new
                    {
                        parts = new[] { new { text = prompt } }
                    }
                }
            };

            var json = JsonConvert.SerializeObject(requestBody);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync(url, content);

            if (!response.IsSuccessStatusCode)
            {
                var errorMsg = await response.Content.ReadAsStringAsync();
                throw new Exception($"Gemini API error: {errorMsg}");
            }

            return await response.Content.ReadAsStringAsync();
        }

        // --- Parse yêu cầu người dùng ---
        public async Task<(int partNumber, int quantity, string? topic)> ParseUserRequestAsync(string userRequest)
        {
            var parsePrompt = PromptFactory.CreateParsingPrompt(userRequest);
            var responseStr = await GenerateResponseAsync(parsePrompt);

            Console.WriteLine("Raw Gemini Response for ParsePrompt: " + responseStr);

            dynamic root = JsonConvert.DeserializeObject(responseStr);
            string textJson = root.candidates[0].content.parts[0].text.ToString();

            textJson = CleanAIResponse(textJson);
            var parseResult = JsonConvert.DeserializeObject<ParseResultDTO>(textJson);

            return (parseResult.PartNumber, parseResult.Quantity, parseResult.Topic);
        }

        // --- Sinh đề thi ---
       /* public async Task<AIGeneratedExamDTO> GenerateExamAsync(int partNumber, int quantity, string? topic)
        {
            var genPrompt = PromptFactory.GetGenerationPrompt(partNumber, quantity, topic);
            var responseStr = await GenerateResponseAsync(genPrompt);

            Console.WriteLine("Raw Gemini Response for GenerateExam: " + responseStr);

            dynamic root = JsonConvert.DeserializeObject(responseStr);
            string textJson = root.candidates[0].content.parts[0].text.ToString();

            textJson = CleanAIResponse(textJson);

            var examDto = JsonConvert.DeserializeObject<AIGeneratedExamDTO>(textJson);
            return examDto;
        }*/

        public async Task<AIGeneratedExamDTO> GenerateExamAsync(int partNumber, int quantity, string? topic)
        {
            var genPrompt = PromptFactory.GetGenerationPrompt(partNumber, quantity, topic);
            var responseStr = await GenerateResponseAsync(genPrompt);

            

            dynamic root = JsonConvert.DeserializeObject(responseStr);
            string textJson = root.candidates[0].content.parts[0].text.ToString();

            textJson = CleanAIResponse(textJson);
            var examDto = JsonConvert.DeserializeObject<AIGeneratedExamDTO>(textJson);

            // ✅ Nếu có prompt với mô tả ảnh -> tạo link ảnh Pollinations
            foreach (var prompt in examDto.Prompts)
            {
                if (!string.IsNullOrEmpty(prompt.ReferenceImageUrl) && !prompt.ReferenceImageUrl.StartsWith("http"))
                {
                    prompt.ReferenceImageUrl = GeneratePollinationsImageUrl(prompt.ReferenceImageUrl);
                }
            }

            return examDto;
        }


        // --- Phát hiện mục đích user ---
        public async Task<IntentResult> DetectIntentAsync(string userRequest)
        {
            var prompt = $@"
                Phân loại yêu cầu của user sau đây:
                - Nếu user muốn TẠO ĐỀ THI/CÂU HỎI TOEIC (có từ khóa: 'tạo', 'generate', 'câu hỏi', 'đề thi', 'Reading Part', 'Listening Part', 'số lượng câu') → IsExamRequest = true
                - Nếu user HỎI THÔNG TIN/GIẢI THÍCH/TRÒ CHUYỆN → IsExamRequest = false

                User request: ""{userRequest}""

                Trả về JSON:
                {{
                  ""isExamRequest"": true/false,
                  ""explanation"": ""lý do ngắn gọn""
                }}
            ";

            var responseStr = await GenerateResponseAsync(prompt);
            dynamic root = JsonConvert.DeserializeObject(responseStr);
            string textJson = root.candidates[0].content.parts[0].text.ToString();

            textJson = CleanAIResponse(textJson);
            return JsonConvert.DeserializeObject<IntentResult>(textJson);
        }

        // --- Trả lời chung ---
        public async Task<string> GeneralChatAsync(string userMessage)
        {
            var prompt = $@"
                Bạn là trợ lý AI chuyên về TOEIC, hỗ trợ giảng viên và học sinh.

                Nhiệm vụ:
                - Trả lời câu hỏi về TOEIC (cấu trúc bài thi, mẹo, từ vựng, ngữ pháp)
                - Giọng văn thân thiện, 200–300 từ
                - Sử dụng emoji phù hợp

                Người dùng hỏi: ""{userMessage}""
                Trả lời:
            ";

            var responseStr = await GenerateResponseAsync(prompt);
            dynamic root = JsonConvert.DeserializeObject(responseStr);

            if (root == null || root.candidates == null || root.candidates.Count == 0)
                throw new Exception("Không nhận được phản hồi hợp lệ từ AI.");

            string textJson = root.candidates[0].content.parts[0].text.ToString();
            textJson = CleanAIResponse(textJson);

            return textJson;
        }


        public string GeneratePollinationsImageUrl(string description)
        {
            if (string.IsNullOrWhiteSpace(description))
                return null;

            // Encode mô tả ảnh để đưa vào URL
            string encodedPrompt = Uri.EscapeDataString(description.Trim());

            // Mẫu URL Pollinations:
            // https://image.pollinations.ai/prompt/{prompt}?model=flux&width=1024&height=1024&seed=random&nologo=true&enhance=true&safe=true
            string imageUrl = $"https://image.pollinations.ai/prompt/{encodedPrompt}?model=flux&width=512&height=512&seed=random&nologo=true&enhance=true&safe=true";

            return imageUrl;
        }

        // --- ✅ Hàm gộp tối ưu: dọn sạch output từ Gemini ---
        private string CleanAIResponse(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return text;

            // 1️⃣ Loại bỏ các cặp ```json ... ```
            text = System.Text.RegularExpressions.Regex.Replace(
                text,
                @"```(json|markdown|html)?\s*([\s\S]*?)\s*```",
                "$2",
                System.Text.RegularExpressions.RegexOptions.IgnoreCase
            );

            // 2️⃣ Xóa các từ dư, ký tự markdown
            text = text
                .Replace("**", "")
                .Replace("##", "")
                .Replace("Here is the JSON output you requested:", "")
                .Replace("Here is your output:", "")
                .Replace("Response:", "")
                .Replace("Output:", "")
                .Replace("Sure!", "")
                .Trim();

            // 3️⃣ Chỉ giữ phần JSON thật giữa { và }
            int firstBrace = text.IndexOf('{');
            int lastBrace = text.LastIndexOf('}');
            if (firstBrace >= 0 && lastBrace > firstBrace)
                text = text.Substring(firstBrace, lastBrace - firstBrace + 1);

            return text.Trim();
        }

    }
}
