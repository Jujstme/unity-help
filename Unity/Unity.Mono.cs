using JHelper;
using JHelper.Logging;
using JHelper.UnityManagers.Mono;
using System;
using System.Threading.Tasks;

#if !LIVESPLIT
namespace JHelper;
#endif

public partial class Unity
{
    private readonly object monoLock = new();

    /// <summary>
    /// Gets the <see cref="Mono"/> manager for the hooked Unity game.
    /// This property is only valid if the current game is Mono-based.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// Thrown if the current game is not Mono-based, or if the Mono manager
    /// fails to initialize after multiple attempts.
    /// </exception>
    public Mono Mono
    {
        get
        {
            if (MonoType != MonoTypeEnum.Mono)
                throw new InvalidOperationException("You are trying to access the Mono manager on a non-Mono game");

            if (field is null)
            {
                lock (monoLock)
                {
                    // Attempt to initialize the Mono manager, retrying up to 5 times.
                    for (int i = 0; i < Globals.HookRetryAttempts; i++)
                    {
                        try
                        {
                            field = new Mono(this);
                            break;
                        }
                        catch
                        {
                            Task.Delay(Globals.HookRetryDelay).Wait();
                        }
                    }
                }

                if (field is null)
                    throw new InvalidOperationException("Failed to instantiate the Mono manager.");

                // Log details about the hooked Unity process and Mono version.
                Log.Info($"  => Unity Game process: {Process.ProcessName}");
                Log.Info($"  => Using Mono struct version: {field.Version}");
            }

            return field;
        }

        private set
        {
            lock (monoLock)
            {
                field = value;
            }
        }
    }
}

