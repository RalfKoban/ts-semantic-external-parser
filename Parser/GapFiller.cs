using System.Collections.Generic;

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

            if (indexInParentChildren > 0 && indexInParentChildren < parentChildren.Count - 1)
            {
                var previousSibling = parentChildren[indexInParentChildren - 1];

                var indexAfter = finder.GetCharacterPosition(previousSibling.LocationSpan.End) + 1;
                var newStartPos = finder.GetLineInfo(indexAfter);

                child.LocationSpan = new LocationSpan(newStartPos, child.LocationSpan.End);
            }

            if (child is Container c)
            {
                var start = c.HeaderSpan.End + 1;

                AdjustChildren(c, finder);

                var end = finder.GetCharacterPosition(c.LocationSpan.End);
                c.FooterSpan = new CharacterSpan(start, end);
            }
            else if (child is TerminalNode t)
            {
                var start = finder.GetCharacterPosition(t.LocationSpan.Start);
                var end = finder.GetCharacterPosition(t.LocationSpan.End);
                t.Span = new CharacterSpan(start, end);
            }
        }
    }
}