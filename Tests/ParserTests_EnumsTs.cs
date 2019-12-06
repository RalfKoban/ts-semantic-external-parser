using System;
using System.IO;
using System.Linq;

using MiKoSolutions.SemanticParsers.TypeScript.Yaml;

using NUnit.Framework;

namespace MiKoSolutions.SemanticParsers.TypeScript
{
    [TestFixture]
    public class ParserTests_EnumsTs
    {
        private Yaml.File _objectUnderTest;

        [SetUp]
        public void PrepareTest()
        {
            var parentDirectory = Directory.GetParent(new Uri(GetType().Assembly.Location).LocalPath).FullName;
            var fileName = Path.Combine(parentDirectory, "Resources", "enums.ts");

            _objectUnderTest = Parser.Parse(fileName);
        }

        [Test]
        public void Name_matches() => Assert.That(_objectUnderTest.Name, Is.EqualTo("enums.ts"));

        [Test]
        public void FileInfo_matches()
        {
            Assert.Multiple(() =>
            {
                Assert.That(_objectUnderTest.ParsingErrorsDetected, Is.False, "Parsing errors");
                Assert.That(_objectUnderTest.LocationSpan, Is.EqualTo(new LocationSpan(new LineInfo(0, -1), new LineInfo(3, 1))), "Wrong location span");
                Assert.That(_objectUnderTest.FooterSpan, Is.EqualTo(CharacterSpan.None), "Wrong footer span");
            });
        }

        [Test]
        public void Enum_matches()
        {
            Assert.Multiple(() =>
            {
                var enumDeclaration = (Container)_objectUnderTest.Children.Single();

                Assert.That(enumDeclaration.Name, Is.EqualTo("StateTypes"));
                Assert.That(enumDeclaration.Type, Is.EqualTo("enum"));
                Assert.That(enumDeclaration.LocationSpan, Is.EqualTo(new LocationSpan(new LineInfo(1, 1), new LineInfo(3, 1))), "Wrong location span");
                Assert.That(enumDeclaration.HeaderSpan, Is.EqualTo(new CharacterSpan(0, 25)), "Wrong header span");
                Assert.That(enumDeclaration.FooterSpan, Is.EqualTo(new CharacterSpan(55, 55)), "Wrong footer span");
            });
        }

        [TestCase(0, "None", 26, 34)]
        [TestCase(1, "Active", 35, 42)]
        [TestCase(2, "NonActive", 43, 54)]
        public void Enum_members_matches(int index, string name, int spanStart, int spanEnd)
        {
            Assert.Multiple(() =>
            {
                var enumDeclaration = (Container)_objectUnderTest.Children.Single();
                var enumMembers = enumDeclaration.Children;

                Assert.That(enumMembers, Has.Count.EqualTo(3));

                var import = (TerminalNode)enumMembers[index];

                Assert.That(import.Name, Is.EqualTo(name));
                Assert.That(import.Type, Is.EqualTo("enum member"));
                Assert.That(import.Span, Is.EqualTo(new CharacterSpan(spanStart, spanEnd)), "Wrong span");
            });
        }
    }
}