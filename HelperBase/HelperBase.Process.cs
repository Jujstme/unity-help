using JHelper.Common.ProcessInterop;
using System;
#if LIVESPLIT
using JHelper.LiveSplit;
#endif

namespace JHelper.HelperBase;

public abstract partial class HelperBase
{
    /// <summary>
    /// Backing field for the memory representation of the currently hooked process.
    /// </summary>
    private ProcessMemory? _process;

    public ProcessMemory Process
    {
        get
        {
            if (_process is null)
            {
#if LIVESPLIT
                ProcessMemory? process = ProcessMemory.HookProcess(Autosplitter.Game);

                if (process is null)
                    throw new NullReferenceException("Could not find the target process.");
                
                _process = process;
#else
                throw new NullReferenceException("Could not find the target process.");
#endif
            }

            return _process;
        }

#if !LIVESPLIT
        internal set
        {
            _process = value;
        }
#endif

    }
}