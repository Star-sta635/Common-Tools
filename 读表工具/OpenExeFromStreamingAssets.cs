using System.Diagnostics;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Debug = UnityEngine.Debug;

/// <summary>
/// Unity编辑器工具：按下F2打开StreamingAssets下读表工具的EXE文件
/// </summary>
public class OpenExeFromStreamingAssets
{
    // 配置项：修改为你的EXE文件名
    private const string TargetExeName = "main.exe";

    // 绑定快捷键：F2（可修改为其他键，如%F2=Ctrl+F2，&F2=Alt+F2）
    [MenuItem("Tools/打开StreamingAssets下的EXE文件 %F2")]
    public static void OpenTargetExe()
    {
        try
        {
            // 1. 获取StreamingAssets的绝对路径（编辑器下的路径）
            string streamingAssetsPath = Application.streamingAssetsPath+ "/TableExcels/Tools/ExportExcels/";
            if (!Directory.Exists(streamingAssetsPath))
            {
                // 如果StreamingAssets目录不存在，自动创建
                Directory.CreateDirectory(streamingAssetsPath);
                EditorUtility.DisplayDialog("提示", "StreamingAssets目录不存在，已自动创建", "确定");
                return;
            }

            // 2. 拼接EXE文件的完整路径
            string exeFullPath = Path.Combine(streamingAssetsPath, TargetExeName);

            // 3. 检查EXE文件是否存在
            if (!File.Exists(exeFullPath))
            {
                EditorUtility.DisplayDialog("错误", $"未找到EXE文件：\n{exeFullPath}", "确定");
                // 可选：打开StreamingAssets目录，方便手动放入EXE
                Process.Start("explorer.exe", streamingAssetsPath);
                return;
            }

            // 4. 启动EXE文件
            ProcessStartInfo startInfo = new ProcessStartInfo
            {
                FileName = exeFullPath,
                WorkingDirectory = Path.GetDirectoryName(exeFullPath), // 设置工作目录为EXE所在文件夹
                UseShellExecute = true // 允许系统外壳执行（解决权限问题）
            };
            Process.Start(startInfo);

            Debug.Log($"成功打开EXE文件：{exeFullPath}");
        }
        catch (System.Exception e)
        {
            EditorUtility.DisplayDialog("执行失败", $"错误信息：{e.Message}", "确定");
            Debug.LogError($"打开EXE失败：{e}");
        }
    }
    // 快捷键：F3
    [MenuItem("Tools/从初始场景运行 %F3")]
    public static void PlayFromFirstSceneInBuildSettings()
    {
        try
        {
            // 1. 如果正在运行，先停止
            if (EditorApplication.isPlaying)
            {
                EditorApplication.isPlaying = false;
                Debug.Log("已停止播放模式");
                return;
            }

            // 2. 检查是否有场景在Build列表
            if (EditorBuildSettings.scenes.Length == 0)
            {
                EditorUtility.DisplayDialog("错误", "Build Settings 中没有添加任何场景！", "确定");
                return;
            }

            // 3. 获取第一个场景（初始场景）
            EditorBuildSettingsScene firstScene = EditorBuildSettings.scenes[0];
            if (!firstScene.enabled || string.IsNullOrEmpty(firstScene.path))
            {
                EditorUtility.DisplayDialog("错误", "第一个场景未启用或路径为空！", "确定");
                return;
            }

            string scenePath = firstScene.path;

            // 4. 保存当前修改
            if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                // 5. 打开初始场景并播放
                EditorSceneManager.OpenScene(scenePath);
                EditorApplication.isPlaying = true;

                Debug.Log($"已从初始场景运行：{scenePath}");
            }
        }
        catch (System.Exception e)
        {
            EditorUtility.DisplayDialog("运行失败", $"错误信息：{e.Message}", "确定");
            Debug.LogError($"从初始场景运行失败：{e}");
        }
    }
}