namespace ShoppingCart.Core.Interfaces;

/// <summary>
/// Represents a product service contract for managing product information.
/// </summary>
public interface IProductService
{
    /// <summary>
    /// Gets the price of a product by its identifier.
    /// </summary>
    /// <param name="productId">The product identifier.</param>
    /// <returns>The price of the product.</returns>
    /// <exception cref="ArgumentException">Thrown when the product is not found.</exception>
    decimal GetPrice(string productId);

    /// <summary>
    /// Checks if a product exists in the catalog.
    /// </summary>
    /// <param name="productId">The product identifier to check.</param>
    /// <returns>True if the product exists, false otherwise.</returns>
    bool ProductExists(string productId);

    /// <summary>
    /// Gets all available products and their prices.
    /// </summary>
    /// <returns>Dictionary containing product IDs and their prices.</returns>
    Dictionary<string, decimal> GetAllProducts();
} 