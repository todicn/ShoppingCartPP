using ShoppingCart.Core.Interfaces;

namespace ShoppingCart.Core.Implementations;

/// <summary>
/// A shopping cart implementation that manages items and calculates totals using in-memory storage.
/// </summary>
public class InMemoryCart : BaseObservableCart
{
    private readonly IProductService _productService;
    private readonly Dictionary<string, int> _items;

    /// <summary>
    /// Initializes a new instance of the InMemoryCart class.
    /// </summary>
    /// <param name="productService">The product service for price lookups.</param>
    /// <exception cref="ArgumentNullException">Thrown when productService is null.</exception>
    public InMemoryCart(IProductService productService)
    {
        _productService = productService ?? throw new ArgumentNullException(nameof(productService));
        _items = new Dictionary<string, int>();
    }

    /// <summary>
    /// Core implementation of adding an item to the cart.
    /// </summary>
    /// <param name="productId">The product identifier to add.</param>
    /// <param name="quantity">The quantity to add.</param>
    /// <exception cref="ArgumentException">Thrown when product is not found or quantity is invalid.</exception>
    protected override void AddItemCore(string productId, int quantity)
    {
        if (string.IsNullOrWhiteSpace(productId))
            throw new ArgumentException("Product ID cannot be null or empty", nameof(productId));

        if (quantity <= 0)
            throw new ArgumentException("Quantity must be greater than zero", nameof(quantity));

        if (!_productService.ProductExists(productId))
            throw new ArgumentException($"Product '{productId}' not found", nameof(productId));

        var normalizedProductId = productId.ToLowerInvariant();
        
        if (_items.ContainsKey(normalizedProductId))
            _items[normalizedProductId] += quantity;
        else
            _items[normalizedProductId] = quantity;
    }

    /// <summary>
    /// Core implementation of removing an item from the cart.
    /// </summary>
    /// <param name="productId">The product identifier to remove.</param>
    /// <exception cref="ArgumentException">Thrown when product ID is invalid.</exception>
    protected override void RemoveItemCore(string productId)
    {
        if (string.IsNullOrWhiteSpace(productId))
            throw new ArgumentException("Product ID cannot be null or empty", nameof(productId));

        var normalizedProductId = productId.ToLowerInvariant();
        _items.Remove(normalizedProductId);
    }

    /// <summary>
    /// Core implementation of calculating the total.
    /// </summary>
    /// <returns>The total cost as a decimal.</returns>
    protected override decimal TotalCore()
    {
        return _items.Sum(item => _productService.GetPrice(item.Key) * item.Value);
    }

    /// <summary>
    /// Core implementation of getting cart items.
    /// </summary>
    /// <returns>Dictionary containing product IDs and quantities.</returns>
    protected override Dictionary<string, int> ItemsCore()
    {
        return new Dictionary<string, int>(_items);
    }

    /// <summary>
    /// Gets the number of items in the cart.
    /// </summary>
    /// <returns>The number of distinct items.</returns>
    protected override int GetItemCountCore()
    {
        return _items.Count;
    }
} 