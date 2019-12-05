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

        private static void AdjustChildren(Container container, CharacterPositionFinder finder)
        {
            var children = container.Children;

            for (var index = 0; index < children.Count; index++)
            {
                AdjustNode(children, index, finder);
            }
        }

        private static void AdjustNode(IList<ContainerOrTerminalNode> parentChildren, int indexInParentChildren, CharacterPositionFinder finder)
        {
            var child = parentChildren[indexInParentChildren];

            // first child, so adjust end-position to next sibling
            if (indexInParentChildren == 0 && parentChildren.Count > 0)
            {
                var nextSibling = parentChildren[indexInParentChildren + 1];

                var startPos = new LineInfo(child.LocationSpan.Start.LineNumber, 1);
                var endPos = FindNewEndPos(child, nextSibling, finder);

                child.LocationSpan = new LocationSpan(startPos, endPos);
            }

            // child between first and last one, adjust gaps to previous sibling
            if (indexInParentChildren > 0 && indexInParentChildren < parentChildren.Count - 1)
            {
                var previousSibling = parentChildren[indexInParentChildren - 1];

                var indexAfter = finder.GetCharacterPosition(previousSibling.LocationSpan.End) + 1;
                var newStartPos = finder.GetLineInfo(indexAfter);

                var nextSibling = parentChildren[indexInParentChildren + 1];
                var newEndPos = FindNewEndPos(child, nextSibling, finder);

                var endLineNumber = child.LocationSpan.End.LineNumber;
                if (newEndPos.LineNumber != endLineNumber)
                {
                    newEndPos = new LineInfo(endLineNumber, finder.GetLineLength(endLineNumber));
                }

                child.LocationSpan = new LocationSpan(newStartPos, newEndPos);
            }

            if (child is Container c)
            {
                AdjustChildren(c, finder);

                if (c.Children.Any())
                {
                    var firstChild = c.Children.First();
                    var lastChild = c.Children.Last();

                    var headerStartLine = firstChild.LocationSpan.Start.LineNumber - 1;

                    var headerStart = c.HeaderSpan.Start;
                    var headerEnd = finder.GetCharacterPosition(headerStartLine, finder.GetLineLength(headerStartLine));

                    var footerStartLine = lastChild.LocationSpan.End.LineNumber + 1;
                    var footerStart = finder.GetCharacterPosition(footerStartLine, 1);
                    var footerEnd = finder.GetCharacterPosition(c.LocationSpan.End);

                    c.HeaderSpan = new CharacterSpan(headerStart, headerEnd);
                    c.FooterSpan = new CharacterSpan(footerStart, footerEnd);
                }
            }
            else if (child is TerminalNode t)
            {
                var start = finder.GetCharacterPosition(t.LocationSpan.Start);
                var end = finder.GetCharacterPosition(t.LocationSpan.End);
                t.Span = new CharacterSpan(start, end);
            }
        }

        private static LineInfo FindNewEndPos(ContainerOrTerminalNode child, ContainerOrTerminalNode nextSibling, CharacterPositionFinder finder)
        {
            var newEndPos = child.LocationSpan.End;

            if (newEndPos.LineNumber < nextSibling.LocationSpan.Start.LineNumber)
            {
                var lineBefore = nextSibling.LocationSpan.Start.LineNumber - 1;
                var length = finder.GetLineLength(lineBefore);
                newEndPos = new LineInfo(lineBefore, length);
            }

            return newEndPos;
        }
    }
}