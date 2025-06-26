using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using ShoppingCart.Core.Implementations;
using ShoppingCart.Core.Interfaces;

namespace ShoppingCart.Demo;

/// <summary>
/// Demo application showcasing the enhanced shopping cart functionality with factory pattern, Redis support, and observers.
/// </summary>
class Program
{
    private static ILogger<Program>? _logger;
    private static CartFactory? _cartFactory;
    private static IConnectionMultiplexer? _redisConnection;

    static async Task Main(string[] args)
    {
        Console.WriteLine("=== Enhanced Shopping Cart Demo ===");
        Console.WriteLine();

        // Setup logging
        SetupLogging();

        try
        {
            // Initialize services
            await InitializeServicesAsync();

            // Run demos
            await RunBackendSelectionDemoAsync();
            await RunObserverDemoAsync();
            await RunPerformanceDemoAsync();
            await RunMultiUserDemoAsync();

            Console.WriteLine();
            Console.WriteLine("=== Demo Completed Successfully ===");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error occurred: {ex.Message}");
            _logger?.LogError(ex, "Demo failed");
        }
        finally
        {
            // Cleanup
            await CleanupAsync();
        }

        Console.WriteLine();
        Console.WriteLine("Press any key to exit...");
        Console.ReadKey();
    }

    /// <summary>
    /// Sets up logging for the demo.
    /// </summary>
    private static void SetupLogging()
    {
        using var loggerFactory = LoggerFactory.Create(builder =>
        {
            builder.AddConsole().SetMinimumLevel(LogLevel.Information);
        });
        _logger = loggerFactory.CreateLogger<Program>();
    }

    /// <summary>
    /// Initializes services including Redis connection and factory.
    /// </summary>
    private static async Task InitializeServicesAsync()
    {
        Console.WriteLine("🔧 Initializing Services...");
        
        // Setup in-memory product service
        var inMemoryProductService = new ProductService();
        
        // Try to connect to Redis
        try
        {
            _redisConnection = await ConnectionMultiplexer.ConnectAsync("localhost:6379");
            Console.WriteLine("✅ Redis connection established");
            
            // Initialize Redis catalog
            var redisDb = _redisConnection.GetDatabase();
            var redisProductService = new RedisProductService(redisDb);
            redisProductService.InitializeDefaultCatalog();
            Console.WriteLine("✅ Redis catalog initialized");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"⚠️  Redis connection failed: {ex.Message}");
            Console.WriteLine("   Demo will continue with in-memory backend only");
        }

        // Create factory
        _cartFactory = new CartFactory(inMemoryProductService, _redisConnection, _logger as ILogger<CartFactory>);
        Console.WriteLine("✅ Cart factory created");
        Console.WriteLine();
    }

    /// <summary>
    /// Demonstrates backend selection and capabilities.
    /// </summary>
    private static async Task RunBackendSelectionDemoAsync()
    {
        Console.WriteLine("🏪 Backend Selection Demo:");
        Console.WriteLine("---------------------------");

        // Show available backends
        var recommendedBackend = _cartFactory!.GetRecommendedBackend();
        var redisAvailable = _cartFactory.IsRedisAvailable();
        
        Console.WriteLine($"Redis Available: {(redisAvailable ? "✅ Yes" : "❌ No")}");
        Console.WriteLine($"Recommended Backend: {recommendedBackend}");
        Console.WriteLine();

        // Demonstrate both backends
        await DemonstrateInMemoryBackendAsync();
        
        if (redisAvailable)
        {
            await DemonstrateRedisBackendAsync();
        }
    }

    /// <summary>
    /// Demonstrates in-memory backend functionality.
    /// </summary>
    private static async Task DemonstrateInMemoryBackendAsync()
    {
        Console.WriteLine("💾 In-Memory Backend Demo:");
        Console.WriteLine("---------------------------");

        var cart = _cartFactory!.CreateCart(CartBackendType.InMemory);
        
        // Setup observers
        var performanceObserver = new CartPerformanceObserver(slowOperationThresholdMs: 10.0);
        cart.Subscribe(performanceObserver);

        // Perform operations
        cart.AddItem("apple", 3);
        cart.AddItem("banana", 2);
        cart.AddItem("bread", 1);
        
        DisplayCartSummary(cart, "In-Memory Cart");
        
        // Show performance stats
        DisplayPerformanceStats(performanceObserver, "In-Memory Backend");
        
        // Remove item
        cart.RemoveItem("banana");
        DisplayCartSummary(cart, "After Removal");
        
        Console.WriteLine();
    }

    /// <summary>
    /// Demonstrates Redis backend functionality.
    /// </summary>
    private static async Task DemonstrateRedisBackendAsync()
    {
        Console.WriteLine("🔄 Redis Backend Demo:");
        Console.WriteLine("----------------------");

        var cartId = $"demo-cart-{DateTime.Now:yyyyMMdd-HHmmss}";
        var cart = _cartFactory!.CreateRedisCart(cartId) as RedisCart;
        
        if (cart != null)
        {
            // Setup observers
            var performanceObserver = new CartPerformanceObserver(slowOperationThresholdMs: 20.0);
            cart.Subscribe(performanceObserver);

            // Perform operations
            cart.AddItem("apple", 5);
            cart.AddItem("milk", 2);
            cart.AddItem("cheese", 1);
            
            DisplayCartSummary(cart, $"Redis Cart (ID: {cartId})");
            
            // Set expiration
            cart.SetExpiry(TimeSpan.FromMinutes(5));
            var ttl = cart.GetTtl();
            Console.WriteLine($"🕐 Cart TTL: {ttl?.TotalMinutes:F1} minutes");
            
            // Show performance stats
            DisplayPerformanceStats(performanceObserver, "Redis Backend");
            
            // Test persistence by creating another cart with same ID
            var cart2 = _cartFactory.CreateRedisCart(cartId);
            DisplayCartSummary(cart2, "Same Cart from Another Instance");
            
            // Cleanup
            cart.Clear();
            Console.WriteLine("🧹 Redis cart cleared");
        }
        
        Console.WriteLine();
    }

    /// <summary>
    /// Demonstrates observer pattern functionality.
    /// </summary>
    private static async Task RunObserverDemoAsync()
    {
        Console.WriteLine("👁️  Observer Pattern Demo:");
        Console.WriteLine("---------------------------");

        var cart = _cartFactory!.CreateCart(CartBackendType.InMemory);
        
        // Setup multiple observers
        var performanceObserver = new CartPerformanceObserver(slowOperationThresholdMs: 5.0);
        var loggingObserver = new CartLoggingObserver(_logger as ILogger<CartLoggingObserver> ?? 
            LoggerFactory.Create(b => b.AddConsole()).CreateLogger<CartLoggingObserver>());
        
        cart.Subscribe(performanceObserver);
        cart.Subscribe(loggingObserver);
        
        Console.WriteLine("✅ Observers attached (performance + logging)");
        Console.WriteLine("   Watch for log messages and performance tracking...");
        Console.WriteLine();

        // Perform operations that will trigger observers
        cart.AddItem("apple", 2);
        cart.AddItem("banana", 1);
        cart.AddItem("orange", 3);
        
        var total = cart.Total();
        var items = cart.Items();
        
        cart.RemoveItem("banana");
        
        // Show accumulated performance data
        DisplayPerformanceStats(performanceObserver, "Observer Demo");
        
        // Demonstrate observer error handling
        Console.WriteLine("Testing observer error handling...");
        try
        {
            cart.AddItem("unknown_product", 1);
        }
        catch (ArgumentException)
        {
            Console.WriteLine("✅ Error was properly handled and logged");
        }
        
        Console.WriteLine();
    }

    /// <summary>
    /// Demonstrates performance monitoring capabilities.
    /// </summary>
    private static async Task RunPerformanceDemoAsync()
    {
        Console.WriteLine("⚡ Performance Monitoring Demo:");
        Console.WriteLine("-------------------------------");

        var cart = _cartFactory!.CreateCart(CartBackendType.InMemory);
        var performanceObserver = new CartPerformanceObserver(slowOperationThresholdMs: 1.0); // Very low threshold
        cart.Subscribe(performanceObserver);

        // Perform many operations to collect statistics
        Console.WriteLine("Performing bulk operations...");
        for (int i = 0; i < 10; i++)
        {
            cart.AddItem("apple", 1);
            cart.AddItem("banana", 1);
            cart.AddItem("orange", 1);
            
            if (i % 3 == 0)
            {
                cart.RemoveItem("apple");
            }
            
            var _ = cart.Total(); // Trigger total calculation
            var __ = cart.Items(); // Trigger items retrieval
        }

        Console.WriteLine("✅ Completed 10 iterations of bulk operations");
        DisplayPerformanceStats(performanceObserver, "Bulk Operations");
        
        // Reset and show empty stats
        performanceObserver.Reset();
        Console.WriteLine("🔄 Performance stats reset");
        DisplayPerformanceStats(performanceObserver, "After Reset");
        
        Console.WriteLine();
    }

    /// <summary>
    /// Demonstrates multi-user cart functionality.
    /// </summary>
    private static async Task RunMultiUserDemoAsync()
    {
        Console.WriteLine("👥 Multi-User Cart Demo:");
        Console.WriteLine("------------------------");

        var userIds = new[] { "user1", "user2", "user3" };
        var userCarts = _cartFactory!.CreateMultiUserCarts(userIds);
        
        Console.WriteLine($"✅ Created {userCarts.Count} user carts");

        // Simulate different users adding items
        userCarts["user1"].AddItem("apple", 2);
        userCarts["user1"].AddItem("banana", 1);
        
        userCarts["user2"].AddItem("bread", 1);
        userCarts["user2"].AddItem("milk", 2);
        
        userCarts["user3"].AddItem("cheese", 1);
        userCarts["user3"].AddItem("chicken", 1);

        // Display all user carts
        foreach (var userCart in userCarts)
        {
            DisplayCartSummary(userCart.Value, $"User: {userCart.Key}");
        }
        
        Console.WriteLine();
    }

    /// <summary>
    /// Displays a summary of cart contents.
    /// </summary>
    private static void DisplayCartSummary(ICart cart, string title)
    {
        Console.WriteLine($"📋 {title}:");
        
        var items = cart.Items();
        if (!items.Any())
        {
            Console.WriteLine("   🛒 Cart is empty");
            return;
        }

        foreach (var item in items.OrderBy(i => i.Key))
        {
            Console.WriteLine($"   • {item.Key} x {item.Value}");
        }
        
        Console.WriteLine($"   💰 Total: ${cart.Total():F2}");
        Console.WriteLine();
    }

    /// <summary>
    /// Displays performance statistics.
    /// </summary>
    private static void DisplayPerformanceStats(CartPerformanceObserver observer, string title)
    {
        Console.WriteLine($"📊 Performance Stats - {title}:");
        
        var stats = observer.GetPerformanceStats();
        if (!stats.Any())
        {
            Console.WriteLine("   📈 No performance data available");
            return;
        }

        foreach (var stat in stats.OrderBy(s => s.Key))
        {
            Console.WriteLine($"   • {stat.Value}");
        }
        
        Console.WriteLine();
    }

    /// <summary>
    /// Cleanup resources.
    /// </summary>
    private static async Task CleanupAsync()
    {
        if (_redisConnection != null)
        {
            await _redisConnection.DisposeAsync();
            Console.WriteLine("🧹 Redis connection disposed");
        }
    }
} 