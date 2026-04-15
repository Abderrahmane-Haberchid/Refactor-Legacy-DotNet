using Refacto.DotNet.Controllers.Entities;

namespace Refacto.DotNet.Controllers.Services;

public interface IProductService
{
    public Task HandleSeasonalProductAsync(Product product, CancellationToken ct);
    public Task HandleExpiredProductAsync(Product product, CancellationToken ct);

    public Task NotifyDelayAsync(int leadTime, Product product, CancellationToken ct);
}