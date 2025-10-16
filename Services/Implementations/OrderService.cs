using Microsoft.EntityFrameworkCore;
using OrderManagementSystem.Data;
using OrderManagementSystem.Models.Entities;
using OrderManagementSystem.Models.ViewModels;
using OrderManagementSystem.Services.Interfaces;

namespace OrderManagementSystem.Services.Implementations
{
    public class OrderService : IOrderService
    {
        private readonly ApplicationDbContext _context;

        public OrderService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<bool> CreateOrder(CreateOrderViewModel model, Guid createdBy)
        {
            // Get or create customer
            var customer = await _context.Customers
                .FirstOrDefaultAsync(c => c.Phone == model.CustomerPhone);

            if (customer == null)
            {
                customer = new Customer
                {
                    Name = model.CustomerName,
                    Phone = model.CustomerPhone,
                    Address = model.CustomerAddress,
                    TotalOrders = 0,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
                _context.Customers.Add(customer);
                await _context.SaveChangesAsync();
            }

            customer.TotalOrders++;
            customer.UpdatedAt = DateTime.UtcNow;

            // Parse order items
            List<OrderItemViewModel> orderItems = null;
            if (!string.IsNullOrEmpty(model.OrderItemsJson))
            {
                orderItems = System.Text.Json.JsonSerializer.Deserialize<List<OrderItemViewModel>>(model.OrderItemsJson);
            }

            // Calculate totals
            decimal totalPrice = 0;
            string productSummary = "";

            if (orderItems != null && orderItems.Any())
            {
                totalPrice = orderItems.Sum(i => i.Price * i.Quantity);
                productSummary = string.Join(", ", orderItems.Select(i => $"{i.ProductName} x{i.Quantity}"));

                // Update stock for each product
                foreach (var item in orderItems)
                {
                    if (item.ProductId.HasValue)
                    {
                        var product = await _context.Products.FindAsync(item.ProductId.Value);
                        if (product != null && product.StockQuantity >= item.Quantity)
                        {
                            product.StockQuantity -= item.Quantity;
                        }
                    }
                }
            }
            else
            {
                // Single product (backward compatibility)
                totalPrice = model.ProductPrice * model.ProductQuantity;
                productSummary = model.ProductName;

                // Update stock for single product
                if (model.ProductId.HasValue)
                {
                    var product = await _context.Products.FindAsync(model.ProductId.Value);
                    if (product != null && product.StockQuantity >= model.ProductQuantity)
                    {
                        product.StockQuantity -= model.ProductQuantity;
                    }
                }
            }

            var order = new Order
            {
                OrderNumber = await GenerateOrderNumber(),
                CustomerId = customer.Id,
                CustomerName = model.CustomerName,
                CustomerPhone = model.CustomerPhone,
                CustomerAddress = model.CustomerAddress,
                ProductId = model.ProductId,
                ProductName = productSummary,
                ProductColor = model.ProductColor,
                ProductQuantity = orderItems?.Sum(i => i.Quantity) ?? model.ProductQuantity,
                ProductPrice = totalPrice,
                OrderItems = model.OrderItemsJson,
                ScreenshotUrls = model.ScreenshotUrls,
                ScreenshotUrl = !string.IsNullOrEmpty(model.ScreenshotUrls)
                    ? System.Text.Json.JsonSerializer.Deserialize<List<string>>(model.ScreenshotUrls)?.FirstOrDefault()
                    : null,
                OrderNotes = model.OrderNotes,
                CreatedBy = createdBy,
                Status = "pending",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Orders.Add(order);

            var history = new OrderHistory
            {
                OrderId = order.Id,
                ChangedBy = createdBy,
                NewStatus = "pending",
                Comment = "Order created",
                CreatedAt = DateTime.UtcNow
            };
            _context.OrderHistories.Add(history);

            return await _context.SaveChangesAsync() > 0;
        }

        public async Task<Order?> GetOrderById(Guid id)
        {
            return await _context.Orders
                .Include(o => o.Customer)
                .Include(o => o.Product)
                .Include(o => o.Creator)
                .Include(o => o.Processor)
                .Include(o => o.OrderHistories)
                    .ThenInclude(h => h.ChangedByUser)
                .FirstOrDefaultAsync(o => o.Id == id);
        }

        public async Task<List<Order>> GetAllOrders(string? status = null, string? searchTerm = null)
        {
            var query = _context.Orders
                .Include(o => o.Customer)
                .Include(o => o.Creator)
                .AsQueryable();

            if (!string.IsNullOrEmpty(status))
                query = query.Where(o => o.Status == status);

            if (!string.IsNullOrEmpty(searchTerm))
            {
                query = query.Where(o =>
                    o.OrderNumber.Contains(searchTerm) ||
                    o.CustomerName.Contains(searchTerm) ||
                    o.CustomerPhone.Contains(searchTerm) ||
                    o.ProductName.Contains(searchTerm)
                );
            }

            return await query.OrderBy(o => o.CreatedAt).ToListAsync();
        }

        public async Task<bool> UpdateOrderStatus(Guid orderId, string newStatus, Guid userId, string? comment = null)
        {
            var order = await _context.Orders.FindAsync(orderId);
            if (order == null) return false;

            var previousStatus = order.Status;
            order.Status = newStatus;
            order.UpdatedAt = DateTime.UtcNow;
            order.ProcessedBy = userId;

            if (newStatus == "completed")
                order.CompletedAt = DateTime.UtcNow;

            var history = new OrderHistory
            {
                OrderId = orderId,
                ChangedBy = userId,
                PreviousStatus = previousStatus,
                NewStatus = newStatus,
                Comment = comment,
                CreatedAt = DateTime.UtcNow
            };
            _context.OrderHistories.Add(history);

            return await _context.SaveChangesAsync() > 0;
        }

        public async Task<bool> UpdateOrder(Order order)
        {
            order.UpdatedAt = DateTime.UtcNow;
            _context.Orders.Update(order);
            return await _context.SaveChangesAsync() > 0;
        }

        public async Task<string> GenerateOrderNumber()
        {
            var today = DateTime.UtcNow.ToString("yyyyMMdd");
            var count = await _context.Orders
                .Where(o => o.OrderNumber.StartsWith($"ORD-{today}"))
                .CountAsync();

            return $"ORD-{today}-{(count + 1):D4}";
        }
    }
}