using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using ShoppingCart.Core.Interfaces;

namespace ShoppingCart.Core.Implementations;

/// <summary>
/// Factory for creating cart instances with different backend implementations.
/// </summary>
public class CartFactory : ICartFactory
{
    private readonly IProductService _inMemoryProductService;
    private readonly IConnectionMultiplexer? _redisConnection;
    private readonly ILogger<CartFactory>? _logger;

    /// <summary>
    /// Initializes a new instance of the CartFactory class.
    /// </summary>
    /// <param name="inMemoryProductService">The in-memory product service.</param>
    /// <param name="redisConnection">Optional Redis connection for Redis-based carts.</param>
    /// <param name="logger">Optional logger instance.</param>
    public CartFactory(
        IProductService inMemoryProductService, 
        IConnectionMultiplexer? redisConnection = null,
        ILogger<CartFactory>? logger = null)
    {
        _inMemoryProductService = inMemoryProductService ?? throw new ArgumentNullException(nameof(inMemoryProductService));
        _redisConnection = redisConnection;
        _logger = logger;
    }

    /// <summary>
    /// Creates a cart instance for the specified backend type.
    /// </summary>
    /// <param name="backendType">The backend storage type.</param>
    /// <returns>A cart instance.</returns>
    /// <exception cref="ArgumentException">Thrown when backend type is not supported.</exception>
    /// <exception cref="InvalidOperationException">Thrown when Redis backend is requested but not configured.</exception>
    public ICart CreateCart(CartBackendType backendType)
    {
        return backendType switch
        {
            CartBackendType.InMemory => CreateInMemoryCart(),
            CartBackendType.Redis => CreateRedisCart(),
            _ => throw new ArgumentException($"Unsupported backend type: {backendType}", nameof(backendType))
        };
    }

    /// <summary>
    /// Creates a cart instance with the specified product service.
    /// </summary>
    /// <param name="productService">The product service to use.</param>
    /// <returns>A cart instance.</returns>
    public ICart CreateCart(IProductService productService)
    {
        if (productService == null)
            throw new ArgumentNullException(nameof(productService));

        var cart = new InMemoryCart(productService);
        SetupObservers(cart);
        return cart;
    }

    /// <summary>
    /// Creates a cart instance with Redis backend and custom cart ID.
    /// </summary>
    /// <param name="cartId">Unique identifier for the cart.</param>
    /// <returns>A Redis-based cart instance.</returns>
    /// <exception cref="InvalidOperationException">Thrown when Redis is not configured.</exception>
    public ICart CreateRedisCart(string cartId)
    {
        if (_redisConnection == null)
            throw new InvalidOperationException("Redis connection is not configured. Cannot create Redis cart.");

        var database = _redisConnection.GetDatabase();
        var productService = new RedisProductService(database);
        var cart = new RedisCart(productService, database, cartId);
        
        SetupObservers(cart);
        return cart;
    }

    /// <summary>
    /// Creates multiple cart instances for different users with Redis backend.
    /// </summary>
    /// <param name="userIds">Collection of user identifiers.</param>
    /// <returns>Dictionary mapping user IDs to their cart instances.</returns>
    public Dictionary<string, ICart> CreateMultiUserCarts(IEnumerable<string> userIds)
    {
        var carts = new Dictionary<string, ICart>();
        
        foreach (var userId in userIds)
        {
            if (string.IsNullOrWhiteSpace(userId))
                continue;
                
            try
            {
                // Try Redis first, fallback to in-memory
                var cart = _redisConnection != null 
                    ? CreateRedisCart($"user_{userId}")
                    : CreateInMemoryCart();
                    
                carts[userId] = cart;
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "Failed to create cart for user {UserId}, using in-memory fallback", userId);
                carts[userId] = CreateInMemoryCart();
            }
        }
        
        return carts;
    }

    /// <summary>
    /// Tests connectivity to Redis and returns whether Redis backend is available.
    /// </summary>
    /// <returns>True if Redis is available, false otherwise.</returns>
    public bool IsRedisAvailable()
    {
        try
        {
            if (_redisConnection == null || !_redisConnection.IsConnected)
                return false;

            var database = _redisConnection.GetDatabase();
            database.Ping();
            return true;
        }
        catch (Exception ex)
        {
            _logger?.LogDebug(ex, "Redis connectivity test failed");
            return false;
        }
    }

    /// <summary>
    /// Gets the recommended backend type based on current configuration.
    /// </summary>
    /// <returns>The recommended backend type.</returns>
    public CartBackendType GetRecommendedBackend()
    {
        return IsRedisAvailable() ? CartBackendType.Redis : CartBackendType.InMemory;
    }

    /// <summary>
    /// Creates an in-memory cart with default configuration.
    /// </summary>
    private ICart CreateInMemoryCart()
    {
        var cart = new InMemoryCart(_inMemoryProductService);
        SetupObservers(cart);
        return cart;
    }

    /// <summary>
    /// Creates a Redis cart with default configuration.
    /// </summary>
    private ICart CreateRedisCart()
    {
        if (_redisConnection == null)
            throw new InvalidOperationException("Redis connection is not configured. Cannot create Redis cart.");

        var database = _redisConnection.GetDatabase();
        var productService = new RedisProductService(database);
        var cart = new RedisCart(productService, database);
        
        SetupObservers(cart);
        return cart;
    }

    /// <summary>
    /// Sets up default observers for a cart instance.
    /// </summary>
    private void SetupObservers(ICart cart)
    {
        try
        {
            // Add performance monitoring observer
            var performanceObserver = new CartPerformanceObserver(slowOperationThresholdMs: 50.0);
            cart.Subscribe(performanceObserver);

            // Add logging observer if logger is available
            if (_logger != null)
            {
                var loggingObserver = new CartLoggingObserver(
                    _logger as ILogger<CartLoggingObserver> ?? 
                    new LoggerAdapter<CartLoggingObserver>(_logger));
                cart.Subscribe(loggingObserver);
            }
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Failed to setup cart observers");
        }
    }
}

/// <summary>
/// Adapter to convert generic logger to specific typed logger.
/// </summary>
internal class LoggerAdapter<T> : ILogger<T>
{
    private readonly ILogger _logger;

    public LoggerAdapter(ILogger logger)
    {
        _logger = logger;
    }

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => _logger.BeginScope(state);

    public bool IsEnabled(LogLevel logLevel) => _logger.IsEnabled(logLevel);

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        => _logger.Log(logLevel, eventId, state, exception, formatter);
} 