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
        public static List<ImageSource> GetHorseAnimation(Color color)
        {
            const int count = 12;
            var bitmaps = ReadImageList("Images/Horses", "WithOutBorder_", ".png", count);
            var masks = ReadImageList("Images/HorsesMask", "mask_", ".png", count);
            return bitmaps.Select((img, i) => ApplyColorMask(img, masks[i], color)).ToList();
        }

        private static List<BitmapImage> ReadImageList(string path, string prefix, string ext, int count)
        {
            var list = new List<BitmapImage>();
            for (int i = 0; i < count; i++)
            {
                var uri = new Uri($"pack://application:,,,/{path}/{prefix}{i:0000}{ext}");
                list.Add(new BitmapImage(uri));
            }
            return list;
        }

        private static ImageSource ApplyColorMask(BitmapImage image, BitmapImage mask, Color color)
        {
            var imgBmp = new WriteableBitmap(image);
            var maskBmp = new WriteableBitmap(mask);
            var resultBmp = BitmapFactory.New(imgBmp.PixelWidth, imgBmp.PixelHeight);

            resultBmp.ForEach((x, y, c) =>
            {
                var maskPixel = maskBmp.GetPixel(x, y);
                var blend = maskPixel.A / 255.0;
                var src = imgBmp.GetPixel(x, y);
                byte r = (byte)(color.R * blend + src.R * (1 - blend));
                byte g = (byte)(color.G * blend + src.G * (1 - blend));
                byte b = (byte)(color.B * blend + src.B * (1 - blend));
                return Color.FromArgb(src.A, r, g, b);
            });

            return resultBmp;
        }
    }
}
