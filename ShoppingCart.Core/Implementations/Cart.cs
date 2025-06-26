using ShoppingCart.Core.Interfaces;

namespace ShoppingCart.Core.Implementations;

/// <summary>
/// A shopping cart implementation that manages items and calculates totals.
/// </summary>
public class Cart : ICart
{
    private readonly IProductService _productService;
    private readonly Dictionary<string, int> _items;

    /// <summary>
    /// Initializes a new instance of the Cart class.
    /// </summary>
    /// <param name="productService">The product service for price lookups.</param>
    /// <exception cref="ArgumentNullException">Thrown when productService is null.</exception>
    public Cart(IProductService productService)
    {
        _productService = productService ?? throw new ArgumentNullException(nameof(productService));
        _items = new Dictionary<string, int>();
    }

    /// <summary>
    /// Adds an item to the cart with the specified quantity.
    /// </summary>
    /// <param name="productId">The product identifier to add.</param>
    /// <param name="quantity">The quantity to add.</param>
    /// <exception cref="ArgumentException">Thrown when product is not found or quantity is invalid.</exception>
    public void AddItem(string productId, int quantity)
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
    /// Removes all quantities of the specified product from the cart.
    /// </summary>
    /// <param name="productId">The product identifier to remove.</param>
    /// <exception cref="ArgumentException">Thrown when product ID is invalid.</exception>
    public void RemoveItem(string productId)
    {
        if (string.IsNullOrWhiteSpace(productId))
            throw new ArgumentException("Product ID cannot be null or empty", nameof(productId));

        var normalizedProductId = productId.ToLowerInvariant();
        _items.Remove(normalizedProductId);
    }

    /// <summary>
    /// Calculates the total cost of all items in the cart.
    /// </summary>
    /// <returns>The total cost as a decimal.</returns>
    public decimal Total()
    {
        return _items.Sum(item => _productService.GetPrice(item.Key) * item.Value);
    }

    /// <summary>
    /// Returns a dictionary of items and their quantities in the cart.
    /// </summary>
    /// <returns>Dictionary containing product IDs and quantities.</returns>
    public Dictionary<string, int> Items()
    {
        return new Dictionary<string, int>(_items);
    }
} 