using System.ComponentModel.DataAnnotations;

namespace SearchAutocomplete.Application.DTOs;

public class AutocompleteRequestDto
{
    [Required]
    [MinLength(2)]
    public string SearchTerm { get; set; } = string.Empty;
    
    public SearchFilters Filters { get; set; } = new();
    
    [Range(1, 20)]
    public int MaxResults { get; set; } = 10;
}