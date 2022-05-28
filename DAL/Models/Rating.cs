using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;

namespace CheckersWeb.DAL.Models;

public class Rating
{
    [Display(Name = "Игрок")]
    [DisplayFormat(NullDisplayText = "—")]
    public IdentityUser? Player { get; set; }

    [Display(Name = "Победы")]
    public int Wins { get; set; }

    [Display(Name = "Поражения")]
    public int Losses { get; set; }

    [Display(Name = "Ничьи")]
    public int Draws { get; set; }

    [Display(Name = "Счёт")]
    public int Score { get; set; }
}