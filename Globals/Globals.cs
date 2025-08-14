using System;

namespace JHelper;

/// <summary>
/// Contains global constants used throughout the JHelper library.
/// </summary>
public static class Globals
{
#if LIVESPLIT
    /// <summary>
    /// Name of the variable defined inside the .asl script that stores the helper instance
    /// </summary>
    public const string HelperVarName = "Helper";
#endif

    /// <summary>
    /// Number of attempts to retry when instantiating new instances of IL2CPP, Mono, or SceneManager hooks.
    /// </summary>
    public const int HookRetryAttempts = 5;

    /// <summary>
    /// Delay in milliseconds between retry attempts for hooking.
    /// </summary>
    public const int HookRetryDelay = 150;
}

public static class Unsafe
{
    unsafe internal static int ToInt<T>(T value) where T : unmanaged => *(int*)&value;

    unsafe internal static IntPtr ToIntPtr<T>(T value) where T : unmanaged
    {
        if (sizeof(T) == 4)
            return (IntPtr)(*(int*)&value);
        else if (sizeof(T) == 8)
            return (IntPtr)(*(long*)&value);
        else
            throw new InvalidOperationException($"Unsupported type {typeof(T)} for conversion to IntPtr.");
    }
}


