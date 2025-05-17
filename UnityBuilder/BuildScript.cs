using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
using Newtonsoft.Json;
using DingFrame;
using DingFrame.Utils;
using Business.Data;

namespace Business.Editor
{
	public class BuildScript
	{
		public static void Run()
		{
			var jsonBase64 = Environment.GetCommandLineArgs().SkipWhile(arg => arg != "-buildConfig").Skip(1).FirstOrDefault();
			if (string.IsNullOrEmpty(jsonBase64)) throw new Exception("Missing build config.");

			string jsonCfg = Encoding.UTF8.GetString(Convert.FromBase64String(jsonBase64));
			BuildInfo buildInfo = JsonConvert.DeserializeObject<BuildInfo>(jsonCfg);

			string gameInfoFilePath = Path.GetFullPath(GameConfigure.GameInfoPath);
			GameInfo gameInfo = JsonConvert.DeserializeObject<GameInfo>(File.ReadAllText(gameInfoFilePath));
			gameInfo.MajorVer = buildInfo.MajorVer;
			gameInfo.MinorVer = buildInfo.MinorVer;
			gameInfo.PatchVer = buildInfo.PatchVer;
			gameInfo.BuildNumber = buildInfo.BuildNumber;
			File.WriteAllText(gameInfoFilePath, JsonConvert.SerializeObject(gameInfo));

			PlayerSettings.applicationIdentifier = $"com.{buildInfo.TeamIdentity}.{buildInfo.ProductionIdentity}";
			PlayerSettings.companyName = buildInfo.TeamName;
			PlayerSettings.productName = buildInfo.ProductionName;

			BuildPlayerOptions buildPlayerOptions = new ();
			buildPlayerOptions.scenes = new[] { "Assets/Scenes/Launch.unity" };
			buildPlayerOptions.options = buildInfo.IsDev ? BuildOptions.Development : BuildOptions.None;

			bool hasLogo = !string.IsNullOrEmpty(buildInfo.LaunchLogoExportPath);
			PlayerSettings.SplashScreenLogo[] logs = null;
			if (hasLogo)
			{
				string extension = Path.GetExtension(buildInfo.LaunchLogoExportPath);
				bool isTexture = extension == ".png" || extension == ".jpg";
				string baseDir = AppDomain.CurrentDomain.BaseDirectory;
				string unityPath = buildInfo.LaunchLogoExportPath.Replace(baseDir, string.Empty);
				logs = new[]
				{
					new PlayerSettings.SplashScreenLogo
					{
						logo = AssetDatabase.LoadAssetAtPath<Sprite>(unityPath),
						duration = 2.0f
					},
				};
			}
			PlayerSettings.SplashScreen.showUnityLogo = false;
			PlayerSettings.SplashScreen.logos = logs;

			GenerateLink.Execute();
			
			string buildMode = buildInfo.IsDev ? "dev" : "release";
			string versionStr = $"{buildInfo.MajorVer}.{buildInfo.MinorVer}.{buildInfo.PatchVer}";
			string language = "cn";
			string versionDir = Path.Combine(buildInfo.PackageBuildExportPath, versionStr, buildMode);
			if (!Directory.Exists(versionDir)) Directory.CreateDirectory(versionDir);
			else FileUtils.ClearDirectory(versionDir, true);

			BuildReport report = null;
			if (buildInfo.BuildAndroid)
			{
				PlayerSettings.bundleVersion = $"{buildInfo.MajorVer}.{buildInfo.MinorVer}.{buildInfo.PatchVer}";
				PlayerSettings.Android.bundleVersionCode = (int)buildInfo.BuildNumber;
				PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
				
				string apkName = $"{buildInfo.ProductionIdentity}_{buildMode}_{versionStr}_{language}.apk";
				string exportFilePath = Path.Combine(versionDir, apkName);
				buildPlayerOptions.target = BuildTarget.Android;
				buildPlayerOptions.locationPathName = exportFilePath;

				report = BuildPipeline.BuildPlayer(buildPlayerOptions);
			}

			if (report != null && report.summary.result != BuildResult.Succeeded) throw new Exception($"[Android Build Failed] Result: {report.summary.result}, Errors: {report.summary.totalErrors}");
			
			if (buildInfo.BuildWindows)
			{
				PlayerSettings.macOS.buildNumber = buildInfo.BuildNumber.ToString();
				PlayerSettings.SetScriptingBackend(NamedBuildTarget.Standalone, ScriptingImplementation.IL2CPP);

				string dirName = $"{buildInfo.ProductionIdentity}_{buildMode}_{versionStr}_{language}_win";
				string exeName = $"{buildInfo.ProductionIdentity}.exe";
				string exportDir = Path.Combine(versionDir, dirName);
				string exportFilePath = Path.Combine(exportDir, exeName);
				if (!Directory.Exists(exportDir)) Directory.CreateDirectory(exportDir);

				buildPlayerOptions.target = BuildTarget.StandaloneWindows64;
				buildPlayerOptions.locationPathName = exportFilePath;

				report = BuildPipeline.BuildPlayer(buildPlayerOptions);
			}

			if (report != null && report.summary.result != BuildResult.Succeeded) throw new Exception($"[Windows Build Failed] Result: {report.summary.result}, Errors: {report.summary.totalErrors}");

			DLogger.Info($"[BuildScript.Run] totalSize: {report?.summary.totalSize ?? 0} bytes");
		}
	}
}