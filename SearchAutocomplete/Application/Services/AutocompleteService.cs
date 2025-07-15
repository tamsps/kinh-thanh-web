using SearchAutocomplete.Application.DTOs;
using SearchAutocomplete.Application.Interfaces;
using SearchAutocomplete.Domain.Interfaces;
using SearchAutocomplete.Infrastructure.Logging;
using System.Diagnostics;

namespace SearchAutocomplete.Application.Services;

public class AutocompleteService : IAutocompleteService
{
    private readonly IKinhThanhRepository _kinhThanhRepository;
    private readonly PerformanceLogger _performanceLogger;

    public AutocompleteService(IKinhThanhRepository kinhThanhRepository, PerformanceLogger performanceLogger)
    {
        _kinhThanhRepository = kinhThanhRepository;
        _performanceLogger = performanceLogger;
    }

    public async Task<IEnumerable<string>> GetSuggestionsAsync(AutocompleteRequestDto request)
    {
        if (string.IsNullOrWhiteSpace(request.SearchTerm) || request.SearchTerm.Length < 2)
        {
            return Enumerable.Empty<string>();
        }

        var stopwatch = Stopwatch.StartNew();
        
        try
        {
            var maxResults = Math.Min(request.MaxResults, 10); // Limit to maximum 10 results
            var suggestions = await _kinhThanhRepository.GetAutocompleteSuggestionsAsync(
                request.SearchTerm,
                request.Filters,
                maxResults);

            stopwatch.Stop();
            
            // Log autocomplete performance metrics
            var context = new Dictionary<string, object>
            {
                ["MaxResults"] = maxResults,
                ["FilterCount"] = request.Filters.Types.Count + request.Filters.Authors.Count + request.Filters.SectionIds.Count
            };
            
            _performanceLogger.LogAutocompleteMetrics(request.SearchTerm, suggestions.Count(), stopwatch.ElapsedMilliseconds, context);

            return suggestions;
        }
        catch (Exception)
        {
            stopwatch.Stop();
            
            // Log failed autocomplete attempt
            var context = new Dictionary<string, object>
            {
                ["MaxResults"] = Math.Min(request.MaxResults, 10),
                ["FilterCount"] = request.Filters.Types.Count + request.Filters.Authors.Count + request.Filters.SectionIds.Count
            };
            
            _performanceLogger.LogAutocompleteMetrics(request.SearchTerm, 0, stopwatch.ElapsedMilliseconds, context);
            throw;
        }
    }
}