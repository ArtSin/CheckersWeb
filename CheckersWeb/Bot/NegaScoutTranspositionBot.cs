using CheckersWeb.Lib;
using CheckersWeb.Models;
using Haus.Math;

namespace CheckersWeb.Bot;

class NegaScoutTranspositionBot
{
    enum TableEntryType { Exact, LowerBound, UpperBound };

    class TableEntry
    {
        public int Depth { get; set; }
        public TableEntryType Type { get; set; }
        public int Value { get; set; }
    }

    // Бесконечность
    private const int INF = 1000000000;
    // Максимальное число позиций, для которых хранятся оценки в таблице транспозиций
    private const int MAX_TABLE_COUNT = 20000000;
    // Минимальная и максимальная допустимая глубина поиска
    public const int MIN_DEPTH = 1;
    public const int MAX_DEPTH = 10;

    // Генератор случайных чисел
    private XorShiftRandom random;
    // Доска
    private Board board;
    // Максимальная глубина
    private int maxDepth;
    // Игрок
    private PlayerColor player;
    // Таблица транспозиций
    private Dictionary<(ulong, uint), TableEntry> transpositionTable;

    public NegaScoutTranspositionBot(Game game, PlayerColor player, Board board)
    {
        this.random = new XorShiftRandom();
        this.board = board;
        this.maxDepth = (player == PlayerColor.White ? game.WhitePlayerBotDepth : game.BlackPlayerBotDepth).Value;
        this.player = player;
        this.transpositionTable = new Dictionary<(ulong, uint), TableEntry>();
    }

    public Move? GetMove()
    {
        Move? maxMove = null;
        NegaScoutTransposition(player, maxDepth, -INF, INF, ref maxMove);
        return maxMove;
    }

    private int NegaScoutTransposition(PlayerColor currPlayer, int depth, int alpha, int beta, ref Move? maxMove)
    {
        // Достигнута максимальная глубина поиска
        if (depth == 0)
            return EvalPieceRow(currPlayer);

        // Проверка, можно ли совершить взятие
        bool canCapture = board.CanPlayerCapture(currPlayer);
        // Все возможные ходы
        var allMoves = new List<Move>();
        for (int row = 0; row < Board.SIZE; row++)
            for (int col = 0; col < Board.SIZE; col++)
                allMoves.AddRange(board.GetMoves(currPlayer, new Cell(row, col), canCapture));
        // Нет ходов, проигрышная позиция
        if (allMoves.Count == 0)
            return -INF + 1;

        int alphaOrig = alpha;

        // Проверка наличия позиции в таблице транспозиций
        var boardHash = board.GetHash();
        if (transpositionTable.ContainsKey(boardHash))
        {
            var entry = transpositionTable[boardHash];
            // Запись в таблице для большей глубины и того же игрока
            if (entry.Depth <= depth && (depth - entry.Depth) % 2 == 0)
            {
                switch (entry.Type)
                {
                    // Точное значение
                    case TableEntryType.Exact:
                        return entry.Value;
                    // Оценка снизу
                    case TableEntryType.LowerBound:
                        alpha = Math.Max(alpha, entry.Value);
                        break;
                    // Оценка сверху
                    case TableEntryType.UpperBound:
                        beta = Math.Min(beta, entry.Value);
                        break;
                }

                if (alpha >= beta)
                    return entry.Value;
            }
        }

        // Наименьший возможный результат
        int result = -INF;
        // Перебор возможных ходов
        bool first = true;
        foreach (var move in allMoves)
        {
            // Применение хода
            board.DoMove(move);
            // Рекурсивный поиск (для другого игрока)
            int res;
            if (first)
            {
                res = -NegaScoutTransposition(1 - currPlayer, depth - 1, -beta, -alpha, ref maxMove);
                first = false;
            }
            else
            {
                res = -NegaScoutTransposition(1 - currPlayer, depth - 1, -alpha - 1, -alpha, ref maxMove);
                if (alpha < res && res < beta)
                    res = -NegaScoutTransposition(1 - currPlayer, depth - 1, -beta, -res, ref maxMove);
            }
            if (res > result)
            {
                // Обновление результата максимумом
                result = res;
                // Обновление лучшего хода
                if (depth == maxDepth)
                    maxMove = move;
            }
            // Отмена хода
            board.UndoMove(move);

            alpha = Math.Max(alpha, res);
            if (alpha >= beta)
                break;
        }

        // Добавление позиции в таблицу транспозиций
        if (transpositionTable.Count < MAX_TABLE_COUNT || transpositionTable.ContainsKey(boardHash))
        {
            if (!transpositionTable.ContainsKey(boardHash))
                transpositionTable[boardHash] = new TableEntry();

            var entry = transpositionTable[boardHash];
            // Оценка сверху
            if (result <= alphaOrig)
                entry.Type = TableEntryType.UpperBound;
            // Оценка снизу
            else if (result >= beta)
                entry.Type = TableEntryType.LowerBound;
            // Точное значение
            else
                entry.Type = TableEntryType.Exact;
            entry.Depth = depth;
            entry.Value = result;
        }

        return result;
    }

    // Оценка позиции
    private int EvalPieceRow(PlayerColor currPlayer)
    {
        int result = 0;
        for (int row = 0; row < Board.SIZE; row++)
            for (int col = 0; col < Board.SIZE; col++)
            {
                int res = 0;
                // Обычная шашка: 5 + количество рядов от начала доски до шашки
                if (board.PiecesType[row, col] == PieceType.Normal)
                    res = 5 + ((board.PiecesColor[row, col] == PlayerColor.White) ? row : (Board.SIZE - 1 - row));
                // Дамка: 15
                else if (board.PiecesType[row, col] == PieceType.King)
                    res = 7 + Board.SIZE;
                // Разность сумм значений шашек текущего игрока и противника
                result += board.PiecesColor[row, col] == currPlayer ? res : -res;
            }
        return (result * 256) + random.NextByte();
    }
}
