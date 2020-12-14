using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using Visualization.Controls.Interfaces;

namespace Visualization.Controls.Bitmap
{
    public class LegendBitmap
    {
        private readonly List<string> _names;
        private readonly IBrushFactory _brushFactory;

        public LegendBitmap(List<string> names, IBrushFactory brushFactory)
        {
            _names = names;
            _brushFactory = brushFactory;
        }


        public static Brush ToDrawingBrush(System.Windows.Media.SolidColorBrush mediaBrush)
        {
            return new SolidBrush(Color.FromArgb(mediaBrush.Color.A, mediaBrush.Color.R, mediaBrush.Color.G, mediaBrush.Color.B));
        }

        public void CreateLegendBitmap(string file)
        {
            var bitmap = new System.Drawing.Bitmap(2000, 2000);
            var graphics = Graphics.FromImage(bitmap);


            var line = 0;
            Debug.Assert(_names.Count > 0);

            foreach (var name in _names)
            {
                // Legend
                var x = 0;
                var y = 30 * line;

                var offsetColorName = 25;
                var offsetDeveloperName = 200;

                var brush = ToDrawingBrush(_brushFactory.GetBrush(name));

                graphics.FillRectangle(brush, x, y, 20, 20);

                graphics.DrawString("(" + GetColorName(name) + ")",
                    new Font(FontFamily.GenericSansSerif, 12), Brushes.Black, x + offsetColorName, y);
                graphics.DrawString(name, new Font(FontFamily.GenericSansSerif, 12), Brushes.Black,
                    x + offsetDeveloperName, y);

                line++;
            }

            var trimmed = BitmapManipulation.TrimBitmap(bitmap);
            trimmed.Save(file);
        }

        public void CreateLegendText(string path)
        {
            using (var file = File.CreateText(path))
            {
                foreach (var name in _names) // dump only used developers!
                {
                    var colorName = GetColorName(name);
                    file.WriteLine(name + "\t" + colorName);
                }
            }
        }

        private string GetColorName(string name)
        {
            var brush = _brushFactory.GetBrush(name);
            var argb = ColorConverter.ToArgb(brush.Color);
            var colorName = "#" + argb.ToString("X");
            return colorName;
        }
    }
}


 