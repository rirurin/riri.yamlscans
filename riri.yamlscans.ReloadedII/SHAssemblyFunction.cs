using Reloaded.Hooks.Definitions;
using Reloaded.Hooks.Definitions.Enums;

namespace riri.yamlscans.ReloadedII;

/// <summary>
/// Create a hook to run assembly code inside of a native function from a YAML Scan Result.
/// </summary>
public class SHAssemblyFunction
{
    private readonly string _Name;
    private IAsmHook? _AssemblyHook;

    /// <summary>
    /// Creates a <see cref="SHAssemblyFunction"/> using the default hook behavior (executes first)
    /// </summary>
    /// <param name="Name">Name of the assembly hook</param>
    /// <param name="AssemblyCode">Assembly instructions to execute</param>
    public SHAssemblyFunction(string Name, string[] AssemblyCode) : this(Name, AssemblyCode, AsmHookBehaviour.ExecuteFirst) {}

    /// <summary>
    /// Creates a <see cref="SHAssemblyFunction"/>
    /// </summary>
    /// <param name="Name">Name of the assembly hook</param>
    /// <param name="AssemblyCode">Assembly instructions to execute</param>
    /// <param name="HookBehavior">When the custom assembly is executed</param>
    public SHAssemblyFunction(string Name, string[] AssemblyCode, 
        AsmHookBehaviour HookBehavior = AsmHookBehaviour.ExecuteFirst)
    {
        _Name = Name;
        YamlScans._sharedScans!.AddScan(_Name, null);
        YamlScans._sharedScans!.CreateListener(_Name, result =>
        {
            _AssemblyHook = YamlScans._hooks!.CreateAsmHook(AssemblyCode, result, HookBehavior).Activate();
        });
    }
}

/// <summary>
/// Implement register preserving and retrieval for a specific calling convention. This is intended to save registers
/// that are used as parameters by C functions. A list of common calling conventions are available at
/// https://en.wikipedia.org/wiki/X86_calling_conventions
/// </summary>
public interface IRegisterPreserveForCallingConvention
{
    /// <summary>
    /// Size of registers for the calling convention. Expected to be 64 or 32.
    /// </summary>
    int RegisterSize { get; }
    /// <summary>
    /// Assembly to preserve registers.
    /// </summary>
    /// <returns>Assembly instructions</returns>
    string Preserve();
    /// <summary>
    /// Assembly to retrieve registers.
    /// </summary>
    /// <returns>Assembly instructions</returns>
    string Retrieve();
}

/// <summary>
/// Register preservation and retrieval for the Microsoft x64 calling convention. This only preserves integer registers.
/// </summary>
public class RegistersMicrosoftX64 : IRegisterPreserveForCallingConvention
{
    /// <summary>
    /// Size of registers for the calling convention. Expected to be 64 or 32.
    /// </summary>
    public int RegisterSize => 64;
    /// <summary>
    /// Assembly to preserve registers.
    /// </summary>
    /// <returns>Assembly instructions</returns>
    public virtual string Preserve() => "push rcx\npush rdx\npush r8\npush r9";
    /// <summary>
    /// Assembly to retrieve registers.
    /// </summary>
    /// <returns>Assembly instructions</returns>
    public virtual string Retrieve() => "pop r9\npop r8\npop rdx\npop rcx";
}

/// <summary>
/// Create a hook to run a C# function inside of a native function from a YAML Scan Result.
/// </summary>
/// <typeparam name="TFunction">Delegate type of the function. Also used as the name searched for in YAML Scans</typeparam>
public class SHAssemblyFunction<TFunction> where TFunction: Delegate
{
    private readonly string _Name;
    private IAsmHook? _AssemblyHook;
    private IReverseWrapper<TFunction>? _ReverseWrapper;
    private TFunction? _HookFunction;

    /// <summary>
    /// Creates a <see cref="SHAssemblyFunction{TFunction}"/> with the default calling convention (Microsoft X64) 
    /// and hook behavior (executes first)
    /// </summary>
    /// <param name="HookFunction">Hook function</param>
    public SHAssemblyFunction(TFunction HookFunction) : this(new RegistersMicrosoftX64(), HookFunction, AsmHookBehaviour.ExecuteFirst) { }

    /// <summary>
    /// Creates a <see cref="SHAssemblyFunction{TFunction}"/> with the default calling convention (Microsoft X64)
    /// </summary>
    /// <param name="HookFunction">Hook function</param>
    /// <param name="HookBehavior">When the assembly hook is executed. By default, the hook runs first</param>
    public SHAssemblyFunction(TFunction HookFunction, AsmHookBehaviour HookBehavior)
        : this(new RegistersMicrosoftX64(), HookFunction, HookBehavior) {}

    /// <summary>
    /// Creates a <see cref="SHAssemblyFunction{TFunction}"/> with the specified calling convention and default
    /// hook behavior (executes first)
    /// </summary>
    /// <param name="PreserveRegisters">The calling convention strategy to preserve/retrieve registers</param>
    /// <param name="HookFunction">Hook function</param>
    public SHAssemblyFunction(IRegisterPreserveForCallingConvention PreserveRegisters, TFunction HookFunction) : this(
        [$"use{PreserveRegisters.RegisterSize}", PreserveRegisters.Preserve()],
        HookFunction, [PreserveRegisters.Retrieve()], AsmHookBehaviour.ExecuteFirst)
    { }

    /// <summary>
    /// Creates a <see cref="SHAssemblyFunction{TFunction}"/> with the specified calling convention
    /// </summary>
    /// <param name="PreserveRegisters">The calling convention strategy to preserve/retrieve registers</param>
    /// <param name="HookFunction">Hook function</param>
    /// <param name="HookBehavior">When the assembly hook is executed. By default, the hook runs first</param>
    public SHAssemblyFunction(IRegisterPreserveForCallingConvention PreserveRegisters, TFunction HookFunction,
        AsmHookBehaviour HookBehavior) : this(
        [$"use{PreserveRegisters.RegisterSize}", PreserveRegisters.Preserve()], 
        HookFunction, [PreserveRegisters.Retrieve()], HookBehavior) {}

    /// <summary>
    /// Creates a <see cref="SHAssemblyFunction{TFunction}"/>, with custom assembly before and after the function
    /// and default hook behavior (executes first)
    /// </summary>
    /// <param name="Prefix">Assembly instructions to run before the function hook</param>
    /// <param name="HookFunction">Hook function</param>
    /// <param name="Postfix">Assembly instructions to run after the function hook</param>
    public SHAssemblyFunction(string[] Prefix, TFunction HookFunction, string[] Postfix)
        : this(Prefix, HookFunction, Postfix, AsmHookBehaviour.ExecuteFirst) {}

    /// <summary>
    /// Creates a <see cref="SHAssemblyFunction{TFunction}"/>, with custom assembly before and after the function
    /// </summary>
    /// <param name="Prefix">Assembly instructions to run before the function hook</param>
    /// <param name="HookFunction">Hook function</param>
    /// <param name="Postfix">Assembly instructions to run after the function hook</param>
    /// <param name="HookBehavior">When the assembly hook is executed. By default, the hook runs first</param>
    public SHAssemblyFunction(string[] Prefix, TFunction HookFunction, string[] Postfix, AsmHookBehaviour HookBehavior)
    {
        _Name = typeof(TFunction).Name;
        _HookFunction = HookFunction;
        YamlScans._sharedScans!.AddScan(_Name, null);
        YamlScans._sharedScans!.CreateListener(_Name, result =>
        {
            var Code = Prefix.ToList();
            Code.Add(YamlScans._hooks!.Utilities.GetAbsoluteCallMnemonics(_HookFunction, out _ReverseWrapper));
            Code.AddRange(Postfix);
            _AssemblyHook = YamlScans._hooks!.CreateAsmHook(Code.ToArray(), result, HookBehavior).Activate();
        });
    }
}