using System.Diagnostics;
using System.Text.Json;
using CheckersWeb.BLL;
using CheckersWeb.BLL.Bot;
using CheckersWeb.DAL;
using CheckersWeb.DAL.Models;
using CheckersWeb.Hubs;
using CheckersWeb.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;

namespace CheckersWeb.Controllers;

public class GamesController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<IdentityUser> _userManager;
    private readonly IHubContext<GameHub> _hubContext;
    private readonly DAL.Games gamesData;
    private readonly BLL.Games gamesLogic;

    public GamesController(ApplicationDbContext context,
        UserManager<IdentityUser> userManager,
        IHubContext<GameHub> hubContext)
    {
        _context = context;
        _userManager = userManager;
        _hubContext = hubContext;

        gamesData = new DAL.Games(_context);
        gamesLogic = new BLL.Games();
    }

    // Главная страница - список всех игр
    public async Task<IActionResult> Index()
    {
        return View(await gamesData.All());
    }

    // Результаты поиска игр по игроку и его цвету в игре
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Search(string? player, string? color)
    {
        // Проверка параметров
        if (!gamesLogic.ValidateSearchParameters(player, color))
            return ValidationProblem();
        // Получение и вывод игр
        if (color == "White")
            return View(await gamesData.SearchByWhitePlayer(player!));
        else
            return View(await gamesData.SearchByBlackPlayer(player!));
    }

    // Страница создания игры
    [Authorize]
    public IActionResult Create()
    {
        return View();
    }

    // Запрос на создание игры
    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(string? color)
    {
        // Получение текущего пользователя
        var user = await _userManager.GetUserAsync(HttpContext.User);
        // Создание игры с ним
        var game = gamesLogic.Create(user, color);
        // Добавление её в базу данных
        await gamesData.Add(game);
        // Переход к списку игр
        return RedirectToAction(nameof(Index));
    }

    // Просмотр игры
    public async Task<IActionResult> View(int? id)
    {
        // Получение игры по номеру
        var game = await gamesData.Find(id);
        if (game == null)
            return NotFound();
        // Проверка возможности просмотра
        if (!gamesLogic.ValidateView(game))
            return ValidationProblem();
        // Загрузка ходов игры
        await gamesData.LoadGameMoves(game);
        // Отображение игры
        return View(game);
    }

    // Участие в игре
    [Authorize]
    public async Task<IActionResult> Play(int? id)
    {
        // Получение игры по номеру
        var game = await gamesData.Find(id);
        if (game == null)
            return NotFound();
        // Проверка возможности игры
        if (!gamesLogic.ValidatePlay(game))
            return ValidationProblem();

        // Загрузка игроков игры
        await gamesData.LoadGamePlayers(game);
        // Получение текущего пользователя
        var user = await _userManager.GetUserAsync(HttpContext.User);
        // Проверка, может ли пользователь участвовать в игре
        if (!gamesLogic.AuthorizePlayer(game, user))
            return Unauthorized();

        // Загрузка ходов игры
        await gamesData.LoadGameMoves(game);
        // Ход ботом, если это возможно
        if (gamesLogic.CanDoBotFirstMove(game))
        {
            var board = new Board(false);
            await DoBotMove(id.Value, game, PlayerColor.White, board);
        }
        // Отображение игры
        return View(game);
    }

    // Запрос на присоединение к игре
    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Join(int? id)
    {
        // Получение игры по номеру
        var game = await gamesData.Find(id);
        if (game == null)
            return NotFound();
        // Проверка, доступна ли игра для присоединения новых игроков (проверка по состоянию)
        if (!gamesLogic.ValidateJoinGameState(game))
            return ValidationProblem();

        // Загрузка игроков игры
        await gamesData.LoadGamePlayers(game);
        // Получение текущего пользователя
        var user = await _userManager.GetUserAsync(HttpContext.User);
        // Проверка, доступна ли игра для присоединения новых игроков (проверка по игрокам)
        if (!gamesLogic.ValidateJoinPlayers(game))
            return ValidationProblem();
        // Добавление игрока в игру
        gamesLogic.AddPlayerToGame(game, user);
        // Игра началась
        game.State = GameState.Running;

        // Обновление игры
        await gamesData.Update(game);
        // Перенаправление на страницу игры
        return RedirectToAction(nameof(Play), new { id = game.Id });
    }

    // Страница добавления бота в игру
    [Authorize]
    public async Task<IActionResult> AddBot(int? id)
    {
        // Получение игры по номеру
        var game = await gamesData.Find(id);
        if (game == null)
            return NotFound();
        return View();
    }

    // Запрос на добавление бота в игру
    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddBot(int? id, int? maxDepth)
    {
        // Получение игры по номеру
        var game = await gamesData.Find(id);
        if (game == null)
            return NotFound();
        // Проверка, возможно ли добавить бота в игру
        if (gamesLogic.ValidateAddBot(game, maxDepth))
            return ValidationProblem();

        // Загрузка игроков игры
        await gamesData.LoadGamePlayers(game);
        // Получение текущего пользователя
        var user = await _userManager.GetUserAsync(HttpContext.User);
        // Проверка, доступна ли игра для присоединения новых игроков (проверка по игрокам)
        if (!gamesLogic.ValidateJoinPlayers(game))
            return ValidationProblem();
        // Добавление игрока в игру
        gamesLogic.AddBotToGame(game, maxDepth);
        // Игра началась
        game.State = GameState.Running;

        // Обновление игры
        await gamesData.Update(game);
        // Перенаправление на страницу игры
        return RedirectToAction(nameof(Play), new { id = game.Id });
    }

    // Запрос на совершение хода
    [Authorize]
    [HttpPost]
    public async Task<IActionResult> DoMove(int id, [FromBody] JsonElement body)
    {
        // Получение игры по номеру
        var game = await gamesData.Find(id);
        if (game == null)
            return NotFound();
        if (game.State != GameState.Running)
        {
            return ValidationProblem();
        }

        // Загрузка игроков игры
        await gamesData.LoadGamePlayers(game);
        // Получение текущего пользователя
        var user = await _userManager.GetUserAsync(HttpContext.User);
        // Проверка, может ли пользователь участвовать в игре
        if (!gamesLogic.AuthorizePlayer(game, user))
            return Unauthorized();
        // Определение цвета игрока
        var player = user == game.WhitePlayer ? PlayerColor.White : PlayerColor.Black;

        // Получение хода
        var move = JsonSerializer.Deserialize<Move>(body);
        if (move == null)
            return ValidationProblem();

        // Загрузка ходов игры
        await gamesData.LoadGameMoves(game);
        // Выполнение всех ходов на доске
        var board = gamesLogic.DoAllMoves(game, out PlayerColor lastPlayer);
        // Игрок не может ходить два раза подряд
        if (player == lastPlayer)
            return Unauthorized();
        // Проверка, может ли игрок выполнить заданный ход на заданной доске
        if (!gamesLogic.CanPlayerDoMove(board, player, move))
            return Unauthorized();

        // Выполнение хода и обновление состояния игры
        gamesLogic.DoMove(game, board, move);
        // Обновление игры
        await gamesData.Update(game);
        // Отправка сообщения о ходе и состоянии игры
        await SendMove(id, game, move);
        // Ход ботом, если это возможно
        if (gamesLogic.CanDoBotMove(game))
            await DoBotMove(id, game, move.Player == PlayerColor.White ? PlayerColor.Black : PlayerColor.White, board);

        return Ok();
    }

    // Отправка сообщения о ходе и состоянии игры
    private async Task SendMove(int id, Game game, Move move)
    {
        // Все пользователи, просматривающие данную игру
        var group = _hubContext.Clients.Group(id.ToString());
        // Отправка нового хода
        await group.SendAsync("newMove", JsonSerializer.Serialize(move));
        // Отправка нового состояния игры
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
    }

    // Ход ботом
    private async Task DoBotMove(int id, Game game, PlayerColor botPlayer, Board board)
    {
        // Создание бота, получение хода
        var bot = new NegaScoutTranspositionBot(game, botPlayer, board);
        var botMove = bot.GetMove()!;
        // Выполнение хода и обновление состояния игры
        gamesLogic.DoMove(game, board, botMove);
        // Обновление игры
        await gamesData.Update(game);
        // Отправка сообщения о ходе и состоянии игры
        await SendMove(id, game, botMove);
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
