namespace API.Services
{
    public interface IImageService
    {
        Task<string?> SaveImageAsync(IFormFile? imageFile, string subFolder);
        void DeleteImage(string? imageUrl);
        string? ResolveImageUrl(string? imageUrl);
    }

    public class ImageService : IImageService
    {
        private readonly IWebHostEnvironment _env;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private const string PhysicalImagesRoot = @"C:\imagesAngular\assets\images";

        public ImageService(IWebHostEnvironment env, IHttpContextAccessor httpContextAccessor)
        {
            _env = env;
            _httpContextAccessor = httpContextAccessor;
        }

        private string GetBaseUrl()
        {
            var request = _httpContextAccessor.HttpContext?.Request;
            return $"{request?.Scheme}://{request?.Host}";
        }

        public async Task<string?> SaveImageAsync(IFormFile? imageFile, string subFolder)
        {
            if (imageFile == null || imageFile.Length == 0)
                return null;

            var uploadsFolder = Path.Combine(PhysicalImagesRoot, subFolder);
            Directory.CreateDirectory(uploadsFolder);

            var uniqueFileName = $"{Guid.NewGuid()}{Path.GetExtension(imageFile.FileName)}";
            var filePath = Path.Combine(uploadsFolder, uniqueFileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await imageFile.CopyToAsync(stream);
            }

            return $"{GetBaseUrl()}/assets/images/{subFolder}/{uniqueFileName}";
        }

        public string? ResolveImageUrl(string? imageUrl)
        {
            if (string.IsNullOrEmpty(imageUrl))
                return null;
            if (imageUrl.StartsWith("http", StringComparison.OrdinalIgnoreCase))
                return imageUrl;
            return $"{GetBaseUrl()}{imageUrl}";
        }

        public void DeleteImage(string? imageUrl)
        {
            if (string.IsNullOrEmpty(imageUrl))
                return;

            // Extraire le chemin relatif depuis une URL absolue
            var pathPart = imageUrl;
            if (Uri.TryCreate(imageUrl, UriKind.Absolute, out var absoluteUri))
            {
                pathPart = absoluteUri.AbsolutePath;
            }

            string filePath;
            if (Path.IsPathRooted(pathPart))
            {
                filePath = Path.GetFullPath(pathPart.Replace('/', Path.DirectorySeparatorChar));
            }
            else if (pathPart.StartsWith("/assets/images/", StringComparison.OrdinalIgnoreCase))
            {
                var relativeToImagesRoot = pathPart["/assets/images/".Length..];
                filePath = Path.GetFullPath(Path.Combine(PhysicalImagesRoot, relativeToImagesRoot.Replace('/', Path.DirectorySeparatorChar)));
            }
            else
            {
                filePath = Path.GetFullPath(Path.Combine(_env.WebRootPath, pathPart.TrimStart('/').Replace('/', Path.DirectorySeparatorChar)));
            }

            if (File.Exists(filePath))
                File.Delete(filePath);
        }
    }
}
