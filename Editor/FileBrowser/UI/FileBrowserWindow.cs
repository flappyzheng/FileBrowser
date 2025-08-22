#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System.IO;

public class FileBrowserWindow : EditorWindow
{
    // 搜索词（文件名包含的关键词）
    public string searchTerm
    {
        get => _searchTerm;
        set
        {
            if (_searchTerm != value)
            {
                _searchTerm = value;
                SaveSearchTerm();
                RefreshFileList();
            }
        }
    }

    // 文件搜索模式（如 *.txt、*.cs 等）
    public string searchPattern
    {
        get => _searchPattern;
        set
        {
            if (_searchPattern != value)
            {
                _searchPattern = value;
                SaveSearchPattern();
                RefreshFileList();
            }
        }
    }

    private string _searchTerm = "";
    private string _searchPattern = "*"; // 默认显示所有文件
    private List<FileInfo> _fileList = new List<FileInfo>();
    private Vector2 _scrollPosition;
    private string _currentDirectory = "";
    
    // PlayerPrefs 存储键
    private const string SearchTermPrefKey = "FileBrowser_SearchTerm";
    private const string SearchPatternPrefKey = "FileBrowser_SearchPattern";

    [MenuItem("Tools/File Browser/Open File Browser")]
    public static void ShowWindow()
    {
        GetWindow<FileBrowserWindow>("File Browser");
    }

    private void OnEnable()
    {
        // 加载保存的目录、搜索词和搜索模式
        if (DirectoryFileManager.HasDirectoryPath())
        {
            _currentDirectory = DirectoryFileManager.GetDirectoryPath();
        }

        LoadSearchTerm();
        LoadSearchPattern();
        RefreshFileList();
    }

    private void OnGUI()
    {
        DrawHeader();

        if (!DirectoryFileManager.HasDirectoryPath())
        {
            DrawNoDirectorySelectedUI();
            return;
        }

        if (!Directory.Exists(_currentDirectory))
        {
            DrawInvalidDirectoryUI();
            return;
        }

        DrawSearchControls();
        DrawFileList();
    }

    private void DrawHeader()
    {
        GUILayout.BeginHorizontal(EditorStyles.toolbar);
        
        GUILayout.Label("当前目录:", EditorStyles.label, GUILayout.Width(70));
        
        string displayPath = _currentDirectory;
        if (displayPath.Length > 50)
        {
            displayPath = "..." + displayPath.Substring(displayPath.Length - 47);
        }
        
        GUILayout.Label(displayPath, EditorStyles.textField, GUILayout.ExpandWidth(true));
        
        if (GUILayout.Button("更改目录", EditorStyles.toolbarButton))
        {
            string newPath = DirectoryFileManager.BrowseForDirectory();
            if (!string.IsNullOrEmpty(newPath))
            {
                DirectoryFileManager.SaveDirectoryPath(newPath);
                _currentDirectory = newPath;
                RefreshFileList();
            }
        }
        
        if (GUILayout.Button("刷新", EditorStyles.toolbarButton))
        {
            RefreshFileList();
        }
        
        GUILayout.EndHorizontal();
    }

    /// <summary>
    /// 绘制搜索控件（包含搜索词和搜索模式）
    /// </summary>
    private void DrawSearchControls()
    {
        GUILayout.Space(5);

        // 搜索词输入
        GUILayout.BeginHorizontal();
        GUILayout.Label("搜索词:", GUILayout.Width(60));
        string newSearchTerm = EditorGUILayout.TextField(searchTerm);
        if (newSearchTerm != searchTerm)
        {
            searchTerm = newSearchTerm;
        }

        if (!string.IsNullOrEmpty(searchTerm) && GUILayout.Button("清除", GUILayout.Width(50)))
        {
            searchTerm = "";
        }
        GUILayout.EndHorizontal();

        // 文件搜索模式输入
        GUILayout.BeginHorizontal();
        GUILayout.Label("文件模式:", GUILayout.Width(60));
        string newSearchPattern = EditorGUILayout.TextField(searchPattern);
        if (newSearchPattern != searchPattern)
        {
            searchPattern = newSearchPattern;
        }

        // 常用模式快捷按钮
        GUILayout.Label("常用:", GUILayout.Width(40));
        if (GUILayout.Button("所有文件", GUILayout.Width(80)))
        {
            searchPattern = "*";
        }
        if (GUILayout.Button("脚本", GUILayout.Width(60)))
        {
            searchPattern = "*.cs";
        }
        if (GUILayout.Button("图片", GUILayout.Width(60)))
        {
            searchPattern = "*.png;*.jpg;*.jpeg;*.gif";
        }
        GUILayout.EndHorizontal();

        GUILayout.Space(5);
    }

    private void DrawFileList()
    {
        if (_fileList.Count == 0)
        {
            GUILayout.Label("没有找到匹配的文件", EditorStyles.centeredGreyMiniLabel);
            return;
        }

        _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);

        foreach (var file in _fileList)
        {
            GUILayout.BeginHorizontal();

            if (GUILayout.Button(file.Name, EditorStyles.label))
            {
                DirectoryFileManager.OpenFile(file);
            }

            string fileSize = FormatFileSize(file.Length);
            GUILayout.Label(fileSize, EditorStyles.boldLabel, GUILayout.Width(80));

            GUILayout.EndHorizontal();
            EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
        }

        EditorGUILayout.EndScrollView();
    }

    private void DrawNoDirectorySelectedUI()
    {
        GUILayout.Space(20);
        GUILayout.Label("尚未选择目录", EditorStyles.boldLabel);
        GUILayout.Space(10);
        GUILayout.Label("请点击下方按钮选择要浏览的目录", EditorStyles.label);
        GUILayout.Space(10);

        if (GUILayout.Button("选择目录", GUILayout.Height(30)))
        {
            string newPath = DirectoryFileManager.BrowseForDirectory();
            if (!string.IsNullOrEmpty(newPath))
            {
                DirectoryFileManager.SaveDirectoryPath(newPath);
                _currentDirectory = newPath;
                RefreshFileList();
            }
        }
    }

    private void DrawInvalidDirectoryUI()
    {
        GUILayout.Space(20);
        GUILayout.Label("目录无效或不存在", EditorStyles.boldLabel);
        GUILayout.Space(10);
        GUILayout.Label($"当前路径: {_currentDirectory}", EditorStyles.label);
        GUILayout.Space(10);

        if (GUILayout.Button("重新选择目录", GUILayout.Height(30)))
        {
            string newPath = DirectoryFileManager.BrowseForDirectory();
            if (!string.IsNullOrEmpty(newPath))
            {
                DirectoryFileManager.SaveDirectoryPath(newPath);
                _currentDirectory = newPath;
                RefreshFileList();
            }
        }

        if (GUILayout.Button("清除保存的路径", GUILayout.Height(25)))
        {
            DirectoryFileManager.ClearDirectoryPath();
            _currentDirectory = "";
        }
    }

    private void RefreshFileList()
    {
        if (string.IsNullOrEmpty(_currentDirectory) || !Directory.Exists(_currentDirectory))
        {
            _fileList.Clear();
            return;
        }

        // 结合搜索词和搜索模式过滤文件
        _fileList = DirectoryFileManager.SearchFiles(
            _currentDirectory, 
            searchTerm, 
            searchPattern
        );
    }

    private string FormatFileSize(long bytes)
    {
        if (bytes < 1024)
            return $"{bytes} B";
        else if (bytes < 1048576)
            return $"{(bytes / 1024f):F1} KB";
        else if (bytes < 1073741824)
            return $"{(bytes / 1048576f):F1} MB";
        else
            return $"{(bytes / 1073741824f):F1} GB";
    }

    #region 持久化方法
    private void LoadSearchTerm()
    {
        _searchTerm = PlayerPrefs.GetString(SearchTermPrefKey, "");
    }

    private void SaveSearchTerm()
    {
        PlayerPrefs.SetString(SearchTermPrefKey, _searchTerm);
        PlayerPrefs.Save();
    }

    private void LoadSearchPattern()
    {
        _searchPattern = PlayerPrefs.GetString(SearchPatternPrefKey, "*");
    }

    private void SaveSearchPattern()
    {
        // 确保搜索模式不为空，至少保留通配符
        if (string.IsNullOrEmpty(_searchPattern))
            _searchPattern = "*";
            
        PlayerPrefs.SetString(SearchPatternPrefKey, _searchPattern);
        PlayerPrefs.Save();
    }

    public void ClearSearchSettings()
    {
        searchTerm = "";
        searchPattern = "*";
        PlayerPrefs.DeleteKey(SearchTermPrefKey);
        PlayerPrefs.DeleteKey(SearchPatternPrefKey);
        PlayerPrefs.Save();
    }
    #endregion
}
#endif
    