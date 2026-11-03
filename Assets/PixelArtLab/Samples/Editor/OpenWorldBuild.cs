using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;

namespace PixelArtLab.Editor
{
    public static class OpenWorldBuild
    {
        [MenuItem("Pixel Art Lab/Build Windows Free Roam Player")]
        public static void WindowsPlayer()
        {
            Directory.CreateDirectory("Builds/Amberwood");
            var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes=new[]{OpenWorldSceneBuilder.ScenePath},locationPathName="Builds/Amberwood/Amberwood.exe",
                target=BuildTarget.StandaloneWindows64,options=BuildOptions.None
            });
            if(report.summary.result!=BuildResult.Succeeded)throw new Exception("Windows build failed: "+report.summary.result);
            File.WriteAllText("Previews/Windows-Build-Verification.txt",$"WINDOWS_BUILD_SUCCESS\nSize: {report.summary.totalSize} bytes\nErrors: {report.summary.totalErrors}\nWarnings: {report.summary.totalWarnings}\n");
        }
    }
}
