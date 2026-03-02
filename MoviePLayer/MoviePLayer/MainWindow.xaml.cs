using MoviePLayer.Movie; // Never removed as requested
using Microsoft.WindowsAPICodePack.Dialogs;
using Newtonsoft.Json;
using System.Diagnostics;
using System.IO;
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
        private MyMovie? currentlySelectedMovie;

        private string settingsFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "settings.txt");
        private string libraryFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "library.json");

        public MainWindow()
        {
            InitializeComponent();
            LoadInitialData();
        }

        private void LoadInitialData()
        {
            string folderToScan = @"G:\kino\movies";
            if (File.Exists(settingsFile)) folderToScan = File.ReadAllText(settingsFile);

            if (File.Exists(libraryFile))
            {
                try
                {
                    string json = File.ReadAllText(libraryFile);
                    allMovies = JsonConvert.DeserializeObject<List<MyMovie>>(json) ?? new List<MyMovie>();
                    FilterChanged(null, null);
                    UpdateContinueWatching();
                    UpdateYearFilter();
                }
                catch { if (Directory.Exists(folderToScan)) LoadMovies(folderToScan); }
            }
            else if (Directory.Exists(folderToScan)) LoadMovies(folderToScan);
        }

        public async void LoadMovies(string folderPath)
        {
            var localFiles = ScanFolder(folderPath);
            TMDbClient client = new TMDbClient(apiKey);

            foreach (var newM in localFiles)
            {
                var old = allMovies.FirstOrDefault(m => m.FilePath == newM.FilePath);
                if (old != null) { newM.IsWatched = old.IsWatched; newM.LastPlayed = old.LastPlayed; }
            }

            allMovies = localFiles;
            FilterChanged(null, null);

            foreach (var movie in allMovies)
            {
                try
                {
                    var results = await client.SearchMovieAsync(movie.Title);
                    var best = results?.Results?.FirstOrDefault();
                    if (best != null)
                    {
                        var fullDetails = await client.GetMovieAsync(best.Id, MovieMethods.ExternalIds | MovieMethods.Credits);
                        if (fullDetails != null)
                        {
                            movie.Description = fullDetails.Overview ?? "";
                            movie.PosterPath = $"https://image.tmdb.org/t/p/w500{fullDetails.PosterPath}";
                            movie.Runtime = fullDetails.Runtime > 0 ? $"{fullDetails.Runtime} min" : "N/A";
                            movie.ImdbId = fullDetails.ExternalIds?.ImdbId;
                            movie.ImdbRating = $"IMDb: {fullDetails.VoteAverage:F1}/10";
                            movie.LetterboxdRating = $"★ {fullDetails.VoteAverage / 2.0:F1}";
                            movie.Director = fullDetails.Credits?.Crew?.FirstOrDefault(c => c.Job == "Director")?.Name ?? "Unknown";

                            Dispatcher.Invoke(() => {
                                MovieDisplayGrid.Items.Refresh();
                                if (currentlySelectedMovie == movie) SelectMovie(movie);
                            });
                        }
                    }
                }
                catch { }
            }
            SaveLibrary();
            UpdateYearFilter();
        }

        public List<MyMovie> ScanFolder(string folderPath)
        {
            var found = new List<MyMovie>();
            if (!Directory.Exists(folderPath)) return found;

            var files = Directory.GetFiles(folderPath, "*.*", SearchOption.AllDirectories)
                                 .Where(f => new[] { ".mp4", ".mkv", ".avi" }.Contains(Path.GetExtension(f).ToLower()));

            foreach (string file in files)
            {
                string raw = Path.GetFileNameWithoutExtension(file);
                string clean = raw.Replace(".", " ").Replace("_", " ");
                string movieYear = "Unknown";

                // RESTORED: Uses LAST match logic to avoid breaking Blade Runner 2049
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

        private void SelectMovie(MyMovie m)
        {
            currentlySelectedMovie = m;
            SelectedTitle.Text = m.Title;
            SelectedDescription.Text = m.Description;
            SelectedYearLabel.Text = m.Year;
            SelectedRuntimeLabel.Text = m.Runtime;
            SelectedImdbRating.Text = m.ImdbRating;
            SelectedLetterboxdRating.Text = m.LetterboxdRating;
            SelectedDirectorLabel.Text = (!string.IsNullOrEmpty(m.Director) && m.Director != "Unknown") ? m.Director.ToUpper() : "UNKNOWN DIRECTOR";

            ImdbButton.Tag = m.ImdbId;
            LetterboxdButton.Tag = m.ImdbId;
            try { SelectedPoster.Source = new BitmapImage(new Uri(m.PosterPath)); } catch { }
        }

        private void DirectorLink_Click(object sender, RoutedEventArgs e)
        {
            if (currentlySelectedMovie != null && !string.IsNullOrEmpty(currentlySelectedMovie.Director) && currentlySelectedMovie.Director != "Unknown")
            {
                string name = currentlySelectedMovie.Director.ToLower();
                string cleanName = Regex.Replace(name, @"[^a-z0-9\s-]", "");
                string slug = Regex.Replace(cleanName.Trim(), @"\s+", "-");
                string url = $"https://letterboxd.com/director/{slug}/";
                Process.Start(new ProcessStartInfo { FileName = url, UseShellExecute = true });
            }
        }

        private void FilterChanged(object? sender, EventArgs? e)
        {
            if (allMovies == null || MovieDisplayGrid == null) return;
            string search = SearchBox.Text.ToLower();
            string year = YearFilter.SelectedItem?.ToString() ?? "All Years";
            var filtered = allMovies.Where(m =>
                (m.Title.ToLower().Contains(search) || (m.Director != null && m.Director.ToLower().Contains(search))) &&
                (year == "All Years" || m.Year == year)).ToList();

            if (RadioUnwatched != null && RadioUnwatched.IsChecked == true) filtered = filtered.Where(m => !m.IsWatched).ToList();
            else if (RadioWatched != null && RadioWatched.IsChecked == true) filtered = filtered.Where(m => m.IsWatched).ToList();

            MovieDisplayGrid.ItemsSource = filtered;
        }

        private void WatchCheckBox_Click(object sender, RoutedEventArgs e)
        {
            if (sender is CheckBox cb && cb.DataContext is MyMovie movie)
            {
                movie.IsWatched = cb.IsChecked ?? false;
                SaveLibrary();
                FilterChanged(null, null);
            }
        }

        private void BtnRefresh_Click(object sender, RoutedEventArgs e) => LoadMovies(File.ReadAllText(settingsFile));

        private void BtnRandomMovie_Click(object sender, RoutedEventArgs e)
        {
            var unwatched = allMovies.Where(m => !m.IsWatched).ToList();
            if (unwatched.Any()) SelectMovie(unwatched[new Random().Next(unwatched.Count)]);
        }

        private void BtnChangeFolder_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new CommonOpenFileDialog { IsFolderPicker = true };
            if (dialog.ShowDialog() == CommonFileDialogResult.Ok) { File.WriteAllText(settingsFile, dialog.FileName); LoadMovies(dialog.FileName); }
        }

        private void MovieCard_Click(object sender, MouseButtonEventArgs e) { if (sender is FrameworkElement el && el.DataContext is MyMovie m) SelectMovie(m); }
        private void MovieCard_MouseDown(object sender, MouseButtonEventArgs e) { if (e.ClickCount == 2 && sender is FrameworkElement el && el.DataContext is MyMovie movie) LaunchMovieFile(movie); }
        private void PlayButton_Click(object sender, RoutedEventArgs e) { if (currentlySelectedMovie != null) LaunchMovieFile(currentlySelectedMovie); }
        private void LaunchMovieFile(MyMovie movie) { try { Process.Start(new ProcessStartInfo { FileName = movie.FilePath, UseShellExecute = true }); movie.LastPlayed = DateTime.Now; SaveLibrary(); UpdateContinueWatching(); } catch (Exception ex) { MessageBox.Show(ex.Message); } }

        private void SaveLibrary() => File.WriteAllText(libraryFile, JsonConvert.SerializeObject(allMovies, Formatting.Indented));
        private void UpdateYearFilter() { var years = allMovies.Select(m => m.Year).Where(y => y != "Unknown").Distinct().OrderByDescending(y => y).ToList(); years.Insert(0, "All Years"); YearFilter.ItemsSource = years; YearFilter.SelectedIndex = 0; }
        private void UpdateContinueWatching() { var recent = allMovies.Where(m => m.LastPlayed != null).OrderByDescending(m => m.LastPlayed).Take(6).ToList(); if (recent.Any()) { ContinueWatchingGrid.ItemsSource = recent; ContinueWatchingSection.Visibility = Visibility.Visible; } else { ContinueWatchingSection.Visibility = Visibility.Collapsed; } }
        private void ImdbButton_Click(object sender, RoutedEventArgs e) { if (ImdbButton.Tag is string id) Process.Start(new ProcessStartInfo($"https://www.imdb.com/title/{id}") { UseShellExecute = true }); }
        private void LetterboxdButton_Click(object sender, RoutedEventArgs e) { if (LetterboxdButton.Tag is string id) Process.Start(new ProcessStartInfo($"https://letterboxd.com/imdb/{id}") { UseShellExecute = true }); }
    }
}