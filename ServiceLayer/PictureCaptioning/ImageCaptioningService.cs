using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace ServiceLayer.PictureCaptioning
{
    public class CaptionResponse
    {
        public string caption { get; set; }
    }

    public class ImageCaptioningService
{
    private readonly HttpClient _httpClient;
    private readonly string _apiEndpoint = "/caption"; 

    public ImageCaptioningService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<string> GetCaptionFromImageUrl(string imageUrl)
    {
        if (imageUrl == null)
        {
            throw new ArgumentNullException(nameof(imageUrl));
        }

        if (string.IsNullOrWhiteSpace(imageUrl))
        {
            throw new ArgumentException("Image URL cannot be null or empty", nameof(imageUrl));
        }

        try
        {
            var requestBody = new { imageUrl = imageUrl };
            var jsonContent = JsonConvert.SerializeObject(requestBody);
            var httpContent = new StringContent(jsonContent, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync(_apiEndpoint, httpContent);
            response.EnsureSuccessStatusCode();

            var responseString = await response.Content.ReadAsStringAsync();
            var responseObject = JsonConvert.DeserializeObject<CaptionResponse>(responseString);

            if (responseObject != null && !string.IsNullOrEmpty(responseObject.caption))
            {
                return responseObject.caption;
            }

            return "Error: Caption field not found or null in API response.";
        }
        catch (HttpRequestException e)
        {
            // Trả về lỗi để xử lý ở tầng Controller
            return $"Error: Could not get caption from AI service. Details: {e.Message}";
        }
        catch (Exception e)
        {
            return $"Error: An unexpected error occurred. Details: {e.Message}";
        }
    }
    }
}