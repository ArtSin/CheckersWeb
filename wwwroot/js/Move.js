"use strict";
class Cell {
    constructor(row, col) {
        this.Row = row;
        this.Col = col;
    }
    static Clone(other) {
        return new Cell(other.Row, other.Col);
    }
    Equals(other) {
        return this.Row == other.Row && this.Col == other.Col;
    }
}
class UsedCell {
    constructor(row, col, type) {
        this.Row = row;
        this.Col = col;
        this.Type = type;
    }
    static Clone(other) {
        return new UsedCell(other.Row, other.Col, other.Type);
    }
    Equals(other) {
        return this.Row == other.Row && this.Col == other.Col && this.Type == other.Type;
    }
}
// Ход одного игрока
class Move {
    constructor(player, cells, posKing, used) {
        this.Player = player;
        this.Cells = cells;
        this.PosKing = posKing;
        this.Used = used == null ? [] : used;
    }
    static Clone(other) {
        return new Move(other.Player, other.Cells.map(x => Cell.Clone(x)), other.PosKing, other.Used.map(x => UsedCell.Clone(x)));
    }
    AddCell(cell) {
        this.Cells.push(cell);
    }
    // Преобразование хода в удобочитаемую строку
    ToHumanReadableString() {
        return (this.Player == PlayerColor.White ? "Б: " : "Ч: ") +
            this.Cells
                .map((x) => String.fromCharCode(x.Col + 97) + (x.Row + 1).toString())
                .join(this.Used.length == 0 ? '-' : ':');
    }
}
