using SearchAutocomplete.Domain.Entities;

namespace SearchAutocomplete.Domain.Interfaces;

public interface ISectionRepository
{
    Task<IEnumerable<Section>> GetAllAsync();
    Task<Section?> GetByIdAsync(int id);
}