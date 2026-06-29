using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;
using System.Threading.Tasks;


public class UIRoot : MonoBehaviour
{
    public static UIRoot instance;

    // [Header("Demo-Control")]
    //public Console demoConcole;
    //public CustomerTheme demoTheme;
    //public string themeId;

    public void Init()
    {
        instance = this;
        ProfileCustomField.Clear();
    }
    public void Inited()
    {
        GameStore.WebGLService.events.onFatalFailed = FatalError;
        Store.IframeData.Init();
    }


    [Space(10)]
    public Camera maincam;
    public Canvas canvas;
    public void StartApp()
    {
        #if UNITY_EDITOR
        var demoConcole = Console.FindAtScene();
        if (demoConcole != null)
        {
            demoConcole.SetActive(true);
            demoConcole.Init();
            return;
        }
        #endif
        Console.Open();
    }


    void FatalError()
    {
        if (UIPopup.instance != null)
        {
            UIPopup.instance.InitBtn(() =>
            {

                LiffBridge.Relogin();

            }, null);
            UIPopup.instance.ChangeBtnName("Ok");
        }
    }


    public void OpenIframe(string pageName,
    Dictionary<string, object> payload = null,
    System.Action<string> callback = null,
    Dictionary<string, object> editor = null)
    {
        var iframe = Store.IframeData.Get(pageName);
        OpenIframe(iframe, payload, callback, editor);
    }
    public void OpenIframe(Store.IframeData iframe,
    Dictionary<string, object> payload = null,
    System.Action<string> callback = null,
    Dictionary<string, object> editor = null)
    {
 
        Debug.Log($"UIRtoon iframe Open : {iframe.path}");
        #if UNITY_EDITOR
        if (payload != null || editor != null)
        {
            if (editor == null && payload != null) editor = payload;
            if (editor != null) EditorGUIService.Popup.OpenEditTextAreaBox("iframe", editor.SerializeToJson(SerializeHandle.FormattingIndented), callback);
        }
        return;
        #endif

        GameStore.WebGLService.OpenIframe(iframe.path, iframe.size[0], iframe.size[1], iframe.unit, payload, (json) =>
        {
            Debug.Log("UIRtoon Open : ${json}");
            if (json.notnull())
            {
                var data = json.ToDictStringObject();
                if (data.ContainsKey("updateApp"))
                {
                    OnLoading(data.GetBool("updateApp"));
                    CRMApi.instance.onGetProfile((ok) =>
                    {
                        OnLoading(false);
                        UIUts.OnUpdateAll();
                        callback?.Invoke(json);
                    });
                    return;
                }
                if (data.ContainsKey("sfxInApp"))
                {
                    CustomerTheme.instance.OnPlaySfxInApp(data.GetString("sfxInApp"));
                }
                callback?.Invoke(json);
            }
            else callback?.Invoke(json);
        });
    }



    public void LoadImage(RawImage image, string url, System.Action<Texture2D> callback = null)
    {
        if (image == null)
            return;

        LoadImage(url, (tex) =>
        {
            if (tex != null) image.texture = tex;
            callback?.Invoke(tex);
        });
    }
    public void LoadImage(string url, System.Action<Texture2D> callback = null)
    {
        if (url.isnull())
        {
            callback?.Invoke(null);
            return;
        }

        // check memory cache...
        var tex = GameStore.WebGLService.Download.OnLoadImageCache(url);
        if (tex != null)
        {
            callback?.Invoke(tex);
        }

        // check local storage...
        tex = LocalImageCache.Load(url);
        if (tex != null)
        {
            GameStore.WebGLService.Download.OnSaveImageCache(url, tex);
            callback?.Invoke(tex);
        }
        else
        {
            // load from web...
            GameStore.WebGLService.Download.OnLoadImage(url, (loadImage) =>
            {
                if (loadImage != null)
                {
                    // save to local storage...
                    LocalImageCache.Save(loadImage, url);
                }
                callback?.Invoke(loadImage);
            }, false);
        }
    }


    public void OnDownloadAssetsBundle(string bundleName, string assetsName, string version, System.Action<bool, string, GameObject> callback)
    {
        GameStore.Core.AssetsBundleHandle.Init();
        if (version.notnull())
        {
            OnDownloadAssetsBundleFile(bundleName, assetsName, version, callback);
        }
        else
        {
            GameStore.WebGLService.Streaming.OnLoadText($"AssetsBundle/version/{bundleName}.txt", (version) =>
            {
                //** get version
                if (version.isnull())
                {
                    callback?.Invoke(false, "BUNDLE_VERSION_NOTFOUND", null);
                }
                else
                {
                    OnDownloadAssetsBundleFile(bundleName, assetsName, version, callback);
                }
            });
        }

    }
    void OnDownloadAssetsBundleFile(string bundleName, string assetsName, string version, System.Action<bool, string, GameObject> callback)
    {
        GameStore.Core.AssetsBundleHandle.OnFullLoadAsync(bundleName, assetsName, GameStore.Core.AssetsBundleHandle.FileType.prefab, (page) =>
        {
            //** get bundle
            if (page == null)
            {
                callback?.Invoke(false, "BUNDLE_FILE_FAILED", null);
            }
            else
            {
                callback?.Invoke(true, null, (GameObject)page);
            }

        }, version);
    }



    public void OnLoading(bool active)
    {
        GameStore.WebGLService.ScreenHandle.OnLoadingScreen(active);
    }
    public float minLoadingTime = 0.5f;
    bool yield = false;
    bool finishYield = false;
    bool yieldShowloading;
    float timimgYield;
    System.Action loadingYield;
    public void OnBeginLoadingYield(bool Showloading = true)
    {
        yield = true;
        yieldShowloading = Showloading;
        finishYield = false;
        timimgYield = 0;
        loadingYield = null;
        if (yieldShowloading)
            OnLoading(true);
    }
    public void OnEndLoadingYield(System.Action loadingYield)
    {
        this.loadingYield = loadingYield;
        finishYield = true;
        if (timimgYield == 0.0f)
        {
            EndYield();
        }
    }
    void ActiveYield()
    {
        if (yield)
        {
            if (timimgYield < minLoadingTime)
            {
                timimgYield += Time.deltaTime;
            }
            else
            {
                if (finishYield)
                {
                    EndYield();
                }
            }
        }
    }
    void EndYield()
    {
        yield = false;
        finishYield = false;
        timimgYield = 0;
        loadingYield?.Invoke();
        loadingYield = null;
        if (yieldShowloading)
            OnLoading(false);
    }
    private void Update()
    {
        ActiveYield();
    }







    public void OpenExtarnalLink(string url)
    {
        CustomerTheme.instance.SfxOpen.Play();
        GameStore.WebGLService.OpenExLink(url);

    }

}
