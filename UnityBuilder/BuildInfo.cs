
public enum InitLanguageEn
{
    SimpleChinese,
}

public struct BuildInfo
{
    public string LaunchLogoPath;
    public string ScriptExportPath;
    public string GitProjectPath;
    public string UnityEditorPath;
    public string UnityProjectPath;
    public int ExecuteTypeIdx;
    public bool GitUpdate;
    
    public string TeamIdentity;
    public string ProductionIdentity;
    public string TeamName;
    public string ProductionName;
    public bool IsDev;
    public uint MajorVer;
    public uint MinorVer;
    public uint PatchVer;
    public uint BuildNumber;
    public int InitLanguageIdx;
    public bool BuildAndroid;
    public bool BuildWindows;
    public string LaunchLogoExportPath;
    public string PackageBuildExportPath;
}