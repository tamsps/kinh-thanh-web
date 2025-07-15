using Microsoft.Data.Sqlite;
using Polly;
using Polly.CircuitBreaker;
using Polly.Timeout;
using System.Diagnostics;
using System.Net;

namespace SearchAutocomplete.Infrastructure.Resilience;

public class ResilienceService
{
    private readonly ILogger<ResilienceService> _logger;
    private readonly ResiliencePipeline _databasePipeline;
    private readonly ResiliencePipeline _httpPipeline;

    public ResilienceService(ILogger<ResilienceService> logger)
    {
        _logger = logger;
        _databasePipeline = CreateDatabaseResiliencePipeline();
        _httpPipeline = CreateHttpResiliencePipeline();
    }

    /// <summary>
    /// Execute database operations with retry logic and circuit breaker
    /// </summary>
    public async Task<T> ExecuteDatabaseOperationAsync<T>(Func<Task<T>> operation, string operationName)
    {
        var stopwatch = Stopwatch.StartNew();
        
        try
        {
            var result = await _databasePipeline.ExecuteAsync(async (cancellationToken) =>
            {
                _logger.LogDebug("Executing database operation: {OperationName}", operationName);
                return await operation();
            });

            stopwatch.Stop();
            _logger.LogDebug("Database operation {OperationName} completed successfully in {ElapsedMs}ms", 
                operationName, stopwatch.ElapsedMilliseconds);
            
            return result;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "Database operation {OperationName} failed after {ElapsedMs}ms", 
                operationName, stopwatch.ElapsedMilliseconds);
            throw;
        }
    }

    /// <summary>
    /// Execute HTTP operations with retry logic and circuit breaker
    /// </summary>
    public async Task<T> ExecuteHttpOperationAsync<T>(Func<Task<T>> operation, string operationName)
    {
        var stopwatch = Stopwatch.StartNew();
        
        try
        {
            var result = await _httpPipeline.ExecuteAsync(async (cancellationToken) =>
            {
                _logger.LogDebug("Executing HTTP operation: {OperationName}", operationName);
                return await operation();
            });

            stopwatch.Stop();
            _logger.LogDebug("HTTP operation {OperationName} completed successfully in {ElapsedMs}ms", 
                operationName, stopwatch.ElapsedMilliseconds);
            
            return result;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "HTTP operation {OperationName} failed after {ElapsedMs}ms", 
                operationName, stopwatch.ElapsedMilliseconds);
            throw;
        }
    }

    /// <summary>
    /// Legacy method for backward compatibility - routes to database operations
    /// </summary>
    public async Task<T> ExecuteWithRetryAsync<T>(Func<Task<T>> operation, string operationName, int maxRetries = 3)
    {
        return await ExecuteDatabaseOperationAsync(operation, operationName);
    }

    private ResiliencePipeline CreateDatabaseResiliencePipeline()
    {
        return new ResiliencePipelineBuilder()
            .AddRetry(new Polly.Retry.RetryStrategyOptions
            {
                ShouldHandle = new PredicateBuilder().Handle<SqliteException>()
                    .Handle<TimeoutException>()
                    .Handle<TaskCanceledException>(),
                MaxRetryAttempts = 3,
                Delay = TimeSpan.FromSeconds(1),
                BackoffType = Polly.DelayBackoffType.Exponential,
                UseJitter = true,
                OnRetry = args =>
                {
                    _logger.LogWarning("Database operation retry {AttemptNumber} after {Delay}ms. Exception: {Exception}", 
                        args.AttemptNumber, args.RetryDelay.TotalMilliseconds, args.Outcome.Exception?.Message);
                    return ValueTask.CompletedTask;
                }
            })
            .AddCircuitBreaker(new CircuitBreakerStrategyOptions
            {
                ShouldHandle = new PredicateBuilder().Handle<SqliteException>()
                    .Handle<TimeoutException>(),
                FailureRatio = 0.5,
                SamplingDuration = TimeSpan.FromSeconds(10),
                MinimumThroughput = 5,
                BreakDuration = TimeSpan.FromSeconds(30),
                OnOpened = args =>
                {
                    _logger.LogError("Database circuit breaker opened due to {Exception}", args.Outcome.Exception?.Message);
                    return ValueTask.CompletedTask;
                },
                OnClosed = args =>
                {
                    _logger.LogInformation("Database circuit breaker closed");
                    return ValueTask.CompletedTask;
                },
                OnHalfOpened = args =>
                {
                    _logger.LogInformation("Database circuit breaker half-opened");
                    return ValueTask.CompletedTask;
                }
            })
            .AddTimeout(TimeSpan.FromSeconds(30))
            .Build();
    }

    private ResiliencePipeline CreateHttpResiliencePipeline()
    {
        return new ResiliencePipelineBuilder()
            .AddRetry(new Polly.Retry.RetryStrategyOptions
            {
                ShouldHandle = new PredicateBuilder().Handle<HttpRequestException>()
                    .Handle<TaskCanceledException>()
                    .Handle<TimeoutException>(),
                MaxRetryAttempts = 2,
                Delay = TimeSpan.FromMilliseconds(500),
                BackoffType = Polly.DelayBackoffType.Exponential,
                UseJitter = true,
                OnRetry = args =>
                {
                    _logger.LogWarning("HTTP operation retry {AttemptNumber} after {Delay}ms. Exception: {Exception}", 
                        args.AttemptNumber, args.RetryDelay.TotalMilliseconds, args.Outcome.Exception?.Message);
                    return ValueTask.CompletedTask;
                }
            })
            .AddCircuitBreaker(new CircuitBreakerStrategyOptions
            {
                ShouldHandle = new PredicateBuilder().Handle<HttpRequestException>()
                    .Handle<TimeoutException>(),
                FailureRatio = 0.5,
                SamplingDuration = TimeSpan.FromSeconds(10),
                MinimumThroughput = 3,
                BreakDuration = TimeSpan.FromSeconds(15),
                OnOpened = args =>
                {
                    _logger.LogError("HTTP circuit breaker opened due to {Exception}", args.Outcome.Exception?.Message);
                    return ValueTask.CompletedTask;
                },
                OnClosed = args =>
                {
                    _logger.LogInformation("HTTP circuit breaker closed");
                    return ValueTask.CompletedTask;
                },
                OnHalfOpened = args =>
                {
                    _logger.LogInformation("HTTP circuit breaker half-opened");
                    return ValueTask.CompletedTask;
                }
            })
            .AddTimeout(TimeSpan.FromSeconds(10))
            .Build();
    }
}