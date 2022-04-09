class Cell {
    Id: number | undefined;
    Row: number;
    Col: number;

    constructor(row: number, col: number) {
        this.Row = row;
        this.Col = col;
    }

    static Clone(other: Cell): Cell {
        return new Cell(other.Row, other.Col);
    }

    Equals(other: Cell): boolean {
        return this.Row == other.Row && this.Col == other.Col;
    }
}

class UsedCell {
    Id: number | undefined;
    Row: number;
    Col: number;
    Type: PieceType;

    constructor(row: number, col: number, type: PieceType) {
        this.Row = row;
        this.Col = col;
        this.Type = type;
    }

    static Clone(other: UsedCell): UsedCell {
        return new UsedCell(other.Row, other.Col, other.Type);
    }

    Equals(other: UsedCell): boolean {
        return this.Row == other.Row && this.Col == other.Col && this.Type == other.Type;
    }
}

// Ход одного игрока
class Move {
    // Игрок
    Player: PlayerColor;
    // Поля
    Cells: Cell[];
    // Номер поля, когда шашка становится дамкой
    PosKing: number;
    // Побитые за ход шашки
    Used: UsedCell[];

    constructor(player: PlayerColor, cells: Cell[], posKing: number, used?: UsedCell[]) {
        this.Player = player;
        this.Cells = cells;
        this.PosKing = posKing;
        this.Used = used == null ? [] : used;
    }

    static Clone(other: Move): Move {
        return new Move(other.Player, other.Cells.map(x => Cell.Clone(x)),
            other.PosKing, other.Used.map(x => UsedCell.Clone(x)));
    }

    AddCell(cell: Cell) {
        this.Cells.push(cell);
    }

    // Преобразование хода в удобочитаемую строку
    ToHumanReadableString(): string {
        return (this.Player == PlayerColor.White ? "Б: " : "Ч: ") +
            this.Cells
                .map((x) => String.fromCharCode(x.Col + 97) + (x.Row + 1).toString())
                .join(this.Used.length == 0 ? '-' : ':');
    }
}
