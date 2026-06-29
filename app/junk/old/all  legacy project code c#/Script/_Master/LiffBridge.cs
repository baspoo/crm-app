using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
[UnityEngine.Scripting.Preserve]
public class LiffBridge : MonoBehaviour
{
    public static LiffPayload User = null;
    [Serializable]
    [UnityEngine.Scripting.Preserve]
    public class LiffPayload
    {
        public LiffPayload() { }
        public bool ok;
        public bool inClient;
        public string os;
        public string appid;
        public string language;
        public string userId;
        public string liffid;
        public string displayName;
        public string pictureUrl;
        public string idToken;
        public string error;
        public CRMData.StoreInfo storeInfo;
        public CRMData.InitializeData initializeData;
    }
    public static string signature;


#if UNITY_EDITOR
    public static string EDITOR_LOGIN_USERID;
    [VInspector.Button]
    void OpenLineLogin()
    {
        Application.OpenURL("https://gamestore-gg.1mobystudio.com/crm_platform/linelogin.html?appid=crm_platform&debug=1");
    }
#endif


    public class EventHTTP
    {
        public const int Relogin = 1001;
        public const int LoginStatus = 1002;
        public const int LoginData = 1003;
        public const int ClearLogin = 1004;
        public const int ThemeVersion = 1005;
        public const int LoginDone = 1006;
    }


    public static void GetAppId(System.Action<string> callback)
    {
        GameStore.WebGLService.GetSessionStorage("CRM_APPID", callback);
    }
    public IEnumerator OnGetData()
    {

        //** FOR LOGIN TEST BY AccessToken...
        if (CRMApi.CRMAccessToken.notnull())
        {
            User = new LiffPayload()
            {
                ok = true,
                initializeData = new CRMData.InitializeData()
                {
                    themeId = GameStore.WebGLService.URLInfomation.Params.Find("themeId")
                }
            };
            yield break;
        }


        HtmlCallback.Call(EventHTTP.ThemeVersion, "", (str) =>
        {
            ThemeVersion = str;
            Debug.Log($"ThemeVersion : {ThemeVersion}");
        });

#if UNITY_EDITOR
        //** FOR SPEED LOGIN TEST....
        EDITOR_LOGIN_USERID = DebugTool.GetUserId();
#endif



        // GET User....
        User = null;
        GameStore.WebGLService.GetSessionStorage("LIFF_PAYLOAD", (data) =>
        {
            Debug.Log($"OnGetData : {data}");
            if (data.notnull())
            {
                User = data.DeserializeObjectSimple<LiffPayload>();
            }
            else
            {
                if (GameStore.WebGLService.IsEditor)
                {
#if UNITY_EDITOR
                    if (DebugTool.GetPayload().notnull())
                    {
                        User = DebugTool.GetPayload().DeserializeObjectSimple<LiffPayload>();
                    }
                    else
                    {
                        CRMApi.instance.onGetInfoData((data) =>
                        {
                            if (data != null)
                            {
                                User = data;
                                
                            }
                            else
                            {
                                Manager.mg.OnFailed("LINE_CANT_CONNECT", Relogin);
                            }
                        });
                    }
#endif
                }
                else
                {
                    //"We’re having trouble connecting to LINE. Please try again later."
                    Manager.mg.OnFailed("LINE_CANT_CONNECT", Relogin);
                }
            }
        });
        yield return new WaitWhile(() => User == null);


        // Final Check AppId....
        if (CRMApi.GameId.isnull())
        {
            if (CRMApi.GameId.isnull())
            {
                CRMApi.instance.AssignAppId(User.appid);
            }
            if (CRMApi.GameId.isnull())
            {
                Manager.mg.OnFailed("NOTFOUND_APPID");
                yield return new WaitWhile(() => true);
            }
        }

        /*
        bool getSignature = false;
        signature = null;
        if (GameStore.WebGLService.IsEditor)
        {
            getSignature = true;
        }
        else
        {
            GameStore.WebGLService.GetSessionStorage("CRM_SIGNATURE", (data) =>
            {
                signature = data;
                getSignature = true;
            });
        }

        yield return new WaitWhile(() => !getSignature || User == null);
        */

    }

    public static string ThemeVersion = null;

    public static IEnumerator PublicLogin(System.Action<Dictionary<string, object>> normalLoginCallback, System.Action notFoundCallback)
    {
#if UNITY_EDITOR
        notFoundCallback?.Invoke();
        yield break;
#endif


        var round = 50;
        var loop = true;
        bool ok = false;
        while (loop)
        {
            HtmlCallback.Call(EventHTTP.LoginStatus, "", (str) =>
            {
                if (str == "logged-in")
                {
                    //** Waiting for complete login....
                }
                if (str == "completed")
                {
                    //** Login complete, get data....
                    ok = true;
                    loop = false;
                }
                if (str == "failed" || str.isnull())
                {
                    //** Login failed or no response, stop loop and clear login data....
                    loop = false;
                    HtmlCallback.Call(EventHTTP.ClearLogin);
                }
            });
            yield return new WaitForSeconds(0.1f);
            round--;
            if (round <= 0)
            {
                loop = false;
            }
        }
        yield return new WaitForSeconds(0.1f);
        if (ok)
        {
            // Handle successful login
            HtmlCallback.Call(EventHTTP.LoginData, "", (str) =>
            {
                //** Get login data....
                var data = str.DeserializeObjectSimple<Dictionary<string, object>>();
                normalLoginCallback?.Invoke(data);
            });
        }
        else
        {
            // Handle login failure
            notFoundCallback?.Invoke();
        }
    }

    /*
    public static void LoginDone( string statId )
    {
        GameStore.WebGLService.SetSessionStorage("CRM_STATID", statId);
    }

    public static void SaveRefreshToken(string statId, string refreshToken, string nextAuth)
    {
        var body = $"{statId}|{refreshToken}|{nextAuth}";
        var key = Service.String.UniSimple();
        PlayerPrefs.SetString("S-KEY", key);

        var newSignature = SimpleHashEncrypt.Encrypt(body, key);
        GameStore.WebGLService.SetSessionStorage("CRM_SIGNATURE", newSignature);
    }
    public static void ClearRefreshToken()
    {
        PlayerPrefs.DeleteKey("S-KEY");
        GameStore.WebGLService.RemoveSessionStorage("CRM_APPID");
        GameStore.WebGLService.RemoveSessionStorage("CRM_SIGNATURE");
        GameStore.WebGLService.RemoveSessionStorage("CRM_STATID");
    }
    */

    /*
    public static string GetRedirectLogin
    {
        get
        {
             return PlayerPrefs.GetString($"{CRMApi.GameId}_REDIRECT_LOGIN");
        }
        set
        {
             PlayerPrefs.SetString($"{CRMApi.GameId}_REDIRECT_LOGIN",value);
        }
    }
    */

    /*
    public static (string, string, string) GetRefreshToken()
    {
        if (signature.isnull())
        {
            return (null, null, null);
        }
        else
        {
            var key = PlayerPrefs.GetString("S-KEY");
            var newSignature = SimpleHashEncrypt.Decrypt(signature, key);
            var tokens = newSignature.Split('|');
            return (tokens[0], tokens[1], tokens[2]);
        }
    }
    */

    public static void LoginDone(string urlroot, string gameId, string statId)
    {
        Dictionary<string, object> body = new Dictionary<string, object>();
        body.Add("urlroot", urlroot);
        body.Add("gameId", gameId);
        body.Add("statId", statId);
        if (!CRMData.InitializeData.current.skipIframeThemeColor)
        {
            body.Add("primaryColor", CRMData.InitializeData.current.primaryColor);
            body.Add("secondaryColor", CRMData.InitializeData.current.secondaryColor);
        }
        body.Add("storeName", CRMData.StoreInfo.current.storeName);
        body.Add("logoURL", CRMData.StoreInfo.current.logoURL);
        body.Add("storeCoverURL", CRMData.StoreInfo.current.storeCoverURL);
        body.Add("customAssets", CRMData.InitializeData.current.customAssets);
        body.Add("language", GameStore.Language.GetLanguageName());

        var path = GameStore.WebGLService.Config.GetDict("path");
        body.Add("resPath", path.GetString("resPath"));
        body.Add("path", path);

        HtmlCallback.Call(EventHTTP.LoginDone, body.SerializeToJsonSimple());

    }
    public static void Relogin()
    {
        UIRoot.instance.OnLoading(true);
        HtmlCallback.Call(EventHTTP.Relogin); // Relogin

        /*
        GameStore.WebGLService.GetSessionStorage("CRM_REDIRECTLOGIN", (data) => {
            ClearRefreshToken();
            if (data.notnull())
            {
                GameStore.WebGLService.OpenURL(data, false);
            }
            else
            {
                var uri = GameStore.WebGLService.GetCurrentURL($"linelogin.html?appid={ CRMApi.GameId }");
                GameStore.WebGLService.OpenURL(uri, false);
            }
        });
        */
        //Manager.mg.OnFailed("NOTFOUND_REDIRECT_LOGIN");
    }





}
