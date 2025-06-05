using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using System.Windows.Media;

namespace OOP_2sem_lab5
{
    public static class HorseImageHelper
    {
        private const int FrameCount = 12;

        private static readonly List<BitmapImage> baseBitmaps = ReadImageList("Images/Horses", "WithOutBorder_", ".png", FrameCount);
        private static readonly List<BitmapImage> maskBitmaps = ReadImageList("Images/HorsesMask", "mask_", ".png", FrameCount);

        private static readonly Dictionary<Color, List<ImageSource>> animationCache = new Dictionary<Color, List<ImageSource>>();

        public static List<ImageSource> GetHorseAnimation(Color color)
        {
            if (animationCache.TryGetValue(color, out var cachedAnimation))
                return cachedAnimation;

            // Створюємо анімацію для нового кольору
            var animation = new List<ImageSource>(FrameCount);
            for (int i = 0; i < FrameCount; i++)
            {
                var coloredFrame = ApplyColorMask(baseBitmaps[i], maskBitmaps[i], color);
                animation.Add(coloredFrame);
            }

            animationCache[color] = animation;
            return animation;
        }

        private static List<BitmapImage> ReadImageList(string path, string prefix, string ext, int count)
        {
            var list = new List<BitmapImage>(count);
            for (int i = 0; i < count; i++)
            {
                var uri = new Uri($"pack://application:,,,/{path}/{prefix}{i:0000}{ext}", UriKind.Absolute);
                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.UriSource = uri;
                bitmap.EndInit();
                bitmap.Freeze();
                list.Add(bitmap);
            }
            return list;
        }

        private static ImageSource ApplyColorMask(BitmapImage image, BitmapImage mask, Color color)
        {
            var imgBmp = new WriteableBitmap(image);
            var maskBmp = new WriteableBitmap(mask);

            int width = imgBmp.PixelWidth;
            int height = imgBmp.PixelHeight;

            int stride = width * 4;
            byte[] imgPixels = new byte[height * stride];
            byte[] maskPixels = new byte[height * stride];
            imgBmp.CopyPixels(imgPixels, stride, 0);
            maskBmp.CopyPixels(maskPixels, stride, 0);

            byte[] resultPixels = new byte[height * stride];

            for (int y = 0; y < height; y++)
            {
                int offset = y * stride;
                for (int x = 0; x < width; x++)
                {
                    int i = offset + x * 4;

                    byte srcB = imgPixels[i];
                    byte srcG = imgPixels[i + 1];
                    byte srcR = imgPixels[i + 2];
                    byte srcA = imgPixels[i + 3];

                    byte maskB = maskPixels[i];
                    byte maskG = maskPixels[i + 1];
                    byte maskR = maskPixels[i + 2];
                    byte maskA = maskPixels[i + 3];

                    double blend = maskA / 255.0;

                    byte r = (byte)(color.R * blend + srcR * (1 - blend));
                    byte g = (byte)(color.G * blend + srcG * (1 - blend));
                    byte b = (byte)(color.B * blend + srcB * (1 - blend));
                    byte a = srcA;

                    resultPixels[i] = b;
                    resultPixels[i + 1] = g;
                    resultPixels[i + 2] = r;
                    resultPixels[i + 3] = a;
                }
            }

            var result = new WriteableBitmap(width, height, image.DpiX, image.DpiY, PixelFormats.Bgra32, null);
            result.WritePixels(new System.Windows.Int32Rect(0, 0, width, height), resultPixels, stride, 0);
            result.Freeze();
            return result;
        }
    }
}
