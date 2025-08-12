// Inspired by asl-help, by just-ero.
// asl-help is licensed under GPL-3.0.
// See: https://github.com/just-ero/asl-help

#if LIVESPLIT
using LiveSplit.Model;
using LiveSplit.UI;
using LiveSplit.View;
using System;
using System.Collections;
using System.Linq;
using System.Reflection;

namespace JHelper.LiveSplit;

/// <summary>
/// Provides access to the current state of a LiveSplit timer and its associated properties.
/// This static class acts as an interface to the LiveSplit timer, layout, and timing methods.
/// </summary>
internal static class Timer
{
    public static LiveSplitState State { get; }

    public static IRun Run { get => State.Run; set => State.Run = value; }

    public static ILayout Layout { get => State.Layout; set => State.Layout = value; }

    public static TimingMethod CurrentTimingMethod { get => State.CurrentTimingMethod; set => State.CurrentTimingMethod = value; }

    static Timer()
    {
        // Attempt to use the Windows Forms assembly using reflection.
        // This is required to bypass .NET Standard 2.0 limitations when interacting with Windows Forms
        // as System.Windows.Forms cannot be referenced directly in .NET Standard 2.0.
        State = (AppDomain.CurrentDomain
            .GetAssemblies()
            .First(a => a.FullName.StartsWith("System.Windows.Forms"))
            .GetType("System.Windows.Forms.Application")
            .GetProperty("OpenForms", BindingFlags.Public | BindingFlags.Static)
            .GetValue(null) as IEnumerable)
            .OfType<TimerForm>()
            .First()
            .CurrentState;
    }
}
#endif