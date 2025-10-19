using OrderManagementSystem.Models.ViewModels;

namespace OrderManagementSystem.Services.Interfaces
{
    public interface ISteadfastService
    {
        Task<SteadfastOrderResponse> CreateOrder(SteadfastOrderRequest request);
        Task<SteadfastStatusResponse> CheckDeliveryStatus(string trackingCode);
        Task<SteadfastBalanceResponse> GetCurrentBalance();
    }
}