using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using SearchAutocomplete.Domain.Entities;
using SearchAutocomplete.Infrastructure.Data;
using System.Net;
using System.Text.Json;

namespace SearchAutocomplete.Tests.Integration;

[TestFixture]
public class AutocompleteControllerIntegrationTests
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
                        options.UseInMemoryDatabase("AutocompleteTestDb");
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
                Content = "Buddha teachings about compassion",
                SectionId = 1,
                Type = "Teaching",
                Author = "Buddha"
            },
            new KinhThanh
            {
                Id = 2,
                Content = "Buddhist meditation practices",
                SectionId = 1,
                Type = "Practice",
                Author = "Buddha"
            },
            new KinhThanh
            {
                Id = 3,
                Content = "Mindfulness in daily life",
                SectionId = 2,
                Type = "Practice",
                Author = "Thich Nhat Hanh"
            },
            new KinhThanh
            {
                Id = 4,
                Content = "The art of living peacefully",
                SectionId = 2,
                Type = "Teaching",
                Author = "Thich Nhat Hanh"
            }
        };

        context.Sections.AddRange(sections);
        context.KinhThanhs.AddRange(kinhThanhs);
        await context.SaveChangesAsync();
    }

    [Test]
    public async Task GetSuggestions_WithValidSearchTerm_ShouldReturnSuggestions()
    {
        // Act
        var response = await _client.GetAsync("/api/autocomplete/suggestions?searchTerm=Bu");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var content = await response.Content.ReadAsStringAsync();
        var suggestions = JsonSerializer.Deserialize<List<string>>(content, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        suggestions.Should().NotBeNull();
        suggestions.Should().NotBeEmpty();
        suggestions.Should().Contain(s => s.Contains("Buddha"));
    }

    [Test]
    public async Task GetSuggestions_WithShortSearchTerm_ShouldReturnBadRequest()
    {
        // Act
        var response = await _client.GetAsync("/api/autocomplete/suggestions?searchTerm=B");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Test]
    public async Task GetSuggestions_WithEmptySearchTerm_ShouldReturnBadRequest()
    {
        // Act
        var response = await _client.GetAsync("/api/autocomplete/suggestions?searchTerm=");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Test]
    public async Task GetSuggestions_WithFilters_ShouldReturnFilteredSuggestions()
    {
        // Act
        var response = await _client.GetAsync("/api/autocomplete/suggestions?searchTerm=te&types=Teaching");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var content = await response.Content.ReadAsStringAsync();
        var suggestions = JsonSerializer.Deserialize<List<string>>(content, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        suggestions.Should().NotBeNull();
        // Should only return suggestions from Teaching type
        suggestions.Should().OnlyContain(s => 
            s.Contains("Buddha teachings") || s.Contains("The art of living"));
    }

    [Test]
    public async Task GetSuggestions_WithMaxResults_ShouldLimitResults()
    {
        // Act
        var response = await _client.GetAsync("/api/autocomplete/suggestions?searchTerm=th&maxResults=2");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var content = await response.Content.ReadAsStringAsync();
        var suggestions = JsonSerializer.Deserialize<List<string>>(content, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        suggestions.Should().NotBeNull();
        suggestions.Count.Should().BeLessThanOrEqualTo(2);
    }

    [Test]
    public async Task GetSuggestions_WithExcessiveMaxResults_ShouldLimitTo10()
    {
        // Act
        var response = await _client.GetAsync("/api/autocomplete/suggestions?searchTerm=th&maxResults=15");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var content = await response.Content.ReadAsStringAsync();
        var suggestions = JsonSerializer.Deserialize<List<string>>(content, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        suggestions.Should().NotBeNull();
        suggestions.Count.Should().BeLessThanOrEqualTo(10); // Should be limited to 10
    }

    [Test]
    public async Task GetSuggestions_WithNoMatches_ShouldReturnEmptyList()
    {
        // Act
        var response = await _client.GetAsync("/api/autocomplete/suggestions?searchTerm=xyz");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var content = await response.Content.ReadAsStringAsync();
        var suggestions = JsonSerializer.Deserialize<List<string>>(content, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        suggestions.Should().NotBeNull();
        suggestions.Should().BeEmpty();
    }

    [Test]
    public async Task GetSuggestions_WithMultipleFilters_ShouldApplyAllFilters()
    {
        // Act
        var response = await _client.GetAsync("/api/autocomplete/suggestions?searchTerm=th&types=Teaching&authors=Buddha");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var content = await response.Content.ReadAsStringAsync();
        var suggestions = JsonSerializer.Deserialize<List<string>>(content, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        suggestions.Should().NotBeNull();
        // Should only return suggestions that match both Teaching type AND Buddha author
        suggestions.Should().OnlyContain(s => s.Contains("Buddha teachings"));
    }

    [Test]
    public async Task GetSuggestions_WithSectionFilter_ShouldReturnSectionSpecificResults()
    {
        // Act
        var response = await _client.GetAsync("/api/autocomplete/suggestions?searchTerm=th&sectionIds=2");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var content = await response.Content.ReadAsStringAsync();
        var suggestions = JsonSerializer.Deserialize<List<string>>(content, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        suggestions.Should().NotBeNull();
        // Should only return suggestions from section 2
        suggestions.Should().OnlyContain(s => 
            s.Contains("Mindfulness") || s.Contains("The art of living"));
    }
}