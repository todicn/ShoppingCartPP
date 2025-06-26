using ShoppingCart.Core.Interfaces;

namespace ShoppingCart.Core.Implementations;

/// <summary>
/// A simple implementation of the product service interface with a static product catalog.
/// </summary>
public class ProductService : IProductService
{
    /// <summary>
    /// Static dictionary containing available products and their prices.
    /// </summary>
    private static readonly Dictionary<string, decimal> ProductCatalog = new()
    {
        { "apple", 0.50m },
        { "banana", 0.30m },
        { "orange", 0.75m },
        { "bread", 2.50m },
        { "milk", 3.25m },
        { "cheese", 4.99m },
        { "chicken", 8.99m },
        { "rice", 1.99m }
    };

    /// <summary>
    /// Gets the price of a product by its identifier.
    /// </summary>
    /// <param name="productId">The product identifier.</param>
    /// <returns>The price of the product.</returns>
    /// <exception cref="ArgumentException">Thrown when the product is not found.</exception>
    public decimal GetPrice(string productId)
    {
        if (string.IsNullOrWhiteSpace(productId))
            throw new ArgumentException("Product ID cannot be null or empty", nameof(productId));

        var normalizedProductId = productId.ToLowerInvariant();
        
        if (!ProductCatalog.TryGetValue(normalizedProductId, out var price))
            throw new ArgumentException($"Product '{productId}' not found", nameof(productId));

        return price;
    }

    /// <summary>
    /// Checks if a product exists in the catalog.
    /// </summary>
    /// <param name="productId">The product identifier to check.</param>
    /// <returns>True if the product exists, false otherwise.</returns>
    public bool ProductExists(string productId)
    {
        if (string.IsNullOrWhiteSpace(productId))
            return false;

        return ProductCatalog.ContainsKey(productId.ToLowerInvariant());
    }

    /// <summary>
    /// Gets all available products and their prices.
    /// </summary>
    /// <returns>Dictionary containing product IDs and their prices.</returns>
    public Dictionary<string, decimal> GetAllProducts()
    {
        return new Dictionary<string, decimal>(ProductCatalog);
    }
} 