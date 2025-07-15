using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using SearchAutocomplete.Application.DTOs;
using SearchAutocomplete.Domain.Entities;
using SearchAutocomplete.Infrastructure.Data;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace SearchAutocomplete.Tests.Integration;

[TestFixture]
public class SearchControllerIntegrationTests
{
    private WebApplicationFactory<Program> _factory;
    private HttpClient _client;

    [OneTimeSetUp]
    public void OneTimeSetUp()
    {
        _factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    // Remove the real database context registration
                    var descriptor = services.SingleOrDefault(
                        d => d.ServiceType == typeof(DbContextOptions<SearchDbContext>));
                    if (descriptor != null)
                        services.Remove(descriptor);

                    // Also remove the DbContext registration itself
                    var contextDescriptor = services.SingleOrDefault(
                        d => d.ServiceType == typeof(SearchDbContext));
                    if (contextDescriptor != null)
                        services.Remove(contextDescriptor);

                    // Add in-memory database for testing
                    services.AddDbContext<SearchDbContext>(options =>
                    {
                        options.UseInMemoryDatabase("TestDb");
                    });
                });
            });

        _client = _factory.CreateClient();
    }

    [OneTimeTearDown]
    public void OneTimeTearDown()
    {
        _client?.Dispose();
        _factory?.Dispose();
    }

    [SetUp]
    public async Task SetUp()
    {
        // Seed test data
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<SearchDbContext>();
        
        await context.Database.EnsureDeletedAsync();
        await context.Database.EnsureCreatedAsync();

        var sections = new List<Section>
        {
            new Section { Id = 1, Name = "Section 1", Description = "First section" },
            new Section { Id = 2, Name = "Section 2", Description = "Second section" }
        };

        var kinhThanhs = new List<KinhThanh>
        {
            new KinhThanh
            {
                Id = 1,
                Content = "This is test content for searching",
                SectionId = 1,
                Type = "Type1",
                Author = "Author1",
                From = "Page 1",
                To = "Page 2"
            },
            new KinhThanh
            {
                Id = 2,
                Content = "Another test content with different keywords",
                SectionId = 2,
                Type = "Type2",
                Author = "Author2",
                From = "Page 3",
                To = "Page 4"
            },
            new KinhThanh
            {
                Id = 3,
                Content = "More test data for comprehensive testing",
                SectionId = 1,
                Type = "Type1",
                Author = "Author1",
                From = "Page 5",
                To = "Page 6"
            }
        };

        context.Sections.AddRange(sections);
        context.KinhThanhs.AddRange(kinhThanhs);
        await context.SaveChangesAsync();
    }

    [Test]
    public async Task Search_WithValidRequest_ShouldReturnResults()
    {
        // Arrange
        var request = new SearchRequestDto
        {
            SearchTerm = "test",
            Page = 1,
            PageSize = 10,
            Filters = new SearchFilters()
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/search", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var content = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<SearchResultDto>(content, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        result.Should().NotBeNull();
        result.Results.Should().HaveCount(3);
        result.TotalCount.Should().Be(3);
        result.CurrentPage.Should().Be(1);
        result.TotalPages.Should().Be(1);
    }

    [Test]
    public async Task Search_WithEmptySearchTerm_ShouldReturnBadRequest()
    {
        // Arrange
        var request = new SearchRequestDto
        {
            SearchTerm = "",
            Page = 1,
            PageSize = 10,
            Filters = new SearchFilters()
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/search", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Test]
    public async Task Search_WithFilters_ShouldReturnFilteredResults()
    {
        // Arrange
        var request = new SearchRequestDto
        {
            SearchTerm = "test",
            Page = 1,
            PageSize = 10,
            Filters = new SearchFilters
            {
                Types = new List<string> { "Type1" }
            }
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/search", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var content = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<SearchResultDto>(content, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        result.Should().NotBeNull();
        result.Results.Should().HaveCount(2); // Only Type1 results
        result.TotalCount.Should().Be(2);
    }

    [Test]
    public async Task Search_WithPagination_ShouldReturnCorrectPage()
    {
        // Arrange
        var request = new SearchRequestDto
        {
            SearchTerm = "test",
            Page = 2,
            PageSize = 2,
            Filters = new SearchFilters()
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/search", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var content = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<SearchResultDto>(content, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        result.Should().NotBeNull();
        result.Results.Should().HaveCount(1); // Last page with 1 item
        result.CurrentPage.Should().Be(2);
        result.TotalPages.Should().Be(2);
        result.TotalCount.Should().Be(3);
    }

    [Test]
    public async Task Search_WithNoResults_ShouldReturnEmptyResults()
    {
        // Arrange
        var request = new SearchRequestDto
        {
            SearchTerm = "nonexistent",
            Page = 1,
            PageSize = 10,
            Filters = new SearchFilters()
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/search", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var content = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<SearchResultDto>(content, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        result.Should().NotBeNull();
        result.Results.Should().BeEmpty();
        result.TotalCount.Should().Be(0);
    }

    [Test]
    public async Task GetSearchCount_WithValidTerm_ShouldReturnCount()
    {
        // Act
        var response = await _client.GetAsync("/api/search/count?searchTerm=test");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var content = await response.Content.ReadAsStringAsync();
        var count = JsonSerializer.Deserialize<int>(content);

        count.Should().Be(3);
    }

    [Test]
    public async Task GetSearchCount_WithEmptyTerm_ShouldReturnBadRequest()
    {
        // Act
        var response = await _client.GetAsync("/api/search/count?searchTerm=");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Test]
    public async Task GetAvailableTypes_ShouldReturnTypesList()
    {
        // Act
        var response = await _client.GetAsync("/api/search/filters/types");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var content = await response.Content.ReadAsStringAsync();
        var types = JsonSerializer.Deserialize<List<string>>(content, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        types.Should().NotBeNull();
        types.Should().NotBeEmpty();
        types.Should().Contain("Kinh");
    }

    [Test]
    public async Task GetAvailableAuthors_ShouldReturnAuthorsList()
    {
        // Act
        var response = await _client.GetAsync("/api/search/filters/authors");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var content = await response.Content.ReadAsStringAsync();
        var authors = JsonSerializer.Deserialize<List<string>>(content, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        authors.Should().NotBeNull();
        authors.Should().NotBeEmpty();
        authors.Should().Contain("Đức Phật");
    }
}