using Refacto.DotNet.Controllers.Entities;

namespace Refacto.DotNet.Controllers.Respository;

public interface IOrderRepository 
{
    public Task<Order?> GetOrderByIdAsync(long orderId, CancellationToken token);
    
    public Task SaveOrderAsync(Order order, CancellationToken token);
    public Task<int> SaveToDatabaseAsync(CancellationToken token);
}