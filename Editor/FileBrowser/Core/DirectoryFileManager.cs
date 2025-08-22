#if UNITY_EDITOR
using UnityEditor;
using System.IO;
using System.Collections.Generic;
using System;
using System.Diagnostics;
using System.Linq;
using UnityEngine;
using Debug = UnityEngine.Debug;

public static class DirectoryFileManager
{
    private const string DirectoryPrefKey = "FileBrowser_DirectoryPath";

    public static bool HasDirectoryPath()
    {
        return !string.IsNullOrEmpty(PlayerPrefs.GetString(DirectoryPrefKey, ""));
    }

    public static string GetDirectoryPath()
    {
        return PlayerPrefs.GetString(DirectoryPrefKey, "");
    }

    public static void SaveDirectoryPath(string path)
    {
        if (IsValidDirectoryPath(path))
        {
            PlayerPrefs.SetString(DirectoryPrefKey, path);
            PlayerPrefs.Save();
        }
        else
        {
            EditorUtility.DisplayDialog("错误", "无效的目录路径", "确定");
        }
    }

    public static string BrowseForDirectory()
    {
        string defaultPath = HasDirectoryPath() ? GetDirectoryPath() : "";
        return EditorUtility.OpenFolderPanel("选择目录", defaultPath, "");
    }

    public static void ClearDirectoryPath()
    {
        PlayerPrefs.DeleteKey(DirectoryPrefKey);
        PlayerPrefs.Save();
    }

    /// <summary>
    /// 结合搜索词和搜索模式获取文件列表
    /// </summary>
    public static List<FileInfo> SearchFiles(string directoryPath, string searchTerm, string searchPattern)
    {
        if (!IsValidDirectoryPath(directoryPath))
        {
            Debug.LogError($"无效的目录路径: {directoryPath}");
            return new List<FileInfo>();
        }

        try
        {
            // 处理多模式（如 "*.png;*.jpg"）
            var patterns = searchPattern.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(p => p.Trim())
                .Where(p => !string.IsNullOrEmpty(p))
                .ToList();

            // 如果没有有效模式，默认显示所有文件
            if (patterns.Count == 0)
                patterns.Add("*");

            // 获取所有匹配模式的文件
            HashSet<string> filePaths = new HashSet<string>();
            foreach (var pattern in patterns)
            {
                try
                {
                    // 搜索符合当前模式的文件
                    string[] currentFiles = Directory.GetFiles(directoryPath, pattern, SearchOption.AllDirectories);
                    foreach (var file in currentFiles)
                    {
                        filePaths.Add(file); // 使用HashSet避免重复文件
                    }
                }
                catch (ArgumentException ex)
                {
                    // 处理无效的搜索模式
                    Debug.LogWarning($"无效的搜索模式 '{pattern}': {ex.Message}");
                    continue;
                }
            }

            // 转换为FileInfo并过滤搜索词
            List<FileInfo> files = new List<FileInfo>();
            foreach (var path in filePaths)
            {
                if (IsValidFilePath(path))
                {
                    var fileInfo = new FileInfo(path);
                    // 应用搜索词过滤（不区分大小写）
                    if (string.IsNullOrEmpty(searchTerm) ||
                        fileInfo.Name.IndexOf(searchTerm, StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        files.Add(fileInfo);
                    }
                }
            }

            // 按文件名排序
            files.Sort((a, b) => string.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase));
            return files;
        }
        catch (Exception e)
        {
            Debug.LogError($"获取文件列表失败: {e.Message}");
            EditorUtility.DisplayDialog("错误", $"获取文件列表失败: {e.Message}", "确定");
            return new List<FileInfo>();
        }
    }

    public static void OpenFile(FileInfo file)
    {
        if (file == null || !file.Exists)
        {
            EditorUtility.DisplayDialog("错误", "文件不存在或已被删除", "确定");
            return;
        }

        try
        {
#if UNITY_EDITOR_WIN
            Process.Start(new ProcessStartInfo(file.FullName) { UseShellExecute = true });
#elif UNITY_EDITOR_OSX
            Process.Start("open", $"\"{file.FullName}\"");
#else
            Process.Start("xdg-open", $"\"{file.FullName}\"");
#endif
            Debug.Log($"已打开文件: {file.FullName}");
        }
        catch (Exception e)
        {
            Debug.LogError($"打开文件失败: {e.Message}");
            EditorUtility.DisplayDialog("错误", $"打开文件失败: {e.Message}", "确定");
        }
    }

    private static bool IsValidDirectoryPath(string path)
    {
        if (string.IsNullOrEmpty(path))
            return false;

        try
        {
            foreach (char c in Path.GetInvalidPathChars())
            {
                if (path.Contains(c.ToString()))
                    return false;
            }

            string fullPath = Path.GetFullPath(path);
            return Directory.Exists(fullPath);
        }
        catch
        {
            return false;
        }
    }

    private static bool IsValidFilePath(string path)
    {
        if (string.IsNullOrEmpty(path))
            return false;

        try
        {
            foreach (char c in Path.GetInvalidFileNameChars())
            {
                if (Path.GetFileName(path).Contains(c.ToString()))
                    return false;
            }

            string fullPath = Path.GetFullPath(path);
            return File.Exists(fullPath);
        }
        catch
        {
            return false;
        }
    }
}
#endif
