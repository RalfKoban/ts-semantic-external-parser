using System.Collections.Generic;

namespace MiKoSolutions.SemanticParsers.TypeScript.Yaml
{
    public interface IParent
    {
        List<ContainerOrTerminalNode> Children { get; }
    }
}