using System.ComponentModel.DataAnnotations;

namespace SearchAutocomplete.Domain.Entities;

public class KinhThanh
{
    public int Id { get; set; }
    
    [Required]
    [MaxLength(2000)]
    public string Content { get; set; } = string.Empty;
    
    public int SectionId { get; set; }
    
    [MaxLength(255)]
    public string From { get; set; } = string.Empty;
    
    [MaxLength(255)]
    public string To { get; set; } = string.Empty;
    
    [MaxLength(100)]
    public string Type { get; set; } = string.Empty;
    
    [MaxLength(255)]
    public string Author { get; set; } = string.Empty;
    
    // Navigation property
    public Section Section { get; set; } = null!;
}