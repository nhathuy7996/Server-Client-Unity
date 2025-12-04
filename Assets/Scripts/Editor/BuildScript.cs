using System;
using System.IO;
using UnityEditor;
using UnityEngine;

public class BuildScript
{
    private static string GetBuildPath()
    {
        return Path.Combine(Application.dataPath, "..", "Builds");
    }

    [MenuItem("Build/Build Android")]
    public static void BuildAndroid()
    {
        string buildPath = GetBuildPath();
        string outputPath = Path.Combine(buildPath, "Android");

        if (!Directory.Exists(outputPath))
        {
            Directory.CreateDirectory(outputPath);
        }

        string appName = PlayerSettings.productName;
        string fileName = Path.Combine(outputPath, $"{appName}.apk");

        // Lấy danh sách scenes được enable trong Build Settings
        string[] scenes = GetEnabledScenes();

        BuildPlayerOptions buildPlayerOptions = new BuildPlayerOptions
        {
            scenes = scenes,
            locationPathName = fileName,
            target = BuildTarget.Android,
            options = BuildOptions.None
        };

        Debug.Log($"Building Android APK to: {fileName}");

        BuildReport report = BuildPipeline.BuildPlayer(buildPlayerOptions);
        BuildSummary summary = report.summary;

        if (summary.result == BuildResult.Succeeded)
        {
            Debug.Log($"Build succeeded: {summary.totalSize} bytes");
            Debug.Log($"Output: {fileName}");
        }
        else if (summary.result == BuildResult.Failed)
        {
            Debug.LogError("Build failed");
            EditorApplication.Exit(1);
        }
    }

    [MenuItem("Build/Build WebGL")]
    public static void BuildWebGL()
    {
        string buildPath = GetBuildPath();
        string outputPath = Path.Combine(buildPath, "WebGL");

        if (!Directory.Exists(outputPath))
        {
            Directory.CreateDirectory(outputPath);
        }

        string[] scenes = GetEnabledScenes();

        BuildPlayerOptions buildPlayerOptions = new BuildPlayerOptions
        {
            scenes = scenes,
            locationPathName = outputPath,
            target = BuildTarget.WebGL,
            options = BuildOptions.None
        };

        Debug.Log($"Building WebGL to: {outputPath}");

        BuildReport report = BuildPipeline.BuildPlayer(buildPlayerOptions);
        BuildSummary summary = report.summary;

        if (summary.result == BuildResult.Succeeded)
        {
            Debug.Log($"Build succeeded: {summary.totalSize} bytes");
            Debug.Log($"Output: {outputPath}");
        }
        else if (summary.result == BuildResult.Failed)
        {
            Debug.LogError("Build failed");
            EditorApplication.Exit(1);
        }
    }

    [MenuItem("Build/Build Windows")]
    public static void BuildWindows()
    {
        string buildPath = GetBuildPath();
        string outputPath = Path.Combine(buildPath, "Windows");

        if (!Directory.Exists(outputPath))
        {
            Directory.CreateDirectory(outputPath);
        }

        string appName = PlayerSettings.productName;
        string fileName = Path.Combine(outputPath, $"{appName}.exe");

        string[] scenes = GetEnabledScenes();

        BuildPlayerOptions buildPlayerOptions = new BuildPlayerOptions
        {
            scenes = scenes,
            locationPathName = fileName,
            target = BuildTarget.StandaloneWindows64,
            options = BuildOptions.None
        };

        Debug.Log($"Building Windows to: {fileName}");

        BuildReport report = BuildPipeline.BuildPlayer(buildPlayerOptions);
        BuildSummary summary = report.summary;

        if (summary.result == BuildResult.Succeeded)
        {
            Debug.Log($"Build succeeded: {summary.totalSize} bytes");
            Debug.Log($"Output: {fileName}");
        }
        else if (summary.result == BuildResult.Failed)
        {
            Debug.LogError("Build failed");
            EditorApplication.Exit(1);
        }
    }

    private static string[] GetEnabledScenes()
    {
        var scenes = new string[EditorBuildSettings.scenes.Length];
        for (int i = 0; i < scenes.Length; i++)
        {
            scenes[i] = EditorBuildSettings.scenes[i].path;
        }
        return scenes;
    }
}
