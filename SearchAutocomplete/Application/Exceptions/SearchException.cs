namespace SearchAutocomplete.Application.Exceptions;

public class SearchException : Exception
{
    public string? SearchTerm { get; }
    public Dictionary<string, object>? SearchContext { get; }

    public SearchException(string message) : base(message)
    {
    }

    public SearchException(string message, Exception innerException) : base(message, innerException)
    {
    }

    public SearchException(string message, string searchTerm) : base(message)
    {
        SearchTerm = searchTerm;
    }

    public SearchException(string message, string searchTerm, Exception innerException) : base(message, innerException)
    {
        SearchTerm = searchTerm;
    }

    public SearchException(string message, string searchTerm, Dictionary<string, object> searchContext) : base(message)
    {
        SearchTerm = searchTerm;
        SearchContext = searchContext;
    }

    public SearchException(string message, string searchTerm, Dictionary<string, object> searchContext, Exception innerException) 
        : base(message, innerException)
    {
        SearchTerm = searchTerm;
        SearchContext = searchContext;
    }
}