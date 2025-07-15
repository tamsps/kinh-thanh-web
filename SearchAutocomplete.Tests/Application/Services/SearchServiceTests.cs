using FluentAssertions;
using Moq;
using NUnit.Framework;
using SearchAutocomplete.Application.DTOs;
using SearchAutocomplete.Application.Services;
using SearchAutocomplete.Domain.Entities;
using SearchAutocomplete.Domain.Interfaces;

namespace SearchAutocomplete.Tests.Application.Services;

[TestFixture]
public class SearchServiceTests
{
    private Mock<IKinhThanhRepository> _mockRepository;
    private SearchService _searchService;
    private Mock<SearchAutocomplete.Infrastructure.Logging.PerformanceLogger> _mockPerformanceLogger;

    [SetUp]
    public void SetUp()
    {
        _mockRepository = new Mock<IKinhThanhRepository>();
        _mockPerformanceLogger = new Mock<SearchAutocomplete.Infrastructure.Logging.PerformanceLogger>();
        _searchService = new SearchService(_mockRepository.Object, _mockPerformanceLogger.Object);
    }

    [Test]
    public async Task SearchAsync_WithValidRequest_ShouldReturnSearchResult()
    {
        // Arrange
        var request = new SearchRequestDto
        {
            SearchTerm = "test",
            Page = 1,
            PageSize = 10,
            Filters = new SearchFilters()
        };

        var mockKinhThanhs = new List<KinhThanh>
        {
            new KinhThanh
            {
                Id = 1,
                Content = "Test content 1",
                SectionId = 1,
                Section = new Section { Id = 1, Name = "Section 1" },
                Type = "Type1",
                Author = "Author1"
            },
            new KinhThanh
            {
                Id = 2,
                Content = "Test content 2",
                SectionId = 2,
                Section = new Section { Id = 2, Name = "Section 2" },
                Type = "Type2",
                Author = "Author2"
            }
        };

        _mockRepository.Setup(r => r.SearchAsync(request.SearchTerm, request.Filters, request.Page, request.PageSize))
                      .ReturnsAsync(mockKinhThanhs);
        _mockRepository.Setup(r => r.GetSearchCountAsync(request.SearchTerm, request.Filters))
                      .ReturnsAsync(2);

        // Act
        var result = await _searchService.SearchAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.Results.Should().HaveCount(2);
        result.TotalCount.Should().Be(2);
        result.CurrentPage.Should().Be(1);
        result.TotalPages.Should().Be(1);

        var firstResult = result.Results.First();
        firstResult.Id.Should().Be(1);
        firstResult.Content.Should().Be("Test content 1");
        firstResult.SectionName.Should().Be("Section 1");
    }

    [Test]
    public async Task SearchAsync_WithNoResults_ShouldReturnEmptyResult()
    {
        // Arrange
        var request = new SearchRequestDto
        {
            SearchTerm = "nonexistent",
            Page = 1,
            PageSize = 10,
            Filters = new SearchFilters()
        };

        _mockRepository.Setup(r => r.SearchAsync(request.SearchTerm, request.Filters, request.Page, request.PageSize))
                      .ReturnsAsync(new List<KinhThanh>());
        _mockRepository.Setup(r => r.GetSearchCountAsync(request.SearchTerm, request.Filters))
                      .ReturnsAsync(0);

        // Act
        var result = await _searchService.SearchAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.Results.Should().BeEmpty();
        result.TotalCount.Should().Be(0);
        result.CurrentPage.Should().Be(1);
        result.TotalPages.Should().Be(0);
    }

    [Test]
    public async Task SearchAsync_WithPagination_ShouldCalculateCorrectTotalPages()
    {
        // Arrange
        var request = new SearchRequestDto
        {
            SearchTerm = "test",
            Page = 2,
            PageSize = 5,
            Filters = new SearchFilters()
        };

        var mockKinhThanhs = new List<KinhThanh>
        {
            new KinhThanh { Id = 6, Content = "Content 6", Section = new Section { Name = "Section" } },
            new KinhThanh { Id = 7, Content = "Content 7", Section = new Section { Name = "Section" } }
        };

        _mockRepository.Setup(r => r.SearchAsync(request.SearchTerm, request.Filters, request.Page, request.PageSize))
                      .ReturnsAsync(mockKinhThanhs);
        _mockRepository.Setup(r => r.GetSearchCountAsync(request.SearchTerm, request.Filters))
                      .ReturnsAsync(12); // Total 12 items

        // Act
        var result = await _searchService.SearchAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.Results.Should().HaveCount(2);
        result.TotalCount.Should().Be(12);
        result.CurrentPage.Should().Be(2);
        result.TotalPages.Should().Be(3); // 12 items / 5 per page = 3 pages
        result.HasNextPage.Should().BeTrue();
        result.HasPreviousPage.Should().BeTrue();
    }

    [Test]
    public async Task GetSearchCountAsync_ShouldReturnCorrectCount()
    {
        // Arrange
        var request = new SearchRequestDto
        {
            SearchTerm = "test",
            Filters = new SearchFilters()
        };

        _mockRepository.Setup(r => r.GetSearchCountAsync(request.SearchTerm, request.Filters))
                      .ReturnsAsync(25);

        // Act
        var count = await _searchService.GetSearchCountAsync(request);

        // Assert
        count.Should().Be(25);
        _mockRepository.Verify(r => r.GetSearchCountAsync(request.SearchTerm, request.Filters), Times.Once);
    }

    [Test]
    public async Task SearchAsync_WithFilters_ShouldPassFiltersToRepository()
    {
        // Arrange
        var filters = new SearchFilters
        {
            Types = new List<string> { "Type1", "Type2" },
            Authors = new List<string> { "Author1" },
            SectionIds = new List<int> { 1, 2 }
        };

        var request = new SearchRequestDto
        {
            SearchTerm = "test",
            Page = 1,
            PageSize = 10,
            Filters = filters
        };

        _mockRepository.Setup(r => r.SearchAsync(It.IsAny<string>(), It.IsAny<SearchFilters>(), It.IsAny<int>(), It.IsAny<int>()))
                      .ReturnsAsync(new List<KinhThanh>());
        _mockRepository.Setup(r => r.GetSearchCountAsync(It.IsAny<string>(), It.IsAny<SearchFilters>()))
                      .ReturnsAsync(0);

        // Act
        await _searchService.SearchAsync(request);

        // Assert
        _mockRepository.Verify(r => r.SearchAsync(
            request.SearchTerm,
            It.Is<SearchFilters>(f => 
                f.Types.SequenceEqual(filters.Types) &&
                f.Authors.SequenceEqual(filters.Authors) &&
                f.SectionIds.SequenceEqual(filters.SectionIds)),
            request.Page,
            request.PageSize), Times.Once);
    }
}