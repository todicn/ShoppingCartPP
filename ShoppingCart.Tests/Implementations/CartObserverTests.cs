using Xunit;
using Moq;
using Microsoft.Extensions.Logging;
using ShoppingCart.Core.Implementations;
using ShoppingCart.Core.Interfaces;

namespace ShoppingCart.Tests.Implementations;

/// <summary>
/// Unit tests for cart observer implementations.
/// </summary>
public class CartObserverTests
{
    [Fact]
    public void CartLoggingObserver_OnItemAdded_LogsCorrectInformation()
    {
        // Arrange
        var mockLogger = new Mock<ILogger<CartLoggingObserver>>();
        var observer = new CartLoggingObserver(mockLogger.Object);
        var executionTime = TimeSpan.FromMilliseconds(25);

        // Act
        observer.OnItemAdded("apple", 2, executionTime);

        // Assert
        mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Added 2 of apple in 25.00ms")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void CartLoggingObserver_OnItemRemoved_LogsCorrectInformation()
    {
        // Arrange
        var mockLogger = new Mock<ILogger<CartLoggingObserver>>();
        var observer = new CartLoggingObserver(mockLogger.Object);
        var executionTime = TimeSpan.FromMilliseconds(15);

        // Act
        observer.OnItemRemoved("banana", executionTime);

        // Assert
        mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Removed banana in 15.00ms")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void CartLoggingObserver_OnTotalCalculated_LogsCorrectInformation()
    {
        // Arrange
        var mockLogger = new Mock<ILogger<CartLoggingObserver>>();
        var observer = new CartLoggingObserver(mockLogger.Object);
        var executionTime = TimeSpan.FromMilliseconds(30);

        // Act
        observer.OnTotalCalculated(12.50m, 3, executionTime);

        // Assert
        mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Calculated total $12.50 for 3 items in 30.00ms")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void CartLoggingObserver_OnError_LogsError()
    {
        // Arrange
        var mockLogger = new Mock<ILogger<CartLoggingObserver>>();
        var observer = new CartLoggingObserver(mockLogger.Object);
        var exception = new ArgumentException("Test error");
        var executionTime = TimeSpan.FromMilliseconds(45);

        // Act
        observer.OnError("AddItem", exception, executionTime);

        // Assert
        mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("AddItem failed after 45.00ms")),
                exception,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void CartPerformanceObserver_RecordsOperationTimes()
    {
        // Arrange
        var observer = new CartPerformanceObserver(slowOperationThresholdMs: 100.0);

        // Act
        observer.OnItemAdded("apple", 1, TimeSpan.FromMilliseconds(50));
        observer.OnItemAdded("banana", 2, TimeSpan.FromMilliseconds(75));
        observer.OnItemRemoved("apple", TimeSpan.FromMilliseconds(25));

        // Assert
        var stats = observer.GetPerformanceStats();
        Assert.Contains("AddItem", stats.Keys);
        Assert.Contains("RemoveItem", stats.Keys);
        
        var addItemStats = stats["AddItem"];
        Assert.Equal(2, addItemStats.TotalCalls);
        Assert.Equal(62.5, addItemStats.AverageTimeMs);
        Assert.Equal(50.0, addItemStats.MinTimeMs);
        Assert.Equal(75.0, addItemStats.MaxTimeMs);
        Assert.Equal(0, addItemStats.SlowCalls);
    }

    [Fact]
    public void CartPerformanceObserver_DetectsSlowOperations()
    {
        // Arrange
        var observer = new CartPerformanceObserver(slowOperationThresholdMs: 50.0);

        // Act
        observer.OnItemAdded("apple", 1, TimeSpan.FromMilliseconds(25));  // Fast
        observer.OnItemAdded("banana", 2, TimeSpan.FromMilliseconds(75)); // Slow
        observer.OnItemAdded("orange", 1, TimeSpan.FromMilliseconds(100)); // Slow

        // Assert
        var stats = observer.GetPerformanceStats();
        var addItemStats = stats["AddItem"];
        Assert.Equal(3, addItemStats.TotalCalls);
        Assert.Equal(2, addItemStats.SlowCalls);
    }

    [Fact]
    public void CartPerformanceObserver_Reset_ClearsAllData()
    {
        // Arrange
        var observer = new CartPerformanceObserver();
        observer.OnItemAdded("apple", 1, TimeSpan.FromMilliseconds(50));
        observer.OnItemRemoved("apple", TimeSpan.FromMilliseconds(25));

        // Act
        observer.Reset();

        // Assert
        var stats = observer.GetPerformanceStats();
        Assert.Empty(stats);
    }

    [Fact]
    public void InMemoryCart_WithObservers_NotifiesAllObservers()
    {
        // Arrange
        var productService = new ProductService();
        var cart = new InMemoryCart(productService);
        
        var mockObserver1 = new Mock<ICartObserver>();
        var mockObserver2 = new Mock<ICartObserver>();
        
        cart.Subscribe(mockObserver1.Object);
        cart.Subscribe(mockObserver2.Object);

        // Act
        cart.AddItem("apple", 2);

        // Assert
        mockObserver1.Verify(x => x.OnItemAdded("apple", 2, It.IsAny<TimeSpan>()), Times.Once);
        mockObserver2.Verify(x => x.OnItemAdded("apple", 2, It.IsAny<TimeSpan>()), Times.Once);
    }

    [Fact]
    public void InMemoryCart_UnsubscribeObserver_StopsNotifications()
    {
        // Arrange
        var productService = new ProductService();
        var cart = new InMemoryCart(productService);
        var mockObserver = new Mock<ICartObserver>();
        
        cart.Subscribe(mockObserver.Object);
        cart.Unsubscribe(mockObserver.Object);

        // Act
        cart.AddItem("apple", 2);

        // Assert
        mockObserver.Verify(x => x.OnItemAdded(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<TimeSpan>()), Times.Never);
    }

    [Fact]
    public void InMemoryCart_ObserverException_DoesNotAffectCartOperation()
    {
        // Arrange
        var productService = new ProductService();
        var cart = new InMemoryCart(productService);
        var mockObserver = new Mock<ICartObserver>();
        
        mockObserver.Setup(x => x.OnItemAdded(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<TimeSpan>()))
                   .Throws(new InvalidOperationException("Observer failed"));
        
        cart.Subscribe(mockObserver.Object);

        // Act & Assert - Should not throw
        cart.AddItem("apple", 2);
        
        // Verify cart still works
        var items = cart.Items();
        Assert.Single(items);
        Assert.Equal(2, items["apple"]);
    }

    [Fact]
    public void InMemoryCart_ErrorInOperation_NotifiesObserversOfError()
    {
        // Arrange
        var productService = new ProductService();
        var cart = new InMemoryCart(productService);
        var mockObserver = new Mock<ICartObserver>();
        
        cart.Subscribe(mockObserver.Object);

        // Act & Assert
        Assert.Throws<ArgumentException>(() => cart.AddItem("unknown_product", 1));
        
        // Verify error was reported to observer
        mockObserver.Verify(x => x.OnError("AddItem", It.IsAny<ArgumentException>(), It.IsAny<TimeSpan>()), Times.Once);
    }

    [Fact]
    public void CartPerformanceObserver_OperationStats_ToStringReturnsFormattedString()
    {
        // Arrange
        var stats = new OperationStats
        {
            OperationName = "AddItem",
            TotalCalls = 5,
            AverageTimeMs = 67.5,
            SlowCalls = 2
        };

        // Act
        var result = stats.ToString();

        // Assert
        Assert.Equal("AddItem: 5 calls, avg 67.50ms, 2 slow", result);
    }
} 