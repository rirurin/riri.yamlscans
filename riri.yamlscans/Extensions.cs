using YamlDotNet.RepresentationModel;
using YamlDotNet.Serialization;

namespace riri.yamlscans;

public static class Extensions
{
    public static T? Cast<T>(this YamlNode node)
        where T : IYamlConvertible
    {
        var targetType = node.NodeType switch
        {
            YamlNodeType.Alias => typeof(YamlNode),
            YamlNodeType.Mapping => typeof(YamlMappingNode),
            YamlNodeType.Scalar => typeof(YamlScalarNode),
            YamlNodeType.Sequence => typeof(YamlSequenceNode),
        };
        return typeof(T) == targetType ? (T?)(IYamlConvertible)node : default;
    }

    public static KeyValuePair<YamlNode, YamlNode>? GetMapping(this YamlNode node)
        => node.Cast<YamlMappingNode>()?.Children[0];
}