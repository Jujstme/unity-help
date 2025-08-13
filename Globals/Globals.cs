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
    public const int HookRetryDelay = 16;
}
