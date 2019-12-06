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
                Assert.That(declaration.LocationSpan, Is.EqualTo(new LocationSpan(new LineInfo(1, 1), new LineInfo(22, 3))), "Wrong location span");
                Assert.That(declaration.HeaderSpan, Is.EqualTo(new CharacterSpan(0, 16)), "Wrong header span");
                Assert.That(declaration.FooterSpan, Is.EqualTo(new CharacterSpan(570, 574)), "Wrong footer span");
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
                Assert.That(element.Type, Is.EqualTo("field"));
                Assert.That(element.LocationSpan, Is.EqualTo(new LocationSpan(new LineInfo(2, 1), new LineInfo(2, 27))), "Wrong location span");
                Assert.That(element.Span, Is.EqualTo(new CharacterSpan(17, 43)), "Wrong span");
            });
        }

        public void Span_PropertyDeclaration_matches()
        {
            Assert.Multiple(() =>
            {
                var declaration = (Container)_objectUnderTest.Children.First();
                var element = (TerminalNode)declaration.Children[1];

                Assert.That(element.Name, Is.EqualTo("span"));
                Assert.That(element.Type, Is.EqualTo("field"));
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
                Assert.That(element.Type, Is.EqualTo("field"));
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
                Assert.That(element.Type, Is.EqualTo("method"));
                Assert.That(element.LocationSpan, Is.EqualTo(new LocationSpan(new LineInfo(5, 1), new LineInfo(12, 7))), "Wrong location span");
                Assert.That(element.Span, Is.EqualTo(new CharacterSpan(93, 383)), "Wrong span");
            });
        }

        [Test]
        public void start_method_matches()
        {
            Assert.Multiple(() =>
            {
                var declaration = (Container)_objectUnderTest.Children.First();
                var element = (TerminalNode)declaration.Children[4];

                Assert.That(element.Name, Is.EqualTo("start"));
                Assert.That(element.Type, Is.EqualTo("method"));
                Assert.That(element.LocationSpan, Is.EqualTo(new LocationSpan(new LineInfo(13, 1), new LineInfo(16, 7))), "Wrong location span");
                Assert.That(element.Span, Is.EqualTo(new CharacterSpan(384, 506)), "Wrong span");
            });
        }

        [Test]
        public void stop_method_matches()
        {
            Assert.Multiple(() =>
            {
                var declaration = (Container)_objectUnderTest.Children.First();
                var element = (TerminalNode)declaration.Children[5];

                Assert.That(element.Name, Is.EqualTo("stop"));
                Assert.That(element.Type, Is.EqualTo("method"));
                Assert.That(element.LocationSpan, Is.EqualTo(new LocationSpan(new LineInfo(17, 1), new LineInfo(20, 7))), "Wrong location span");
                Assert.That(element.Span, Is.EqualTo(new CharacterSpan(507, 569)), "Wrong span");
            });
        }

        [Test]
        public void ExpressionStatement_matches()
        {
            Assert.Multiple(() =>
            {
                var statement = (TerminalNode)_objectUnderTest.Children.Last();

                Assert.That(statement.Name, Is.EqualTo("window.onload"));
                Assert.That(statement.Type, Is.EqualTo("expression"));
                Assert.That(statement.LocationSpan, Is.EqualTo(new LocationSpan(new LineInfo(23, 1), new LineInfo(28, 2))), "Wrong location span");
                Assert.That(statement.Span, Is.EqualTo(new CharacterSpan(575, 711)), "Wrong span");
            });
        }
    }
}
