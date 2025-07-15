using System.ComponentModel.DataAnnotations;

namespace SearchAutocomplete.Application.DTOs;

public class SearchRequestDto
{
    [Required]
    [MinLength(1)]
    public string SearchTerm { get; set; } = string.Empty;
    
    public SearchFilters Filters { get; set; } = new();
    
    [Range(1, int.MaxValue)]
    public int Page { get; set; } = 1;
    
    [Range(1, 100)]
    public int PageSize { get; set; } = 10;
}