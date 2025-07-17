using Microsoft.AspNetCore.Mvc;
using SearchAutocomplete.Application.DTOs;
using SearchAutocomplete.Application.Interfaces;
using SearchAutocomplete.Domain.Interfaces;
using System.ComponentModel.DataAnnotations;

namespace SearchAutocomplete.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SearchController : ControllerBase
{
    private readonly ISearchService _searchService;
    private readonly IKinhThanhRepository _kinhThanhRepository;
    private readonly ILogger<SearchController> _logger;

    public SearchController(ISearchService searchService, IKinhThanhRepository kinhThanhRepository, ILogger<SearchController> logger)
    {
        _searchService = searchService;
        _kinhThanhRepository = kinhThanhRepository;
        _logger = logger;
    }

    [HttpPost]
    public async Task<ActionResult<SearchResultDto>> Search([FromBody] SearchRequestDto request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            if (string.IsNullOrWhiteSpace(request.SearchTerm))
            {
                return BadRequest(new { message = "Search term is required" });
            }

            _logger.LogInformation("Executing search with term: {SearchTerm}, Page: {Page}, PageSize: {PageSize}", 
                request.SearchTerm, request.Page, request.PageSize);

            var result = await _searchService.SearchAsync(request);

            _logger.LogInformation("Search completed. Found {TotalCount} results", result.TotalCount);

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred during search with term: {SearchTerm}", request.SearchTerm);
            return StatusCode(500, new { message = "An error occurred while processing your search" });
        }
    }

    [HttpGet("count")]
    public async Task<ActionResult<int>> GetSearchCount([FromQuery] string searchTerm, 
        [FromQuery] List<string>? bookNames = null,
        [FromQuery] List<string>? bookTypes = null,
        [FromQuery] List<int>? chapterNumbers = null)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(searchTerm))
            {
                return BadRequest(new { message = "Search term is required" });
            }

            var request = new SearchRequestDto
            {
                SearchTerm = searchTerm,
                Filters = new SearchFilters
                {
                    BookNames = bookNames ?? new List<string>(),
                    BookTypes = bookTypes ?? new List<string>(),
                    ChapterNumbers = chapterNumbers ?? new List<int>()
                }
            };

            var count = await _searchService.GetSearchCountAsync(request);
            return Ok(count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while getting search count for term: {SearchTerm}", searchTerm);
            return StatusCode(500, new { message = "An error occurred while processing your request" });
        }
    }



    [HttpGet("filters/book-names")]
    public async Task<ActionResult<IEnumerable<string>>> GetAvailableBookNames()
    {
        try
        {
            _logger.LogInformation("Getting available book names from database");
            var bookNames = await _kinhThanhRepository.GetDistinctBookNamesAsync();
            return Ok(bookNames);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while getting available book names");
            return StatusCode(500, new { message = "An error occurred while getting filter options" });
        }
    }

    [HttpGet("filters/book-types")]
    public async Task<ActionResult<IEnumerable<string>>> GetAvailableBookTypes()
    {
        try
        {
            _logger.LogInformation("Getting available book types from database");
            var bookTypes = await _kinhThanhRepository.GetDistinctBookTypesAsync();
            return Ok(bookTypes);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while getting available book types");
            return StatusCode(500, new { message = "An error occurred while getting filter options" });
        }
    }
}