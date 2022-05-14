using System.Diagnostics;
using CheckersWeb.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CheckersWeb.Controllers;

public class HomeController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<HomeController> _logger;

    public HomeController(ApplicationDbContext context, ILogger<HomeController> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<IActionResult> Index()
    {
        var newGames = await _context.Games
            .Include(x => x.WhitePlayer)
            .Include(x => x.BlackPlayer)
            .Where(x => x.State == GameState.NotStarted)
            .OrderByDescending(x => x.Id)
            .Take(10)
            .ToListAsync();
        var currGames = await _context.Games
            .Include(x => x.WhitePlayer)
            .Include(x => x.BlackPlayer)
            .Where(x => x.State == GameState.Running)
            .OrderByDescending(x => x.Id)
            .Take(10)
            .ToListAsync();
        var pastGames = await _context.Games
            .Include(x => x.WhitePlayer)
            .Include(x => x.BlackPlayer)
            .Where(x => x.State == GameState.WhitePlayerWon || x.State == GameState.BlackPlayerWon || x.State == GameState.Draw)
            .OrderByDescending(x => x.Id)
            .Take(10)
            .ToListAsync();
        return View(new[] { newGames, currGames, pastGames });
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
