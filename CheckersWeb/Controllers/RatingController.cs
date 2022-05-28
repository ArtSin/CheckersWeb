using System.Diagnostics;
using CheckersWeb.DAL;
using CheckersWeb.Models;
using Microsoft.AspNetCore.Mvc;

namespace CheckersWeb.Controllers;

public class RatingController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<HomeController> _logger;
    private readonly DAL.Rating ratingData;
    private readonly BLL.Rating ratingLogic;

    public RatingController(ApplicationDbContext context, ILogger<HomeController> logger)
    {
        _context = context;
        _logger = logger;

        ratingData = new DAL.Rating(_context);
        ratingLogic = new BLL.Rating();
    }

    // Страница рейтинга игроков
    public async Task<IActionResult> Index()
    {
        // Количество побед для каждого игрока
        var wins = await ratingData.WinsByPlayer();
        // Количество поражений для каждого игрока
        var losses = await ratingData.LossesByPlayer();
        // Количество ничьих для каждого игрока
        var draws = await ratingData.DrawsByPlayer();
        // Составление рейтинга из побед, поражений и ничьих
        var res = ratingLogic.GetRating(wins, losses, draws);
        return View(res);
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
