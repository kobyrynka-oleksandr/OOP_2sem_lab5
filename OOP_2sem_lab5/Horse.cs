using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Controls;

namespace OOP_2sem_lab5
{
    public class Horse
    {
        private static readonly Random RandomGen = new Random();
        private readonly List<ImageSource> animationFrames;
        private int currentFrame = 0;
        private double acceleration;

        public double Position { get; private set; }
        public int? FixedPosition { get; set; } = null;
        public Image HorseImageControl { get; set; }
        public string Name { get; }
        public Color Color { get; }
        public double Speed { get; private set; }
        public TimeSpan RaceTime { get; private set; }
        public int PositionOnTrack => (int)Position;
        public double Coefficient { get; set; }
        public double MoneyWon { get; set; }

        public bool HasFinished { get; set; }
        public double FinishTime { get; set; } = 0;

        public Horse(string name, Color color, double coefficient)
        {
            Name = name;
            Color = color;
            Speed = RandomGen.NextDouble() * 5 + 5; 
            Coefficient = Math.Round(coefficient, 2);
            animationFrames = HorseImageHelper.GetHorseAnimation(color);
        }

        public ImageSource GetCurrentFrame()
        {
            return animationFrames[currentFrame % animationFrames.Count];
        }

        public async Task RunAsync(double finishLine, Stopwatch timer, CancellationToken token)
        {
            while (Position < finishLine)
            {
                token.ThrowIfCancellationRequested();

                double factor = RandomGen.NextDouble() * 0.3 + 0.7;
                acceleration = Speed * factor;
                Position += acceleration;
                currentFrame++;

                RaceTime = timer.Elapsed;

                if (Position >= finishLine && !HasFinished)
                {
                    Position = finishLine;
                    RaceTime = timer.Elapsed;
                    FinishTime = RaceTime.TotalSeconds;
                    HasFinished = true;
                }

                await Task.Delay(75, token);
            }
        }

        public void Reset()
        {
            Position = 0;
            HasFinished = false;
            FinishTime = 0;
            RaceTime = TimeSpan.Zero;
            currentFrame = 0;
            FixedPosition = null;
            MoneyWon = 0;
            Speed = RandomGen.NextDouble() * 5 + 5;
        }
    }
}
