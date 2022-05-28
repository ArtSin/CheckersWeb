using System.Diagnostics;
using CheckersWeb.DAL;
using CheckersWeb.Models;
using Microsoft.AspNetCore.Mvc;

namespace CheckersWeb.Controllers;

public class HomeController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<HomeController> _logger;
    private readonly DAL.Games gamesData;

    public HomeController(ApplicationDbContext context, ILogger<HomeController> logger)
    {
        _context = context;
        _logger = logger;

        gamesData = new DAL.Games(_context);
    }

    // Главная страница
    public async Task<IActionResult> Index()
    {
        // Новые, текущие, прошедшие игры
        var newGames = await gamesData.TopNewGames();
        var currGames = await gamesData.TopCurrentGames();
        var pastGames = await gamesData.TopPastGames();
        return View(new[] { newGames, currGames, pastGames });
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
