using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using SearchAutocomplete.Application.DTOs;
using SearchAutocomplete.Domain.Entities;
using SearchAutocomplete.Infrastructure.Data;
using System.Diagnostics;
using System.Net.Http.Json;

namespace SearchAutocomplete.Tests.Performance;

[TestFixture]
public class PerformanceTests
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
                        options.UseInMemoryDatabase("PerformanceTestDb");
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
        // Seed large dataset for performance testing
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<SearchDbContext>();
        
        await context.Database.EnsureDeletedAsync();
        await context.Database.EnsureCreatedAsync();

        await SeedLargeDataset(context);
    }

    private async Task SeedLargeDataset(SearchDbContext context)
    {
        var sections = new List<Section>();
        for (int i = 1; i <= 10; i++)
        {
            sections.Add(new Section
            {
                Id = i,
                Name = $"Section {i}",
                Description = $"Description for section {i}"
            });
        }

        var kinhThanhs = new List<KinhThanh>();
        var random = new Random(42); // Fixed seed for consistent results
        var types = new[] { "Kinh", "Luật", "Luận", "Sách" };
        var authors = new[] { "Đức Phật", "Thích Nhất Hạnh", "Thích Minh Châu", "Thích Trí Quang" };
        var contentTemplates = new[]
        {
            "This is test content about {0} and {1} for performance testing",
            "Performance test data containing {0} with {1} keywords",
            "Large dataset entry for {0} testing with {1} content",
            "Comprehensive test data for {0} search functionality with {1}",
            "Detailed content about {0} practices and {1} teachings"
        };

        // Create 1000 records for performance testing
        for (int i = 1; i <= 1000; i++)
        {
            var sectionId = random.Next(1, 11);
            var type = types[random.Next(types.Length)];
            var author = authors[random.Next(authors.Length)];
            var template = contentTemplates[random.Next(contentTemplates.Length)];
            var keyword1 = $"keyword{random.Next(1, 100)}";
            var keyword2 = $"term{random.Next(1, 50)}";

            kinhThanhs.Add(new KinhThanh
            {
                Id = i,
                Content = string.Format(template, keyword1, keyword2),
                SectionId = sectionId,
                Type = type,
                Author = author,
                From = $"Page {random.Next(1, 100)}",
                To = $"Page {random.Next(101, 200)}"
            });
        }

        context.Sections.AddRange(sections);
        context.KinhThanhs.AddRange(kinhThanhs);
        await context.SaveChangesAsync();
    }

    [Test]
    public async Task Search_WithLargeDataset_ShouldCompleteWithin2Seconds()
    {
        // Arrange
        var request = new SearchRequestDto
        {
            SearchTerm = "test",
            Page = 1,
            PageSize = 10,
            Filters = new SearchFilters()
        };

        var stopwatch = Stopwatch.StartNew();

        // Act
        var response = await _client.PostAsJsonAsync("/api/search", request);

        // Assert
        stopwatch.Stop();
        response.IsSuccessStatusCode.Should().BeTrue();
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(2000, "Search should complete within 2 seconds");

        TestContext.WriteLine($"Search completed in {stopwatch.ElapsedMilliseconds}ms");
    }

    [Test]
    public async Task Autocomplete_ShouldCompleteWithin200Milliseconds()
    {
        // Arrange
        var stopwatch = Stopwatch.StartNew();

        // Act
        var response = await _client.GetAsync("/api/autocomplete/suggestions?searchTerm=te");

        // Assert
        stopwatch.Stop();
        response.IsSuccessStatusCode.Should().BeTrue();
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(200, "Autocomplete should complete within 200ms");

        TestContext.WriteLine($"Autocomplete completed in {stopwatch.ElapsedMilliseconds}ms");
    }

    [Test]
    public async Task Search_WithComplexFilters_ShouldMaintainPerformance()
    {
        // Arrange
        var request = new SearchRequestDto
        {
            SearchTerm = "test",
            Page = 1,
            PageSize = 10,
            Filters = new SearchFilters
            {
                Types = new List<string> { "Kinh", "Luật" },
                Authors = new List<string> { "Đức Phật", "Thích Nhất Hạnh" },
                SectionIds = new List<int> { 1, 2, 3 }
            }
        };

        var stopwatch = Stopwatch.StartNew();

        // Act
        var response = await _client.PostAsJsonAsync("/api/search", request);

        // Assert
        stopwatch.Stop();
        response.IsSuccessStatusCode.Should().BeTrue();
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(2000, "Filtered search should complete within 2 seconds");

        TestContext.WriteLine($"Filtered search completed in {stopwatch.ElapsedMilliseconds}ms");
    }

    [Test]
    public async Task Search_WithPagination_ShouldMaintainConsistentPerformance()
    {
        var times = new List<long>();

        // Test multiple pages
        for (int page = 1; page <= 5; page++)
        {
            var request = new SearchRequestDto
            {
                SearchTerm = "test",
                Page = page,
                PageSize = 20,
                Filters = new SearchFilters()
            };

            var stopwatch = Stopwatch.StartNew();
            var response = await _client.PostAsJsonAsync("/api/search", request);
            stopwatch.Stop();

            response.IsSuccessStatusCode.Should().BeTrue();
            times.Add(stopwatch.ElapsedMilliseconds);

            TestContext.WriteLine($"Page {page} completed in {stopwatch.ElapsedMilliseconds}ms");
        }

        // All pages should complete within reasonable time
        times.Should().OnlyContain(time => time < 2000);
        
        // Performance should be consistent (no page should be more than 3x slower than the fastest)
        var minTime = times.Min();
        var maxTime = times.Max();
        (maxTime / (double)minTime).Should().BeLessThan(3.0, "Pagination performance should be consistent");
    }

    [Test]
    public async Task ConcurrentSearches_ShouldHandleMultipleRequests()
    {
        // Arrange
        var request = new SearchRequestDto
        {
            SearchTerm = "performance",
            Page = 1,
            PageSize = 10,
            Filters = new SearchFilters()
        };

        var tasks = new List<Task<HttpResponseMessage>>();
        var stopwatch = Stopwatch.StartNew();

        // Act - Send 10 concurrent requests
        for (int i = 0; i < 10; i++)
        {
            tasks.Add(_client.PostAsJsonAsync("/api/search", request));
        }

        var responses = await Task.WhenAll(tasks);

        // Assert
        stopwatch.Stop();
        responses.Should().OnlyContain(r => r.IsSuccessStatusCode);
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(5000, "10 concurrent searches should complete within 5 seconds");

        TestContext.WriteLine($"10 concurrent searches completed in {stopwatch.ElapsedMilliseconds}ms");
    }

    [Test]
    public async Task ConcurrentAutocomplete_ShouldHandleMultipleRequests()
    {
        // Arrange
        var tasks = new List<Task<HttpResponseMessage>>();
        var stopwatch = Stopwatch.StartNew();

        // Act - Send 20 concurrent autocomplete requests
        for (int i = 0; i < 20; i++)
        {
            tasks.Add(_client.GetAsync($"/api/autocomplete/suggestions?searchTerm=te{i % 10}"));
        }

        var responses = await Task.WhenAll(tasks);

        // Assert
        stopwatch.Stop();
        responses.Should().OnlyContain(r => r.IsSuccessStatusCode);
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(3000, "20 concurrent autocomplete requests should complete within 3 seconds");

        TestContext.WriteLine($"20 concurrent autocomplete requests completed in {stopwatch.ElapsedMilliseconds}ms");
    }

    [Test]
    public async Task Search_WithVeryLongSearchTerm_ShouldHandleGracefully()
    {
        // Arrange
        var longSearchTerm = new string('a', 1000); // 1000 character search term
        var request = new SearchRequestDto
        {
            SearchTerm = longSearchTerm,
            Page = 1,
            PageSize = 10,
            Filters = new SearchFilters()
        };

        var stopwatch = Stopwatch.StartNew();

        // Act
        var response = await _client.PostAsJsonAsync("/api/search", request);

        // Assert
        stopwatch.Stop();
        response.IsSuccessStatusCode.Should().BeTrue();
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(3000, "Long search term should be handled within 3 seconds");

        TestContext.WriteLine($"Long search term handled in {stopwatch.ElapsedMilliseconds}ms");
    }

    [Test]
    public async Task MemoryUsage_ShouldNotGrowExcessively()
    {
        // Arrange
        var initialMemory = GC.GetTotalMemory(true);

        // Act - Perform multiple searches to test memory usage
        for (int i = 0; i < 50; i++)
        {
            var request = new SearchRequestDto
            {
                SearchTerm = $"test{i}",
                Page = 1,
                PageSize = 10,
                Filters = new SearchFilters()
            };

            var response = await _client.PostAsJsonAsync("/api/search", request);
            response.IsSuccessStatusCode.Should().BeTrue();
        }

        // Force garbage collection
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        var finalMemory = GC.GetTotalMemory(false);
        var memoryIncrease = finalMemory - initialMemory;

        // Assert
        // Memory increase should be reasonable (less than 50MB for 50 searches)
        memoryIncrease.Should().BeLessThan(50 * 1024 * 1024, "Memory usage should not grow excessively");

        TestContext.WriteLine($"Memory increased by {memoryIncrease / 1024 / 1024}MB after 50 searches");
    }
}