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
using MoviePLayer.Movie;

namespace MoviePlayer;

public partial class MainWindow : Window
{
    private string apiKey = "273e7df007feb07917cb631c4eca5f02";

    public MainWindow()
    {
        InitializeComponent();
        LoadMovies();
    }

    // Logic for updating the Sidebar
    private void MovieCard_Click(object sender, MouseButtonEventArgs e)
    {
        var border = sender as Border;
        if (border?.DataContext is Movie clickedMovie)
        {
            SelectedTitle.Text = clickedMovie.Title;
            SelectedDescription.Text = clickedMovie.Description;

            if (!string.IsNullOrEmpty(clickedMovie.PosterPath))
            {
                try
                {
                    SelectedPoster.Source = new BitmapImage(new Uri(clickedMovie.PosterPath));
                }
                catch { /* Ignore if URL is broken */ }
            }
        }
    }

    // Logic for Double-Click Play
    private void MovieBorder_MouseDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ClickCount == 2)
        {
            var element = sender as FrameworkElement;
            if (element?.DataContext is Movie clickedMovie)
            {
                try
                {
                    Process.Start(new ProcessStartInfo { FileName = clickedMovie.FilePath, UseShellExecute = true });
                }
                catch { MessageBox.Show("File not found or no player installed."); }
            }
        }
    }

    // Existing scan/load methods here...
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
                    movie.Description = best.Overview ?? "No description available.";
                    movie.PosterPath = $"https://image.tmdb.org/t/p/w500{best.PosterPath}";
                }
            }
            catch { }
        }
        MovieDisplayGrid.ItemsSource = localFiles;
    }

    public List<Movie> ScanFolder(string folderPath)
    {
        var found = new List<Movie>();
        if (!Directory.Exists(folderPath)) return found;
        var files = Directory.GetFiles(folderPath, "*.*", SearchOption.AllDirectories)
                             .Where(f => f.EndsWith(".mp4") || f.EndsWith(".mkv") || f.EndsWith(".avi"));
        foreach (var file in files)
        {
            string raw = Path.GetFileNameWithoutExtension(file);
            string clean = Regex.Replace(raw.Replace(".", " "), @"\d{4}.*", "").Trim();
            found.Add(new Movie { Title = clean == "" ? raw : clean, FilePath = file, PosterPath = "https://via.placeholder.com/500x750" });
        }
        return found;
    }
}