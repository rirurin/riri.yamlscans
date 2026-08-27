namespace riri.yamlscans.ReloadedII;

/// <summary>
/// Create a pointer to static data in the executable from a YAML scan result.
/// </summary>
/// <typeparam name="TPointer">Type of the static data.</typeparam>
public unsafe class SHStatic<TPointer> where TPointer: unmanaged
{
    private readonly string _Name;
    
    /// <summary>
    /// Pointer to the static data.
    /// </summary>
    public TPointer* Instance { get; private set; }

    /// <summary>
    /// Creates a SHStatic.
    /// Must be done before scanning has started, during normal mod initialization.
    /// </summary>
    /// <param name="Name">The name of the static data pointer. This must be specified</param>
    public SHStatic(string Name) : this(Name, null) {}

    public SHStatic(string Name, Action<nint>? onScanFound)
    {
        _Name = Name;
        YamlScans._sharedScans!.AddScan(_Name, null);
        YamlScans._sharedScans!.CreateListener(_Name, result =>
        {
            Instance = (TPointer*)result;
            onScanFound?.Invoke(result);
        });
    }
}