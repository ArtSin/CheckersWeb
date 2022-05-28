using CheckersWeb.DAL.Models;
using Microsoft.EntityFrameworkCore;

namespace CheckersWeb.DAL;

public class Games
{
    private readonly ApplicationDbContext _context;

    public Games(ApplicationDbContext context)
    {
        _context = context;
    }

    // Получение всех игр (по убыванию номера)
    public async Task<List<Game>> All()
    {
        return await _context.Games
            .Include(x => x.WhitePlayer)
            .Include(x => x.BlackPlayer)
            .OrderByDescending(x => x.Id)
            .ToListAsync();
    }

    // Все игры, в которых белый игрок равен заданному
    public async Task<List<Game>> SearchByWhitePlayer(string player)
    {
        return await _context.Games
            .Include(x => x.WhitePlayer)
            .Include(x => x.BlackPlayer)
            .Where(x => x.WhitePlayer != null && x.WhitePlayer.UserName == player)
            .OrderByDescending(x => x.Id)
            .ToListAsync();
    }

    // Все игры, в которых чёрный игрок равен заданному
    public async Task<List<Game>> SearchByBlackPlayer(string player)
    {
        return await _context.Games
            .Include(x => x.WhitePlayer)
            .Include(x => x.BlackPlayer)
            .Where(x => x.BlackPlayer != null && x.BlackPlayer.UserName == player)
            .OrderByDescending(x => x.Id)
            .ToListAsync();
    }

    // Добавление игры
    public async Task Add(Game game)
    {
        _context.Add(game);
        await _context.SaveChangesAsync();
    }

    // Обновление игры
    public async Task Update(Game game)
    {
        _context.Update(game);
        await _context.SaveChangesAsync();
    }

    // Поиск игры по номеру
    public async Task<Game?> Find(int? id)
    {
        if (id == null)
            return null;
        return await _context.Games.FindAsync(id);
    }

    // Загрузка ходов игры
    public async Task LoadGameMoves(Game game)
    {
        await _context.Entry(game).Collection(x => x.Moves).LoadAsync();
        if (game.Moves == null)
            return;
        foreach (var currMove in game.Moves)
        {
            await _context.Entry(currMove).Collection(x => x.Cells).LoadAsync();
            await _context.Entry(currMove).Collection(x => x.Used).LoadAsync();
        }
    }

    // Загрузка игроков игры
    public async Task LoadGamePlayers(Game game)
    {
        await _context.Entry(game).Reference(x => x.WhitePlayer).LoadAsync();
        await _context.Entry(game).Reference(x => x.BlackPlayer).LoadAsync();
    }

    // Получение последних игр, отвечающих условию
    public async Task<List<Game>> TopFilteredGames(System.Linq.Expressions.Expression<Func<Game, bool>> filter)
    {
        const int TOP_COUNT = 10;

        return await _context.Games
            .Include(x => x.WhitePlayer)
            .Include(x => x.BlackPlayer)
            .Where(filter)
            .OrderByDescending(x => x.Id)
            .Take(TOP_COUNT)
            .ToListAsync();
    }

    // Новые игры
    public async Task<List<Game>> TopNewGames() =>
        await TopFilteredGames(x => x.State == GameState.NotStarted);

    // Текущие игры
    public async Task<List<Game>> TopCurrentGames() =>
        await TopFilteredGames(x => x.State == GameState.Running);

    // Прошедшие игры
    public async Task<List<Game>> TopPastGames() =>
        await TopFilteredGames(x => x.State == GameState.WhitePlayerWon || x.State == GameState.BlackPlayerWon || x.State == GameState.Draw);
}