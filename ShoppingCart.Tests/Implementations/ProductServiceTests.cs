using Xunit;
using ShoppingCart.Core.Implementations;

namespace ShoppingCart.Tests.Implementations;

/// <summary>
/// Unit tests for the ProductService class.
/// </summary>
public class ProductServiceTests
{
    [Fact]
    public void GetPrice_WithValidProduct_ReturnsCorrectPrice()
    {
        // Arrange
        var productService = new ProductService();

        // Act
        decimal price = productService.GetPrice("apple");

        // Assert
        Assert.Equal(0.50m, price);
    }

    [Fact]
    public void GetPrice_WithValidProductDifferentCase_ReturnsCorrectPrice()
    {
        // Arrange
        var productService = new ProductService();

        // Act
        decimal price = productService.GetPrice("APPLE");

        // Assert
        Assert.Equal(0.50m, price);
    }

    [Fact]
    public void GetPrice_WithInvalidProduct_ThrowsArgumentException()
    {
        // Arrange
        var productService = new ProductService();

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => productService.GetPrice("invalid_product"));
        Assert.Contains("Product 'invalid_product' not found", exception.Message);
    }

    [Fact]
    public void GetPrice_WithNullProduct_ThrowsArgumentException()
    {
        // Arrange
        var productService = new ProductService();

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => productService.GetPrice(null!));
        Assert.Contains("Product ID cannot be null or empty", exception.Message);
    }

    [Fact]
    public void GetPrice_WithEmptyProduct_ThrowsArgumentException()
    {
        // Arrange
        var productService = new ProductService();

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => productService.GetPrice(""));
        Assert.Contains("Product ID cannot be null or empty", exception.Message);
    }

    [Fact]
    public void GetPrice_WithWhitespaceProduct_ThrowsArgumentException()
    {
        // Arrange
        var productService = new ProductService();

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => productService.GetPrice("   "));
        Assert.Contains("Product ID cannot be null or empty", exception.Message);
    }

    [Theory]
    [InlineData("apple", 0.50)]
    [InlineData("banana", 0.30)]
    [InlineData("orange", 0.75)]
    [InlineData("bread", 2.50)]
    [InlineData("milk", 3.25)]
    [InlineData("cheese", 4.99)]
    [InlineData("chicken", 8.99)]
    [InlineData("rice", 1.99)]
    public void GetPrice_WithVariousProducts_ReturnsCorrectPrices(string productId, decimal expectedPrice)
    {
        // Arrange
        var productService = new ProductService();

        // Act
        decimal actualPrice = productService.GetPrice(productId);

        // Assert
        Assert.Equal(expectedPrice, actualPrice);
    }

    [Fact]
    public void ProductExists_WithValidProduct_ReturnsTrue()
    {
        // Arrange
        var productService = new ProductService();

        // Act
        bool exists = productService.ProductExists("apple");

        // Assert
        Assert.True(exists);
    }

    [Fact]
    public void ProductExists_WithValidProductDifferentCase_ReturnsTrue()
    {
        // Arrange
        var productService = new ProductService();

        // Act
        bool exists = productService.ProductExists("APPLE");

        // Assert
        Assert.True(exists);
    }

    [Fact]
    public void ProductExists_WithInvalidProduct_ReturnsFalse()
    {
        // Arrange
        var productService = new ProductService();

        // Act
        bool exists = productService.ProductExists("invalid_product");

        // Assert
        Assert.False(exists);
    }

    [Fact]
    public void ProductExists_WithNullProduct_ReturnsFalse()
    {
        // Arrange
        var productService = new ProductService();

        // Act
        bool exists = productService.ProductExists(null!);

        // Assert
        Assert.False(exists);
    }

    [Fact]
    public void ProductExists_WithEmptyProduct_ReturnsFalse()
    {
        // Arrange
        var productService = new ProductService();

        // Act
        bool exists = productService.ProductExists("");

        // Assert
        Assert.False(exists);
    }

    [Fact]
    public void GetAllProducts_ReturnsAllProducts()
    {
        // Arrange
        var productService = new ProductService();

        // Act
        var products = productService.GetAllProducts();

        // Assert
        Assert.NotEmpty(products);
        Assert.True(products.Count >= 8); // At least 8 products
        Assert.True(products.ContainsKey("apple"));
        Assert.True(products.ContainsKey("banana"));
        Assert.True(products.ContainsKey("bread"));
        Assert.Equal(0.50m, products["apple"]);
        Assert.Equal(0.30m, products["banana"]);
        Assert.Equal(2.50m, products["bread"]);
    }

    [Fact]
    public void GetAllProducts_ReturnsDefensiveCopy()
    {
        // Arrange
        var productService = new ProductService();

        // Act
        var products1 = productService.GetAllProducts();
        var products2 = productService.GetAllProducts();
        products1["apple"] = 999.99m; // Modify the returned dictionary

        // Assert
        Assert.NotEqual(products1["apple"], products2["apple"]);
        Assert.Equal(0.50m, products2["apple"]); // Original value should be unchanged
    }
} 