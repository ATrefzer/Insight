using System;
using System.Collections.Generic;

using Insight.Shared;

using NUnit.Framework;

namespace Tests
{
    internal class DtoString
    {
        public string A { get; set; }
    }

    internal class DtoDouble
    {
        public double A { get; set; }
    }

    internal class DtoMany
    {
        public double A { get; set; }
        public string B { get; set; }
        public string C { get; set; }
    }

    [TestFixture]
    internal class CsvWriterTests
    {
        private CsvWriter _csv;

        [Test]
        public void DoubleProperty_OneItem()
        {
            var dto = new DtoDouble();
            dto.A = Math.PI;

            _csv.NumberFormat = "F2";
            var result = _csv.ToCsv(new List<DtoDouble> { dto });
            Assert.AreEqual("3.14\r\n", result);
        }


        [Test]
        public void ManyProperties_ManyItems()
        {
            var expected =
                    "3.142,B1,\"C,1\"\r\n" +
                    "3.142,B2,\"C\t2\"\r\n" +
                    "3.142,B3,\"C 3\"\r\n";

            var items = new List<DtoMany>
                        {
                                new DtoMany
                                {
                                        A = Math.PI,
                                        B = "B1",
                                        C = "C,1"
                                },
                                new DtoMany
                                {
                                        A = Math.PI,
                                        B = "B2",
                                        C = "C\t2"
                                },
                                new DtoMany
                                {
                                        A = Math.PI,
                                        B = "B3",
                                        C = "C 3"
                                }
                        };

            _csv.NumberFormat = "F3";
            var result = _csv.ToCsv(items);
            Assert.AreEqual(expected, result);
        }

        [Test]
        public void ManyProperties_ManyItems_IncludeHeader()
        {
            var expected =
                    "A,B,C\r\n" +
                    "3.142,B1,\"C,1\"\r\n" +
                    "3.142,B2,\"C\t2\"\r\n" +
                    "3.142,B3,\"C 3\"\r\n";

            var items = new List<DtoMany>
                        {
                                new DtoMany
                                {
                                        A = Math.PI,
                                        B = "B1",
                                        C = "C,1"
                                },
                                new DtoMany
                                {
                                        A = Math.PI,
                                        B = "B2",
                                        C = "C\t2"
                                },
                                new DtoMany
                                {
                                        A = Math.PI,
                                        B = "B3",
                                        C = "C 3"
                                }
                        };

            _csv.Header = true;
            _csv.NumberFormat = "F3";
            var result = _csv.ToCsv(items);
            Assert.AreEqual(expected, result);
        }

        [SetUp]
        public void Setup()
        {
            _csv = new CsvWriter();
        }

        [Test]
        public void StringProperty_ManyItems()
        {
            var items = new List<DtoString>
                        {
                                new DtoString { A = "StringA" },
                                new DtoString { A = "StringB" }
                        };
            var result = _csv.ToCsv(items);
            Assert.AreEqual("StringA\r\nStringB\r\n", result);
        }

        [Test]
        public void StringProperty_OneItem()
        {
            var dto = new DtoString();
            dto.A = "StringA";

            var result = _csv.ToCsv(new List<DtoString> { dto });
            Assert.AreEqual("StringA\r\n", result);
        }
    }
}