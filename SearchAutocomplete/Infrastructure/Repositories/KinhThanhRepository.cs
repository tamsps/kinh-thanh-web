using Microsoft.EntityFrameworkCore;
using SearchAutocomplete.Domain.Entities;
using SearchAutocomplete.Domain.Interfaces;
using SearchAutocomplete.Infrastructure.Data;
using SearchAutocomplete.Application.DTOs;
using SearchAutocomplete.Infrastructure.Resilience;

namespace SearchAutocomplete.Infrastructure.Repositories;

public class KinhThanhRepository : IKinhThanhRepository
{
    private readonly SearchDbContext _context;
    private readonly ResilienceService _resilienceService;

    public KinhThanhRepository(SearchDbContext context, ResilienceService resilienceService)
    {
        _context = context;
        _resilienceService = resilienceService;
    }

    public async Task<IEnumerable<KinhThanh>> SearchAsync(string searchTerm, SearchFilters filters, int page, int pageSize)
    {
        return await _resilienceService.ExecuteDatabaseOperationAsync(async () =>
        {
            var query = _context.KinhThanhs.Include(k => k.Section).AsQueryable();

            // Apply search term filter - only search in content
            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                query = query.Where(k => k.Content.Contains(searchTerm));
            }

            // Apply filters
            query = ApplyFilters(query, filters);

            // Apply pagination
            return await query
                .OrderBy(k => k.Id)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }, $"SearchAsync(term: {searchTerm}, page: {page}, pageSize: {pageSize})");
    }

    public async Task<IEnumerable<string>> GetAutocompleteSuggestionsAsync(string searchTerm, SearchFilters filters, int maxResults)
    {
        if (string.IsNullOrWhiteSpace(searchTerm) || searchTerm.Length < 2)
            return Enumerable.Empty<string>();

        return await _resilienceService.ExecuteDatabaseOperationAsync(async () =>
        {
            var query = _context.KinhThanhs.AsQueryable();

            // Apply filters
            query = ApplyFilters(query, filters);

            // Get suggestions from content field
            var suggestions = await query
                .Where(k => k.Content.Contains(searchTerm))
                .Select(k => k.Content)
                .Distinct()
                .Take(maxResults)
                .ToListAsync();

            return suggestions;
        }, $"GetAutocompleteSuggestionsAsync(term: {searchTerm}, maxResults: {maxResults})");
    }

    public async Task<int> GetSearchCountAsync(string searchTerm, SearchFilters filters)
    {
        return await _resilienceService.ExecuteDatabaseOperationAsync(async () =>
        {
            var query = _context.KinhThanhs.AsQueryable();

            // Apply search term filter - only search in content
            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                query = query.Where(k => k.Content.Contains(searchTerm));
            }

            // Apply filters
            query = ApplyFilters(query, filters);

            return await query.CountAsync();
        }, $"GetSearchCountAsync(term: {searchTerm})");
    }



    public async Task<IEnumerable<string>> GetDistinctBookNamesAsync()
    {
        return await _resilienceService.ExecuteDatabaseOperationAsync(async () =>
        {
            return await _context.KinhThanhs
                .Where(k => !string.IsNullOrEmpty(k.BookName))
                .Select(k => k.BookName)
                .Distinct()
                .OrderBy(b => b)
                .ToListAsync();
        }, "GetDistinctBookNamesAsync");
    }

    public async Task<IEnumerable<string>> GetDistinctBookTypesAsync()
    {
        return await _resilienceService.ExecuteDatabaseOperationAsync(async () =>
        {
            return await _context.KinhThanhs
                .Where(k => !string.IsNullOrEmpty(k.BookType))
                .Select(k => k.BookType)
                .Distinct()
                .OrderBy(t => t)
                .ToListAsync();
        }, "GetDistinctBookTypesAsync");
    }

    private IQueryable<KinhThanh> ApplyFilters(IQueryable<KinhThanh> query, SearchFilters filters)
    {
        if (filters.BookNames.Any())
        {
            query = query.Where(k => filters.BookNames.Contains(k.BookName));
        }

        if (filters.BookTypes.Any())
        {
            query = query.Where(k => filters.BookTypes.Contains(k.BookType));
        }

        if (filters.ChapterNumbers.Any())
        {
            query = query.Where(k => filters.ChapterNumbers.Contains(k.ChapterNumber));
        }

        return query;
    }
}