using Microsoft.AspNetCore.Mvc;
using SearchAutocomplete.Application.DTOs;
using SearchAutocomplete.Application.Interfaces;

namespace SearchAutocomplete.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AutocompleteController : ControllerBase
{
    private readonly IAutocompleteService _autocompleteService;
    private readonly ILogger<AutocompleteController> _logger;

    public AutocompleteController(IAutocompleteService autocompleteService, ILogger<AutocompleteController> logger)
    {
        _autocompleteService = autocompleteService;
        _logger = logger;
    }

    [HttpGet("suggestions")]
    public async Task<ActionResult<IEnumerable<string>>> GetSuggestions(
        [FromQuery] string searchTerm,
        [FromQuery] List<string>? bookNames = null,
        [FromQuery] List<string>? bookTypes = null,
        [FromQuery] List<int>? chapterNumbers = null,
        [FromQuery] int maxResults = 10)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(searchTerm))
            {
                return BadRequest(new { message = "Search term is required" });
            }

            if (searchTerm.Length < 2)
            {
                return BadRequest(new { message = "Search term must be at least 2 characters long" });
            }

            var request = new AutocompleteRequestDto
            {
                SearchTerm = searchTerm,
                Filters = new SearchFilters
                {
                    BookNames = bookNames ?? new List<string>(),
                    BookTypes = bookTypes ?? new List<string>(),
                    ChapterNumbers = chapterNumbers ?? new List<int>()
                },
                MaxResults = Math.Min(maxResults, 10) // Ensure maximum 10 results
            };

            _logger.LogDebug("Getting autocomplete suggestions for term: {SearchTerm}", searchTerm);

            var suggestions = await _autocompleteService.GetSuggestionsAsync(request);

            return Ok(suggestions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while getting autocomplete suggestions for term: {SearchTerm}", searchTerm);
            return StatusCode(500, new { message = "An error occurred while getting suggestions" });
        }
    }
}