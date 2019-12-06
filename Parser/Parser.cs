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
            var source = SystemFile.ReadAllText(filePath);

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
                var node = ParseNode(child, finder);
                file.Children.Add(node);
            }

            return file;
        }

        private static ContainerOrTerminalNode ParseNode(Node node, CharacterPositionFinder finder)
        {
            switch (node)
            {
                case ClassDeclaration cd:
                    return ParseClassDeclaration(cd, finder);

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

        private static ContainerOrTerminalNode ParseClassDeclaration(ClassDeclaration node, CharacterPositionFinder finder)
        {
            // get header span
            var headerSpanStart = GetNodeStart((Node)(node.First as Decorator) ?? node);
            var headerSpanEnd = GetNodeEnd(node.Children.OfType<Identifier>().First());
            var headerSpan = new CharacterSpan(headerSpanStart, headerSpanEnd);

            // get footer span
            var nodeEnd = GetNodeEnd(node);
            var lineInfo = finder.GetLineInfo(nodeEnd);
            var length = finder.GetLineLength(lineInfo);
            var footerEnd = finder.GetCharacterPosition(lineInfo.LineNumber, length);
            var characterSpan = new CharacterSpan(nodeEnd, footerEnd);

            var container = new Container
                                {
                                    Name = node.IdentifierStr,
                                    Type = GetType(node),
                                    HeaderSpan = headerSpan,
                                    FooterSpan = characterSpan,
                                    LocationSpan = GetLocationSpan(node, finder),
                                };

            foreach (var child in node.Children)
            {
                switch (child.Kind)
                {
                    case SyntaxKind.EndOfFileToken:
                    case SyntaxKind.Identifier:
                    case SyntaxKind.ExportKeyword:
                    case SyntaxKind.HeritageClause:
                    case SyntaxKind.Decorator:
                        continue;

                    case SyntaxKind.Constructor:
                    case SyntaxKind.PropertyDeclaration:
                    case SyntaxKind.MethodDeclaration:
                    {
                        var item = ParseTerminalNode(child, finder);
                        container.Children.Add(item);
                        continue;
                    }

                    default:
                        Tracer.Trace($"Cannot handle '{child.Kind}'");
                        continue;
                }
            }

            return container;
        }

        private static ContainerOrTerminalNode ParseExpressionStatement(ExpressionStatement node, CharacterPositionFinder finder)
        {
            switch (node.Expression)
            {
                case CallExpression c when c.IdentifierStr == "describe":
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
                                    Type = node.IdentifierStr,
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
                        Tracer.Trace($"Cannot handle '{child.Kind}'");
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
                case "beforeEach":
                {
                    return new TerminalNode
                               {
                                   Name = node.IdentifierStr,
                                   Type = node.IdentifierStr,
                                   Span = GetCharacterSpan(node),
                                   LocationSpan = GetLocationSpan(node.Parent, finder),
                               };
                }
                case "it":
                {
                    var testName = node.Arguments.OfType<StringLiteral>().FirstOrDefault()?.Text;

                    return new TerminalNode
                                {
                                    Name = testName,
                                    Type = node.IdentifierStr,
                                    Span = GetCharacterSpan(node),
                                    LocationSpan = GetLocationSpan(node.Parent, finder),
                                };
                }

                case "describe":
                {
                    return ParseDescribeTestExpression(node, finder);
                }

                default:
                    return null; // ignore non-test methods
            }
        }

        private static TerminalNode ParseFunctionDeclaration(FunctionDeclaration node, CharacterPositionFinder finder) => ParseTerminalNode(node, finder);

        private static TerminalNode ParseTerminalNode(Node node, CharacterPositionFinder finder)
        {
            var name = node.IdentifierStr;
            var type = GetType(node);

            if (string.IsNullOrEmpty(name))
            {
                name = type;
            }

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
                case SyntaxKind.Constructor: return "constructor";
                case SyntaxKind.ExpressionStatement: return "expression";
                case SyntaxKind.FunctionDeclaration: return "function";
                case SyntaxKind.ImportDeclaration: return "import";
                case SyntaxKind.MethodDeclaration: return "method";
                case SyntaxKind.PropertyDeclaration: return "property";
                case SyntaxKind.VariableDeclaration: return node.Parent.Flags == NodeFlags.Const ? "const" : "variable";
                default:
                    return kind.ToString();
            }
        }

        private static int GetNodeStart(Node node) => node.NodeStart;

        private static int GetNodeEnd(Node node) => node.End.GetValueOrDefault() - 1;

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
    }
}