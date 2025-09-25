public struct BuildInfo
{
    public string[] ChannelNames;
    public string LaunchLogoPath;
    public string ScriptExportPath;
    public string GitProjectPath;
    public string UnityEditorPath;
    public string UnityProjectPath;
    public int ExecuteTypeIdx;
    public bool GitUpdate;
    
    public string TeamIdentity;
    public string ProductionIdentity;
    public string ApplicationName;
    public bool IsDev;
    public uint MajorVer;
    public uint MinorVer;
    public uint PatchVer;
    public uint BuildNumber;
    public string Channel;
    public bool BuildAndroid;
    public bool BuildWindows;
    public string LaunchLogoExportPath;
    public string PackageBuildExportPath;
}