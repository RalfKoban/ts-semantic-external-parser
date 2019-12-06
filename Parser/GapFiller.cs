using System.Collections.Generic;
using System.Linq;

using MiKoSolutions.SemanticParsers.TypeScript.Yaml;

namespace MiKoSolutions.SemanticParsers.TypeScript
{
    public static class GapFiller
    {
        public static void Fill(File file, CharacterPositionFinder finder)
        {
            var children = file.Children;
            for (var index = 0; index < children.Count; index++)
            {
                AdjustNode(children, index, finder);
            }
        }

        private static void AdjustNode(IList<ContainerOrTerminalNode> parentChildren, int indexInParentChildren, CharacterPositionFinder finder)
        {
            var child = parentChildren[indexInParentChildren];

            if (parentChildren.Count == 1)
            {
                AdjustSingleChild(child, finder);
            }
            else
            {
                // first child, so adjust end-position to next sibling
                if (indexInParentChildren == 0 && parentChildren.Count > 0)
                {
                    AdjustFirstChild(child, parentChildren, indexInParentChildren, finder);
                }

                // child between first and last one, adjust gaps to previous sibling
                if (indexInParentChildren > 0 && indexInParentChildren < parentChildren.Count - 1)
                {
                    AdjustMiddleChild(child, parentChildren, indexInParentChildren, finder);
                }

                // last child, adjust start-position and end-position (on same line)
                if (indexInParentChildren == parentChildren.Count - 1)
                {
                    AdjustLastChild(child, parentChildren, indexInParentChildren, finder);
                }
            }

            if (child is Container c)
            {
                AdjustContainerChild(c, finder);
            }
            else if (child is TerminalNode t)
            {
                AdjustTerminalNodeChild(t, finder);
            }
        }

        private static void AdjustSingleChild(ContainerOrTerminalNode child, CharacterPositionFinder finder)
        {
            // TODO: RKN find out how to adjust
        }

        private static void AdjustFirstChild(ContainerOrTerminalNode child, IList<ContainerOrTerminalNode> parentChildren, int indexInParentChildren, CharacterPositionFinder finder)
        {
            var nextSibling = parentChildren[indexInParentChildren + 1];

            var startPos = new LineInfo(child.LocationSpan.Start.LineNumber, 1);
            var endPos = FindNewEndPos(child, nextSibling, finder);

            child.LocationSpan = new LocationSpan(startPos, endPos);
        }

        private static void AdjustMiddleChild(ContainerOrTerminalNode child, IList<ContainerOrTerminalNode> parentChildren, int indexInParentChildren, CharacterPositionFinder finder)
        {
            var previousSibling = parentChildren[indexInParentChildren - 1];
            var nextSibling = parentChildren[indexInParentChildren + 1];

            var indexAfter = finder.GetCharacterPosition(previousSibling.LocationSpan.End) + 1;
            var startPos = finder.GetLineInfo(indexAfter);
            var endPos = FindNewEndPos(child, nextSibling, finder);

            child.LocationSpan = new LocationSpan(startPos, endPos);
        }

        private static void AdjustLastChild(ContainerOrTerminalNode child, IList<ContainerOrTerminalNode> parentChildren, int indexInParentChildren, CharacterPositionFinder finder)
        {
            var previousSibling = parentChildren[indexInParentChildren - 1];

            var indexAfter = finder.GetCharacterPosition(previousSibling.LocationSpan.End) + 1;
            var startPos = finder.GetLineInfo(indexAfter);
            var endPos = GetLineEnd(child.LocationSpan.End.LineNumber, finder);

            child.LocationSpan = new LocationSpan(startPos, endPos);
        }

        private static void AdjustContainerChild(Container child, CharacterPositionFinder finder)
        {
            AdjustChildren(child, finder);

            switch (child.Children.Count)
            {
                case 0:
                {
                    var headerStart = finder.GetCharacterPosition(child.LocationSpan.Start);
                    var headerEnd = child.HeaderSpan.End;
                    var footerStart = child.FooterSpan.Start;
                    var footerEnd = finder.GetCharacterPosition(child.LocationSpan.End);
                    child.HeaderSpan = new CharacterSpan(headerStart, headerEnd);
                    child.FooterSpan = new CharacterSpan(footerStart, footerEnd);
                    break;
                }

                case 1:
                    break;

                default:
                {
                    var firstChild = child.Children.First();
                    var lastChild = child.Children.Last();

                    var headerStartLine = firstChild.LocationSpan.Start.LineNumber - 1;

                    var headerStart = child.HeaderSpan.Start;
                    var headerEnd = finder.GetCharacterPosition(headerStartLine, finder.GetLineLength(headerStartLine));

                    var footerStartLine = lastChild.LocationSpan.End.LineNumber + 1;
                    var footerStart = finder.GetCharacterPosition(footerStartLine, 1);
                    var footerEnd = finder.GetCharacterPosition(child.LocationSpan.End);

                    child.HeaderSpan = new CharacterSpan(headerStart, headerEnd);
                    child.FooterSpan = new CharacterSpan(footerStart, footerEnd);
                    break;
                }
            }
        }

        private static void AdjustTerminalNodeChild(TerminalNode child, CharacterPositionFinder finder)
        {
            var start = finder.GetCharacterPosition(child.LocationSpan.Start);
            var end = finder.GetCharacterPosition(child.LocationSpan.End);
            child.Span = new CharacterSpan(start, end);
        }

        private static void AdjustChildren(Container container, CharacterPositionFinder finder)
        {
            var children = container.Children;

            for (var index = 0; index < children.Count; index++)
            {
                AdjustNode(children, index, finder);
            }
        }

        private static LineInfo FindNewEndPos(ContainerOrTerminalNode child, ContainerOrTerminalNode nextSibling, CharacterPositionFinder finder)
        {
            var endPos = child.LocationSpan.End;
            var endPosLineNumber = endPos.LineNumber;

            return endPosLineNumber < nextSibling.LocationSpan.Start.LineNumber
                    ? GetLineEnd(endPosLineNumber, finder)
                    : endPos;
        }

        private static LineInfo GetLineEnd(int lineNumber, CharacterPositionFinder finder)
        {
            var length = finder.GetLineLength(lineNumber);
            return new LineInfo(lineNumber, length);
        }
    }
}