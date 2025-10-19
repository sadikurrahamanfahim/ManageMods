using Microsoft.Extensions.Hosting;
using OrderManagementSystem.Services.Interfaces;
using OrderManagementSystem.Data;
using Microsoft.EntityFrameworkCore;

namespace OrderManagementSystem.Services.Implementations
{
    public class SteadfastSyncService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<SteadfastSyncService> _logger;

        public SteadfastSyncService(
            IServiceProvider serviceProvider,
            ILogger<SteadfastSyncService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await SyncOrderStatuses();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error syncing Steadfast statuses");
                }

                // Run every 30 minutes
                await Task.Delay(TimeSpan.FromMinutes(30), stoppingToken);
            }
        }

        private async Task SyncOrderStatuses()
        {
            using var scope = _serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var steadfastService = scope.ServiceProvider.GetRequiredService<ISteadfastService>();

            var ordersToSync = await dbContext.Orders
                .Where(o => o.SentToSteadfast &&
                           o.SteadfastTrackingCode != null &&
                           o.Status != "completed" &&
                           o.Status != "cancelled")
                .ToListAsync();

            foreach (var order in ordersToSync)
            {
                try
                {
                    var statusResponse = await steadfastService.CheckDeliveryStatus(order.SteadfastTrackingCode!);

                    if (statusResponse.Status == 200)
                    {
                        order.SteadfastStatus = statusResponse.DeliveryStatus;

                        // Update local status based on Steadfast status
                        if (statusResponse.DeliveryStatus == "delivered")
                        {
                            order.Status = "completed";
                            order.CompletedAt = DateTime.UtcNow;
                        }
                        else if (statusResponse.DeliveryStatus == "cancelled")
                        {
                            order.Status = "cancelled";
                        }

                        order.UpdatedAt = DateTime.UtcNow;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Error syncing order {order.OrderNumber}");
                }
            }

            await dbContext.SaveChangesAsync();
            _logger.LogInformation($"Synced {ordersToSync.Count} orders with Steadfast");
        }
    }
}