using SearchAutocomplete.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace SearchAutocomplete.Infrastructure.Data;

public class DatabaseSeeder
{
    private readonly SearchDbContext _context;
    private readonly ILogger<DatabaseSeeder> _logger;

    public DatabaseSeeder(SearchDbContext context, ILogger<DatabaseSeeder> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task SeedAsync()
    {
        try
        {
            // Ensure database is created
            await _context.Database.EnsureCreatedAsync();

            // Check if data already exists
            if (await _context.Sections.AnyAsync())
            {
                _logger.LogInformation("Database already contains data. Skipping seeding.");
                return;
            }

            _logger.LogInformation("Starting database seeding from JSON...");

            // Load data from JSON file
            var jsonData = await LoadJsonDataAsync();
            
            // Create sections from unique book names
            var sections = CreateSectionsFromJsonData(jsonData);
            await _context.Sections.AddRangeAsync(sections);
            await _context.SaveChangesAsync();

            // Create KinhThanh records from JSON data
            var kinhThanhs = CreateKinhThanhsFromJsonData(jsonData, sections);
            await _context.KinhThanhs.AddRangeAsync(kinhThanhs);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Database seeding completed successfully. Added {SectionCount} sections and {KinhThanhCount} KinhThanh records.", 
                sections.Count, kinhThanhs.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred during database seeding");
            throw;
        }
    }

    private async Task<JsonDataRoot> LoadJsonDataAsync()
    {
        var jsonFilePath = Path.Combine("..", "data", "kinh_thanh_combined.json");
        
        if (!File.Exists(jsonFilePath))
        {
            _logger.LogWarning("JSON file not found at {FilePath}. Using fallback data.", jsonFilePath);
            return GetFallbackJsonData();
        }

        try
        {
            var jsonContent = await File.ReadAllTextAsync(jsonFilePath);
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };
            
            var jsonData = JsonSerializer.Deserialize<JsonDataRoot>(jsonContent, options);
            return jsonData ?? GetFallbackJsonData();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reading JSON file at {FilePath}. Using fallback data.", jsonFilePath);
            return GetFallbackJsonData();
        }
    }

    private JsonDataRoot GetFallbackJsonData()
    {
        return new JsonDataRoot
        {
            Books = new List<JsonBook>
            {
                new JsonBook
                {
                    Book_Name = "SÁCH SÁNG THẾ",
                    Book_Type = "C",
                    Chapters = new List<JsonChapter>
                    {
                        new JsonChapter
                        {
                            NumberString = "1",
                            Statements = new List<JsonStatement>
                            {
                                new JsonStatement
                                {
                                    Number = 1,
                                    Content = "Lúc khởi đầu, Thiên Chúa sáng tạo trời đất."
                                },
                                new JsonStatement
                                {
                                    Number = 2,
                                    Content = "Đất còn trống rỗng, chưa có hình dạng, bóng tối bao trùm vực thẳm, và thần khí Thiên Chúa bay lượn trên mặt nước."
                                }
                            }
                        }
                    }
                }
            }
        };
    }

    private List<Section> CreateSectionsFromJsonData(JsonDataRoot jsonData)
    {
        var sections = new List<Section>();
        var sectionId = 1;

        foreach (var book in jsonData.Books)
        {
            sections.Add(new Section
            {
                Id = sectionId++,
                Name = book.Book_Name,
                Description = $"{book.Book_Type} - {book.Book_Name}"
            });
        }

        return sections;
    }

    private List<KinhThanh> CreateKinhThanhsFromJsonData(JsonDataRoot jsonData, List<Section> sections)
    {
        var kinhThanhs = new List<KinhThanh>();
        var sectionLookup = sections.ToDictionary(s => s.Name, s => s.Id);

        foreach (var book in jsonData.Books)
        {
            var sectionId = sectionLookup.GetValueOrDefault(book.Book_Name, 1);

            foreach (var chapter in book.Chapters)
            {
                foreach (var statement in chapter.Statements)
                {
                    kinhThanhs.Add(new KinhThanh
                    {
                        Content = statement.Content,
                        SectionId = sectionId,
                        BookName = book.Book_Name,
                        BookType = book.Book_Type,
                        ChapterNumber = chapter.Number,
                        StatementNumber = statement.Number,
                        From = $"Chương {chapter.Number}",
                        To = $"Câu {statement.Number}",
                        Type = book.Book_Type,
                        Author = "Từ JSON Data"
                    });
                }
            }
        }

        return kinhThanhs;
    }


}