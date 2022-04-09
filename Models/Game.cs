using System.ComponentModel.DataAnnotations;
using CheckersWeb.Lib;
using Microsoft.AspNetCore.Identity;

namespace CheckersWeb.Models;

public enum GameState
{
    [Display(Name = "Не начата")]
    NotStarted,
    [Display(Name = "Идёт")]
    Running,
    [Display(Name = "Белый игрок выиграл")]
    WhitePlayerWon,
    [Display(Name = "Чёрный игрок выиграл")]
    BlackPlayerWon,
    [Display(Name = "Ничья")]
    Draw
}

public class Game
{
    [Display(Name = "№")]
    public int Id { get; set; }

    [Display(Name = "Белый игрок")]
    [DisplayFormat(NullDisplayText = "—")]
    public IdentityUser? WhitePlayer { get; set; }

    [Display(Name = "Чёрный игрок")]
    [DisplayFormat(NullDisplayText = "—")]
    public IdentityUser? BlackPlayer { get; set; }

    [Display(Name = "Статус")]
    public GameState State { get; set; }

    public List<Move>? Moves { get; set; }
}