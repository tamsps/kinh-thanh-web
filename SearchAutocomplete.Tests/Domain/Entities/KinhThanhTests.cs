using FluentAssertions;
using NUnit.Framework;
using SearchAutocomplete.Domain.Entities;
using System.ComponentModel.DataAnnotations;

namespace SearchAutocomplete.Tests.Domain.Entities;

[TestFixture]
public class KinhThanhTests
{
    [Test]
    public void KinhThanh_ShouldHaveCorrectProperties()
    {
        // Arrange & Act
        var kinhThanh = new KinhThanh
        {
            Id = 1,
            Content = "Test content",
            SectionId = 1,
            From = "Test from",
            To = "Test to",
            Type = "Test type",
            Author = "Test author"
        };

        // Assert
        kinhThanh.Id.Should().Be(1);
        kinhThanh.Content.Should().Be("Test content");
        kinhThanh.SectionId.Should().Be(1);
        kinhThanh.From.Should().Be("Test from");
        kinhThanh.To.Should().Be("Test to");
        kinhThanh.Type.Should().Be("Test type");
        kinhThanh.Author.Should().Be("Test author");
    }

    [Test]
    public void KinhThanh_WithValidData_ShouldPassValidation()
    {
        // Arrange
        var kinhThanh = new KinhThanh
        {
            Content = "Valid content",
            SectionId = 1,
            From = "Valid from",
            To = "Valid to",
            Type = "Valid type",
            Author = "Valid author"
        };

        // Act
        var validationResults = ValidateModel(kinhThanh);

        // Assert
        validationResults.Should().BeEmpty();
    }

    [Test]
    public void KinhThanh_WithEmptyContent_ShouldFailValidation()
    {
        // Arrange
        var kinhThanh = new KinhThanh
        {
            Content = "",
            SectionId = 1
        };

        // Act
        var validationResults = ValidateModel(kinhThanh);

        // Assert
        validationResults.Should().NotBeEmpty();
        validationResults.Should().Contain(r => r.MemberNames.Contains("Content"));
    }

    [Test]
    public void KinhThanh_WithTooLongContent_ShouldFailValidation()
    {
        // Arrange
        var kinhThanh = new KinhThanh
        {
            Content = new string('a', 2001), // Exceeds 2000 character limit
            SectionId = 1
        };

        // Act
        var validationResults = ValidateModel(kinhThanh);

        // Assert
        validationResults.Should().NotBeEmpty();
        validationResults.Should().Contain(r => r.MemberNames.Contains("Content"));
    }

    private static IList<ValidationResult> ValidateModel(object model)
    {
        var validationResults = new List<ValidationResult>();
        var ctx = new ValidationContext(model, null, null);
        Validator.TryValidateObject(model, ctx, validationResults, true);
        return validationResults;
    }
}