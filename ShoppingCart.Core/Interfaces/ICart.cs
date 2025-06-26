namespace ShoppingCart.Core.Interfaces;

/// <summary>
/// Represents a shopping cart contract for managing items and calculating totals.
/// </summary>
public interface ICart
{
    /// <summary>
    /// Adds an item to the cart with the specified quantity.
    /// </summary>
    /// <param name="productId">The product identifier to add.</param>
    /// <param name="quantity">The quantity to add.</param>
    /// <exception cref="ArgumentException">Thrown when product is not found or quantity is invalid.</exception>
    void AddItem(string productId, int quantity);

    /// <summary>
    /// Removes all quantities of the specified product from the cart.
    /// </summary>
    /// <param name="productId">The product identifier to remove.</param>
    /// <exception cref="ArgumentException">Thrown when product ID is invalid.</exception>
    void RemoveItem(string productId);

    /// <summary>
    /// Calculates the total cost of all items in the cart.
    /// </summary>
    /// <returns>The total cost as a decimal.</returns>
    decimal Total();

    /// <summary>
    /// Returns a dictionary of items and their quantities in the cart.
    /// </summary>
    /// <returns>Dictionary containing product IDs and quantities.</returns>
    Dictionary<string, int> Items();
} 