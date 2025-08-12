// Inspired by asl-help, by just-ero.
// asl-help is licensed under GPL-3.0.
// See: https://github.com/just-ero/asl-help

#if LIVESPLIT
using JHelper.Logging;
using LiveSplit.ASL;
using System;
using System.Diagnostics;
using System.Linq;
using System.Reflection;

namespace JHelper.LiveSplit;

/// <summary>
/// Represents a collection of actions that can be performed in an autosplitter.
/// Each action corresponds to a method defined in the ASL (Auto Split Language) script.
/// </summary>
internal class Actions
{
    public Action Startup { get; set; } = new(ASLMethodNames.Startup);
    public Action Shutdown { get; set; } = new(ASLMethodNames.Shutdown);
    public Action Init { get; set; } = new(ASLMethodNames.Init);
    public Action Exit { get; set; } = new(ASLMethodNames.Exit);
    public Action Update { get; set; } = new(ASLMethodNames.Update);
    public Action Start { get; set; } = new(ASLMethodNames.Start);
    public Action Split { get; set; } = new(ASLMethodNames.Split);
    public Action Reset { get; set; } = new(ASLMethodNames.Reset);
    public Action GameTime { get; set; } = new(ASLMethodNames.GameTime);
    public Action IsLoading { get; set; } = new(ASLMethodNames.IsLoading);
    public Action OnStart { get; set; } = new(ASLMethodNames.OnStart);
    public Action OnSplit { get; set; } = new(ASLMethodNames.OnSplit);
    public Action OnReset { get; set; } = new(ASLMethodNames.OnReset);

    /// <summary>
    /// Gets the current action being executed by inspecting the stack trace.
    /// </summary>
    public static string CurrentAction
    {
        get
        {
            StackFrame[] frames = new StackTrace().GetFrames();
            frames.Reverse();

            string? currentAction = frames
                    .Select(frame =>
                    {
                        MethodBase method = frame.GetMethod();
                        Type decl = method.DeclaringType;
                        string trace = decl is null ? method.Name : $"{decl.Name}.{method.Name}";

                        if (trace.StartsWith("ASLScript.Do"))
                            return trace[12..].ToLower();
                        else if (trace.StartsWith("ASLScript.Run"))
                            return trace[13..].ToLower();
                        else
                            return null;
                    })
                    .FirstOrDefault(s => s is not null);

            return currentAction is null ? string.Empty : currentAction;
        }
    }

    /// <summary>
    /// Represents an action in the ASL script.
    /// </summary>
    public record Action
    {
        public string Body { get; private set; }
        public string Name { get; }
        public int Line { get; }

        public Action(string name)
            : this("", name, 0) { }

        public Action(string body, string name, int line)
        {
            Body = body;
            Name = name;
            Line = line;
        }

        public void Append(string content)
        {
            Body = $"{Body}{content}";
            Update();
            Log.Info($"    => Added the following code to the end of {Name}:");
            Log.Info($"       `{content}`");
        }

        public void Prepend(string content)
        {
            Body = $"{content}{Body}";
            Update();
            Log.Info($"    => Added the following code to the beginning of {Name}:");
            Log.Info($"       `{content}`");
        }

        private void Update()
        {
            ASLMethod method = new(Body, Name, Line);

            switch (Name)
            {
                case ASLMethodNames.Startup: Autosplitter.ASLMethods.startup = method; break;
                case ASLMethodNames.Shutdown: Autosplitter.ASLMethods.shutdown = method; break;
                case ASLMethodNames.Init: Autosplitter.ASLMethods.init = method; break;
                case ASLMethodNames.Exit: Autosplitter.ASLMethods.exit = method; break;
                case ASLMethodNames.Update: Autosplitter.ASLMethods.update = method; break;
                case ASLMethodNames.Start: Autosplitter.ASLMethods.start = method; break;
                case ASLMethodNames.Split: Autosplitter.ASLMethods.split = method; break;
                case ASLMethodNames.Reset: Autosplitter.ASLMethods.reset = method; break;
                case ASLMethodNames.GameTime: Autosplitter.ASLMethods.gameTime = method; break;
                case ASLMethodNames.IsLoading: Autosplitter.ASLMethods.isLoading = method; break;
                case ASLMethodNames.OnStart: Autosplitter.ASLMethods.onStart = method; break;
                case ASLMethodNames.OnSplit: Autosplitter.ASLMethods.onSplit = method; break;
                case ASLMethodNames.OnReset: Autosplitter.ASLMethods.onReset = method; break;
            }
        }
    }
}
#endif