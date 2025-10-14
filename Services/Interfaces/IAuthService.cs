using OrderManagementSystem.Models.Entities;
using OrderManagementSystem.Models.ViewModels;

namespace OrderManagementSystem.Services.Interfaces
{
    public interface IAuthService
    {
        Task<User?> Login(string email, string password);
        Task<bool> Register(RegisterViewModel model);
        Task<User?> GetUserById(Guid id);
        Task<bool> ChangePassword(Guid userId, string oldPassword, string newPassword);
        Task<List<User>> GetAllUsers();
        Task<bool> UpdateUserRole(Guid userId, string role);
        Task<bool> ToggleUserStatus(Guid userId);
    }
}