using UnityEngine;
using System;
using System.Collections.Generic;


public static class LocalImageCache
{
    const string PREFIX = "saveimg_";
    const string ALL_KEY = "saveimg_all";
    static int MAXSIZE = 720;
    static int QUALITY = 85;
    static bool SETTING = false;
    static bool ACTIVE = false;
    static void Setting()
    {
        if (!SETTING)
        {
            SETTING = true;
            GameStore.WebGLService.Config.Modify("localImage", (val) =>
            {
                var data = val.ToDictStringObject();
                ACTIVE = data.Find("active").ToBool();
                MAXSIZE = data.Find("maxSize").ToInt();
                QUALITY = data.Find("quality").ToInt();
            });
            Debug.Log(ACTIVE ? $"LocalImageCache Active (maxSize: {MAXSIZE}, quality: {QUALITY})" : "LocalImageCache Inactive");
        }
    }
    // ================================
    // SAVE
    // ================================
    public static void Save(Texture2D tex, string url)
    {

       Setting();

        if (!ACTIVE)
            return;

        if (tex == null || string.IsNullOrEmpty(url))
            return;

        // Resize
        tex = MakeReadable(tex);
        Texture2D resized = Resize(tex, MAXSIZE);

        // Encode JPG (quality 65)
        byte[] jpg = resized.EncodeToJPG(QUALITY);

        // Base64
        string base64 = Convert.ToBase64String(jpg);

        string key = PREFIX + Hash(url);

        // Save image
        PlayerPrefs.SetString(key, base64);

        // Save index
        AddToIndex(key);

        PlayerPrefs.Save();
    }
    static Texture2D MakeReadable(Texture2D src)
    {
        RenderTexture rt = RenderTexture.GetTemporary(
            src.width,
            src.height,
            0,
            RenderTextureFormat.Default,
            RenderTextureReadWrite.Linear
        );

        Graphics.Blit(src, rt);

        RenderTexture prev = RenderTexture.active;
        RenderTexture.active = rt;

        Texture2D readable = new Texture2D(
            src.width,
            src.height,
            TextureFormat.RGB24,
            false
        );

        readable.ReadPixels(
            new Rect(0, 0, rt.width, rt.height),
            0,
            0
        );

        readable.Apply();

        RenderTexture.active = prev;
        RenderTexture.ReleaseTemporary(rt);

        return readable;
    }

    // ================================
    // LOAD
    // ================================
    public static Texture2D Load(string url)
    {
        Setting();

        if (!ACTIVE)
            return null;

        string key = PREFIX + Hash(url);

        if (!PlayerPrefs.HasKey(key))
            return null;

        string base64 = PlayerPrefs.GetString(key);
        byte[] data = Convert.FromBase64String(base64);

        Texture2D tex = new Texture2D(2, 2);
        tex.LoadImage(data);

        return tex;
    }

    // ================================
    // CHECK
    // ================================
    public static bool IsHas(string url)
    {
        string key = PREFIX + Hash(url);
        return PlayerPrefs.HasKey(key);
    }

    // ================================
    // REMOVE ONE
    // ================================
    public static void Remove(string url)
    {
        string key = PREFIX + Hash(url);

        if (PlayerPrefs.HasKey(key))
        {
            PlayerPrefs.DeleteKey(key);
            RemoveFromIndex(key);
            PlayerPrefs.Save();
        }
    }

    // ================================
    // CLEAR ALL
    // ================================
    public static void ClearAll()
    {
        var list = GetIndex();

        foreach (var key in list)
        {
            PlayerPrefs.DeleteKey(key);
        }

        PlayerPrefs.DeleteKey(ALL_KEY);
        PlayerPrefs.Save();
    }

    // ================================
    // INDEX SYSTEM
    // ================================
    static void AddToIndex(string key)
    {
        var list = GetIndex();

        if (!list.Contains(key))
        {
            list.Add(key);
            SaveIndex(list);
        }
    }

    static void RemoveFromIndex(string key)
    {
        var list = GetIndex();

        if (list.Remove(key))
        {
            SaveIndex(list);
        }
    }

    static List<string> GetIndex()
    {
        if (!PlayerPrefs.HasKey(ALL_KEY))
            return new List<string>();

        string json = PlayerPrefs.GetString(ALL_KEY);

        return JsonUtility
            .FromJson<StringList>(json)
            .list;
    }

    static void SaveIndex(List<string> list)
    {
        var data = new StringList { list = list };
        string json = JsonUtility.ToJson(data);

        PlayerPrefs.SetString(ALL_KEY, json);
    }

    // ================================
    // RESIZE
    // ================================
    static Texture2D Resize(Texture2D src, int maxSize)
    {
        int w = src.width;
        int h = src.height;

        if (w <= maxSize && h <= maxSize)
            return src;

        float ratio = Mathf.Min(
            (float)maxSize / w,
            (float)maxSize / h
        );

        int nw = Mathf.RoundToInt(w * ratio);
        int nh = Mathf.RoundToInt(h * ratio);

        RenderTexture rt = RenderTexture.GetTemporary(nw, nh);
        Graphics.Blit(src, rt);

        RenderTexture prev = RenderTexture.active;
        RenderTexture.active = rt;

        Texture2D tex = new Texture2D(nw, nh, TextureFormat.RGB24, false);
        tex.ReadPixels(new Rect(0, 0, nw, nh), 0, 0);
        tex.Apply();

        RenderTexture.active = prev;
        RenderTexture.ReleaseTemporary(rt);

        return tex;
    }

    // ================================
    // HASH URL → SAFE KEY
    // ================================
    static string Hash(string input)
    {
        return input.GetHashCode().ToString();
    }

    // ================================
    // JSON Helper
    // ================================
    [Serializable]
    class StringList
    {
        public List<string> list = new();
    }
}
