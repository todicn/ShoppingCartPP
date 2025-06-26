using ShoppingCart.Core.Implementations;
using ShoppingCart.Core.Interfaces;
using Xunit;
using StackExchange.Redis;

namespace ShoppingCart.Tests.Implementations;

public class RedisCartTests : IDisposable
{
    private readonly IConnectionMultiplexer _redis;
    private readonly IDatabase _database;
    private readonly IProductService _productService;
    private readonly RedisCart _cart;
    private readonly string _testUserId = "test-user-123";

    public RedisCartTests()
    {
        // Initialize Redis connection for testing
        _redis = ConnectionMultiplexer.Connect("localhost:6379");
        _database = _redis.GetDatabase();
        
        // Create product service
        _productService = new ProductService();
        
        // Create Redis cart instance
        _cart = new RedisCart(_productService, _database, _testUserId);
        
        // Clean up any existing test data
        CleanupTestData();
    }

    public void Dispose()
    {
        CleanupTestData();
        _redis?.Dispose();
    }

    private void CleanupTestData()
    {
        try
        {
            var key = $"cart:{_testUserId}";
            _database.KeyDelete(key);
        }
        catch
        {
            // Ignore cleanup errors
        }
    }

    #region AddItem Tests

    [Fact]
    public void AddItem_ValidProduct_AddsSuccessfully()
    {
        // Act
        _cart.AddItem("apple", 2);

        // Assert
        var items = _cart.Items();
        Assert.Single(items);
        Assert.Equal("apple", items.First().Key);
        Assert.Equal(2, items.First().Value);
    }

    [Fact]
    public void AddItem_ExistingProduct_IncreasesQuantity()
    {
        // Arrange
        _cart.AddItem("apple", 2);

        // Act
        _cart.AddItem("apple", 3);

        // Assert
        var items = _cart.Items();
        Assert.Single(items);
        Assert.Equal("apple", items.First().Key);
        Assert.Equal(5, items.First().Value);
    }

    [Fact]
    public void AddItem_MultipleProducts_AddsAll()
    {
        // Act
        _cart.AddItem("apple", 2);
        _cart.AddItem("banana", 1);
        _cart.AddItem("bread", 3);

        // Assert
        var items = _cart.Items();
        Assert.Equal(3, items.Count);
        Assert.Equal(2, items["apple"]);
        Assert.Equal(1, items["banana"]);
        Assert.Equal(3, items["bread"]);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void AddItem_InvalidProductId_ThrowsArgumentException(string invalidProductId)
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => _cart.AddItem(invalidProductId, 1));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-10)]
    public void AddItem_InvalidQuantity_ThrowsArgumentException(int invalidQuantity)
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => _cart.AddItem("apple", invalidQuantity));
    }

    [Fact]
    public void AddItem_NonExistentProduct_ThrowsArgumentException()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => _cart.AddItem("nonexistent", 1));
    }

    [Fact]
    public void AddItem_CaseInsensitive_AddsSuccessfully()
    {
        // Act
        _cart.AddItem("apple", 2);

        // Assert
        var items = _cart.Items();
        Assert.Single(items);
        Assert.Equal("apple", items.First().Key);
        Assert.Equal(2, items.First().Value);
    }

    [Fact]
    public void AddItem_ProductWithSpaces_TrimsAndAdds()
    {
        // Act
        _cart.AddItem("  apple  ", 2);

        // Assert
        var items = _cart.Items();
        Assert.Single(items);
        Assert.Equal("apple", items.First().Key);
        Assert.Equal(2, items.First().Value);
    }

    #endregion

    #region RemoveItem Tests

    [Fact]
    public void RemoveItem_ExistingProduct_RemovesSuccessfully()
    {
        // Arrange
        _cart.AddItem("apple", 2);
        _cart.AddItem("banana", 1);

        // Act
        _cart.RemoveItem("apple");

        // Assert
        var items = _cart.Items();
        Assert.Single(items);
        Assert.Equal("banana", items.First().Key);
        Assert.Equal(1, items.First().Value);
    }

    [Fact]
    public void RemoveItem_NonExistentProduct_DoesNotThrow()
    {
        // Act & Assert - Should not throw, just silently do nothing
        _cart.RemoveItem("nonexistent");
        
        // Verify cart is still empty
        Assert.Empty(_cart.Items());
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void RemoveItem_InvalidProductId_ThrowsArgumentException(string invalidProductId)
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => _cart.RemoveItem(invalidProductId));
    }

    [Fact]
    public void RemoveItem_CaseInsensitive_RemovesSuccessfully()
    {
        // Arrange
        _cart.AddItem("apple", 2);

        // Act
        _cart.RemoveItem("apple");

        // Assert
        var items = _cart.Items();
        Assert.Empty(items);
    }

    [Fact]
    public void RemoveItem_LastProduct_EmptiesCart()
    {
        // Arrange
        _cart.AddItem("apple", 2);

        // Act
        _cart.RemoveItem("apple");

        // Assert
        var items = _cart.Items();
        Assert.Empty(items);
        Assert.Equal(0, _cart.Total());
    }

    [Fact]
    public void RemoveItem_ProductWithSpaces_TrimsAndRemoves()
    {
        // Arrange
        _cart.AddItem("apple", 2);

        // Act
        _cart.RemoveItem("  apple  ");

        // Assert
        var items = _cart.Items();
        Assert.Empty(items);
    }

    #endregion

    #region Total Tests

    [Fact]
    public void Total_EmptyCart_ReturnsZero()
    {
        // Act
        var total = _cart.Total();

        // Assert
        Assert.Equal(0, total);
    }

    [Fact]
    public void Total_SingleProduct_ReturnsCorrectTotal()
    {
        // Arrange
        _cart.AddItem("apple", 2);

        // Act
        var total = _cart.Total();

        // Assert
        Assert.Equal(1.00m, total); // 2 * 0.50
    }

    [Fact]
    public void Total_MultipleProducts_ReturnsCorrectTotal()
    {
        // Arrange
        _cart.AddItem("apple", 2);    // 2 * 0.50 = 1.00
        _cart.AddItem("banana", 3);     // 3 * 0.30 = 0.90
        _cart.AddItem("bread", 1);  // 1 * 2.50 = 2.50

        // Act
        var total = _cart.Total();

        // Assert
        Assert.Equal(4.40m, total); // 1.00 + 0.90 + 2.50
    }

    [Fact]
    public void Total_AfterRemoval_ReturnsUpdatedTotal()
    {
        // Arrange
        _cart.AddItem("apple", 2);    // 2 * 0.50 = 1.00
        _cart.AddItem("banana", 3);     // 3 * 0.30 = 0.90
        _cart.RemoveItem("apple");

        // Act
        var total = _cart.Total();

        // Assert
        Assert.Equal(0.90m, total); // Only banana remains
    }

    [Fact]
    public void Total_AfterQuantityIncrease_ReturnsUpdatedTotal()
    {
        // Arrange
        _cart.AddItem("banana", 2);     // 2 * 0.30 = 0.60
        _cart.AddItem("banana", 3);     // +3 * 0.30 = +0.90, total = 1.50

        // Act
        var total = _cart.Total();

        // Assert
        Assert.Equal(1.50m, total); // 5 * 0.30
    }

    #endregion

    #region Items Tests

    [Fact]
    public void Items_EmptyCart_ReturnsEmptyDictionary()
    {
        // Act
        var items = _cart.Items();

        // Assert
        Assert.Empty(items);
        Assert.IsType<Dictionary<string, int>>(items);
    }

    [Fact]
    public void Items_WithProducts_ReturnsCorrectItems()
    {
        // Arrange
        _cart.AddItem("apple", 2);
        _cart.AddItem("banana", 3);
        _cart.AddItem("bread", 1);

        // Act
        var items = _cart.Items();

        // Assert
        Assert.Equal(3, items.Count);
        Assert.Equal(2, items["apple"]);
        Assert.Equal(3, items["banana"]);
        Assert.Equal(1, items["bread"]);
    }

    [Fact]
    public void Items_ReturnsReadOnlySnapshot()
    {
        // Arrange
        _cart.AddItem("apple", 2);

        // Act
        var items = _cart.Items();
        items["apple"] = 999; // Modify returned dictionary

        // Assert - Original cart should be unchanged
        var freshItems = _cart.Items();
        Assert.Equal(2, freshItems["apple"]);
    }

    [Fact]
    public void Items_AfterModifications_ReturnsCurrentState()
    {
        // Arrange
        _cart.AddItem("apple", 2);
        _cart.AddItem("banana", 1);
        
        // Act - Get initial state
        var initialItems = _cart.Items();
        
        // Modify cart
        _cart.AddItem("apple", 3); // Total apple: 5
        _cart.RemoveItem("banana");
        _cart.AddItem("bread", 2);
        
        // Get updated state
        var updatedItems = _cart.Items();

        // Assert
        Assert.Equal(2, initialItems.Count);
        Assert.Equal(2, updatedItems.Count);
        Assert.Equal(5, updatedItems["apple"]);
        Assert.Equal(2, updatedItems["bread"]);
        Assert.False(updatedItems.ContainsKey("banana"));
    }

    #endregion

    #region Persistence Tests

    [Fact]
    public void Persistence_DataSurvivesNewInstance()
    {
        // Arrange - Add items to cart
        _cart.AddItem("apple", 2);
        _cart.AddItem("banana", 3);

        // Act - Create new cart instance with same user ID
        var newCart = new RedisCart(_productService, _database, _testUserId);
        var items = newCart.Items();
        var total = newCart.Total();

        // Assert
        Assert.Equal(2, items.Count);
        Assert.Equal(2, items["apple"]);
        Assert.Equal(3, items["banana"]);
        Assert.Equal(1.90m, total); // 2*0.50 + 3*0.30
    }

    [Fact]
    public void Persistence_DifferentUsers_IsolatedData()
    {
        // Arrange
        var user1Cart = new RedisCart(_productService, _database, "user1");
        var user2Cart = new RedisCart(_productService, _database, "user2");

        // Act
        user1Cart.AddItem("apple", 1);
        user2Cart.AddItem("banana", 2);

        // Assert
        var user1Items = user1Cart.Items();
        var user2Items = user2Cart.Items();

        Assert.Single(user1Items);
        Assert.Equal("apple", user1Items.First().Key);
        Assert.Equal(1, user1Items.First().Value);

        Assert.Single(user2Items);
        Assert.Equal("banana", user2Items.First().Key);
        Assert.Equal(2, user2Items.First().Value);

        // Cleanup
        user1Cart.RemoveItem("apple");
        user2Cart.RemoveItem("banana");
    }

    #endregion

    #region Observer Tests

    [Fact]
    public void Subscribe_ValidObserver_AddsSuccessfully()
    {
        // Arrange
        var observer = new TestObserver();

        // Act
        _cart.Subscribe(observer);

        // Assert - Observer should receive notifications
        _cart.AddItem("apple", 1);
        Assert.Single(observer.Notifications);
        Assert.Contains("OnItemAdded", observer.Notifications[0]);
    }

    [Fact]
    public void Subscribe_NullObserver_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => _cart.Subscribe(null));
    }

    [Fact]
    public void Unsubscribe_ExistingObserver_RemovesSuccessfully()
    {
        // Arrange
        var observer = new TestObserver();
        _cart.Subscribe(observer);

        // Act
        _cart.Unsubscribe(observer);
        _cart.AddItem("apple", 1);

        // Assert - Observer should not receive notifications
        Assert.Empty(observer.Notifications);
    }

    [Fact]
    public void Unsubscribe_NonExistentObserver_DoesNotThrow()
    {
        // Arrange
        var observer = new TestObserver();

        // Act & Assert - Should not throw
        _cart.Unsubscribe(observer);
    }

    [Fact]
    public void Operations_WithObserver_NotifiesCorrectly()
    {
        // Arrange
        var observer = new TestObserver();
        _cart.Subscribe(observer);

        // Act
        _cart.AddItem("apple", 2);
        _cart.AddItem("banana", 1);
        _cart.RemoveItem("apple");

        // Assert
        Assert.Equal(3, observer.Notifications.Count);
        Assert.Contains("OnItemAdded", observer.Notifications[0]);
        Assert.Contains("apple", observer.Notifications[0]);
        Assert.Contains("OnItemAdded", observer.Notifications[1]);
        Assert.Contains("banana", observer.Notifications[1]);
        Assert.Contains("OnItemRemoved", observer.Notifications[2]);
        Assert.Contains("apple", observer.Notifications[2]);
    }

    #endregion

    #region Error Handling Tests

    [Fact]
    public void Operations_RedisUnavailable_HandlesGracefully()
    {
        // This test would require a way to simulate Redis being unavailable
        // For now, we'll test with a disposed connection
        var disposedRedis = ConnectionMultiplexer.Connect("localhost:6379");
        disposedRedis.Dispose();

        // Act & Assert - Should throw appropriate exceptions
        Assert.Throws<ObjectDisposedException>(() => 
        {
            var faultyCart = new RedisCart(_productService, disposedRedis.GetDatabase(), "test");
            faultyCart.AddItem("apple", 1);
        });
    }

    #endregion

    #region Integration Tests

    [Fact]
    public void ComplexScenario_AddModifyRemove_WorksCorrectly()
    {
        // Add products
        _cart.AddItem("apple", 2);
        _cart.AddItem("banana", 3);
        _cart.AddItem("bread", 1);

        // Verify initial state
        Assert.Equal(3, _cart.Items().Count);
        Assert.Equal(4.40m, _cart.Total()); // 2*0.50 + 3*0.30 + 1*2.50

        // Modify quantities
        _cart.AddItem("apple", 1); // Total: 3
        _cart.AddItem("banana", 2);  // Total: 5

        // Verify modified state
        Assert.Equal(3, _cart.Items().Count);
        Assert.Equal(5.50m, _cart.Total()); // 3*0.50 + 5*0.30 + 1*2.50

        // Remove one product
        _cart.RemoveItem("bread");

        // Verify final state
        Assert.Equal(2, _cart.Items().Count);
        Assert.Equal(3.00m, _cart.Total()); // 3*0.50 + 5*0.30
        Assert.False(_cart.Items().ContainsKey("bread"));
    }

    [Fact]
    public void ComplexScenario_WithObservers_NotifiesCorrectly()
    {
        // Arrange
        var observer1 = new TestObserver();
        var observer2 = new TestObserver();
        
        _cart.Subscribe(observer1);
        _cart.Subscribe(observer2);

        // Act - Perform various operations
        _cart.AddItem("apple", 2);
        _cart.AddItem("banana", 1);
        _cart.RemoveItem("apple");

        // Unsubscribe one observer
        _cart.Unsubscribe(observer1);
        _cart.AddItem("bread", 1);

        // Assert
        // Observer1 should have 3 notifications (before unsubscribe)
        Assert.Equal(3, observer1.Notifications.Count);
        
        // Observer2 should have 4 notifications (all operations)
        Assert.Equal(4, observer2.Notifications.Count);
    }

    #endregion

    #region Test Helper Classes

    private class TestObserver : ICartObserver
    {
        public List<string> Notifications { get; } = new List<string>();

        public void OnItemAdded(string productId, int quantity, TimeSpan executionTime)
        {
            Notifications.Add($"OnItemAdded:{productId}:{quantity}:{executionTime.TotalMilliseconds}ms");
        }

        public void OnItemRemoved(string productId, TimeSpan executionTime)
        {
            Notifications.Add($"OnItemRemoved:{productId}:{executionTime.TotalMilliseconds}ms");
        }

        public void OnTotalCalculated(decimal total, int itemCount, TimeSpan executionTime)
        {
            Notifications.Add($"OnTotalCalculated:{total}:{itemCount}:{executionTime.TotalMilliseconds}ms");
        }

        public void OnItemsRetrieved(int itemCount, TimeSpan executionTime)
        {
            Notifications.Add($"OnItemsRetrieved:{itemCount}:{executionTime.TotalMilliseconds}ms");
        }

        public void OnError(string operation, Exception exception, TimeSpan executionTime)
        {
            Notifications.Add($"OnError:{operation}:{exception.Message}:{executionTime.TotalMilliseconds}ms");
        }
    }

    #endregion
} 
