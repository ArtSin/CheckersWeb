using CheckersWeb.DAL;

namespace CheckersWeb.BLL;

// Доска
public class Board
{
    // Размер доски
    public const int SIZE = 8;

    // Типы шашек
    public PieceType[,] PiecesType { get; }
    // Цвета шашек
    public PlayerColor[,] PiecesColor { get; }

    // Создание пустой доски или с начальной расстановкой
    public Board(bool empty = true)
    {
        PiecesType = new PieceType[SIZE, SIZE];
        PiecesColor = new PlayerColor[SIZE, SIZE];

        // Пустая доска
        if (empty)
            return;
        // Шашки белого игрока
        for (int row = 0; row <= 2; row++)
            for (int col = 0; col < SIZE; col++)
                if (!GetPositionColor((row, col))) // Чёрная клетка
                {
                    PiecesType[row, col] = PieceType.Normal;
                    PiecesColor[row, col] = PlayerColor.White;
                }

        // Шашки чёрного игрока
        for (int row = SIZE - 1; row >= SIZE - 3; row--)
            for (int col = 0; col < SIZE; col++)
                if (!GetPositionColor((row, col))) // Чёрная клетка
                {
                    PiecesType[row, col] = PieceType.Normal;
                    PiecesColor[row, col] = PlayerColor.Black;
                }
    }

    // Копирование доски
    public Board(Board other)
    {
        PiecesType = (PieceType[,])other.PiecesType.Clone();
        PiecesColor = (PlayerColor[,])other.PiecesColor.Clone();

        for (int row = 0; row < SIZE; row++)
            for (int col = 0; col < SIZE; col++)
            {
                PiecesType[row, col] = other.PiecesType[row, col];
                PiecesColor[row, col] = other.PiecesColor[row, col];
            }
    }

    // Обновление доски информацией из другой доски
    public void Update(Board other)
    {
        for (int row = 0; row < SIZE; row++)
            for (int col = 0; col < SIZE; col++)
            {
                PiecesType[row, col] = other.PiecesType[row, col];
                PiecesColor[row, col] = other.PiecesColor[row, col];
            }
    }

    // Получение цвета клетки (false - чёрный, true - белый)
    public static bool GetPositionColor((int, int) position)
    {
        return ((position.Item1 + position.Item2) % 2) != 0;
    }

    // Совершение хода
    public void DoMove(Move move)
    {
        // Удаление побитых шашек
        foreach (var pr in move.Used)
            PiecesType[pr.Row, pr.Col] = PieceType.Empty;
        // Удаление шашки с начального поля
        PiecesType[move.Cells[0].Row, move.Cells[0].Col] = PieceType.Empty;
        // Добавление шашки на последнее поле
        var lastCell = move.Cells[move.Cells.Count - 1];
        PiecesType[lastCell.Row, lastCell.Col] =
            (move.PosKing != -1 ? PieceType.King : PieceType.Normal);
        PiecesColor[lastCell.Row, lastCell.Col] = move.Player;
    }

    // Отмена хода
    public void UndoMove(Move move)
    {
        // Удаление шашки с последнего поля
        var lastCell = move.Cells[move.Cells.Count - 1];
        PiecesType[lastCell.Row, lastCell.Col] = PieceType.Empty;
        // Добавление шашки на начальное поле
        var firstCell = move.Cells[0];
        PiecesType[firstCell.Row, firstCell.Col] =
            (move.PosKing == 0 ? PieceType.King : PieceType.Normal);
        PiecesColor[firstCell.Row, firstCell.Col] = move.Player;
        // Восстановление побитых шашек
        foreach (var pr in move.Used)
        {
            PiecesType[pr.Row, pr.Col] = pr.Type;
            PiecesColor[pr.Row, pr.Col] = 1 - move.Player;
        }
    }

    // Получение возможных ходов
    public List<Move> GetMoves(PlayerColor player, Cell cell, bool canCapture)
    {
        var moves = new List<Move>();
        // Нет шашки или шашка другого игрока
        if (PiecesType[cell.Row, cell.Col] == PieceType.Empty ||
            PiecesColor[cell.Row, cell.Col] != player)
            return moves;
        var startMove = new Move(player, new List<Cell>(),
            PiecesType[cell.Row, cell.Col] == PieceType.King ? 0 : -1);
        startMove.AddCell(cell);
        // Получение ходов
        GetMovesDFS(startMove, moves, canCapture);
        return moves;
    }

    private void GetMovesDFS(Move currMove, List<Move> moves, bool canCapture)
    {
        // Начальное поле
        var startCell = currMove.Cells[currMove.Cells.Count - 1];
        int r = startCell.Row, c = startCell.Col;
        // Обычная шашка
        if (currMove.PosKing == -1)
        {
            // Необходимо взятие
            if (canCapture)
            {
                // Ход по диагонали на 1 поле
                foreach (int dR in new[] { -1, 1 })
                    foreach (int dC in new[] { -1, 1 })
                    {
                        // Текущее поле
                        int cR = r + dR, cC = c + dC;
                        if (cR < 0 || cR >= SIZE || cC < 0 || cC >= SIZE)
                            continue;
                        // Следующее поле
                        int nR = cR + dR, nC = cC + dC;
                        if (nR < 0 || nR >= SIZE || nC < 0 || nC >= SIZE)
                            continue;
                        // Текущее поле занято непобитой шашкой противника и следующее поле свободно
                        if (PiecesType[cR, cC] != PieceType.Empty && PiecesColor[cR, cC] != currMove.Player &&
                            PiecesType[nR, nC] == PieceType.Empty &&
                            !currMove.Used.Contains(new UsedCell(cR, cC, PieceType.Normal)) &&
                            !currMove.Used.Contains(new UsedCell(cR, cC, PieceType.King)))
                        {
                            var newMove = new Move(currMove);
                            // Если шашка достигает последней горизонтали, то она становится дамкой
                            if ((currMove.Player == PlayerColor.White && nR == SIZE - 1) ||
                                (currMove.Player == PlayerColor.Black && nR == 0))
                                newMove.PosKing = newMove.Cells.Count;
                            // Добавление поля к новому ходу
                            newMove.AddCell(new Cell(nR, nC));
                            // Шашка противника побита
                            newMove.Used.Add(new UsedCell(cR, cC, PiecesType[cR, cC]));
                            // Если можно продолжить взятие
                            if (CanCapture(nR, nC, currMove.Player, newMove.PosKing != -1, newMove.Used))
                                GetMovesDFS(newMove, moves, true);
                            else // Ход завершается
                                moves.Add(newMove);
                        }
                    }
            }
            // Ход без взятия
            else
            {
                // Ход вперёд по диагонали на 1 поле
                int dR = currMove.Player == PlayerColor.White ? 1 : -1;
                foreach (int dC in new[] { -1, 1 })
                {
                    // Текущее поле
                    int cR = r + dR, cC = c + dC;
                    if (cR < 0 || cR >= SIZE || cC < 0 || cC >= SIZE)
                        continue;
                    if (PiecesType[cR, cC] != PieceType.Empty)
                        continue;
                    // Текущее поле свободно
                    var newMove = new Move(currMove);
                    // Если шашка достигает последней горизонтали, то она становится дамкой
                    if ((currMove.Player == PlayerColor.White && cR == SIZE - 1) ||
                        (currMove.Player == PlayerColor.Black && cR == 0))
                        newMove.PosKing = newMove.Cells.Count;
                    // Добавление поля к новому ходу
                    newMove.AddCell(new Cell(cR, cC));
                    // Ход завершается
                    moves.Add(newMove);
                }
            }
        }
        // Дамка
        else
        {
            // Необходимо взятие
            if (canCapture)
            {
                // Ход по диагонали на любое количество клеток
                foreach (int dR in new[] { -1, 1 })
                    foreach (int dC in new[] { -1, 1 })
                    {
                        // Шашка противника
                        int otherR = -1, otherC = -1;
                        for (int cR = r + dR, cC = c + dC;
                            cR >= 0 && cR < SIZE && cC >= 0 && cC < SIZE; cR += dR, cC += dC)
                        {
                            // Ещё не найдена шашка противника
                            if (otherR == -1)
                            {
                                // Если текущее поле занято непобитой шашкой противника
                                if (PiecesType[cR, cC] != PieceType.Empty &&
                                PiecesColor[cR, cC] != currMove.Player &&
                                !currMove.Used.Contains(new UsedCell(cR, cC, PieceType.Normal)) &&
                                !currMove.Used.Contains(new UsedCell(cR, cC, PieceType.King)))
                                {
                                    otherR = cR;
                                    otherC = cC;
                                }
                                // Иначе, если поле занято своей шашкой или уже побитой шашкой противника
                                else if (PiecesType[cR, cC] != PieceType.Empty)
                                    break;
                            }
                            // Если уже найдена шашка противника
                            else if (otherR != -1)
                            {
                                // И текущее поле несвободно
                                if (PiecesType[cR, cC] != PieceType.Empty)
                                    break;
                                else
                                {
                                    var newMove = new Move(currMove);
                                    // Добавление поля к новому ходу
                                    newMove.AddCell(new Cell(cR, cC));
                                    // Шашка противника побита
                                    newMove.Used.Add(new UsedCell(otherR, otherC, PiecesType[otherR, otherC]));
                                    // Если можно продолжить взятие
                                    if (CanCapture(cR, cC, currMove.Player, true, newMove.Used))
                                        GetMovesDFS(newMove, moves, true);
                                    else // Ход завершается
                                        moves.Add(newMove);
                                }
                            }
                        }
                    }
            }
            // Ход без взятия
            else
            {
                // Ход по диагонали на любое количество клеток
                foreach (int dR in new[] { -1, 1 })
                    foreach (int dC in new[] { -1, 1 })
                        for (int cR = r + dR, cC = c + dC;
                            cR >= 0 && cR < SIZE && cC >= 0 && cC < SIZE; cR += dR, cC += dC)
                        {
                            // Несвободная клетка
                            if (PiecesType[cR, cC] != PieceType.Empty)
                                break;
                            var newMove = new Move(currMove);
                            // Добавление поля к новому ходу
                            newMove.AddCell(new Cell(cR, cC));
                            // Ход завершается
                            moves.Add(newMove);
                        }
            }
        }
    }

    // Проверка, можно ли совершить взятие
    private bool CanCapture(int r, int c, PlayerColor player, bool isKing, List<UsedCell> used)
    {
        // Обычная шашка
        if (!isKing)
        {
            // Ход по диагонали на 1 поле
            foreach (int dR in new[] { -1, 1 })
                foreach (int dC in new[] { -1, 1 })
                {
                    // Текущее поле
                    int cR = r + dR, cC = c + dC;
                    if (cR < 0 || cR >= SIZE || cC < 0 || cC >= SIZE)
                        continue;
                    // Следующее поле
                    int nR = cR + dR, nC = cC + dC;
                    if (nR < 0 || nR >= SIZE || nC < 0 || nC >= SIZE)
                        continue;
                    // Текущее поле занято непобитой шашкой противника и следующее поле свободно
                    if (PiecesType[cR, cC] != PieceType.Empty && PiecesColor[cR, cC] != player &&
                        PiecesType[nR, nC] == PieceType.Empty &&
                        !used.Contains(new UsedCell(cR, cC, PieceType.Normal)) &&
                        !used.Contains(new UsedCell(cR, cC, PieceType.King)))
                        return true;
                }
        }
        // Дамка
        else
        {
            // Ход по диагонали на любое количество клеток
            foreach (int dR in new[] { -1, 1 })
                foreach (int dC in new[] { -1, 1 })
                {
                    // Шашка противника
                    int otherR = -1, otherC = -1;
                    for (int cR = r + dR, cC = c + dC;
                        cR >= 0 && cR < SIZE && cC >= 0 && cC < SIZE; cR += dR, cC += dC)
                    {
                        // Ещё не найдена шашка противника
                        if (otherR == -1)
                        {
                            // Если текущее поле занято непобитой шашкой противника
                            if (PiecesType[cR, cC] != PieceType.Empty &&
                            PiecesColor[cR, cC] != player &&
                            !used.Contains(new UsedCell(cR, cC, PieceType.Normal)) &&
                            !used.Contains(new UsedCell(cR, cC, PieceType.King)))
                            {
                                otherR = cR;
                                otherC = cC;
                            }
                            // Иначе, если поле занято своей шашкой или уже побитой шашкой противника
                            else if (PiecesType[cR, cC] != PieceType.Empty)
                                break;
                        }
                        // Если уже найдена шашка противника
                        else if (otherR != -1)
                        {
                            // И текущее поле несвободно
                            if (PiecesType[cR, cC] != PieceType.Empty)
                                break;
                            else
                                return true;
                        }
                    }
                }
        }
        return false;
    }

    // Проверка, может ли игрок совершить взятие
    public bool CanPlayerCapture(PlayerColor player)
    {
        for (int row = 0; row < SIZE; row++)
            for (int col = 0; col < SIZE; col++)
                if (PiecesType[row, col] != PieceType.Empty && PiecesColor[row, col] == player &&
                    CanCapture(row, col, player, PiecesType[row, col] == PieceType.King, new List<UsedCell>()))
                    return true;
        return false;
    }

    // Проверка, может ли игрок сделать ход
    public bool CanPlayerMove(PlayerColor player)
    {
        if (CanPlayerCapture(player))
            return true;
        for (int row = 0; row < SIZE; row++)
            for (int col = 0; col < SIZE; col++)
                if (GetMoves(player, new Cell(row, col), false).Count != 0)
                    return true;
        return false;
    }

    public (ulong, uint) GetHash()
    {
        ulong hash1 = 0;
        uint hash2 = 0;
        for (int row = 0, i = 0; row < SIZE; row++)
            for (int col = row % 2; col < SIZE; col += 2)
            {
                if (PiecesType[row, col] == PieceType.Normal)
                    hash1 |= (1UL << i);
                if (PiecesColor[row, col] == PlayerColor.White)
                    hash2 |= (1U << i);
                i++;
                if (PiecesType[row, col] == PieceType.King)
                    hash1 |= (1UL << i);
                i++;
            }
        return (hash1, hash2);
    }
}
