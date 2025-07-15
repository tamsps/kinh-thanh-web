using SearchAutocomplete.Application.DTOs;
using SearchAutocomplete.Application.Interfaces;
using SearchAutocomplete.Domain.Interfaces;
using SearchAutocomplete.Infrastructure.Logging;
using System.Diagnostics;

namespace SearchAutocomplete.Application.Services;

public class SearchService : ISearchService
{
    private readonly IKinhThanhRepository _kinhThanhRepository;
    private readonly PerformanceLogger _performanceLogger;

    public SearchService(IKinhThanhRepository kinhThanhRepository, PerformanceLogger performanceLogger)
    {
        _kinhThanhRepository = kinhThanhRepository;
        _performanceLogger = performanceLogger;
    }

    public async Task<SearchResultDto> SearchAsync(SearchRequestDto request)
    {
        var stopwatch = Stopwatch.StartNew();
        
        try
        {
            var results = await _kinhThanhRepository.SearchAsync(
                request.SearchTerm, 
                request.Filters, 
                request.Page, 
                request.PageSize);

            var totalCount = await _kinhThanhRepository.GetSearchCountAsync(
                request.SearchTerm, 
                request.Filters);

            var totalPages = (int)Math.Ceiling((double)totalCount / request.PageSize);

            var kinhThanhDtos = results.Select(k => new KinhThanhDto
            {
                Id = k.Id,
                Content = k.Content,
                SectionId = k.SectionId,
                SectionName = k.Section?.Name ?? string.Empty,
                From = k.From,
                To = k.To,
                Type = k.Type,
                Author = k.Author
            });

            var searchResult = new SearchResultDto
            {
                Results = kinhThanhDtos,
                TotalCount = totalCount,
                CurrentPage = request.Page,
                TotalPages = totalPages
            };

            stopwatch.Stop();
            
            // Log search performance metrics
            var context = new Dictionary<string, object>
            {
                ["Page"] = request.Page,
                ["PageSize"] = request.PageSize,
                ["FilterCount"] = request.Filters.Types.Count + request.Filters.Authors.Count + request.Filters.SectionIds.Count
            };
            
            _performanceLogger.LogSearchMetrics(request.SearchTerm, totalCount, stopwatch.ElapsedMilliseconds, context);

            return searchResult;
        }
        catch (Exception)
        {
            stopwatch.Stop();
            
            // Log failed search attempt
            var context = new Dictionary<string, object>
            {
                ["Page"] = request.Page,
                ["PageSize"] = request.PageSize,
                ["FilterCount"] = request.Filters.Types.Count + request.Filters.Authors.Count + request.Filters.SectionIds.Count
            };
            
            _performanceLogger.LogSearchMetrics(request.SearchTerm, 0, stopwatch.ElapsedMilliseconds, context);
            throw;
        }
    }

    public async Task<int> GetSearchCountAsync(SearchRequestDto request)
    {
        return await _kinhThanhRepository.GetSearchCountAsync(request.SearchTerm, request.Filters);
    }
}