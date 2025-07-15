namespace SearchAutocomplete.Application.DTOs;

public class SearchResultDto
{
    public IEnumerable<KinhThanhDto> Results { get; set; } = new List<KinhThanhDto>();
    public int TotalCount { get; set; }
    public int CurrentPage { get; set; }
    public int TotalPages { get; set; }
    public bool HasNextPage => CurrentPage < TotalPages;
    public bool HasPreviousPage => CurrentPage > 1;
}