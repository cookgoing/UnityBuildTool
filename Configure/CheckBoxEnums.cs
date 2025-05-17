using System.Collections.Generic;

namespace UnityBuildTool.Configure;

public static class CheckBoxEnums
{
    public enum LogType
    {
        Info,
        Warn,
        Error,
    }
    
    public enum BuildModeEn
    {
        Dev,
        Release
    }

    public enum ExecuteTypeEn
    {
        Build,
        RevertDefault,
    }
    
    public static readonly Dictionary<BuildModeEn, string> BuildModeDic = new ()
    {
        {BuildModeEn.Dev, "Dev"},
        {BuildModeEn.Release, "Release"},
    };
    
    public static readonly Dictionary<InitLanguageEn, string> LanguageDic = new()
    {
        {InitLanguageEn.SimpleChinese, "简体中文"},
    };

    public static readonly Dictionary<ExecuteTypeEn, string> ExecuteTypeDic = new()
    {
        {ExecuteTypeEn.Build, "打包"},
        {ExecuteTypeEn.RevertDefault, "恢复默认值"},
    };
}