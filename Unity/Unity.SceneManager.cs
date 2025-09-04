using JHelper;
using JHelper.Logging;
using JHelper.UnityManagers.SceneManager;
using System;
using System.Threading.Tasks;

#if !LIVESPLIT
namespace JHelper;
#endif

public partial class Unity
{
    public SceneManager SceneManager
    {
        get
        {
            if (field is null)
            {
                // Letting the code retry 5 times before throwing
                for (int i = 0; i < Globals.HookRetryAttempts; i++)
                {
                    try
                    {
                        field = new(this);
                        break;
                    }
                    catch
                    {
                        Task.Delay(Globals.HookRetryDelay).Wait();
                    }
                }

                if (field is null)
                    throw new InvalidOperationException("Failed to load the SceneManager.");

                Log.Info($"  => Scene Manager loaded");
            }

            return field;
        }

        private set
        {
            field = value;
        }
    }
}
