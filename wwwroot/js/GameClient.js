"use strict";
var __awaiter = (this && this.__awaiter) || function (thisArg, _arguments, P, generator) {
    function adopt(value) { return value instanceof P ? value : new P(function (resolve) { resolve(value); }); }
    return new (P || (P = Promise))(function (resolve, reject) {
        function fulfilled(value) { try { step(generator.next(value)); } catch (e) { reject(e); } }
        function rejected(value) { try { step(generator["throw"](value)); } catch (e) { reject(e); } }
        function step(result) { result.done ? resolve(result.value) : adopt(result.value).then(fulfilled, rejected); }
        step((generator = generator.apply(thisArg, _arguments || [])).next());
    });
};
var GameState;
(function (GameState) {
    GameState[GameState["NotStarted"] = 0] = "NotStarted";
    GameState[GameState["Running"] = 1] = "Running";
    GameState[GameState["WhitePlayerWon"] = 2] = "WhitePlayerWon";
    GameState[GameState["BlackPlayerWon"] = 3] = "BlackPlayerWon";
    GameState[GameState["Draw"] = 4] = "Draw";
})(GameState || (GameState = {}));
;
class GameClient {
    static Initialize() {
        return __awaiter(this, void 0, void 0, function* () {
            for (let move of moves) {
                board.DoMove(move);
                boardControl.lastMove = move;
                currPlayer = move.Player;
            }
            boardControl.Reset(true);
            currPlayer = currPlayer == PlayerColor.White ? PlayerColor.Black : PlayerColor.White;
            connection.on("newMove", (moveStr) => {
                this.OnNewMove(Move.Clone(JSON.parse(moveStr)));
            });
            connection.on("whitePlayerWon", () => { this.OnWhitePlayerWon(); });
            connection.on("blackPlayerWon", () => { this.OnBlackPlayerWon(); });
            connection.on("draw", () => { this.OnDraw(); });
            yield connection.start();
            connection.invoke("AddToGroup", GAME_ID);
        });
    }
    static UpdateState() {
        var statusText = document.getElementById("status-text");
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
    static OnNewMove(move) {
        moves.push(move);
        board.DoMove(move);
        boardControl.lastMove = move;
        boardControl.Reset(true);
        currPlayer = currPlayer == PlayerColor.White ? PlayerColor.Black : PlayerColor.White;
        this.UpdateState();
        var ul = document.getElementById("moves-list");
        var li = document.createElement("li");
        li.className = "list-group-item";
        li.innerText = move.ToHumanReadableString();
        ul.append(li);
    }
    static DoMove(move) {
        return __awaiter(this, void 0, void 0, function* () {
            yield fetch(`/Games/DoMove/${GAME_ID}`, {
                method: "POST",
                headers: { "Content-Type": "application/json" },
                body: JSON.stringify(move)
            });
        });
    }
}
