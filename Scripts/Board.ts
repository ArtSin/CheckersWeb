// Тип шашки (нет шашки, обычная, дамка)
enum PieceType { Empty, Normal, King };
// Цвет игрока
enum PlayerColor { Black, White };

// Доска
class Board {
    // Размер доски
    static SIZE = 8;

    // Типы шашек
    PiecesType: PieceType[][];
    // Цвета шашек
    PiecesColor: PlayerColor[][];

    // Создание пустой доски или с начальной расстановкой
    constructor(empty = true) {
        this.PiecesType = new Array(Board.SIZE);
        for (let i = 0; i < Board.SIZE; i++)
            this.PiecesType[i] = new Array(Board.SIZE).fill(PieceType.Empty);
        this.PiecesColor = new Array(Board.SIZE);
        for (let i = 0; i < Board.SIZE; i++)
            this.PiecesColor[i] = new Array(Board.SIZE).fill(PlayerColor.Black);

        // Пустая доска
        if (empty) {
            return;
        }
        // Шашки белого игрока
        for (var row = 0; row <= 2; row++)
            for (var col = 0; col < Board.SIZE; col++)
                if (!Board.GetPositionColor(row, col)) // Чёрная клетка
                {
                    this.PiecesType[row][col] = PieceType.Normal;
                    this.PiecesColor[row][col] = PlayerColor.White;
                }

        // Шашки чёрного игрока
        for (var row = Board.SIZE - 1; row >= Board.SIZE - 3; row--)
            for (var col = 0; col < Board.SIZE; col++)
                if (!Board.GetPositionColor(row, col)) // Чёрная клетка
                {
                    this.PiecesType[row][col] = PieceType.Normal;
                    this.PiecesColor[row][col] = PlayerColor.Black;
                }
    }

    // Получение цвета клетки (false - чёрный, true - белый)
    static GetPositionColor(row: number, col: number): boolean {
        return ((row + col) % 2) != 0;
    }

    // Совершение хода
    DoMove(move: Move) {
        // Удаление побитых шашек
        for (let pr of move.Used) {
            this.PiecesType[pr.Row][pr.Col] = PieceType.Empty;
        }
        // Удаление шашки с начального поля
        this.PiecesType[move.Cells[0].Row][move.Cells[0].Col] = PieceType.Empty;
        // Добавление шашки на последнее поле
        var lastCell = move.Cells[move.Cells.length - 1];
        this.PiecesType[lastCell.Row][lastCell.Col] =
            (move.PosKing != -1 ? PieceType.King : PieceType.Normal);
        this.PiecesColor[lastCell.Row][lastCell.Col] = move.Player;
    }

    // Получение возможных ходов
    GetMoves(player: PlayerColor, cell: Cell, canCapture: boolean): Move[] {
        let moves: Move[] = [];
        // Нет шашки или шашка другого игрока
        if (this.PiecesType[cell.Row][cell.Col] == PieceType.Empty || this.PiecesColor[cell.Row][cell.Col] != player)
            return moves;
        var startMove = new Move(player, [], this.PiecesType[cell.Row][cell.Col] == PieceType.King ? 0 : -1);
        startMove.AddCell(cell);
        // Получение ходов
        this.GetMovesDFS(startMove, moves, canCapture);
        return moves;
    }

    GetMovesDFS(currMove: Move, moves: Move[], canCapture: boolean) {
        // Начальное поле
        let startCell = currMove.Cells[currMove.Cells.length - 1];
        let r = startCell.Row, c = startCell.Col;
        // Обычная шашка
        if (currMove.PosKing == -1) {
            // Необходимо взятие
            if (canCapture) {
                // Ход по диагонали на 1 поле
                for (let dR of [-1, 1]) {
                    for (let dC of [-1, 1]) {
                        // Текущее поле
                        let cR = r + dR, cC = c + dC;
                        if (cR < 0 || cR >= Board.SIZE || cC < 0 || cC >= Board.SIZE)
                            continue;
                        // Следующее поле
                        let nR = cR + dR, nC = cC + dC;
                        if (nR < 0 || nR >= Board.SIZE || nC < 0 || nC >= Board.SIZE)
                            continue;
                        // Текущее поле занято непобитой шашкой противника и следующее поле свободно
                        if (this.PiecesType[cR][cC] != PieceType.Empty &&
                            this.PiecesColor[cR][cC] != currMove.Player &&
                            this.PiecesType[nR][nC] == PieceType.Empty &&
                            !currMove.Used.some(x => x.Equals(new UsedCell(cR, cC, PieceType.Normal))) &&
                            !currMove.Used.some(x => x.Equals(new UsedCell(cR, cC, PieceType.King)))) {
                            var newMove = Move.Clone(currMove);
                            // Если шашка достигает последней горизонтали, то она становится дамкой
                            if ((currMove.Player == PlayerColor.White && nR == Board.SIZE - 1) ||
                                (currMove.Player == PlayerColor.Black && nR == 0)) {
                                newMove.PosKing = newMove.Cells.length;
                            }
                            // Добавление поля к новому ходу
                            newMove.AddCell(new Cell(nR, nC));
                            // Шашка противника побита
                            newMove.Used.push(new UsedCell(cR, cC, this.PiecesType[cR][cC]));
                            // Если можно продолжить взятие
                            if (this.CanCapture(nR, nC, currMove.Player, newMove.PosKing != -1, newMove.Used))
                                this.GetMovesDFS(newMove, moves, true);
                            else {
                                // Ход завершается
                                moves.push(newMove);
                            }
                        }
                    }
                }
            }

            // Ход без взятия
            else {
                // Ход вперёд по диагонали на 1 поле
                let dR = currMove.Player == PlayerColor.White ? 1 : -1;
                for (let dC of [-1, 1]) {
                    // Текущее поле
                    let cR = r + dR, cC = c + dC;
                    if (cR < 0 || cR >= Board.SIZE || cC < 0 || cC >= Board.SIZE)
                        continue;
                    if (this.PiecesType[cR][cC] != PieceType.Empty)
                        continue;
                    // Текущее поле свободно
                    var newMove = Move.Clone(currMove);
                    // Если шашка достигает последней горизонтали, то она становится дамкой
                    if ((currMove.Player == PlayerColor.White && cR == Board.SIZE - 1) ||
                        (currMove.Player == PlayerColor.Black && cR == 0))
                        newMove.PosKing = newMove.Cells.length;
                    // Добавление поля к новому ходу
                    newMove.AddCell(new Cell(cR, cC));
                    // Ход завершается
                    moves.push(newMove);
                }
            }
        }

        // Дамка
        else {
            // Необходимо взятие
            if (canCapture) {
                // Ход по диагонали на любое количество клеток
                for (let dR of [-1, 1]) {
                    for (let dC of [-1, 1]) {
                        // Шашка противника
                        let otherR = -1, otherC = -1;
                        for (let cR = r + dR, cC = c + dC; cR >= 0 && cR < Board.SIZE && cC >= 0 && cC < Board.SIZE; cR += dR, cC += dC) {
                            // Ещё не найдена шашка противника
                            if (otherR == -1) {
                                // Если текущее поле занято непобитой шашкой противника
                                if (this.PiecesType[cR][cC] != PieceType.Empty &&
                                    this.PiecesColor[cR][cC] != currMove.Player &&
                                    !currMove.Used.some(x => x.Equals(new UsedCell(cR, cC, PieceType.Normal))) &&
                                    !currMove.Used.some(x => x.Equals(new UsedCell(cR, cC, PieceType.King)))) {
                                    otherR = cR;
                                    otherC = cC;
                                }

                                // Иначе, если поле занято своей шашкой или уже побитой шашкой противника
                                else if (this.PiecesType[cR][cC] != PieceType.Empty) {
                                    break;
                                }
                            }

                            // Если уже найдена шашка противника
                            else if (otherR != -1) {
                                // И текущее поле несвободно
                                if (this.PiecesType[cR][cC] != PieceType.Empty) {
                                    break;
                                }
                                else {
                                    var newMove = Move.Clone(currMove);
                                    // Добавление поля к новому ходу
                                    newMove.AddCell(new Cell(cR, cC));
                                    // Шашка противника побита
                                    newMove.Used.push(new UsedCell(otherR, otherC, this.PiecesType[otherR][otherC]));
                                    // Если можно продолжить взятие
                                    if (this.CanCapture(cR, cC, currMove.Player, true, newMove.Used))
                                        this.GetMovesDFS(newMove, moves, true);
                                    else // Ход завершается
                                        moves.push(newMove);
                                }
                            }
                        }
                    }
                }
            }

            // Ход без взятия
            else {
                // Ход по диагонали на любое количество клеток
                for (let dR of [-1, 1]) {
                    for (let dC of [-1, 1]) {
                        for (let cR = r + dR, cC = c + dC; cR >= 0 && cR < Board.SIZE && cC >= 0 && cC < Board.SIZE; cR += dR, cC += dC) {
                            // Несвободная клетка
                            if (this.PiecesType[cR][cC] != PieceType.Empty)
                                break;
                            var newMove = Move.Clone(currMove);
                            // Добавление поля к новому ходу
                            newMove.AddCell(new Cell(cR, cC));
                            // Ход завершается
                            moves.push(newMove);
                        }
                    }
                }
            }
        }
    }

    // Проверка, можно ли совершить взятие
    CanCapture(r: number, c: number, player: PlayerColor, isKing: boolean, used: UsedCell[]): boolean {
        // Обычная шашка
        if (!isKing) {
            // Ход по диагонали на 1 поле
            for (let dR of [-1, 1]) {
                for (let dC of [-1, 1]) {
                    // Текущее поле
                    let cR = r + dR, cC = c + dC;
                    if (cR < 0 || cR >= Board.SIZE || cC < 0 || cC >= Board.SIZE) {
                        continue;
                    }
                    // Следующее поле
                    let nR = cR + dR, nC = cC + dC;
                    if (nR < 0 || nR >= Board.SIZE || nC < 0 || nC >= Board.SIZE) {
                        continue;
                    }
                    // Текущее поле занято непобитой шашкой противника и следующее поле свободно
                    if (this.PiecesType[cR][cC] != PieceType.Empty && this.PiecesColor[cR][cC] != player &&
                        this.PiecesType[nR][nC] == PieceType.Empty &&
                        !used.some(x => x.Equals(new UsedCell(cR, cC, PieceType.Normal))) &&
                        !used.some(x => x.Equals(new UsedCell(cR, cC, PieceType.King)))) {
                        return true;
                    }
                }
            }
        }

        // Дамка
        else {
            // Ход по диагонали на любое количество клеток
            for (let dR of [-1, 1]) {
                for (let dC of [-1, 1]) {
                    // Шашка противника
                    let otherR = -1, otherC = -1;
                    for (let cR = r + dR, cC = c + dC; cR >= 0 && cR < Board.SIZE && cC >= 0 && cC < Board.SIZE; cR += dR, cC += dC) {
                        // Ещё не найдена шашка противника
                        if (otherR == -1) {
                            // Если текущее поле занято непобитой шашкой противника
                            if (this.PiecesType[cR][cC] != PieceType.Empty &&
                                this.PiecesColor[cR][cC] != player &&
                                !used.some(x => x.Equals(new UsedCell(cR, cC, PieceType.Normal))) &&
                                !used.some(x => x.Equals(new UsedCell(cR, cC, PieceType.King)))) {
                                otherR = cR;
                                otherC = cC;
                            }

                            // Иначе, если поле занято своей шашкой или уже побитой шашкой противника
                            else if (this.PiecesType[cR][cC] != PieceType.Empty) {
                                break;
                            }
                        }

                        // Если уже найдена шашка противника
                        else if (otherR != -1) {
                            // И текущее поле несвободно
                            if (this.PiecesType[cR][cC] != PieceType.Empty) {
                                break;
                            }
                            else {
                                return true;
                            }
                        }
                    }
                }
            }
        }
        return false;
    }

    // Проверка, может ли игрок совершить взятие
    CanPlayerCapture(player: PlayerColor): boolean {
        for (let row = 0; row < Board.SIZE; row++) {
            for (let col = 0; col < Board.SIZE; col++) {
                if (this.PiecesType[row][col] != PieceType.Empty && this.PiecesColor[row][col] == player &&
                    this.CanCapture(row, col, player, this.PiecesType[row][col] == PieceType.King, [])) {
                    return true;
                }
            }
        }
        return false;
    }

    // Проверка, может ли игрок сделать ход
    CanPlayerMove(player: PlayerColor): boolean {
        if (this.CanPlayerCapture(player)) {
            return true;
        }
        for (let row = 0; row < Board.SIZE; row++) {
            for (let col = 0; col < Board.SIZE; col++) {
                if (this.GetMoves(player, new Cell(row, col), false).length != 0) {
                    return true;
                }
            }
        }
        return false;
    }
}
