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
    private SceneManager? _sceneManager;

    public SceneManager SceneManager
    {
        get
        {
            if (_sceneManager is null)
            {
                // Letting the code retry 5 times before throwing
                for (int i = 0; i < Globals.HookRetryAttempts; i++)
                {
                    try
                    {
                        _sceneManager = new(this);
                        break;
                    }
                    catch
                    {
                        Task.Delay(Globals.HookRetryDelay).Wait();
                    }
                }

                if (_sceneManager is null)
                    throw new InvalidOperationException("Failed to load the SceneManager.");

                Log.Info($"  => Scene Manager loaded");
            }

            return _sceneManager;
        }
    }
}
