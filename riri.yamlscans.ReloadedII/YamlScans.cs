using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Reloaded.Hooks.ReloadedII.Interfaces;
using Reloaded.Memory.SigScan.ReloadedII.Interfaces;
using Reloaded.Mod.Interfaces;
using Reloaded.Mod.Interfaces.Internal;
using RyoTune.Reloaded;
using SharedScans.Interfaces;

namespace riri.yamlscans.ReloadedII;

/// <summary>
/// Singleton used to process YAML Scan files, which are defined within a Reloaded mod in Project/[mod_id]/scans.yaml
/// </summary>
public static class YamlScans
{
    private static IModLoader? _modLoader;
    private static string? _modId;
    internal static ISharedScans? _sharedScans { get; private set; }
    internal static IReloadedHooks? _hooks { get; private set; }
    internal static IStartupScanner? _startupScanner { get; private set; }
    internal static TransformProviderAMD64? TransformProvider { get; private set; }
    private static nint BaseAddress;
    private static Dictionary<string, SignatureEntry>? Signatures;
    
    /// <summary>
    /// Initializes the singleton. Ensure that this is called after RyoTune.Reloaded.Project.Initialize!
    /// </summary>
    /// <param name="_modConfig">Mod Config object from your Reloaded mod</param>
    /// <param name="_modLoader">Mod Loader object from your Reloaded mod</param>
    public static void Initialize(IModConfig _modConfig, IModLoader _modLoader)
    {
        YamlScans._modLoader = _modLoader;
        _modId = _modConfig.ModId;

        BaseAddress = Process.GetCurrentProcess().MainModule!.BaseAddress;
        TransformProvider = new(BaseAddress);
        Signatures = new();

        _hooks = GetDependency<IReloadedHooks>("reloaded.sharedlib.hooks");
        _sharedScans = GetDependency<ISharedScans>("SharedScans.Reloaded");
        _startupScanner = GetDependency<IStartupScanner>("Reloaded.Memory.SigScan.ReloadedII");
        
        YamlScans._modLoader.ModLoaded += OnModLoaded;
    }
    
    private static TDependency GetDependencyInner<TDependency>(string Message) where TDependency: class
    {
        var controller = _modLoader!.GetController<TDependency>();
        if (controller == null || !controller.TryGetTarget(out var target))
            throw new Exception(Message);
        return target;
    }
    
    /// <summary>
    /// Try to resolve a controller from another Reloaded-II mod, throwing an exception if unsuccessful.
    /// </summary>
    /// <typeparam name="TDependency">The controller type</typeparam>
    /// <returns>The controller if the dependency is found</returns>
    public static TDependency GetDependency<TDependency>() where TDependency: class
        => GetDependencyInner<TDependency>(
            $"[{_modId}]: Could not get controller for \"{typeof(TDependency).Name}\". This dependency is likely missing.");

    /// <summary>
    /// Try to resolve a controller from another Reloaded-II mod, throwing an exception if unsuccessful.
    /// This additionally prints the mod ID for the missing mod, which is better for troubleshooting.
    /// </summary>
    /// <typeparam name="TDependency">The controller type</typeparam>
    /// <returns>The controller if the dependency is found</returns>
    public static TDependency GetDependency<TDependency>(string ModOwner) where TDependency : class
        => GetDependencyInner<TDependency>(
            $"[{_modId}]: Could not get controller for \"{typeof(TDependency).Name}\". Check that {ModOwner} is in your mod's ModConfig.json!");

    private static void OnModLoaded(IModV1 _mod, IModConfigV1 _modConfig)
    {
        Log.Verbose($"OnModLoaded: {_modConfig.ModId}");
        if (_modConfig.ModId != _modId && !Project.IsModDependent(_modConfig)) return;
        LoadYamls((IModConfig)_modConfig);
    }

    private static void LoadYamls(IModConfig _modConfig)
    {
        var projectFolder = Project.Instance.GetProjectFolder(
            _modLoader!.GetDirectoryForModId(_modConfig.ModId));
        if (TryLoadYamlFiles(projectFolder, out var globalScans))
            AddSignaturesFromYaml(globalScans!);
        if (TryLoadYamlFiles(Path.Join(projectFolder, Project.Instance.AppId), out var appScans))
            AddSignaturesFromYaml(appScans!);
    }

    private static void AddSignaturesFromYaml(ScanModel model)
    {
        var asDict = model.ToDictionary();
        foreach (var (name, candidates) in asDict)
        {
            if (Signatures!.TryGetValue(name, out var oldEntry))
                oldEntry.IsLatest = false;
            Signatures[name] = new(name, candidates);
            foreach (var candidate in candidates)
                Signatures[name].CreateCandidateScan(candidate);
        }
    }

    private static bool TryLoadYamlFiles(string folder, out ScanModel? scans)
    {
        scans = null;
        if (!Path.Exists(folder)) return false;
        foreach (var scan in Directory.EnumerateFiles(folder, "scans.yaml", SearchOption.TopDirectoryOnly)
                     .Select(ScanModel.FromPath))
        {
            scans = scan;
            break;
        }
        return scans != null;
    }
}