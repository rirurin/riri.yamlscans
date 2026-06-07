using NCalc;
using YamlDotNet.RepresentationModel;

namespace riri.yamlscans;

public interface ITransform
{
    nint Transform(TransformProviderAMD64 provider, nint ptr);
}

// GetDirectAddressRelative
public class GetDirectAddress : ITransform
{
    public nint Transform(TransformProviderAMD64 provider, nint ptr)
        => provider.TryDeref(provider.GetDirectAddress(ptr));

    public override bool Equals(object? obj)
        => obj is GetDirectAddress;
}

public class GetDirectAddressAbsolute : ITransform
{
    public nint Transform(TransformProviderAMD64 provider, nint ptr)
        => provider.TryDeref(ptr);
    
    public override bool Equals(object? obj)
        => obj is GetDirectAddressAbsolute;
}

public abstract class GetIndirectAddress : ITransform
{
    protected abstract int Amount { get; }
    public nint Transform(TransformProviderAMD64 provider, nint ptr)
        => provider.TryDeref(provider.GetGlobalAddress(provider.GetDirectAddress(ptr) + Amount));
    
    public override bool Equals(object? obj)
        => obj.GetType() == GetType();
}

public class GetIndirectAddressShort : GetIndirectAddress
{
    protected override int Amount => 1;
}

public class GetIndirectAddressShort2 : GetIndirectAddress
{
    protected override int Amount => 2;
}

public class GetIndirectAddressLong : GetIndirectAddress
{
    protected override int Amount => 3;
}

public class GetIndirectAddressLong4 : GetIndirectAddress
{
    protected override int Amount => 4;
}

public class CustomExpression(string expr) : ITransform
{
    private string Expr { get; } = expr;

    private static nint Res(ExpressionFunctionData p)
        => (nint)p[0].Evaluate()!;
    
    public nint Transform(TransformProviderAMD64 provider, nint ptr)
    => (nint?)new Expression(Expr)
        {
            Parameters = { ["result"] = ptr },
            Functions =
            {
                ["GetDirectAddress"] = p => provider.GetDirectAddress(Res(p)),
                ["GetGlobalAddress"] = p => provider.GetGlobalAddress(Res(p)),
                ["GetIndirectAddressShort"] = p => new GetIndirectAddressShort().Transform(provider, Res(p)),
                ["GetIndirectAddressShort2"] = p => new GetIndirectAddressShort2().Transform(provider, Res(p)),
                ["GetIndirectAddressLong"] = p => new GetIndirectAddressLong().Transform(provider, Res(p)),
                ["GetIndirectAddressLong4"] = p => new GetIndirectAddressLong4().Transform(provider, Res(p)),
            }
        }.Evaluate() ?? throw new Exception("Error while trying to evaluate custom expression");

    public override bool Equals(object? obj)
        => obj is CustomExpression expression && Expr.Equals(expression.Expr);
}

public class TransformProviderAMD64(nint baseAddress)
{

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

    public unsafe nint TryDeref(nint ptr)
        => ((byte*)ptr)[0] switch
        {
            0xeb => DerefShort(ptr),
            0xe9 => DerefNear(ptr),
            0xff => TryDerefLong(ptr),
            _ => ptr
        };

    public unsafe nint GetGlobalAddress(nint ptr)
        => *(int*)ptr + ptr + 4;
    
    public nint GetDirectAddress(nint ptr)
        => BaseAddress + ptr;
}

public class Candidate(string signature, ITransform transformer)
{
    public string Signature { get; } = signature;
    public ITransform Transformer { get; set; } = transformer;

    public override string ToString()
        => $"(\"{Signature}\" => {Transformer.GetType().Name})";
}

public class ScanEntry(string key, List<Candidate> candidates)
{
    public string Key { get; } = key;
    public List<Candidate> Candidates { get; } = candidates;

    public override string ToString()
        => $"{Key} = [{string.Join(",", Candidates.Select(x => x.ToString()))}]";
}

public class ScanModel(List<ScanEntry> entries)
{

    public List<ScanEntry> Entries { get; } = entries;
    
    private const string ResultSettingTag = "_RESULT";
    private const string DisabledScanValue = "DISABLED";

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
    
    public static ScanModel? FromString(string yaml)
    {
        var reader = new YamlStream();
        reader.Load(new StringReader(yaml));
        var root = reader.Documents[0].RootNode.Cast<YamlSequenceNode>() 
                   ?? throw new Exception("Expected a sequence at the top-level");
        Dictionary<string, ScanEntry> scanEntries = new();
        foreach (var node in root)
        {
            var mapping = node.GetMapping() ?? throw new Exception("Expected a mapping");
            var key = mapping.Key.Cast<YamlScalarNode>()?.Value ?? throw new Exception("Expected a string for the key");
            if (key.EndsWith(ResultSettingTag))
            {
                if (!scanEntries.TryGetValue(key[..^ResultSettingTag.Length], out var scanEntry))
                    throw new Exception($"{key[..^ResultSettingTag.Length]} should be declared before it's result transformer");
                switch (mapping.Value.NodeType)
                {
                    case YamlNodeType.Scalar:
                        scanEntry.Candidates[0].Transformer = TransformFromString(
                            mapping.Value.Cast<YamlScalarNode>()?.Value ?? throw new Exception("Transform entry must be a string"));
                        break;
                    case YamlNodeType.Sequence:
                        var sequence = mapping.Value.Cast<YamlSequenceNode>();
                        if (scanEntry.Candidates.Count != sequence.Children.Count)
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
            else
            {
                switch (mapping.Value.NodeType)
                {
                    case YamlNodeType.Scalar:
                        var scalar = mapping.Value.Cast<YamlScalarNode>()?.Value!;
                        if (scalar != DisabledScanValue)
                            scanEntries.Add(key, new(key, [new(scalar, new GetDirectAddress())]));
                        break;
                    case YamlNodeType.Sequence:
                        var sequence = mapping.Value.Cast<YamlSequenceNode>();
                        // Scan YAML format (mapping with inner signatures and transform fields
                        /*
                        if (sequence.Children.Count > 0 && sequence.Children[0].NodeType == YamlNodeType.Mapping)
                        {
                            var signatureMapping = sequence.Children[0].GetMapping() ?? throw new Exception("Expected a mapping");
                            Console.WriteLine(signatureMapping.Key);
                            Console.WriteLine(signatureMapping.Value);
                        }
                        // Scan INI format
                        else
                        */
                        {
                            scanEntries.Add(key, new (key, sequence.Children.Select(x =>
                                new Candidate(x.Cast<YamlScalarNode>()?.Value ?? 
                                              throw new Exception("Signature entries are expected to be strings"), 
                                    new GetDirectAddress()) 
                            ).ToList()));   
                        }
                        break;
                    default:
                        throw new Exception(
                            $"Unexpected formatting for signatures: Detected YAML node type {mapping.Value.NodeType}");
                }
            }
            Console.WriteLine($"{mapping.Key} = {mapping.Value}");
        }
        return new ScanModel(scanEntries.Values.ToList());
    }

    public static ScanModel? FromPath(string path)
    {
        using var stream = new StreamReader(path);
        return FromString(stream.ReadToEnd());
    }
}