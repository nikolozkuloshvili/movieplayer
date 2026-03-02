using MoviePLayer.Movie; // Never removed as requested
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using TMDbLib.Client;
using TMDbLib.Objects.Movies;

namespace MoviePlayer
{
    using MyMovie = MoviePLayer.Movie.Movie;

    public partial class MainWindow : Window
    {
        private string apiKey = "273e7df007feb07917cb631c4eca5f02";
        private List<MyMovie> allMovies = new List<MyMovie>();
        private MyMovie currentlySelectedMovie;

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
                    var best = results?.Results?.FirstOrDefault();

                    if (best != null)
                    {
                        var fullDetails = await client.GetMovieAsync(best.Id, MovieMethods.ExternalIds);
                        movie.Description = fullDetails.Overview ?? "No description available.";
                        movie.PosterPath = $"https://image.tmdb.org/t/p/w500{fullDetails.PosterPath}";
                        movie.Runtime = fullDetails.Runtime > 0 ? $"{fullDetails.Runtime} min" : "N/A";
                        movie.ImdbId = fullDetails.ExternalIds?.ImdbId;
                        movie.ImdbRating = fullDetails.VoteAverage > 0 ? $"IMDb: {fullDetails.VoteAverage:F1}/10" : "IMDb: —";
                    }
                }
                catch { }
            }

            allMovies = localFiles;
            MovieDisplayGrid.ItemsSource = allMovies;

            var years = allMovies.Select(m => m.Year).Where(y => y != "Unknown").Distinct().OrderByDescending(y => y).ToList();
            years.Insert(0, "All Years");
            YearFilter.ItemsSource = years;
            YearFilter.SelectedIndex = 0;
        }

        public List<MyMovie> ScanFolder(string folderPath)
        {
            var found = new List<MyMovie>();
            if (!Directory.Exists(folderPath)) return found;

            string[] extensions = { ".mp4", ".mkv", ".avi" };
            var files = Directory.GetFiles(folderPath, "*.*", SearchOption.AllDirectories)
                                 .Where(f => extensions.Contains(Path.GetExtension(f).ToLower()));

            foreach (string file in files)
            {
                string raw = Path.GetFileNameWithoutExtension(file);
                string clean = raw.Replace(".", " ").Replace("_", " ");
                string movieYear = "Unknown";
                var matches = Regex.Matches(clean, @"\b(19|20)\d{2}\b");

                if (matches.Count > 0)
                {
                    var lastMatch = matches[matches.Count - 1];
                    movieYear = lastMatch.Value;
                    clean = clean.Substring(0, lastMatch.Index);
                }

                found.Add(new MyMovie { Title = clean.Trim(), Year = movieYear, FilePath = file, PosterPath = "https://via.placeholder.com/160x240" });
            }
            return found;
        }

        private void MovieCard_Click(object sender, MouseButtonEventArgs e)
        {
            if (sender is FrameworkElement el && el.DataContext is MyMovie m)
            {
                currentlySelectedMovie = m;
                SelectedTitle.Text = m.Title;
                SelectedDescription.Text = m.Description;
                SelectedYearLabel.Text = m.Year;
                SelectedRuntimeLabel.Text = m.Runtime;
                SelectedImdbRating.Text = m.ImdbRating;
                ImdbButton.Tag = m.ImdbId;
                LetterboxdButton.Tag = m.ImdbId;
                try { SelectedPoster.Source = new BitmapImage(new Uri(m.PosterPath)); } catch { }
            }
        }

        private void MovieCard_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount == 2 && sender is FrameworkElement el && el.DataContext is MyMovie movie)
            {
                LaunchMovieFile(movie);
            }
        }

        private void PlayButton_Click(object sender, RoutedEventArgs e)
        {
            if (currentlySelectedMovie != null)
            {
                LaunchMovieFile(currentlySelectedMovie);
            }
            else
            {
                MessageBox.Show("Please select a movie first!");
            }
        }

        private void LaunchMovieFile(MyMovie movie)
        {
            try
            {
                Process.Start(new ProcessStartInfo { FileName = movie.FilePath, UseShellExecute = true });

                // Track the time played
                movie.LastPlayed = DateTime.Now;
                UpdateContinueWatching();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Could not start movie: {ex.Message}");
            }
        }

        private void UpdateContinueWatching()
        {
            // Get the 6 most recently played movies
            var recent = allMovies
                .Where(m => m.LastPlayed != null)
                .OrderByDescending(m => m.LastPlayed)
                .Take(6)
                .ToList();

            if (recent.Any())
            {
                ContinueWatchingGrid.ItemsSource = null; // Reset to force refresh
                ContinueWatchingGrid.ItemsSource = recent;
                ContinueWatchingSection.Visibility = Visibility.Visible;
            }
        }

        private void FilterChanged(object? sender, EventArgs e)
        {
            if (allMovies == null) return;
            string searchText = SearchBox.Text.ToLower();
            string? selectedYear = YearFilter.SelectedItem?.ToString();

            var filtered = allMovies.Where(m =>
                m.Title.ToLower().Contains(searchText) &&
                (selectedYear == "All Years" || m.Year == selectedYear)
            ).ToList();

            MovieDisplayGrid.ItemsSource = filtered;
        }

        private void ImdbButton_Click(object? sender, RoutedEventArgs e)
        {
            if (ImdbButton.Tag is string id) Process.Start(new ProcessStartInfo($"https://www.imdb.com/title/{id}") { UseShellExecute = true });
        }

        private void LetterboxdButton_Click(object? sender, RoutedEventArgs e)
        {
            if (LetterboxdButton.Tag is string id) Process.Start(new ProcessStartInfo($"https://letterboxd.com/imdb/{id}") { UseShellExecute = true });
        }
    }
}