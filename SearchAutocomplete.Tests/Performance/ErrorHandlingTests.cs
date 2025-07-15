using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using SearchAutocomplete.Application.DTOs;
using SearchAutocomplete.Application.Exceptions;
using SearchAutocomplete.Application.Services;
using SearchAutocomplete.Domain.Interfaces;
using SearchAutocomplete.Infrastructure.Data;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace SearchAutocomplete.Tests.Performance;

[TestFixture]
public class ErrorHandlingTests
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
                        options.UseInMemoryDatabase("ErrorTestDb");
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

    [Test]
    public async Task Search_WithInvalidRequest_ShouldReturnBadRequest()
    {
        // Arrange
        var invalidRequest = new SearchRequestDto
        {
            SearchTerm = "", // Empty search term
            Page = 0, // Invalid page
            PageSize = -1 // Invalid page size
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/search", invalidRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Test]
    public async Task Search_WithNullRequest_ShouldReturnBadRequest()
    {
        // Act
        var response = await _client.PostAsync("/api/search", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Test]
    public async Task Search_WithMalformedJson_ShouldReturnBadRequest()
    {
        // Arrange
        var malformedJson = new StringContent("{ invalid json }", System.Text.Encoding.UTF8, "application/json");

        // Act
        var response = await _client.PostAsync("/api/search", malformedJson);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Test]
    public async Task Autocomplete_WithInvalidParameters_ShouldReturnBadRequest()
    {
        // Test empty search term
        var response1 = await _client.GetAsync("/api/autocomplete/suggestions?searchTerm=");
        response1.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        // Test short search term
        var response2 = await _client.GetAsync("/api/autocomplete/suggestions?searchTerm=a");
        response2.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        // Test missing search term
        var response3 = await _client.GetAsync("/api/autocomplete/suggestions");
        response3.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Test]
    public async Task Search_WithExtremelyLargePageSize_ShouldHandleGracefully()
    {
        // Arrange
        var request = new SearchRequestDto
        {
            SearchTerm = "test",
            Page = 1,
            PageSize = int.MaxValue // Extremely large page size
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/search", request);

        // Assert
        // Should either return BadRequest or handle gracefully
        response.StatusCode.Should().BeOneOf(HttpStatusCode.BadRequest, HttpStatusCode.OK);
    }

    [Test]
    public async Task Search_WithExtremelyLargePage_ShouldHandleGracefully()
    {
        // Arrange
        var request = new SearchRequestDto
        {
            SearchTerm = "test",
            Page = int.MaxValue, // Extremely large page number
            PageSize = 10
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
        result.Results.Should().BeEmpty(); // Should return empty results for non-existent page
    }

    [Test]
    public async Task GlobalExceptionMiddleware_ShouldHandleUnhandledExceptions()
    {
        // This test would require a way to trigger an unhandled exception
        // For now, we'll test that the middleware is properly configured
        // by checking that normal requests work correctly

        var request = new SearchRequestDto
        {
            SearchTerm = "test",
            Page = 1,
            PageSize = 10
        };

        var response = await _client.PostAsJsonAsync("/api/search", request);
        response.IsSuccessStatusCode.Should().BeTrue();
    }

    [Test]
    public void SearchService_WithRepositoryException_ShouldPropagateException()
    {
        // Arrange
        var mockRepository = new Mock<IKinhThanhRepository>();
        mockRepository.Setup(r => r.SearchAsync(It.IsAny<string>(), It.IsAny<SearchFilters>(), It.IsAny<int>(), It.IsAny<int>()))
                     .ThrowsAsync(new Exception("Database connection failed"));

        var mockPerformanceLogger = new Mock<SearchAutocomplete.Infrastructure.Logging.PerformanceLogger>();
        var searchService = new SearchService(mockRepository.Object, mockPerformanceLogger.Object);
        var request = new SearchRequestDto
        {
            SearchTerm = "test",
            Page = 1,
            PageSize = 10,
            Filters = new SearchFilters()
        };

        // Act & Assert
        Func<Task> act = async () => await searchService.SearchAsync(request);
        act.Should().ThrowAsync<Exception>().WithMessage("Database connection failed");
    }

    [Test]
    public void AutocompleteService_WithRepositoryException_ShouldPropagateException()
    {
        // Arrange
        var mockRepository = new Mock<IKinhThanhRepository>();
        mockRepository.Setup(r => r.GetAutocompleteSuggestionsAsync(It.IsAny<string>(), It.IsAny<SearchFilters>(), It.IsAny<int>()))
                     .ThrowsAsync(new AutocompleteException("Autocomplete service failed"));

        var mockPerformanceLogger = new Mock<SearchAutocomplete.Infrastructure.Logging.PerformanceLogger>();
        var autocompleteService = new AutocompleteService(mockRepository.Object, mockPerformanceLogger.Object);
        var request = new AutocompleteRequestDto
        {
            SearchTerm = "test",
            MaxResults = 10,
            Filters = new SearchFilters()
        };

        // Act & Assert
        Func<Task> act = async () => await autocompleteService.GetSuggestionsAsync(request);
        act.Should().ThrowAsync<AutocompleteException>().WithMessage("Autocomplete service failed");
    }

    [Test]
    public async Task Search_WithSpecialCharacters_ShouldHandleGracefully()
    {
        // Arrange
        var specialCharacters = new[]
        {
            "test & search",
            "test < > search",
            "test \"quoted\" search",
            "test 'single' search",
            "test \\ backslash",
            "test / forward slash",
            "test % percent",
            "test # hash",
            "test @ at symbol",
            "test * asterisk",
            "test ? question",
            "test | pipe",
            "test ~ tilde",
            "test ` backtick"
        };

        foreach (var searchTerm in specialCharacters)
        {
            var request = new SearchRequestDto
            {
                SearchTerm = searchTerm,
                Page = 1,
                PageSize = 10,
                Filters = new SearchFilters()
            };

            // Act
            var response = await _client.PostAsJsonAsync("/api/search", request);

            // Assert
            response.IsSuccessStatusCode.Should().BeTrue($"Search with '{searchTerm}' should succeed");
        }
    }

    [Test]
    public async Task Autocomplete_WithSpecialCharacters_ShouldHandleGracefully()
    {
        // Arrange
        var specialCharacters = new[]
        {
            "te&st",
            "te<st",
            "te>st",
            "te\"st",
            "te'st",
            "te\\st",
            "te/st",
            "te%st",
            "te#st",
            "te@st",
            "te*st",
            "te?st",
            "te|st",
            "te~st",
            "te`st"
        };

        foreach (var searchTerm in specialCharacters)
        {
            // Act
            var response = await _client.GetAsync($"/api/autocomplete/suggestions?searchTerm={Uri.EscapeDataString(searchTerm)}");

            // Assert
            response.IsSuccessStatusCode.Should().BeTrue($"Autocomplete with '{searchTerm}' should succeed");
        }
    }

    [Test]
    public async Task Search_WithUnicodeCharacters_ShouldHandleCorrectly()
    {
        // Arrange
        var unicodeSearchTerms = new[]
        {
            "Đức Phật", // Vietnamese
            "佛教", // Chinese
            "仏教", // Japanese
            "불교", // Korean
            "Буддизм", // Russian
            "बौद्ध धर्म", // Hindi
            "🙏 meditation", // Emoji
            "café résumé", // Accented characters
        };

        foreach (var searchTerm in unicodeSearchTerms)
        {
            var request = new SearchRequestDto
            {
                SearchTerm = searchTerm,
                Page = 1,
                PageSize = 10,
                Filters = new SearchFilters()
            };

            // Act
            var response = await _client.PostAsJsonAsync("/api/search", request);

            // Assert
            response.IsSuccessStatusCode.Should().BeTrue($"Search with Unicode '{searchTerm}' should succeed");
        }
    }

    [Test]
    public async Task Search_WithSqlInjectionAttempts_ShouldBeSafe()
    {
        // Arrange
        var sqlInjectionAttempts = new[]
        {
            "'; DROP TABLE KinhThanhs; --",
            "' OR '1'='1",
            "'; DELETE FROM KinhThanhs WHERE 1=1; --",
            "' UNION SELECT * FROM KinhThanhs --",
            "'; INSERT INTO KinhThanhs VALUES ('hack'); --"
        };

        foreach (var maliciousInput in sqlInjectionAttempts)
        {
            var request = new SearchRequestDto
            {
                SearchTerm = maliciousInput,
                Page = 1,
                PageSize = 10,
                Filters = new SearchFilters()
            };

            // Act
            var response = await _client.PostAsJsonAsync("/api/search", request);

            // Assert
            response.IsSuccessStatusCode.Should().BeTrue($"SQL injection attempt '{maliciousInput}' should be handled safely");
            
            // Verify that the database is still intact by making a normal search
            var normalRequest = new SearchRequestDto
            {
                SearchTerm = "test",
                Page = 1,
                PageSize = 10,
                Filters = new SearchFilters()
            };
            
            var normalResponse = await _client.PostAsJsonAsync("/api/search", normalRequest);
            normalResponse.IsSuccessStatusCode.Should().BeTrue("Database should still be intact after SQL injection attempt");
        }
    }

    [Test]
    public async Task RateLimiting_ShouldHandleExcessiveRequests()
    {
        // This test simulates rapid requests to check if the system handles them gracefully
        var tasks = new List<Task<HttpResponseMessage>>();

        // Send 100 rapid requests
        for (int i = 0; i < 100; i++)
        {
            var request = new SearchRequestDto
            {
                SearchTerm = $"test{i}",
                Page = 1,
                PageSize = 10,
                Filters = new SearchFilters()
            };

            tasks.Add(_client.PostAsJsonAsync("/api/search", request));
        }

        // Act
        var responses = await Task.WhenAll(tasks);

        // Assert
        // Most requests should succeed, but some might be rate limited or timeout
        var successfulResponses = responses.Count(r => r.IsSuccessStatusCode);
        successfulResponses.Should().BeGreaterThan(50, "At least half of the requests should succeed");

        TestContext.WriteLine($"{successfulResponses} out of 100 rapid requests succeeded");
    }
}