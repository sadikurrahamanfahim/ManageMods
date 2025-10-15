using OrderManagementSystem.Services.Interfaces;
using SupabaseClient = Supabase.Client; // Alias to avoid ambiguity

namespace OrderManagementSystem.Services.Implementations
{
    public class SupabaseStorageService : ISupabaseStorageService
    {
        private readonly SupabaseClient _supabaseClient;
        private readonly IConfiguration _configuration;

        public SupabaseStorageService(IConfiguration configuration)
        {
            _configuration = configuration;

            var url = configuration["Supabase:Url"];
            var key = configuration["Supabase:Key"];

            var options = new Supabase.SupabaseOptions
            {
                AutoConnectRealtime = false
            };

            _supabaseClient = new SupabaseClient(url, key, options);
        }

        public async Task<string> UploadFile(IFormFile file, string bucket, string folder = "")
        {
            if (file == null || file.Length == 0)
                return string.Empty;

            try
            {
                // Generate unique filename
                var fileExtension = Path.GetExtension(file.FileName);
                var uniqueFileName = $"{Guid.NewGuid()}{fileExtension}";
                var filePath = string.IsNullOrEmpty(folder)
                    ? uniqueFileName
                    : $"{folder}/{uniqueFileName}";

                // Convert IFormFile to byte array
                using var memoryStream = new MemoryStream();
                await file.CopyToAsync(memoryStream);
                var fileBytes = memoryStream.ToArray();

                // Upload to Supabase Storage
                await _supabaseClient.Storage
                    .From(bucket)
                    .Upload(fileBytes, filePath, new Supabase.Storage.FileOptions
                    {
                        CacheControl = "3600",
                        Upsert = false
                    });

                // Return the file path
                return filePath;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Upload error: {ex.Message}");
                return string.Empty;
            }
        }

        public async Task<bool> DeleteFile(string bucket, string filePath)
        {
            if (string.IsNullOrEmpty(filePath))
                return false;

            try
            {
                await _supabaseClient.Storage
                    .From(bucket)
                    .Remove(new List<string> { filePath });

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Delete error: {ex.Message}");
                return false;
            }
        }

        public string GetPublicUrl(string bucket, string filePath)
        {
            if (string.IsNullOrEmpty(filePath))
                return string.Empty;

            var url = _supabaseClient.Storage
                .From(bucket)
                .GetPublicUrl(filePath);

            return url;
        }
    }
}