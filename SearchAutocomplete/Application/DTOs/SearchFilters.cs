namespace SearchAutocomplete.Application.DTOs;

public class SearchFilters
{
    public List<string> Types { get; set; } = new();
    public List<string> Authors { get; set; } = new();
    public List<int> SectionIds { get; set; } = new();
}