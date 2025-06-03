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
        public Image HorseImageControl { get; set; }
        public string Name { get; }
        public Color Color { get; }
        public double Speed { get; }
        public TimeSpan RaceTime { get; private set; }
        public int PositionOnTrack => (int)Position;
        public double Coefficient { get; set; }
        public double MoneyWon { get; set; }

        public bool HasFinished { get; set; }
        public double FinishTime { get; set; } = 0;

        public Horse(string name, Color color)
        {
            Name = name;
            Color = color;
            Speed = RandomGen.NextDouble() * 5 + 5; 
            Coefficient = Math.Round(2 + RandomGen.NextDouble() * 2, 2); 
            animationFrames = HorseImageHelper.GetHorseAnimation(color);
        }

        public ImageSource GetCurrentFrame()
        {
            return animationFrames[currentFrame % animationFrames.Count];
        }

        public async Task RunAsync(double finishLine, Stopwatch timer, CancellationToken token)
        {
            var start = timer.Elapsed;

            while (Position < finishLine)
            {
                if (token.IsCancellationRequested) break;

                double factor = RandomGen.NextDouble() * 0.3 + 0.7;
                acceleration = Speed * factor;
                Position += acceleration;
                currentFrame++;
                await Task.Delay(100, token);
            }


            RaceTime = timer.Elapsed - start;
        }

        public void Reset()
        {
            Position = 0;
            currentFrame = 0;
            RaceTime = TimeSpan.Zero;
            MoneyWon = 0;
        }
    }
}
