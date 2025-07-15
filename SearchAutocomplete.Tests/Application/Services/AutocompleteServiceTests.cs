using FluentAssertions;
using Moq;
using NUnit.Framework;
using SearchAutocomplete.Application.DTOs;
using SearchAutocomplete.Application.Services;
using SearchAutocomplete.Domain.Interfaces;

namespace SearchAutocomplete.Tests.Application.Services;

[TestFixture]
public class AutocompleteServiceTests
{
    private Mock<IKinhThanhRepository> _mockRepository;
    private AutocompleteService _autocompleteService;

    private Mock<SearchAutocomplete.Infrastructure.Logging.PerformanceLogger> _mockPerformanceLogger;

    [SetUp]
    public void SetUp()
    {
        _mockRepository = new Mock<IKinhThanhRepository>();
        _mockPerformanceLogger = new Mock<SearchAutocomplete.Infrastructure.Logging.PerformanceLogger>();
        _autocompleteService = new AutocompleteService(_mockRepository.Object, _mockPerformanceLogger.Object);
    }

    [Test]
    public async Task GetSuggestionsAsync_WithValidRequest_ShouldReturnSuggestions()
    {
        // Arrange
        var request = new AutocompleteRequestDto
        {
            SearchTerm = "test",
            MaxResults = 5,
            Filters = new SearchFilters()
        };

        var mockSuggestions = new List<string>
        {
            "test suggestion 1",
            "test suggestion 2",
            "test suggestion 3"
        };

        _mockRepository.Setup(r => r.GetAutocompleteSuggestionsAsync(request.SearchTerm, request.Filters, request.MaxResults))
                      .ReturnsAsync(mockSuggestions);

        // Act
        var result = await _autocompleteService.GetSuggestionsAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(3);
        result.Should().Contain("test suggestion 1");
        result.Should().Contain("test suggestion 2");
        result.Should().Contain("test suggestion 3");
    }

    [Test]
    public async Task GetSuggestionsAsync_WithEmptySearchTerm_ShouldReturnEmpty()
    {
        // Arrange
        var request = new AutocompleteRequestDto
        {
            SearchTerm = "",
            MaxResults = 5,
            Filters = new SearchFilters()
        };

        // Act
        var result = await _autocompleteService.GetSuggestionsAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty();
        _mockRepository.Verify(r => r.GetAutocompleteSuggestionsAsync(It.IsAny<string>(), It.IsAny<SearchFilters>(), It.IsAny<int>()), Times.Never);
    }

    [Test]
    public async Task GetSuggestionsAsync_WithShortSearchTerm_ShouldReturnEmpty()
    {
        // Arrange
        var request = new AutocompleteRequestDto
        {
            SearchTerm = "a", // Only 1 character
            MaxResults = 5,
            Filters = new SearchFilters()
        };

        // Act
        var result = await _autocompleteService.GetSuggestionsAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty();
        _mockRepository.Verify(r => r.GetAutocompleteSuggestionsAsync(It.IsAny<string>(), It.IsAny<SearchFilters>(), It.IsAny<int>()), Times.Never);
    }

    [Test]
    public async Task GetSuggestionsAsync_WithMaxResultsExceeding10_ShouldLimitTo10()
    {
        // Arrange
        var request = new AutocompleteRequestDto
        {
            SearchTerm = "test",
            MaxResults = 15, // Exceeds limit
            Filters = new SearchFilters()
        };

        var mockSuggestions = new List<string> { "suggestion" };
        _mockRepository.Setup(r => r.GetAutocompleteSuggestionsAsync(request.SearchTerm, request.Filters, 10))
                      .ReturnsAsync(mockSuggestions);

        // Act
        var result = await _autocompleteService.GetSuggestionsAsync(request);

        // Assert
        _mockRepository.Verify(r => r.GetAutocompleteSuggestionsAsync(request.SearchTerm, request.Filters, 10), Times.Once);
    }

    [Test]
    public async Task GetSuggestionsAsync_WithFilters_ShouldPassFiltersToRepository()
    {
        // Arrange
        var filters = new SearchFilters
        {
            Types = new List<string> { "Type1" },
            Authors = new List<string> { "Author1" },
            SectionIds = new List<int> { 1 }
        };

        var request = new AutocompleteRequestDto
        {
            SearchTerm = "test",
            MaxResults = 5,
            Filters = filters
        };

        _mockRepository.Setup(r => r.GetAutocompleteSuggestionsAsync(It.IsAny<string>(), It.IsAny<SearchFilters>(), It.IsAny<int>()))
                      .ReturnsAsync(new List<string>());

        // Act
        await _autocompleteService.GetSuggestionsAsync(request);

        // Assert
        _mockRepository.Verify(r => r.GetAutocompleteSuggestionsAsync(
            request.SearchTerm,
            It.Is<SearchFilters>(f => 
                f.Types.SequenceEqual(filters.Types) &&
                f.Authors.SequenceEqual(filters.Authors) &&
                f.SectionIds.SequenceEqual(filters.SectionIds)),
            5), Times.Once);
    }

    [Test]
    public async Task GetSuggestionsAsync_WithNoResults_ShouldReturnEmpty()
    {
        // Arrange
        var request = new AutocompleteRequestDto
        {
            SearchTerm = "nonexistent",
            MaxResults = 5,
            Filters = new SearchFilters()
        };

        _mockRepository.Setup(r => r.GetAutocompleteSuggestionsAsync(request.SearchTerm, request.Filters, request.MaxResults))
                      .ReturnsAsync(new List<string>());

        // Act
        var result = await _autocompleteService.GetSuggestionsAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    [Test]
    public async Task GetSuggestionsAsync_WithWhitespaceSearchTerm_ShouldReturnEmpty()
    {
        // Arrange
        var request = new AutocompleteRequestDto
        {
            SearchTerm = "   ", // Only whitespace
            MaxResults = 5,
            Filters = new SearchFilters()
        };

        // Act
        var result = await _autocompleteService.GetSuggestionsAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty();
        _mockRepository.Verify(r => r.GetAutocompleteSuggestionsAsync(It.IsAny<string>(), It.IsAny<SearchFilters>(), It.IsAny<int>()), Times.Never);
    }
}