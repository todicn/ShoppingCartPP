# Shopping Cart Implementation

A comprehensive C# implementation of a shopping cart system with clean architecture, dependency injection, and comprehensive testing.

## 🏗️ Architecture

This project follows a clean architecture pattern with the following components:

- **ShoppingCart.Core** - Core business logic with interfaces and implementations
- **ShoppingCart.Tests** - Comprehensive unit tests using xUnit
- **ShoppingCart.Demo** - Console application demonstrating the functionality

## 🎯 Features

- **Add Items**: Add products with specified quantities to the cart
- **Remove Items**: Remove all quantities of a product from the cart
- **Calculate Total**: Get the total cost of all items in the cart
- **View Items**: Get a list of all items and their quantities
- **Price Lookup**: Prices are retrieved from a configurable product service
- **Error Handling**: Comprehensive validation for invalid inputs
- **Dependency Injection**: Proper separation of concerns with interface-based design
- **Comprehensive Testing**: Full test coverage with xUnit

## 🛍️ Available Products

The cart supports the following products with their prices:

| Product | Price |
|---------|-------|
| apple   | $0.50 |
| banana  | $0.30 |
| orange  | $0.75 |
| bread   | $2.50 |
| milk    | $3.25 |
| cheese  | $4.99 |
| chicken | $8.99 |
| rice    | $1.99 |

## 🔧 Prerequisites

- .NET 8.0 SDK or later
- PowerShell (for build script)

## 🚀 Quick Start

### Option 1: Using the Build Script (Recommended)
```powershell
.\build.ps1
```

This will:
- Clean previous builds
- Restore dependencies
- Build the solution
- Run all tests
- Execute the demo application

### Option 2: Manual Commands

```bash
# Restore dependencies
dotnet restore

# Build the solution
dotnet build

# Run tests
dotnet test

# Run demo application
dotnet run --project ShoppingCart.Demo
```

## 📚 API Reference

### ICart Interface

#### `AddItem(string productId, int quantity)`
- Adds the specified quantity of a product to the cart
- If the product already exists, adds to the existing quantity
- Case-insensitive product lookup
- Throws `ArgumentException` for invalid inputs

#### `RemoveItem(string productId)`
- Removes all quantities of the specified product from the cart
- Case-insensitive product lookup
- No error if product doesn't exist in cart
- Throws `ArgumentException` for null/empty product ID

#### `Total()`
- Returns the total cost of all items in the cart as a decimal
- Returns 0 for empty cart

#### `Items()`
- Returns a copy of the cart contents as a `Dictionary<string, int>`
- Key: product ID (lowercase), Value: quantity
- Returns empty dictionary for empty cart

### IProductService Interface

#### `GetPrice(string productId)`
- Gets the price of a product by its identifier
- Throws `ArgumentException` for invalid or unknown products

#### `ProductExists(string productId)`
- Checks if a product exists in the catalog
- Returns boolean indicating existence

#### `GetAllProducts()`
- Returns all available products and their prices
- Useful for displaying product catalog

## 💻 Usage Example

```csharp
using ShoppingCart.Core.Implementations;
using ShoppingCart.Core.Interfaces;

// Create services using dependency injection
IProductService productService = new ProductService();
ICart cart = new Cart(productService);

// Add items
cart.AddItem("apple", 5);
cart.AddItem("banana", 3);
cart.AddItem("bread", 1);

// View cart contents
var items = cart.Items();
foreach (var item in items)
{
    Console.WriteLine($"{item.Key}: {item.Value}");
}

// Get total cost
decimal total = cart.Total();
Console.WriteLine($"Total: ${total:F2}");

// Remove an item
cart.RemoveItem("banana");

// Add more of existing item
cart.AddItem("apple", 2); // Now has 7 apples total
```

## 🧪 Testing

The project includes comprehensive unit tests covering:

- **Normal Functionality**: All methods work as expected
- **Edge Cases**: Empty carts, non-existent products, etc.
- **Error Conditions**: Invalid inputs, null values, etc.
- **Case Sensitivity**: Products are handled case-insensitively
- **Dependency Injection**: Proper constructor validation
- **Integration Tests**: Complete workflow scenarios

### Running Tests

```bash
# Run all tests
dotnet test

# Run tests with detailed output
dotnet test --verbosity normal

# Run tests in a specific project
dotnet test ShoppingCart.Tests
```

## 📁 Project Structure

```
ShoppingCartPP/
├── ShoppingCart.Core/
│   ├── Interfaces/
│   │   ├── ICart.cs
│   │   └── IProductService.cs
│   ├── Implementations/
│   │   ├── Cart.cs
│   │   └── ProductService.cs
│   └── ShoppingCart.Core.csproj
├── ShoppingCart.Tests/
│   ├── Implementations/
│   │   ├── CartTests.cs
│   │   └── ProductServiceTests.cs
│   └── ShoppingCart.Tests.csproj
├── ShoppingCart.Demo/
│   ├── Program.cs
│   └── ShoppingCart.Demo.csproj
├── ShoppingCartPP.sln
├── build.ps1
└── README.md
```

## 🔍 Error Handling

The cart validates all inputs and throws appropriate exceptions:

- **ArgumentException**: For invalid product IDs, quantities, or unknown products
- **ArgumentNullException**: For null dependencies in constructors

All exceptions include descriptive error messages to help with debugging.

## 🎨 Design Patterns

This implementation demonstrates several design patterns:

- **Dependency Injection**: Cart depends on IProductService abstraction
- **Interface Segregation**: Clean separation between cart and product concerns
- **Defensive Programming**: Returns copies to prevent external modification
- **Factory Pattern**: Could be extended with cart factory for different cart types

## 📈 Future Enhancements

Potential areas for expansion:

- **Discount System**: Apply percentage or fixed-amount discounts
- **Cart Persistence**: Save/load cart state to/from storage
- **Multi-Currency**: Support for different currencies
- **Tax Calculation**: Add tax computation based on location
- **Product Categories**: Group products by category
- **Quantity Validation**: Set min/max quantities per product
- **Cart Expiration**: Implement time-based cart expiration

## 🤝 Contributing

1. Fork the repository
2. Create a feature branch
3. Make your changes
4. Add tests for new functionality
5. Ensure all tests pass
6. Submit a pull request

## 📄 License

This project is provided as-is for educational and demonstration purposes. 