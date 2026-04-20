using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;
using Refacto.DotNet.Controllers.Entities;
using Refacto.DotNet.Controllers.Enums;
using Refacto.DotNet.Controllers.Respository;
using Refacto.DotNet.Controllers.Services;
using Refacto.DotNet.Controllers.Services.Impl;

namespace Refacto.DotNet.Controllers.Tests.Services;

public class ProcessOrderServiceTest
{
    
    private readonly Mock<IOrderRepository> _orderRepositoryMoq;
    private readonly Mock<ILogger<ProcessOrderService>> _loggerMoq;
    private readonly Mock<IProductService> _productServiceMoq;
    private readonly ProcessOrderService _sut;

    public ProcessOrderServiceTest()
    {
        _orderRepositoryMoq = new Mock<IOrderRepository>();
        _loggerMoq = new Mock<ILogger<ProcessOrderService>>();
        _productServiceMoq = new Mock<IProductService>();
        
        _sut = new ProcessOrderService(_loggerMoq.Object, _productServiceMoq.Object, _orderRepositoryMoq.Object);
    }
    
    [Fact]
    public async Task OrderProcessorAsync_ShouldThrowKeyNotFound_WhenOrderIsNull()
    {
        //Arrange
        const long orderId = 5;

        _orderRepositoryMoq.Setup(s => s.GetOrderByIdAsync(orderId, CancellationToken.None))
            .Returns(Task.FromResult<Order>(null!)!);

        // Act + Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(async () => await _sut.OrderProcessorAsync(orderId, CancellationToken.None));
    }

    [Fact]
    public async Task OrderProcessorAsync_ShouldThrowException_WhenOrderItemsIsNull()
    {
        var order = new Order()
        {
            Id = 5,
            Items = new List<Product>()
        };
        
        _orderRepositoryMoq.Setup(s => s.GetOrderByIdAsync(order.Id, CancellationToken.None))
            .Returns(Task.FromResult(order)!);
        
        await Assert.ThrowsAsync<Exception>(async () => await _sut.OrderProcessorAsync(order.Id, CancellationToken.None));
    }

    [Fact]
    public async Task OrderProcessorAsync_ShouldReturnProcessOrderResponse_WhenProductProcessedSuccessfully()
    {
        // Arrange
        const long orderId = 5;
        var products = CreateProducts();
        var order = CreateOrder(products);
        
        _orderRepositoryMoq.Setup(s => s.GetOrderByIdAsync(orderId, CancellationToken.None))
            .Returns(Task.FromResult(order)!);
        
        // Act
        var result = await _sut.OrderProcessorAsync(orderId, CancellationToken.None);

        // Assert
        Assert.Equal(5, result.id);
    }
    
    
    // Method helpers
    private static List<Product> CreateProducts()
    {
        return new List<Product>
        {
            new Product { LeadTime = 15, Available = 30, Type = ProductType.NORMAL, Name = "USB Cable" },
            new Product { LeadTime = 10, Available = 0, Type = ProductType.NORMAL, Name = "USB Dongle" },
            new Product { LeadTime = 15, Available = 30, Type = ProductType.EXPIRABLE, Name = "Butter", ExpiryDate = DateTime.Now.AddDays(26) },
            new Product { LeadTime = 90, Available = 6, Type = ProductType.EXPIRABLE, Name = "Milk", ExpiryDate = DateTime.Now.AddDays(-2) },
            new Product { LeadTime = 15, Available = 30, Type = ProductType.SEASONAL, Name = "Watermelon", SeasonStartDate = DateTime.Now.AddDays(-2), SeasonEndDate = DateTime.Now.AddDays(58) },
            new Product { LeadTime = 15, Available = 30, Type = ProductType.SEASONAL, Name = "Grapes", SeasonStartDate = DateTime.Now.AddDays(180), SeasonEndDate = DateTime.Now.AddDays(240) }
        };
    }

    private static Order CreateOrder(ICollection<Product> products)
    {
        return new Order { Id = 5, Items = products };  
    }
        
    private record ProcessOrderResponse(long id);
}