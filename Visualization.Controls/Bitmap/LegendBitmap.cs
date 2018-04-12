using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Drawing;

namespace Visualization.Controls.Bitmap
{
    public class LegendBitmap
    {
        public ColorScheme ColorScheme { get; }

        public LegendBitmap(ColorScheme colorScheme)
            {
            ColorScheme = colorScheme;
        }

        public void CreateLegendBitmap(string file)
        {
            var bitmap = new System.Drawing.Bitmap(2000, 2000);
            var graphics = Graphics.FromImage(bitmap);


            var line = 0;
            var developersToPrint = ColorScheme.Names.ToList();
            Debug.Assert(developersToPrint.Count > 0);

            foreach (var developer in developersToPrint)
            {
                // Legend
                var x = 0;
                var y = 30 * line;

                var offsetColorName = 25;
                var offsetDeveloperName = 200;

                var brush = ColorScheme.GetBrush(developer);

                graphics.FillRectangle(brush, x, y, 20, 20);

                graphics.DrawString("(" + ColorScheme.GetColorName(developer) + ")",
                    new Font(FontFamily.GenericSansSerif, 12), Brushes.Black, x + offsetColorName, y);
                graphics.DrawString(developer, new Font(FontFamily.GenericSansSerif, 12), Brushes.Black,
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
                foreach (var developer in ColorScheme.Names) // dump only used developers!
                {
                    file.WriteLine(developer + "\t" + ColorScheme.GetColorName(developer));
                }
            }
        }

     

    }
}
