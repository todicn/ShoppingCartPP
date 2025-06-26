using System.Diagnostics;
using ShoppingCart.Core.Interfaces;

namespace ShoppingCart.Core.Implementations;

/// <summary>
/// Base abstract cart implementation that provides observer pattern functionality.
/// </summary>
public abstract class BaseObservableCart : ICart
{
    private readonly List<ICartObserver> _observers;
    private readonly object _observerLock = new();

    /// <summary>
    /// Initializes a new instance of the BaseObservableCart class.
    /// </summary>
    protected BaseObservableCart()
    {
        _observers = new List<ICartObserver>();
    }

    /// <summary>
    /// Subscribes an observer to cart operations.
    /// </summary>
    public void Subscribe(ICartObserver observer)
    {
        if (observer == null) throw new ArgumentNullException(nameof(observer));
        
        lock (_observerLock)
        {
            if (!_observers.Contains(observer))
            {
                _observers.Add(observer);
            }
        }
    }

    /// <summary>
    /// Unsubscribes an observer from cart operations.
    /// </summary>
    public void Unsubscribe(ICartObserver observer)
    {
        if (observer == null) return;
        
        lock (_observerLock)
        {
            _observers.Remove(observer);
        }
    }

    /// <summary>
    /// Adds an item to the cart with the specified quantity.
    /// </summary>
    public void AddItem(string productId, int quantity)
    {
        var stopwatch = Stopwatch.StartNew();
        try
        {
            AddItemCore(productId, quantity);
            stopwatch.Stop();
            NotifyItemAdded(productId, quantity, stopwatch.Elapsed);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            NotifyError("AddItem", ex, stopwatch.Elapsed);
            throw;
        }
    }

    /// <summary>
    /// Removes all quantities of the specified product from the cart.
    /// </summary>
    public void RemoveItem(string productId)
    {
        var stopwatch = Stopwatch.StartNew();
        try
        {
            RemoveItemCore(productId);
            stopwatch.Stop();
            NotifyItemRemoved(productId, stopwatch.Elapsed);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            NotifyError("RemoveItem", ex, stopwatch.Elapsed);
            throw;
        }
    }

    /// <summary>
    /// Calculates the total cost of all items in the cart.
    /// </summary>
    public decimal Total()
    {
        var stopwatch = Stopwatch.StartNew();
        try
        {
            var total = TotalCore();
            var itemCount = GetItemCountCore();
            stopwatch.Stop();
            NotifyTotalCalculated(total, itemCount, stopwatch.Elapsed);
            return total;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            NotifyError("Total", ex, stopwatch.Elapsed);
            throw;
        }
    }

    /// <summary>
    /// Returns a dictionary of items and their quantities in the cart.
    /// </summary>
    public Dictionary<string, int> Items()
    {
        var stopwatch = Stopwatch.StartNew();
        try
        {
            var items = ItemsCore();
            stopwatch.Stop();
            NotifyItemsRetrieved(items.Count, stopwatch.Elapsed);
            return items;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            NotifyError("Items", ex, stopwatch.Elapsed);
            throw;
        }
    }

    /// <summary>
    /// Core implementation of adding an item to the cart.
    /// </summary>
    protected abstract void AddItemCore(string productId, int quantity);

    /// <summary>
    /// Core implementation of removing an item from the cart.
    /// </summary>
    protected abstract void RemoveItemCore(string productId);

    /// <summary>
    /// Core implementation of calculating the total.
    /// </summary>
    protected abstract decimal TotalCore();

    /// <summary>
    /// Core implementation of getting cart items.
    /// </summary>
    protected abstract Dictionary<string, int> ItemsCore();

    /// <summary>
    /// Gets the number of items in the cart.
    /// </summary>
    protected abstract int GetItemCountCore();

    /// <summary>
    /// Notifies observers when an item is added.
    /// </summary>
    private void NotifyItemAdded(string productId, int quantity, TimeSpan executionTime)
    {
        lock (_observerLock)
        {
            foreach (var observer in _observers)
            {
                try
                {
                    observer.OnItemAdded(productId, quantity, executionTime);
                }
                catch (Exception ex)
                {
                    // Log observer errors but don't let them affect cart operation
                    Debug.WriteLine($"Observer error in OnItemAdded: {ex.Message}");
                }
            }
        }
    }

    /// <summary>
    /// Notifies observers when an item is removed.
    /// </summary>
    private void NotifyItemRemoved(string productId, TimeSpan executionTime)
    {
        lock (_observerLock)
        {
            foreach (var observer in _observers)
            {
                try
                {
                    observer.OnItemRemoved(productId, executionTime);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Observer error in OnItemRemoved: {ex.Message}");
                }
            }
        }
    }

    /// <summary>
    /// Notifies observers when total is calculated.
    /// </summary>
    private void NotifyTotalCalculated(decimal total, int itemCount, TimeSpan executionTime)
    {
        lock (_observerLock)
        {
            foreach (var observer in _observers)
            {
                try
                {
                    observer.OnTotalCalculated(total, itemCount, executionTime);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Observer error in OnTotalCalculated: {ex.Message}");
                }
            }
        }
    }

    /// <summary>
    /// Notifies observers when items are retrieved.
    /// </summary>
    private void NotifyItemsRetrieved(int itemCount, TimeSpan executionTime)
    {
        lock (_observerLock)
        {
            foreach (var observer in _observers)
            {
                try
                {
                    observer.OnItemsRetrieved(itemCount, executionTime);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Observer error in OnItemsRetrieved: {ex.Message}");
                }
            }
        }
    }

    /// <summary>
    /// Notifies observers when an error occurs.
    /// </summary>
    private void NotifyError(string operation, Exception exception, TimeSpan executionTime)
    {
        lock (_observerLock)
        {
            foreach (var observer in _observers)
            {
                try
                {
                    observer.OnError(operation, exception, executionTime);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Observer error in OnError: {ex.Message}");
                }
            }
        }
    }
} 