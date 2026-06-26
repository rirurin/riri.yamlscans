namespace riri.yamlscans.ReloadedII;

/// <summary>
/// A wrapper type to allow pointers to be used in type parameters
/// </summary>
/// <param name="value">The pointer to wrap</param>
/// <typeparam name="T">Data type for the pointer</typeparam>
public readonly unsafe struct Ptr<T>(T* value) : IEquatable<Ptr<T>>
    where T : unmanaged
{
    /// <summary>
    /// Retrivees the raw value
    /// </summary>
    public readonly T* Value = value;

    /// <inheritdoc/>
    public bool Equals(Ptr<T> other) => Value == other.Value;

    /// <inheritdoc/>
    public static bool operator ==(Ptr<T> a, Ptr<T> b) => a.Equals(b);

    /// <inheritdoc/>
    public static bool operator !=(Ptr<T> a, Ptr<T> b) => !a.Equals(b);

    /// <inheritdoc/>
    public override string ToString()
        => $"Ptr<{typeof(T).Name}>({(Value != null ? Value->ToString() : "")})";
}