using CheckersWeb.DAL.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace CheckersWeb.DAL;

public class Rating
{
    private readonly ApplicationDbContext _context;

    public Rating(ApplicationDbContext context)
    {
        _context = context;
    }

    public struct PlayerWins
    {
        public IdentityUser Player;
        public int Wins;
    }

    public struct PlayerLosses
    {
        public IdentityUser Player;
        public int Losses;
    }

    public struct PlayerDraws
    {
        public IdentityUser Player;
        public int Draws;
    }

    // Количество побед для каждого игрока
    public async Task<List<PlayerWins>> WinsByPlayer()
    {
        // Победы в качестве белого игрока
        var whiteWins = await _context.Games
            .Where(x => x.State == GameState.WhitePlayerWon)
            .Include(x => x.WhitePlayer)
            .GroupBy(x => x.WhitePlayer!.Id)
            .Select(x => new { x.Key, Wins = x.Count() })
            .ToListAsync();
        // Победы в качестве чёрного игрока
        var blackWins = await _context.Games
            .Where(x => x.State == GameState.BlackPlayerWon)
            .Include(x => x.BlackPlayer)
            .GroupBy(x => x.BlackPlayer!.Id)
            .Select(x => new { x.Key, Wins = x.Count() })
            .ToListAsync();
        // Все победы
        return whiteWins.Join(blackWins, x => x.Key, x => x.Key, (x, y) =>
            new { x.Key, Wins = x.Wins + y.Wins })
            .UnionBy(whiteWins, x => x.Key)
            .UnionBy(blackWins, x => x.Key)
            .Join(_context.Users, x => x.Key, x => x.Id, (x, y) => new PlayerWins { Player = y, Wins = x.Wins })
            .UnionBy(_context.Users.Select(x => new PlayerWins { Player = x, Wins = 0 }), x => x.Player)
            .ToList();
    }

    // Количество поражений для каждого игрока
    public async Task<List<PlayerLosses>> LossesByPlayer()
    {
        // Поражения в качестве белого игрока
        var whiteLosses = await _context.Games
            .Where(x => x.State == GameState.BlackPlayerWon)
            .Include(x => x.WhitePlayer)
            .GroupBy(x => x.WhitePlayer!.Id)
            .Select(x => new { x.Key, Losses = x.Count() })
            .ToListAsync();
        // Поражения в качестве чёрного игрока
        var blackLosses = await _context.Games
            .Where(x => x.State == GameState.WhitePlayerWon)
            .Include(x => x.BlackPlayer)
            .GroupBy(x => x.BlackPlayer!.Id)
            .Select(x => new { x.Key, Losses = x.Count() })
            .ToListAsync();
        // Все поражения
        return whiteLosses.Join(blackLosses, x => x.Key, x => x.Key, (x, y) =>
            new { x.Key, Losses = x.Losses + y.Losses })
            .UnionBy(whiteLosses, x => x.Key)
            .UnionBy(blackLosses, x => x.Key)
            .Join(_context.Users, x => x.Key, x => x.Id, (x, y) => new PlayerLosses { Player = y, Losses = x.Losses })
            .UnionBy(_context.Users.Select(x => new PlayerLosses { Player = x, Losses = 0 }), x => x.Player)
            .ToList();
    }

    // Количество ничьих для каждого игрока
    public async Task<List<PlayerDraws>> DrawsByPlayer()
    {
        // Ничьи в качестве белого игрока
        var whiteDraws = await _context.Games
            .Where(x => x.State == GameState.Draw)
            .Include(x => x.WhitePlayer)
            .GroupBy(x => x.WhitePlayer!.Id)
            .Select(x => new { x.Key, Draws = x.Count() })
            .ToListAsync();
        // Ничьи в качестве чёрного игрока
        var blackDraws = await _context.Games
            .Where(x => x.State == GameState.Draw)
            .Include(x => x.BlackPlayer)
            .GroupBy(x => x.BlackPlayer!.Id)
            .Select(x => new { x.Key, Draws = x.Count() })
            .ToListAsync();
        // Все ничьи
        return whiteDraws.Join(blackDraws, x => x.Key, x => x.Key, (x, y) =>
            new { x.Key, Draws = x.Draws + y.Draws })
            .UnionBy(whiteDraws, x => x.Key)
            .UnionBy(blackDraws, x => x.Key)
            .Join(_context.Users, x => x.Key, x => x.Id, (x, y) => new PlayerDraws { Player = y, Draws = x.Draws })
            .UnionBy(_context.Users.Select(x => new PlayerDraws { Player = x, Draws = 0 }), x => x.Player)
            .ToList();
    }
}