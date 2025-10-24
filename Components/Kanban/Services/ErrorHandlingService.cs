using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace kairos.Components.Kanban.Services
{
    public interface IErrorHandlingService
    {
        Task<T> ExecuteWithRetryAsync<T>(Func<Task<T>> operation, string operationName, int maxRetries = 3, int baseDelayMs = 1000);
        Task ExecuteWithRetryAsync(Func<Task> operation, string operationName, int maxRetries = 3, int baseDelayMs = 1000);
        Task<T> ExecuteWithFallbackAsync<T>(Func<Task<T>> operation, Func<Task<T>> fallback, string operationName);
        Task ExecuteWithFallbackAsync(Func<Task> operation, Func<Task> fallback, string operationName);
        Task<T> ExecuteWithCircuitBreakerAsync<T>(Func<Task<T>> operation, string circuitName);
        Task ExecuteWithCircuitBreakerAsync(Func<Task> operation, string circuitName);
        void LogError(Exception exception, string operationName, object context = null);
        void LogWarning(string message, string operationName, object context = null);
        void LogInfo(string message, string operationName, object context = null);
        ErrorStatistics GetErrorStatistics(string operationName = null);
        void ClearErrorStatistics();
        bool IsOperationHealthy(string operationName);
    }

    public class ErrorHandlingService : IErrorHandlingService
    {
        private readonly ILogger<ErrorHandlingService> _logger;
        private readonly IUserFeedbackService _feedbackService;
        private readonly ConcurrentDictionary<string, CircuitBreakerState> _circuitBreakers = new();
        private readonly ConcurrentDictionary<string, ErrorStatistics> _errorStatistics = new();
        private readonly object _lockObject = new();

        public ErrorHandlingService(ILogger<ErrorHandlingService> logger, IUserFeedbackService feedbackService)
        {
            _logger = logger;
            _feedbackService = feedbackService;
        }

        public async Task<T> ExecuteWithRetryAsync<T>(Func<Task<T>> operation, string operationName, int maxRetries = 3, int baseDelayMs = 1000)
        {
            var attempt = 0;
            Exception lastException = null;

            while (attempt <= maxRetries)
            {
                try
                {
                    LogInfo($"Executando operação (tentativa {attempt + 1}/{maxRetries + 1})", operationName);
                    
                    var result = await operation();
                    
                    if (attempt > 0)
                    {
                        LogInfo($"Operação bem-sucedida após {attempt + 1} tentativa(s)", operationName);
                        await _feedbackService.ShowSuccessAsync($"Operação '{operationName}' executada com sucesso após {attempt + 1} tentativa(s)!");
                    }
                    
                    RecordSuccess(operationName);
                    return result;
                }
                catch (Exception ex)
                {
                    lastException = ex;
                    attempt++;
                    
                    LogError(ex, operationName, new { Attempt = attempt, MaxRetries = maxRetries });
                    RecordError(operationName, ex);

                    if (attempt <= maxRetries)
                    {
                        var delay = CalculateExponentialBackoff(attempt, baseDelayMs);
                        LogWarning($"Tentativa {attempt} falhou. Tentando novamente em {delay}ms", operationName);
                        
                        await _feedbackService.ShowWarningAsync($"Tentativa {attempt} falhou. Tentando novamente...", 2000);
                        await Task.Delay(delay);
                    }
                }
            }

            // All retries failed
            LogError(lastException, operationName, new { FinalFailure = true, TotalAttempts = attempt });
            await _feedbackService.ShowErrorAsync($"Operação '{operationName}' falhou após {maxRetries + 1} tentativas.");
            
            throw new OperationFailedException($"Operação '{operationName}' falhou após {maxRetries + 1} tentativas", lastException);
        }

        public async Task ExecuteWithRetryAsync(Func<Task> operation, string operationName, int maxRetries = 3, int baseDelayMs = 1000)
        {
            await ExecuteWithRetryAsync(async () =>
            {
                await operation();
                return true;
            }, operationName, maxRetries, baseDelayMs);
        }

        public async Task<T> ExecuteWithFallbackAsync<T>(Func<Task<T>> operation, Func<Task<T>> fallback, string operationName)
        {
            try
            {
                LogInfo("Executando operação principal", operationName);
                var result = await operation();
                RecordSuccess(operationName);
                return result;
            }
            catch (Exception ex)
            {
                LogError(ex, operationName, new { UsingFallback = true });
                RecordError(operationName, ex);
                
                try
                {
                    LogWarning("Executando operação de fallback", operationName);
                    await _feedbackService.ShowWarningAsync($"Usando método alternativo para '{operationName}'...", 3000);
                    
                    var fallbackResult = await fallback();
                    
                    LogInfo("Operação de fallback bem-sucedida", operationName);
                    await _feedbackService.ShowInfoAsync($"Operação '{operationName}' executada usando método alternativo.");
                    
                    return fallbackResult;
                }
                catch (Exception fallbackEx)
                {
                    LogError(fallbackEx, operationName, new { FallbackFailed = true });
                    await _feedbackService.ShowErrorAsync($"Tanto a operação principal quanto o método alternativo falharam para '{operationName}'.");
                    
                    throw new OperationFailedException($"Tanto a operação principal quanto o fallback falharam para '{operationName}'", 
                        new AggregateException(ex, fallbackEx));
                }
            }
        }

        public async Task ExecuteWithFallbackAsync(Func<Task> operation, Func<Task> fallback, string operationName)
        {
            await ExecuteWithFallbackAsync(async () =>
            {
                await operation();
                return true;
            }, async () =>
            {
                await fallback();
                return true;
            }, operationName);
        }

        public async Task<T> ExecuteWithCircuitBreakerAsync<T>(Func<Task<T>> operation, string circuitName)
        {
            var circuitBreaker = GetOrCreateCircuitBreaker(circuitName);
            
            if (circuitBreaker.State == CircuitState.Open)
            {
                if (DateTime.UtcNow < circuitBreaker.NextAttemptTime)
                {
                    LogWarning("Circuit breaker está aberto, rejeitando operação", circuitName);
                    await _feedbackService.ShowWarningAsync($"Serviço '{circuitName}' temporariamente indisponível. Tente novamente em alguns momentos.");
                    throw new CircuitBreakerOpenException($"Circuit breaker para '{circuitName}' está aberto");
                }
                else
                {
                    // Transition to half-open
                    circuitBreaker.State = CircuitState.HalfOpen;
                    LogInfo("Circuit breaker transitioning para half-open", circuitName);
                }
            }

            try
            {
                var result = await operation();
                
                // Success - reset circuit breaker
                if (circuitBreaker.State == CircuitState.HalfOpen)
                {
                    circuitBreaker.State = CircuitState.Closed;
                    circuitBreaker.FailureCount = 0;
                    LogInfo("Circuit breaker resetado para closed", circuitName);
                }
                
                return result;
            }
            catch (Exception ex)
            {
                circuitBreaker.FailureCount++;
                LogError(ex, circuitName, new { FailureCount = circuitBreaker.FailureCount });
                
                if (circuitBreaker.FailureCount >= circuitBreaker.FailureThreshold)
                {
                    circuitBreaker.State = CircuitState.Open;
                    circuitBreaker.NextAttemptTime = DateTime.UtcNow.AddMilliseconds(circuitBreaker.TimeoutMs);
                    LogWarning($"Circuit breaker aberto devido a {circuitBreaker.FailureCount} falhas", circuitName);
                    await _feedbackService.ShowErrorAsync($"Serviço '{circuitName}' temporariamente indisponível devido a múltiplas falhas.");
                }
                
                throw;
            }
        }

        public async Task ExecuteWithCircuitBreakerAsync(Func<Task> operation, string circuitName)
        {
            await ExecuteWithCircuitBreakerAsync(async () =>
            {
                await operation();
                return true;
            }, circuitName);
        }

        public void LogError(Exception exception, string operationName, object context = null)
        {
            _logger.LogError(exception, "Erro na operação {OperationName}. Contexto: {@Context}", operationName, context);
        }

        public void LogWarning(string message, string operationName, object context = null)
        {
            _logger.LogWarning("Aviso na operação {OperationName}: {Message}. Contexto: {@Context}", operationName, message, context);
        }

        public void LogInfo(string message, string operationName, object context = null)
        {
            _logger.LogInformation("Info na operação {OperationName}: {Message}. Contexto: {@Context}", operationName, message, context);
        }

        public ErrorStatistics GetErrorStatistics(string operationName = null)
        {
            if (operationName != null)
            {
                return _errorStatistics.TryGetValue(operationName, out var stats) ? stats : new ErrorStatistics(operationName);
            }

            // Aggregate all statistics
            var aggregated = new ErrorStatistics("All Operations");
            foreach (var stats in _errorStatistics.Values)
            {
                aggregated.TotalOperations += stats.TotalOperations;
                aggregated.SuccessfulOperations += stats.SuccessfulOperations;
                aggregated.FailedOperations += stats.FailedOperations;
                
                if (stats.LastError != null && (aggregated.LastError == null || stats.LastErrorTime > aggregated.LastErrorTime))
                {
                    aggregated.LastError = stats.LastError;
                    aggregated.LastErrorTime = stats.LastErrorTime;
                }
            }
            
            return aggregated;
        }

        public void ClearErrorStatistics()
        {
            _errorStatistics.Clear();
        }

        public bool IsOperationHealthy(string operationName)
        {
            if (!_errorStatistics.TryGetValue(operationName, out var stats))
                return true; // No data means healthy

            if (stats.TotalOperations < 10)
                return true; // Not enough data

            var errorRate = (double)stats.FailedOperations / stats.TotalOperations;
            return errorRate < 0.1; // Less than 10% error rate is considered healthy
        }

        private int CalculateExponentialBackoff(int attempt, int baseDelayMs)
        {
            // Exponential backoff with jitter
            var delay = baseDelayMs * Math.Pow(2, attempt - 1);
            var jitter = new Random().NextDouble() * 0.1 * delay; // 10% jitter
            return (int)(delay + jitter);
        }

        private CircuitBreakerState GetOrCreateCircuitBreaker(string circuitName)
        {
            return _circuitBreakers.GetOrAdd(circuitName, _ => new CircuitBreakerState
            {
                State = CircuitState.Closed,
                FailureThreshold = 5,
                TimeoutMs = 30000, // 30 seconds
                FailureCount = 0
            });
        }

        private void RecordSuccess(string operationName)
        {
            var stats = _errorStatistics.GetOrAdd(operationName, _ => new ErrorStatistics(operationName));
            lock (_lockObject)
            {
                stats.TotalOperations++;
                stats.SuccessfulOperations++;
            }
        }

        private void RecordError(string operationName, Exception exception)
        {
            var stats = _errorStatistics.GetOrAdd(operationName, _ => new ErrorStatistics(operationName));
            lock (_lockObject)
            {
                stats.TotalOperations++;
                stats.FailedOperations++;
                stats.LastError = exception;
                stats.LastErrorTime = DateTime.UtcNow;
            }
        }
    }

    public class CircuitBreakerState
    {
        public CircuitState State { get; set; }
        public int FailureCount { get; set; }
        public int FailureThreshold { get; set; }
        public int TimeoutMs { get; set; }
        public DateTime NextAttemptTime { get; set; }
    }

    public enum CircuitState
    {
        Closed,
        Open,
        HalfOpen
    }

    public class ErrorStatistics
    {
        public string OperationName { get; }
        public int TotalOperations { get; set; }
        public int SuccessfulOperations { get; set; }
        public int FailedOperations { get; set; }
        public Exception? LastError { get; set; }
        public DateTime LastErrorTime { get; set; }
        
        public double SuccessRate => TotalOperations > 0 ? (double)SuccessfulOperations / TotalOperations : 0;
        public double ErrorRate => TotalOperations > 0 ? (double)FailedOperations / TotalOperations : 0;

        public ErrorStatistics(string operationName)
        {
            OperationName = operationName;
        }

        public override string ToString()
        {
            return $"{OperationName}: {TotalOperations} ops, {SuccessRate:P1} success rate, {ErrorRate:P1} error rate";
        }
    }

    public class OperationFailedException : Exception
    {
        public OperationFailedException(string message) : base(message) { }
        public OperationFailedException(string message, Exception innerException) : base(message, innerException) { }
    }

    public class CircuitBreakerOpenException : Exception
    {
        public CircuitBreakerOpenException(string message) : base(message) { }
    }
}