using JHelper.Common.ProcessInterop;
using System;
using System.Collections.Generic;

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
        this.helper = helper;
        this.offsets = new SceneManagerOffsets(helper.Process.Is64Bit);

#if LIVESPLIT
        this.isIL2CPP = helper.MonoType == global::Unity.MonoTypeEnum.IL2CPP;
#else
        this.isIL2CPP = helper.MonoType == JHelper.Unity.MonoTypeEnum.IL2CPP;
#endif

        ProcessModule unityPlayer = helper.Process.Modules["UnityPlayer.dll"];

        IntPtr ptr = IntPtr.Zero;
        if (helper.Process.Is64Bit)
        {
            ScanPattern[] patterns64 =
            [
                new ScanPattern(7, "48 83 EC 20 4C 8B ?5 ?? ?? ?? ?? 33 F6")
            ];

            foreach (ScanPattern pattern in patterns64)
            {
                ptr = this.helper.Process.Scan(pattern, unityPlayer);
                if (ptr != IntPtr.Zero)
                    break;
            }
            if (ptr == IntPtr.Zero)
                throw new ArgumentNullException("Address", "Could not find the scene manager");

            ptr = ptr + 0x4 + this.helper.Process.Read<int>(ptr);
        }
        else
        {
            ScanPattern[] patterns32 =
            {
                new ScanPattern(5, "55 8B EC 51 A1 ???????? 53 33 DB"),
                new ScanPattern(-4, "53 8D 41 ?? 33 DB"),
                new ScanPattern(7, "55 8B EC 83 EC 18 A1 ???????? 33 C9 53"),
            };

            foreach (ScanPattern pattern in patterns32)
            {
                ptr = this.helper.Process.Scan(pattern, unityPlayer);
                if (ptr != IntPtr.Zero)
                    break;
            }

            if (ptr == IntPtr.Zero || !this.helper.Process.ReadPointer(ptr, out ptr))
                throw new ArgumentNullException("Address", "Could not find the scene manager");
        }

        if (!this.helper.Process.ReadPointer(ptr, out baseAddress))
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
            int numScenes;
            IntPtr addr;

            if (helper.Process.Is64Bit)
            {
                Span<long> buf = stackalloc long[3];
                helper.Process.ReadArray<long>(baseAddress + offsets.sceneCount, buf);
                numScenes = (int)buf[0];
                addr = (IntPtr)buf[2];
            }
            else
            {
                Span<int> buf = stackalloc int[3];
                helper.Process.ReadArray<int>(baseAddress + offsets.sceneCount, buf);
                numScenes = (int)buf[0];
                addr = (IntPtr)buf[2];
            }

            for (int i = 0; i < numScenes; i++)
            {
                if (helper.Process.ReadPointer(addr + i * helper.Process.PointerSize, out IntPtr ptr) && ptr != IntPtr.Zero)
                    yield return new Scene(this, ptr);
            }
        }
    }
}