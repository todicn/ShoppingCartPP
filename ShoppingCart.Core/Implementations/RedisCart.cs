using System.Text.Json;
using StackExchange.Redis;
using ShoppingCart.Core.Interfaces;

namespace ShoppingCart.Core.Implementations;

/// <summary>
/// A shopping cart implementation that stores data in Redis cache.
/// </summary>
public class RedisCart : BaseObservableCart, IDisposable
{
    private readonly IProductService _productService;
    private readonly IDatabase _database;
    private readonly string _cartKey;
    private readonly JsonSerializerOptions _jsonOptions;

    /// <summary>
    /// Initializes a new instance of the RedisCart class.
    /// </summary>
    /// <param name="productService">The product service for price lookups.</param>
    /// <param name="database">The Redis database connection.</param>
    /// <param name="cartId">Unique identifier for this cart instance.</param>
    /// <exception cref="ArgumentNullException">Thrown when any parameter is null.</exception>
    public RedisCart(IProductService productService, IDatabase database, string cartId = "default")
    {
        _productService = productService ?? throw new ArgumentNullException(nameof(productService));
        _database = database ?? throw new ArgumentNullException(nameof(database));
        
        if (string.IsNullOrWhiteSpace(cartId))
            throw new ArgumentException("Cart ID cannot be null or empty", nameof(cartId));
            
        _cartKey = $"cart:{cartId}";
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
    }

    /// <summary>
    /// Core implementation of adding an item to the cart.
    /// </summary>
    protected override void AddItemCore(string productId, int quantity)
    {
        if (string.IsNullOrWhiteSpace(productId))
            throw new ArgumentException("Product ID cannot be null or empty", nameof(productId));

        if (quantity <= 0)
            throw new ArgumentException("Quantity must be greater than zero", nameof(quantity));

        var normalizedProductId = productId.Trim().ToLowerInvariant();
        
        if (!_productService.ProductExists(normalizedProductId))
            throw new ArgumentException($"Product '{productId}' not found", nameof(productId));
        
        // Get current cart data
        var currentItems = GetCartFromRedis();
        
        // Add or update the item
        if (currentItems.ContainsKey(normalizedProductId))
            currentItems[normalizedProductId] += quantity;
        else
            currentItems[normalizedProductId] = quantity;
            
        // Save back to Redis
        SaveCartToRedis(currentItems);
    }

    /// <summary>
    /// Core implementation of removing an item from the cart.
    /// </summary>
    protected override void RemoveItemCore(string productId)
    {
        if (string.IsNullOrWhiteSpace(productId))
            throw new ArgumentException("Product ID cannot be null or empty", nameof(productId));

        var normalizedProductId = productId.Trim().ToLowerInvariant();
        
        // Get current cart data
        var currentItems = GetCartFromRedis();
        
        // Remove the item
        currentItems.Remove(normalizedProductId);
        
        // Save back to Redis
        SaveCartToRedis(currentItems);
    }

    /// <summary>
    /// Core implementation of calculating the total.
    /// </summary>
    protected override decimal TotalCore()
    {
        var items = GetCartFromRedis();
        return items.Sum(item => _productService.GetPrice(item.Key) * item.Value);
    }

    /// <summary>
    /// Core implementation of getting cart items.
    /// </summary>
    protected override Dictionary<string, int> ItemsCore()
    {
        return new Dictionary<string, int>(GetCartFromRedis());
    }

    /// <summary>
    /// Gets the number of items in the cart.
    /// </summary>
    protected override int GetItemCountCore()
    {
        var items = GetCartFromRedis();
        return items.Count;
    }

    /// <summary>
    /// Clears all items from the cart.
    /// </summary>
    public void Clear()
    {
        _database.KeyDelete(_cartKey);
    }

    /// <summary>
    /// Sets an expiration time for the cart in Redis.
    /// </summary>
    /// <param name="expiry">The expiration timespan.</param>
    public void SetExpiry(TimeSpan expiry)
    {
        _database.KeyExpire(_cartKey, expiry);
    }

    /// <summary>
    /// Gets the current TTL (time to live) of the cart in Redis.
    /// </summary>
    /// <returns>The TTL timespan, or null if no expiry is set.</returns>
    public TimeSpan? GetTtl()
    {
        return _database.KeyTimeToLive(_cartKey);
    }

    /// <summary>
    /// Retrieves cart data from Redis.
    /// </summary>
    private Dictionary<string, int> GetCartFromRedis()
    {
        try
        {
            var cartJson = _database.StringGet(_cartKey);
            
            if (!cartJson.HasValue)
                return new Dictionary<string, int>();
                
            var cartData = JsonSerializer.Deserialize<Dictionary<string, int>>(cartJson!, _jsonOptions);
            return cartData ?? new Dictionary<string, int>();
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException($"Failed to deserialize cart data from Redis: {ex.Message}", ex);
        }
        catch (RedisException ex)
        {
            throw new InvalidOperationException($"Redis operation failed: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Saves cart data to Redis.
    /// </summary>
    private void SaveCartToRedis(Dictionary<string, int> items)
    {
        try
        {
            if (items.Count == 0)
            {
                // If cart is empty, delete the key
                _database.KeyDelete(_cartKey);
            }
            else
            {
                var cartJson = JsonSerializer.Serialize(items, _jsonOptions);
                _database.StringSet(_cartKey, cartJson);
            }
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException($"Failed to serialize cart data for Redis: {ex.Message}", ex);
        }
        catch (RedisException ex)
        {
            throw new InvalidOperationException($"Redis operation failed: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Disposes of the RedisCart resources.
    /// </summary>
    public void Dispose()
    {
        // Nothing to dispose for this implementation
        // The Redis connection is managed externally
    }
} 