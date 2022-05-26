// Отрисовка доски
class BoardControl {
    // Цвета фона для клеток
    static WHITE_CELL_COLOR = "rgb(255, 206, 158)";
    static BLACK_CELL_COLOR = "rgb(209, 139, 71)";

    // Цвета для выделенных, доступных, использованных полей
    static SELECTED_CELL_COLOR = "rgb(255, 0, 0)";
    static AVAILABLE_CELL_COLOR = "rgb(0, 128, 0)";
    static USED_CELL_COLOR = "rgb(0, 0, 255)";
    static MAIN_LINE_WIDTH = 3.0;

    // Цвета для последнего хода
    static LAST_MOVE_COLOR = "rgb(128, 128, 128)";
    static LAST_MOVE_USED_COLOR = "rgb(148, 0, 211)";
    static LAST_MOVE_LINE_WIDTH = 5.0;

    // Выбранное поле
    selectedCell: Cell = new Cell(-1, -1);
    // Доступные ходы
    availableMoves: Move[] = [];
    // Доступные поля
    availableCells: boolean[][] = new Array(Board.SIZE);
    // Количество использованных полей за ход
    moveIndex: number = 0;
    // Использованные поля
    usedCells: boolean[][] = new Array(Board.SIZE);
    // Последний ход
    lastMove: Move | null = null;

    canvas: HTMLCanvasElement | null = null;
    ctx: CanvasRenderingContext2D | null = null;

    constructor() {
        for (let i = 0; i < Board.SIZE; i++)
            this.availableCells[i] = new Array(Board.SIZE).fill(false);
        for (let i = 0; i < Board.SIZE; i++)
            this.usedCells[i] = new Array(Board.SIZE).fill(false);
    }

    set Canvas(canvas: HTMLCanvasElement) {
        this.canvas = canvas;
        this.ctx = canvas.getContext("2d")!;
    }

    // Сброс выделенных, доступных, использованных полей
    Reset(invalidate: boolean = false, resetLastMove: boolean = false) {
        // Сброс выбранного поля
        this.selectedCell = new Cell(-1, -1);
        // Сброс доступных ходов
        this.availableMoves = [];
        // Сброс доступных полей
        for (let r = 0; r < Board.SIZE; r++) {
            for (let c = 0; c < Board.SIZE; c++) {
                this.availableCells[r][c] = false;
            }
        }
        // Cброс использованных полей
        for (let r = 0; r < Board.SIZE; r++) {
            for (let c = 0; c < Board.SIZE; c++) {
                this.usedCells[r][c] = false;
            }
        }
        this.moveIndex = 0;
        // Сброс последнего хода
        if (resetLastMove) {
            this.lastMove = null;
        }
        // Если требуется перерисовка доски
        if (invalidate)
            this.Paint();
    }

    OnClick(e: MouseEvent, boundingClientRect: DOMRect) {
        if (gameState != GameState.Running || thisPlayer != currPlayer)
            return;
        // Ход текущего игрока

        // Координаты поля по координатам мыши
        let [row, col] = BoardControl.GetCellCoords([e.clientX - boundingClientRect.left, e.clientY - boundingClientRect.top],
            [this.canvas!.clientWidth, this.canvas!.clientHeight]);
        if (row < 0 && row >= Board.SIZE && col < 0 && col >= Board.SIZE) {
            return;
        }
        // Правильные координаты

        // Если поле уже выбрано, то снять выделение
        if (this.moveIndex == 0 && this.selectedCell.Row == row && this.selectedCell.Col == col) {
            this.Reset();
        }
        // Иначе выбрать поле для начала хода
        else if (this.moveIndex == 0 && !this.availableCells[row][col]) {
            this.Reset();
            // Выбранное поле - нажатое поле
            this.selectedCell = new Cell(row, col);
            // Доступные ходы
            this.availableMoves = board.GetMoves(thisPlayer,
                this.selectedCell, board.CanPlayerCapture(thisPlayer));
            // Заполнение доступных полей
            for (let move of this.availableMoves)
                this.availableCells[move.Cells[1].Row][move.Cells[1].Col] = true;
            // Использовано начальное поле
            this.usedCells[row][col] = true;
        }
        // Ход уже выполняется
        else {
            // Выбор ходов, в которых используется нажатое поле
            var newAvailableMoves = this.availableMoves.filter((move) =>
                move.Cells[this.moveIndex + 1].Row == row && move.Cells[this.moveIndex + 1].Col == col);
            if (newAvailableMoves.length != 0) {
                this.availableMoves = newAvailableMoves;
                // Если конец хода
                if (this.availableMoves[0].Cells.length == this.moveIndex + 2) {
                    // Совершить ход
                    GameClient.DoMove(this.availableMoves[0]);
                    this.Reset(false, true);
                    this.moveIndex = 0;
                }
                else {
                    // Поле использовано
                    this.moveIndex++;
                    // Сброс доступных полей
                    for (let r = 0; r < Board.SIZE; r++) {
                        for (let c = 0; c < Board.SIZE; c++) {
                            this.availableCells[r][c] = false;
                        }
                    }
                    // Заполнение доступных полей
                    for (let move of this.availableMoves)
                        this.availableCells[move.Cells[this.moveIndex + 1].Row][move.Cells[this.moveIndex + 1].Col] = true;
                    // Поле использовано
                    this.usedCells[row][col] = true;
                }
            }
        }

        this.Paint();
    }

    // Координаты поля по координатам мыши
    static GetCellCoords(mouseCoords: [number, number], boardSize: [number, number]): [number, number] {
        // Размер поля
        let cellSize = (Math.min(boardSize[0], boardSize[1]) - 30) / Board.SIZE;

        let loc = [mouseCoords[0] - boardSize[0] / 2 + cellSize * Board.SIZE / 2,
        mouseCoords[1] - boardSize[1] / 2 + cellSize * Board.SIZE / 2];

        let col = Math.floor(loc[0] / cellSize);
        let row = -Math.floor((loc[1] + 15) / cellSize - Board.SIZE + 1);
        return [row, col];
    }

    Paint() {
        if (this.canvas == null || this.ctx == null)
            return;

        this.ctx.clearRect(0, 0, this.canvas.clientWidth, this.canvas.clientHeight);
        this.ctx.save();

        // Размер поля
        let cellSize = (Math.min(this.canvas.clientWidth, this.canvas.clientHeight) - 30) / Board.SIZE;

        // Центр доски в (0, 0)
        this.ctx.translate(-cellSize * Board.SIZE / 2.0, -cellSize * Board.SIZE / 2.0);
        // Центр доски в центре области рисования
        this.ctx.translate(this.canvas.clientWidth / 2.0, this.canvas.clientHeight / 2.0);
        // Отрисовка полей
        for (let row = 0; row < Board.SIZE; row++) {
            for (let col = 0; col < Board.SIZE; col++) {
                // Сохранение преобразования
                this.ctx.save();
                // Переход к текущему полю
                this.ctx.translate(col * cellSize, (Board.SIZE - 1 - row) * cellSize - 15);
                // Фон
                this.ctx.fillStyle = Board.GetPositionColor(row, col) ?
                    BoardControl.WHITE_CELL_COLOR : BoardControl.BLACK_CELL_COLOR;
                this.ctx.fillRect(0, 0, cellSize, cellSize);
                // Если поле выделено
                if (this.selectedCell.Row == row && this.selectedCell.Col == col) {
                    this.ctx.strokeStyle = BoardControl.SELECTED_CELL_COLOR;
                    this.ctx.lineWidth = BoardControl.MAIN_LINE_WIDTH;
                    this.ctx.strokeRect(1, 1, cellSize - 3, cellSize - 3);
                }
                // Если поле доступно для хода
                else if (this.availableCells[row][col]) {
                    this.ctx.strokeStyle = BoardControl.AVAILABLE_CELL_COLOR;
                    this.ctx.lineWidth = BoardControl.MAIN_LINE_WIDTH;
                    this.ctx.strokeRect(1, 1, cellSize - 3, cellSize - 3);
                }
                // Если поле использовано
                else if (this.usedCells[row][col]) {
                    this.ctx.strokeStyle = BoardControl.USED_CELL_COLOR;
                    this.ctx.lineWidth = BoardControl.MAIN_LINE_WIDTH;
                    this.ctx.strokeRect(1, 1, cellSize - 3, cellSize - 3);
                }
                // Восстановление преобразования
                this.ctx.restore();
            }
        }
        // Отрисовка последнего хода
        if (this.lastMove != null) {
            for (let i = 0; i < this.lastMove.Cells.length - 1; i++) {
                var currCell = this.lastMove.Cells[i];
                var nextCell = this.lastMove.Cells[i + 1];

                this.ctx.strokeStyle = BoardControl.LAST_MOVE_COLOR;
                this.ctx.lineWidth = BoardControl.LAST_MOVE_LINE_WIDTH;

                this.ctx.beginPath();
                this.ctx.moveTo((currCell.Col + 0.5) * cellSize,
                    (Board.SIZE - 0.5 - currCell.Row) * cellSize - 15);
                this.ctx.lineTo((nextCell.Col + 0.5) * cellSize,
                    (Board.SIZE - 0.5 - nextCell.Row) * cellSize - 15);
                this.ctx.stroke();
            }
            for (let pr of this.lastMove.Used) {
                this.ctx.save();
                this.ctx.translate((pr.Col + 0.5) * cellSize,
                    (Board.SIZE - 0.5 - pr.Row) * cellSize - 15);

                this.ctx.strokeStyle = BoardControl.LAST_MOVE_USED_COLOR;
                this.ctx.lineWidth = BoardControl.LAST_MOVE_LINE_WIDTH;

                this.ctx.beginPath();
                this.ctx.moveTo(-cellSize / 8, -cellSize / 8);
                this.ctx.lineTo(cellSize / 8, cellSize / 8);
                this.ctx.stroke();

                this.ctx.beginPath();
                this.ctx.moveTo(-cellSize / 8, cellSize / 8);
                this.ctx.lineTo(cellSize / 8, -cellSize / 8);
                this.ctx.stroke();

                this.ctx.restore();
            }
        }
        // Отрисовка шашек
        for (let row = 0; row < Board.SIZE; row++) {
            for (let col = 0; col < Board.SIZE; col++) {
                // Сохранение преобразования
                this.ctx.save();
                // Переход к текущему полю
                this.ctx.translate(col * cellSize, (Board.SIZE - 1 - row) * cellSize - 15);
                // Отрисовка шашки
                if (board != null) {
                    var paintColorPiece = (board.PiecesColor[row][col] == PlayerColor.White) ?
                        () => this.PaintPiece(this.ctx!, cellSize, "white", "black") :
                        () => this.PaintPiece(this.ctx!, cellSize, "black", "white");

                    switch (board.PiecesType[row][col]) {
                        // Обычная
                        case PieceType.Normal:
                            paintColorPiece();
                            break;
                        // Дамка
                        case PieceType.King:
                            this.ctx.save();
                            this.ctx.translate(0, 0.15 * cellSize);
                            paintColorPiece();
                            this.ctx.restore();

                            paintColorPiece();
                            break;
                        // Нет шашки
                        default:
                            break;
                    }
                }
                // Восстановление преобразования
                this.ctx.restore();
            }
        }

        // Шрифт
        this.ctx.font = "18px sans-serif";
        this.ctx.fillStyle = "black";
        this.ctx.textAlign = "right";
        this.ctx.textBaseline = "middle";
        // Подписи для строк
        for (let row = 0; row < Board.SIZE; row++) {
            var str = `${row + 1}`;
            this.ctx.fillText(str, 0, (Board.SIZE - 0.5 - row) * cellSize - 15);
        }
        this.ctx.textAlign = "center";
        this.ctx.textBaseline = "top";
        // Подписи для столбцов
        for (let col = 0; col < Board.SIZE; col++) {
            var str = String.fromCharCode(col + 97);
            this.ctx.fillText(str, (col + 0.5) * cellSize, Board.SIZE * cellSize - 15);
        }

        this.ctx.restore();
    }

    // Отрисовка шашки
    PaintPiece(ctx: CanvasRenderingContext2D, cellSize: number, color1: string, color2: string) {
        ctx.moveTo(0, 0);

        // Цвета и ширина линии
        ctx.fillStyle = color1;
        ctx.strokeStyle = color2;
        ctx.lineWidth = 2.0 / cellSize;

        // Масштабирование
        ctx.scale(cellSize, cellSize);

        // Заливка
        ctx.fillRect(0.1, 0.4, 0.8, 0.15);

        ctx.beginPath();
        ctx.ellipse(0.5, 0.4, 0.4, 0.15, 0.0, 0.0, 2.0 * Math.PI);
        ctx.fill();

        ctx.beginPath();
        ctx.ellipse(0.5, 0.55, 0.4, 0.15, 0.0, 0.0, 2.0 * Math.PI);
        ctx.fill();

        // Рамки
        ctx.beginPath();
        ctx.ellipse(0.5, 0.4, 0.4, 0.15, 0.0, 0.0, 2.0 * Math.PI);
        ctx.stroke();

        ctx.beginPath();
        ctx.moveTo(0.1, 0.4);
        ctx.lineTo(0.1, 0.55);
        ctx.stroke();

        ctx.beginPath();
        ctx.moveTo(0.9, 0.4);
        ctx.lineTo(0.9, 0.55);
        ctx.stroke();

        ctx.beginPath();
        ctx.ellipse(0.5, 0.55, 0.4, 0.15, 0.0, 0.0, Math.PI);
        ctx.stroke();
    }
}
