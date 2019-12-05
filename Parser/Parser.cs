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
            if (node is ExpressionStatement es)
            {
                return ParseExpressionStatement(es, finder);
            }

            if (node is ClassDeclaration cd)
            {
                return ParseClassDeclaration(cd, finder);
            }

            return null;
        }

        private static ContainerOrTerminalNode ParseClassDeclaration(ClassDeclaration node, CharacterPositionFinder finder)
        {
            var identifier = node.First;
            var nodeEnd = GetNodeEnd(node);

            var lineInfo = finder.GetLineInfo(nodeEnd);
            var length = finder.GetLineLength(lineInfo);
            var footerEnd = finder.GetCharacterPosition(lineInfo.LineNumber, length);

            var container = new Container
                                {
                                    Name = node.IdentifierStr,
                                    Type = GetType(node),
                                    HeaderSpan = new CharacterSpan(GetNodeStart(node), GetNodeEnd(identifier)),
                                    FooterSpan = new CharacterSpan(nodeEnd, footerEnd), // is this the issue here ???
                                    LocationSpan = GetLocationSpan(node, finder),
                                };

            foreach (var child in node.Children)
            {
                switch (child.Kind)
                {
                    case SyntaxKind.EndOfFileToken:
                    case SyntaxKind.Identifier:
                        continue;

                    case SyntaxKind.Constructor:
                    case SyntaxKind.PropertyDeclaration:
                    case SyntaxKind.MethodDeclaration:
                    default:
                    {
                        var item = ParseTerminalNode(child, finder);
                        container.Children.Add(item);
                        break;
                    }
                }
            }

            return container;
        }

        private static TerminalNode ParseExpressionStatement(ExpressionStatement node, CharacterPositionFinder finder)
        {
            var assignment = node.GetDescendants().OfType<PropertyAccessExpression>().FirstOrDefault();

            return new TerminalNode
                       {
                           Name = assignment.GetText(),
                           Type = GetType(node),
                           Span = GetCharacterSpan(node),
                           LocationSpan = GetLocationSpan(node, finder),
                       };
        }

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
                case SyntaxKind.ClassDeclaration: return "class";
                case SyntaxKind.PropertyDeclaration: return "property";
                case SyntaxKind.MethodDeclaration: return "method";
                case SyntaxKind.Constructor: return "constructor";
                case SyntaxKind.ExpressionStatement: return "expression";
                default: return kind.ToString();
            }
        }

        private static int GetNodeStart(Node node) => node.NodeStart;

        private static int GetNodeEnd(Node node) => node.End.GetValueOrDefault() - 1;

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