using FluentAssertions;
using NUnit.Framework;
using SearchAutocomplete.Application.DTOs;
using System.ComponentModel.DataAnnotations;

namespace SearchAutocomplete.Tests.Application.DTOs;

[TestFixture]
public class SearchRequestDtoTests
{
    [Test]
    public void SearchRequestDto_WithValidData_ShouldPassValidation()
    {
        // Arrange
        var dto = new SearchRequestDto
        {
            SearchTerm = "valid search term",
            Page = 1,
            PageSize = 10,
            Filters = new SearchFilters()
        };

        // Act
        var validationResults = ValidateModel(dto);

        // Assert
        validationResults.Should().BeEmpty();
    }

    [Test]
    public void SearchRequestDto_WithEmptySearchTerm_ShouldFailValidation()
    {
        // Arrange
        var dto = new SearchRequestDto
        {
            SearchTerm = "",
            Page = 1,
            PageSize = 10
        };

        // Act
        var validationResults = ValidateModel(dto);

        // Assert
        validationResults.Should().NotBeEmpty();
        validationResults.Should().Contain(r => r.MemberNames.Contains("SearchTerm"));
    }

    [Test]
    public void SearchRequestDto_WithInvalidPage_ShouldFailValidation()
    {
        // Arrange
        var dto = new SearchRequestDto
        {
            SearchTerm = "valid search",
            Page = 0, // Invalid page number
            PageSize = 10
        };

        // Act
        var validationResults = ValidateModel(dto);

        // Assert
        validationResults.Should().NotBeEmpty();
        validationResults.Should().Contain(r => r.MemberNames.Contains("Page"));
    }

    [Test]
    public void SearchRequestDto_WithInvalidPageSize_ShouldFailValidation()
    {
        // Arrange
        var dto = new SearchRequestDto
        {
            SearchTerm = "valid search",
            Page = 1,
            PageSize = 0 // Invalid page size
        };

        // Act
        var validationResults = ValidateModel(dto);

        // Assert
        validationResults.Should().NotBeEmpty();
        validationResults.Should().Contain(r => r.MemberNames.Contains("PageSize"));
    }

    [Test]
    public void SearchRequestDto_WithTooLargePageSize_ShouldFailValidation()
    {
        // Arrange
        var dto = new SearchRequestDto
        {
            SearchTerm = "valid search",
            Page = 1,
            PageSize = 101 // Exceeds maximum of 100
        };

        // Act
        var validationResults = ValidateModel(dto);

        // Assert
        validationResults.Should().NotBeEmpty();
        validationResults.Should().Contain(r => r.MemberNames.Contains("PageSize"));
    }

    [Test]
    public void SearchRequestDto_ShouldHaveDefaultValues()
    {
        // Arrange & Act
        var dto = new SearchRequestDto();

        // Assert
        dto.Page.Should().Be(1);
        dto.PageSize.Should().Be(10);
        dto.Filters.Should().NotBeNull();
        dto.SearchTerm.Should().Be(string.Empty);
    }

    private static IList<ValidationResult> ValidateModel(object model)
    {
        var validationResults = new List<ValidationResult>();
        var ctx = new ValidationContext(model, null, null);
        Validator.TryValidateObject(model, ctx, validationResults, true);
        return validationResults;
    }
}