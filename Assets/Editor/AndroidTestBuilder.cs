using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace GEL.EditorTools
{
    public static class AndroidTestBuilder
    {
        private const string ApkPath = "Builds/Android/GEL-prototype.apk";

        [MenuItem("GEL/Build Android Test APK")]
        public static void BuildAndroidTest()
        {
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.LandscapeLeft;
            PlayerSettings.SetApplicationIdentifier(
                BuildTargetGroup.Android, "com.Portfolio.GELPrototype");
            PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel22;
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
            PlayerSettings.SetScriptingBackend(
                BuildTargetGroup.Android, ScriptingImplementation.IL2CPP);

            string directory = Path.GetDirectoryName(ApkPath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                Directory.CreateDirectory(directory);

            BuildPlayerOptions options = new BuildPlayerOptions
            {
                scenes = new[] { "Assets/Scenes/P0_Prototype.unity" },
                locationPathName = ApkPath,
                target = BuildTarget.Android,
                options = BuildOptions.Development
            };

            BuildReport report = BuildPipeline.BuildPlayer(options);
            BuildSummary summary = report.summary;

            if (summary.result == BuildResult.Succeeded)
                Debug.Log("ANDROID_BUILD_OK " + ApkPath);
            else
                Debug.LogError("ANDROID_BUILD_FAILED " + summary.result);
        }
    }
}
