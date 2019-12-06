using System;
using System.IO;
using System.Linq;

using MiKoSolutions.SemanticParsers.TypeScript.Yaml;

using NUnit.Framework;

namespace MiKoSolutions.SemanticParsers.TypeScript
{
    [TestFixture]
    public class ParserTests_DecoratorsTs
    {
        private Yaml.File _objectUnderTest;

        [SetUp]
        public void PrepareTest()
        {
            var parentDirectory = Directory.GetParent(new Uri(GetType().Assembly.Location).LocalPath).FullName;
            var fileName = Path.Combine(parentDirectory, "Resources", "decorators.ts");

            _objectUnderTest = Parser.Parse(fileName);
        }

        [Test]
        public void Name_matches() => Assert.That(_objectUnderTest.Name, Is.EqualTo("decorators.ts"));

        [Test]
        public void FileInfo_matches()
        {
            Assert.Multiple(() =>
            {
                Assert.That(_objectUnderTest.ParsingErrorsDetected, Is.False, "Parsing errors");
                Assert.That(_objectUnderTest.LocationSpan, Is.EqualTo(new LocationSpan(new LineInfo(0, -1), new LineInfo(9, 22))), "Wrong location span");
                Assert.That(_objectUnderTest.FooterSpan, Is.EqualTo(CharacterSpan.None), "Wrong footer span");
            });
        }

        [Test]
        public void Decorator_matches()
        {
            Assert.Multiple(() =>
            {
                var classDecoration = (Container)_objectUnderTest.Children.Last();
                Assert.That(classDecoration.Name, Is.EqualTo("AppComponent"));
                Assert.That(classDecoration.Type, Is.EqualTo("class"));

                Assert.That(classDecoration.LocationSpan, Is.EqualTo(new LocationSpan(new LineInfo(2, 1), new LineInfo(9, 22))), "Wrong location span");
                Assert.That(classDecoration.HeaderSpan, Is.EqualTo(new CharacterSpan(44, 192)), "Wrong header span");
                Assert.That(classDecoration.FooterSpan, Is.EqualTo(new CharacterSpan(196, 196)), "Wrong footer span");
            });
        }
    }
}