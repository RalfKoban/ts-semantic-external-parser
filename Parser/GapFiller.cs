using System.Linq;

using MiKoSolutions.SemanticParsers.TypeScript.Yaml;

namespace MiKoSolutions.SemanticParsers.TypeScript
{
    public static class GapFiller
    {
        public static void Fill(File file, CharacterPositionFinder finder)
        {
            for (var index = 0; index < file.Children.Count; index++)
            {
                AdjustNode(file, index, finder);
            }
        }

        private static void AdjustNode(IParent parent, int indexInParentChildren, CharacterPositionFinder finder)
        {
            var parentChildren = parent.Children;

            var child = parentChildren[indexInParentChildren];

            if (parentChildren.Count == 1)
            {
                AdjustSingleChild(parent, finder);
            }
            else
            {
                // first child, so adjust end-position to next sibling
                if (indexInParentChildren == 0 && parentChildren.Count > 0)
                {
                    AdjustFirstChild(parent, indexInParentChildren, finder);
                }

                // child between first and last one, adjust gaps to previous sibling
                if (indexInParentChildren > 0 && indexInParentChildren < parentChildren.Count - 1)
                {
                    AdjustMiddleChild(parent, indexInParentChildren, finder);
                }

                // last child, adjust start-position and end-position (on same line)
                if (indexInParentChildren == parentChildren.Count - 1)
                {
                    AdjustLastChild(parent, indexInParentChildren, finder);
                }
            }

            if (child is Container c)
            {
                AdjustContainerChild(parent, c, finder);
            }
            else if (child is TerminalNode t)
            {
                AdjustTerminalNodeChild(parent, t, finder);
            }
        }

        private static void AdjustSingleChild(IParent parent, CharacterPositionFinder finder)
        {
            // TODO: RKN find out how to adjust
        }

        private static void AdjustFirstChild(IParent parent, int indexInParentChildren, CharacterPositionFinder finder)
        {
            var parentChildren = parent.Children;
            var child = parentChildren[indexInParentChildren];
            var nextSibling = parentChildren[indexInParentChildren + 1];

            // same line, so start immediately after
            var startPos = parent is Container c && c.LocationSpan.Start.LineNumber == child.LocationSpan.Start.LineNumber
                            ? finder.GetLineInfo(c.HeaderSpan.End + 1)
                            : new LineInfo(child.LocationSpan.Start.LineNumber, 1);

            var endPos = FindNewEndPos(child, nextSibling, finder);

            child.LocationSpan = new LocationSpan(startPos, endPos);
        }

        private static void AdjustMiddleChild(IParent parent, int indexInParentChildren, CharacterPositionFinder finder)
        {
            var parentChildren = parent.Children;
            var child = parentChildren[indexInParentChildren];
            var previousSibling = parentChildren[indexInParentChildren - 1];
            var nextSibling = parentChildren[indexInParentChildren + 1];

            var indexAfter = finder.GetCharacterPosition(previousSibling.LocationSpan.End) + 1;
            var startPos = finder.GetLineInfo(indexAfter);
            var endPos = FindNewEndPos(child, nextSibling, finder);

            child.LocationSpan = new LocationSpan(startPos, endPos);
        }

        private static void AdjustLastChild(IParent parent, int indexInParentChildren, CharacterPositionFinder finder)
        {
            var parentChildren = parent.Children;
            var child = parentChildren[indexInParentChildren];
            var previousSibling = parentChildren[indexInParentChildren - 1];

            var indexAfter = finder.GetCharacterPosition(previousSibling.LocationSpan.End) + 1;
            var startPos = finder.GetLineInfo(indexAfter);
            var endPos = GetLineEnd(child.LocationSpan.End.LineNumber, finder);

            child.LocationSpan = new LocationSpan(startPos, endPos);
        }

        private static void AdjustContainerChild(IParent parent, Container child, CharacterPositionFinder finder)
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

                    var headerOffset = child.LocationSpan.Start.LineNumber == firstChild.LocationSpan.Start.LineNumber
                                         ? 0 // it's on the same line
                                         : -1;

                    var footerOffset = child.LocationSpan.Start.LineNumber == firstChild.LocationSpan.Start.LineNumber
                                         ? 0 // it's on the same line
                                         : 1;

                    var headerStartLine = firstChild.LocationSpan.Start.LineNumber + headerOffset;

                    var headerStart = child.HeaderSpan.Start;
                    var headerEnd = finder.GetCharacterPosition(headerStartLine, finder.GetLineLength(headerStartLine));

                    var footerStartLine = lastChild.LocationSpan.End.LineNumber + footerOffset;
                    var footerStart = finder.GetCharacterPosition(footerStartLine, 1);
                    var footerEnd = finder.GetCharacterPosition(child.LocationSpan.End);

                    child.HeaderSpan = new CharacterSpan(headerStart, headerEnd);
                    child.FooterSpan = new CharacterSpan(footerStart, footerEnd);
                    break;
                }
            }
        }

        private static void AdjustTerminalNodeChild(IParent parent, TerminalNode child, CharacterPositionFinder finder)
        {
            var start = finder.GetCharacterPosition(child.LocationSpan.Start);
            var end = finder.GetCharacterPosition(child.LocationSpan.End);
            child.Span = new CharacterSpan(start, end);
        }

        private static void AdjustChildren(Container container, CharacterPositionFinder finder)
        {
            for (var index = 0; index < container.Children.Count; index++)
            {
                AdjustNode(container, index, finder);
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