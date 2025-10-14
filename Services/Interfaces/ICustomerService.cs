using OrderManagementSystem.Models.Entities;

namespace OrderManagementSystem.Services.Interfaces
{
    public interface ICustomerService
    {
        Task<Customer?> GetCustomerByPhone(string phone);
        Task<List<Customer>> GetAllCustomers();
        Task<List<Order>> GetCustomerOrders(Guid customerId);
    }
}