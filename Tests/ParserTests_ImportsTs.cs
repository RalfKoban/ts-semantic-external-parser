using System;
using System.IO;

using MiKoSolutions.SemanticParsers.TypeScript.Yaml;

using NUnit.Framework;

namespace MiKoSolutions.SemanticParsers.TypeScript
{
    [TestFixture]
    public class ParserTests_ImportsTs
    {
        private Yaml.File _objectUnderTest;

        [SetUp]
        public void PrepareTest()
        {
            var parentDirectory = Directory.GetParent(new Uri(GetType().Assembly.Location).LocalPath).FullName;
            var fileName = Path.Combine(parentDirectory, "Resources", "imports.ts");

            _objectUnderTest = Parser.Parse(fileName);
        }

        [Test]
        public void Name_matches() => Assert.That(_objectUnderTest.Name, Is.EqualTo("imports.ts"));

        [Test]
        public void FileInfo_matches()
        {
            Assert.Multiple(() =>
            {
                Assert.That(_objectUnderTest.ParsingErrorsDetected, Is.False, "Parsing errors");
                Assert.That(_objectUnderTest.LocationSpan, Is.EqualTo(new LocationSpan(new LineInfo(0, -1), new LineInfo(7, 31))), "Wrong location span");
                Assert.That(_objectUnderTest.FooterSpan, Is.EqualTo(CharacterSpan.None), "Wrong footer span");
            });
        }

        [TestCase(0, "import", "'rxjs/add/operator/filter'", 0, 35)]
        [TestCase(1, "import", "'rxjs/add/operator/map'", 36, 68)]
        [TestCase(2, "import", "'rxjs/add/operator/mergeMap'", 69, 106)]
        [TestCase(3, "import", "'@angular/core'", 107, 171)]
        [TestCase(4, "import", "'@angular/router'",172, 245)]
        [TestCase(5, "import", "'rxjs'", 246, 276)]
        public void Imports_matches(int index, string type, string name, int spanStart, int spanEnd)
        {
            Assert.Multiple(() =>
            {
                Assert.That(_objectUnderTest.Children, Has.Count.EqualTo(6));

                var import = (TerminalNode)_objectUnderTest.Children[index];

                Assert.That(import.Name, Is.EqualTo(name));
                Assert.That(import.Type, Is.EqualTo(type));
                Assert.That(import.Span, Is.EqualTo(new CharacterSpan(spanStart, spanEnd)), "Wrong span");
            });
        }
    }
}