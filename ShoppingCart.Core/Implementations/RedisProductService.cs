using System.Text.Json;
using StackExchange.Redis;
using ShoppingCart.Core.Interfaces;

namespace ShoppingCart.Core.Implementations;

/// <summary>
/// A product service implementation that stores product catalog in Redis cache.
/// </summary>
public class RedisProductService : IProductService
{
    private readonly IDatabase _database;
    private readonly string _catalogKeyPrefix;
    private readonly JsonSerializerOptions _jsonOptions;

    /// <summary>
    /// Initializes a new instance of the RedisProductService class.
    /// </summary>
    /// <param name="database">The Redis database connection.</param>
    /// <param name="catalogKeyPrefix">Prefix for catalog keys in Redis.</param>
    public RedisProductService(IDatabase database, string catalogKeyPrefix = "product")
    {
        _database = database ?? throw new ArgumentNullException(nameof(database));
        _catalogKeyPrefix = catalogKeyPrefix ?? throw new ArgumentNullException(nameof(catalogKeyPrefix));
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
    }

    /// <summary>
    /// Gets the price of a product by its identifier.
    /// </summary>
    /// <param name="productId">The product identifier.</param>
    /// <returns>The price of the product.</returns>
    /// <exception cref="ArgumentException">Thrown when product is not found.</exception>
    public decimal GetPrice(string productId)
    {
        if (string.IsNullOrWhiteSpace(productId))
            throw new ArgumentException("Product ID cannot be null or empty", nameof(productId));

        var normalizedProductId = productId.ToLowerInvariant();
        var productKey = $"{_catalogKeyPrefix}:{normalizedProductId}";

        try
        {
            var productJson = _database.StringGet(productKey);
            if (!productJson.HasValue)
                throw new ArgumentException($"Product '{productId}' not found");

            var product = JsonSerializer.Deserialize<ProductInfo>(productJson!, _jsonOptions);
            return product?.Price ?? throw new ArgumentException($"Product '{productId}' has invalid price data");
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException($"Failed to deserialize product data from Redis: {ex.Message}", ex);
        }
        catch (RedisException ex)
        {
            throw new InvalidOperationException($"Redis operation failed: {ex.Message}", ex);
        }
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

        var normalizedProductId = productId.ToLowerInvariant();
        var productKey = $"{_catalogKeyPrefix}:{normalizedProductId}";

        try
        {
            return _database.KeyExists(productKey);
        }
        catch (RedisException ex)
        {
            throw new InvalidOperationException($"Redis operation failed: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Gets all available products from the catalog.
    /// </summary>
    /// <returns>Dictionary of product IDs and their prices.</returns>
    public Dictionary<string, decimal> GetAllProducts()
    {
        try
        {
            var pattern = $"{_catalogKeyPrefix}:*";
            var server = _database.Multiplexer.GetServer(_database.Multiplexer.GetEndPoints().First());
            var keys = server.Keys(pattern: pattern);

            var products = new Dictionary<string, decimal>();
            
            foreach (var key in keys)
            {
                var productJson = _database.StringGet(key);
                if (productJson.HasValue)
                {
                    var product = JsonSerializer.Deserialize<ProductInfo>(productJson!, _jsonOptions);
                    if (product != null)
                    {
                        var productId = key.ToString().Substring($"{_catalogKeyPrefix}:".Length);
                        products[productId] = product.Price;
                    }
                }
            }

            return products;
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException($"Failed to deserialize product data from Redis: {ex.Message}", ex);
        }
        catch (RedisException ex)
        {
            throw new InvalidOperationException($"Redis operation failed: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Adds or updates a product in the catalog.
    /// </summary>
    /// <param name="productId">The product identifier.</param>
    /// <param name="price">The product price.</param>
    /// <param name="name">The product name (optional).</param>
    /// <param name="description">The product description (optional).</param>
    public void AddOrUpdateProduct(string productId, decimal price, string? name = null, string? description = null)
    {
        if (string.IsNullOrWhiteSpace(productId))
            throw new ArgumentException("Product ID cannot be null or empty", nameof(productId));

        if (price < 0)
            throw new ArgumentException("Price cannot be negative", nameof(price));

        var normalizedProductId = productId.ToLowerInvariant();
        var productKey = $"{_catalogKeyPrefix}:{normalizedProductId}";

        var product = new ProductInfo
        {
            Id = normalizedProductId,
            Price = price,
            Name = name ?? productId,
            Description = description,
            LastUpdated = DateTime.UtcNow
        };

        try
        {
            var productJson = JsonSerializer.Serialize(product, _jsonOptions);
            _database.StringSet(productKey, productJson);
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException($"Failed to serialize product data for Redis: {ex.Message}", ex);
        }
        catch (RedisException ex)
        {
            throw new InvalidOperationException($"Redis operation failed: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Removes a product from the catalog.
    /// </summary>
    /// <param name="productId">The product identifier to remove.</param>
    /// <returns>True if the product was removed, false if it didn't exist.</returns>
    public bool RemoveProduct(string productId)
    {
        if (string.IsNullOrWhiteSpace(productId))
            return false;

        var normalizedProductId = productId.ToLowerInvariant();
        var productKey = $"{_catalogKeyPrefix}:{normalizedProductId}";

        try
        {
            return _database.KeyDelete(productKey);
        }
        catch (RedisException ex)
        {
            throw new InvalidOperationException($"Redis operation failed: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Initializes the catalog with default products.
    /// </summary>
    public void InitializeDefaultCatalog()
    {
        var defaultProducts = new Dictionary<string, (decimal price, string name, string description)>
        {
            { "apple", (0.50m, "Apple", "Fresh red apple") },
            { "banana", (0.30m, "Banana", "Ripe yellow banana") },
            { "orange", (0.75m, "Orange", "Juicy orange") },
            { "bread", (2.50m, "Bread", "Whole wheat bread loaf") },
            { "milk", (3.25m, "Milk", "Fresh whole milk (1 gallon)") },
            { "cheese", (4.99m, "Cheese", "Sharp cheddar cheese block") },
            { "chicken", (8.99m, "Chicken", "Free-range chicken breast (1 lb)") },
            { "rice", (1.99m, "Rice", "Long grain white rice (2 lb bag)") }
        };

        foreach (var kvp in defaultProducts)
        {
            AddOrUpdateProduct(kvp.Key, kvp.Value.price, kvp.Value.name, kvp.Value.description);
        }
    }

    /// <summary>
    /// Clears all products from the catalog.
    /// </summary>
    public void ClearCatalog()
    {
        try
        {
            var pattern = $"{_catalogKeyPrefix}:*";
            var server = _database.Multiplexer.GetServer(_database.Multiplexer.GetEndPoints().First());
            var keys = server.Keys(pattern: pattern);

            foreach (var key in keys)
            {
                _database.KeyDelete(key);
            }
        }
        catch (RedisException ex)
        {
            throw new InvalidOperationException($"Redis operation failed: {ex.Message}", ex);
        }
    }
}

/// <summary>
/// Represents product information stored in Redis.
/// </summary>
internal class ProductInfo
{
    /// <summary>
    /// Product identifier.
    /// </summary>
    public required string Id { get; set; }

    /// <summary>
    /// Product price.
    /// </summary>
    public decimal Price { get; set; }

    /// <summary>
    /// Product name.
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// Product description.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Last updated timestamp.
    /// </summary>
    public DateTime LastUpdated { get; set; }
}