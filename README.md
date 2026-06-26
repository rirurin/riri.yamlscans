# riri.yamlscans

## Usage

A C# library for reading YAML Scan files, which are YAML files with a specific formatting to facilitate searching for memory locations within an executable. This is designed to integrate with Reloaded-II mods and a default implementation is available with `riri.yamlscans.ReloadedII`, but is designed to be portable enough to work on other .NET modding APIs.

This is based on Scan INI from [RyoTune.Reloaded](https://github.com/RyoTune/RyoTune.Reloaded/) but includes extra features such as setting multiple signatures for each scan, which allow mods to support multiple game versions if a signature breaks between those versions.

`riri.yamlscans` and `riri.yamlscans.ReloadedII` are available on NuGet.

### YAML Scans

YAML scans follow a similar format to Scan INI in that they are stored in `MOD_FOLDER/Project/MOD_ID/scans.yaml`.

Signatures can either be defined in a Scan INI-like format for users familiar with RyoTune.Reloaded or in a YAML style format:

```yaml

# INI style format

ULevelStreamingDynamic_LoadLevelInstance: "E8 ?? ?? ?? ?? 48 8B 4D ?? 49 89 06 48 85 C9 74 ?? E8 ?? ?? ?? ?? 48 8B 4D ?? 48 85 C9 74 ?? E8 ?? ?? ?? ?? 48 8B 9C 24 ?? ?? ?? ??"
ULevelStreaming_GetStreamingLevel: "48 89 54 24 ?? 55 53 56 57 41 55 41 56 41 57 48 8B EC 48 83 EC 40"

# YAML style format

ULevelStreamingDynamic_LoadLevelInstance:
    signatures: "E8 ?? ?? ?? ?? 48 8B 4D ?? 49 89 06 48 85 C9 74 ?? E8 ?? ?? ?? ?? 48 8B 4D ?? 48 85 C9 74 ?? E8 ?? ?? ?? ?? 48 8B 9C 24 ?? ?? ?? ??"
ULevelStreaming_GetStreamingLevel:
    signatures: "48 89 54 24 ?? 55 53 56 57 41 55 41 56 41 57 48 8B EC 48 83 EC 40"
```

To adjust the address of a scan once it's discovered, define a transform by either setting the `[signature_name]_RESULT` (the name of your scan + `_RESULT`) value in INI style or by setting the `transforms` value in YAML style:

```yaml

# INI style

ULevelStreamingDynamic_LoadLevelInstance: "E8 ?? ?? ?? ?? 48 8B 4D ?? 49 89 06 48 85 C9 74 ?? E8 ?? ?? ?? ?? 48 8B 4D ?? 48 85 C9 74 ?? E8 ?? ?? ?? ?? 48 8B 9C 24 ?? ?? ?? ??"
ULevelStreamingDynamic_LoadLevelInstance_RESULT: "GetIndirectAddressShort"

# YAML style

ULevelStreamingDynamic_LoadLevelInstance:
    signatures: "E8 ?? ?? ?? ?? 48 8B 4D ?? 49 89 06 48 85 C9 74 ?? E8 ?? ?? ?? ?? 48 8B 4D ?? 48 85 C9 74 ?? E8 ?? ?? ?? ?? 48 8B 9C 24 ?? ?? ?? ??"
    transforms: "GetIndirectAddressShort"

```

To define multiple signatures for a particular scan, define an array of strings instead of a single string. Make sure that if you're using transforms that the length of the transform array is the same as the length of the signature array:

```yaml

# INI style

ULevelStreamingDynamic_LoadLevelInstance: ["E8 ?? ?? ?? ?? 48 8B 4D ?? 49 89 06 48 85 C9 74 ?? E8 ?? ?? ?? ?? 48 8B 4D ?? 48 85 C9 74 ?? E8 ?? ?? ?? ?? 48 8B 9C 24 ?? ?? ?? ??", "48 89 54 24 ?? 55 53 56 57 41 55 41 56 41 57 48 8B EC 48 83 EC 40"]
ULevelStreamingDynamic_LoadLevelInstance_RESULT: ["GetIndirectAddressShort2", "GetIndirectAddressShort2"]

# YAML style

ULevelStreamingDynamic_LoadLevelInstance:
    signatures: ["E8 ?? ?? ?? ?? 48 8B 4D ?? 49 89 06 48 85 C9 74 ?? E8 ?? ?? ?? ?? 48 8B 4D ?? 48 85 C9 74 ?? E8 ?? ?? ?? ?? 48 8B 9C 24 ?? ?? ?? ??", "48 89 54 24 ?? 55 53 56 57 41 55 41 56 41 57 48 8B EC 48 83 EC 40"]
    transforms: ["GetIndirectAddressShort2", "GetIndirectAddressShort2"]

```

#### Available Built in Transforms

All of the functions below include an additional step of checking if the target function is a thunk function (a function consisting of a single jump instruction to another function) and travels to that function instead. This prevents issues where the game crashes when attempting to construct a hook on the thunk.

- `GetDirectAddress`: Adds the base address of the executable to the value. This is the default option.
- `GetDirectAddressAbsolute`: No effect, only runs the thunk check
- `GetIndirectAddressShort`: An address transformation that takes `result + 1` and dereferences the Int32 relative pointer at that location. In Scans INI, this is equivalent to `GetGlobalAddress(result + 1)`
- `GetIndirectAddressShort2`: Equivalent to `GetGlobalAddress(result + 2)` in Scans INI
- `GetIndirectAddressLong`: Equivalent to `GetGlobalAddress(result + 3)` in Scans INI
- `GetIndirectAddressLong4`: Equivalent to `GetGlobalAddress(result + 4)` in Scans INI

*(The GetIndirectAddress transforms have an equivalent function in `riri_mod_tools_rt::sigscan_resolver` in [riri-mod-tools](https://github.com/rirurin/riri-mod-tools) for the one person writing R2 mods in Rust)*

#### Custom Transforms

Custom address transforms are possible in cases where the defaults don't meet your needs. This leverages the [NCalc](https://github.com/ncalc/ncalc) expression evaluator to perform arithmetic and call certain functions. 

As an example, `GetIndirectAddressShort` written as an expression is: `TryDeref(GetGlobalAddress(GetDirectAddress(result) + 1))`

When writing expressions, the following input is available:

- **result**: The initial value. This is a **relative offset** from the executable instead of an absolute address like in Scans INI.

and the following functions are available:

- `GetDirectAddress`: Adds the base address of the executable to the result. This is usually the first thing you do so that you can convert the relative offset into an absolute pointer
- `GetGlobalAddress`: Dereference an Int32 relative pointer. These are usually encoded into instructions that use pointers
- `DerefData`: Dereference a pointer-sized absolute pointer, such as a function pointer in a VTable
- `TryDeref`: Perform the thunk check. This is optional to allow for special cases where you don't run the check
- `GetIndirectAddressShort`: see above
- `GetIndirectAddressShort2`: see above
- `GetIndirectAddressLong`: see above
- `GetIndirectAddressLong4`: see above

### Integrating with Reloaded-II

To use in a Reloaded-II mod, install `riri.yamlscans.ReloadedII` from NuGet into your mod's project. 
Ensure that your project has: 
- `RyoTune.Reloaded`, `Reloaded.Mod.Interfaces`, `Reloaded.SharedLib.Hooks` and `SharedScans.Interfaces` **added in NuGet**
- `Reloaded.Memory.SigScan.ReloadedII`, `reloaded.sharedlib.hooks` and `SharedScans.Reloaded` **in your `ModConfig.json`**. 

Then, call `YamlScans.Initialize` after you have called `Project.Initialize`:

```c#
public Mod(ModContext context) 
{
    // ...
    Project.Initialize(_modConfig, _modLoader, _logger, true);
    Log.LogLevel = _configuration.LogLevel;
    YamlScans.Initialize(_modConfig, _modLoader);
    // ...
}

```

#### `SHFunction2<TFunction>`

To add a function wrapper or hook that retrieves it's location from YAML Scans, declare a `SHFunction2<TFunction>`, with `TFunction` being the delegate for your function hook. This is designed as a drop-in replacement for `SHFunction`:

```c#
[Function(CallingConventions.Microsoft)]
private delegate byte UAtlEvtSubsystem_DoesLevelStreamingLevelExist(nint self, nint worldOut, nint pathOut);

private byte UAtlEvtSubsystem_DoesLevelStreamingLevelExistImpl(nint self, nint worldOut, nint pathOut)
{
    Log.Debug("UAtlEvtSubsystem::DoesLevelStreamingLevelExist was called");
    return _UAtlEvtSubsystem_DoesLevelStreamingLevelExist.Hook!.OriginalFunction(self, worldOut, pathOut);
}

private SHFunction2<UAtlEvtSubsystem_DoesLevelStreamingLevelExist> _UAtlEvtSubsystem_DoesLevelStreamingLevelExist;

public Mod(ModContext context)
{
    // ...
    Project.Initialize(_modConfig, _modLoader, _logger, true);
    Log.LogLevel = _configuration.LogLevel;
    YamlScans.Initialize(_modConfig, _modLoader);
    // ...
    _UAtlEvtSubsystem_DoesLevelStreamingLevelExist = new(UAtlEvtSubsystem_DoesLevelStreamingLevelExistImpl);
}

```

#### `SHStatic<TPointer>`

To add a pointer to static data within the executable, declare a `SHStatic<TPointer>`, where TPointer is the data type for the static data: 

```c#
private SHStatic<nint> _GEngine;

public Mod(ModContext context)
{
    // ...
    Project.Initialize(_modConfig, _modLoader, _logger, true);
    Log.LogLevel = _configuration.LogLevel;
    YamlScans.Initialize(_modConfig, _modLoader);
    // ...
    _GEngine = new("GEngine");
}
```

To get the inner value, call, then dereference the `SHStatic<TPointer>.Instance` property (`_GEngine.Instance`).
In some cases, the static data is itself a pointer to a heap allocation, which is common practice with singletons (in Unreal, GEngine is a `UEngine*`). In this case, the `Ptr<T>` type is provided:

```c#
private SHStatic<Ptr<UEngine>> _GEngine;

// ... Do the same thing as shown above with initialization
// To access the inner value:
var GEngine = (*_GEngine.Instance).Value;
```