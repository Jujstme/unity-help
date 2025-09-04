using System;

namespace JHelper;

/// <summary>
/// Contains global constants used throughout the JHelper library.
/// </summary>
public static class Globals
{
#if LIVESPLIT
    /// <summary>
    /// Name of the variable defined inside the LiveSplit .asl script 
    /// that stores the helper instance.
    /// </summary>
    public const string HelperVarName = "Helper";
#endif

    /// <summary>
    /// Number of attempts to retry when instantiating new instances of
    /// IL2CPP, Mono, or SceneManager hooks.
    /// </summary>
    public const int HookRetryAttempts = 5;

    /// <summary>
    /// Delay in milliseconds between retry attempts for hooking.
    /// </summary>
    public const int HookRetryDelay = 150;
}

/// <summary>
/// Internal helper class providing low-level memory reinterpretation functions.
/// <para>
/// These functions read the underlying bytes of a value and treat them as another type
/// without performing any type conversion. Should be used only within the JHelper assembly.
/// </para>
/// <para>
/// WARNING: Misuse can corrupt memory, crash the process, or lead to undefined behavior.
/// </para>
/// </summary>
internal static class Unsafe
{
    /// <summary>
    /// Reinterprets the memory of an unmanaged value as an <see cref="int"/>.
    /// The underlying bits are preserved; no numeric conversion occurs.
    /// </summary>
    /// <typeparam name="T">The unmanaged type to reinterpret. Must have size ≥ 4 bytes.</typeparam>
    /// <param name="value">The value whose memory will be reinterpreted.</param>
    /// <returns>The <see cref="int"/> representing the same bits in memory.</returns>
    unsafe internal static int ToInt<T>(T value) where T : unmanaged => sizeof(T) < 4
        ? throw new InvalidOperationException($"Cannot reinterpret {typeof(T)} as int: size is less than 4 bytes.")
        : *(int*)&value;

    /// <summary>
    /// Reinterprets the memory of an unmanaged value as an <see cref="IntPtr"/>.
    /// The underlying bits are preserved; no numeric conversion occurs.
    /// </summary>
    /// <typeparam name="T">The unmanaged type to reinterpret. Must be 4 bytes on 32-bit or 8 bytes on 64-bit processes.</typeparam>
    /// <param name="value">The value whose memory will be reinterpreted.</param>
    /// <returns>The <see cref="IntPtr"/> representing the same bits in memory.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown if the type size is neither 4 nor 8 bytes, which are required for pointer representation.
    /// </exception>
    unsafe internal static IntPtr ToIntPtr<T>(T value) where T : unmanaged => sizeof(T) == 4
        ? (IntPtr)(*(int*)&value)
        : sizeof(T) == 8
        ? (IntPtr)(*(long*)&value)
        : throw new InvalidOperationException($"Unsupported type {typeof(T)} for conversion to IntPtr.");
}