// ImageCaptioningService.cs

using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json; // Cần có NuGet package Newtonsoft.Json

// Định nghĩa một class nhỏ để phân tích phản hồi
public class CaptionResponse
{
    public string caption { get; set; }
}

public class ImageCaptioningService
{
    private readonly HttpClient _httpClient;
    private readonly string _apiEndpoint = "/caption"; // Endpoint của API Python

    // HttpClient được truyền vào qua Constructor Injection
    public ImageCaptioningService(HttpClient httpClient)
    {
        _httpClient = httpClient;
        // Cấu hình Timeout tại đây hoặc trong lúc đăng ký service
        // Nếu bạn cấu hình BaseAddress lúc đăng ký, bạn có thể bỏ qua dòng dưới
        // _httpClient.BaseAddress = new Uri("http://localhost:5000"); 
    }

    public async Task<string> GetCaptionFromImageUrl(string imageUrl)
    {
        try
        {
            var requestBody = new { imageUrl = imageUrl };
            var jsonContent = JsonConvert.SerializeObject(requestBody);
            var httpContent = new StringContent(jsonContent, Encoding.UTF8, "application/json");

            // Gửi yêu cầu POST tới /caption. HttpClient đã biết BaseAddress
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