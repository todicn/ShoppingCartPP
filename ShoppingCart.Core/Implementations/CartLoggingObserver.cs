using Microsoft.Extensions.Logging;
using ShoppingCart.Core.Interfaces;

namespace ShoppingCart.Core.Implementations;

/// <summary>
/// Observer that logs cart operations for monitoring and debugging.
/// </summary>
public class CartLoggingObserver : ICartObserver
{
    private readonly ILogger<CartLoggingObserver> _logger;

    /// <summary>
    /// Initializes a new instance of the CartLoggingObserver class.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    public CartLoggingObserver(ILogger<CartLoggingObserver> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Called when an item is added to the cart.
    /// </summary>
    public void OnItemAdded(string productId, int quantity, TimeSpan executionTime)
    {
        _logger.LogInformation(
            "Cart operation: Added {Quantity} of {ProductId} in {ExecutionTime:F2}ms",
            quantity, productId, executionTime.TotalMilliseconds);
    }

    /// <summary>
    /// Called when an item is removed from the cart.
    /// </summary>
    public void OnItemRemoved(string productId, TimeSpan executionTime)
    {
        _logger.LogInformation(
            "Cart operation: Removed {ProductId} in {ExecutionTime:F2}ms",
            productId, executionTime.TotalMilliseconds);
    }

    /// <summary>
    /// Called when the cart total is calculated.
    /// </summary>
    public void OnTotalCalculated(decimal total, int itemCount, TimeSpan executionTime)
    {
        _logger.LogInformation(
            "Cart operation: Calculated total ${Total:F2} for {ItemCount} items in {ExecutionTime:F2}ms",
            total, itemCount, executionTime.TotalMilliseconds);
    }

    /// <summary>
    /// Called when cart items are retrieved.
    /// </summary>
    public void OnItemsRetrieved(int itemCount, TimeSpan executionTime)
    {
        _logger.LogInformation(
            "Cart operation: Retrieved {ItemCount} items in {ExecutionTime:F2}ms",
            itemCount, executionTime.TotalMilliseconds);
    }

    /// <summary>
    /// Called when an error occurs during cart operations.
    /// </summary>
    public void OnError(string operation, Exception exception, TimeSpan executionTime)
    {
        _logger.LogError(exception,
            "Cart operation failed: {Operation} failed after {ExecutionTime:F2}ms - {ErrorMessage}",
            operation, executionTime.TotalMilliseconds, exception.Message);
    }
} 