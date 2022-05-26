enum GameState {
    NotStarted,
    Running,
    WhitePlayerWon,
    BlackPlayerWon,
    Draw
};

class GameClient {
    static async Initialize() {
        for (let move of moves) {
            board.DoMove(move);
            boardControl.lastMove = move;
            currPlayer = move.Player;
        }
        boardControl.Reset(true);
        currPlayer = currPlayer == PlayerColor.White ? PlayerColor.Black : PlayerColor.White;

        connection.on("newMove", (moveStr: string) => {
            this.OnNewMove(Move.Clone(JSON.parse(moveStr)));
        });
        connection.on("whitePlayerWon", () => { this.OnWhitePlayerWon(); });
        connection.on("blackPlayerWon", () => { this.OnBlackPlayerWon(); });
        connection.on("draw", () => { this.OnDraw(); });
        await connection.start();
        connection.invoke("AddToGroup", GAME_ID);
    }

    static UpdateState() {
        var statusText = document.getElementById("status-text")!;
        if (gameState == GameState.Running) {
            if (VIEW_ONLY) {
                statusText.innerText = currPlayer == PlayerColor.White ? "Ход белого игрока" : "Ход чёрного игрока";
            }
            else {
                statusText.innerText = thisPlayer == currPlayer ? "Ваш ход" : "Ход другого игрока";
            }
        }
        else if (gameState == GameState.WhitePlayerWon) {
            statusText.innerText = "Белый игрок выиграл";
        }
        else if (gameState == GameState.BlackPlayerWon) {
            statusText.innerText = "Чёрный игрок выиграл";
        }
        else if (gameState == GameState.Draw) {
            statusText.innerText = "Ничья";
        }
    }

    static OnWhitePlayerWon() {
        gameState = GameState.WhitePlayerWon;
        this.UpdateState();
        window.alert("Белый игрок выиграл!");
    }

    static OnBlackPlayerWon() {
        gameState = GameState.BlackPlayerWon;
        this.UpdateState();
        window.alert("Чёрный игрок выиграл!");
    }

    static OnDraw() {
        gameState = GameState.Draw;
        this.UpdateState();
        window.alert("Ничья!");
    }

    static OnNewMove(move: Move) {
        moves.push(move);
        board.DoMove(move);
        boardControl.lastMove = move;
        boardControl.Reset(true);
        currPlayer = currPlayer == PlayerColor.White ? PlayerColor.Black : PlayerColor.White;
        this.UpdateState();

        var ul = document.getElementById("moves-list")!;
        var li = document.createElement("li");
        li.className = "list-group-item";
        li.innerText = move.ToHumanReadableString();
        ul.append(li);
    }

    static async DoMove(move: Move) {
        await fetch(`/Games/DoMove/${GAME_ID}`, {
            method: "POST",
            headers: { "Content-Type": "application/json" },
            body: JSON.stringify(move)
        });
    }
}