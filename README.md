# unity-help

unity-help is a C# library intended to provide easy access to memory addresses in games built using the Unity engine.
Its primary intended use case is in conjunction with a .asl script for LiveSplit autosplitters on Windows systems.

## Examples

```cs
state("LiveSplit") {}

startup
{
    Assembly.Load(File.ReadAllBytes("Components/unity-help")).CreateInstance("Unity");
}
```

## Defining pointer paths

```cs
init
{
    ...
    vars.IGT = vars.Helper.Make<float>("StageManager", 0, "_instance", "Timer");
    vars.Map = vars.Helper.MakeString("StageManager", 0, "_instance", "CurrentMap");
}
```

```cs
update
{
    print(vars.IGT.Current.ToString());
    
    current.Map = vars.Helper.SceneManager.Current.Name;
}
```

