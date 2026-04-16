
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Refacto.DotNet.Controllers.Database.Context;
using Refacto.DotNet.Controllers.Entities;
using Refacto.DotNet.Controllers.Enums;
using Refacto.DotNet.Controllers.Services;
using System.Net;
using System.Text.Json;

namespace Refacto.Dotnet.Controllers.Tests.Controllers
{
    [Collection("Sequential")]
    public class OrderControllerIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
    {
        private readonly WebApplicationFactory<Program> _factory;
        private readonly AppDbContext _context;
        private readonly Mock<INotificationService> _mockNotificationService;
        public OrderControllerIntegrationTests(WebApplicationFactory<Program> factory)
        {
            _mockNotificationService = new Mock<INotificationService>();

            _factory = factory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    // Remove existing DbContext
                    var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<AppDbContext>));
                    if (descriptor != null) services.Remove(descriptor);

                    // Add in-memory database
                    var dbName = $"InMemoryDbForTesting-{Guid.NewGuid()}";
                    services.AddDbContext<AppDbContext>(options =>
                        options.UseInMemoryDatabase(dbName));

                    // Replace notification service with mock
                    var notificationDescriptor = services.SingleOrDefault(s => s.ServiceType == typeof(INotificationService));
                    if (notificationDescriptor != null) services.Remove(notificationDescriptor);
                    services.AddSingleton(_mockNotificationService.Object);
                });
            });

            var scope = _factory.Services.CreateScope();
            _context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            _context.Database.EnsureCreated();
        }

        [Fact]
        public async Task ProcessOrder_WithValidOrder_ReturnsSuccess()
        {
            // Arrange
            var client = _factory.CreateClient();
            
            var products = CreateProducts();
            var order = CreateOrder(products);
            
            await _context.Products.AddRangeAsync(products);
            await _context.Orders.AddAsync(order);
            await _context.SaveChangesAsync();
            _context.ChangeTracker.Clear();

            // Act
            var response = await client.PostAsync($"/orders/{order.Id}/processOrder", null);
            
            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            
            var content = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<ProcessOrderResponse>(content, new JsonSerializerOptions 
            { 
                PropertyNameCaseInsensitive = true 
            });
            
            Assert.Equal(order.Id, result.id);
        }

        [Fact]
        public async Task ProcessOrder_WithNonExistentOrder_ReturnsBadRequest()
        {
            // Arrange
            var client = _factory.CreateClient();
            var nonExistentOrderId = 99999;

            // Act
            var response = await client.PostAsync($"/orders/{nonExistentOrderId}/processOrder", null);
            
            // Assert
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task ProcessOrder_WithNormalProductInStock_DecrementsAvailable()
        {
            // Arrange
            var client = _factory.CreateClient();
            var initialStock = 5;
            var product = new Product 
            { 
                LeadTime = 15, 
                Available = initialStock, 
                Type = ProductType.NORMAL, 
                Name = "USB Cable" 
            };
            var order = CreateOrder(new List<Product> { product });
            
            await _context.Products.AddAsync(product);
            await _context.Orders.AddAsync(order);
            await _context.SaveChangesAsync();
            _context.ChangeTracker.Clear();

            // Act
            var response = await client.PostAsync($"/orders/{order.Id}/processOrder", null);
            
            // Assert
            response.EnsureSuccessStatusCode();
            
            var updatedProduct = await _context.Products.FindAsync(product.Id);
            Assert.Equal(initialStock - 1, updatedProduct.Available);
        }

        [Fact]
        public async Task ProcessOrder_WithNormalProductOutOfStock_CallsNotification()
        {
            // Arrange
            var client = _factory.CreateClient();
            var product = new Product 
            { 
                LeadTime = 15, 
                Available = 0, 
                Type = ProductType.NORMAL, 
                Name = "USB Dongle" 
            };
            var order = CreateOrder(new List<Product> { product });
            
            await _context.Products.AddAsync(product);
            await _context.Orders.AddAsync(order);
            await _context.SaveChangesAsync();
            _context.ChangeTracker.Clear();

            // Act
            var response = await client.PostAsync($"/orders/{order.Id}/processOrder", null);
            
            // Assert
            response.EnsureSuccessStatusCode();
            _mockNotificationService.Verify(
                x => x.SendDelayNotification(It.IsAny<int>(), It.IsAny<string>()), 
                Times.AtLeastOnce);
        }

        [Fact]
        public async Task ProcessOrder_WithExpirableProductNotExpired_DecrementsAvailable()
        {
            // Arrange
            var client = _factory.CreateClient();
            var initialStock = 30;
            var product = new Product 
            { 
                LeadTime = 15, 
                Available = initialStock, 
                Type = ProductType.EXPIRABLE, 
                Name = "Butter",
                ExpiryDate = DateTime.Now.AddDays(26)
            };
            var order = CreateOrder(new List<Product> { product });
            
            await _context.Products.AddAsync(product);
            await _context.Orders.AddAsync(order);
            await _context.SaveChangesAsync();
            _context.ChangeTracker.Clear();

            // Act
            var response = await client.PostAsync($"/orders/{order.Id}/processOrder", null);
            
            // Assert
            response.EnsureSuccessStatusCode();
            
            var updatedProduct = await _context.Products.FindAsync(product.Id);
            Assert.Equal(initialStock - 1, updatedProduct.Available);
        }

        [Fact]
        public async Task ProcessOrder_WithExpiredProduct_SendsExpirationNotification()
        {
            // Arrange
            var client = _factory.CreateClient();
            var product = new Product 
            { 
                LeadTime = 90, 
                Available = 6, 
                Type = ProductType.EXPIRABLE, 
                Name = "Milk",
                ExpiryDate = DateTime.Now.AddDays(-2)
            };
            var order = CreateOrder(new List<Product> { product });
            
            await _context.Products.AddAsync(product);
            await _context.Orders.AddAsync(order);
            await _context.SaveChangesAsync();
            _context.ChangeTracker.Clear();

            // Act
            var response = await client.PostAsync($"/orders/{order.Id}/processOrder", null);
            
            // Assert
            response.EnsureSuccessStatusCode();
            _mockNotificationService.Verify(
                x => x.SendExpirationNotification(It.IsAny<string>(), It.IsAny<DateTime>()), 
                Times.AtLeastOnce);
        }

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
            return new Order { Id = new Random().Next(1, 999999), Items = products };  // id set manually
        }
        
        private record ProcessOrderResponse(long id);
    }
}
