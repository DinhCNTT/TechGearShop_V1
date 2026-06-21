using Moq;
using TechGearShop_V1.Models.Entities;
using TechGearShop_V1.Models.ViewModels;
using TechGearShop_V1.Repositories.Interfaces;
using TechGearShop_V1.Services;
using TechGearShop_V1.Services.Interfaces;
using Xunit;

namespace TechGearShop_V1.Tests.Services
{
    public class CartServiceTests
    {
        private readonly Mock<ICartRepository> _mockCartRepo;
        private readonly Mock<IProductService> _mockProductService;
        private readonly CartService _cartService;

        public CartServiceTests()
        {
            _mockCartRepo = new Mock<ICartRepository>();
            _mockProductService = new Mock<IProductService>();
            _cartService = new CartService(_mockCartRepo.Object, _mockProductService.Object);
        }

        [Fact]
        public async Task GetCartItemsAsync_ReturnsEmptyList_WhenCartIsNull()
        {
            // Arrange
            int userId = 1;
            _mockCartRepo.Setup(r => r.GetCartByUserIdAsync(userId))
                         .ReturnsAsync((CartEntity?)null);

            // Act
            var result = await _cartService.GetCartItemsAsync(userId);

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result);
        }

        [Fact]
        public async Task GetCartItemsAsync_ReturnsCartItemsWithCorrectPrices_WhenCartHasItems()
        {
            // Arrange
            int userId = 1;
            var product1 = new Product { Id = 101, Name = "Mouse", Price = 100, PromotionalPrice = 80, Stock = 5, IsActive = true };
            var product2 = new Product { Id = 102, Name = "Keyboard", Price = 200, PromotionalPrice = null, Stock = 1, IsActive = true };

            var cartEntity = new CartEntity
            {
                UserId = userId,
                Items = new List<CartItemEntity>
                {
                    new CartItemEntity { ProductId = 101, Product = product1, Quantity = 2 },
                    new CartItemEntity { ProductId = 102, Product = product2, Quantity = 2 } // Quantity 2 > Stock 1 -> Out of stock
                }
            };

            _mockCartRepo.Setup(r => r.GetCartByUserIdAsync(userId))
                         .ReturnsAsync(cartEntity);

            // Act
            var result = await _cartService.GetCartItemsAsync(userId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Count);

            // Check product 1 (promotional price applied)
            var item1 = result.First(i => i.ProductId == 101);
            Assert.Equal(80, item1.Price);
            Assert.False(item1.IsOutOfStock);

            // Check product 2 (normal price applied, out of stock flag true)
            var item2 = result.First(i => i.ProductId == 102);
            Assert.Equal(200, item2.Price);
            Assert.True(item2.IsOutOfStock);
        }

        [Fact]
        public async Task AddItemAsync_ReturnsError_WhenProductDoesNotExist()
        {
            // Arrange
            int userId = 1;
            int productId = 999;
            _mockProductService.Setup(s => s.GetProductByIdAsync(productId))
                               .ReturnsAsync((Product?)null);

            // Act
            var result = await _cartService.AddItemAsync(userId, productId, 1);

            // Assert
            Assert.False(result.Success);
            Assert.Equal("Sản phẩm không tồn tại hoặc ngừng kinh doanh.", result.Message);
            _mockCartRepo.Verify(r => r.AddOrUpdateItemAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<decimal>()), Times.Never);
        }

        [Fact]
        public async Task AddItemAsync_ReturnsError_WhenProductIsInactive()
        {
            // Arrange
            int userId = 1;
            int productId = 101;
            var inactiveProduct = new Product { Id = productId, Name = "Old Screen", Price = 150, Stock = 10, IsActive = false };
            _mockProductService.Setup(s => s.GetProductByIdAsync(productId))
                               .ReturnsAsync(inactiveProduct);

            // Act
            var result = await _cartService.AddItemAsync(userId, productId, 1);

            // Assert
            Assert.False(result.Success);
            Assert.Equal("Sản phẩm không tồn tại hoặc ngừng kinh doanh.", result.Message);
            _mockCartRepo.Verify(r => r.AddOrUpdateItemAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<decimal>()), Times.Never);
        }

        [Fact]
        public async Task AddItemAsync_ReturnsError_WhenQuantityExceedsStock()
        {
            // Arrange
            int userId = 1;
            int productId = 101;
            var product = new Product { Id = productId, Name = "RAM", Price = 50, Stock = 5, IsActive = true };
            _mockProductService.Setup(s => s.GetProductByIdAsync(productId))
                               .ReturnsAsync(product);

            // Mock current cart having 3 items of this product
            var cartEntity = new CartEntity
            {
                UserId = userId,
                Items = new List<CartItemEntity>
                {
                    new CartItemEntity { ProductId = productId, Product = product, Quantity = 3 }
                }
            };
            _mockCartRepo.Setup(r => r.GetCartByUserIdAsync(userId))
                         .ReturnsAsync(cartEntity);

            // Act: Adding 3 more items (Total: 3 + 3 = 6, which exceeds stock of 5)
            var result = await _cartService.AddItemAsync(userId, productId, 3);

            // Assert
            Assert.False(result.Success);
            Assert.Equal("Xin lỗi, kho chỉ còn 5 sản phẩm.", result.Message);
            _mockCartRepo.Verify(r => r.AddOrUpdateItemAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<decimal>()), Times.Never);
        }

        [Fact]
        public async Task AddItemAsync_CallsRepoAndReturnsSuccess_WhenQuantityIsValid()
        {
            // Arrange
            int userId = 1;
            int productId = 101;
            var product = new Product { Id = productId, Name = "SSD", Price = 100, PromotionalPrice = 90, Stock = 10, IsActive = true };
            _mockProductService.Setup(s => s.GetProductByIdAsync(productId))
                               .ReturnsAsync(product);

            // Mock sequence for GetCartByUserIdAsync: first call returns null (empty cart), second call returns cart with 1 item
            var cartWithOneItem = new CartEntity
            {
                UserId = userId,
                Items = new List<CartItemEntity> { new CartItemEntity { ProductId = productId, Quantity = 2 } }
            };
            _mockCartRepo.SetupSequence(r => r.GetCartByUserIdAsync(userId))
                         .ReturnsAsync((CartEntity?)null)
                         .ReturnsAsync(cartWithOneItem);

            // Act
            var result = await _cartService.AddItemAsync(userId, productId, 2);

            // Assert
            Assert.True(result.Success);
            Assert.Equal("Đã thêm SSD vào giỏ!", result.Message);
            Assert.Equal(1, result.CartCount);
            
            // Verify repository is called with promotional price
            _mockCartRepo.Verify(r => r.AddOrUpdateItemAsync(userId, productId, 2, 90), Times.Once);
        }

        [Fact]
        public async Task UpdateQuantityAsync_RemovesItem_WhenQuantityIsZeroOrNegative()
        {
            // Arrange
            int userId = 1;
            int productId = 101;

            // Act (Zero quantity)
            var result0 = await _cartService.UpdateQuantityAsync(userId, productId, 0);

            // Act (Negative quantity)
            var resultNegative = await _cartService.UpdateQuantityAsync(userId, productId, -5);

            // Assert
            Assert.Null(result0);
            Assert.Null(resultNegative);
            _mockCartRepo.Verify(r => r.RemoveItemAsync(userId, productId), Times.Exactly(2));
        }

        [Fact]
        public async Task UpdateQuantityAsync_CapsQuantityToStockAndReturnsWarning_WhenQuantityExceedsStock()
        {
            // Arrange
            int userId = 1;
            int productId = 101;
            var product = new Product { Id = productId, Name = "GPU", Stock = 2, IsActive = true };
            _mockProductService.Setup(s => s.GetProductByIdAsync(productId))
                               .ReturnsAsync(product);

            // Act: Request 5 units, but stock is only 2
            var result = await _cartService.UpdateQuantityAsync(userId, productId, 5);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("Kho chỉ còn tối đa 2 sản phẩm.", result);
            _mockCartRepo.Verify(r => r.UpdateItemQuantityAsync(userId, productId, 2), Times.Once);
        }

        [Fact]
        public async Task UpdateQuantityAsync_UpdatesQuantityAndReturnsNull_WhenQuantityIsValid()
        {
            // Arrange
            int userId = 1;
            int productId = 101;
            var product = new Product { Id = productId, Name = "GPU", Stock = 10, IsActive = true };
            _mockProductService.Setup(s => s.GetProductByIdAsync(productId))
                               .ReturnsAsync(product);

            // Act: Request 4 units (valid)
            var result = await _cartService.UpdateQuantityAsync(userId, productId, 4);

            // Assert
            Assert.Null(result);
            _mockCartRepo.Verify(r => r.UpdateItemQuantityAsync(userId, productId, 4), Times.Once);
        }
    }
}
