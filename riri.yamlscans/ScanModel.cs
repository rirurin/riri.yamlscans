using NCalc;
using YamlDotNet.RepresentationModel;

namespace riri.yamlscans;

/// <inheritdoc/>
public interface ITransform
{
    /// <inheritdoc/>
    nint Transform(TransformProviderAMD64 provider, nint ptr);
}

// GetDirectAddressRelative
/// <inheritdoc/>
public class GetDirectAddress : ITransform
{
    /// <inheritdoc/>
    public nint Transform(TransformProviderAMD64 provider, nint ptr)
        => provider.TryDeref(provider.GetDirectAddress(ptr));
    /// <inheritdoc/>
    public override bool Equals(object? obj)
        => obj is GetDirectAddress;

    /// <inheritdoc/>
    public override int GetHashCode()
        => 0.GetHashCode();
}

/// <inheritdoc/>
public class GetDirectAddressAbsolute : ITransform
{
    /// <inheritdoc/>
    public nint Transform(TransformProviderAMD64 provider, nint ptr)
        => provider.TryDeref(ptr);

    /// <inheritdoc/>
    public override bool Equals(object? obj)
        => obj is GetDirectAddressAbsolute;
    /// <inheritdoc/>
    public override int GetHashCode()
        => 1.GetHashCode();
}

/// <inheritdoc/>
public abstract class GetIndirectAddress : ITransform
{
    /// <inheritdoc/>
    protected abstract int Amount { get; }
    /// <inheritdoc/>
    public nint Transform(TransformProviderAMD64 provider, nint ptr)
        => provider.TryDeref(provider.GetGlobalAddress(provider.GetDirectAddress(ptr) + Amount));
    /// <inheritdoc/>
    public override bool Equals(object? obj)
        => obj != null && obj.GetType() == GetType();
    /// <inheritdoc/>
    public abstract override int GetHashCode();
}

/// <inheritdoc/>
public class GetIndirectAddressShort : GetIndirectAddress
{
    /// <inheritdoc/>
    protected override int Amount => 1;
    /// <inheritdoc/>
    public override int GetHashCode()
        => 2.GetHashCode();
}

/// <inheritdoc/>
public class GetIndirectAddressShort2 : GetIndirectAddress
{
    /// <inheritdoc/>
    protected override int Amount => 2;
    /// <inheritdoc/>
    public override int GetHashCode()
        => 3.GetHashCode();
}

/// <inheritdoc/>
public class GetIndirectAddressLong : GetIndirectAddress
{
    /// <inheritdoc/>
    protected override int Amount => 3;
    /// <inheritdoc/>
    public override int GetHashCode()
        => 4.GetHashCode();
}

/// <inheritdoc/>
public class GetIndirectAddressLong4 : GetIndirectAddress
{
    /// <inheritdoc/>
    protected override int Amount => 4;
    /// <inheritdoc/>
    public override int GetHashCode()
        => 5.GetHashCode();
}

/// <inheritdoc/>
public class CustomExpression(string expr) : ITransform
{
    private string Expr { get; } = expr;

    private static nint Res(ExpressionFunctionData p)
        => (nint)p[0].Evaluate()!;

    /// <inheritdoc/>
    public nint Transform(TransformProviderAMD64 provider, nint ptr)
    => (nint?)new Expression(Expr)
        {
            Parameters = { ["result"] = ptr },
            Functions =
            {
                ["GetDirectAddress"] = p => provider.GetDirectAddress(Res(p)),
                ["GetGlobalAddress"] = p => provider.GetGlobalAddress(Res(p)),
                ["DerefData"] = p => provider.DerefData(Res(p)),
                ["GetIndirectAddressShort"] = p => new GetIndirectAddressShort().Transform(provider, Res(p)),
                ["GetIndirectAddressShort2"] = p => new GetIndirectAddressShort2().Transform(provider, Res(p)),
                ["GetIndirectAddressLong"] = p => new GetIndirectAddressLong().Transform(provider, Res(p)),
                ["GetIndirectAddressLong4"] = p => new GetIndirectAddressLong4().Transform(provider, Res(p)),
            }
        }.Evaluate() ?? throw new Exception("Error while trying to evaluate custom expression");

    /// <inheritdoc/>
    public override bool Equals(object? obj)
        => obj is CustomExpression expression && Expr.Equals(expression.Expr);

    /// <inheritdoc/>
    public override int GetHashCode()
        => 6.GetHashCode();
}

/// <inheritdoc/>
public class TransformProviderAMD64(nint baseAddress)
{
    /// <inheritdoc/>
    public nint BaseAddress { get; } = baseAddress;
    
    private unsafe nint DerefShort(nint ptr)
        => TryDeref(ptr + (*(sbyte*)(ptr + 1) + 2));

    private unsafe nint DerefNear(nint ptr)
        => TryDeref(ptr + (*(int*)(ptr + 1) + 5));

    private unsafe nint TryDerefLong(nint ptr)
        => ((byte*)ptr)[1] switch
        {
            5 => TryDeref(*(nint*)(ptr + 6)),
            _ => ptr
        };

    /// <inheritdoc/>
    public unsafe nint TryDeref(nint ptr)
        => ((byte*)ptr)[0] switch
        {
            0xeb => DerefShort(ptr),
            0xe9 => DerefNear(ptr),
            0xff => TryDerefLong(ptr),
            _ => ptr
        };

    /// <inheritdoc/>
    public unsafe nint GetGlobalAddress(nint ptr)
        => *(int*)ptr + ptr + 4;

    /// <inheritdoc/>
    public nint GetDirectAddress(nint ptr)
        => BaseAddress + ptr;

    /// <inheritdoc/>
    public unsafe nint DerefData(nint ptr)
        => *(nint*)ptr;
}

/// <inheritdoc/>
public class Candidate(string signature, ITransform transformer)
{
    /// <inheritdoc/>
    public string Signature { get; } = signature;
    /// <inheritdoc/>
    public ITransform Transformer { get; set; } = transformer;

    /// <inheritdoc/>
    public override string ToString()
        => $"(\"{Signature}\" => {Transformer.GetType().Name})";
}

/// <inheritdoc/>
public class ScanEntry(string key, List<Candidate> candidates)
{
    /// <inheritdoc/>
    public string Key { get; } = key;
    /// <inheritdoc/>
    public List<Candidate> Candidates { get; } = candidates;

    /// <inheritdoc/>
    public override string ToString()
        => $"{Key} = [{string.Join(",", Candidates.Select(x => x.ToString()))}]";
}

/// <inheritdoc/>
public class ScanModel(List<ScanEntry> entries)
{
    /// <inheritdoc/>
    public List<ScanEntry> Entries { get; } = entries;

    /// <inheritdoc/>
    public const string ResultSettingTag = "_RESULT";

    /// <inheritdoc/>
    public const string DisabledScanValue = "DISABLED";

    /// <inheritdoc/>
    public const string SignatureTag = "signatures";

    /// <inheritdoc/>
    public const string TransformTag = "transforms";

    private static ITransform TransformFromString(string value)
    => value switch
        {
            "GetDirectAddress" => new GetDirectAddress(),
            "GetDirectAddressAbsolute" => new GetDirectAddressAbsolute(),
            "GetIndirectAddressShort" => new GetIndirectAddressShort(),
            "GetIndirectAddressShort2" => new GetIndirectAddressShort2(),
            "GetIndirectAddressLong" => new GetIndirectAddressLong(),
            "GetIndirectAddressLong4" => new GetIndirectAddressLong4(),
            _ => new CustomExpression(value)
        };

    private static void CreateScalarSignature(string key, string scalar, ref Dictionary<string, ScanEntry> scanEntries)
    {
        if (scalar != DisabledScanValue)
            scanEntries.Add(key, new(key, [new(scalar, new GetDirectAddress())]));
    }

    private static void SetSignatureTransformer(KeyValuePair<YamlNode, YamlNode> mapping, ScanEntry scanEntry)
    {
        switch (mapping.Value.NodeType)
        {
            case YamlNodeType.Scalar:
                scanEntry.Candidates[0].Transformer = TransformFromString(
                    mapping.Value.Cast<YamlScalarNode>()?.Value ?? throw new Exception("Transform entry must be a string"));
                break;
            case YamlNodeType.Sequence:
                var sequence = mapping.Value.Cast<YamlSequenceNode>();
                if (scanEntry.Candidates.Count != sequence!.Children.Count)
                {
                    throw new Exception(
                        $"Signature transform list should be the same length as the signature list (got {sequence.Children.Count} transforms when we expected {scanEntry.Candidates.Count})");                           
                }

                foreach (var (candidate, transform) in scanEntry.Candidates.Zip(sequence.Children))
                    candidate.Transformer = TransformFromString(transform.Cast<YamlScalarNode>()?.Value ??
                                                                throw new Exception(
                                                                    "Transform entry must be a string"));
                break;
            default:
                throw new Exception(
                    $"Unexpected formatting for signature transform: Detected YAML node type {mapping.Value.NodeType}");
        }   
    }

    /// <inheritdoc/>
    public static ScanModel FromNode(YamlMappingNode root)
    {
        Dictionary<string, ScanEntry> scanEntries = new();

        void AddSignatureSequence(string key, YamlSequenceNode sequence)
        {
            scanEntries.Add(key, new (key, sequence.Children.Select(x =>
                new Candidate(x.Cast<YamlScalarNode>()?.Value ?? 
                              throw new Exception("Signature entries are expected to be strings"), 
                    new GetDirectAddress()) 
            ).ToList()));
        };
        
        void AddScanYAMLSignature(string key, YamlMappingNode sequence)
        {
            foreach (var child in sequence.Children)
            {
                var sigKey = child.Key.Cast<YamlScalarNode>()?.Value ??
                    throw new Exception("Signature field key is expected to be a string");
                switch (sigKey)
                {
                    case SignatureTag:
                        switch (child.Value.NodeType)
                        {
                            case YamlNodeType.Scalar:
                                CreateScalarSignature(key, child.Value.Cast<YamlScalarNode>()?.Value!, ref scanEntries);
                                break;
                            case YamlNodeType.Sequence:
                                AddSignatureSequence(key, child.Value.Cast<YamlSequenceNode>()!);
                                break;
                            default:
                                throw new Exception($"Value for signature field should be a string or list of strings. Got YAML node type {child.Value.NodeType} instead.");
                        }
                        break;
                    case TransformTag:
                        if (!scanEntries.TryGetValue(key, out var scanEntry))
                            throw new Exception($"{key}'s signatures should be declared before their transformers");
                        SetSignatureTransformer(child, scanEntry);
                        break;
                    default:
                        throw new Exception($"Unexpected signature field key {sigKey}");
                }
            }
        }
        
        foreach (var node in root)
        {
            var key = node.Key.Cast<YamlScalarNode>()?.Value ?? throw new Exception("Expected a string for the key");
            if (key.EndsWith(ResultSettingTag))
            {
                if (!scanEntries.TryGetValue(key[..^ResultSettingTag.Length], out var scanEntry))
                    throw new Exception($"{key[..^ResultSettingTag.Length]} should be declared before it's result transformer");
                SetSignatureTransformer(node, scanEntry);
            }
            else
            {
                switch (node.Value.NodeType)
                {
                    case YamlNodeType.Scalar:
                        CreateScalarSignature(key, node.Value.Cast<YamlScalarNode>()?.Value!, ref scanEntries);
                        break;
                    case YamlNodeType.Sequence:
                        var sequence = node.Value.Cast<YamlSequenceNode>()!;
                        AddSignatureSequence(key, sequence); // Scan INI format
                        break;
                    case YamlNodeType.Mapping:
                        var mapping = node.Value.Cast<YamlMappingNode>()!;
                        AddScanYAMLSignature(key, mapping); // Scan YAML format
                        break;
                    default:
                        throw new Exception(
                            $"Unexpected formatting for signatures: Detected YAML node type {node.Value.NodeType}");
                }
            }
        }
        return new ScanModel(scanEntries.Values.ToList());   
    }
    /// <inheritdoc/>
    public static ScanModel FromString(string yaml)
    {
        var reader = new YamlStream();
        reader.Load(new StringReader(yaml));
        var root = reader.Documents[0].RootNode.Cast<YamlMappingNode>() 
                   ?? throw new Exception("Expected a mapping at the top-level");
        return FromNode(root);
    }
    /// <inheritdoc/>
    public static ScanModel FromPath(string path)
    {
        using var stream = new StreamReader(path);
        return FromString(stream.ReadToEnd());
    }
    /// <inheritdoc/>
    public Dictionary<string, List<Candidate>> ToDictionary()
        => Entries.Select(x => (x.Key, x.Candidates)).ToDictionary();
}