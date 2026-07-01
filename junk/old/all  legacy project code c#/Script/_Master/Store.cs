using System.Collections.Generic;
using UnityEngine;

public class Store : MonoBehaviour
{

    public static Store instance;
    public static void Init()
    {
        instance = ((GameObject)Resources.Load("Store")).GetComponent<Store>();
    }



    [Header("Image")]
    public Texture iconShopee;
    public Texture iconLazada;
    public Texture iconTiktok;
    public Texture iconLine;















    [System.Serializable]
    [UnityEngine.Scripting.Preserve]
    public class HtmlPages
    {
        public const string Condition = "Condition";
        public const string AddressForm = "AddressForm";
        public const string QRcode = "QRcode";
        public const string UploadSlip = "UploadSlip";
        public const string LinkMarketplace = "LinkMarketplace";
        public const string MyCoupons = "MyCoupons";
        public const string Expiring = "Expiring";
        public const string Rank = "Rank";
        public const string HowToUploadSlip = "HowToUploadSlip";
        public const string HowToLinkmarketplace = "HowToLinkmarketplace";
        public const string Referal = "Referal";
        public const string DailyCheckIn = "DailyCheckIn";
    }

    [System.Serializable]
    [UnityEngine.Scripting.Preserve]
    public class IframeData
    {
        static Dictionary<string, IframeData> htmlPages = new Dictionary<string, IframeData>();
        public static void Init()
        {
            if (GameStore.WebGLService.Config.ContainsKey("htmlPages"))
                htmlPages = GameStore.WebGLService.Config["htmlPages"].DeserializeObjectSimple<Dictionary<string, IframeData>>();
        }
        public static IframeData Get(string pageName)
        {
            if (
                CRMData.InitializeData.current != null &&
                CRMData.InitializeData.current.htmlPages != null &&
                CRMData.InitializeData.current.htmlPages.ContainsKey(pageName))
                return CRMData.InitializeData.current.htmlPages[pageName];

            if (htmlPages.ContainsKey(pageName))
                return htmlPages[pageName];
            return null;
        }



        public string path = "StreamingAssets/form/xxxxx.html";
        public float[] size = new float[] { 90, 90 };
        public string unit = "%";
        public IframeData Clone()
        {
            return new IframeData()
            {
                path = path,
                size = size,
                unit = unit
            };
        }
        public static IframeData Create(string path, float width, float height, string unit = "%")
        {
            return new IframeData()
            {
                path = path,
                size = new float[] { width, height },
                unit = unit
            };
        }
    }



}
