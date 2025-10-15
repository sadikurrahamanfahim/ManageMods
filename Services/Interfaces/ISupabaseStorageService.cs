namespace OrderManagementSystem.Services.Interfaces
{
    public interface ISupabaseStorageService
    {
        Task<string> UploadFile(IFormFile file, string bucket, string folder = "");
        Task<bool> DeleteFile(string bucket, string filePath);
        string GetPublicUrl(string bucket, string filePath);
    }
}