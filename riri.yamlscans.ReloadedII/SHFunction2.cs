using Reloaded.Hooks.Definitions;

namespace riri.yamlscans.ReloadedII;

/// <summary>
/// Create a hook and/or wrapper from a YAML Scan result. Intended as a drop-in replacement for SHFunction from RyoTune.Reloaded
/// </summary>
/// <typeparam name="TFunction">Delegate type of the function. Also used as the name searched for in YAML Scans</typeparam>
public class SHFunction2<TFunction>
{
    private readonly string _Name;
    private IFunction<TFunction>? _Function;
    private TFunction? _HookFunction;
    
    /// <summary>
    /// <see cref="IReloadedHooks"/> instance, if a hook function was set.
    /// </summary>
    public IHook<TFunction>? Hook { get; private set; }
    
    /// <summary>
    /// Function wrapper for calling the native function.
    /// </summary>
    public TFunction Wrapper => _Function!.GetWrapper();
    
    /// <summary>
    /// Creates a <see cref="SHFunction2{TFunction}"/> with both a function wrapper
    /// and function hook.
    /// </summary>
    /// <param name="hookFunction">Hook function.</param>
    public SHFunction2(TFunction hookFunction) : this()
    {
        _HookFunction = hookFunction;
    }
    
    /// <summary>
    /// Creates a <see cref="SHFunction2{TFunction}"/> with only a function wrapper,
    /// and the option to set a hook separately with <see cref="SetHook"/>.
    /// </summary>
    public SHFunction2()
    {
        _Name = typeof(TFunction).Name;
        YamlScans._sharedScans!.AddScan(_Name, null);
        YamlScans._sharedScans!.CreateListener(_Name, result =>
        {
            _Function = YamlScans._hooks!.CreateFunction<TFunction>(result);
            if (_HookFunction != null) Hook = _Function!.Hook(_HookFunction).Activate();
        });
    }
    
    /// <summary>
    /// Set a function to create a <see cref="IReloadedHooks"/> hook with.
    /// Must be done before scanning has started, during normal mod initialization.
    /// </summary>
    /// <param name="hookFunction">The hook function. If <c>null</c>, no hook will be created.</param>
    public void SetHook(TFunction? hookFunction) => _HookFunction = hookFunction;
}