using Microsoft.EntityFrameworkCore;
using OrderManagementSystem.Data;
using OrderManagementSystem.Models.Entities;
using OrderManagementSystem.Services.Interfaces;

namespace OrderManagementSystem.Services.Implementations
{
    public class ProductService : IProductService
    {
        private readonly ApplicationDbContext _context;

        public ProductService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<Product>> GetAllProducts(bool activeOnly = true)
        {
            var query = _context.Products.AsQueryable();

            if (activeOnly)
                query = query.Where(p => p.IsActive);

            return await query.OrderBy(p => p.Name).ToListAsync();
        }

        public async Task<Product?> GetProductById(Guid id)
        {
            return await _context.Products.FindAsync(id);
        }

        public async Task<bool> CreateProduct(Product product)
        {
            product.CreatedAt = DateTime.UtcNow;
            _context.Products.Add(product);
            return await _context.SaveChangesAsync() > 0;
        }

        public async Task<bool> UpdateProduct(Product product)
        {
            _context.Products.Update(product);
            return await _context.SaveChangesAsync() > 0;
        }

        public async Task<bool> UpdateStock(Guid productId, int quantity)
        {
            var product = await _context.Products.FindAsync(productId);
            if (product == null) return false;

            product.StockQuantity += quantity;
            return await _context.SaveChangesAsync() > 0;
        }

        public async Task<bool> DeleteProduct(Guid id)
        {
            var product = await _context.Products.FindAsync(id);
            if (product == null) return false;

            // Permanently delete instead of just marking inactive
            _context.Products.Remove(product);
            return await _context.SaveChangesAsync() > 0;
        }
    }
}