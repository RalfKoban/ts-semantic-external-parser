using System;
using System.IO;
using System.Linq;

using MiKoSolutions.SemanticParsers.TypeScript.Yaml;

using NUnit.Framework;

namespace MiKoSolutions.SemanticParsers.TypeScript
{
    [TestFixture]
    public class ParserTests_AppTs
    {
        private Yaml.File _objectUnderTest;

        [SetUp]
        public void PrepareTest()
        {
            var parentDirectory = Directory.GetParent(new Uri(GetType().Assembly.Location).LocalPath).FullName;
            var fileName = Path.Combine(parentDirectory, "Resources", "app.ts");

            _objectUnderTest = Parser.Parse(fileName);
        }

        [Test]
        public void Name_matches() => Assert.That(_objectUnderTest.Name, Is.EqualTo("app.ts"));

        [Test]
        public void FileInfo_matches()
        {
            Assert.Multiple(() =>
            {
                Assert.That(_objectUnderTest.ParsingErrorsDetected, Is.False, "Parsing errors");
                Assert.That(_objectUnderTest.LocationSpan, Is.EqualTo(new LocationSpan(new LineInfo(0, -1), new LineInfo(28, 2))), "Wrong location span");
                Assert.That(_objectUnderTest.FooterSpan, Is.EqualTo(CharacterSpan.None), "Wrong footer span");
            });
        }

        [Test]
        public void ClassDeclaration_matches()
        {
            Assert.Multiple(() =>
            {
                var declaration = (Container)_objectUnderTest.Children.First();

                Assert.That(declaration.Name, Is.EqualTo("Greeter"));
                Assert.That(declaration.Type, Is.EqualTo("class"));
                Assert.That(declaration.LocationSpan, Is.EqualTo(new LocationSpan(new LineInfo(1, 1), new LineInfo(23, 2))), "Wrong location span");
                Assert.That(declaration.HeaderSpan, Is.EqualTo(new CharacterSpan(0, 16)), "Wrong header span");
                Assert.That(declaration.FooterSpan, Is.EqualTo(new CharacterSpan(572, 576)), "Wrong footer span");
            });
        }

        [Test]
        public void Element_PropertyDeclaration_matches()
        {
            Assert.Multiple(() =>
            {
                var declaration = (Container)_objectUnderTest.Children.First();
                var element = (TerminalNode)declaration.Children[0];

                Assert.That(element.Name, Is.EqualTo("element"));
                Assert.That(element.Type, Is.EqualTo("property"));
                Assert.That(element.LocationSpan, Is.EqualTo(new LocationSpan(new LineInfo(2, 1), new LineInfo(2, 27))), "Wrong location span");
                Assert.That(element.Span, Is.EqualTo(new CharacterSpan(17, 43)), "Wrong span");
            });
        }

        [Test]
        public void Span_PropertyDeclaration_matches()
        {
            Assert.Multiple(() =>
            {
                var declaration = (Container)_objectUnderTest.Children.First();
                var element = (TerminalNode)declaration.Children[1];

                Assert.That(element.Name, Is.EqualTo("span"));
                Assert.That(element.Type, Is.EqualTo("property"));
                Assert.That(element.LocationSpan, Is.EqualTo(new LocationSpan(new LineInfo(3, 1), new LineInfo(3, 24))), "Wrong location span");
                Assert.That(element.Span, Is.EqualTo(new CharacterSpan(44, 67)), "Wrong span");
            });
        }

        [Test]
        public void TimerToken_PropertyDeclaration_matches()
        {
            Assert.Multiple(() =>
            {
                var declaration = (Container)_objectUnderTest.Children.First();
                var element = (TerminalNode)declaration.Children[2];

                Assert.That(element.Name, Is.EqualTo("timerToken"));
                Assert.That(element.Type, Is.EqualTo("property"));
                Assert.That(element.LocationSpan, Is.EqualTo(new LocationSpan(new LineInfo(4, 1), new LineInfo(4, 25))), "Wrong location span");
                Assert.That(element.Span, Is.EqualTo(new CharacterSpan(68, 92)), "Wrong span");
            });
        }

        [Test]
        public void Constructor_matches()
        {
            Assert.Multiple(() =>
            {
                var declaration = (Container)_objectUnderTest.Children.First();
                var element = (TerminalNode)declaration.Children[3];

                Assert.That(element.Name, Is.EqualTo("constructor"));
                Assert.That(element.Type, Is.EqualTo("constructor"));
                Assert.That(element.LocationSpan, Is.EqualTo(new LocationSpan(new LineInfo(12, 7), new LineInfo(4, 25))), "Wrong location span");
                Assert.That(element.Span, Is.EqualTo(new CharacterSpan(95, 383)), "Wrong span");
            });
        }

        [Test]
        public void ExpressionStatement_matches()
        {
            Assert.Multiple(() =>
            {
                var decaration = (TerminalNode)_objectUnderTest.Children.Last();

                Assert.That(decaration.Name, Is.EqualTo("window.onload"));
                Assert.That(decaration.Type, Is.EqualTo("expression"));
                Assert.That(decaration.LocationSpan, Is.EqualTo(new LocationSpan(new LineInfo(24, 1), new LineInfo(28, 2))), "Wrong location span");
                Assert.That(decaration.Span, Is.EqualTo(new CharacterSpan(577, 711)), "Wrong span");
            });
        }
    }
}
