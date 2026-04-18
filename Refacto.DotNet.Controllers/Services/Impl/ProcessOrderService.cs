using Microsoft.EntityFrameworkCore;
using Refacto.DotNet.Controllers.Database.Context;
using Refacto.DotNet.Controllers.Dtos.Response;
using Refacto.DotNet.Controllers.Entities;
using Refacto.DotNet.Controllers.Enums;
using Refacto.DotNet.Controllers.Respository;

namespace Refacto.DotNet.Controllers.Services.Impl;

public class ProcessOrderService : IProcessOrderService
{
    private readonly ILogger<ProcessOrderService> _logger;
    private readonly IProductService _productService;
    private readonly IOrderRepository _orderRepository;

    public ProcessOrderService(
        ILogger<ProcessOrderService> logger,
        IProductService productService,
        IOrderRepository orderRepository
        )
    {
        _logger = logger;
        _productService = productService;
        _orderRepository = orderRepository;
    }
    
    
    public async Task<ProcessOrderResponse> OrderProcessorAsync(long orderId, CancellationToken ct = default)
    {
        try
        {
            // 1. Validation, fail fast
            var order = await _orderRepository.GetOrderByIdAsync(orderId, ct);

            if (order is null)
                throw new KeyNotFoundException("Order not found");
        
            _logger.LogInformation($"Processing order {order}");
        
            if(order.Items != null && !order.Items.Any())
                throw new Exception("Items not found");
        
        
            // 2. Apply busness logic
            var products = order.Items;

            await CheckProductAvailabilityAsync(products, ct);

            return new ProcessOrderResponse(order.Id);
        }
        catch(Exception ex)
        {
            _logger.LogError(ex.Message);
            throw;
        }
    }

    private async Task CheckProductAvailabilityAsync(ICollection<Product> products, CancellationToken ct)
    {
        foreach (var product in products)
        {
            switch (product.Type)
            {
                case ProductType.NORMAL:
                    await HandleNormalProductAsync(product, ct);
                    break;
                case ProductType.SEASONAL:
                    await HandleSeasonalProductAsync(product, ct);
                    break;
                case ProductType.EXPIRABLE:
                    await HandleExpirableProductAsync(product, ct);
                    break;
                case null:
                    throw new NullReferenceException("Product not found");
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        await _orderRepository.SaveToDatabaseAsync(ct);
    }

    private async Task HandleNormalProductAsync(Product product, CancellationToken ct)
    {
        if (product.Available > 0)
        {
            product.Available -= 1;
            //_appDbContext.Entry(product).State = EntityState.Modified;
        }
        else if (product.LeadTime > 0)
        {
            await _productService.NotifyDelayAsync(product.LeadTime, product, ct);
        }
    }
    
    private async Task HandleSeasonalProductAsync(Product product, CancellationToken ct)
    {
        if (DateTime.Now.Date > product.SeasonStartDate && DateTime.Now.Date < product.SeasonEndDate && product.Available > 0)
        {
            product.Available -= 1;
        }
        await _productService.HandleSeasonalProductAsync(product, ct);
    }
    
    private async Task HandleExpirableProductAsync(Product product, CancellationToken ct)
    {
        // Decrement is duplicated, delegate full process to product service
        await _productService.HandleExpiredProductAsync(product, ct);
    }
}