using JHelper;
using JHelper.Logging;
using JHelper.UnityManagers.IL2CPP;
using System;
using System.Threading.Tasks;

#if !LIVESPLIT
namespace JHelper;
#endif

public partial class Unity
{
    private readonly object il2cppLock = new();

    /// <summary>
    /// Gets the <see cref="IL2CPP"/> manager for the hooked Unity game.
    /// This property is only valid if the current game is IL2CPP-based.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// Thrown if the current game is not IL2CPP-based, or if the IL2CPP
    /// manager fails to initialize after multiple attempts.
    /// </exception>
    public IL2CPP IL2CPP
    {
        get
        {
            if (MonoType != MonoTypeEnum.IL2CPP)
                throw new InvalidOperationException("You are trying to access the IL2CPP manager on a non-IL2CPP game");

            if (field is null)
            {
                lock (il2cppLock)
                {
                    // Attempt to initialize the IL2CPP manager, retrying up to 5 times.
                    for (int i = 0; i < Globals.HookRetryAttempts; i++)
                    {
                        try
                        {
                            field = new IL2CPP(this);
                            break;
                        }
                        catch
                        {
                            Task.Delay(Globals.HookRetryDelay).Wait();
                        }
                    }
                }

                if (field is null)
                    throw new InvalidOperationException("Failed to instantiate the IL2CPP manager.");

                // Log details about the hooked Unity process and IL2CPP version.
                Log.Info($"  => Unity Game process: {Process.ProcessName}");
                Log.Info($"  => Using IL2CPP struct version: {field.Version}");
            }

            return field;
        }

        private set
        {
            lock (il2cppLock)
            {
                field = value;
            }
        }
    }
}
