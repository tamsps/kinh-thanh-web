using System.Text.Json.Serialization;

namespace SearchAutocomplete.Infrastructure.Data;

public class JsonDataRoot
{
    [JsonPropertyName("Sách")]
    public List<JsonBook> Books { get; set; } = new();
}

public class JsonBook
{
    [JsonPropertyName("book_name")]
    public string Book_Name { get; set; } = string.Empty;
    
    [JsonPropertyName("book_type")]
    public string Book_Type { get; set; } = string.Empty;
    
    [JsonPropertyName("chapters")]
    public List<JsonChapter> Chapters { get; set; } = new();
}

public class JsonChapter
{
    [JsonPropertyName("number")]
    public string NumberString { get; set; } = string.Empty;
    
    [JsonIgnore]
    public int Number => int.TryParse(NumberString, out var num) ? num : 1;
    
    [JsonPropertyName("statements")]
    public List<JsonStatement> Statements { get; set; } = new();
}

public class JsonStatement
{
    [JsonPropertyName("number")]
    public int Number { get; set; }
    
    [JsonPropertyName("content")]
    public string Content { get; set; } = string.Empty;
}