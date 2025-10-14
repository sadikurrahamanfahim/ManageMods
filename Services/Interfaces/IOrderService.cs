using OrderManagementSystem.Models.Entities;
using OrderManagementSystem.Models.ViewModels;

namespace OrderManagementSystem.Services.Interfaces
{
    public interface IOrderService
    {
        Task<bool> CreateOrder(CreateOrderViewModel model, Guid createdBy);
        Task<Order?> GetOrderById(Guid id);
        Task<List<Order>> GetAllOrders(string? status = null, string? searchTerm = null);
        Task<bool> UpdateOrderStatus(Guid orderId, string newStatus, Guid userId, string? comment = null);
        Task<bool> UpdateOrder(Order order);
        Task<string> GenerateOrderNumber();
    }
}