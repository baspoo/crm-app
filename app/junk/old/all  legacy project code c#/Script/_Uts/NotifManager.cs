using UnityEngine;
using System.Collections.Generic;
using System.Linq;





public class NotifManager
{




    [System.Serializable]
    public class ReadData
    {
        public List<string> keys = new();
        public List<bool> values = new();

        public Dictionary<string, bool> ToDict()
        {
            var dict = new Dictionary<string, bool>();

            for (int i = 0; i < keys.Count; i++)
            {
                dict[keys[i]] = values[i];
            }

            return dict;
        }

        public static ReadData FromDict(Dictionary<string, bool> dict)
        {
            var data = new ReadData();

            foreach (var kv in dict)
            {
                data.keys.Add(kv.Key);
                data.values.Add(kv.Value);
            }

            return data;
        }
    }


    public NotifManager(string key)
    {
        PREF_KEY = key;
        reads = null;
    }


    string PREF_KEY = "notif_items";
    Dictionary<string, bool> reads = null;

    // โหลดจาก PlayerPrefs
    void Load()
    {
        if (reads != null)
            return;

        if (!PlayerPrefs.HasKey(PREF_KEY))
        {
            reads = new Dictionary<string, bool>();
            return;
        }
        var json = PlayerPrefs.GetString(PREF_KEY);
        if (string.IsNullOrEmpty(json))
        {
            reads = new Dictionary<string, bool>();
            return;
        }
        var data = JsonUtility.FromJson<ReadData>(json);
        if (data == null)
        {
            reads = new Dictionary<string, bool>();
            return;
        }
        reads = data.ToDict();
    }

    // Save
    void Save()
    {
        var data = ReadData.FromDict(reads);
        var json = JsonUtility.ToJson(data);

        PlayerPrefs.SetString(PREF_KEY, json);
        PlayerPrefs.Save();
    }

    // ============================================
    // REGISTER
    // ============================================
    public bool RegisterItems(string[] itemIds)
    {
        Load();

        bool isFirstTime = !PlayerPrefs.HasKey(PREF_KEY);
        bool hasNew = false;

        var newSet = new HashSet<string>(itemIds);

        // ---------- ลบของเก่าที่ไม่มีแล้ว ----------
        var removeList = reads.Keys
            .Where(k => !newSet.Contains(k))
            .ToList();

        foreach (var k in removeList)
            reads.Remove(k);

        // ---------- เพิ่มของใหม่ ----------
        foreach (var id in itemIds)
        {
            if (!reads.ContainsKey(id))
            {
                // ครั้งแรก = อ่านหมด
                if (isFirstTime)
                    reads[id] = true;
                else
                {
                    reads[id] = false;
                    hasNew = true;
                }
            }
        }

        Save();

        return hasNew;
    }

    // ============================================
    // READ ITEM
    // ============================================
    public void ReadItem(string itemId)
    {
        Load();

        if (reads.ContainsKey(itemId))
        {
            if(reads[itemId] == false)
            {
                reads[itemId] = true;
                Save();
            }
        }
    }

    // ============================================
    // CHECK READ
    // ============================================
    public bool IsRead(string itemId)
    {
        Load();

        if (!reads.ContainsKey(itemId))
            return true;

        return reads[itemId];
    }

    // ============================================
    // CHECK RED DOT
    // ============================================
    public bool HasNotif()
    {
        Load();

        return reads.Values.Any(v => v == false);
    }
}
