using YamlDotNet.RepresentationModel;
using YamlDotNet.Serialization;

namespace riri.yamlscans;

/// <summary>
/// Type extensions used by YAML scans
/// </summary>
public static class Extensions
{
    /// <summary>
    /// Downcasts a YAML node to a specific type, throwing an exception if the NodeType does not match.
    /// </summary>
    /// <typeparam name="T">Target YAML object type</typeparam>
    /// <param name="node">Node to downcast</param>
    /// <returns>The casted YAML node</returns>
    /// <exception cref="Exception">Throws if the NodeType for the type to cast to doesn't match the node</exception>
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

    /// <summary>
    /// Get YAML Mapping from the first child
    /// </summary>
    /// <param name="node">Target node</param>
    /// <returns>YAML Mapping from the first child</returns>
    public static KeyValuePair<YamlNode, YamlNode>? GetMapping(this YamlNode node)
        => node.Cast<YamlMappingNode>()?.Children[0];
}