using SearchAutocomplete.Domain.Entities;
using Microsoft.EntityFrameworkCore;

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

            _logger.LogInformation("Starting database seeding...");

            // Seed Sections
            var sections = GetSampleSections();
            await _context.Sections.AddRangeAsync(sections);
            await _context.SaveChangesAsync();

            // Seed KinhThanh records
            var kinhThanhs = GetSampleKinhThanhs(sections);
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

    private List<Section> GetSampleSections()
    {
        return new List<Section>
        {
            new Section
            {
                Id = 1,
                Name = "Kinh Pháp Cú",
                Description = "Tập hợp những lời dạy ngắn gọn và sâu sắc của Đức Phật"
            },
            new Section
            {
                Id = 2,
                Name = "Kinh Kim Cương",
                Description = "Một trong những kinh điển quan trọng nhất của Phật giáo Đại Thừa"
            },
            new Section
            {
                Id = 3,
                Name = "Kinh Tâm",
                Description = "Kinh ngắn nhưng chứa đựng tinh hoa của triết lý Bát Nhã"
            },
            new Section
            {
                Id = 4,
                Name = "Luật Tỳ Kheo",
                Description = "Các quy tắc và giới luật dành cho tăng sĩ"
            },
            new Section
            {
                Id = 5,
                Name = "Luận Abhidhamma",
                Description = "Phân tích chi tiết về tâm lý học và triết học Phật giáo"
            }
        };
    }

    private List<KinhThanh> GetSampleKinhThanhs(List<Section> sections)
    {
        return new List<KinhThanh>
        {
            // Kinh Pháp Cú
            new KinhThanh
            {
                Content = "Tâm là chủ tạo ra tất cả, tâm đi trước, tâm làm chủ. Nếu với tâm thanh tịnh mà nói năng, hành động, thì an lạc sẽ theo sau, như bóng theo hình.",
                SectionId = 1,
                From = "Phẩm Song Yếu",
                To = "Câu 2",
                Type = "Kinh",
                Author = "Đức Phật"
            },
            new KinhThanh
            {
                Content = "Hận thù không thể diệt trừ hận thù. Chỉ có từ ái mới diệt trừ được hận thù. Đây là chân lý bất biến.",
                SectionId = 1,
                From = "Phẩm Song Yếu",
                To = "Câu 5",
                Type = "Kinh",
                Author = "Đức Phật"
            },
            new KinhThanh
            {
                Content = "Người nào không tham lam, không sân hận, không si mê, thì người ấy được gọi là bậc A-la-hán.",
                SectionId = 1,
                From = "Phẩm A-la-hán",
                To = "Câu 94",
                Type = "Kinh",
                Author = "Đức Phật"
            },
            new KinhThanh
            {
                Content = "Như người thợ làm mũi tên làm thẳng cây tên, người trí cũng vậy, điều phục tâm mình.",
                SectionId = 1,
                From = "Phẩm Tâm",
                To = "Câu 33",
                Type = "Kinh",
                Author = "Đức Phật"
            },
            new KinhThanh
            {
                Content = "Tất cả chúng sinh đều sợ chết, đều yêu sống. Hãy lấy mình làm thước đo, không nên giết hại hay bảo người khác giết hại.",
                SectionId = 1,
                From = "Phẩm Bạo Lực",
                To = "Câu 129",
                Type = "Kinh",
                Author = "Đức Phật"
            },

            // Kinh Kim Cương
            new KinhThanh
            {
                Content = "Tất cả pháp hữu vi như mộng huyễn, như bọt nước, như bóng, như sương mai, như chớp. Nên quán như vậy.",
                SectionId = 2,
                From = "Phẩm 32",
                To = "Kệ cuối",
                Type = "Kinh",
                Author = "Đức Phật"
            },
            new KinhThanh
            {
                Content = "Nếu có người nói Như Lai có đến, có đi, có ngồi, có nằm, người ấy không hiểu nghĩa lời ta nói.",
                SectionId = 2,
                From = "Phẩm 29",
                To = "Bất lai bất khứ",
                Type = "Kinh",
                Author = "Đức Phật"
            },
            new KinhThanh
            {
                Content = "Phàm có tướng đều là hư vọng. Nếu thấy các tướng không phải tướng, tức thấy Như Lai.",
                SectionId = 2,
                From = "Phẩm 5",
                To = "Như lý thật kiến",
                Type = "Kinh",
                Author = "Đức Phật"
            },
            new KinhThanh
            {
                Content = "Ứng sinh tâm vô sở trụ - Nên sinh khởi tâm không nương tựa vào đâu cả.",
                SectionId = 2,
                From = "Phẩm 10",
                To = "Trang nghiêm tịnh độ",
                Type = "Kinh",
                Author = "Đức Phật"
            },

            // Kinh Tâm
            new KinhThanh
            {
                Content = "Quán Tự Tại Bồ Tát hành thâm Bát Nhã Ba La Mật Đa thời, chiếu kiến ngũ uẩn giai không, độ nhất thiết khổ ách.",
                SectionId = 3,
                From = "Mở đầu",
                To = "Câu đầu",
                Type = "Kinh",
                Author = "Đức Phật"
            },
            new KinhThanh
            {
                Content = "Sắc bất dị không, không bất dị sắc, sắc tức thị không, không tức thị sắc.",
                SectionId = 3,
                From = "Ngũ uẩn",
                To = "Sắc không",
                Type = "Kinh",
                Author = "Đức Phật"
            },
            new KinhThanh
            {
                Content = "Bát Nhã Ba La Mật Đa, thị đại thần chú, thị đại minh chú, thị vô thượng chú, thị vô đẳng đẳng chú.",
                SectionId = 3,
                From = "Thần chú",
                To = "Bát Nhã chú",
                Type = "Kinh",
                Author = "Đức Phật"
            },

            // Luật Tỳ Kheo
            new KinhThanh
            {
                Content = "Tỳ kheo không được cố ý đoạt mạng sinh vật. Ai vi phạm phạm Ba-la-di.",
                SectionId = 4,
                From = "Ba-la-di",
                To = "Điều 1",
                Type = "Luật",
                Author = "Đức Phật"
            },
            new KinhThanh
            {
                Content = "Tỳ kheo phải khất thực đúng thời, không được ăn phi thời. Phi thời là từ trưa đến sáng hôm sau.",
                SectionId = 4,
                From = "Ni-tát-kỳ",
                To = "Điều về ăn uống",
                Type = "Luật",
                Author = "Đức Phật"
            },
            new KinhThanh
            {
                Content = "Tỳ kheo nên sống đơn giản, tri túc, ít dục, hài lòng với những gì có được.",
                SectionId = 4,
                From = "Giáo giới",
                To = "Về lối sống",
                Type = "Luật",
                Author = "Đức Phật"
            },

            // Luận Abhidhamma
            new KinhThanh
            {
                Content = "Tâm có 89 loại, chia thành thiện tâm, bất thiện tâm, và vô ký tâm.",
                SectionId = 5,
                From = "Tâm luận",
                To = "Phân loại tâm",
                Type = "Luận",
                Author = "Thích Xá Lợi Phất"
            },
            new KinhThanh
            {
                Content = "Tâm sở có 52 loại, bao gồm biến hành, biệt cảnh, thiện, phiền não, bất định.",
                SectionId = 5,
                From = "Tâm sở luận",
                To = "52 tâm sở",
                Type = "Luận",
                Author = "Thích Xá Lợi Phất"
            },
            new KinhThanh
            {
                Content = "Nghiệp có 4 loại: thiện nghiệp, bất thiện nghiệp, vô ký nghiệp, và hỗn hợp nghiệp.",
                SectionId = 5,
                From = "Nghiệp luận",
                To = "Phân loại nghiệp",
                Type = "Luận",
                Author = "Thích Xá Lợi Phất"
            },

            // Additional diverse content for better search testing
            new KinhThanh
            {
                Content = "Thiền định là phương pháp tu tập để đạt được tâm an tịnh và trí tuệ sáng suốt.",
                SectionId = 1,
                From = "Phẩm Thiền",
                To = "Về thiền định",
                Type = "Kinh",
                Author = "Thích Nhất Hạnh"
            },
            new KinhThanh
            {
                Content = "Từ bi là tình thương vô điều kiện dành cho tất cả chúng sinh, không phân biệt kẻ thù hay bạn bè.",
                SectionId = 1,
                From = "Phẩm Từ Bi",
                To = "Về lòng từ",
                Type = "Sách",
                Author = "Thích Minh Châu"
            },
            new KinhThanh
            {
                Content = "Giới luật không phải để trói buộc mà để giải thoát, giúp con người sống hạnh phúc và an lạc.",
                SectionId = 4,
                From = "Tổng luận",
                To = "Ý nghĩa giới luật",
                Type = "Luận",
                Author = "Thích Trí Quang"
            },
            new KinhThanh
            {
                Content = "Chánh niệm là sự tỉnh thức trong từng khoảnh khắc, biết rõ những gì đang xảy ra trong thân và tâm.",
                SectionId = 3,
                From = "Tứ niệm xứ",
                To = "Chánh niệm",
                Type = "Kinh",
                Author = "Đức Phật"
            }
        };
    }
}