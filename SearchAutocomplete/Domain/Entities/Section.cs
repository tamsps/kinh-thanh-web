using System.ComponentModel.DataAnnotations;

namespace SearchAutocomplete.Domain.Entities;

public class Section
{
    public int Id { get; set; }
    
    [Required]
    [MaxLength(255)]
    public string Name { get; set; } = string.Empty;
    
    [MaxLength(1000)]
    public string Description { get; set; } = string.Empty;
    
    // Navigation property
    public ICollection<KinhThanh> KinhThanhs { get; set; } = new List<KinhThanh>();
}