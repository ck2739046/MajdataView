#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using System.Diagnostics;
using UnityEngine;

/// <summary>
/// Unity 构建后流程：
/// 1. 调用 tools/patch-exe-info.ps1 为 Windows EXE 注入版本属性
/// 2. 调用 tools/clean-burst-folder.ps1 清理 Burst 调试文件夹
/// </summary>
public class BuildPostprocessor : IPostprocessBuildWithReport
{
    public int callbackOrder => 999;

    private const string LogTag = "[BuildPostprocessor]";

    public void OnPostprocessBuild(BuildReport report)
    {
        if (report.summary.platform != BuildTarget.StandaloneWindows &&
            report.summary.platform != BuildTarget.StandaloneWindows64)
        {
            return;
        }

        string exePath = report.summary.outputPath;
        string toolsDir = System.IO.Path.Combine(Application.dataPath, "..", "tools");

        // 1) 注入 EXE 版本属性
        RunPs1Script(toolsDir, "patch-exe-info.ps1", $"-ExePath \"{exePath}\"");

        // 2) 清理 Burst 调试文件夹
        RunPs1Script(toolsDir, "clean-burst-folder.ps1", $"-ExePath \"{exePath}\"");
    }

    private static void RunPs1Script(string toolsDir, string scriptName, string args)
    {
        string scriptPath = System.IO.Path.Combine(toolsDir, scriptName);

        if (!System.IO.File.Exists(scriptPath))
        {
            UnityEngine.Debug.LogWarning(
                $"{LogTag} 脚本未找到: {scriptPath}\n跳过。");
            return;
        }

        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = "powershell.exe",
                Arguments = $"-NoProfile -ExecutionPolicy Bypass -File \"{scriptPath}\" {args}",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            using var proc = Process.Start(psi);
            proc.WaitForExit(30000);

            string stdout = proc.StandardOutput.ReadToEnd();
            string stderr = proc.StandardError.ReadToEnd();

            if (proc.ExitCode == 0)
            {
                UnityEngine.Debug.Log(
                    $"{LogTag} {scriptName} 执行成功.\n{stdout}");
            }
            else
            {
                UnityEngine.Debug.LogWarning(
                    $"{LogTag} {scriptName} 返回非零退出码 ({proc.ExitCode})\n" +
                    $"stdout: {stdout}\nstderr: {stderr}");
            }
        }
        catch (System.Exception ex)
        {
            UnityEngine.Debug.LogError(
                $"{LogTag} 运行 {scriptName} 失败: {ex.Message}");
        }
    }
}
#endif
