namespace OrderManagementSystem.Services.Interfaces
{
    public interface IFileService
    {
        Task<string> UploadFile(IFormFile file, string folder = "uploads");
        bool DeleteFile(string filePath);
        string GetFileUrl(string fileName);
    }
}