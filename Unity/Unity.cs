using JHelper.Common.ProcessInterop;
using JHelper.HelperBase;

#if !LIVESPLIT
namespace JHelper;
#endif

/// <summary>
/// A helper implementation for working with Unity-based games.
/// This class inherits from <see cref="HelperBase"/> and uses its
/// process hooking and code generation capabilities.
/// </summary>
public partial class Unity : JHelper.HelperBase.HelperBase
{
#if LIVESPLIT
    /// <summary>
    /// Initializes a new instance of the <see cref="Unity"/> helper
    /// </summary>
    public Unity()
        : this(true) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="Unity"/> helper,
    /// optionally enabling code generation.
    /// </summary>
    /// <param name="generateCode">
    /// <c>true</c> to enable code generation; <c>false</c> to disable it.
    /// </param>
    public Unity(bool generateCode)
        : base(generateCode) { }
#else
    /// <summary>
    /// Initializes a new instance of the <see cref="Unity"/> helper
    /// </summary>
    public Unity(ProcessMemory process)
        : base()
    {
        this.Process = process;
    }

#endif

    public void SetUnityVersion(int major, int minor)
    {
        if (MonoType == MonoTypeEnum.IL2CPP)
            IL2CPP = new JHelper.UnityManagers.IL2CPP.IL2CPP(this, JHelper.UnityManagers.IL2CPP.IL2CPP.ForceVersion(major, minor));
    }

    public override void Dispose()
    {
        _monoType = MonoTypeEnum.Undefined;
        IL2CPP = null!;
        Mono = null!;
        base.Dispose();
    }
}
