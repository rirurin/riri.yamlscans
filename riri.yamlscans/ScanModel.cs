using NCalc;
using NCalc.Handlers;
using YamlDotNet.RepresentationModel;

namespace riri.yamlscans;

/// <summary>
/// Implemented by classes that represent some form of transformation on a target address.
/// </summary>
public interface ITransform
{
    /// <summary>
    /// Performs the transformation on the pointer
    /// </summary>
    /// <param name="provider">A context object that defines platform-specific behaviors</param>
    /// <param name="ptr">Pointer to transform</param>
    /// <returns>The transformed address</returns>
    nint Transform(TransformProviderAMD64 provider, nint ptr);
}

/// <summary>
/// An address transform that adds the executable base address. 
/// Useful in cases such as where a signature points directly to the start of the target function.
/// </summary>
public class GetDirectAddress : ITransform
{
    /// <inheritdoc/>
    public nint Transform(TransformProviderAMD64 provider, nint ptr)
        => provider.TryDeref(provider.GetDirectAddress(ptr));

    /// <inheritdoc/>
    public override bool Equals(object? obj)
        => obj is GetDirectAddress;

    /// <summary>
    /// Hash code for GetDirectAddress
    /// </summary>
    /// <returns>Hash code for GetDirectAddress</returns>
    public override int GetHashCode()
        => 0.GetHashCode();
}

/// <summary>
/// Like GetDirectAddress but with the base address already added.
/// </summary>
public class GetDirectAddressAbsolute : ITransform
{
    /// <inheritdoc/>
    public nint Transform(TransformProviderAMD64 provider, nint ptr)
        => provider.TryDeref(ptr);

    /// <inheritdoc/>
    public override bool Equals(object? obj)
        => obj is GetDirectAddressAbsolute;

    /// <summary>
    /// Hash code for GetDirectAddressAbsolute
    /// </summary>
    /// <returns>Hash code for GetDirectAddressAbsolute</returns>
    public override int GetHashCode()
        => 1.GetHashCode();
}

/// <summary>
/// A base class representing address transformations involving dereferencing an Int32 sized relative pointer encoded into the instruction.
/// </summary>
public abstract class GetIndirectAddress : ITransform
{
    /// <summary>
    /// The number of bytes that the int32 pointer encoded into the instruction is offset by
    /// </summary>
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

/// <summary>
/// An address transformation that takes the current location + 1 and dereferences the Int32 relative pointer at that location to retrieve the target address.
/// For example, a near jump/call instruction consist of an opcode byte (E9 and E8 respectively), followed by the relative pointer.
/// </summary>
public class GetIndirectAddressShort : GetIndirectAddress
{
    /// <inheritdoc/>
    protected override int Amount => 1;

    /// <inheritdoc/>
    public override int GetHashCode()
        => 2.GetHashCode();
}

/// <summary>
/// An address transformation that takes the current location + 2 and deferences the Int32 relative pointer at that location to retrieve the target address.
/// </summary>
public class GetIndirectAddressShort2 : GetIndirectAddress
{
    /// <inheritdoc/>
    protected override int Amount => 2;

    /// <inheritdoc/>
    public override int GetHashCode()
        => 3.GetHashCode();
}

/// <summary>
/// An address transformation that takes the current location + 3 and deferences the Int32 relative pointer at that location to retrieve the target address.
/// </summary>
public class GetIndirectAddressLong : GetIndirectAddress
{
    /// <inheritdoc/>
    protected override int Amount => 3;

    /// <inheritdoc/>
    public override int GetHashCode()
        => 4.GetHashCode();
}

/// <summary>
/// An address transformation that takes the current location + 4 and deferences the Int32 relative pointer at that location to retrieve the target address.
/// </summary>
public class GetIndirectAddressLong4 : GetIndirectAddress
{
    /// <inheritdoc/>
    protected override int Amount => 4;

    /// <inheritdoc/>
    public override int GetHashCode()
        => 5.GetHashCode();
}

/// <summary>
/// Like GetDirectAddress but with the base address already added.
/// </summary>
public class GetAddressFromInt : ITransform
{
    /// <inheritdoc/>
    public nint Transform(TransformProviderAMD64 provider, nint ptr)
        => provider.GetDirectAddress(provider.DerefInt(provider.GetDirectAddress(ptr)));

    /// <inheritdoc/>
    public override bool Equals(object? obj)
        => obj is GetAddressFromInt;

    /// <summary>
    /// Hash code for GetAddressFromInt
    /// </summary>
    /// <returns>Hash code for GetAddressFromInt</returns>
    public override int GetHashCode()
        => 6.GetHashCode();
}

/// <summary>
/// A custom address transformation that supports arithmetic functions and use of the other address transformers.
/// </summary>
/// <param name="expr">Value of the custom transform</param>
public class CustomExpression(string expr) : ITransform
{
    private string Expr { get; } = expr;

    private static long Res(FunctionData p)
        => (long)p.Evaluate(0)!;

    /// <inheritdoc/>
    public nint Transform(TransformProviderAMD64 provider, nint ptr)
    => (nint)((long?)new Expression(Expr)
        {
            Parameters = { ["result"] = (long)ptr },
            Functions =
            {
                ["GetDirectAddress"] = p => (long)provider.GetDirectAddress((nint)Res(p)),
                ["GetGlobalAddress"] = p => (long)provider.GetGlobalAddress((nint)Res(p)),
                ["TryDeref"] = p => (long)provider.TryDeref((nint)Res(p)),
                ["DerefData"] = p => (long)provider.DerefData((nint)Res(p)),
                ["DerefInt"] = p => (long)provider.DerefInt((nint)Res(p)),
                ["GetAddressFromInt"] = p => (long)new GetAddressFromInt().Transform(provider, (nint)Res(p)),
                ["GetIndirectAddressShort"] = p => (long)new GetIndirectAddressShort().Transform(provider, (nint)Res(p)),
                ["GetIndirectAddressShort2"] = p => (long)new GetIndirectAddressShort2().Transform(provider, (nint)Res(p)),
                ["GetIndirectAddressLong"] = p => (long)new GetIndirectAddressLong().Transform(provider, (nint)Res(p)),
                ["GetIndirectAddressLong4"] = p => (long)new GetIndirectAddressLong4().Transform(provider, (nint)Res(p)),
            }
        }.Evaluate() ?? throw new Exception("Error while trying to evaluate custom expression"));

    /// <inheritdoc/>
    public override bool Equals(object? obj)
        => obj is CustomExpression expression && Expr.Equals(expression.Expr);

    /// <inheritdoc/>
    public override int GetHashCode()
        => 6.GetHashCode();
}

/// <summary>
/// Transform provider for AMD64 (x86_64)
/// </summary>
/// <param name="baseAddress">The base address for the executable's main module</param>
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

    /// <summary>
    /// Tries to dereference the JMP instruction at the current address
    /// </summary>
    /// <param name="ptr">Current address</param>
    /// <returns>Dereferenced value</returns>
    public unsafe nint TryDeref(nint ptr)
        => ((byte*)ptr)[0] switch
        {
            0xeb => DerefShort(ptr),
            0xe9 => DerefNear(ptr),
            0xff => TryDerefLong(ptr),
            _ => ptr
        };

    /// <summary>
    /// Get the absolute address from a relative address pointer
    /// </summary>
    /// <param name="ptr"></param>
    /// <returns></returns>
    public unsafe nint GetGlobalAddress(nint ptr)
        => *(int*)ptr + ptr + 4;

    /// <summary>
    /// Adds the base address of the executable's main module to an offset
    /// </summary>
    /// <param name="ptr">Address offset</param>
    /// <returns>Absolute pointer</returns>
    public nint GetDirectAddress(nint ptr)
        => BaseAddress + ptr;

    /// <summary>
    /// Dereference a platform-size pointer. This is useful in cases such as entering a function from a VTable.
    /// </summary>
    /// <param name="ptr">Current address</param>
    /// <returns>Derferenced address</returns>
    public unsafe nint DerefData(nint ptr)
        => *(nint*)ptr;

    /// <summary>
    /// Dereference an integer-size pointer. This is useful in cases where an instruction is storing a pointer relative
    /// to the start of the program's memory map instead of relative to the next instruction's location.
    /// </summary>
    /// <param name="ptr"></param>
    /// <returns></returns>
    public unsafe int DerefInt(nint ptr)
        => *(int*)ptr;
}

/// <summary>
/// Structure for a single signature candidate.
/// </summary>
/// <param name="signature">A sequence of bytes used as the search parameter to find a particular location in the executable.</param>
/// <param name="transformer">The transform function to convert the found location into an appropriate format.</param>
public class Candidate(string signature, ITransform transformer)
{
    /// <summary>
    /// A sequence of bytes used as the search parameter to find a particular location in the executable.
    /// </summary>
    public string Signature { get; } = signature;

    /// <summary>
    /// The transform function to convert the found location into an appropriate format.
    /// </summary>
    public ITransform Transformer { get; set; } = transformer;

    /// <inheritdoc/>
    public override string ToString()
        => $"(\"{Signature}\" => {Transformer.GetType().Name})";
}

/// <summary>
/// Represents a location in the executable to search for. This associates a key to identify the target location with a set of candidates.
/// Multiple candidates are selected to accommodate for differences between different executable versions.
/// </summary>
/// <param name="key">Name of the location</param>
/// <param name="candidates">List of candidates</param>
public class ScanEntry(string key, List<Candidate> candidates)
{
    /// <summary>
    /// Name of the location
    /// </summary>
    public string Key { get; } = key;
    
    /// <summary>
    /// List of candidates
    /// </summary>
    public List<Candidate> Candidates { get; } = candidates;

    /// <inheritdoc/>
    public override string ToString()
        => $"{Key} = [{string.Join(",", Candidates.Select(x => x.ToString()))}]";
}

/// <summary>
/// The structure used to store a successfully parsed YAML scan
/// </summary>
/// <param name="entries">A list of <see cref="ScanEntry"/></param>
public class ScanModel(List<ScanEntry> entries)
{
    /// <summary>
    /// A list of <see cref="ScanEntry"/>
    /// </summary>
    public List<ScanEntry> Entries { get; } = entries;

    /// <summary>
    /// Name of the suffix attached to a function name to indicate that it represents the transformation for the given function
    /// </summary>
    public const string ResultSettingTag = "_RESULT";

    /// <summary>
    /// Keyword to indicate that a function is not used
    /// </summary>
    public const string DisabledScanValue = "DISABLED";

    /// <summary>
    /// YAML key with an associated value containing for one or more signatures
    /// </summary>
    public const string SignatureTag = "signatures";

    /// <summary>
    /// YAML key with an associated value containing for one or more address transforms
    /// </summary>
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

    /// <summary>
    /// Constructs a ScanModel starting from the target YAML mapping node. This can be inside of a parent YAML document.
    /// </summary>
    /// <param name="root">Root YAML node to start parsing as a Scan YAML from.</param>
    /// <returns>ScanModel</returns>
    /// <exception cref="Exception">If parsing of the Scan YAML fails.</exception>
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

    /// <summary>
    /// Constructs a ScanModel from a string
    /// </summary>
    /// <param name="yaml">String containing a YAML Scan</param>
    /// <returns>ScanModel</returns>
    public static ScanModel FromString(string yaml)
    {
        var reader = new YamlStream();
        reader.Load(new StringReader(yaml));
        var root = reader.Documents[0].RootNode.Cast<YamlMappingNode>() 
                   ?? throw new Exception("Expected a mapping at the top-level");
        return FromNode(root);
    }
    
    /// <summary>
    /// Constructs a ScanModel from a file path
    /// </summary>
    /// <param name="path">Filename pointing to a YAML Scan</param>
    /// <returns>ScanModel</returns>
    public static ScanModel FromPath(string path)
    {
        using var stream = new StreamReader(path);
        return FromString(stream.ReadToEnd());
    }
    
    /// <summary>
    /// Converts a ScanModel into a Dictionary to quickly search for signatures by name
    /// </summary>
    /// <returns>ScanModel signatures as a dictionary</returns>
    public Dictionary<string, List<Candidate>> ToDictionary()
        => Entries.Select(x => (x.Key, x.Candidates)).ToDictionary();
}