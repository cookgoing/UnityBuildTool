using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using Newtonsoft.Json;
using Tmds.DBus.Protocol;
using UnityBuildTool.Configure;
using UnityBuildTool.Utils;

namespace UnityBuildTool.Views;

public partial class MainWindow : Window
{
    private BuildInfo customInfo;
    
    public MainWindow()
    {
        InitializeComponent();
        LogMessageHandler.Init(this);

        customInfo = ReadJsonCfg(GeneralCfg.CustomDataPath);
        LogListBox.Items.Clear();

        RefreshLogo();
        RefreshComboBox();
        RefreshTextBox();
        RefreshCheckBox();
    }

    protected override void OnClosed(EventArgs e) => WriteJsonCfg(customInfo, GeneralCfg.CustomDataPath);

    private BuildInfo ReadJsonCfg(string path)
    {
        using StreamReader sr = new (path);
        return JsonConvert.DeserializeObject<BuildInfo>(sr.ReadToEnd());
    }
    private void WriteJsonCfg(BuildInfo buildInfo, string path)
    {
        using StreamWriter sw = new (path);
        sw.Write(JsonConvert.SerializeObject(buildInfo));
    }

    private async Task<string?> SelectFile(string desc, FilePickerFileType  filter)
    {
        var fileDialog = new FilePickerOpenOptions
        {
            Title = desc,
            AllowMultiple = false,
            FileTypeFilter  = [filter]
        };
        
        var list = await StorageProvider.OpenFilePickerAsync(fileDialog);
        if (list.Count == 0) return null;
        
        return list[0].Path.LocalPath;
    }
    private async Task<string?> SelectFolder(string desc)
    {
        var folderDialog = new FolderPickerOpenOptions
        {
            Title = desc,
            AllowMultiple = false
        };

        var list = await StorageProvider.OpenFolderPickerAsync(folderDialog);
        if (list.Count == 0) return null;
        
        return list[0].Path.LocalPath;
    }

    private string NormlizePath(string path)
    {
        if (string.IsNullOrEmpty(path)) return path;

        return Path.GetFullPath(path);
    }
    public static string ParsePath(string path)
    {
        if (string.IsNullOrEmpty(path)) return path;
        
        return path.Replace("%BaseDir%", AppDomain.CurrentDomain.BaseDirectory);    
    }

    private async Task CloseProcess(string processName)
    {
        var processes = Process.GetProcessesByName(processName);
        foreach (var process in processes)
        {
            if (string.IsNullOrEmpty(process.MainWindowTitle)) continue;
                
            process.Kill(true);
            await process.WaitForExitAsync();
        }
    }

    private void RefreshLogo()
    {
        var iconPath = Path.Combine(AppContext.BaseDirectory, "images", "teamLogo.png");
        var bitmap = new Bitmap(iconPath);
        double maxSize = 250;
        double width, height;
        if (bitmap.Size.Width > bitmap.Size.Height)
        {
            width = maxSize;
            height = width / bitmap.Size.AspectRatio;
        }
        else
        {
            height = maxSize;
            width = height * bitmap.Size.AspectRatio;
        }

        TeamLogoImg.Source = bitmap;
        TeamLogoImg.Width = width;
        TeamLogoImg.Height = height;
    }

    private void RefreshComboBox()
    {
        BuildModeBox.Items.Clear();
        Channel.Items.Clear();
        ExecuteBox.Items.Clear();

        foreach (var kv in CheckBoxEnums.BuildModeDic) BuildModeBox.Items.Add(new ComboBoxItem { Content = kv.Value });
        foreach (var channelName in customInfo.ChannelNames) Channel.Items.Add(new ComboBoxItem { Content = channelName });
        foreach (var kv in CheckBoxEnums.ExecuteTypeDic) ExecuteBox.Items.Add(new ComboBoxItem { Content = kv.Value });

        int channelIdx = Array.IndexOf(customInfo.ChannelNames, customInfo.Channel);
        
        BuildModeBox.SelectedIndex = (int)(customInfo.IsDev ? CheckBoxEnums.BuildModeEn.Dev : CheckBoxEnums.BuildModeEn.Release);
        Channel.SelectedIndex = channelIdx;
        ExecuteBox.SelectedIndex = customInfo.ExecuteTypeIdx;
    }
    private void RefreshTextBox()
    {
        TeamIdentityText.Text = customInfo.TeamIdentity;
        ProductIdentityText.Text = customInfo.ProductionIdentity;
        TeamNameText.Text = customInfo.TeamName;
        ProductNameText.Text = customInfo.ProductionName;
        LanuchLogoPathText.Text = NormlizePath(ParsePath(customInfo.LaunchLogoPath));
        MajorVerText.Text = customInfo.MajorVer.ToString();
        MinorVerText.Text = customInfo.MinorVer.ToString();
        PatchVerText.Text = customInfo.PatchVer.ToString();
        BuildCodeText.Text = customInfo.BuildNumber.ToString();
        ScriptExportPathText.Text = NormlizePath(ParsePath(customInfo.ScriptExportPath));
        LanuchLogoExportPathText.Text = NormlizePath(ParsePath(customInfo.LaunchLogoExportPath));
        GitProjectPathText.Text = NormlizePath(ParsePath(customInfo.GitProjectPath));
        UnityEditorPathText.Text = NormlizePath(ParsePath(customInfo.UnityEditorPath));
        UnityProjectPathText.Text = NormlizePath(ParsePath(customInfo.UnityProjectPath));
        PackageExportPathText.Text = NormlizePath(ParsePath(customInfo.PackageBuildExportPath));
    }
    private void RefreshCheckBox()
    {
        GitUpdateCheck.IsChecked = customInfo.GitUpdate;
        AndroidBuildCheck.IsChecked = customInfo.BuildAndroid;
        WindowBuildCheck.IsChecked = customInfo.BuildWindows;
    }

    public int AddLogItem(string logStr, CheckBoxEnums.LogType logType)
    {
        return LogListBox.Items.Add(new ListBoxItem
        {
            Content = logStr,
            Foreground = logType == CheckBoxEnums.LogType.Error ? Brushes.Red : logType == CheckBoxEnums.LogType.Warn ? Brushes.Orange : Brushes.Black,
        });
    }
    public void MoveLogListScroll(int idx = -1)
    {
        if (idx == -1 && LogListBox.Items.Count > 0)
        {
            MoveLogListScroll(LogListBox.Items.Count - 1);
            return;
        }

        if (idx < 0 || idx >= LogListBox.Items.Count) return;

        Dispatcher.UIThread.Post(() => LogListBox.ScrollIntoView(idx));
    }
    
    private void OnMajorVerTextChanged(object? sender, TextChangedEventArgs e)
    {
        if (string.IsNullOrEmpty(MajorVerText.Text))
        {
            customInfo.MajorVer = 0;
            MajorVerText.Text = "0";
            return;
        }

        if (!uint.TryParse((string?)MajorVerText.Text, out uint majorVer))
        {
            LogMessageHandler.AddError("版本号解析失败，只能是整形字符串");
            MajorVerText.Text = customInfo.MajorVer.ToString();
            return;
        }

        customInfo.MajorVer = majorVer;
    }
    private void OnMinorVerTextChanged(object? sender, TextChangedEventArgs e)
    {
        if (string.IsNullOrEmpty(MinorVerText.Text))
        {
            customInfo.MinorVer = 0;
            MinorVerText.Text = "0";
            return;
        }
        
        if (!uint.TryParse((string?)MinorVerText.Text, out uint minorVer))
        {
            LogMessageHandler.AddError("版本号解析失败，只能是整形字符串");
            MinorVerText.Text = customInfo.MinorVer.ToString();
            return;
        }

        customInfo.MinorVer = minorVer;
    }
    private void OnPatchVerTextChanged(object? sender, TextChangedEventArgs e)
    {
        if (string.IsNullOrEmpty(PatchVerText.Text))
        {
            customInfo.PatchVer = 0;
            PatchVerText.Text = "0";
            return;
        }
        
        if (!uint.TryParse((string?)PatchVerText.Text, out uint patchVer))
        {
            LogMessageHandler.AddError("版本号解析失败，只能是整形字符串");
            PatchVerText.Text = customInfo.PatchVer.ToString();
            return;
        }

        customInfo.PatchVer = patchVer;
    }
    private void OnBuildNumberTextChanged(object? sender, TextChangedEventArgs e)
    {
        if (string.IsNullOrEmpty(BuildCodeText.Text))
        {
            customInfo.BuildNumber = 0;
            BuildCodeText.Text = "0";
            return;
        }
        
        if (!uint.TryParse((string?)BuildCodeText.Text, out uint buldNumber) || buldNumber == 0)
        {
            LogMessageHandler.AddError("构建号解析失败，只能是 >0 整形字符串");
            BuildCodeText.Text = customInfo.BuildNumber.ToString();
            return;
        }

        customInfo.BuildNumber = buldNumber;
    }
    
    private void OnBuildModeChanged(object? sender, SelectionChangedEventArgs e) => customInfo.IsDev = (CheckBoxEnums.BuildModeEn)BuildModeBox.SelectedIndex == CheckBoxEnums.BuildModeEn.Dev;
    private void OnChannelChanged(object? sender, SelectionChangedEventArgs e) => customInfo.Channel = customInfo.ChannelNames[Channel.SelectedIndex];
    private void OnExecuteTypeChanged(object? sender, SelectionChangedEventArgs e) => customInfo.ExecuteTypeIdx = ExecuteBox.SelectedIndex;
    
    private void OnGitUpdateToggleChanged(object? sender, RoutedEventArgs e) => customInfo.GitUpdate = GitUpdateCheck.IsChecked??false;
    private void OnBuildAndroidToggleChanged(object? sender, RoutedEventArgs e) => customInfo.BuildAndroid = AndroidBuildCheck.IsChecked??false;
    private void OnBuildWindowsToggleChanged(object? sender, RoutedEventArgs e) => customInfo.BuildWindows = WindowBuildCheck.IsChecked ?? false;

    private async void OnAppLaunchLogoSelectBtn(object? sender, RoutedEventArgs e) => LanuchLogoPathText.Text = customInfo.LaunchLogoPath = NormlizePath(await SelectFile("选择启动Logo文件", new FilePickerFileType("视频，图片"){ Patterns = ["*.png", "*.jpg", "*.mp4"] })??customInfo.LaunchLogoPath);
    private async void OnUnityBuilderExportBtn(object? sender, RoutedEventArgs e) => ScriptExportPathText.Text = customInfo.ScriptExportPath = NormlizePath(await SelectFolder("构建脚本导出到Unity的文件夹")??customInfo.ScriptExportPath);
    private async void OnAppLaunchLogoExportBtn(object? sender, RoutedEventArgs e) => LanuchLogoExportPathText.Text = customInfo.LaunchLogoExportPath = NormlizePath(await SelectFolder("启动Logo导出到Unity的文件夹")??customInfo.LaunchLogoExportPath);
    private async void OnUnityEditorBtn(object? sender, RoutedEventArgs e) => UnityEditorPathText.Text = customInfo.UnityEditorPath = NormlizePath(await SelectFile("Unity Editor文件", new FilePickerFileType("unityEditor") { Patterns = ["*.exe"] })??customInfo.UnityEditorPath);
    private async void OnUnityProjectBtn(object? sender, RoutedEventArgs e) => UnityProjectPathText.Text = customInfo.UnityProjectPath = NormlizePath(await SelectFolder("Unity工程文件夹")??customInfo.UnityProjectPath);
    private async void OnPackageExportBtn(object? sender, RoutedEventArgs e) => PackageExportPathText.Text = customInfo.PackageBuildExportPath = NormlizePath(await SelectFolder("包导出文件夹")??customInfo.PackageBuildExportPath);
    private async void OnGitProjectBtn(object? sender, RoutedEventArgs e) => GitProjectPathText.Text = customInfo.GitProjectPath = NormlizePath(await SelectFolder("git文件夹")??customInfo.GitProjectPath);
    private async void OnExecuteBtn(object? sender, RoutedEventArgs e)
    {
        CheckBoxEnums.ExecuteTypeEn executeType = (CheckBoxEnums.ExecuteTypeEn) customInfo.ExecuteTypeIdx;
        switch (executeType)
        {
            case CheckBoxEnums.ExecuteTypeEn.Build:
                await BuildPackage();
                break;
            case CheckBoxEnums.ExecuteTypeEn.RevertDefault:
                RevertDefault();
                break;
        }

        MoveLogListScroll();
    }

    private void RevertDefault()
    {
        customInfo = ReadJsonCfg(GeneralCfg.DefaultDataPath);
        
        RefreshComboBox();
        RefreshTextBox();
        RefreshCheckBox();
    }

    private async Task<bool> BuildPackage()
    {
        try
        {
            bool result = true;
            customInfo.TeamIdentity = TeamIdentityText.Text;
            customInfo.ProductionIdentity = ProductIdentityText.Text;
            customInfo.TeamName = TeamNameText.Text;
            customInfo.ProductionName = ProductNameText.Text;

            WriteJsonCfg(customInfo, GeneralCfg.CustomDataPath);

            result = await GitUpdate();
            result &= FilesCopy();
            result &= await UnityBuild();
            return result;
        }
        catch (Exception ex)
        {
            // LogMessageHandler.AddError($"[BuildPackage]. errorMsg: {ex.Message}");
            LogMessageHandler.LogException(ex);
            return false;
        }
    }
    private async Task<bool> GitUpdate()
    {
        string gitProjectPath = NormlizePath(ParsePath(customInfo.GitProjectPath));
        if (string.IsNullOrEmpty(gitProjectPath) || !customInfo.GitUpdate) return true;

        LogMessageHandler.AddInfo("[git 更新]");
        if (!Directory.Exists(gitProjectPath) || !File.Exists(Path.Combine(gitProjectPath, ".git", "config")))
        {
            LogMessageHandler.AddError($"路径不是有效的 Git 仓库: {gitProjectPath}");
            return false;
        }

        var psi = new ProcessStartInfo
        {
            FileName = "git",
            Arguments = "pull",
            WorkingDirectory = gitProjectPath,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = Process.Start(psi);

        string output = await process.StandardOutput.ReadToEndAsync();
        string error = await process.StandardError.ReadToEndAsync();
        await process.WaitForExitAsync();

        if (process.ExitCode == 0) LogMessageHandler.AddInfo($"Git pull 成功：{output}");
        else LogMessageHandler.AddError($"Git pull 失败：{error}");
        
        LogMessageHandler.AddInfo("[git 更新完成]");
        return true;
    }
    private bool FilesCopy()
    {
        string launchLogoPath = NormlizePath(ParsePath(customInfo.LaunchLogoPath));
        string launchLogoExportPath = NormlizePath(ParsePath(customInfo.LaunchLogoExportPath));
        if (!string.IsNullOrEmpty(launchLogoPath) && !string.IsNullOrEmpty(launchLogoExportPath))
        {
            LogMessageHandler.AddInfo("[copy 启动Logo]");
            if (!Directory.Exists(launchLogoExportPath)) Directory.CreateDirectory(launchLogoExportPath);

            string fileName = Path.GetFileName(launchLogoPath);
            string exportPath = Path.Combine(launchLogoExportPath, fileName);
            
            File.Copy(launchLogoPath, exportPath, true);
        }

        string scriptExportPath = NormlizePath(ParsePath(customInfo.ScriptExportPath));
        if (string.IsNullOrEmpty(scriptExportPath))
        {
            LogMessageHandler.AddError("构建脚本 没有导出路径，无法导出");
            return false;
        }

        LogMessageHandler.AddInfo("[copy 构建脚本]");
        if (!Directory.Exists(scriptExportPath)) Directory.CreateDirectory(scriptExportPath);

        string buildInfoFileName = Path.GetFileName(GeneralCfg.BuildInfoPath);
        string buildScriptFileName = Path.GetFileName(GeneralCfg.BuildScriptPath);
        string buildInfoExportPath = Path.Combine(scriptExportPath, buildInfoFileName);
        string buildScriptExportPath = Path.Combine(scriptExportPath, buildScriptFileName);
        
        File.Copy(GeneralCfg.BuildInfoPath, buildInfoExportPath, true);
        File.Copy(GeneralCfg.BuildScriptPath, buildScriptExportPath, true);

        LogMessageHandler.AddInfo("[文件copy完成]");
        return true;
    }
    private async Task<bool> UnityBuild()
    {
        string unityEditorPath = NormlizePath(ParsePath(customInfo.UnityEditorPath));
        if (string.IsNullOrEmpty(unityEditorPath))
        {
            LogMessageHandler.AddError("没有Unity Editor执行文件，无法启动");
            return false;
        }

        string unityProjectPath = NormlizePath(ParsePath(customInfo.UnityProjectPath));
        if (string.IsNullOrEmpty(unityProjectPath) || !Directory.Exists(unityProjectPath))
        {
            LogMessageHandler.AddError("没有Unity 工程路径，无法启动");
            return false;
        }
        
        LogMessageHandler.AddInfo("[尝试关闭所有Unity的编辑器]");
        await CloseProcess("Unity");
        LogMessageHandler.AddInfo("[已关闭所有Unity的编辑器]");
        
        LogMessageHandler.AddInfo("[开始构建]");
        string unityPath = unityEditorPath;
        string projectPath = unityProjectPath.TrimEnd('\\');
        string jsonCfg = JsonConvert.SerializeObject(customInfo);
        string jsonBase64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(jsonCfg));
        string logPath = "Log/Build.log";
        string arguments = @$"-batchmode -quit -projectPath ""{projectPath}"" -executeMethod Business.Editor.BuildScript.Run -buildConfig ""{jsonBase64}"" -logFile ""{logPath}""";

        var startInfo = new ProcessStartInfo
        {
            FileName = unityPath,
            Arguments = arguments,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };

        startInfo.EnvironmentVariables.Remove("DOTNET_STARTUP_HOOKS");
        using Process process = Process.Start(startInfo);
        
        string output = process.StandardOutput.ReadToEnd();
        string error = process.StandardError.ReadToEnd();
        await process.WaitForExitAsync();

        using var fs = new FileStream(logPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        using var reader = new StreamReader(fs);
        string logContent = await reader.ReadToEndAsync();
        LogMessageHandler.AddInfo($"[构建结束].\nlog: {logContent}");
        
        if (process.ExitCode == 0) LogMessageHandler.AddInfo($"构建成功：{output}");
        else LogMessageHandler.AddError($"构建失败：exitCode: {process.ExitCode}; errorContent: {error}\noutput: {output}");

        return true;
    }
}