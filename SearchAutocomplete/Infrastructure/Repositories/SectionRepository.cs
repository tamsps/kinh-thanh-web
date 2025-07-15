using Microsoft.EntityFrameworkCore;
using SearchAutocomplete.Domain.Entities;
using SearchAutocomplete.Domain.Interfaces;
using SearchAutocomplete.Infrastructure.Data;
using SearchAutocomplete.Infrastructure.Resilience;

namespace SearchAutocomplete.Infrastructure.Repositories;

public class SectionRepository : ISectionRepository
{
    private readonly SearchDbContext _context;
    private readonly ResilienceService _resilienceService;

    public SectionRepository(SearchDbContext context, ResilienceService resilienceService)
    {
        _context = context;
        _resilienceService = resilienceService;
    }

    public async Task<IEnumerable<Section>> GetAllAsync()
    {
        return await _resilienceService.ExecuteDatabaseOperationAsync(async () =>
        {
            return await _context.Sections
                .OrderBy(s => s.Name)
                .ToListAsync();
        }, "GetAllSectionsAsync");
    }

    public async Task<Section?> GetByIdAsync(int id)
    {
        return await _resilienceService.ExecuteDatabaseOperationAsync(async () =>
        {
            return await _context.Sections
                .Include(s => s.KinhThanhs)
                .FirstOrDefaultAsync(s => s.Id == id);
        }, $"GetSectionByIdAsync(id: {id})");
    }
}