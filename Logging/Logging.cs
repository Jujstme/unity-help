using System.Diagnostics;

namespace JHelper.Logging;

/// <summary>
/// The <see cref="Log"/> class provides static methods for logging messages.
/// This class is intended for internal use within the Helper.Common namespace.
/// </summary>
internal static class Log
{
    public const string HelperName = "Helper";

    /// <summary>
    /// Logs an informational message to the trace output.
    /// This method does not take any parameters and simply logs a header message.
    /// </summary>
    public static void Info() => Trace.WriteLine($"[{HelperName}]");

    /// <summary>
    /// Logs an informational message to the trace output, including additional output content.
    /// This overload allows passing an object, which will be converted to its string representation.
    /// </summary>
    /// <param name="output">The output to log, which can be any object.</param>
    public static void Info(object output) => Trace.WriteLine($"[{HelperName}] {output}");

    /// <summary>
    /// Logs a welcome message to the trace output.
    /// </summary>
    public static void Welcome()
    {
        Trace.WriteLine("""
    Helper loaded. By default, code generation is enabled.
   
    If you would like to opt out of code generation, please use the following code in 'startup {}' instead:

        var type = Assembly.Load(File.ReadAllBytes("Components/unity-help")).GetType("Unity");
        vars.Helper = Activator.CreateInstance(type, args: false);

    If you have any questions, please tag Jujstme in the #auto-splitters channel
    of the Speedrun Tool Development Discord server: https://discord.gg/cpYsxz7.
    """);
    }
}