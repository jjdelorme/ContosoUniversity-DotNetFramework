using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using ContosoUniversityCore.Models; // For ErrorViewModel
using ContosoUniversityCore.Data;
using ContosoUniversityCore.ViewModels;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;

namespace ContosoUniversityCore.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly SchoolContext _context;

    public HomeController(ILogger<HomeController> logger, SchoolContext context)
    {
        _logger = logger;
        _context = context;
    }

    public IActionResult Index()
    {
        return View();
    }

    public async Task<IActionResult> About()
    {
        var data = await _context.Students
            .GroupBy(s => s.EnrollmentDate)
            .Select(dateGroup => new EnrollmentDateGroup()
            {
                EnrollmentDate = dateGroup.Key,
                StudentCount = dateGroup.Count()
            })
            .OrderBy(s => s.EnrollmentDate) // Added ordering for consistent display
            .ToListAsync();

        return View(data);
    }

    public IActionResult Contact()
    {
        ViewData["Message"] = "Your contact page."; // Changed from ViewBag to ViewData for common practice
        return View();
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
