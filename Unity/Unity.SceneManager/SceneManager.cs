using JHelper.Common.ProcessInterop;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;

namespace JHelper.UnityManagers.SceneManager;

public partial class SceneManager
{
#if LIVESPLIT
    internal readonly global::Unity helper;
#else
    internal readonly JHelper.Unity helper;
#endif

    internal readonly bool isIL2CPP;
    private readonly IntPtr baseAddress;
    internal readonly SceneManagerOffsets offsets;

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

    public Scene Current
    {
        get => new(this, helper.Process.ReadPointer(baseAddress + offsets.activeScene));
    }

    public Scene DontDestroyOnLoad
    {
        get => new(this, baseAddress + offsets.dontDestroyOnLoadScene);
    }

    public int CurrentSceneIndex => Current.Index;
    public string CurrentScenePath => Current.Path;

    public int SceneCount => helper.Process.Read<int>(baseAddress + offsets.sceneCount);

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
                int numScenes = ToInt(buf[0]);;
                IntPtr addr = ToIntPtr(buf[2]);

                T[] scenes = ArrayPool<T>.Shared.Rent(numScenes);
                try
                {
                    if (!process.ReadArray<T>(addr, scenes.AsSpan(0, numScenes)))
                        yield break;

                    for (int i = 0; i < numScenes; i++)
                    {
                        if (ToIntPtr(scenes[i]) == IntPtr.Zero)
                            continue;
                        yield return new Scene(this, ToIntPtr(scenes[i]));
                    }
                }
                finally
                {
                    ArrayPool<T>.Shared.Return(scenes);
                }


                unsafe int ToInt(T value) => *(int*)&value;
                unsafe IntPtr ToIntPtr(T value)
                {
                    if (typeof(T) == typeof(int))
                        return (IntPtr)(*(long*)&value);
                    else if (typeof(T) == typeof(long))
                        return (IntPtr)(*(int*)&value);
                    else
                        throw new InvalidOperationException($"Unsupported type {typeof(T)} for conversion to IntPtr.");
                }
            }
        }
    }
}