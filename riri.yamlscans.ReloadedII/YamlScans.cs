using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Reloaded.Hooks.ReloadedII.Interfaces;
using Reloaded.Memory.SigScan.ReloadedII.Interfaces;
using Reloaded.Mod.Interfaces;
using Reloaded.Mod.Interfaces.Internal;
using RyoTune.Reloaded;
using SharedScans.Interfaces;

namespace riri.yamlscans.ReloadedII;

public static class YamlScans
{
    private static IModLoader _modLoader;
    private static string _modId;
    internal static ISharedScans _sharedScans { get; private set; }
    internal static IReloadedHooks _hooks { get; private set; }
    internal static IStartupScanner _startupScanner { get; private set; }
    internal static TransformProviderAMD64 TransformProvider { get; private set; }
    private static nint BaseAddress;
    private static Dictionary<string, SignatureEntry> Signatures;
    
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
        Log.Debug($"Initialized YAML Scans: Base Address is 0x{BaseAddress:x}");
    }
    
    private static TDependency GetDependencyInner<TDependency>(string Message) where TDependency: class
    {
        var controller = _modLoader.GetController<TDependency>();
        if (controller == null || !controller.TryGetTarget(out var target))
            throw new Exception(Message);
        return target;
    }
    
    public static TDependency GetDependency<TDependency>() where TDependency: class
        => GetDependencyInner<TDependency>(
            $"[{_modId}]: Could not get controller for \"{typeof(TDependency).Name}\". This dependency is likely missing.");

    public static TDependency GetDependency<TDependency>(string ModOwner) where TDependency : class
        => GetDependencyInner<TDependency>(
            $"[{_modId}]: Could not get controller for \"{typeof(TDependency).Name}\". Check that {ModOwner} is in your mod's ModConfig.json!");

    private static void OnModLoaded(IModV1 _mod, IModConfigV1 _modConfig)
    {
        Log.Information($"OnModLoaded: {_modConfig.ModId}");
        if (_modConfig.ModId != _modId && !Project.IsModDependent(_modConfig)) return;
        LoadYamls((IModConfig)_modConfig);
    }

    private static void LoadYamls(IModConfig _modConfig)
    {
        var projectFolder = Project.Instance.GetProjectFolder(
            _modLoader.GetDirectoryForModId(_modConfig.ModId));
        if (TryLoadYamlFiles(projectFolder, out var globalScans))
            AddSignaturesFromYaml(globalScans);
        if (TryLoadYamlFiles(Path.Join(projectFolder, Project.Instance.AppId), out var appScans))
            AddSignaturesFromYaml(appScans);
    }

    private static void AddSignaturesFromYaml(ScanModel model)
    {
        var asDict = model.ToDictionary();
        foreach (var (name, candidates) in asDict)
        {
            if (Signatures.TryGetValue(name, out var oldEntry))
                oldEntry.IsLatest = false;
            Signatures[name] = new(name, candidates);
            foreach (var candidate in candidates)
                Signatures[name].CreateCandidateScan(candidate);
        }
    }

    private static bool TryLoadYamlFiles(string folder, [MaybeNullWhen(false)] out ScanModel scans)
    {
        scans = null;
        if (!Path.Exists(folder)) return false;
        scans = Directory.EnumerateFiles(folder, "scans.yaml", SearchOption.TopDirectoryOnly)
            .Select(ScanModel.FromPath).FirstOrDefault();
        return true;
    }
}