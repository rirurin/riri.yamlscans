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
    public SHFunction2(TFunction hookFunction) : this(hookFunction, null) {}

    /// <summary>
    /// Creates a <see cref="SHFunction2{TFunction}"/> with both a function wrapper
    /// and function hook and additionally includes a callback triggered when a scan is found.
    /// </summary>
    /// <param name="hookFunction">Hook function.</param>
    /// <param name="onScanFound">Callback called when a scan is found</param>
    public SHFunction2(TFunction hookFunction, Action<nint>? onScanFound) : this(onScanFound)
        => _HookFunction = hookFunction;
    
    /// <summary>
    /// Creates a <see cref="SHFunction2{TFunction}"/> with only a function wrapper,
    /// and the option to set a hook separately with <see cref="SetHook"/>.
    /// </summary>
    public SHFunction2() : this(null) {}

    /// <summary>
    /// Creates a <see cref="SHFunction2{TFunction}"/> with only a function wrapper,
    /// and the option to set a hook separately with <see cref="SetHook"/>. Additionally, a callback can be
    /// specified to execute when a scan is found.
    /// </summary>
    /// <param name="onScanFound"></param>
    public SHFunction2(Action<nint>? onScanFound)
    {
        _Name = typeof(TFunction).Name;
        YamlScans._sharedScans!.AddScan(_Name, null);
        YamlScans._sharedScans!.CreateListener(_Name, result =>
        {
            if (_Function != null) return; // Don't call this more than once
            _Function = YamlScans._hooks!.CreateFunction<TFunction>(result);
            if (_HookFunction != null) Hook = _Function!.Hook(_HookFunction).Activate();
            onScanFound?.Invoke(result);
        });
    }
    
    /// <summary>
    /// Set a function to create a <see cref="IReloadedHooks"/> hook with.
    /// Must be done before scanning has started, during normal mod initialization.
    /// </summary>
    /// <param name="hookFunction">The hook function. If <c>null</c>, no hook will be created.</param>
    public void SetHook(TFunction? hookFunction) => _HookFunction = hookFunction;

    /// <summary>
    /// Programmatically set the address for the function, instead of retrieving the result from Scans YAML.
    /// </summary>
    /// <param name="value">Address pointing to the start of the function.</param>
    public void SetResult(nint value) => YamlScans._sharedScans!.Broadcast(_Name, value);

    /// <summary>
    /// Check if the function hook is enabled. If there is no function hook, this will always be false.
    /// </summary>
    public bool IsHookEnabled => Hook?.IsHookEnabled ?? false;
    
    /// <summary>
    /// Toggles the function hook between enabled and disabled.
    /// </summary>
    /// <returns>The new enable state for the function hook.</returns>
    public bool ToggleEnabled()
    {
        if (Hook == null) return false;
        if (Hook.IsHookEnabled) Hook.Disable();
        else Hook.Enable();
        return Hook.IsHookEnabled;
    }
    
    /// <summary>
    /// If a hook exists for this function, disables the function hook. This is useful in cases where you want to
    /// unload a portion of your mod's functionality.
    /// </summary>
    public void Disable()
    {
        if (Hook is { IsHookEnabled: true }) Hook.Disable();
    }

    /// <summary>
    /// If it exists, enables the function hook if it's been disabled. This is useful when you want to re-enable
    /// a disabled function hook.
    /// </summary>
    public void Enable()
    {
        if (Hook is { IsHookEnabled: false }) Hook.Enable();
    }
}