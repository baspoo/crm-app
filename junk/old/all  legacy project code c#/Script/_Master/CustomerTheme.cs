using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using GameStore;
using GameStore.Core;

[UnityEngine.Scripting.Preserve]
public class CustomerTheme : MonoBehaviour
{
    public static CustomerTheme instance;
    public static IEnumerator Load()
    {
        CustomerTheme theme = null;
        string themeId = CRMData.InitializeData.current.themeId;

        // demo ....
#if UNITY_EDITOR
        theme = DebugTool.GetTheme();
        if (theme == null && DebugTool.GetThemeId().notnull())
            themeId = DebugTool.GetThemeId();
#endif


        // download ....
        if (theme == null)
        {
            UIRoot.instance.OnDownloadAssetsBundle(themeId, "Theme", LiffBridge.ThemeVersion, (ok, err, asset) =>
            {
                if (ok)
                {
                    Debug.Log("GET CustomerTheme!");
                    theme = asset.GetComponent<CustomerTheme>();
                }
                else
                {
                    Manager.mg.OnFailed(err);
                }
            });
            yield return new WaitWhile(() => theme == null);
        }
        Debug.Log("GET THEME COMPLETED!");
        theme.inited  = false;
        theme.spritesLoaded = false;
        Manager.mg.StartCoroutine(theme.Init());
        Manager.mg.StartCoroutine(theme.LoadSprite());
        yield return new WaitWhile(() => !theme.inited || !theme.spritesLoaded);
    }


    bool inited = false;
    public IEnumerator Init()
    {
        instance = this;


        //** Override... Perfab
        var master = GameStore.Core.GameBundle.instance.uiTemplate;
        if (uiTemplateOverride.perfabRotateScreen != null)
            master.perfabRotateScreen = uiTemplateOverride.perfabRotateScreen;
        if (uiTemplateOverride.perfabLoading != null)
            master.perfabRotateScreen = uiTemplateOverride.perfabLoading;
        if (uiTemplateOverride.perfabPopup != null)
            master.perfabRotateScreen = uiTemplateOverride.perfabPopup;
        if (uiTemplateOverride.perfabTopMessage != null)
            master.perfabRotateScreen = uiTemplateOverride.perfabTopMessage;
        if (uiTemplateOverride.perfabKeyBoardPanel != null)
            master.perfabRotateScreen = uiTemplateOverride.perfabKeyBoardPanel;
        Debug.Log("SETUP OVERRIDE Perfab COMPLETED!");


        //** Get && Setup Language ..
        if (!GameStore.Language.IsSetup())
        {
            var language = GameStore.Language.ConvertType(CRMData.InitializeData.current.defaultLanguage, DefaultLanguage);
            language = GameStore.Language.SetDefault(language);
        }
        yield return Manager.mg.StartCoroutine(GameStore.Language.InitCorotine());
        ReplaceLanguage();
        Debug.Log("SETUP LANGUAGE COMPLETED!");
        yield return new WaitForEndOfFrame();
        inited = true;
    }
    bool spritesLoaded = false;
    IEnumerator LoadSprite()
    {
        //Debug.Log("########## LoadSprite ##########");
        //** Load Custom Sprite Atlas
        if (customeThemeSprite != null && customeThemeSprite.Count > 0
        && CRMData.InitializeData.current.customAssets != null && CRMData.InitializeData.current.customAssets.Count > 0)
        {
            var matchedAssets = customeThemeSprite.FindAll(x => CRMData.InitializeData.current.customAssets.ContainsKey(x.name));
            int loadCount = matchedAssets.Count;
            int loadDoneCount = 0;
            foreach (var sprite in matchedAssets)
            {
                    UIRoot.instance.LoadImage(CRMData.InitializeData.current.customAssets[sprite.name], (texture) =>
                    {
                        loadDoneCount++;
                        if (texture != null)
                        {
                            var sprites = texture.BuildAtlas((int)sprite.slice.x, (int)sprite.slice.y);
                            sprite.SetSprites(sprites);
                            Debug.Log($"########## LoadSprite (App-{sprite.name} = {sprites.Count}) ##########");
                            if (sprite.name == CRMData.InitializeData.CustomAssetsName.pointIcon)
                            {
                                iconCurrencyImage = sprite.GetSprite();
                            }
                        }
                    });
            }
            yield return new WaitWhile(() => loadCount != loadDoneCount);
        }
        Debug.Log("SETUP CUSTOM SPRITES COMPLETED!");
        spritesLoaded = true;
    }
    void ReplaceLanguage()
    {
        // Replace by custom language in theme if exist
        if (Languages != null && Languages.Count > 0)
        {
            var textFile = Languages.Find(x => x.type == Language.GetLanguage()) ?? Languages[0];
            var customLanguage = textFile.textAsset.text.DeserializeObjectSimple<Dictionary<string, string>>();
            foreach (var item in customLanguage)
            {
                if (Language.LanguageData.ContainsKey(item.Key))
                {
                    Language.LanguageData[item.Key] = item.Value;
                }
                else
                {
                    Language.LanguageData.Add(item.Key, item.Value);
                }
            }
        }
    }


























/*

    [System.Serializable]
    [UnityEngine.Scripting.Preserve]
    public class IframeData
    {
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
    [Header("iframe-URL")]
    public IframeData ConditionURL = new IframeData() { path = "StreamingAssets/form/Condition.html" };
    public IframeData AddressFormURL = new IframeData() { path = "StreamingAssets/form/AddressForm.html" };
    public IframeData QRcodeURL = new IframeData() { path = "StreamingAssets/form/QRcode.html" };
    public IframeData UploadSlip = new IframeData() { path = "StreamingAssets/form/UploadSlip.html" };
    public IframeData LinkMarketplace = new IframeData() { path = "StreamingAssets/form/LinkMarketplace.html" };
    public IframeData MyCoupons = new IframeData() { path = "StreamingAssets/form/MyCoupons.html" };
    public IframeData Expiring = new IframeData() { path = "StreamingAssets/form/Expiring.html" };
    public IframeData Rank = new IframeData() { path = "StreamingAssets/form/Rank.html" };
    public IframeData HowToUploadSlip = new IframeData() { path = "StreamingAssets/form/HowToUploadSlip.html" };
    public IframeData HowToLinkmarketplace = new IframeData() { path = "StreamingAssets/form/HowToLinkmarketplace.html" };
    public IframeData Referal = new IframeData() { path = "StreamingAssets/form/Referal.html" };
    public IframeData DailyCheckIn = new IframeData() { path = "StreamingAssets/form/DailyCheckIn.html" };
*/


    //public string ResPath = "res";
    //public string ResVersion = "1.0";
    //public string CoinPath = "res/icon_point.png";






    [Header("Main")]
    public GameObject Login;
    public GameObject Console;

    [Header("SubPage")]
    public GameObject SettingPage;
    public GameObject EditProfilePage;
    public GameObject GameDetailPage;
    public GameObject InventoryPage;
    public GameObject PointHistoryPage;
    public GameObject RedeemPage;
    public GameObject ShippingPage;
    //public GameObject DailyCheckinPage;
    //public GameObject ReferFriendPage;


    [Header("Object")]
    public GameObject addressObj;
    public GameObject rewardObj;
    public GameObject leaderboardTopspendObj;
    public RecyclingListViewItem leaderboardGameObj;
    public RecyclingListViewItem couponObj;
    public RecyclingListViewItem pointHistoryObj;
    public RecyclingListViewItem shippingObj;





    [Header("Image")]

    public Sprite iconCurrencyImage;
    public Sprite iconRewardPhysical;
    public Sprite iconRewardDigital;
    public Sprite iconRewardGame;
    public Texture iconCompleteImage;
    public Texture iconEditProfile;
    public Texture defaultImage;
    public Texture defaultUserImage;
    public Texture defaultCampaignImage;
    public Texture2D defaultRankImage;
    //public Texture2D[] rankImages;



    [Header("Asset")]
    public List<CustomeThemeSprite> customeThemeSprite;
    public CustomeThemeSprite FindCustomeThemeSprite(string name)
    {
        if (customeThemeSprite == null || customeThemeSprite.Count == 0)
            return null;
        return customeThemeSprite.Find(x => x.name == name);
    }
    public CustomeThemeSprite FindCustomeThemeSprite(CusThemeSpriteType type)
        => FindCustomeThemeSprite(type.ToString());
    public enum CusThemeSpriteType
    {
        consoleBtns,
        profileBtns,
        topspenderHeaderImg,
        bgImg,
        pointIcon,
        topspenderRankImg
    }
    [System.Serializable]
    [UnityEngine.Scripting.Preserve]
    public class CustomeThemeSprite
    {
        public string name;
        public Vector2 slice;
        private List<Sprite> sprites;
        public bool ready { get; private set; }
        public void SetSprites(List<Sprite> sprites)
        {
            this.sprites = sprites;
            this.ready = true;
        }
        public Sprite GetSprite(int index = 0)
        {
            Debug.Log($"Get Sprite : {name}[{index}] === { ((sprites == null) ?  "null" : sprites.Count.ToString()) }");
            if (sprites == null || index < 0 || index >= sprites.Count)
                return null;
            return sprites[index];
        }
    }


    [Header("Sfx")]
    public AudioClip SfxClick;
    public AudioClip SfxSelect;
    public AudioClip SfxOpen;
    public AudioClip SfxClose;
    public AudioClip SfxComplete;
    public AudioClip SfxFail;

    public void OnPlaySfxInApp(string clipName)
    {
        switch (clipName)
        {
            case "SfxClick": SfxClick.Play(); break;
            case "SfxSelect": SfxSelect.Play(); break;
            case "SfxOpen": SfxOpen.Play(); break;
            case "SfxClose": SfxClose.Play(); break;
            case "SfxComplete": SfxComplete.Play(); break;
            case "SfxFail": SfxFail.Play(); break;
        }
    }


    [Header("Override")]
    public UITemplateOverride uiTemplateOverride;
    [System.Serializable]
    [UnityEngine.Scripting.Preserve]
    public class UITemplateOverride
    {
        public GameObject perfabRotateScreen;
        public GameObject perfabLoading;
        public GameObject perfabPopup;
        public GameObject perfabTopMessage;
        public GameObject perfabKeyBoardPanel;
    }

    [Header("Language-Override")]
    public GameStore.Language.LanguageType DefaultLanguage;
    public List<LanguageOverride> Languages;
    [System.Serializable]
    [UnityEngine.Scripting.Preserve]
    public class LanguageOverride
    {
        public GameStore.Language.LanguageType type;
        public TextAsset textAsset;
    }


























    [VInspector.Button()]
    void CheckTheme()
    {

    }




}
