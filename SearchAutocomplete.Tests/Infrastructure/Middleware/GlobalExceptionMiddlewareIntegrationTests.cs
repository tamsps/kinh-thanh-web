using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using NUnit.Framework;
using SearchAutocomplete.Application.DTOs;
using System.Net;
using System.Text;
using System.Text.Json;

namespace SearchAutocomplete.Tests.Infrastructure.Middleware;

[TestFixture]
public class GlobalExceptionMiddlewareIntegrationTests
{
    private WebApplicationFactory<Program> _factory = null!;
    private HttpClient _client = null!;

    [SetUp]
    public void Setup()
    {
        _factory = new WebApplicationFactory<Program>();
        _client = _factory.CreateClient();
    }

    [TearDown]
    public void TearDown()
    {
        _client?.Dispose();
        _factory?.Dispose();
    }

    [Test]
    public async Task SearchEndpoint_WithInvalidRequest_ShouldReturnStructuredErrorResponse()
    {
        // Arrange
        var invalidRequest = new SearchRequestDto
        {
            SearchTerm = null!, // This should cause validation issues
            Page = -1, // Invalid page number
            PageSize = 0 // Invalid page size
        };

        var json = JsonSerializer.Serialize(invalidRequest);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        // Act
        var response = await _client.PostAsync("/api/search", content);

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.BadRequest, HttpStatusCode.InternalServerError);
        
        var responseContent = await response.Content.ReadAsStringAsync();
        responseContent.Should().NotBeEmpty();

        // Verify it's a structured JSON response
        var jsonResponse = JsonSerializer.Deserialize<JsonElement>(responseContent);
        jsonResponse.TryGetProperty("error", out var errorProperty).Should().BeTrue();
        
        if (errorProperty.ValueKind != JsonValueKind.Undefined)
        {
            errorProperty.TryGetProperty("message", out _).Should().BeTrue();
            errorProperty.TryGetProperty("correlationId", out _).Should().BeTrue();
            errorProperty.TryGetProperty("timestamp", out _).Should().BeTrue();
        }
    }

    [Test]
    public async Task AutocompleteEndpoint_WithInvalidRequest_ShouldReturnStructuredErrorResponse()
    {
        // Arrange - Request with invalid parameters that might cause exceptions
        var invalidQuery = "?searchTerm=&maxResults=-1";

        // Act
        var response = await _client.GetAsync($"/api/autocomplete{invalidQuery}");

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.BadRequest, HttpStatusCode.InternalServerError);
        
        var responseContent = await response.Content.ReadAsStringAsync();
        responseContent.Should().NotBeEmpty();

        // Verify it's a structured JSON response
        var jsonResponse = JsonSerializer.Deserialize<JsonElement>(responseContent);
        jsonResponse.TryGetProperty("error", out var errorProperty).Should().BeTrue();
        
        if (errorProperty.ValueKind != JsonValueKind.Undefined)
        {
            errorProperty.TryGetProperty("message", out _).Should().BeTrue();
            errorProperty.TryGetProperty("correlationId", out _).Should().BeTrue();
            errorProperty.TryGetProperty("timestamp", out _).Should().BeTrue();
        }
    }

    [Test]
    public async Task NonExistentEndpoint_ShouldReturn404WithoutMiddlewareIntervention()
    {
        // Arrange & Act
        var response = await _client.GetAsync("/api/nonexistent");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}