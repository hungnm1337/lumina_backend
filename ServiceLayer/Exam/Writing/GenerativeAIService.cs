using GenerativeAI;
using GenerativeAI.Core;
using System.Threading.Tasks;

namespace ServiceLayer.Exam.Writting
{
    public class GenerativeAIService : IGenerativeAIService
    {
        private readonly string _apiKey;
        private readonly string _modelName;

        public GenerativeAIService(string apiKey, string modelName = "gemini-2.5-flash")
        {
            _apiKey = apiKey;
            _modelName = modelName;
        }

        public async Task<string> GenerateContentAsync(string prompt)
        {
            var generativeModel = new GenerativeModel(_apiKey, new ModelParams { Model = _modelName });
            var response = await generativeModel.GenerateContentAsync(prompt);
            return response.Text;
        }
    }
}

