using YamlDotNet.RepresentationModel;
using YamlDotNet.Serialization;

namespace riri.yamlscans;

/// <inheritdoc/>
public static class Extensions
{
    /// <inheritdoc/>
    public static T? Cast<T>(this YamlNode node)
        where T : IYamlConvertible
    {
        var targetType = node.NodeType switch
        {
            YamlNodeType.Alias => typeof(YamlNode),
            YamlNodeType.Mapping => typeof(YamlMappingNode),
            YamlNodeType.Scalar => typeof(YamlScalarNode),
            YamlNodeType.Sequence => typeof(YamlSequenceNode),
            _ => throw new Exception($"Node type {node.NodeType} is not supported")
        };
        return typeof(T) == targetType ? (T?)(IYamlConvertible)node : default;
    }

    /// <inheritdoc/>
    public static KeyValuePair<YamlNode, YamlNode>? GetMapping(this YamlNode node)
        => node.Cast<YamlMappingNode>()?.Children[0];
}