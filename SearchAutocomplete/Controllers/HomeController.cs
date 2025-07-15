using Microsoft.AspNetCore.Mvc;
using SearchAutocomplete.Domain.Interfaces;

namespace SearchAutocomplete.Controllers;

public class HomeController : Controller
{
    private readonly ISectionRepository _sectionRepository;

    public HomeController(ISectionRepository sectionRepository)
    {
        _sectionRepository = sectionRepository;
    }

    public async Task<IActionResult> Index()
    {
        var sections = await _sectionRepository.GetAllAsync();
        return View(sections);
    }
}