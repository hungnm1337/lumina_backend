using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using DataLayer.DTOs;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace ServiceLayer.UploadFile
{
    public class UploadService : IUploadService
    {
        private readonly Cloudinary _cloudinary;

        public UploadService(IConfiguration configuration)
        {
            var account = new Account(
                configuration["CloudinarySettings:CloudName"],
                configuration["CloudinarySettings:ApiKey"],
                configuration["CloudinarySettings:ApiSecret"]);

            _cloudinary = new Cloudinary(account);
        }

        public async Task<UploadResultDTO> UploadFileAsync(IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                throw new ArgumentException("File không hợp lệ.");
            }

            UploadResult uploadResult;
            string contentType = file.ContentType?.ToLower() ?? "";
            string fileName = file.FileName?.ToLower() ?? "";

            // Xử lý Audio files
            if (contentType.StartsWith("audio/"))
            {
                var audioParams = new VideoUploadParams()
                {
                    File = new FileDescription(file.FileName, file.OpenReadStream()),
                    PublicId = $"lumina/audio/{Path.GetFileNameWithoutExtension(file.FileName)}_{Guid.NewGuid()}"
                    // ResourceType is automatically set to Video by VideoUploadParams
                };

                uploadResult = await _cloudinary.UploadAsync(audioParams);
            }
            // Xử lý Video files
            else if (contentType.StartsWith("video/") || 
                     fileName.EndsWith(".mp4") || fileName.EndsWith(".mov") || 
                     fileName.EndsWith(".avi") || fileName.EndsWith(".wmv") || 
                     fileName.EndsWith(".flv") || fileName.EndsWith(".webm"))
            {
                var videoParams = new VideoUploadParams()
                {
                    File = new FileDescription(file.FileName, file.OpenReadStream()),
                    PublicId = $"lumina/videos/{Path.GetFileNameWithoutExtension(file.FileName)}_{Guid.NewGuid()}",
                    // ResourceType is automatically set to Video by VideoUploadParams
                    EagerTransforms = new List<Transformation>()
                    {
                        new Transformation().Width(1280).Height(720).Crop("limit").Quality("auto"),
                        new Transformation().Width(640).Height(360).Crop("limit").Quality("auto")
                    }
                };

                uploadResult = await _cloudinary.UploadAsync(videoParams);
            }
            // Xử lý Image files (mặc định)
            else
            {
                var imageParams = new ImageUploadParams
                {
                    File = new FileDescription(file.FileName, file.OpenReadStream()),
                    PublicId = $"lumina/images/{Path.GetFileNameWithoutExtension(file.FileName)}_{Guid.NewGuid()}",
                    // Không đặt Transformation để giữ nguyên kích thước gốc ảnh
                };

                var imageUploadResult = await _cloudinary.UploadAsync(imageParams);
                if (imageUploadResult.Error != null)
                {
                    throw new Exception(imageUploadResult.Error.Message);
                }
                return new DataLayer.DTOs.UploadResultDTO { Url = imageUploadResult.SecureUrl.ToString(), PublicId = imageUploadResult.PublicId };
            }

            if (uploadResult.Error != null)
            {
                throw new Exception(uploadResult.Error.Message);
            }

            return new UploadResultDTO { Url = uploadResult.SecureUrl.ToString(), PublicId = uploadResult.PublicId };
        }

        // ✅ Hàm mới: Upload từ URL (không cần file)
        public async Task<UploadResultDTO> UploadFromUrlAsync(string fileUrl)
        {
            if (string.IsNullOrEmpty(fileUrl))
                throw new ArgumentException("URL không hợp lệ.");

            string urlLower = fileUrl.ToLower();
            UploadResult uploadResult;

            // Kiểm tra đuôi file để xác định loại upload
            bool isAudio = urlLower.EndsWith(".mp3") || urlLower.EndsWith(".wav") || urlLower.EndsWith(".m4a") || urlLower.EndsWith(".ogg");
            bool isVideo = urlLower.EndsWith(".mp4") || urlLower.EndsWith(".mov") || urlLower.EndsWith(".avi") || 
                          urlLower.EndsWith(".wmv") || urlLower.EndsWith(".flv") || urlLower.EndsWith(".webm");

            if (isAudio)
            {
                var audioParams = new VideoUploadParams
                {
                    File = new FileDescription(fileUrl),
                    PublicId = $"lumina/audio_{Guid.NewGuid()}"
                    // ResourceType is automatically set to Video by VideoUploadParams
                };
                uploadResult = await _cloudinary.UploadAsync(audioParams);
            }
            else if (isVideo)
            {
                var videoParams = new VideoUploadParams
                {
                    File = new FileDescription(fileUrl),
                    PublicId = $"lumina/videos_{Guid.NewGuid()}"
                    // ResourceType is automatically set to Video by VideoUploadParams
                };
                uploadResult = await _cloudinary.UploadAsync(videoParams);
            }
            else // Mặc định là image
            {
                var imageParams = new ImageUploadParams
                {
                    File = new FileDescription(fileUrl),
                    PublicId = $"lumina/images_{Guid.NewGuid()}",
                };
                uploadResult = await _cloudinary.UploadAsync(imageParams);
            }

            if (uploadResult.Error != null)
                throw new Exception(uploadResult.Error.Message);

            return new UploadResultDTO
            {
                Url = uploadResult.SecureUrl.ToString(),
                PublicId = uploadResult.PublicId
            };
        }
    }
}