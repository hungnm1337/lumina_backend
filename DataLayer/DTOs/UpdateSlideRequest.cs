using Microsoft.AspNetCore.Http;

namespace DataLayer.DTOs
{
    public class UpdateSlideRequest
    {
        public string SlideName { get; set; } = null!;
        public bool? IsActive { get; set; }
        public IFormFile? ImageFile { get; set; }
    }
}

