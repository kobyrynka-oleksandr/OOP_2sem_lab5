using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace OOP_2sem_lab5
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private List<Horse> horses = new List<Horse>();
        private Horse selectedHorse;
        private int selectedHorseIndex = 0;
        private double userBalance = 100;
        private double userBet = 10;
        private const double finishLine = 1000;
        private List<HorseRating> horseRatings;

        private DispatcherTimer raceTimer;
        private DateTime raceStartTime;


        private CancellationTokenSource cts;

        public MainWindow()
        {
            InitializeComponent();
            InitializeHorses();
            InitializeRatingTable();
            UpdateUI();
        }

        private void InitializeHorses()
        {
            horses = new List<Horse>
            {
                new Horse("Bolt", Colors.Red),
                new Horse("Storm", Colors.Blue),
                new Horse("Shadow", Colors.Green),
                new Horse("Blaze", Colors.Orange),
                new Horse("Thunder", Colors.Purple)
            };
            selectedHorse = horses[0];

            for (int i = 0; i < horses.Count; i++)
            {
                Ellipse horseEllipse = new Ellipse
                {
                    Width = 40,
                    Height = 30,
                    Fill = new SolidColorBrush(horses[i].Color)
                };

                Image horseImage = new Image
                {
                    Width = 60,
                    Height = 60,
                    Source = horses[i].GetCurrentFrame()
                };

                RaceCanvas.Children.Add(horseImage);
                horses[i].HorseImageControl = horseImage;

                Canvas.SetLeft(horseImage, 0);
                Canvas.SetTop(horseImage, i * 60 + 10);
            }
        }

        private void UpdateUI()
        {
            ChosenHorse.Text = $"Selected: {selectedHorse.Name}";
            Bet.Text = $"Bet: ${userBet}";
            CurrentBalance.Text = $"Balance: ${userBalance}";

        }

        private void ReduceBetButton_Click(object sender, RoutedEventArgs e)
        {
            if (userBet > 5)
            {
                userBet -= 5;
                UpdateUI();
            }
        }

        private void RaiseBetButton_Click(object sender, RoutedEventArgs e)
        {
            if (userBet + 5 <= userBalance)
            {
                userBet += 5;
                UpdateUI();
            }
        }

        private void PreviousHorseButton_Click(object sender, RoutedEventArgs e)
        {
            selectedHorseIndex = (selectedHorseIndex - 1 + horses.Count) % horses.Count;
            selectedHorse = horses[selectedHorseIndex];
            UpdateUI();
        }

        private void NextHorseButton_Click(object sender, RoutedEventArgs e)
        {
            selectedHorseIndex = (selectedHorseIndex + 1) % horses.Count;
            selectedHorse = horses[selectedHorseIndex];
            UpdateUI();
        }

        private void BetButton_Click(object sender, RoutedEventArgs e)
        {
            if (userBet > userBalance)
            {
                MessageBox.Show("Not enough money to place this bet.");
                return;
            }

            userBalance -= userBet;
            UpdateUI();
        }

        private async void StartRaceButton_Click(object sender, RoutedEventArgs e)
        {
            StartRace();
            cts?.Cancel();
            cts = new CancellationTokenSource();

            foreach (var horse in horses)
                horse.Reset();

            Stopwatch timer = Stopwatch.StartNew();

            var updateTask = UpdateRaceAsync(cts.Token);
            var raceTasks = horses.Select(h => h.RunAsync(finishLine, timer, cts.Token)).ToArray();

            try
            {
                await Task.WhenAll(raceTasks);
                cts.Cancel();
                timer.Stop();

                var sorted = horses.OrderBy(h => h.RaceTime).ToList();
                var winner = sorted.First();

                if (selectedHorse == winner)
                {
                    double prize = Math.Round(userBet * selectedHorse.Coefficient, 2);
                    userBalance += prize;
                    selectedHorse.MoneyWon = prize;
                    MessageBox.Show($"You won! Your horse {winner.Name} finished first.\nPrize: ${prize}");
                }
                else
                {
                    selectedHorse.MoneyWon = 0;
                    MessageBox.Show($"You lost. Winner: {winner.Name}");
                }

                UpdateUI();
            }
            catch (OperationCanceledException)
            {
                MessageBox.Show("Race was canceled.");
            }
        }
        private async Task UpdateRaceAsync(CancellationToken token)
        {
            double scrollSpeed = 2;
            double backgroundX = 0;

            while (!token.IsCancellationRequested)
            {
                for (int i = 0; i < horses.Count; i++)
                {
                    var horse = horses[i];
                    if (horse.HorseImageControl != null)
                    {
                        double progress = horse.Position / finishLine;
                        double x = progress * (RaceCanvas.ActualWidth - 100);
                        Canvas.SetLeft(horse.HorseImageControl, x);

                        horse.HorseImageControl.Source = horse.GetCurrentFrame();
                    }
                }

                backgroundX -= scrollSpeed;
                Canvas.SetLeft(TrackBackground, backgroundX);

                await Task.Delay(30, token);
            }
        }
        private void InitializeRatingTable()
        {
            var random = new Random();
            horseRatings = horses.Select(h => new HorseRating
            {
                Color = h.Color.ToString(),
                Name = h.Name,
                Position = 0,
                Time = "0.00 c",
                Coefficient = Math.Round(1 + random.NextDouble() * 4, 2),
                Money = "0$"
            }).ToList();

            RatingDataGrid.ItemsSource = horseRatings;
        }
        private void StartRace()
        {
            raceStartTime = DateTime.Now;

            raceTimer = new DispatcherTimer();
            raceTimer.Interval = TimeSpan.FromMilliseconds(100);
            raceTimer.Tick += RaceTimer_Tick;
            raceTimer.Start();
        }

        private void RaceTimer_Tick(object sender, EventArgs e)
        {
            var elapsed = DateTime.Now - raceStartTime;

            foreach (var rating in horseRatings)
            {
                rating.Time = elapsed.TotalSeconds.ToString("0.00") + " c";
            }
        }
        private void SwitchCameraButton_Click(object sender, RoutedEventArgs e)
        {

        }
    }
}
