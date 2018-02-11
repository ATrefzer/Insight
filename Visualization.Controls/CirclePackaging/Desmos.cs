using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Windows;

namespace Visualization.Controls.CirclePackaging
{
    public static class DebugExtensions
    {
        public static Desmos ToDesmos(this FrontChain frontChain)
        {
            // roughly estimate the size of the graph
            var top = 0;
            var bottom = 0;
            var right = 0;
            var left = 0;

            var desmos = new Desmos();

            var id = 0;
            var current = frontChain.Head;
            do
            {
                var layout = current.Value;
                var expression = layout.ToString();

                id++;
                desmos.Add(id.ToString(), expression);
                desmos.AddXY(layout.Center);

                left = (int) Math.Min(left, layout.Center.X - layout.Radius);
                right = (int) Math.Max(right, layout.Center.X + layout.Radius);
                top = (int) Math.Max(top, layout.Center.Y + layout.Radius);
                bottom = (int) Math.Min(bottom, layout.Center.Y - layout.Radius);

                current = current.Next;
            } while (current != frontChain.Head);

            desmos.SetBounds(top, left, bottom, right);

            return desmos;
        }
    }

    public sealed class Desmos
    {
        private readonly List<string> _expressions = new List<string>();

        private readonly string _template = @"
<html>
<head>
  <meta name=""viewport"" content=""width=device-width, initial-scale=1"">
  <script src=""https://www.desmos.com/api/v1.0/calculator.js?apiKey=dcb31709b452b1cf9dc26972add0fda6""></script>
  <style>
    html, body {
      width: 100%;
      height: 100%;
      margin: 0;
      padding: 0;
      overflow: hidden;
    }

    #calculator {
      width: 100%;
      height: 100%;
    }
  </style>
</head>
<body>
  <div id=""calculator""></div>
  <script >
    var elt = document.getElementById('calculator');
    var options = { border: false };
    var calculator = Desmos.GraphingCalculator(elt, options);

    {bounds}


    calculator.setExpressions([
    {expressions}	
    ]);
  </script>
</body>
</html>
";

        private readonly List<string> _x = new List<string>();
        private readonly List<string> _y = new List<string>();
        private int _bottom = -100;
        private int _left = -100;
        private int _right = 100;
        private bool _setBounds;
        private int _top = 100;

        public void Add(string id, string expression)
        {
            var formatted = $"{{id:'{id}', latex: '{expression}'}}";
            _expressions.Add(formatted);
        }

        public string Build()
        {
            var x = new StringBuilder();
            var y = new StringBuilder();
            string tableExpression = null;

            // Table expression
            if (_y.Count > 0)
            {
                x.Append("[");
                y.Append("[");

                for (var i = 0; i < _x.Count; i++)
                {
                    if (i != 0)
                    {
                        x.Append(",");
                        y.Append(",");
                    }

                    x.Append("'" + _x[i] + "'");
                    y.Append("'" + _y[i] + "'");
                }

                x.Append("]");
                y.Append("]");

                tableExpression = @"
                {{
                    type: 'table',
                    columns: [
                    {{
                        latex: 'x',
                        values: {0}
                    }},
                    {{
                        latex: 'y',
                        values: {1},
     
                        color: Desmos.Colors.BLUE,
                        columnMode: Desmos.ColumnModes.LINES,
                        dragMode: Desmos.DragModes.XY
                    }}]
                }}";
                tableExpression = string.Format(tableExpression, x, y);
            }

            if (!string.IsNullOrEmpty(tableExpression))
            {
                _expressions.Add(tableExpression);
            }

            // Functions
            var all = string.Join(",", _expressions);
            var result = _template.Replace("{expressions}", all);

            var bounds = "";
            if (_setBounds)
            {
                bounds = $@"calculator.setMathBounds({{
                    left: {_left},
                    right: {_right},
                    bottom: {_bottom},
                    top: {_top}
            }});";
            }

            result = result.Replace("{bounds}", bounds);

            return result;
        }

        public void Write(string path)
        {
            File.WriteAllText(path, Build());
        }


        internal void AddXY(Point center)
        {
            _x.Add(center.X.ToString("F5", CultureInfo.InvariantCulture));
            _y.Add(center.Y.ToString("F5", CultureInfo.InvariantCulture));
        }

        internal void SetBounds(int top, int left, int bottom, int right)
        {
            _top = top;
            _bottom = bottom;
            _right = right;
            _left = left;
            _setBounds = true;
        }
    }
}