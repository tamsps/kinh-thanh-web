namespace SearchAutocomplete.Application.DTOs;

public class KinhThanhDto
{
    public int Id { get; set; }
    public string Content { get; set; } = string.Empty;
    public int SectionId { get; set; }
    public string SectionName { get; set; } = string.Empty;
    public string From { get; set; } = string.Empty;
    public string To { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string Author { get; set; } = string.Empty;
}