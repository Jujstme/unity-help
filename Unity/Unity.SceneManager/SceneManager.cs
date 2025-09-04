using JHelper.Common.ProcessInterop;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;

namespace JHelper.UnityManagers.SceneManager;

/// <summary>
/// Provides access to Unity's <c>SceneManager</c>
/// by reading from the UnityPlayer module in memory.
/// 
/// This class allows retrieval of the currently active scene,
/// the <c>DontDestroyOnLoad</c> scene, all loaded scenes,
/// and scene metadata such as index and path.
/// </summary>
public partial class SceneManager
{
    /// <summary>
    /// Reference to the parent Unity helper
    /// </summary>
#if LIVESPLIT
    internal readonly global::Unity helper;
#else
    internal readonly JHelper.Unity helper;
#endif

    /// <summary>
    /// Indicates whether the game is running under IL2CPP.
    /// </summary>
    internal readonly bool isIL2CPP;

    /// <summary>
    /// Base memory address of the Unity SceneManager instance.
    /// </summary>
    private readonly IntPtr baseAddress;

    /// <summary>
    /// Offsets used for resolving SceneManager-related fields.
    /// </summary>
    internal readonly SceneManagerOffsets offsets;

    /// <summary>
    /// Initializes a new instance of the <see cref="SceneManager"/> class,
    /// scanning UnityPlayer.dll for the SceneManager base address.
    /// </summary>
    /// <param name="helper">The parent Unity helper.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown if the SceneManager address cannot be found or resolved.
    /// </exception>
#if LIVESPLIT
    public SceneManager(global::Unity helper)
#else
    public SceneManager(JHelper.Unity helper)
#endif
    {
        ProcessMemory process = helper.Process;

        this.helper = helper;
        this.offsets = new SceneManagerOffsets(process.Is64Bit);

#if LIVESPLIT
        this.isIL2CPP = helper.MonoType == global::Unity.MonoTypeEnum.IL2CPP;
#else
        this.isIL2CPP = helper.MonoType == JHelper.Unity.MonoTypeEnum.IL2CPP;
#endif

        // Retrieve UnityPlayer.dll module.
        ProcessModule unityPlayer = process.Modules["UnityPlayer.dll"];

        IntPtr ptr;
        if (helper.Process.Is64Bit)
        {
            ScanPattern[] patterns64 =
            [
                new ScanPattern(7, "48 83 EC 20 4C 8B ?5 ?? ?? ?? ?? 33 F6") { OnFound = addr => addr + 0x4 + process.Read<int>(addr) },
            ];

            ptr = patterns64.Select(pattern => process.Scan(pattern, unityPlayer)).FirstOrDefault(addr => addr != IntPtr.Zero);
        }
        else
        {
            ScanPattern[] patterns32 =
            {
                new ScanPattern(5, "55 8B EC 51 A1 ???????? 53 33 DB") { OnFound = addr => (IntPtr)process.Read<int>(addr) },
                new ScanPattern(-4, "53 8D 41 ?? 33 DB") { OnFound = addr => (IntPtr)process.Read<int>(addr) },
                new ScanPattern(7, "55 8B EC 83 EC 18 A1 ???????? 33 C9 53") { OnFound = addr => (IntPtr)process.Read<int>(addr) },
            };

            ptr = patterns32.Select(pattern => process.Scan(pattern, unityPlayer)).FirstOrDefault(addr => addr != IntPtr.Zero);
        }

        if (ptr == IntPtr.Zero)
            throw new ArgumentNullException("Address", "Could not find the scene manager");

        if (!process.ReadPointer(ptr, out baseAddress) || baseAddress == IntPtr.Zero)
            throw new ArgumentNullException("Address", "Could not find the scene manager");
    }

    /// <summary>
    /// Gets the currently active scene.
    /// </summary>
    public Scene Current
    {
        get => new(this, helper.Process.ReadPointer(baseAddress + offsets.activeScene));
    }

    /// <summary>
    /// Gets the <c>DontDestroyOnLoad</c> scene.
    /// This scene persists across scene changes.
    /// </summary>
    public Scene DontDestroyOnLoad
    {
        get => new(this, baseAddress + offsets.dontDestroyOnLoadScene);
    }

    /// <summary>
    /// Gets the index of the currently active scene.
    /// </summary>
    public int CurrentSceneIndex => Current.Index;

    /// <summary>
    /// Gets the path of the currently active scene.
    /// </summary>
    public string CurrentScenePath => Current.Path;

    /// <summary>
    /// Gets the total number of loaded scenes.
    /// </summary>
    public int SceneCount => helper.Process.Read<int>(baseAddress + offsets.sceneCount);

    /// <summary>
    /// Enumerates all currently loaded scenes.
    /// </summary>
    public IEnumerable<Scene> Scenes
    {
        get
        {
            ProcessMemory process = helper.Process;

            return process.Is64Bit
                ? Get<long>()
                : Get<int>();

            IEnumerable<Scene> Get<T>() where T : unmanaged
            {
                Span<T> buf = stackalloc T[3];
                process.ReadArray<T>(baseAddress + offsets.sceneCount, buf);
                int numScenes = Unsafe.ToInt(buf[0]);;
                IntPtr addr = Unsafe.ToIntPtr(buf[2]);

                T[] scenes = ArrayPool<T>.Shared.Rent(numScenes);
                try
                {
                    if (!process.ReadArray<T>(addr, scenes.AsSpan(0, numScenes)))
                        yield break;

                    for (int i = 0; i < numScenes; i++)
                    {
                        var saddr = Unsafe.ToIntPtr(scenes[i]);
                        if (saddr == IntPtr.Zero)
                            continue;
                        yield return new Scene(this, saddr);
                    }
                }
                finally
                {
                    ArrayPool<T>.Shared.Return(scenes);
                }
            }
        }
    }
}