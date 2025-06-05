using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
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
    public partial class MainWindow : Window
    {
        private List<Horse> horses = new List<Horse>();
        private Dictionary<Horse, double> placedBets = new Dictionary<Horse, double>();
        private double userBalance = 250;
        private double userBet = 10;
        private const double finishLine = 1000;
        private List<HorseRating> horseRatings;
        private Horse selectedHorse;
        private int selectedHorseIndex = 0;
        private int currentHorseCount = 5;

        private int cameraFocusIndex = 0;

        private DispatcherTimer raceTimer;
        private DateTime raceStartTime;

        private CancellationTokenSource cts;

        public MainWindow()
        {
            InitializeComponent();
            InitializeHorses();
            selectedHorse = horses.FirstOrDefault();
            cameraFocusIndex = 0;
            CameraOnHorse.Text = horses[cameraFocusIndex].Name.ToString();
            InitializeRatingTable();
            UpdateUI();
        }

        private void InitializeHorses()
        {
            int horseCount = int.Parse(((ComboBoxItem)HorseCountComboBox.SelectedItem).Content.ToString());
            var allHorses = new List<Horse>
            {
                new Horse("Bolt", Colors.Red, horseCount / 1.5),
                new Horse("Storm", Colors.Blue, horseCount / 1.5),
                new Horse("Shadow", Colors.Green, horseCount / 1.5),
                new Horse("Blaze", Colors.Orange, horseCount / 1.5),
                new Horse("Thunder", Colors.Purple, horseCount / 1.5)
            };
            for (int i = 0; i < horseCount; i++)
            {
                horses.Add(allHorses[i]);
            }

            for (int i = 0; i < horses.Count; i++)
            {
                Image horseImage = new Image
                {
                    Width = 60,
                    Height = 60,
                    Source = horses[i].GetCurrentFrame()
                };

                RaceCanvas.Children.Add(horseImage);
                horses[i].HorseImageControl = horseImage;

                Canvas.SetLeft(horseImage, 0);
                Canvas.SetTop(horseImage, i * 35 + 80);
            }
        }

        private void UpdateUI()
        {
            ChosenHorse.Text = $"Selected: {selectedHorse.Name}";
            Bet.Text = $"Bet: ${userBet}";
            CurrentBalance.Text = $"Balance: ${Math.Round(userBalance, 1)}";
        }

        private void ReduceBetButton_Click(object sender, RoutedEventArgs e)
        {
            if (userBet > 10)
            {
                userBet -= 10;
                UpdateUI();
            }
        }

        private void RaiseBetButton_Click(object sender, RoutedEventArgs e)
        {
            if (userBet + 10 <= userBalance)
            {
                userBet += 10;
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
            if (placedBets.ContainsKey(selectedHorse))
            {
                placedBets[selectedHorse] += userBet;
            }
            else
            {
                placedBets.Add(selectedHorse, userBet);
            }

            if (userBet > userBalance)
            {
                MessageBox.Show("Not enough money to place this bet.");
                return;
            }

            userBalance -= userBet;

            UpdateHorseRatingTable();
            UpdateUI();
        }

        private async void StartRaceButton_Click(object sender, RoutedEventArgs e)
        {
            StartRaceButton.IsEnabled = false;
            BetButton.IsEnabled = false;
            HorseCountComboBox.IsEnabled = false;

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
                raceTimer.Stop();

                var sorted = horses.OrderBy(h => h.RaceTime).ToList();
                var winner = sorted.First();

                double payout = 0;
                if (placedBets.TryGetValue(winner, out double betOnWinner))
                {
                    payout = Math.Round(betOnWinner * winner.Coefficient, 2);
                    userBalance += payout;
                }

                MessageBox.Show($"Winner: {winner.Name}\nYou won: ${payout}");

                placedBets.Clear();

                foreach (var horse in horses)
                {
                    FindNewCoefficient(horse);
                }

                UpdateHorseRatingTable();
                UpdateUI();

                StartRaceButton.IsEnabled = true;
                BetButton.IsEnabled = true;
                HorseCountComboBox.IsEnabled = true;
            }
            catch (OperationCanceledException)
            {
                MessageBox.Show("Race was canceled.");
            }
        }
        private async Task UpdateRaceAsync(CancellationToken token)
        {
            double scrollSpeed = 0.5;
            double backgroundX = 0;
            bool backgroundFrozen = false;

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

                var focusedHorse = horses[cameraFocusIndex];
                if (focusedHorse != null)
                {
                    double progressOfFocusedHorse = Math.Min(focusedHorse.Position / finishLine * 100, 100);
                    RaceProgressBar.Value = progressOfFocusedHorse;
                }

                if (!backgroundFrozen && horses.Any(h => h.Position >= finishLine))
                {
                    backgroundFrozen = true;
                }

                if (!backgroundFrozen)
                {
                    backgroundX -= scrollSpeed;

                    double maxShift = TrackBackground.Width - RaceCanvas.ActualWidth;
                    if (backgroundX < -maxShift)
                        backgroundX = -maxShift;

                    Canvas.SetLeft(TrackBackground, backgroundX);
                }

                await Task.Delay(30, token);
            }
        }
        private void InitializeRatingTable()
        {
            var random = new Random();
            horseRatings = horses.Select((h, index) => new HorseRating
            {
                Color = GetColorName(h.Color),
                Name = h.Name,
                Position = index + 1,
                Time = "0.00 c",
                Coefficient = h.Coefficient,
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
            UpdateHorseRatingTable();
        }
        private void SwitchCameraButton_Click(object sender, RoutedEventArgs e)
        {
            if (horses.Count == 0) return;

            cameraFocusIndex = (cameraFocusIndex + 1) % horses.Count;

            CameraOnHorse.Text = horses[cameraFocusIndex].Name.ToString();
        }
        private void UpdateHorseRatingTable()
        {
            int maxFixedPosition = horses.Where(h => h.FixedPosition.HasValue)
                                         .Select(h => h.FixedPosition.Value)
                                         .DefaultIfEmpty(0)
                                         .Max();

            var runningHorses = horses.Where(h => !h.FixedPosition.HasValue)
                                      .OrderByDescending(h => h.Position)
                                      .ToList();

            foreach (var horse in runningHorses)
            {
                if (horse.HasFinished && !horse.FixedPosition.HasValue)
                {
                    horse.FixedPosition = ++maxFixedPosition;
                }
            }

            foreach (var rating in horseRatings)
            {
                var horse = horses.FirstOrDefault(h => h.Name == rating.Name);
                if (horse != null)
                {
                    if (horse.FixedPosition.HasValue)
                    {
                        rating.Position = horse.FixedPosition.Value;
                    }
                    else
                    {
                        rating.Position = horses
                            .Where(h => !h.HasFinished)
                            .OrderByDescending(h => h.Position)
                            .ToList()
                            .FindIndex(h => h.Name == horse.Name) + maxFixedPosition + 1;
                    }

                    if (!horse.HasFinished)
                        rating.Time = horse.RaceTime.TotalSeconds.ToString("0.00") + " c";
                    else
                        rating.Time = horse.FinishTime.ToString("0.00") + " c";

                    rating.Money = placedBets.ContainsKey(horse)
                        ? $"{placedBets[horse]}$"
                        : "0$";

                    rating.Coefficient = Math.Round(horse.Coefficient, 2);
                }
            }

            RatingDataGrid.Items.Refresh();
            if (horses.All(h => h.HasFinished) && raceTimer?.IsEnabled == true)
            {
                raceTimer.Stop();
            }
        }
        private void HorseCountComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (HorseCountComboBox.SelectedItem is ComboBoxItem selectedItem && int.Parse(((ComboBoxItem)HorseCountComboBox.SelectedItem).Content.ToString()) != currentHorseCount)
            {
                foreach (var horse in horses)
                {
                    if (horse.HorseImageControl != null && RaceCanvas.Children.Contains(horse.HorseImageControl))
                    {
                        RaceCanvas.Children.Remove(horse.HorseImageControl);
                    }
                }
                horses.Clear();
                InitializeHorses();
                selectedHorse = horses.FirstOrDefault();

                cameraFocusIndex = 0;
                CameraOnHorse.Text = horses[cameraFocusIndex].Name.ToString();

                double progressOfFocusedHorse = Math.Min(horses[cameraFocusIndex].Position / finishLine * 100, 100);
                RaceProgressBar.Value = progressOfFocusedHorse;

                InitializeRatingTable();
                UpdateUI();
                currentHorseCount = int.Parse(((ComboBoxItem)HorseCountComboBox.SelectedItem).Content.ToString());
            }
        }
        public static string GetColorName(Color color)
        {
            foreach (PropertyInfo prop in typeof(Colors).GetProperties(BindingFlags.Public | BindingFlags.Static))
            {
                if (color == (Color)prop.GetValue(null))
                    return prop.Name;
            }
            return color.ToString();
        }
        public void FindNewCoefficient(Horse horse)
        {
            double alpha = 0.2;
            int place = (int)horse.FixedPosition;
            int averagePlace = (horses.Count + 1) / 2;

            horse.Coefficient = Math.Max(1.0, (horse.Coefficient * (1 + alpha * (place - averagePlace))));
        }
    }
}
