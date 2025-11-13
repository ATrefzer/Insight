using NUnit.Framework;
using System;
using System.IO;
using System.Runtime.Serialization.Json;
using System.Text;
using NUnit.Framework.Legacy;
using Visualization.Controls.Common;

using ColorConverter = Visualization.Controls.Common.ColorConverter;

namespace Tests
{
    [TestFixture]
    public class ColorSchemeTest
    {
        [Test]
        public void ArgbConversion()
        {
            const int argb1 = 0x12345678;
            var color = ColorConverter.FromArgb(argb1);

            var argb2 = ColorConverter.ToArgb(color);
            Assert.That(argb2, Is.EqualTo(argb1));
        }


        [Test]
        public void AddingTooMuchColors()
        {
            var scheme = new ColorScheme();

            // Add names until no more colors are available
            while (scheme.AssignFreeColor(Guid.NewGuid().ToString()))
            {
            }

            ClassicAssert.IsFalse(scheme.AssignFreeColor("me"));

            // Even if we do not have a color, the name is added.
            // The coloring can be edited later.
            ClassicAssert.IsTrue(scheme.Names.Contains("me"));

            // me has the default color
            var name = scheme.GetBrush("me").Color.ToString();
            var defaultColor = DefaultDrawingPrimitives.DefaultColor.ToString();
            Assert.That(name, Is.EqualTo(defaultColor));

        }

        [Test]
        public void Serialization()
        {
            var scheme = new ColorScheme();
            scheme.AssignFreeColor("me");
            scheme.AssignFreeColor("you");

            var serializer = new DataContractJsonSerializer(typeof(ColorScheme));

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
            Assert.That(deserialized.GetBrush("me").ToString(), Is.EqualTo(scheme.GetBrush("me").ToString()));
            Assert.That(deserialized.GetBrush("you").ToString(), Is.EqualTo(scheme.GetBrush("you").ToString()));
        }
    }
}

