using MoviePLayer.Movie;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using TMDbLib.Client;
using System.Text.RegularExpressions;
using System.Diagnostics;

namespace MoviePlayer;

public partial class MainWindow : Window
{
    private string apiKey = "273e7df007feb07917cb631c4eca5f02";
    private List<Movie> allMovies = new List<Movie>();

    public MainWindow()
    {
        InitializeComponent();
        LoadMovies();
    }

    public async void LoadMovies()
    {
        var localFiles = ScanFolder(@"G:\kino\movies");
        TMDbClient client = new TMDbClient(apiKey);

        foreach (var movie in localFiles)
        {
            try
            {
                var results = await client.SearchMovieAsync(movie.Title);
                var best = results.Results.FirstOrDefault();
                if (best != null)
                {
                    // Get full movie details to find the Runtime
                    var fullDetails = await client.GetMovieAsync(best.Id);

                    movie.Description = fullDetails.Overview ?? "No description available.";
                    movie.PosterPath = $"https://image.tmdb.org/t/p/w500{fullDetails.PosterPath}";

                    // Format the runtime (e.g., "124 min")
                    movie.Runtime = fullDetails.Runtime > 0 ? $"{fullDetails.Runtime} min" : "N/A";
                }
            }
            catch { }
        }

        allMovies = localFiles;
        MovieDisplayGrid.ItemsSource = allMovies;

        // --- NEW LOGIC FOR YEAR FILTER ---
        // 1. Get all unique years from the scanned movies
        // 2. Filter out "Unknown" and sort them from newest to oldest
        var years = allMovies
            .Select(m => m.Year)
            .Where(y => y != "Unknown")
            .Distinct()
            .OrderByDescending(y => y)
            .ToList();

        // 3. Add "All Years" and "Unknown" to the top of the list
        years.Insert(0, "All Years");
        if (allMovies.Any(m => m.Year == "Unknown"))
        {
            years.Add("Unknown");
        }

        // 4. Bind the list to your ComboBox
        YearFilter.ItemsSource = years;
        YearFilter.SelectedIndex = 0; // Default to "All Years"
    }

    public List<Movie> ScanFolder(string folderPath)
    {
        var found = new List<Movie>();
        if (!Directory.Exists(folderPath)) return found;

        string[] extensions = { ".mp4", ".mkv", ".avi" };
        var files = Directory.GetFiles(folderPath, "*.*", SearchOption.AllDirectories)
                             .Where(f => extensions.Contains(Path.GetExtension(f).ToLower()));

        foreach (string file in files)
        {
            string raw = Path.GetFileNameWithoutExtension(file);
            string clean = raw.Replace(".", " ").Replace("_", " ");
            string movieYear = "Unknown"; // Default value

            // 1. Find all 4-digit years
            var matches = Regex.Matches(clean, @"\b(19|20)\d{2}\b");

            if (matches.Count > 0)
            {
                // The RELEASE YEAR is almost always the last 4-digit number in the file
                var lastMatch = matches[matches.Count - 1];
                movieYear = lastMatch.Value;

                // Logic to clean title while keeping "2049" if it's not the release year
                if (matches.Count > 1)
                {
                    // If there are TWO years, cut at the second one (the release year)
                    int secondYearIndex = matches[1].Index;
                    clean = clean.Substring(0, secondYearIndex);
                }
                else if (matches.Count == 1)
                {
                    int yearIndex = matches[0].Index;
                    // Only cut if the year is in brackets or followed by 'junk' tags
                    if (raw.Contains("(" + movieYear + ")") || clean.Length > yearIndex + 6)
                    {
                        clean = clean.Substring(0, yearIndex);
                    }
                }
            }

            found.Add(new Movie
            {
                Title = clean.Trim(),
                Year = movieYear, // THIS IS KEY: Storing the year for the filter
                FilePath = file,
                PosterPath = "https://via.placeholder.com/500x750?text=Loading..."
            });
        }
        return found;
    }
    // Single Click: Updates Sidebar
    private void MovieCard_Click(object sender, MouseButtonEventArgs e)
    {
        if (sender is Border b && b.DataContext is Movie m)
        {
            SelectedTitle.Text = m.Title;
            SelectedDescription.Text = m.Description;
            SelectedYearLabel.Text = m.Year;
            SelectedRuntimeLabel.Text = m.Runtime; // Add this line

            try { SelectedPoster.Source = new BitmapImage(new Uri(m.PosterPath)); } catch { }
        }
    }

    // Double Click: Plays Video
    private void MovieBorder_MouseDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ClickCount == 2 && sender is FrameworkElement el && el.DataContext is Movie movie)
        {
            try { Process.Start(new ProcessStartInfo { FileName = movie.FilePath, UseShellExecute = true }); } catch { }
        }
    }

    // Search Logic
    private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        string query = SearchBox.Text.ToLower();
        var filtered = allMovies.Where(m => m.Title.ToLower().Contains(query)).ToList();
        MovieDisplayGrid.ItemsSource = filtered;
    }
    private void FilterChanged(object sender, EventArgs e)
    {
        if (allMovies == null) return;

        string searchText = SearchBox.Text.ToLower();
        string selectedYear = YearFilter.SelectedItem?.ToString();

        var filtered = allMovies.Where(m =>
            m.Title.ToLower().Contains(searchText) &&
            (selectedYear == "All Years" || m.Year == selectedYear)
        ).ToList();

        MovieDisplayGrid.ItemsSource = filtered;
    }
}