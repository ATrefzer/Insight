using System.Windows.Media;

namespace Visualization.Controls
{
    public static class ColorConverter
    {
        public static int ToArgb(Color color)
        {
            int argb = (color.A << 24) | (color.R << 16) | (color.G << 8) | color.B;
            return argb;


            //byte[] bytes = new byte[] { color.A, color.R, color.G, color.B };
            //return BitConverter.ToInt32(bytes, 0);
        }

        public static Color FromArgb(int argb)
        {
            // Format is little endian
            byte a = (byte) ((argb & 0xff000000) >> 24);
            byte r = (byte) ((argb & 0x00ff0000) >> 16);
            byte g = (byte) ((argb & 0x0000ff00) >> 8);
            byte b = (byte) (argb & 0x000000ff);

            var color = Color.FromArgb(a, r, g, b);
            return color;

        
        }
    }
}