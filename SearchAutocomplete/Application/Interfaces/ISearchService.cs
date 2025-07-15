using SearchAutocomplete.Application.DTOs;

namespace SearchAutocomplete.Application.Interfaces;

public interface ISearchService
{
    Task<SearchResultDto> SearchAsync(SearchRequestDto request);
    Task<int> GetSearchCountAsync(SearchRequestDto request);
}