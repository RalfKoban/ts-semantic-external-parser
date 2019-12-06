using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

using MiKoSolutions.SemanticParsers.TypeScript.Yaml;

using Zu.TypeScript;
using Zu.TypeScript.TsTypes;

using File = MiKoSolutions.SemanticParsers.TypeScript.Yaml.File;
using SystemFile = System.IO.File;

namespace MiKoSolutions.SemanticParsers.TypeScript
{
    public class Parser
    {
        public static File Parse(string filePath, string encoding = "UTF-8")
        {
            var encodingToUse = Encoding.GetEncoding(encoding);

            File file;
            using (var finder = CharacterPositionFinder.CreateFrom(filePath, encodingToUse))
            {
                file = ParseCore(filePath, finder, encodingToUse);

                Resorter.Resort(file);

                GapFiller.Fill(file, finder);
            }

            return file;
        }

        public static File ParseCore(string filePath, CharacterPositionFinder finder, Encoding encoding)
        {
            var fileName = Path.GetFileName(filePath);
            var source = SystemFile.ReadAllText(filePath, encoding);

            var ast = new TypeScriptAST(source, fileName);

            var rootNode = ast.RootNode;

            var file = new File
                           {
                               Name = fileName,
                               FooterSpan = CharacterSpan.None, // there is no footer
                               LocationSpan = GetLocationSpan(rootNode, finder),
                           };

            foreach (var child in rootNode.Children.Where(_ => _.Kind != SyntaxKind.EndOfFileToken))
            {
                var node = ParseNodeForType(child, finder);
                file.Children.Add(node);
            }

            return file;
        }

        private static ContainerOrTerminalNode ParseNodeForType(Node node, CharacterPositionFinder finder)
        {
            switch (node)
            {
                case ClassDeclaration cd:
                    return ParseClassDeclaration(cd, finder);

                case EnumDeclaration ed:
                    return ParseEnumDeclaration(ed, finder);

                case ExpressionStatement es:
                    return ParseExpressionStatement(es, finder);
                
                case ImportDeclaration id:
                    return ParseImportDeclaration(id, finder);

                case VariableStatement vs:
                    return ParseVariableStatement(vs, finder).First();
                
                default:
                    Tracer.Trace($"Special handing missing for '{node.Kind}'");
                    return ParseTerminalNode(node, finder);
            }
        }

        private static ContainerOrTerminalNode ParseNodeForKind(Node node, CharacterPositionFinder finder)
        {
            switch (node.Kind)
            {
                case SyntaxKind.EndOfFileToken:
                case SyntaxKind.Identifier:
                case SyntaxKind.ExportKeyword:
                case SyntaxKind.HeritageClause:
                case SyntaxKind.Decorator:
                    return null;

                case SyntaxKind.GetAccessor:
                case SyntaxKind.SetAccessor:
                case SyntaxKind.Constructor:
                case SyntaxKind.PropertyDeclaration:
                case SyntaxKind.MethodDeclaration:
                {
                    return ParseTerminalNode(node, finder);
                }

                default:
                    Tracer.Trace($"Cannot handle '{node.Kind}'");
                    return null;
            }
        }

        private static ContainerOrTerminalNode ParseClassDeclaration(ClassDeclaration node, CharacterPositionFinder finder)
        {
            var headerSpan = GetHeaderSpan(node, finder);
            var footerSpan = GetFooterSpan(node, finder);

            var container = new Container
                                {
                                    Name = node.IdentifierStr,
                                    Type = GetType(node),
                                    HeaderSpan = headerSpan,
                                    FooterSpan = footerSpan,
                                    LocationSpan = GetLocationSpan(node, finder),
                                };

            foreach (var child in node.Children)
            {
                var item = ParseNodeForKind(child, finder);
                if (item != null)
                {
                    container.Children.Add(item);
                }
            }

            return container;
        }

        private static ContainerOrTerminalNode ParseEnumDeclaration(EnumDeclaration node, CharacterPositionFinder finder)
        {
            var headerSpan = GetHeaderSpan(node, finder);
            var footerSpan = GetFooterSpan(node, finder);

            var container = new Container
                                {
                                    Name = node.IdentifierStr,
                                    Type = GetType(node),
                                    HeaderSpan = headerSpan,
                                    FooterSpan = footerSpan,
                                    LocationSpan = GetLocationSpan(node, finder),
                                };

            foreach (var child in node.Children.OfType<EnumMember>())
            {
                var item = ParseEnumMember(child, finder);
                container.Children.Add(item);
            }

            return container;
        }

        private static TerminalNode ParseEnumMember(EnumMember node, CharacterPositionFinder finder)
        {
            var name = node.IdentifierStr;
            var type = GetType(node);

            var nodeStart = GetNodeStart(node);
            var nodeEnd = GetNodeEnd(node, 0);

            var start = finder.GetLineInfo(nodeStart);
            var end = finder.GetLineInfo(nodeEnd);

            return new TerminalNode
                       {
                           Name = name,
                           Type = type,
                           Span = new CharacterSpan(nodeStart, nodeEnd),
                           LocationSpan = new LocationSpan(start, end),
                       };
        }

        private static ContainerOrTerminalNode ParseExpressionStatement(ExpressionStatement node, CharacterPositionFinder finder)
        {
            switch (node.Expression)
            {
                case CallExpression c when c.IdentifierStr == IdentifierNames.Describe || c.IdentifierStr == IdentifierNames.DescribeDisabled:
                {
                    return ParseDescribeTestExpression(c, finder);
                }

                case BinaryExpression b when b.First is PropertyAccessExpression p:
                {
                    return new TerminalNode
                               {
                                   Name = p.GetText(),
                                   Type = GetType(node),
                                   Span = GetCharacterSpan(node),
                                   LocationSpan = GetLocationSpan(node, finder),
                               };
                }
                default:
                {
                    // TODO RKN: Are there more ???
                    Tracer.Trace($"Special handing missing for expression '{node.Kind}'");
                    return ParseTerminalNode(node, finder);
                }
            }
        }

        private static TerminalNode ParseImportDeclaration(ImportDeclaration node, CharacterPositionFinder finder)
        {
            var name = node.GetText();
            if (node.First is StringLiteral s)
            {
                name = s.GetText();
            }
            else if (node.First is ImportClause)
            {
                name = node.Last.GetText();
            }

            return new TerminalNode
                       {
                           Name = name,
                           Type = GetType(node),
                           Span = GetCharacterSpan(node),
                           LocationSpan = GetLocationSpan(node, finder),
                       };
        }

        private static ContainerOrTerminalNode ParseDescribeTestExpression(CallExpression node, CharacterPositionFinder finder)
        {
            var name = node.Arguments.OfType<StringLiteral>().FirstOrDefault()?.Text;

            var headerStart = node.NodeStart;
            var headerEnd = node.Arguments.Last().End.GetValueOrDefault();

            var container = new Container
                                {
                                    Name = name,
                                    Type = IdentifierNames.Describe,
                                    HeaderSpan = new CharacterSpan(headerStart, headerEnd),
                                    LocationSpan = GetLocationSpan(node.Parent, finder),
                                };

            foreach (var child in node.Children.OfType<ArrowFunction>().SelectMany(_ => _.Body.Children))
            {
                switch (child)
                {
                    case VariableStatement v:
                    {
                        container.Children.AddRange(ParseVariableStatement(v, finder));
                        break;
                    }
                    case ExpressionStatement e when e.Expression is CallExpression ca:
                    {
                        var method = ParseTestExpression(ca, finder);
                        if (method != null)
                        {
                            container.Children.Add(method);
                        }

                        break;
                    }
                    case FunctionDeclaration f:
                    {
                        var t  = ParseFunctionDeclaration(f, finder);
                        container.Children.Add(t);
                        break;
                    }

                    default:
                        Tracer.Trace($"Cannot handle '{child.Kind}'"); // TODO RKN: GetAccessor
                        break;
                }
            }

            return container;
        }

        private static IEnumerable<TerminalNode> ParseVariableStatement(VariableStatement node, CharacterPositionFinder finder)
        {
            var variables = new List<TerminalNode>();
            var variableDeclarationList = node.DeclarationList;

            foreach (var declaration in variableDeclarationList.Declarations)
            {
                variables.Add(new TerminalNode
                                  {
                                      Name = declaration.IdentifierStr,
                                      Type = GetType(declaration),
                                      Span = GetCharacterSpan(node),
                                      LocationSpan = GetLocationSpan(node, finder),
                                  });
            }
            return variables;
        }

        private static ContainerOrTerminalNode ParseTestExpression(CallExpression node, CharacterPositionFinder finder)
        {
            switch (node.IdentifierStr)
            {
                case IdentifierNames.AfterAll:
                case IdentifierNames.AfterEach:
                case IdentifierNames.BeforeAll:
                case IdentifierNames.BeforeEach:
                {
                    return new TerminalNode
                               {
                                   Name = node.IdentifierStr,
                                   Type = node.IdentifierStr,
                                   Span = GetCharacterSpan(node),
                                   LocationSpan = GetLocationSpan(node.Parent, finder),
                               };
                }
                case IdentifierNames.Describe:
                case IdentifierNames.DescribeDisabled:
                {
                    return ParseDescribeTestExpression(node, finder);
                }
                case IdentifierNames.It:
                case IdentifierNames.ItDisabled:
                case IdentifierNames.Test:
                {
                    var testName = node.Arguments.OfType<StringLiteral>().FirstOrDefault()?.Text;

                    return new TerminalNode
                               {
                                   Name = testName,
                                   Type = IdentifierNames.Test,
                                   Span = GetCharacterSpan(node),
                                   LocationSpan = GetLocationSpan(node.Parent, finder),
                               };
                }

                default:
                    return null; // ignore non-test methods
            }
        }

        private static TerminalNode ParseFunctionDeclaration(FunctionDeclaration node, CharacterPositionFinder finder) => ParseTerminalNode(node, finder);

        private static TerminalNode ParseTerminalNode(Node node, CharacterPositionFinder finder)
        {
            var name = node.Kind is SyntaxKind.Constructor ? "constructor" : node.IdentifierStr;
            var type = GetType(node);

            return new TerminalNode
                       {
                           Name = name,
                           Type = type,
                           Span = GetCharacterSpan(node),
                           LocationSpan = GetLocationSpan(node, finder),
                       };
        }

        private static string GetType(Node node)
        {
            var kind = node.Kind;
            switch (kind)
            {
                case SyntaxKind.CallExpression: return "call";
                case SyntaxKind.ClassDeclaration: return "class";
                case SyntaxKind.EnumDeclaration: return "enum";
                case SyntaxKind.EnumMember: return "enum member";
                case SyntaxKind.ExpressionStatement: return "expression";
                case SyntaxKind.FunctionDeclaration: return "function";
                case SyntaxKind.ImportDeclaration: return "import";
                case SyntaxKind.Constructor:
                case SyntaxKind.MethodDeclaration: return "method";
                case SyntaxKind.PropertyDeclaration: return "property";
                case SyntaxKind.VariableDeclaration: return node.Parent.Flags == NodeFlags.Const ? "const" : "variable";
                case SyntaxKind.GetAccessor: return "getter";
                case SyntaxKind.SetAccessor: return "setter";

                default:
                    return kind.ToString();
            }
        }

        private static CharacterSpan GetHeaderSpan(Node node, CharacterPositionFinder finder)
        {
            var headerSpanStart = GetNodeStart((Node) (node.First as Decorator) ?? node);
            var headerSpanEnd = GetNodeEnd(node.Children.OfType<Identifier>().First());
            var headerSpan = new CharacterSpan(headerSpanStart, headerSpanEnd);
            return headerSpan;
        }

        private static CharacterSpan GetFooterSpan(Node node, CharacterPositionFinder finder)
        {
            var nodeEnd = GetNodeEnd(node);
            var lineInfo = finder.GetLineInfo(nodeEnd);
            var length = finder.GetLineLength(lineInfo);
            var footerEnd = finder.GetCharacterPosition(lineInfo.LineNumber, length);
            var characterSpan = new CharacterSpan(nodeEnd, footerEnd);
            return characterSpan;
        }

        private static LocationSpan GetLocationSpan(INode node, CharacterPositionFinder finder) => GetLocationSpan((Node) node, finder);

        private static LocationSpan GetLocationSpan(Node node, CharacterPositionFinder finder)
        {
            var nodeStart = GetNodeStart(node);
            var nodeEnd = GetNodeEnd(node);

            var start = finder.GetLineInfo(nodeStart);
            var end = finder.GetLineInfo(nodeEnd);

            return new LocationSpan(start, end);
        }

        private static CharacterSpan GetCharacterSpan(Node node)
        {
            var nodeStart = GetNodeStart(node);
            var nodeEnd = GetNodeEnd(node);

            return new CharacterSpan(nodeStart, nodeEnd);
        }

        private static int GetNodeStart(Node node) => node.NodeStart;

        private static int GetNodeEnd(Node node, int correction = -1) => node.End.GetValueOrDefault() + correction;

        private static class IdentifierNames
        {
            public const string AfterAll = "afterAll";
            public const string AfterEach = "afterEach";
            public const string BeforeAll = "beforeAll";
            public const string BeforeEach = "beforeEach";
            public const string Describe = "describe";
            public const string DescribeDisabled = "xdescribe";
            public const string It = "it";
            public const string ItDisabled = "xit";
            public const string Test = "test";
        }
    }
}