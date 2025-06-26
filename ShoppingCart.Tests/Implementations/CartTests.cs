using Xunit;
using ShoppingCart.Core.Implementations;
using ShoppingCart.Core.Interfaces;

namespace ShoppingCart.Tests.Implementations;

/// <summary>
/// Unit tests for the Cart class.
/// </summary>
public class CartTests
{
    private readonly IProductService _productService;
    private readonly Cart _cart;

    public CartTests()
    {
        _productService = new ProductService();
        _cart = new Cart(_productService);
    }

    [Fact]
    public void Constructor_WithNullProductService_ThrowsArgumentNullException()
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentNullException>(() => new Cart(null!));
        Assert.Equal("productService", exception.ParamName);
    }

    [Fact]
    public void Constructor_WithValidProductService_CreatesEmptyCart()
    {
        // Arrange & Act
        var cart = new Cart(_productService);

        // Assert
        Assert.Empty(cart.Items());
        Assert.Equal(0m, cart.Total());
    }

    [Fact]
    public void AddItem_WithValidProduct_AddsItemToCart()
    {
        // Act
        _cart.AddItem("apple", 2);

        // Assert
        var items = _cart.Items();
        Assert.Single(items);
        Assert.Equal(2, items["apple"]);
        Assert.Equal(1.00m, _cart.Total()); // 2 * 0.50
    }

    [Fact]
    public void AddItem_WithMultipleProducts_AddsAllItems()
    {
        // Act
        _cart.AddItem("apple", 2);
        _cart.AddItem("banana", 3);
        _cart.AddItem("bread", 1);

        // Assert
        var items = _cart.Items();
        Assert.Equal(3, items.Count);
        Assert.Equal(2, items["apple"]);
        Assert.Equal(3, items["banana"]);
        Assert.Equal(1, items["bread"]);
        Assert.Equal(4.40m, _cart.Total()); // (2*0.50) + (3*0.30) + (1*2.50)
    }

    [Fact]
    public void AddItem_WithSameProductTwice_AccumulatesQuantity()
    {
        // Act
        _cart.AddItem("apple", 2);
        _cart.AddItem("apple", 3);

        // Assert
        var items = _cart.Items();
        Assert.Single(items);
        Assert.Equal(5, items["apple"]);
        Assert.Equal(2.50m, _cart.Total()); // 5 * 0.50
    }

    [Fact]
    public void AddItem_WithDifferentCase_AccumulatesQuantity()
    {
        // Act
        _cart.AddItem("Apple", 2);
        _cart.AddItem("APPLE", 3);

        // Assert
        var items = _cart.Items();
        Assert.Single(items);
        Assert.Equal(5, items["apple"]);
        Assert.Equal(2.50m, _cart.Total()); // 5 * 0.50
    }

    [Fact]
    public void AddItem_WithNullProductId_ThrowsArgumentException()
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => _cart.AddItem(null!, 1));
        Assert.Contains("Product ID cannot be null or empty", exception.Message);
        Assert.Equal("productId", exception.ParamName);
    }

    [Fact]
    public void AddItem_WithEmptyProductId_ThrowsArgumentException()
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => _cart.AddItem("", 1));
        Assert.Contains("Product ID cannot be null or empty", exception.Message);
        Assert.Equal("productId", exception.ParamName);
    }

    [Fact]
    public void AddItem_WithWhitespaceProductId_ThrowsArgumentException()
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => _cart.AddItem("   ", 1));
        Assert.Contains("Product ID cannot be null or empty", exception.Message);
        Assert.Equal("productId", exception.ParamName);
    }

    [Fact]
    public void AddItem_WithZeroQuantity_ThrowsArgumentException()
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => _cart.AddItem("apple", 0));
        Assert.Contains("Quantity must be greater than zero", exception.Message);
        Assert.Equal("quantity", exception.ParamName);
    }

    [Fact]
    public void AddItem_WithNegativeQuantity_ThrowsArgumentException()
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => _cart.AddItem("apple", -1));
        Assert.Contains("Quantity must be greater than zero", exception.Message);
        Assert.Equal("quantity", exception.ParamName);
    }

    [Fact]
    public void AddItem_WithUnknownProduct_ThrowsArgumentException()
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => _cart.AddItem("unknown_product", 1));
        Assert.Contains("Product 'unknown_product' not found", exception.Message);
        Assert.Equal("productId", exception.ParamName);
    }

    [Fact]
    public void RemoveItem_WithExistingItem_RemovesItem()
    {
        // Arrange
        _cart.AddItem("apple", 2);
        _cart.AddItem("banana", 3);

        // Act
        _cart.RemoveItem("apple");

        // Assert
        var items = _cart.Items();
        Assert.Single(items);
        Assert.False(items.ContainsKey("apple"));
        Assert.Equal(3, items["banana"]);
        Assert.Equal(0.90m, _cart.Total()); // 3 * 0.30
    }

    [Fact]
    public void RemoveItem_WithDifferentCase_RemovesItem()
    {
        // Arrange
        _cart.AddItem("apple", 2);

        // Act
        _cart.RemoveItem("APPLE");

        // Assert
        var items = _cart.Items();
        Assert.Empty(items);
        Assert.Equal(0m, _cart.Total());
    }

    [Fact]
    public void RemoveItem_WithNonExistentItem_DoesNothing()
    {
        // Arrange
        _cart.AddItem("apple", 2);

        // Act
        _cart.RemoveItem("banana"); // Doesn't exist

        // Assert
        var items = _cart.Items();
        Assert.Single(items);
        Assert.Equal(2, items["apple"]);
        Assert.Equal(1.00m, _cart.Total());
    }

    [Fact]
    public void RemoveItem_WithNullProductId_ThrowsArgumentException()
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => _cart.RemoveItem(null!));
        Assert.Contains("Product ID cannot be null or empty", exception.Message);
        Assert.Equal("productId", exception.ParamName);
    }

    [Fact]
    public void RemoveItem_WithEmptyProductId_ThrowsArgumentException()
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => _cart.RemoveItem(""));
        Assert.Contains("Product ID cannot be null or empty", exception.Message);
        Assert.Equal("productId", exception.ParamName);
    }

    [Fact]
    public void Total_WithEmptyCart_ReturnsZero()
    {
        // Act & Assert
        Assert.Equal(0m, _cart.Total());
    }

    [Fact]
    public void Total_WithMultipleItems_CalculatesCorrectTotal()
    {
        // Arrange
        _cart.AddItem("apple", 2);    // 2 * 0.50 = 1.00
        _cart.AddItem("banana", 3);   // 3 * 0.30 = 0.90
        _cart.AddItem("bread", 1);    // 1 * 2.50 = 2.50
        _cart.AddItem("milk", 2);     // 2 * 3.25 = 6.50

        // Act & Assert
        Assert.Equal(10.90m, _cart.Total());
    }

    [Fact]
    public void Items_WithEmptyCart_ReturnsEmptyDictionary()
    {
        // Act
        var items = _cart.Items();

        // Assert
        Assert.NotNull(items);
        Assert.Empty(items);
    }

    [Fact]
    public void Items_ReturnsDefensiveCopy()
    {
        // Arrange
        _cart.AddItem("apple", 2);
        _cart.AddItem("banana", 3);

        // Act
        var items = _cart.Items();
        items["apple"] = 10; // Modify the returned dictionary

        // Assert
        var originalItems = _cart.Items();
        Assert.Equal(2, originalItems["apple"]); // Original should be unchanged
    }

    [Theory]
    [InlineData("apple", 1, 0.50)]
    [InlineData("banana", 2, 0.60)]
    [InlineData("bread", 3, 7.50)]
    [InlineData("milk", 1, 3.25)]
    public void AddItem_WithVariousProductsAndQuantities_CalculatesCorrectTotals(string productId, int quantity, decimal expectedTotal)
    {
        // Act
        _cart.AddItem(productId, quantity);

        // Assert
        Assert.Equal(expectedTotal, _cart.Total());
    }

    [Fact]
    public void IntegrationTest_CompleteCartWorkflow()
    {
        // Arrange & Act
        _cart.AddItem("apple", 5);
        _cart.AddItem("banana", 3);
        _cart.AddItem("bread", 2);
        _cart.AddItem("milk", 1);

        // Verify initial state
        Assert.Equal(4, _cart.Items().Count);
        Assert.Equal(11.65m, _cart.Total()); // (5*0.50) + (3*0.30) + (2*2.50) + (1*3.25)

        // Add more of existing item
        _cart.AddItem("apple", 2);
        Assert.Equal(7, _cart.Items()["apple"]);
        Assert.Equal(12.65m, _cart.Total()); // Previous + (2*0.50)

        // Remove an item
        _cart.RemoveItem("banana");
        Assert.Equal(3, _cart.Items().Count);
        Assert.Equal(11.75m, _cart.Total()); // Previous - (3*0.30)

        // Verify final state
        var finalItems = _cart.Items();
        Assert.Equal(7, finalItems["apple"]);
        Assert.Equal(2, finalItems["bread"]);
        Assert.Equal(1, finalItems["milk"]);
        Assert.False(finalItems.ContainsKey("banana"));
    }
} 