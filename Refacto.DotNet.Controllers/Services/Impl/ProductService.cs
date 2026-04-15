using Refacto.DotNet.Controllers.Database.Context;
using Refacto.DotNet.Controllers.Entities;

namespace Refacto.DotNet.Controllers.Services.Impl
{
    public class ProductService : IProductService
    {
        private readonly INotificationService _notificationService;
        private readonly AppDbContext _appDbContext;

        public ProductService(INotificationService notificationService, AppDbContext appDbContext)
        {
            _notificationService = notificationService;
            _appDbContext = appDbContext;
        }

        public async Task HandleSeasonalProductAsync(Product product, CancellationToken ct)
        {
            if (DateTime.Now.AddDays(product.LeadTime) > product.SeasonEndDate)
            {
                _notificationService.SendOutOfStockNotification(product.Name);
                product.Available = 0;
            }
            else if (product.SeasonStartDate > DateTime.Now)
            {
                _notificationService.SendOutOfStockNotification(product.Name);
            }
            else
            {
                await NotifyDelayAsync(product.LeadTime, product, ct);
            }
            await _appDbContext.SaveChangesAsync(ct);
        }

        public async Task HandleExpiredProductAsync(Product product, CancellationToken ct)
        {
            if (product.Available > 0 && product.ExpiryDate > DateTime.Now)
            {
                product.Available -= 1;
            }
            else
            {
                _notificationService.SendExpirationNotification(product.Name, (DateTime)product.ExpiryDate);
                product.Available = 0;
            }
            
            await _appDbContext.SaveChangesAsync(ct);
        }
        
        public async Task NotifyDelayAsync(int leadTime, Product product, CancellationToken ct)
        {
            product.LeadTime = leadTime;
            await _appDbContext.SaveChangesAsync(ct);
            _notificationService.SendDelayNotification(leadTime, product.Name);
        }
    }
}
