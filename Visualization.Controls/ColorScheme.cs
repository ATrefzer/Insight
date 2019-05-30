using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Media;

using Visualization.Controls.Interfaces;

using Brush = System.Drawing.Brush;
using Color = System.Drawing.Color;

namespace Visualization.Controls
{
    [Serializable]
    public sealed class ColorScheme : IColorScheme
    {
        private readonly List<string> _names;
        private readonly Dictionary<string, Brush> _nameToBrush = new Dictionary<string, Brush>();
        private readonly Dictionary<string, Color> _nameToColor = new Dictionary<string, Color>();

        /// <summary>
        /// Additionally store System.Windows.Media.SolidColorBrushes for Wpf application.
        /// </summary>
        private readonly Dictionary<string, SolidColorBrush> _nameToMediaBrush = new Dictionary<string, SolidColorBrush>();

        private string[] _colorDefinitions;

        private int _nextFreeColorIndex;

        private bool _canAddColors;

        public List<string> Names
        {
            get
            {
                return _names.ToList();
            }
        }


        public void Export(string file)
        {
            using (var stream = new FileStream(file, FileMode.Create))
            {
                using (var writer = new StreamWriter(stream, Encoding.UTF8))
                {

                    writer.WriteLine(_canAddColors);
                    writer.WriteLine(_nextFreeColorIndex);

                    foreach (var name in Names)
                    {
                        var color = _nameToColor[name];
                        var parts = new List<string>();
                        parts.Add(color.ToArgb().ToString("X"));
                        parts.Add(name);

                        writer.WriteLine(string.Join(",", parts));
                    }
                }
            }
        }

        public void Import(string file)
        {
            _names.Clear();
            _nameToColor.Clear();
            _nameToMediaBrush.Clear();
            _nameToBrush.Clear();
            _canAddColors = false;
            _nextFreeColorIndex = 0;

            using (var stream = new FileStream(file, FileMode.Open))
            {
                using (StreamReader reader = new StreamReader(stream, Encoding.UTF8))
                {
                    _canAddColors = bool.Parse(reader.ReadLine());
                    _nextFreeColorIndex = int.Parse(reader.ReadLine());

                    while (!reader.EndOfStream)
                    {
                        var line = reader.ReadLine();
                        var parts = line.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries).ToList();
                        Debug.Assert(parts.Count() == 2);
                        var color = Color.FromArgb(int.Parse(parts[0], NumberStyles.HexNumber));

                        var name = string.Join("", parts.GetRange(1, parts.Count - 1));
                        _names.Add(name);
                        InitColor(name, color);
                    }
                }
            }
        }

        public ColorScheme(string[] names, Color[] colors)
        {
            _canAddColors = false;
            _names = names.ToList();
            for (var index = 0; index < names.Length; index++)
            {
                var color = colors[index];
                var name = names[index];
                InitColor(name, color);
            }

            // all other names get the default color: White
        }

        public ColorScheme()
        {
            CreateColorsDefinitions();
            _names = new List<string>();
            _canAddColors = true;
        }

        public ColorScheme(string[] names)
        {
            CreateColorsDefinitions();
            _names = names.ToList();
            foreach (var name in _names)
            {
                InitializeColorForName(name);
            }
            _canAddColors = true;
        }



        public void AddColorKey(string name)
        {
            if (!_canAddColors)
            {
                Debug.Assert(false);
                return;
            }

            if (!_names.Contains(name))
            {
                _names.Add(name);
                InitializeColorForName(name);
            }
        }

        public string GetColorName(string name)
        {
            if (!_names.Contains(name))
            {
                return "White";
            }

            //return _nameToColor[name].Name;
            var color = _nameToColor[name];
            return $"#{color.R:X2}{color.G:X2}{color.B:X2}";
        }

        public SolidColorBrush GetMediaBrush(string name)
        {
            if (!_nameToMediaBrush.ContainsKey(name))
            {
                return DefaultDrawingPrimitives.DefaultColor;
            }

            return _nameToMediaBrush[name];
        }

        public Brush GetBrush(string name)
        {
            return _nameToBrush[name];
        }

        private void CreateColorsDefinitions()
        {

            var experimential = new[]
            {
                /*"FF000000", "FFFFFF00",*/ "FF1CE6FF", "FFFF34FF", "FFFF4A46", "FF008941", "FF006FA6", "FFA30059",
                "FFFFDBE5", "FF7A4900", "FF0000A6", "FF63FFAC", "FFB79762", "FF004D43", "FF8FB0FF", "FF997D87",
                "FF5A0007", "FF809693", "FFFEFFE6", "FF1B4400", "FF4FC601", "FF3B5DFF", "FF4A3B53", "FFFF2F80",
                "FF61615A", "FFBA0900", "FF6B7900", "FF00C2A0", "FFFFAA92", "FFFF90C9", "FFB903AA", "FFD16100",
                "FFDDEFFF", "FF000035", "FF7B4F4B", "FFA1C299", "FF300018", "FF0AA6D8", "FF013349", "FF00846F",
                "FF372101", "FFFFB500", "FFC2FFED", "FFA079BF", "FFCC0744", "FFC0B9B2", "FFC2FF99", "FF001E09",
                "FF00489C", "FF6F0062", "FF0CBD66", "FFEEC3FF", "FF456D75", "FFB77B68", "FF7A87A1", "FF788D66",
                "FF885578", "FFFAD09F", "FFFF8A9A", "FFD157A0", "FFBEC459", "FF456648", "FF0086ED", "FF886F4C",
                "FF34362D", "FFB4A8BD", "FF00A6AA", "FF452C2C", "FF636375", "FFA3C8C9", "FFFF913F", "FF938A81",
                "FF575329", "FF00FECF", "FFB05B6F", "FF8CD0FF", "FF3B9700", "FF04F757", "FFC8A1A1", "FF1E6E00",
                "FF7900D7", "FFA77500", "FF6367A9", "FFA05837", "FF6B002C", "FF772600", "FFD790FF", "FF9B9700",
                "FF549E79", "FFFFF69F", "FF201625", "FF72418F", "FFBC23FF", "FF99ADC0", "FF3A2465", "FF922329",
                "FF5B4534", "FFFDE8DC", "FF404E55", "FF0089A3", "FFCB7E98", "FFA4E804", "FF324E72", "FF6A3A4C",
                "FF83AB58", "FF001C1E", "FFD1F7CE", "FF004B28", "FFC8D0F6", "FFA3A489", "FF806C66", "FF222800",
                "FFBF5650", "FFE83000", "FF66796D", "FFDA007C", "FFFF1A59", "FF8ADBB4", "FF1E0200", "FF5B4E51",
                "FFC895C5", "FF320033", "FFFF6832", "FF66E1D3", "FFCFCDAC", "FFD0AC94", "FF7ED379", "FF012C58"
            };


            // http://stackoverflow.com/questions/309149/generate-distinctly-different-rgb-colors-in-graphs

            _colorDefinitions = new[]
                                {
                                        // "FF000000",
                                        "FF00FF00",
                                        "FF0000FF",
                                        "FFFF0000",
                                        "FF01FFFE",
                                        "FFFFA6FE",
                                        "FFFFDB66",
                                        "FF006401",
                                        "FF010067",
                                        "FF95003A",
                                        "FF007DB5",
                                        "FFFF00F6",
                                        "FFFFEEE8",
                                        "FF774D00",
                                        "FF90FB92",
                                        "FF0076FF",
                                        "FFD5FF00",
                                        "FFFF937E",
                                        "FF6A826C",
                                        "FFFF029D",
                                        "FFFE8900",
                                        "FF7A4782",
                                        "FF7E2DD2",
                                        "FF85A900",
                                        "FFFF0056",
                                        "FFA42400",
                                        "FF00AE7E",
                                        "FF683D3B",
                                        "FFBDC6FF",
                                        "FF263400",
                                        "FFBDD393",
                                        "FF00B917",
                                        "FF9E008E",
                                        "FF001544",
                                        "FFC28C9F",
                                        "FFFF74A3",
                                        "FF01D0FF",
                                        "FF004754",
                                        "FFE56FFE",
                                        "FF788231",
                                        "FF0E4CA1",
                                        "FF91D0CB",
                                        "FFBE9970",
                                        "FF968AE8",
                                        "FFBB8800",
                                        "FF43002C",
                                        "FFDEFF74",
                                        "FF00FFC6",
                                        "FFFFE502",
                                        "FF620E00",
                                        "FF008F9C",
                                        "FF98FF52",
                                        "FF7544B1",
                                        "FFB500FF",
                                        "FF00FF78",
                                        "FFFF6E41",
                                        "FF005F39",
                                        "FF6B6882",
                                        "FF5FAD4E",
                                        "FFA75740",
                                        "FFA5FFD2",
                                        "FFFFB167",
                                        "FF009BFF",
                                        "FFE85EBE"
                                };

            // TODO
            _colorDefinitions = experimential;
        }

        private void InitializeColorForName(string name)
        {
            if (!_names.Contains(name))
            {
                // Default color for unknown name
                return;
            }

            if (_nameToColor.ContainsKey(name))
            {
                // Already known
                return;
            }

            Color color;

            try
            {
                color = Color.FromArgb(int.Parse(_colorDefinitions[_nextFreeColorIndex], NumberStyles.HexNumber));
            }
            catch (IndexOutOfRangeException)
            {
                // Too much colors!
                color = Color.LightGray;
                Debug.Assert(false); // Not enough colors!
            }
            InitColor(name, color);

            _nextFreeColorIndex++;
        }

        private void InitColor(string name, Color color)
        {
            var brush = new SolidBrush(color);

            _nameToBrush.Add(name, brush);
            _nameToColor.Add(name, color);

            var mediaBrush = ToMediaBrush(color);
            _nameToMediaBrush.Add(name, mediaBrush);
        }

        private SolidColorBrush ToMediaBrush(Color color)
        {
            var mediaBrush = new SolidColorBrush(System.Windows.Media.Color.FromArgb(color.A, color.R, color.G, color.B));
            mediaBrush.Freeze();
            return mediaBrush;
        }
    }
}