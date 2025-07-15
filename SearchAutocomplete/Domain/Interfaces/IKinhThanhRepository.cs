using SearchAutocomplete.Domain.Entities;
using SearchAutocomplete.Application.DTOs;

namespace SearchAutocomplete.Domain.Interfaces;

public interface IKinhThanhRepository
{
    Task<IEnumerable<KinhThanh>> SearchAsync(string searchTerm, SearchFilters filters, int page, int pageSize);
    Task<IEnumerable<string>> GetAutocompleteSuggestionsAsync(string searchTerm, SearchFilters filters, int maxResults);
    Task<int> GetSearchCountAsync(string searchTerm, SearchFilters filters);
    Task<IEnumerable<string>> GetDistinctTypesAsync();
    Task<IEnumerable<string>> GetDistinctAuthorsAsync();
}