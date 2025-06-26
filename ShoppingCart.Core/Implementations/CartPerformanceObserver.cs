using System.Diagnostics;
using ShoppingCart.Core.Interfaces;

namespace ShoppingCart.Core.Implementations;

/// <summary>
/// Observer that monitors cart operation performance and collects metrics.
/// </summary>
public class CartPerformanceObserver : ICartObserver
{
    private readonly TimeSpan _slowOperationThreshold;
    private readonly Dictionary<string, List<double>> _operationTimes;
    private readonly object _lock = new();

    /// <summary>
    /// Initializes a new instance of the CartPerformanceObserver class.
    /// </summary>
    /// <param name="slowOperationThresholdMs">Threshold in milliseconds for marking operations as slow.</param>
    public CartPerformanceObserver(double slowOperationThresholdMs = 100.0)
    {
        _slowOperationThreshold = TimeSpan.FromMilliseconds(slowOperationThresholdMs);
        _operationTimes = new Dictionary<string, List<double>>();
    }

    /// <summary>
    /// Called when an item is added to the cart.
    /// </summary>
    public void OnItemAdded(string productId, int quantity, TimeSpan executionTime)
    {
        RecordOperation("AddItem", executionTime);
        LogSlowOperation("AddItem", productId, executionTime);
    }

    /// <summary>
    /// Called when an item is removed from the cart.
    /// </summary>
    public void OnItemRemoved(string productId, TimeSpan executionTime)
    {
        RecordOperation("RemoveItem", executionTime);
        LogSlowOperation("RemoveItem", productId, executionTime);
    }

    /// <summary>
    /// Called when the cart total is calculated.
    /// </summary>
    public void OnTotalCalculated(decimal total, int itemCount, TimeSpan executionTime)
    {
        RecordOperation("Total", executionTime);
        LogSlowOperation("Total", $"items:{itemCount}", executionTime);
    }

    /// <summary>
    /// Called when cart items are retrieved.
    /// </summary>
    public void OnItemsRetrieved(int itemCount, TimeSpan executionTime)
    {
        RecordOperation("Items", executionTime);
        LogSlowOperation("Items", $"count:{itemCount}", executionTime);
    }

    /// <summary>
    /// Called when an error occurs during cart operations.
    /// </summary>
    public void OnError(string operation, Exception exception, TimeSpan executionTime)
    {
        RecordOperation($"{operation}_Error", executionTime);
        
        // Always log errors regardless of time threshold
        Debug.WriteLine($"[PERF ERROR] {operation} failed after {executionTime.TotalMilliseconds:F2}ms: {exception.Message}");
    }

    /// <summary>
    /// Gets performance statistics for all operations.
    /// </summary>
    /// <returns>Dictionary of operation names and their statistics.</returns>
    public Dictionary<string, OperationStats> GetPerformanceStats()
    {
        lock (_lock)
        {
            var stats = new Dictionary<string, OperationStats>();
            
            foreach (var kvp in _operationTimes)
            {
                var times = kvp.Value;
                if (times.Count > 0)
                {
                    stats[kvp.Key] = new OperationStats
                    {
                        OperationName = kvp.Key,
                        TotalCalls = times.Count,
                        AverageTimeMs = times.Average(),
                        MinTimeMs = times.Min(),
                        MaxTimeMs = times.Max(),
                        SlowCalls = times.Count(t => t > _slowOperationThreshold.TotalMilliseconds)
                    };
                }
            }
            
            return stats;
        }
    }

    /// <summary>
    /// Resets all collected performance data.
    /// </summary>
    public void Reset()
    {
        lock (_lock)
        {
            _operationTimes.Clear();
        }
    }

    /// <summary>
    /// Records the execution time for an operation.
    /// </summary>
    private void RecordOperation(string operationName, TimeSpan executionTime)
    {
        lock (_lock)
        {
            if (!_operationTimes.ContainsKey(operationName))
            {
                _operationTimes[operationName] = new List<double>();
            }
            
            _operationTimes[operationName].Add(executionTime.TotalMilliseconds);
        }
    }

    /// <summary>
    /// Logs slow operations to debug output.
    /// </summary>
    private void LogSlowOperation(string operation, string context, TimeSpan executionTime)
    {
        if (executionTime > _slowOperationThreshold)
        {
            Debug.WriteLine($"[PERF SLOW] {operation} ({context}) took {executionTime.TotalMilliseconds:F2}ms (threshold: {_slowOperationThreshold.TotalMilliseconds}ms)");
        }
    }
}

/// <summary>
/// Performance statistics for a specific operation.
/// </summary>
public class OperationStats
{
    /// <summary>
    /// Name of the operation.
    /// </summary>
    public required string OperationName { get; set; }

    /// <summary>
    /// Total number of calls made.
    /// </summary>
    public int TotalCalls { get; set; }

    /// <summary>
    /// Average execution time in milliseconds.
    /// </summary>
    public double AverageTimeMs { get; set; }

    /// <summary>
    /// Minimum execution time in milliseconds.
    /// </summary>
    public double MinTimeMs { get; set; }

    /// <summary>
    /// Maximum execution time in milliseconds.
    /// </summary>
    public double MaxTimeMs { get; set; }

    /// <summary>
    /// Number of calls that exceeded the slow operation threshold.
    /// </summary>
    public int SlowCalls { get; set; }

    /// <summary>
    /// Returns a string representation of the statistics.
    /// </summary>
    public override string ToString()
    {
        return $"{OperationName}: {TotalCalls} calls, avg {AverageTimeMs:F2}ms, {SlowCalls} slow";
    }
} 