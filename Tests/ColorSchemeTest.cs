using NUnit.Framework;
using System;
using System.IO;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Windows.Media;

using Visualization.Controls;

using ColorConverter = Visualization.Controls.ColorConverter;

namespace Tests
{
    [TestFixture]
    public class ColorSchemeTest
    {
        [Test]
        public void ArgbConversion()
        {
            int argb1 = 0x12345678;
            var color = ColorConverter.FromArgb(argb1);

            int argb2 = ColorConverter.ToArgb(color);
            Assert.AreEqual(argb1, argb2);
        }


        [Test]
        public void AddingTooMuchColors()
        {
            var scheme = new ColorScheme();

            // Add names until no more colors are available
            while (scheme.AssignFreeColor(Guid.NewGuid().ToString()))
            {
                ;
            }

            Assert.IsFalse(scheme.AssignFreeColor("me"));

            // Even if we do not have a color, the name is added.
            // The coloring can be edited later.
            Assert.IsTrue(scheme.Names.Contains("me"));

            // me has the default color
            var name = scheme.GetBrush("me").Color;
            var defaultColor = DefaultDrawingPrimitives.DefaultColor.ToString();
            Assert.AreEqual(defaultColor, name);

        }

        [Test]
        public void Serialization()
        {
            var scheme = new ColorScheme();
            scheme.AssignFreeColor("me");
            scheme.AssignFreeColor("you");

            DataContractJsonSerializer serializer = new DataContractJsonSerializer(typeof(ColorScheme));

            // Serialize
            var stream = new MemoryStream();
            serializer.WriteObject(stream, scheme);
            var bytes = stream.ToArray();
            var json = Encoding.UTF8.GetString(bytes);

            // Deserialize
            stream.Close();
            stream = new MemoryStream(bytes);
            var deserialized = (ColorScheme)serializer.ReadObject(stream);

            // Colors are the same
            Assert.AreEqual(scheme.GetBrush("me").ToString(), deserialized.GetBrush("me").ToString());
            Assert.AreEqual(scheme.GetBrush("you").ToString(), deserialized.GetBrush("you").ToString());
        }
    }
}

