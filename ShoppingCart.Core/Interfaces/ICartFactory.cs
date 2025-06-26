namespace ShoppingCart.Core.Interfaces;

/// <summary>
/// Factory interface for creating cart instances.
/// </summary>
public interface ICartFactory
{
    /// <summary>
    /// Creates a cart instance for the specified backend type.
    /// </summary>
    /// <param name="backendType">The backend storage type.</param>
    /// <returns>A cart instance.</returns>
    ICart CreateCart(CartBackendType backendType);

    /// <summary>
    /// Creates a cart instance with the specified product service.
    /// </summary>
    /// <param name="productService">The product service to use.</param>
    /// <returns>A cart instance.</returns>
    ICart CreateCart(IProductService productService);
}

/// <summary>
/// Enumeration of supported cart backend types.
/// </summary>
public enum CartBackendType
{
    /// <summary>
    /// In-memory storage (default implementation).
    /// </summary>
    InMemory,

    /// <summary>
    /// Redis cache storage.
    /// </summary>
    Redis
} 