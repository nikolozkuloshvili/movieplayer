namespace MoviePLayer.Movie;

public class Movie
{
    public string Title { get; set; } = "";
    public string Description { get; set; } = "";
    public string PosterPath { get; set; } = "";
    public string FilePath { get; set; } = "";
    public string? Year { get; set; }
    public string? Runtime { get; set; }
    public string? ImdbRating { get; set; }
    public string? LetterboxdRating { get; set; }
    public string? ImdbId { get; set; }
    public DateTime? LastPlayed { get; set; }
    public bool IsWatched { get; set; }
    public string? Director { get; set; }
}
