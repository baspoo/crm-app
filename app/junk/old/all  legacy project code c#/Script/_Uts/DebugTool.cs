#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.Experimental.SceneManagement;
using GameStore.Core;
 

public class DebugTool
{
    internal const string KEY_PAYLOAD = "DEBUG_PAYLOAD";
    internal const string KEY_USERID = "DEBUG_USERID";
    internal const string KEY_GAMEID = "DEBUG_GAMEID";
    internal const string KEY_THEMEID = "DEBUG_THEMEID";
    internal const string KEY_THEME_GID = "DEBUG_THEME_GID";
    internal const string KEY_IGNORE_CONSOLE_AT_SCENE_GID = "DEBUG_IGNORE_CONSOLE_AT_SCENE_GID";

    public static string GetPayload()
    {
        return EditorPrefs.GetString(KEY_PAYLOAD, "");
    }
    public static string GetUserId()
    {
        return EditorPrefs.GetString(KEY_USERID, "");
    }
    public static string GetGameId()
    {
        return EditorPrefs.GetString(KEY_GAMEID, "");
    }
    public static string GetThemeId()
    {
        return EditorPrefs.GetString(KEY_THEMEID, "");
    }
    public static bool IsIgnoreConsoleAtScene()
    {
        return EditorPrefs.GetBool(KEY_IGNORE_CONSOLE_AT_SCENE_GID, false);
    }

    public static CustomerTheme GetTheme()
    {
        var g = LoadObject(KEY_THEME_GID);
        if (g == null) return null; return g.GetComponent<CustomerTheme>();
    }





    internal static GameObject LoadObject(string key)
    {
        string gidStr = EditorPrefs.GetString(key, "");

        if (string.IsNullOrEmpty(gidStr))
            return null;

        if (GlobalObjectId.TryParse(gidStr, out GlobalObjectId gid))
        {
            Object obj = GlobalObjectId.GlobalObjectIdentifierToObjectSlow(gid);
            return obj as GameObject;
        }

        return null;
    }
}



public class DebugToolWindow : EditorWindow
{


    string payload;
    string userId;
    string gameId;
    string themeId;
    GameObject themeObj;
    bool isIgnoreConsoleAtScene;



    [MenuItem("REFLEX/Editor/CRM/Debug Tool")]
    static void Open()
    {
        GetWindow<DebugToolWindow>("Debug Tool");
    }

    void OnEnable()
    {
        payload = EditorPrefs.GetString(DebugTool.KEY_PAYLOAD, "");
        userId = EditorPrefs.GetString(DebugTool.KEY_USERID, "");
        gameId = EditorPrefs.GetString(DebugTool.KEY_GAMEID, "");
        themeId = EditorPrefs.GetString(DebugTool.KEY_THEMEID, "");
        themeObj = DebugTool.LoadObject(DebugTool.KEY_THEME_GID);
    }

    void OnGUI()
    {
        EditorGUI.BeginChangeCheck();



        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("Debug Login", EditorStyles.boldLabel);
        if (GUILayout.Button("Open LINE Login... [DEV]"))
        {
            Application.OpenURL($"https://gamestore-gg.1mobystudio.com/crm_platform/linelogin.html?appid={gameId}&debug=1");
        }
        if (GUILayout.Button("Open LINE Login... [PROD]"))
        {
            Application.OpenURL($"https://crm.reflexstudio.co/linelogin.html?appid={gameId}&debug=1");
        }
        payload = EditorGUILayout.TextArea(payload, GUILayout.Height(100));
        userId = EditorGUILayout.TextField("User ID", userId);
        gameId = EditorGUILayout.TextField("Game ID", gameId);


        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("AssetsBundle", EditorStyles.boldLabel);
        var assetsBundleHandle = FindFirstObjectByType<AssetsBundleHandle>();
        assetsBundleHandle.loadType = (AssetsBundleHandle.LoadType)EditorGUILayout.EnumPopup("LoadType",assetsBundleHandle.loadType);


        EditorGUILayout.Space(20);
        EditorGUILayout.LabelField("Debug Theme", EditorStyles.boldLabel);
        themeId = EditorGUILayout.TextField("Theme ID", themeId);
        themeObj = (GameObject)EditorGUILayout.ObjectField("Theme", themeObj, typeof(GameObject), true);
        isIgnoreConsoleAtScene = EditorGUILayout.Toggle("Ignore Console @Scene", isIgnoreConsoleAtScene);



        if (EditorGUI.EndChangeCheck())
        {
            Save();
        }
    }

    void Save()
    {
        EditorPrefs.SetString(DebugTool.KEY_PAYLOAD, payload);
        EditorPrefs.SetString(DebugTool.KEY_USERID, userId);
        EditorPrefs.SetString(DebugTool.KEY_GAMEID, gameId);
        EditorPrefs.SetString(DebugTool.KEY_THEMEID, themeId);
        EditorPrefs.SetBool(DebugTool.KEY_IGNORE_CONSOLE_AT_SCENE_GID, isIgnoreConsoleAtScene);

        SaveObject(DebugTool.KEY_THEME_GID, themeObj);
    }

    void SaveObject(string key, GameObject obj)
    {
        if (obj == null)
        {
            EditorPrefs.DeleteKey(key);
            return;
        }

        GlobalObjectId gid = GlobalObjectId.GetGlobalObjectIdSlow(obj);
        EditorPrefs.SetString(key, gid.ToString());
    }


}

#endif