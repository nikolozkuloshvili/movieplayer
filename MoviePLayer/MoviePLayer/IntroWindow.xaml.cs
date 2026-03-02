using System;
using System.IO;
using System.Windows;
using System.Windows.Input;

namespace MoviePlayer
{
    public partial class IntroWindow : Window
    {
        public IntroWindow()
        {
            InitializeComponent();

            string introPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", "Intro", "intro.mp4");

            if (File.Exists(introPath))
            {
                IntroPlayer.Source = new Uri(introPath);
            }
            else
            {
                FinishIntro();
            }
        }

        private void IntroPlayer_MediaEnded(object sender, RoutedEventArgs e)
        {
            FinishIntro();
        }

        // THIS IS THE MISSING PIECE THAT FIXES THE CS1061 ERROR
        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            FinishIntro();
        }

        private void FinishIntro()
        {
            MainWindow main = new MainWindow();
            main.Show();
            this.Close();
        }
    }
}