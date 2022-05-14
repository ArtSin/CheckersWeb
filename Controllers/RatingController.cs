using System.Diagnostics;
using CheckersWeb.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CheckersWeb.Controllers;

public class RatingController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<HomeController> _logger;

    public RatingController(ApplicationDbContext context, ILogger<HomeController> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<IActionResult> Index()
    {
        //var res = await _context.Users.Join(_context.Games, u => u.UserName, g => g.WhitePlayer, (u, g) => ) /*Select((player) =>
        /*{
            var wins = _context.Games
                .Include(x => x.WhitePlayer)
                .Include(x => x.BlackPlayer)
                .Where(x => (x.WhitePlayer == player && x.State == GameState.WhitePlayerWon) || (x.BlackPlayer == player && x.State == GameState.BlackPlayerWon))
                .Count();
            return new Rating { Player = player, Wins = wins };
        }).ToListAsync();*/
        var whiteWins = await _context.Games
            .Where(x => x.State == GameState.WhitePlayerWon)
            .Include(x => x.WhitePlayer)
            .GroupBy(x => x.WhitePlayer.Id)
            .Select(x => new { x.Key, Wins = x.Count() })
            .ToListAsync();
        var blackWins = await _context.Games
            .Where(x => x.State == GameState.BlackPlayerWon)
            .Include(x => x.BlackPlayer)
            .GroupBy(x => x.BlackPlayer.Id)
            .Select(x => new { x.Key, Wins = x.Count() })
            .ToListAsync();
        var wins = whiteWins.Join(blackWins, x => x.Key, x => x.Key, (x, y) =>
            new { x.Key, Wins = x.Wins + y.Wins })
            .UnionBy(whiteWins, x => x.Key)
            .UnionBy(blackWins, x => x.Key)
            .Join(_context.Users, x => x.Key, x => x.Id, (x, y) => new { Player = y, x.Wins })
            .UnionBy(_context.Users.Select(x => new { Player = x, Wins = 0 }), x => x.Player)
            .ToList();

        var whiteLosses = await _context.Games
            .Where(x => x.State == GameState.BlackPlayerWon)
            .Include(x => x.WhitePlayer)
            .GroupBy(x => x.WhitePlayer.Id)
            .Select(x => new { x.Key, Losses = x.Count() })
            .ToListAsync();
        var blackLosses = await _context.Games
            .Where(x => x.State == GameState.WhitePlayerWon)
            .Include(x => x.BlackPlayer)
            .GroupBy(x => x.BlackPlayer.Id)
            .Select(x => new { x.Key, Losses = x.Count() })
            .ToListAsync();
        var losses = whiteLosses.Join(blackLosses, x => x.Key, x => x.Key, (x, y) =>
            new { x.Key, Losses = x.Losses + y.Losses })
            .UnionBy(whiteLosses, x => x.Key)
            .UnionBy(blackLosses, x => x.Key)
            .Join(_context.Users, x => x.Key, x => x.Id, (x, y) => new { Player = y, x.Losses })
            .UnionBy(_context.Users.Select(x => new { Player = x, Losses = 0 }), x => x.Player)
            .ToList();

        var whiteDraws = await _context.Games
            .Where(x => x.State == GameState.Draw)
            .Include(x => x.WhitePlayer)
            .GroupBy(x => x.WhitePlayer.Id)
            .Select(x => new { x.Key, Draws = x.Count() })
            .ToListAsync();
        var blackDraws = await _context.Games
            .Where(x => x.State == GameState.Draw)
            .Include(x => x.BlackPlayer)
            .GroupBy(x => x.BlackPlayer.Id)
            .Select(x => new { x.Key, Draws = x.Count() })
            .ToListAsync();
        var draws = whiteDraws.Join(blackDraws, x => x.Key, x => x.Key, (x, y) =>
            new { x.Key, Draws = x.Draws + y.Draws })
            .UnionBy(whiteDraws, x => x.Key)
            .UnionBy(blackDraws, x => x.Key)
            .Join(_context.Users, x => x.Key, x => x.Id, (x, y) => new { Player = y, x.Draws })
            .UnionBy(_context.Users.Select(x => new { Player = x, Draws = 0 }), x => x.Player)
            .ToList();

        var res = wins.Join(
            losses.Join(draws, x => x.Player, y => y.Player, (x, y) => new { Player = x.Player, x.Losses, y.Draws }),
            x => x.Player, y => y.Player, (x, y) =>
                new Rating { Player = x.Player, Wins = x.Wins, Losses = y.Losses, Draws = y.Draws, Score = x.Wins - y.Losses })
            .OrderByDescending(x => x.Score)
            .ToList();
        return View(res);
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
