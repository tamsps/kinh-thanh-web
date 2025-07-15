using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using SearchAutocomplete.Application.Exceptions;
using SearchAutocomplete.Infrastructure.Middleware;
using System.Text.Json;

namespace SearchAutocomplete.Tests.Infrastructure.Middleware;

[TestFixture]
public class GlobalExceptionMiddlewareTests
{
    private Mock<ILogger<GlobalExceptionMiddleware>> _mockLogger = null!;
    private GlobalExceptionMiddleware _middleware = null!;
    private DefaultHttpContext _httpContext = null!;

    [SetUp]
    public void Setup()
    {
        _mockLogger = new Mock<ILogger<GlobalExceptionMiddleware>>();
        _httpContext = new DefaultHttpContext();
        _httpContext.Response.Body = new MemoryStream();
    }

    [Test]
    public async Task InvokeAsync_WhenNoException_ShouldCallNext()
    {
        // Arrange
        var nextCalled = false;
        RequestDelegate next = (HttpContext hc) =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        };

        _middleware = new GlobalExceptionMiddleware(next, _mockLogger.Object);

        // Act
        await _middleware.InvokeAsync(_httpContext);

        // Assert
        nextCalled.Should().BeTrue();
    }

    [Test]
    public async Task InvokeAsync_WhenSearchException_ShouldReturnBadRequest()
    {
        // Arrange
        var searchException = new SearchException("Search failed", "test query");
        RequestDelegate next = (HttpContext hc) => throw searchException;

        _middleware = new GlobalExceptionMiddleware(next, _mockLogger.Object);

        // Act
        await _middleware.InvokeAsync(_httpContext);

        // Assert
        _httpContext.Response.StatusCode.Should().Be(400);
        _httpContext.Response.ContentType.Should().Be("application/json");

        _httpContext.Response.Body.Seek(0, SeekOrigin.Begin);
        var responseBody = await new StreamReader(_httpContext.Response.Body).ReadToEndAsync();
        var response = JsonSerializer.Deserialize<JsonElement>(responseBody);

        response.GetProperty("error").GetProperty("message").GetString()!
            .Should().Contain("An error occurred while searching");
        response.GetProperty("error").TryGetProperty("correlationId", out _).Should().BeTrue();
        response.GetProperty("error").TryGetProperty("timestamp", out _).Should().BeTrue();
    }

    [Test]
    public async Task InvokeAsync_WhenAutocompleteException_ShouldReturnBadRequest()
    {
        // Arrange
        var autocompleteException = new AutocompleteException("Autocomplete failed", "test", 10);
        RequestDelegate next = (HttpContext hc) => throw autocompleteException;

        _middleware = new GlobalExceptionMiddleware(next, _mockLogger.Object);

        // Act
        await _middleware.InvokeAsync(_httpContext);

        // Assert
        _httpContext.Response.StatusCode.Should().Be(400);
        _httpContext.Response.ContentType.Should().Be("application/json");

        _httpContext.Response.Body.Seek(0, SeekOrigin.Begin);
        var responseBody = await new StreamReader(_httpContext.Response.Body).ReadToEndAsync();
        var response = JsonSerializer.Deserialize<JsonElement>(responseBody);

        response.GetProperty("error").GetProperty("message").GetString()!
            .Should().Contain("An error occurred while getting suggestions");
    }

    [Test]
    public async Task InvokeAsync_WhenArgumentNullException_ShouldReturnBadRequest()
    {
        // Arrange
        var argumentException = new ArgumentNullException("parameter");
        RequestDelegate next = (HttpContext hc) => throw argumentException;

        _middleware = new GlobalExceptionMiddleware(next, _mockLogger.Object);

        // Act
        await _middleware.InvokeAsync(_httpContext);

        // Assert
        _httpContext.Response.StatusCode.Should().Be(400);

        _httpContext.Response.Body.Seek(0, SeekOrigin.Begin);
        var responseBody = await new StreamReader(_httpContext.Response.Body).ReadToEndAsync();
        var response = JsonSerializer.Deserialize<JsonElement>(responseBody);

        response.GetProperty("error").GetProperty("message").GetString()!
            .Should().Contain("Required parameters are missing");
        
        var hasDetails = response.GetProperty("error").TryGetProperty("details", out var details) && 
                        details.ValueKind != JsonValueKind.Null;
        hasDetails.Should().BeFalse();
    }

    [Test]
    public async Task InvokeAsync_WhenUnknownException_ShouldReturnInternalServerError()
    {
        // Arrange
        var unknownException = new InvalidOperationException("Something went wrong");
        RequestDelegate next = (HttpContext hc) => throw unknownException;

        _middleware = new GlobalExceptionMiddleware(next, _mockLogger.Object);

        // Act
        await _middleware.InvokeAsync(_httpContext);

        // Assert
        _httpContext.Response.StatusCode.Should().Be(500);

        _httpContext.Response.Body.Seek(0, SeekOrigin.Begin);
        var responseBody = await new StreamReader(_httpContext.Response.Body).ReadToEndAsync();
        var response = JsonSerializer.Deserialize<JsonElement>(responseBody);

        response.GetProperty("error").GetProperty("message").GetString()!
            .Should().Contain("An unexpected error occurred");
    }

    [Test]
    public async Task InvokeAsync_WhenExceptionOccurs_ShouldLogError()
    {
        // Arrange
        var testException = new Exception("Test exception");
        RequestDelegate next = (HttpContext hc) => throw testException;

        _middleware = new GlobalExceptionMiddleware(next, _mockLogger.Object);

        // Act
        await _middleware.InvokeAsync(_httpContext);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Unhandled exception occurred")),
                testException,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }
}