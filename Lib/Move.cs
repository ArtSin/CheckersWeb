namespace CheckersWeb.Lib;

public class Cell : IEquatable<Cell>
{
    public int Id { get; set; }
    public int Row { get; set; }
    public int Col { get; set; }

    public Cell() { }

    public Cell(int row, int col)
    {
        Row = row;
        Col = col;
    }

    public Cell(Cell other)
    {
        Row = other.Row;
        Col = other.Col;
    }

    public bool Equals(Cell? other)
    {
        if (other == null)
            return false;
        return Row == other.Row && Col == other.Col;
    }
}

public class UsedCell : IEquatable<UsedCell>
{
    public int Id { get; set; }
    public int Row { get; set; }
    public int Col { get; set; }
    public PieceType Type { get; set; }

    public UsedCell() { }

    public UsedCell(int row, int col, PieceType type)
    {
        Row = row;
        Col = col;
        Type = type;
    }

    public UsedCell(UsedCell other)
    {
        Row = other.Row;
        Col = other.Col;
        Type = other.Type;
    }

    public bool Equals(UsedCell? other)
    {
        if (other == null)
            return false;
        return Row == other.Row && Col == other.Col && Type == other.Type;
    }
}

// Ход одного игрока
public class Move : IEquatable<Move>
{
    public int Id { get; set; }
    // Игрок
    public PlayerColor Player { get; set; }
    // Поля
    public List<Cell> Cells { get; set; }
    // Номер поля, когда шашка становится дамкой
    public int PosKing { get; set; }
    // Побитые за ход шашки
    public List<UsedCell> Used { get; set; }

    public Move() { }

    // Создание хода
    public Move(PlayerColor player, List<Cell> cells, int posKing)
    {
        Player = player;
        Cells = cells;
        PosKing = posKing;
        Used = new List<UsedCell>();
    }

    // Создание хода из другого хода
    public Move(Move other)
    {
        Player = other.Player;
        Cells = new List<Cell>(other.Cells.Count);
        foreach (var cell in other.Cells)
            Cells.Add(new Cell(cell));
        PosKing = other.PosKing;
        Used = new List<UsedCell>(other.Used.Count);
        foreach (var usedCell in other.Used)
            Used.Add(new UsedCell(usedCell));
    }

    // Добавление поля в ход
    public void AddCell(Cell cell) => Cells.Add(cell);

    // Преобразование хода в удобочитаемую строку
    public string ToHumanReadableString() =>
        (Player == PlayerColor.White ? "Б: " : "Ч: ") +
        string.Join(Used.Count == 0 ? '-' : ':',
            Cells.Select(pr => $"{(char)(pr.Col + 'a')}{pr.Row + 1}"));

    public bool Equals(Move? other)
    {
        if (other == null)
            return false;
        return Player == other.Player
            && Cells.SequenceEqual(other.Cells)
            && PosKing == other.PosKing
            && Used.SequenceEqual(other.Used);
    }
}
