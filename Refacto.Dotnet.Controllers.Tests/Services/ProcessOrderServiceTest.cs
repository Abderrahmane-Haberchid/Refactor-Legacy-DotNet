using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;
using Refacto.DotNet.Controllers.Entities;
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
        long orderId = 5; 
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
}