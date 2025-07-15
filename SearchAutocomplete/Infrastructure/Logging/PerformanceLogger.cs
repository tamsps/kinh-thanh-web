using System.Diagnostics;

namespace SearchAutocomplete.Infrastructure.Logging;

public class PerformanceLogger
{
    private readonly ILogger<PerformanceLogger> _logger;

    public PerformanceLogger(ILogger<PerformanceLogger> logger)
    {
        _logger = logger;
    }

    public async Task<T> LogPerformanceAsync<T>(Func<Task<T>> operation, string operationName, Dictionary<string, object>? context = null)
    {
        var stopwatch = Stopwatch.StartNew();
        var startTime = DateTime.UtcNow;
        
        try
        {
            var result = await operation();
            stopwatch.Stop();
            
            var logContext = new Dictionary<string, object>
            {
                ["OperationName"] = operationName,
                ["Duration"] = stopwatch.ElapsedMilliseconds,
                ["StartTime"] = startTime,
                ["EndTime"] = DateTime.UtcNow,
                ["Success"] = true
            };

            if (context != null)
            {
                foreach (var kvp in context)
                {
                    logContext[kvp.Key] = kvp.Value;
                }
            }

            // Log performance metrics
            if (stopwatch.ElapsedMilliseconds > 1000) // Log as warning if operation takes more than 1 second
            {
                _logger.LogWarning("Slow operation detected: {OperationName} took {Duration}ms. Context: {@Context}", 
                    operationName, stopwatch.ElapsedMilliseconds, logContext);
            }
            else if (stopwatch.ElapsedMilliseconds > 200) // Log as info if operation takes more than 200ms
            {
                _logger.LogInformation("Operation {OperationName} completed in {Duration}ms. Context: {@Context}", 
                    operationName, stopwatch.ElapsedMilliseconds, logContext);
            }
            else
            {
                _logger.LogDebug("Operation {OperationName} completed in {Duration}ms. Context: {@Context}", 
                    operationName, stopwatch.ElapsedMilliseconds, logContext);
            }

            return result;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            
            var logContext = new Dictionary<string, object>
            {
                ["OperationName"] = operationName,
                ["Duration"] = stopwatch.ElapsedMilliseconds,
                ["StartTime"] = startTime,
                ["EndTime"] = DateTime.UtcNow,
                ["Success"] = false,
                ["Exception"] = ex.GetType().Name,
                ["ExceptionMessage"] = ex.Message
            };

            if (context != null)
            {
                foreach (var kvp in context)
                {
                    logContext[kvp.Key] = kvp.Value;
                }
            }

            _logger.LogError(ex, "Operation {OperationName} failed after {Duration}ms. Context: {@Context}", 
                operationName, stopwatch.ElapsedMilliseconds, logContext);
            
            throw;
        }
    }

    public void LogSearchMetrics(string searchTerm, int resultCount, long durationMs, Dictionary<string, object>? additionalContext = null)
    {
        var context = new Dictionary<string, object>
        {
            ["SearchTerm"] = searchTerm,
            ["ResultCount"] = resultCount,
            ["Duration"] = durationMs,
            ["Timestamp"] = DateTime.UtcNow
        };

        if (additionalContext != null)
        {
            foreach (var kvp in additionalContext)
            {
                context[kvp.Key] = kvp.Value;
            }
        }

        if (durationMs > 200) // Autocomplete should be under 200ms as per requirements
        {
            _logger.LogWarning("Search performance issue: Search took {Duration}ms for term '{SearchTerm}' with {ResultCount} results. Context: {@Context}", 
                durationMs, searchTerm, resultCount, context);
        }
        else
        {
            _logger.LogInformation("Search completed: {Duration}ms for term '{SearchTerm}' with {ResultCount} results. Context: {@Context}", 
                durationMs, searchTerm, resultCount, context);
        }
    }

    public void LogAutocompleteMetrics(string searchTerm, int suggestionCount, long durationMs, Dictionary<string, object>? additionalContext = null)
    {
        var context = new Dictionary<string, object>
        {
            ["SearchTerm"] = searchTerm,
            ["SuggestionCount"] = suggestionCount,
            ["Duration"] = durationMs,
            ["Timestamp"] = DateTime.UtcNow
        };

        if (additionalContext != null)
        {
            foreach (var kvp in additionalContext)
            {
                context[kvp.Key] = kvp.Value;
            }
        }

        if (durationMs > 200) // Autocomplete should be under 200ms as per requirements
        {
            _logger.LogWarning("Autocomplete performance issue: Request took {Duration}ms for term '{SearchTerm}' with {SuggestionCount} suggestions. Context: {@Context}", 
                durationMs, searchTerm, suggestionCount, context);
        }
        else
        {
            _logger.LogDebug("Autocomplete completed: {Duration}ms for term '{SearchTerm}' with {SuggestionCount} suggestions. Context: {@Context}", 
                durationMs, searchTerm, suggestionCount, context);
        }
    }
}