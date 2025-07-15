using SearchAutocomplete.Application.DTOs;

namespace SearchAutocomplete.Application.Interfaces;

public interface IAutocompleteService
{
    Task<IEnumerable<string>> GetSuggestionsAsync(AutocompleteRequestDto request);
}