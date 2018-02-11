using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace Visualization.Controls.Bitmap
{
    internal static class BitmapManipulation
    {
        /// <summary>
        ///     This code is from http://stackoverflow.com/questions/4820212/automatically-trim-a-bitmap-to-minimum-size
        ///     Kudos to the developer
        /// </summary>
        public static System.Drawing.Bitmap TrimBitmap(System.Drawing.Bitmap source)
        {
            Rectangle srcRect;
            BitmapData data = null;
            try
            {
                data = source.LockBits(new Rectangle(0, 0, source.Width, source.Height), ImageLockMode.ReadOnly,
                                       PixelFormat.Format32bppArgb);
                var buffer = new byte[data.Height * data.Stride];
                Marshal.Copy(data.Scan0, buffer, 0, buffer.Length);

                int xMin = int.MaxValue,
                    xMax = int.MinValue,
                    yMin = int.MaxValue,
                    yMax = int.MinValue;

                var foundPixel = false;

                // Find xMin
                for (var x = 0; x < data.Width; x++)
                {
                    var stop = false;
                    for (var y = 0; y < data.Height; y++)
                    {
                        var alpha = buffer[y * data.Stride + 4 * x + 3];
                        if (alpha != 0)
                        {
                            xMin = x;
                            stop = true;
                            foundPixel = true;
                            break;
                        }
                    }

                    if (stop)
                    {
                        break;
                    }
                }

                // Image is empty...
                if (!foundPixel)
                {
                    return null;
                }

                // Find yMin
                for (var y = 0; y < data.Height; y++)
                {
                    var stop = false;
                    for (var x = xMin; x < data.Width; x++)
                    {
                        var alpha = buffer[y * data.Stride + 4 * x + 3];
                        if (alpha != 0)
                        {
                            yMin = y;
                            stop = true;
                            break;
                        }
                    }

                    if (stop)
                    {
                        break;
                    }
                }

                // Find xMax
                for (var x = data.Width - 1; x >= xMin; x--)
                {
                    var stop = false;
                    for (var y = yMin; y < data.Height; y++)
                    {
                        var alpha = buffer[y * data.Stride + 4 * x + 3];
                        if (alpha != 0)
                        {
                            xMax = x;
                            stop = true;
                            break;
                        }
                    }

                    if (stop)
                    {
                        break;
                    }
                }

                // Find yMax
                for (var y = data.Height - 1; y >= yMin; y--)
                {
                    var stop = false;
                    for (var x = xMin; x <= xMax; x++)
                    {
                        var alpha = buffer[y * data.Stride + 4 * x + 3];
                        if (alpha != 0)
                        {
                            yMax = y;
                            stop = true;
                            break;
                        }
                    }

                    if (stop)
                    {
                        break;
                    }
                }

                srcRect = Rectangle.FromLTRB(xMin, yMin, xMax, yMax);
            }
            finally
            {
                if (data != null)
                {
                    source.UnlockBits(data);
                }
            }

            var dest = new System.Drawing.Bitmap(srcRect.Width, srcRect.Height);
            var destRect = new Rectangle(0, 0, srcRect.Width, srcRect.Height);
            using (var graphics = Graphics.FromImage(dest))
            {
                graphics.DrawImage(source, destRect, srcRect, GraphicsUnit.Pixel);
            }

            return dest;
        }
    }
}