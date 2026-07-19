using System;
using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UIElements;
using System.Reflection;

[InitializeOnLoad]
public static class SceneBootToolbar
{
    #region 配置区 自行修改
    // 扫描场景的根目录
    private const string ScanSceneFolder = "Assets/GameMain/Scenes";
    // 持久化存储key
    private const string Key_LastSelectScene = "BootToolbar_LastScene";
    private const string Key_CacheSceneList = "BootToolbar_CacheScenes";
    #endregion

    private static ScriptableObject _toolbarObj;
    private static List<string> _scenePathList = new List<string>();
    private static List<string> _sceneShowNameList = new List<string>();
    private static int _selectIndex = 0;
    private static string _waitOpenScenePath = null;

    static SceneBootToolbar()
    {
        EditorApplication.playModeStateChanged += OnPlayStateChange;
        EditorApplication.update += InjectToolbarUI;
        LoadSavedSelect();
        RefreshSceneList();
    }

    #region 注入Toolbar右侧UI
    private static void InjectToolbarUI()
    {
        if (_toolbarObj != null) return;
        Type toolbarType = typeof(Editor).Assembly.GetType("UnityEditor.Toolbar");
        var toolbars = Resources.FindObjectsOfTypeAll(toolbarType);
        if (toolbars.Length == 0) return;
        _toolbarObj = toolbars[0] as ScriptableObject;

        FieldInfo rootField = _toolbarObj.GetType().GetField("m_Root", BindingFlags.NonPublic | BindingFlags.Instance);
        VisualElement root = rootField.GetValue(_toolbarObj) as VisualElement;
        if (root == null) return;

        VisualElement rightZone = root.Q("ToolbarZoneRightAlign");
        VisualElement container = new VisualElement()
        {
            style = { flexDirection = FlexDirection.Row, paddingLeft = 10 }
        };
        IMGUIContainer gui = new IMGUIContainer(DrawToolbarGUI);
        container.Add(gui);
        rightZone.Add(container);
    }

    private static void DrawToolbarGUI()
    {
        GUILayout.BeginHorizontal();
        // 场景下拉框
        int newIdx = EditorGUILayout.Popup(_selectIndex, _sceneShowNameList.ToArray(), GUILayout.Width(180));
        if (newIdx != _selectIndex)
        {
            _selectIndex = newIdx;
            SaveSelectScene();
        }

        GUILayout.Space(6);
        // 启动按钮
        Texture playIcon = EditorGUIUtility.FindTexture("PlayButton");
        if (GUILayout.Button(new GUIContent("Start", playIcon), GUILayout.Width(60)))
        {
            LaunchSelectSceneBoot();
        }
        GUILayout.EndHorizontal();
    }
    #endregion

    #region 扫描指定目录场景
    /// 刷新目录下所有场景，生成显示名称+完整路径
    private static void RefreshSceneList()
    {
        _scenePathList.Clear();
        _sceneShowNameList.Clear();
        // 查找目录下所有场景资源
        string[] sceneGuids = AssetDatabase.FindAssets("t:Scene", new[] { ScanSceneFolder });
        foreach (var guid in sceneGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            string showName = System.IO.Path.GetFileNameWithoutExtension(path);
            _scenePathList.Add(path);
            _sceneShowNameList.Add(showName);
        }
        // 修正选中下标防止越界
        if (_selectIndex >= _scenePathList.Count)
            _selectIndex = 0;
    }
    #endregion

    #region 本地持久化记忆选中场景
    private static void SaveSelectScene()
    {
        if (_scenePathList.Count == 0) return;
        string selectPath = _scenePathList[_selectIndex];
        EditorPrefs.SetString(Key_LastSelectScene, selectPath);
    }

    private static void LoadSavedSelect()
    {
        string savedPath = EditorPrefs.GetString(Key_LastSelectScene, "");
        if (string.IsNullOrEmpty(savedPath)) return;
        int idx = _scenePathList.IndexOf(savedPath);
        if (idx >= 0) _selectIndex = idx;
    }
    #endregion

    #region 一键启动逻辑
    private static void LaunchSelectSceneBoot()
    {
        if (EditorApplication.isPlaying)
        {
            EditorApplication.isPlaying = false;
            return;
        }
        if (_scenePathList.Count == 0)
        {
            EditorUtility.DisplayDialog("提示", $"目录无场景：{ScanSceneFolder}", "确定");
            return;
        }
        // 保存当前修改
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            return;

        // 缓存当前所有打开场景路径，退出播放恢复
        CacheCurrentOpenScenes();

        // 记录要打开的启动场景
        _waitOpenScenePath = _scenePathList[_selectIndex];
        EditorApplication.update += WaitOpenBootScene;
    }

    private static void WaitOpenBootScene()
    {
        if (string.IsNullOrEmpty(_waitOpenScenePath)
            || EditorApplication.isPlaying
            || EditorApplication.isCompiling)
            return;

        EditorApplication.update -= WaitOpenBootScene;
        EditorSceneManager.OpenScene(_waitOpenScenePath, OpenSceneMode.Single);
        EditorApplication.isPlaying = true;
        _waitOpenScenePath = null;
    }

    // 缓存当前所有编辑场景
    private static void CacheCurrentOpenScenes()
    {
        StringBuilder sb = new StringBuilder();
        int count = EditorSceneManager.loadedSceneCount;
        for (int i = 0; i < count; i++)
        {
            var sc = EditorSceneManager.GetSceneAt(i);
            sb.Append(sc.path).Append(";");
        }
        EditorPrefs.SetString(Key_CacheSceneList, sb.ToString());
    }
    #endregion

    #region 退出播放自动恢复原场景
    private static void OnPlayStateChange(PlayModeStateChange state)
    {
        if (state != PlayModeStateChange.EnteredEditMode) return;
        string cacheStr = EditorPrefs.GetString(Key_CacheSceneList, "");
        if (string.IsNullOrEmpty(cacheStr)) return;
        string[] paths = cacheStr.Split(';', StringSplitOptions.RemoveEmptyEntries);
        if (paths.Length == 0) return;

        // 恢复多场景编辑状态
        EditorSceneManager.OpenScene(paths[0], OpenSceneMode.Single);
        for (int i = 1; i < paths.Length; i++)
        {
            EditorSceneManager.OpenScene(paths[i], OpenSceneMode.Additive);
        }
        EditorPrefs.DeleteKey(Key_CacheSceneList);
    }
    #endregion
}