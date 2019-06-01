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
        private readonly IColorScheme _colorScheme;

        public LegendBitmap(List<string> names, IColorScheme colorScheme)
        {
            _names = names;
            _colorScheme = colorScheme;
        }


        public static System.Drawing.Brush ToDrawingBrush(System.Windows.Media.SolidColorBrush mediaBrush)
        {
            return new System.Drawing.SolidBrush(System.Drawing.Color.FromArgb(mediaBrush.Color.A, mediaBrush.Color.R, mediaBrush.Color.G, mediaBrush.Color.B));
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

                var brush = ToDrawingBrush(_colorScheme.GetBrush(name));

                graphics.FillRectangle(brush, x, y, 20, 20);

                graphics.DrawString("(" + _colorScheme.GetColorName(name) + ")",
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
                    file.WriteLine(name + "\t" + _colorScheme.GetColorName(name));
                }
            }
        }
    }
}
