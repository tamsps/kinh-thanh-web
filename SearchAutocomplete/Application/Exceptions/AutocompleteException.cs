namespace SearchAutocomplete.Application.Exceptions;

public class AutocompleteException : Exception
{
    public string? SearchTerm { get; }
    public int? MaxResults { get; }
    public Dictionary<string, object>? AutocompleteContext { get; }

    public AutocompleteException(string message) : base(message)
    {
    }

    public AutocompleteException(string message, Exception innerException) : base(message, innerException)
    {
    }

    public AutocompleteException(string message, string searchTerm) : base(message)
    {
        SearchTerm = searchTerm;
    }

    public AutocompleteException(string message, string searchTerm, Exception innerException) : base(message, innerException)
    {
        SearchTerm = searchTerm;
    }

    public AutocompleteException(string message, string searchTerm, int maxResults) : base(message)
    {
        SearchTerm = searchTerm;
        MaxResults = maxResults;
    }

    public AutocompleteException(string message, string searchTerm, int maxResults, Dictionary<string, object> autocompleteContext) 
        : base(message)
    {
        SearchTerm = searchTerm;
        MaxResults = maxResults;
        AutocompleteContext = autocompleteContext;
    }

    public AutocompleteException(string message, string searchTerm, int maxResults, Dictionary<string, object> autocompleteContext, Exception innerException) 
        : base(message, innerException)
    {
        SearchTerm = searchTerm;
        MaxResults = maxResults;
        AutocompleteContext = autocompleteContext;
    }
}