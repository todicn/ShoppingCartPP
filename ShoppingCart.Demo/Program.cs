using ShoppingCart.Core.Implementations;
using ShoppingCart.Core.Interfaces;

namespace ShoppingCart.Demo;

/// <summary>
/// Demo application showcasing the shopping cart functionality.
/// </summary>
class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine("=== Shopping Cart Demo ===");
        Console.WriteLine();

        try
        {
            // Create services
            IProductService productService = new ProductService();
            ICart cart = new Cart(productService);

            // Display available products
            DisplayAvailableProducts(productService);

            // Demonstrate cart functionality
            DemonstrateCartOperations(cart, productService);

            // Show error handling
            DemonstrateErrorHandling(cart);

            Console.WriteLine();
            Console.WriteLine("=== Demo Completed Successfully ===");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error occurred: {ex.Message}");
        }

        Console.WriteLine();
        Console.WriteLine("Press any key to exit...");
        Console.ReadKey();
    }

    /// <summary>
    /// Displays all available products and their prices.
    /// </summary>
    /// <param name="productService">The product service.</param>
    private static void DisplayAvailableProducts(IProductService productService)
    {
        Console.WriteLine("📋 Available Products:");
        Console.WriteLine("---------------------");

        var products = productService.GetAllProducts();
        foreach (var product in products.OrderBy(p => p.Key))
        {
            Console.WriteLine($"• {product.Key.PadRight(10)} - ${product.Value:F2}");
        }

        Console.WriteLine();
    }

    /// <summary>
    /// Demonstrates various cart operations.
    /// </summary>
    /// <param name="cart">The shopping cart.</param>
    /// <param name="productService">The product service.</param>
    private static void DemonstrateCartOperations(ICart cart, IProductService productService)
    {
        Console.WriteLine("🛒 Cart Operations Demo:");
        Console.WriteLine("------------------------");

        // Add items to cart
        Console.WriteLine();
        Console.WriteLine("Adding items to cart:");
        AddItemToCart(cart, "apple", 5);
        AddItemToCart(cart, "banana", 3);
        AddItemToCart(cart, "bread", 2);
        AddItemToCart(cart, "milk", 1);

        // Display current cart
        DisplayCartContents(cart, productService, "Current cart contents:");

        // Add more of existing item
        Console.WriteLine();
        Console.WriteLine("Adding 2 more apples...");
        AddItemToCart(cart, "apple", 2);

        // Remove an item
        Console.WriteLine("Removing bananas...");
        cart.RemoveItem("banana");
        Console.WriteLine("✅ Removed bananas from cart");

        // Display final cart
        DisplayCartContents(cart, productService, "Final cart contents:");
    }

    /// <summary>
    /// Demonstrates error handling scenarios.
    /// </summary>
    /// <param name="cart">The shopping cart.</param>
    private static void DemonstrateErrorHandling(ICart cart)
    {
        Console.WriteLine();
        Console.WriteLine("⚠️  Error Handling Demo:");
        Console.WriteLine("-------------------------");

        // Test invalid product
        try
        {
            cart.AddItem("invalid_product", 1);
        }
        catch (ArgumentException ex)
        {
            Console.WriteLine($"✅ Expected error caught: {ex.Message}");
        }

        // Test invalid quantity
        try
        {
            cart.AddItem("apple", -1);
        }
        catch (ArgumentException ex)
        {
            Console.WriteLine($"✅ Expected error caught: {ex.Message}");
        }

        // Test null product ID
        try
        {
            cart.AddItem(null!, 1);
        }
        catch (ArgumentException ex)
        {
            Console.WriteLine($"✅ Expected error caught: {ex.Message}");
        }
    }

    /// <summary>
    /// Helper method to add an item to the cart with logging.
    /// </summary>
    /// <param name="cart">The shopping cart.</param>
    /// <param name="productId">The product identifier.</param>
    /// <param name="quantity">The quantity to add.</param>
    private static void AddItemToCart(ICart cart, string productId, int quantity)
    {
        cart.AddItem(productId, quantity);
        Console.WriteLine($"✅ Added {quantity} {productId}(s) to cart");
    }

    /// <summary>
    /// Displays the current contents of the cart.
    /// </summary>
    /// <param name="cart">The shopping cart.</param>
    /// <param name="productService">The product service.</param>
    /// <param name="title">The title to display.</param>
    private static void DisplayCartContents(ICart cart, IProductService productService, string title)
    {
        Console.WriteLine();
        Console.WriteLine(title);
        Console.WriteLine(new string('-', title.Length));

        var items = cart.Items();
        if (!items.Any())
        {
            Console.WriteLine("🛒 Cart is empty");
            return;
        }

        foreach (var item in items.OrderBy(i => i.Key))
        {
            var price = productService.GetPrice(item.Key);
            var itemTotal = price * item.Value;
            Console.WriteLine($"• {item.Key.PadRight(10)} x {item.Value.ToString().PadLeft(2)} = ${itemTotal:F2} (${price:F2} each)");
        }

        Console.WriteLine(new string('-', 40));
        Console.WriteLine($"🧮 Total: ${cart.Total():F2}");
    }
} 