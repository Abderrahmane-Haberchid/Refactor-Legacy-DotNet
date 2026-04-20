using Microsoft.EntityFrameworkCore;
using Refacto.DotNet.Controllers.Database.Context;
using Refacto.DotNet.Controllers.Entities;

namespace Refacto.DotNet.Controllers.Respository;

public class OrderRepository :  IOrderRepository
{
    private readonly AppDbContext _appDbContext;

    public OrderRepository(AppDbContext appDbContext)
    {
        _appDbContext = appDbContext;
    }
    
    public async Task<Order?> GetOrderByIdAsync(long orderId, CancellationToken token)
    {
        var order = await _appDbContext.Orders
            .Include(o => o.Items)
            .SingleOrDefaultAsync(o => o.Id == orderId, token);

        if (order is null)
            throw new InvalidOperationException($"Order {orderId} not found");
    
        return order;
    }

    public async Task SaveOrderAsync(Order order, CancellationToken token)
    {
        ArgumentNullException.ThrowIfNull(order);

        if (order.Items != null && !order.Items.Any())
            throw new InvalidOperationException("Order items are required");
        
        await _appDbContext.Orders.AddAsync(order, token);
        await _appDbContext.SaveChangesAsync(token);
    }

    public async Task<int> SaveToDatabaseAsync(CancellationToken token)
    {
        return await _appDbContext.SaveChangesAsync(token);
    }
}