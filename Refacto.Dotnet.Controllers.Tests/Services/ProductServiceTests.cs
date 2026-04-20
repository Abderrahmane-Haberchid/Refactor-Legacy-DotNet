using Moq;
using Refacto.DotNet.Controllers.Entities;
using Refacto.DotNet.Controllers.Enums;
using Refacto.DotNet.Controllers.Services;
using Refacto.DotNet.Controllers.Services.Impl;
using Refacto.DotNet.Controllers.Respository;

namespace Refacto.DotNet.Controllers.Tests.Services
{
    public class ProductServiceTests
    {
        private readonly Mock<INotificationService> _mockNotificationService;
        private readonly Mock<IOrderRepository> _orderRepository;
        private readonly IProductService _productService;

        public ProductServiceTests()
        {
            _mockNotificationService = new Mock<INotificationService>();
            _orderRepository = new Mock<IOrderRepository>();
            
            _productService = new ProductService(_mockNotificationService.Object, _orderRepository.Object);
        }

        [Fact]
        public async Task NotifyDelay_WhenProductOutOfStock_SavesAndSendsNotification()
        {
            // GIVEN
            var product = new Product
            {
                Id = 1,
                LeadTime = 15,
                Available = 0,
                Type = ProductType.NORMAL,
                Name = "RJ45 Cable"
            };

            // Setup SaveChangesAsync
            _orderRepository.Setup(x => x.SaveToDatabaseAsync(It.IsAny<CancellationToken>()));

            // WHEN
            await _productService.NotifyDelayAsync(product.LeadTime, product, CancellationToken.None);

            // THEN
            Assert.Equal(0, product.Available);
            Assert.Equal(15, product.LeadTime);
            _mockNotificationService.Verify(
                service => service.SendDelayNotification(product.LeadTime, product.Name), 
                Times.Once());
        }

        [Fact]
        public async Task HandleExpiredProduct_WhenProductExpired_SetsAvailableToZeroAndSendsNotification()
        {
            // GIVEN
            var product = new Product
            {
                Id = 1,
                LeadTime = 90,
                Available = 6,
                Type = ProductType.EXPIRABLE,
                Name = "Milk",
                ExpiryDate = DateTime.Now.AddDays(-2) // Expired
            };

            _ = _orderRepository.Setup(x => x.SaveToDatabaseAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(1);

            // WHEN
            await _productService.HandleExpiredProductAsync(product, CancellationToken.None);

            // THEN
            Assert.Equal(0, product.Available); // Should be set to 0
            _mockNotificationService.Verify(
                service => service.SendExpirationNotification(product.Name, (DateTime)product.ExpiryDate), 
                Times.Once());
        }

        [Fact]
        public async Task HandleExpiredProduct_WhenProductNotExpired_DecrementsAvailable()
        {
            // GIVEN
            var initialStock = 30;
            var product = new Product
            {
                Id = 2,
                LeadTime = 15,
                Available = initialStock,
                Type = ProductType.EXPIRABLE,
                Name = "Butter",
                ExpiryDate = DateTime.Now.AddDays(26) // Not expired
            };

            _ = _orderRepository.Setup(x => x.SaveToDatabaseAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(1);

            // WHEN
            await _productService.HandleExpiredProductAsync(product, CancellationToken.None);

            // THEN
            Assert.Equal(initialStock - 1, product.Available);
            _mockNotificationService.Verify(
                service => service.SendExpirationNotification(It.IsAny<string>(), It.IsAny<DateTime>()), 
                Times.Never());
        }

        [Fact]
        public async Task HandleSeasonalProduct_WhenInSeasonAndInStock_DoesNotSendNotification()
        {
            // GIVEN
            var product = new Product
            {
                Id = 3,
                LeadTime = 15,
                Available = 30,
                Type = ProductType.SEASONAL,
                Name = "Watermelon",
                SeasonStartDate = DateTime.Now.AddDays(-2),
                SeasonEndDate = DateTime.Now.AddDays(58)
            };

            _ = _orderRepository.Setup(x => x.SaveToDatabaseAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(1);

            // WHEN
            await _productService.HandleSeasonalProductAsync(product, CancellationToken.None);

            // THEN
            _mockNotificationService.Verify(
                service => service.SendOutOfStockNotification(It.IsAny<string>()), 
                Times.Never());
        }

        [Fact]
        public async Task HandleSeasonalProduct_WhenOutOfSeason_SendsOutOfStockNotification()
        {
            // GIVEN
            var product = new Product
            {
                Id = 4,
                LeadTime = 15,
                Available = 30,
                Type = ProductType.SEASONAL,
                Name = "Grapes",
                SeasonStartDate = DateTime.Now.AddDays(180),
                SeasonEndDate = DateTime.Now.AddDays(240)
            };

            _ = _orderRepository.Setup(x => x.SaveToDatabaseAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(1);

            // WHEN
            await _productService.HandleSeasonalProductAsync(product, CancellationToken.None);

            // THEN
            _mockNotificationService.Verify(
                service => service.SendOutOfStockNotification(product.Name), 
                Times.AtLeastOnce());
        }
    }
}
