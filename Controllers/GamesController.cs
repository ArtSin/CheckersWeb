using System.Diagnostics;
using System.Text.Json;
using CheckersWeb.Hubs;
using CheckersWeb.Lib;
using CheckersWeb.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace CheckersWeb.Controllers;

public class GamesController : Controller
{
    // Максимальное количество ходов до ничьей
    private const int MAX_MOVES = 200;

    private readonly ApplicationDbContext _context;
    private readonly UserManager<IdentityUser> _userManager;
    private readonly IHubContext<GameHub> _hubContext;

    public GamesController(ApplicationDbContext context,
        UserManager<IdentityUser> userManager,
        IHubContext<GameHub> hubContext)
    {
        _context = context;
        _userManager = userManager;
        _hubContext = hubContext;
    }

    public async Task<IActionResult> Index()
    {
        return View(await _context.Games
            .Include(x => x.WhitePlayer)
            .Include(x => x.BlackPlayer)
            .OrderByDescending(x => x.Id)
            .ToListAsync());
    }

    [Authorize]
    public IActionResult Create()
    {
        return View();
    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(string? player)
    {
        bool isBlack = player == "Black";
        var user = await _userManager.GetUserAsync(HttpContext.User);
        var game = new Game()
        {
            WhitePlayer = isBlack ? null : user,
            BlackPlayer = isBlack ? user : null,
            State = GameState.NotStarted,
            Moves = null
        };

        if (ModelState.IsValid)
        {
            _context.Add(game);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> View(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var game = await _context.Games.FindAsync(id);
        if (game == null)
        {
            return NotFound();
        }
        if (game.State == GameState.NotStarted)
        {
            return ValidationProblem();
        }

        await _context.Entry(game).Collection(x => x.Moves).LoadAsync();
        if (game.Moves != null)
        {
            foreach (var currMove in game.Moves)
            {
                await _context.Entry(currMove).Collection(x => x.Cells).LoadAsync();
                await _context.Entry(currMove).Collection(x => x.Used).LoadAsync();
            }
        }

        return View(game);
    }

    [Authorize]
    public async Task<IActionResult> Play(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var game = await _context.Games.FindAsync(id);
        if (game == null)
        {
            return NotFound();
        }
        if (game.State != GameState.Running)
        {
            return ValidationProblem();
        }

        await _context.Entry(game).Reference(x => x.WhitePlayer).LoadAsync();
        await _context.Entry(game).Reference(x => x.BlackPlayer).LoadAsync();
        var user = await _userManager.GetUserAsync(HttpContext.User);
        if (user != game.WhitePlayer && user != game.BlackPlayer)
        {
            return Unauthorized();
        }

        await _context.Entry(game).Collection(x => x.Moves).LoadAsync();
        if (game.Moves != null)
        {
            foreach (var currMove in game.Moves)
            {
                await _context.Entry(currMove).Collection(x => x.Cells).LoadAsync();
                await _context.Entry(currMove).Collection(x => x.Used).LoadAsync();
            }
        }

        return View(game);
    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Join(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var game = await _context.Games.FindAsync(id);
        if (game == null)
        {
            return NotFound();
        }
        if (game.State != GameState.NotStarted)
        {
            return ValidationProblem();
        }

        await _context.Entry(game).Reference(x => x.WhitePlayer).LoadAsync();
        await _context.Entry(game).Reference(x => x.BlackPlayer).LoadAsync();

        var user = await _userManager.GetUserAsync(HttpContext.User);
        if ((game.WhitePlayer == null && game.BlackPlayer == null) || (game.WhitePlayer != null && game.BlackPlayer != null))
            return ValidationProblem();
        else if (game.WhitePlayer == null)
            game.WhitePlayer = user;
        else
            game.BlackPlayer = user;

        game.State = GameState.Running;

        try
        {
            _context.Update(game);
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!_context.Games.Any(e => e.Id == id))
            {
                return NotFound();
            }
            else
            {
                throw;
            }
        }
        return RedirectToAction(nameof(Play), new { id = game.Id });
    }

    [Authorize]
    [HttpPost]
    public async Task<IActionResult> DoMove(int id, [FromBody] JsonElement body)
    {
        var game = await _context.Games.FindAsync(id);
        if (game == null)
        {
            return NotFound();
        }
        if (game.State != GameState.Running)
        {
            return ValidationProblem();
        }

        await _context.Entry(game).Reference(x => x.WhitePlayer).LoadAsync();
        await _context.Entry(game).Reference(x => x.BlackPlayer).LoadAsync();
        var user = await _userManager.GetUserAsync(HttpContext.User);
        if (user != game.WhitePlayer && user != game.BlackPlayer)
        {
            return Unauthorized();
        }
        var player = user == game.WhitePlayer ? PlayerColor.White : PlayerColor.Black;

        var move = JsonSerializer.Deserialize<Move>(body);
        if (move == null)
        {
            return ValidationProblem();
        }

        await _context.Entry(game).Collection(x => x.Moves).LoadAsync();
        var board = new Board(false);
        var lastPlayer = PlayerColor.Black;
        if (game.Moves != null)
        {
            foreach (var currMove in game.Moves)
            {
                await _context.Entry(currMove).Collection(x => x.Cells).LoadAsync();
                await _context.Entry(currMove).Collection(x => x.Used).LoadAsync();
                board.DoMove(currMove);
                lastPlayer = game.Moves.Last().Player;
            }
        }
        if (player == lastPlayer)
        {
            return Unauthorized();
        }

        var moves = board.GetMoves(player, move.Cells[0], board.CanPlayerCapture(player));
        if (!moves.Contains(move))
        {
            return Unauthorized();
        }

        if (game.Moves == null)
            game.Moves = new List<Move>();
        game.Moves.Add(move);
        board.DoMove(move);

        if (move.Player == PlayerColor.White && !board.CanPlayerMove(PlayerColor.Black))
        {
            game.State = GameState.WhitePlayerWon;
        }
        else if (move.Player == PlayerColor.Black && !board.CanPlayerMove(PlayerColor.White))
        {
            game.State = GameState.BlackPlayerWon;
        }
        else if (game.Moves.Count >= MAX_MOVES)
        {
            game.State = GameState.Draw;
        }

        _context.Update(game);
        await _context.SaveChangesAsync();

        var group = _hubContext.Clients.Group(id.ToString());
        await group.SendAsync("newMove", JsonSerializer.Serialize(move));
        switch (game.State)
        {
            case GameState.WhitePlayerWon:
                await group.SendAsync("whitePlayerWon");
                break;
            case GameState.BlackPlayerWon:
                await group.SendAsync("blackPlayerWon");
                break;
            case GameState.Draw:
                await group.SendAsync("draw");
                break;
            default:
                break;
        };

        return Ok();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
