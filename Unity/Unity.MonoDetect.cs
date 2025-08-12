using System.IO;

#if !LIVESPLIT
namespace JHelper;
#endif

public partial class Unity
{
    /// <summary>
    /// Cached result of the detected Unity runtime type (Mono or IL2CPP).
    /// Defaults to <see cref="MonoTypeEnum.Undefined"/> until detection is performed.
    /// </summary>
    private MonoTypeEnum _monoType = MonoTypeEnum.Undefined;

    /// <summary>
    /// Gets the detected runtime type of the hooked Unity game.
    /// Detection is performed on first access by checking for the presence of
    /// <c>GameAssembly.dll</c> in the same directory as the game's executable.
    /// </summary>
    /// </summary>
    public MonoTypeEnum MonoType
    {
        get
        {
            if (_monoType == MonoTypeEnum.Undefined)
            {
                string processPath = Process.MainModule.FileName;
                string gameassemblyPath = processPath[0..(processPath.LastIndexOf('\\') + 1)] + "GameAssembly.dll";

                _monoType = File.Exists(gameassemblyPath)
                    ? MonoTypeEnum.IL2CPP
                    : MonoTypeEnum.Mono;
            }

            return _monoType;
        }
    }

    /// <summary>
    /// Represents the type of Unity runtime used by the hooked game.
    /// </summary>
    public enum MonoTypeEnum
    {
        /// <summary>
        /// The game uses the IL2CPP backend.
        /// </summary>
        IL2CPP,

        /// <summary>
        /// The game uses the Mono backend.
        /// </summary>
        Mono,

        /// <summary>
        /// The runtime type has not yet been determined.
        /// </summary>
        Undefined,
    }
}
