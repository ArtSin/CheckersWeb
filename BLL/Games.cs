using CheckersWeb.BLL.Bot;
using CheckersWeb.DAL;
using CheckersWeb.DAL.Models;
using Microsoft.AspNetCore.Identity;

namespace CheckersWeb.BLL;

public class Games
{
    // Проверка параметров запроса на поиск игр по игроку и его цвету в игре
    public bool ValidateSearchParameters(string? player, string? color) =>
        player != null && color != null && (color == "White" || color == "Black");

    // Создание игры с заданным игроком и его цветом
    public Game Create(IdentityUser user, string? color)
    {
        bool isBlack = color == "Black";
        return new Game()
        {
            WhitePlayer = isBlack ? null : user,
            BlackPlayer = isBlack ? user : null,
            State = GameState.NotStarted,
            Moves = null
        };
    }

    // Проверка, доступна ли игра для просмотра
    public bool ValidateView(Game game) => game.State != GameState.NotStarted;

    // Проверка, доступна ли игра для участия
    public bool ValidatePlay(Game game) => game.State == GameState.Running;

    // Проверка, может ли пользователь участвовать в игре
    public bool AuthorizePlayer(Game game, IdentityUser? user) =>
        user == game.WhitePlayer || user == game.BlackPlayer;

    // Проверка, может ли бот сделать первый ход
    public bool CanDoBotFirstMove(Game game) =>
        game.WhitePlayerBotDepth != null && (game.Moves == null || game.Moves.Count == 0);

    // Проверка, доступна ли игра для присоединения новых игроков (проверка по состоянию)
    public bool ValidateJoinGameState(Game game) => game.State == GameState.NotStarted;

    // Проверка, доступна ли игра для присоединения новых игроков (проверка по игрокам)
    public bool ValidateJoinPlayers(Game game)
    {
        var whitePlayerNull = game.WhitePlayer == null && game.WhitePlayerBotDepth == null;
        var blackPlayerNull = game.BlackPlayer == null && game.BlackPlayerBotDepth == null;
        // В игре должен быть ровно один игрок (или бот)
        return !(whitePlayerNull && blackPlayerNull) && !(!whitePlayerNull && !blackPlayerNull);
    }

    // Добавление игрока в игру
    public void AddPlayerToGame(Game game, IdentityUser user)
    {
        if (game.WhitePlayer == null && game.WhitePlayerBotDepth == null)
            game.WhitePlayer = user;
        else
            game.BlackPlayer = user;
    }

    // Проверка, возможно ли добавить бота в игру
    public bool ValidateAddBot(Game game, int? maxDepth) =>
        maxDepth != null && maxDepth >= NegaScoutTranspositionBot.MIN_DEPTH && maxDepth <= NegaScoutTranspositionBot.MAX_DEPTH &&
        game.State == GameState.NotStarted;

    // Добавление бота в игру
    public void AddBotToGame(Game game, int? maxDepth)
    {
        if (game.WhitePlayer == null && game.WhitePlayerBotDepth == null)
            game.WhitePlayerBotDepth = maxDepth;
        else
            game.BlackPlayerBotDepth = maxDepth;
    }

    // Выполнение всех ходов на доске
    public Board DoAllMoves(Game game, out PlayerColor lastPlayer)
    {
        var board = new Board(false);
        lastPlayer = PlayerColor.Black;
        if (game.Moves != null)
        {
            foreach (var currMove in game.Moves)
                board.DoMove(currMove);
            lastPlayer = game.Moves.Last().Player;
        }
        return board;
    }

    // Проверка, может ли игрок выполнить заданный ход на заданной доске
    public bool CanPlayerDoMove(Board board, PlayerColor player, Move move) =>
        board.GetMoves(player, move.Cells[0], board.CanPlayerCapture(player)).Contains(move);

    // Выполнение хода и обновление состояния игры
    public void DoMove(Game game, Board board, Move move)
    {
        // Максимальное количество ходов до ничьей
        const int MAX_MOVES = 200;

        // Добавление хода
        if (game.Moves == null)
            game.Moves = new List<Move>();
        game.Moves.Add(move);
        board.DoMove(move);

        // Обновление состояния игры
        if (move.Player == PlayerColor.White && !board.CanPlayerMove(PlayerColor.Black))
            game.State = GameState.WhitePlayerWon;
        else if (move.Player == PlayerColor.Black && !board.CanPlayerMove(PlayerColor.White))
            game.State = GameState.BlackPlayerWon;
        else if (game.Moves.Count >= MAX_MOVES)
            game.State = GameState.Draw;
    }

    // Проверка, может ли бот сделать ход после другого хода
    public bool CanDoBotMove(Game game) =>
        game.State == GameState.Running && (game.WhitePlayerBotDepth != null || game.BlackPlayerBotDepth != null);
}