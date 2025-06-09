using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
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

namespace OOP_2sem_lab5_2
{
    public partial class MainWindow : Window
    {
        private List<Point> sites = new List<Point>();
        private RenderTargetBitmap voronoiBitmap;
        private bool multiThreaded = false;
        private int threadCount = Environment.ProcessorCount;
        private readonly object drawLock = new object();
        private Dictionary<Point, Color> siteColors = new Dictionary<Point, Color>();

        public MainWindow()
        {
            InitializeComponent();
        }

        private void canvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var pos = e.GetPosition(canvas);
            sites.Add(pos);
            siteColors[pos] = GenerateRandomColor(sites.Count);
            DrawSites();
        }

        private void canvas_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            var pos = e.GetPosition(canvas);
            var nearest = sites.OrderBy(p => Distance(p, pos)).FirstOrDefault();
            if (Distance(nearest, pos) < 100)
            {
                sites.Remove(nearest);
                siteColors.Remove(nearest);
            }
            DrawSites();
        }

        private void BtnGenerateRandom_Click(object sender, RoutedEventArgs e)
        {
            if (!int.TryParse(NumOfPoints.Text, out int result))
            {
                MessageBox.Show("Некоректний формат!");
                return;
            }
            else if (result < 1)
            {
                MessageBox.Show("Кількість точок повинна бути білше за 0!");
                return;
            }
            else if (result > 5000)
            {
                MessageBox.Show("Кількість точок повинна бути не білше за 5000!");
                return;
            }
            var rnd = new Random();
            sites.Clear();
            siteColors.Clear();
            for (int i = 0; i < result; i++)
            {
                var p = new Point(rnd.Next((int)canvas.Width), rnd.Next((int)canvas.Height));
                sites.Add(p);
                siteColors[p] = GenerateRandomColor(i);
            }
            DrawSites();
        }

        private void BtnVoronoi_Click(object sender, RoutedEventArgs e)
        {
            if (multiThreaded)
                RunVoronoiMultiThreaded();
            else
                RunVoronoiSingleThreaded();
        }

        private void CheckBoxMultiThread_Checked(object sender, RoutedEventArgs e) => multiThreaded = true;

        private void CheckBoxMultiThread_Unchecked(object sender, RoutedEventArgs e) => multiThreaded = false;

        private void RunVoronoiSingleThreaded()
        {
            var stopwatch = Stopwatch.StartNew();
            var cpuStartTime = Process.GetCurrentProcess().TotalProcessorTime;
            long memoryBefore = GC.GetTotalMemory(true);

            int width = (int)canvas.Width;
            int height = (int)canvas.Height;

            DrawingVisual visual = new DrawingVisual();
            using (DrawingContext dc = visual.RenderOpen())
            {
                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        Point pt = new Point(x, y);
                        var nearest = GetNearestSite(pt);
                        var color = GetColorForSite(nearest);
                        Brush brush = new SolidColorBrush(color);
                        dc.DrawRectangle(brush, null, new Rect(x, y, 1, 1));
                    }
                }
            }

            voronoiBitmap = new RenderTargetBitmap(width, height, 96, 96, PixelFormats.Pbgra32);
            voronoiBitmap.Render(visual);
            canvas.Background = new ImageBrush(voronoiBitmap);

            stopwatch.Stop();
            var cpuEndTime = Process.GetCurrentProcess().TotalProcessorTime;
            long memoryAfter = GC.GetTotalMemory(true);

            textBlockTime.Text = $"Однопотоково:\n" +
                                 $"- Реальний час: {stopwatch.ElapsedMilliseconds} мс\n" +
                                 $"- Процесорний час: {(cpuEndTime - cpuStartTime).TotalMilliseconds:F1} мс\n" +
                                 $"- Спожито пам’яті: {(memoryAfter - memoryBefore) / 1024.0:F1} КБ";
        }

        private void RunVoronoiMultiThreaded()
        {
            var stopwatch = Stopwatch.StartNew();
            var cpuStartTime = Process.GetCurrentProcess().TotalProcessorTime;
            long memoryBefore = GC.GetTotalMemory(true);

            int width = (int)canvas.Width;
            int height = (int)canvas.Height;
            int stride = width * 4;
            byte[] pixels = new byte[height * stride];

            Parallel.For(0, threadCount, i =>
            {
                int yStart = i * height / threadCount;
                int yEnd = (i + 1) * height / threadCount;

                for (int y = yStart; y < yEnd; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        Point pt = new Point(x, y);
                        var nearest = GetNearestSite(pt);
                        var color = GetColorForSite(nearest);
                        int index = y * stride + x * 4;

                        pixels[index + 0] = color.B;
                        pixels[index + 1] = color.G;
                        pixels[index + 2] = color.R;
                        pixels[index + 3] = 255;
                    }
                }
            });

            var wb = new WriteableBitmap(width, height, 96, 96, PixelFormats.Bgra32, null);
            wb.WritePixels(new Int32Rect(0, 0, width, height), pixels, stride, 0);

            DrawingVisual visual = new DrawingVisual();
            using (DrawingContext dc = visual.RenderOpen())
            {
                dc.DrawImage(wb, new Rect(0, 0, width, height));
            }

            voronoiBitmap = new RenderTargetBitmap(width, height, 96, 96, PixelFormats.Pbgra32);
            voronoiBitmap.Render(visual);
            canvas.Background = new ImageBrush(voronoiBitmap);

            stopwatch.Stop();
            var cpuEndTime = Process.GetCurrentProcess().TotalProcessorTime;
            long memoryAfter = GC.GetTotalMemory(true);

            textBlockTime.Text = $"Багатопотоково ({threadCount} потоків):\n" +
                                 $"- Реальний час: {stopwatch.ElapsedMilliseconds} мс\n" +
                                 $"- Процесорний час: {(cpuEndTime - cpuStartTime).TotalMilliseconds:F1} мс\n" +
                                 $"- Спожито пам’яті: {(memoryAfter - memoryBefore) / 1024.0:F1} КБ";
        }

        private double Distance(Point a, Point b)
        {
            double dx = a.X - b.X;
            double dy = a.Y - b.Y;
            return dx * dx + dy * dy;
        }

        private Color GetColorForSite(Point p) =>
            siteColors.ContainsKey(p) ? siteColors[p] : Colors.Gray;

        private Color GenerateRandomColor(int seed)
        {
            var rnd = new Random(seed * 997);
            return Color.FromRgb((byte)rnd.Next(64, 256), (byte)rnd.Next(64, 256), (byte)rnd.Next(64, 256));
        }

        private void DrawSites()
        {
            canvas.Children.Clear();

            foreach (var p in sites)
            {
                Ellipse ellipse = new Ellipse
                {
                    Width = 6,
                    Height = 6,
                    Fill = Brushes.Black
                };
                Canvas.SetLeft(ellipse, p.X - 3);
                Canvas.SetTop(ellipse, p.Y - 3);
                canvas.Children.Add(ellipse);
            }
        }
        private Point GetNearestSite(Point pt)
        {
            double minDist = double.MaxValue;
            Point nearest = default;

            foreach (var site in sites)
            {
                double d = Distance(site, pt);
                if (d < minDist)
                {
                    minDist = d;
                    nearest = site;
                }
            }

            return nearest;
        }
    }
}
