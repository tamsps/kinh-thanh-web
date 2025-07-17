namespace SearchAutocomplete.Application.DTOs;

public class SearchFilters
{
    public List<string> BookNames { get; set; } = new();
    public List<string> BookTypes { get; set; } = new();
    public List<int> ChapterNumbers { get; set; } = new();
}