namespace ShoppingCart.Core.Interfaces;

/// <summary>
/// Observer interface for monitoring cart operations.
/// </summary>
public interface ICartObserver
{
    /// <summary>
    /// Called when an item is added to the cart.
    /// </summary>
    /// <param name="productId">The product identifier.</param>
    /// <param name="quantity">The quantity added.</param>
    /// <param name="executionTime">Time taken to execute the operation.</param>
    void OnItemAdded(string productId, int quantity, TimeSpan executionTime);

    /// <summary>
    /// Called when an item is removed from the cart.
    /// </summary>
    /// <param name="productId">The product identifier.</param>
    /// <param name="executionTime">Time taken to execute the operation.</param>
    void OnItemRemoved(string productId, TimeSpan executionTime);

    /// <summary>
    /// Called when the cart total is calculated.
    /// </summary>
    /// <param name="total">The calculated total.</param>
    /// <param name="itemCount">Number of items in the cart.</param>
    /// <param name="executionTime">Time taken to execute the operation.</param>
    void OnTotalCalculated(decimal total, int itemCount, TimeSpan executionTime);

    /// <summary>
    /// Called when cart items are retrieved.
    /// </summary>
    /// <param name="itemCount">Number of items retrieved.</param>
    /// <param name="executionTime">Time taken to execute the operation.</param>
    void OnItemsRetrieved(int itemCount, TimeSpan executionTime);

    /// <summary>
    /// Called when an error occurs during cart operations.
    /// </summary>
    /// <param name="operation">The operation that failed.</param>
    /// <param name="exception">The exception that occurred.</param>
    /// <param name="executionTime">Time taken before the error occurred.</param>
    void OnError(string operation, Exception exception, TimeSpan executionTime);
} 