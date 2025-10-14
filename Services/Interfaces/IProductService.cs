using OrderManagementSystem.Models.Entities;

namespace OrderManagementSystem.Services.Interfaces
{
    public interface IProductService
    {
        Task<List<Product>> GetAllProducts(bool activeOnly = true);
        Task<Product?> GetProductById(Guid id);
        Task<bool> CreateProduct(Product product);
        Task<bool> UpdateProduct(Product product);
        Task<bool> UpdateStock(Guid productId, int quantity);
        Task<bool> DeleteProduct(Guid id);
    }
}