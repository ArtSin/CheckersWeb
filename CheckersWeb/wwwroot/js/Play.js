"use strict";
const connection = new signalR.HubConnectionBuilder()
    .withUrl("/GameHub")
    .withAutomaticReconnect()
    .build();
let board = new Board(false);
let moves = JSON.parse(START_MOVES).map((x) => Move.Clone(x));
let thisPlayer = THIS_PLAYER;
let currPlayer = PlayerColor.Black;
let gameState = GAME_STATE;
const boardControl = new BoardControl();
GameClient.Initialize();
function initializeCanvas() {
    GameClient.UpdateState();
    const canvas = document.getElementById("canvas");
    boardControl.Canvas = canvas;
    function resizeCanvas() {
        canvas.width = canvas.clientWidth;
        canvas.height = canvas.clientHeight;
        boardControl.Paint();
    }
    window.addEventListener("resize", resizeCanvas, false);
    if (!VIEW_ONLY) {
        canvas.addEventListener("click", (e) => boardControl.OnClick(e, canvas.getBoundingClientRect()));
    }
    resizeCanvas();
}
if (document.readyState == "complete") {
    initializeCanvas();
}
else {
    window.addEventListener("load", initializeCanvas);
}
