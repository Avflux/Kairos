using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace kairos.Components.Kanban.Services
{
    public interface IPerformanceService
    {
        Task<T> DebounceAsync<T>(string key, Func<Task<T>> operation, int delayMs = 300);
        Task DebounceAsync(string key, Func<Task> operation, int delayMs = 300);
        void CancelDebounce(string key);
        Task<T> ThrottleAsync<T>(string key, Func<Task<T>> operation, int intervalMs = 1000);
        Task ThrottleAsync(string key, Func<Task> operation, int intervalMs = 1000);
        void TrackPerformance(string operationName, TimeSpan duration);
        PerformanceMetrics GetMetrics(string operationName);
        void ClearMetrics();
    }

    public class PerformanceService : IPerformanceService, IDisposable
    {
        private readonly ConcurrentDictionary<string, CancellationTokenSource> _debounceCancellations = new();
        private readonly ConcurrentDictionary<string, DateTime> _throttleLastExecution = new();
        private readonly ConcurrentDictionary<string, PerformanceMetrics> _performanceMetrics = new();
        private readonly object _lockObject = new();

        public async Task<T> DebounceAsync<T>(string key, Func<Task<T>> operation, int delayMs = 300)
        {
            // Cancel any existing debounce for this key
            CancelDebounce(key);

            // Create new cancellation token
            var cts = new CancellationTokenSource();
            _debounceCancellations[key] = cts;

            try
            {
                // Wait for the debounce delay
                await Task.Delay(delayMs, cts.Token);

                // Execute the operation if not cancelled
                var startTime = DateTime.UtcNow;
                var result = await operation();
                var duration = DateTime.UtcNow - startTime;
                
                TrackPerformance($"debounce_{key}", duration);
                
                return result;
            }
            catch (OperationCanceledException)
            {
                // Operation was cancelled (debounced)
                throw;
            }
            finally
            {
                // Clean up
                _debounceCancellations.TryRemove(key, out _);
                cts.Dispose();
            }
        }

        public async Task DebounceAsync(string key, Func<Task> operation, int delayMs = 300)
        {
            await DebounceAsync(key, async () =>
            {
                await operation();
                return true;
            }, delayMs);
        }

        public void CancelDebounce(string key)
        {
            if (_debounceCancellations.TryRemove(key, out var existingCts))
            {
                existingCts.Cancel();
                existingCts.Dispose();
            }
        }

        public async Task<T> ThrottleAsync<T>(string key, Func<Task<T>> operation, int intervalMs = 1000)
        {
            lock (_lockObject)
            {
                var now = DateTime.UtcNow;
                
                if (_throttleLastExecution.TryGetValue(key, out var lastExecution))
                {
                    var timeSinceLastExecution = now - lastExecution;
                    if (timeSinceLastExecution.TotalMilliseconds < intervalMs)
                    {
                        // Too soon, skip this execution
                        return default(T);
                    }
                }
                
                _throttleLastExecution[key] = now;
            }

            var startTime = DateTime.UtcNow;
            var result = await operation();
            var duration = DateTime.UtcNow - startTime;
            
            TrackPerformance($"throttle_{key}", duration);
            
            return result;
        }

        public async Task ThrottleAsync(string key, Func<Task> operation, int intervalMs = 1000)
        {
            await ThrottleAsync(key, async () =>
            {
                await operation();
                return true;
            }, intervalMs);
        }

        public void TrackPerformance(string operationName, TimeSpan duration)
        {
            _performanceMetrics.AddOrUpdate(operationName, 
                new PerformanceMetrics(operationName, duration),
                (key, existing) => existing.AddMeasurement(duration));
        }

        public PerformanceMetrics GetMetrics(string operationName)
        {
            return _performanceMetrics.TryGetValue(operationName, out var metrics) 
                ? metrics 
                : new PerformanceMetrics(operationName);
        }

        public void ClearMetrics()
        {
            _performanceMetrics.Clear();
        }

        public void Dispose()
        {
            // Cancel all pending debounce operations
            foreach (var cts in _debounceCancellations.Values)
            {
                cts.Cancel();
                cts.Dispose();
            }
            _debounceCancellations.Clear();
            _throttleLastExecution.Clear();
            _performanceMetrics.Clear();
        }
    }

    public class PerformanceMetrics
    {
        private readonly List<TimeSpan> _measurements = new();
        private readonly object _lock = new();

        public string OperationName { get; }
        public int ExecutionCount { get; private set; }
        public TimeSpan TotalDuration { get; private set; }
        public TimeSpan AverageDuration => ExecutionCount > 0 ? TimeSpan.FromTicks(TotalDuration.Ticks / ExecutionCount) : TimeSpan.Zero;
        public TimeSpan MinDuration { get; private set; } = TimeSpan.MaxValue;
        public TimeSpan MaxDuration { get; private set; } = TimeSpan.MinValue;
        public DateTime LastExecution { get; private set; }

        public PerformanceMetrics(string operationName)
        {
            OperationName = operationName;
        }

        public PerformanceMetrics(string operationName, TimeSpan initialDuration) : this(operationName)
        {
            AddMeasurement(initialDuration);
        }

        public PerformanceMetrics AddMeasurement(TimeSpan duration)
        {
            lock (_lock)
            {
                _measurements.Add(duration);
                ExecutionCount++;
                TotalDuration = TotalDuration.Add(duration);
                LastExecution = DateTime.UtcNow;

                if (duration < MinDuration)
                    MinDuration = duration;
                
                if (duration > MaxDuration)
                    MaxDuration = duration;
            }

            return this;
        }

        public TimeSpan GetPercentile(double percentile)
        {
            lock (_lock)
            {
                if (_measurements.Count == 0)
                    return TimeSpan.Zero;

                var sortedMeasurements = _measurements.OrderBy(m => m.Ticks).ToList();
                var index = (int)Math.Ceiling(percentile / 100.0 * sortedMeasurements.Count) - 1;
                index = Math.Max(0, Math.Min(index, sortedMeasurements.Count - 1));
                
                return sortedMeasurements[index];
            }
        }

        public override string ToString()
        {
            return $"{OperationName}: {ExecutionCount} executions, Avg: {AverageDuration.TotalMilliseconds:F2}ms, " +
                   $"Min: {MinDuration.TotalMilliseconds:F2}ms, Max: {MaxDuration.TotalMilliseconds:F2}ms";
        }
    }
}