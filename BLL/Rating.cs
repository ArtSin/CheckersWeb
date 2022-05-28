namespace CheckersWeb.BLL;

public class Rating
{
    // Составление рейтинга из побед, поражений и ничьих
    public List<DAL.Models.Rating> GetRating(List<DAL.Rating.PlayerWins> wins,
        List<DAL.Rating.PlayerLosses> losses, List<DAL.Rating.PlayerDraws> draws)
    {
        var lossesDraws = losses.Join(draws, x => x.Player, y => y.Player, (x, y) => new { Player = x.Player, x.Losses, y.Draws });
        return wins.Join(lossesDraws, x => x.Player, y => y.Player, (x, y) =>
                new DAL.Models.Rating { Player = x.Player, Wins = x.Wins, Losses = y.Losses, Draws = y.Draws, Score = x.Wins - y.Losses })
            .OrderByDescending(x => x.Score)
            .ToList();
    }
}