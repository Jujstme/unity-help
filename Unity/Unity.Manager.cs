using JHelper;
using JHelper.Logging;
using JHelper.UnityManagers.IL2CPP;
using JHelper.UnityManagers.Abstractions;
using JHelper.UnityManagers.Mono;
using System;
using System.Threading.Tasks;

#if !LIVESPLIT
namespace JHelper;
#endif

public partial class Unity
{
    /// <summary>
    /// Gets the underlying <see cref="UnityManager"/> for the hooked Unity game.
    /// This may be an IL2CPP or Mono manager depending on the runtime.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// Thrown if the manager cannot be initialized after multiple retry attempts.
    /// </exception>
    public UnityManager Manager
    {
        get
        {
            if (field is null)
            {
                // Attempt to initialize the manager, retrying up to 5 times.
                for (int i = 0; i < Globals.HookRetryAttempts; i++)
                {
                    try
                    {
                        field = MonoType == MonoTypeEnum.IL2CPP
                            ? new IL2CPP(this)
                            : new Mono(this);
                        break;
                    }
                    catch
                    {
                        Task.Delay(Globals.HookRetryDelay).Wait();
                    }
                }

                // Throw if initialization still failed.
                if (field is null)
                    throw new InvalidOperationException("Failed to instantiate the Unity manager.");

                // Log information about the hooked Unity process and runtime version.
                Log.Info($"  => Unity Game process: {Process.ProcessName}");
                    
                if (field is IL2CPP il2cpp)
                    Log.Info($"  => Using IL2CPP struct version: {il2cpp.Version}");
                else if (field is Mono mono)
                    Log.Info($"  => Using Mono struct version: {mono.Version}");
            }

            return field;
        }

        private set
        {
            field = value;
        }
    }

    /// <summary>
    /// Indexer for accessing <see cref="UnityImage"/>s by name.
    /// </summary>
    /// <param name="name">The name of the assembly to retrieve.</param>
    /// <returns>The <see cref="UnityImage"/> corresponding to the given assembly name.</returns>
    public UnityImage this[string name] => Manager[name];
}
