#if LIVESPLIT
using System;
using System.IO;
using System.Reflection;

namespace JHelper.LiveSplit;

public static class LiveSplitAssembly
{
    public static void AssemblyResolveSubscribe()
    {
        AppDomain.CurrentDomain.AssemblyResolve += AssemblyResolve;
    }

    public static void AssemblyResolveUnsubscribe()
    {
        AppDomain.CurrentDomain.AssemblyResolve -= AssemblyResolve;
    }

    /// <summary>
    /// Resolves the assembly specified in the <see cref="ResolveEventArgs"/>.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The event arguments containing the assembly name.</param>
    /// <returns>The loaded assembly, or null if loading fails.</returns>
    private static Assembly AssemblyResolve(object? sender, ResolveEventArgs e)
    {
        int index = e.Name.IndexOf(',');
        if (index == -1)
            throw new InvalidOperationException();
        return Assembly.LoadFrom(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Components", $"{e.Name[..index]}.dll"));
    }
}
#endif