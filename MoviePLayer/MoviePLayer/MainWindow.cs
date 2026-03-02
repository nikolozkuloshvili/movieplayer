using System.Windows; // Make sure this is present

namespace MoviePlayer // Remove the semicolon here
{
    public partial class MainWindow : Window
    {
        // ... rest of your code remains unchanged ...
        public MainWindow()
        {
            InitializeComponent(); // This will now be recognized
            LoadMovies();
        }
        // ... rest of your code remains unchanged ...
    }
}