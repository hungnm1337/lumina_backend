namespace DataLayer.DTOs
{
    public class OpenAIOptions
    {
        public string ApiKey { get; set; } = string.Empty;
        public string BaseUrl { get; set; } = "https://api.openai.com/v1";
        public string Model { get; set; } = "gpt-4-turbo-preview";
        public string ChatModel { get; set; } = "gpt-3.5-turbo";
        public int MaxTokens { get; set; } = 4000;
        public double Temperature { get; set; } = 0.7;
    }
}