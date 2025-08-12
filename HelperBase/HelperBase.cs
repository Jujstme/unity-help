using System;
using JHelper.Logging;
using System.Diagnostics;
using JHelper.Common.Collections;

#if LIVESPLIT
using JHelper.LiveSplit;
#endif

namespace JHelper.HelperBase;

/// <summary>
/// Provides a base implementation for helpers that interact with games using the Unity engine.
/// This abstract class handles core logic such as code generation (when supported)
/// and assembly resolution.
/// </summary>
public abstract partial class HelperBase : IDisposable
{
    internal MemStateTracker _tickCounter = new MemStateTracker();

#if LIVESPLIT
    private readonly bool isASLCodeGenerating;

    /// <summary>
    /// Initializes a new instance of the <see cref="HelperBase"/> class
    /// with code generation enabled by default.
    /// </summary>
    public HelperBase()
        : this(true) { }
#endif

#if LIVESPLIT
/// <summary>
    /// Initializes a new instance of the <see cref="HelperBase"/> class, with
    /// optional code generation. You can disable code generation when using the helper
    /// in advanced <c>.asl</c> scripts that implement their own control logic.
    /// </summary>
    /// <param name="generateCode">
    /// <c>true</c> to enable code generation; <c>false</c> to disable it.
    /// </param>
    public HelperBase(bool generateCode)
#else
    /// <summary>
    /// Initializes a new instance of the <see cref="HelperBase"/> class
    /// </summary>
    public HelperBase()
#endif
    {
#if LIVESPLIT
        // Validate the process context — this helper may only run inside LiveSplit.
        using (Process thisProcess = System.Diagnostics.Process.GetCurrentProcess())
        {
            if (!thisProcess.ProcessName.Equals("livesplit", StringComparison.InvariantCultureIgnoreCase))
                throw new InvalidProgramException("This helper can be initialized only from LiveSplit.");
        }

        // Verify that the helper is being instantiated in the startup action.
        if (Actions.CurrentAction != ASLMethodNames.Startup)
            throw new InvalidOperationException("The helper may only be instantiated in the 'startup {}' action.");

        // Display welcome messages in the log.
        Log.Welcome();

        // Subscribe to assembly resolution to load dependencies dynamically.
        LiveSplitAssembly.AssemblyResolveSubscribe();
        try
        {
            // Manually invoke the static constructor of the Autosplitter class.
            // This ensures script and LiveSplit data are accessible even without code generation.
            typeof(Autosplitter).TypeInitializer.Invoke(null, null);
        }
        finally
        {
            // Always unsubscribe from the assembly resolution event to avoid leaks.
            LiveSplitAssembly.AssemblyResolveUnsubscribe();
        }

        isASLCodeGenerating = generateCode;
        if (isASLCodeGenerating)
        {
            Log.Info("Loading unity-help...");
            Log.Info("  => Generating code...");

            string helperName = Globals.HelperVarName;
            Autosplitter.Vars[helperName] = this;
            
            Log.Info($"    => Assigned helper to vars.{helperName}.");

            // Insert update and cleanup calls into the autosplitter's action hooks.
            Autosplitter.Actions.Update.Prepend($"vars.{helperName}.Update();");
            Autosplitter.Actions.Shutdown.Append($"vars.{helperName}.Dispose();");
            Autosplitter.Actions.Exit.Append($"vars.{helperName}.Exit();");
        }
        else
        {
            // Fallback logging when code generation is disabled.
#endif
            Log.Info("Loading helper...");
#if LIVESPLIT
        }
#endif
    }

    /// <summary>
    /// Performs per-tick update logic, such as maintaining hooks into
    /// the target process and updating tracked memory addresses.
    /// </summary>
    public virtual void Update()
    {
        _tickCounter.Tick();
    }

    /// <summary>
    /// Called when the autosplitter exits its hooked process.
    /// Performs cleanup logic.
    /// </summary>
    public virtual void Exit()
    {
        Dispose();
    }

    /// <summary>
    /// Releases resources used by the helper, including any
    /// associated handles or other unmanaged resources.
    /// </summary>
    public virtual void Dispose()
    {
        _process?.Dispose();
        _process = null;
    }
}