using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;

using YamlDotNet.Serialization;

namespace MiKoSolutions.SemanticParsers.TypeScript.Yaml
{
    [DebuggerDisplay("[{GetType().Name}] Name={Name}, Type={Type}")]
    public sealed class File : IParent
    {
        [YamlMember(Alias = "type", Order = 1)]
        public string Type { get; } = "file";

        [YamlMember(Alias = "name", Order = 2)]
        public string Name { get; set; }

        [YamlMember(Alias = "locationSpan", Order = 3)]
        public LocationSpan LocationSpan { get; set; }

        [YamlMember(Alias = "footerSpan", Order = 4)]
        public CharacterSpan FooterSpan { get; set; }

        [YamlMember(Alias = "children", Order = 7)]
        public List<ContainerOrTerminalNode> Children { get; } = new List<ContainerOrTerminalNode>();

        [YamlMember(Alias = "parsingErrorsDetected", Order = 5)]
        public bool? ParsingErrorsDetected => ParsingErrors.Any();

        [YamlMember(Alias = "parsingError", Order = 6)]
        public List<ParsingError> ParsingErrors { get; } = new List<ParsingError>();

        public string ToYaml()
        {
            var sb = new StringBuilder();

            using (var writer = new StringWriter(sb))
            {
                YamlWriter.Write(writer, this);
            }

            return sb.ToString();
        }
    }
}