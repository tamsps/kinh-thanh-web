using FluentAssertions;
using NUnit.Framework;
using SearchAutocomplete.Domain.Entities;
using System.ComponentModel.DataAnnotations;

namespace SearchAutocomplete.Tests.Domain.Entities;

[TestFixture]
public class SectionTests
{
    [Test]
    public void Section_ShouldHaveCorrectProperties()
    {
        // Arrange & Act
        var section = new Section
        {
            Id = 1,
            Name = "Test Section",
            Description = "Test Description"
        };

        // Assert
        section.Id.Should().Be(1);
        section.Name.Should().Be("Test Section");
        section.Description.Should().Be("Test Description");
        section.KinhThanhs.Should().NotBeNull();
        section.KinhThanhs.Should().BeEmpty();
    }

    [Test]
    public void Section_WithValidData_ShouldPassValidation()
    {
        // Arrange
        var section = new Section
        {
            Name = "Valid Section Name",
            Description = "Valid description"
        };

        // Act
        var validationResults = ValidateModel(section);

        // Assert
        validationResults.Should().BeEmpty();
    }

    [Test]
    public void Section_WithEmptyName_ShouldFailValidation()
    {
        // Arrange
        var section = new Section
        {
            Name = "",
            Description = "Valid description"
        };

        // Act
        var validationResults = ValidateModel(section);

        // Assert
        validationResults.Should().NotBeEmpty();
        validationResults.Should().Contain(r => r.MemberNames.Contains("Name"));
    }

    [Test]
    public void Section_WithTooLongName_ShouldFailValidation()
    {
        // Arrange
        var section = new Section
        {
            Name = new string('a', 256), // Exceeds 255 character limit
            Description = "Valid description"
        };

        // Act
        var validationResults = ValidateModel(section);

        // Assert
        validationResults.Should().NotBeEmpty();
        validationResults.Should().Contain(r => r.MemberNames.Contains("Name"));
    }

    [Test]
    public void Section_CanAddKinhThanhs()
    {
        // Arrange
        var section = new Section { Name = "Test Section" };
        var kinhThanh = new KinhThanh { Content = "Test Content", SectionId = section.Id };

        // Act
        section.KinhThanhs.Add(kinhThanh);

        // Assert
        section.KinhThanhs.Should().HaveCount(1);
        section.KinhThanhs.Should().Contain(kinhThanh);
    }

    private static IList<ValidationResult> ValidateModel(object model)
    {
        var validationResults = new List<ValidationResult>();
        var ctx = new ValidationContext(model, null, null);
        Validator.TryValidateObject(model, ctx, validationResults, true);
        return validationResults;
    }
}