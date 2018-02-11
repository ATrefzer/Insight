using System.Windows;
using System.Windows.Media;

namespace Visualization.Controls
{
    public static class ColorScheme
    {
        public static readonly Pen BlackPen = new Pen(Brushes.Black, 1.0);

        public static readonly SolidColorBrush DefaultColor = Brushes.LightGray;


        public static readonly SolidColorBrush Highlight = Brushes.Yellow;

        public static readonly GradientBrush RedToWhiteGradient;



        public static readonly GradientBrush WhiteToRedGradient;
        private static NameToColorMapper _mapper;

        static ColorScheme()
        {
            RedToWhiteGradient = new LinearGradientBrush(Colors.Red, Colors.White, new Point(0, 0), new Point(1, 1));
            RedToWhiteGradient.GradientStops.Add(new GradientStop(Colors.Red, 0.0));
            RedToWhiteGradient.GradientStops.Add(new GradientStop(Colors.White, 1.0));
            RedToWhiteGradient.Freeze();

            WhiteToRedGradient = new LinearGradientBrush(Colors.LightGray, Colors.DarkRed, new Point(0, 0), new Point(1, 1));
            WhiteToRedGradient.GradientStops.Add(new GradientStop(Colors.White, 0.0));
            WhiteToRedGradient.GradientStops.Add(new GradientStop(Colors.Red, 1.0));
            WhiteToRedGradient.Freeze();
            BlackPen.Freeze();
        }

     

        public static SolidColorBrush GetBrush(string key)
        {
            if (_mapper == null)
            {
                return DefaultColor;
            }

            return _mapper.GetMediaBrush(key);
        }

        public static void SetColorMapping(NameToColorMapper mapper)
        {
            _mapper = mapper;
        }
    }
}