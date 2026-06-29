using GameStore.Core.Behaviour;
using System.Collections;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using static CRMApi;
using static ParseAPI;
using GameStore.Core;
using GameStore;


public class CRMApi : MonoBehaviour
{
    public static CRMApi instance;
    public string URLCRMROOT = "https://be-stg-web.mookept.com/"; //"https://portal.mookept.com/";
    public static string UID;
    public static string idToken;
    public static string GameId;
    public static string CRMAccessToken { get; private set; }



#if UNITY_EDITOR
    private const string EDITORKEY = "e52f861c8b3d4a9e70125c6f81a3d4b79e0f2142c6a8b0d3e5f7192a34bc6e8d90f12a34b5c6d7e8f9a0b1c2d3e4f5a6b7c8d9e0f1a2b3c4d5e6f7a8b9c0d1e2";
#endif








    public void Init()
    {
        instance = this;

        // www.gamesore.com/crm_platform/? appid=1slim & uid=uuxxxxxxxxxxxxxxx
        GameStore.Core.GameBundle.instance.gameConfigId = null;
        GameStore.WebGLService.URLInfomationTigger = (url) =>
        {
            if (url.Params.ContainsKey("accessToken"))
            {
                CRMAccessToken = url.Params["accessToken"];
            }

            //** change gameId
            if (url.Params.ContainsKey("appid"))
            {
                AssignAppId(url.Params["appid"]);
            }
            else
            {
#if UNITY_EDITOR
                AssignAppId(DebugTool.GetGameId());
#else
                    LiffBridge.GetAppId(AssignAppId);
#endif

            }
        };
    }
    public void Inited()
    {
        UID = LiffBridge.User.userId;
        idToken = LiffBridge.User.idToken;
    }
    public void AssignAppId(string appId)
    {
        GameId = appId;
        GameStore.Core.GameBundle.instance.gameConfigId = appId;
    }







    // [ Auth ]
#if UNITY_EDITOR
    [VInspector.Button]
    void GetAccessToken()
    {
        var payload = new Dictionary<string, object>();
        payload.Add("EDITORKEY", EDITORKEY);
        CloudFunction("crm_getEditorAuth", payload, (ok, result) =>
        {
            if (ok && result.ok)
            {
                EditorGUIService.Popup.OpenEditTextAreaBox("Token", result.data.SerializeToJson(SerializeHandle.FormattingIndented));
            }
        });
    }
#endif

    [UnityEngine.Scripting.Preserve]
    [System.Serializable]
    public class AuthResult
    {
        [UnityEngine.Scripting.Preserve]
        public class ActionType
        {
            public const string OPEN_OTP_PIN = "OPEN_OTP_PIN";
            public const string OPEN_REGISTER = "OPEN_REGISTER";
            public const string INVALID = "INVALID";
            public const string LOGIN_DONE = "LOGIN_DONE";
            public const string VERIFY_DONE = "VERIFY_DONE";
            public const string EXPIRE = "EXPIRE";

        }
        [UnityEngine.Scripting.Preserve]
        public class ErrorType
        {
            public const string LOGIN_FAILED = "LOGIN_FAILED";
            public const string LINE_ID_EXISTS = "LINE_ID_EXISTS";
            public const string PHONE_ALREADY_LINKED = "PHONE_ALREADY_LINKED";
            public const string EXPIRE = "EXPIRE";
        }
        [UnityEngine.Scripting.Preserve]
        public class SessionKey
        {
            public const string LOGIN_FAILED = "LOGIN_FAILED";
            public const string LINE_ID_EXISTS = "LINE_ID_EXISTS";
            public const string PHONE_ALREADY_LINKED = "PHONE_ALREADY_LINKED";
        }
        public string action;

        //public string refreshToken;
        //public string nextAuth;
        public string error;
        public string lineUserId;
        public string verificationToken;
        public string refNo;
        public string existingPhone;
        public string phone;
        public long expiresIn;
        public string redirectlogin;

    }
    public void onAuth(string phone, System.Action<AuthResult> callack = null)
    {

#if UNITY_EDITOR
        if (LiffBridge.EDITOR_LOGIN_USERID.notnull())
        {
            onEditorLogin(callack);
            return;
        }
#endif

        if (CRMAccessToken.notnull())
        {
            onAccessToken(callack);
            return;
        }


        /*
        if (LiffBridge.signature.notnull())
        {
            onReauth(callack);
            return;
        }
        */


        StartCoroutine(LiffBridge.PublicLogin((result) =>
        {

            //** Public Login
            onAfterLogin(result, callack);

        }, () =>
        {


            //** Classic Login
            var payload = new Dictionary<string, object>();
            payload.Add("uid", UID);
            payload.Add("idToken", idToken);
            payload.Add("gameId", GameId);
            payload.AddIfExists("phone", phone);
            CloudFunctionWithoutEncryption("crm_auth", payload, (ok, result) =>
            {
                Debug.Log($"onAuth {ok} --> {result.ok}");
                if (ok && result.ok)
                {
                    onAfterLogin(result.data, callack);
                }
                else
                {
                    callack?.Invoke(null);
                }
            });

        }));



    }



    void onAfterLogin(Dictionary<string, object> data, System.Action<AuthResult> callack)
    {
        var authResult = data.DeserializeObjectSimple<AuthResult>();
        Debug.Log($"onAfterLogin {authResult.action}");
        if (authResult.action == CRMApi.AuthResult.ActionType.LOGIN_DONE)
        {

            var unixCache = data.Find("unixCache").ToStr();
            //** create crm user... done
            CRMData.User = data["crmUser"].DeserializeObjectSimple<CRMData.UserData>();
            CRMData.DailyData.current = data["dailyData"].DeserializeObjectSimple<CRMData.DailyData>();

            //** login gamification... done
            GameStore.WebGLService.NetworkService.OnExternalLogin(data, (user) =>
            {

                if (user != null)
                {
                    //stamp login done... for fast login...
                    LiffBridge.LoginDone(
                        GameStore.WebGLService.NetworkService.serverURL,
                        GameId,
                        GameStore.WebGLService.STATID
                        );


                    //** get marketData
                    onGetMaketData(unixCache, marketData =>
                    {
                        Debug.Log(marketData != null);
                        callack?.Invoke(marketData != null ? authResult : null);
                    });
                }

            });
        }
        else
        {
            callack?.Invoke(authResult);
        }
    }


    public void onAccessToken(System.Action<AuthResult> callack = null)
    {
        var payload = new Dictionary<string, object>();
        payload.Add("gameId", GameId);
        payload.AddIfExists("accessToken", CRMAccessToken);
        CloudFunctionWithoutEncryption("crm_tokenSign", payload, (ok, result) =>
        {
            if (ok && result.ok)
            {
                onAfterLogin(result.data, callack);
            }
            else
            {
                callack?.Invoke(new AuthResult()
                {
                    action = CRMApi.AuthResult.ActionType.EXPIRE
                });
            }
        });
    }

    #if UNITY_EDITOR
    public void onEditorLogin(System.Action<AuthResult> callack = null)
    {
        //** FOR SPEED TEST....
        var payload = new Dictionary<string, object>();
        payload.Add("gameId", GameId);
        payload.Add("uid", LiffBridge.EDITOR_LOGIN_USERID);
        payload.Add("EDITORKEY", EDITORKEY);
        CloudFunctionWithoutEncryption("crm_login_editor", payload, (ok, result) =>
        {
            if (ok && result.ok)
            {
                onAfterLogin(result.data, callack);
            }
            else
            {
                if (result.code == 30004)
                {
                    callack?.Invoke(new AuthResult()
                    {
                        action = "PERMISSION_DENIED"
                    });
                }
                else
                {
                    callack?.Invoke(new AuthResult()
                    {
                        action = CRMApi.AuthResult.ActionType.EXPIRE
                    });
                }

            }
        });
    }
    #endif
    public void onOTPVerify(string verificationToken, string pin, System.Action<bool> callack = null)
    {
        var payload = new Dictionary<string, object>();
        payload.Add("verificationToken", verificationToken);
        payload.Add("pin", pin);
        payload.Add("gameId", GameId);
        CloudFunctionWithoutEncryption("crm_verify", payload, (ok, result) =>
        {
            if (ok && result.ok)
            {
                var action = result.data.GetValue<string>("action");
                callack?.Invoke(action == AuthResult.ActionType.VERIFY_DONE);
            }
            else
            {
                callack?.Invoke(false);
            }
        });
    }























    // [ Profile ]
#if UNITY_EDITOR
    [VInspector.Button]
    void ViewGameUser()
    {
        EditorGUIService.Popup.OpenEditTextAreaBox("Game-User", GameStore.WebGLService.User.SerializeToJson(SerializeHandle.FormattingIndented));
    }
    [VInspector.Button]
    void ViewCRMUser()
    {
        EditorGUIService.Popup.OpenEditTextAreaBox("CRM-User", CRMData.User.SerializeToJson(SerializeHandle.FormattingIndented));
    }
    [VInspector.Button]
    void ViewMarket()
    {
        EditorGUIService.Popup.OpenEditTextAreaBox("CRM-Market", CRMData.MarketData.current.SerializeToJson(SerializeHandle.FormattingIndented));
    }
#endif

    [VInspector.Button]
    void GetProfile() => onGetProfile();
    public void onGetProfile(System.Action<bool> callack = null)
    {
        var payload = new Dictionary<string, object>();
        CloudFunction("crm_getuser", payload, (ok, result) =>
        {
            if (ok && result.ok)
            {
                CRMData.User = result.data["crmUser"].DeserializeObjectSimple<CRMData.UserData>();
                callack?.Invoke(true);
            }
            else
            {
                callack?.Invoke(false);
            }
        });
    }
    [VInspector.Button]
    void UpdateProfile()
    {
        var profile = new Dictionary<string, object>();
        //profile.Add("displayName", "baspoo");
        // profile.Add("pictureUrl", "url");
        // profile.Add("phone", $"{Random.Range(1111111111, 9999999999)}");
        profile.Add("email", $"myemail_{99999.Random()}@1moby.com");
        onUpdateProfile(profile);
    }



    public class ProfileFields
    {
        public const string email = "email";
        public const string customFields = "customFields";
        public const string displayName = "displayName";
        public const string pictureUrl = "pictureUrl";
    }
    [System.Serializable]
    public class CustomFields
    {
        public const string gender = "gender";
        public const string birthday = "birthday";
        public const string consent_notif = "consentnotif";
    }
    public void onUpdateProfileCustomFields(Dictionary<string, object> customFields, System.Action<bool> callack = null)
    {
        var profile = new Dictionary<string, object>();
        profile.Add(ProfileFields.customFields, customFields);
        onUpdateProfile(profile, callack);
    }
    public void onUpdateProfile(Dictionary<string, object> profile, System.Action<bool> callack = null)
    {
        var payload = new Dictionary<string, object>();
        payload.Add("profile", profile);
        CloudFunction("crm_updateProfile", payload, (ok, result) =>
        {
            //Debug.Log("crm_updateProfile BACK");
            if (ok && result.ok)
            {
                Debug.Log("crm_updateProfile OK");
                CRMData.User = result.data["crmUser"].DeserializeObjectSimple<CRMData.UserData>();

                Debug.Log(result.data["crmUser"].SerializeToJsonSimple());
                Debug.Log(CRMData.User.SerializeToJsonSimple());

                callack?.Invoke(true);
            }
            else
            {
                callack?.Invoke(false);
            }
        });
    }







    // [ Point ]
#if UNITY_EDITOR
    [VInspector.Button]
    void GetEarnedPoints()
    {
        onGetEarnedPoints(20, 0, (datas, more) =>
        {
            if (datas != null)
            {
                EditorGUIService.Popup.OpenEditTextAreaBox("GetEarnedPoints", datas.SerializeToJson(SerializeHandle.FormattingIndented));
            }
        });
    }
    [VInspector.Button]
    void GetDeductPoints()
    {
        onGetDeductPoints(20, 0, (datas, more) =>
        {
            if (datas != null)
            {
                EditorGUIService.Popup.OpenEditTextAreaBox("GetDeductPoints", datas.SerializeToJson(SerializeHandle.FormattingIndented));
            }
        });
    }
    [VInspector.Button]
    void CreatePoints()
    {
        var payload = new Dictionary<string, object>();
        payload.Add("channel", "editor");
        payload.Add("items", new[] {
            new{
                name = "Product A",
                unitPrice = 150.00,
                quantity = 1
            },
            new {
                name = "Product B",
                unitPrice = 80.75,
                quantity = 1
            }
        });
        payload.Add("description", "API test order");
        payload.Add("tags", new string[2] { "new-year", "promotion" });

        EditorGUIService.Popup.OpenEditTextAreaBox("CreatePoints", payload.SerializeToJson(SerializeHandle.FormattingIndented), (json) =>
        {
            payload = json.DeserializeObjectSimple<Dictionary<string, object>>();
            onCreatePoints(payload, (ok) =>
            {

            });
        });
    }
#endif
    public void onGetEarnedPoints(int limit = 20, int offset = 0, System.Action<List<CRMData.EarnedPointsTransactionData>, bool> callack = null)
    {
        var payload = new Dictionary<string, object>();
        payload.Add("limit", limit);
        payload.Add("offset", offset);
        CloudFunction("crm_getEarnedPoints", payload, (ok, result) =>
        {
            if (ok && result.ok)
            {
                var data = result.data["transactions"].DeserializeObjectSimple<List<CRMData.EarnedPointsTransactionData>>();
                if (limit > 1) CRMData.EarnedPointsTransactionData.current = data;
                callack?.Invoke(data, data.Count == limit);
            }
            else
            {
                callack?.Invoke(null, false);
            }
        });
    }
    public void onGetDeductPoints(int limit = 20, int offset = 0, System.Action<List<CRMData.DeductionHistoryTransactionData>, bool> callack = null)
    {
        var payload = new Dictionary<string, object>();
        payload.Add("limit", limit);
        payload.Add("offset", offset);
        CloudFunction("crm_getDeductPoints", payload, (ok, result) =>
        {
            if (ok && result.ok)
            {
                var data = result.data["deduction_history"].DeserializeObjectSimple<List<CRMData.DeductionHistoryTransactionData>>();
                CRMData.DeductionHistoryTransactionData.current = data;
                callack?.Invoke(data, data.Count == limit);
            }
            else
            {
                callack?.Invoke(null, false);
            }
        });
    }
    public void onCreatePoints(Dictionary<string, object> payload, System.Action<bool> callack = null)
    {
        //var payload = new Dictionary<string, object>();
        CloudFunction("crm_createPoints", payload, (ok, result) =>
        {
            if (ok && result.ok)
            {
                var data = result.data["order"].ToDictStringObject();
                Debug.Log(data.SerializeToJsonSimple());
                callack?.Invoke(true);
            }
            else
            {
                callack?.Invoke(false);
            }
        });
    }










    // [ OCR ]
    public void onGetOCRstatus(string jobId, System.Action<CRMData.OCRStatusData> callack = null)
    {
        var payload = new Dictionary<string, object>();
        if (jobId.notnull())
            payload.Add("jobId", jobId);
        CloudFunction("crm_getOCRstatus", payload, (ok, result) =>
        {
            if (ok && result.ok)
            {
                var data = result.data["job"].DeserializeObjectSimple<CRMData.OCRStatusData>();
                callack?.Invoke(data);
            }
            else
            {
                callack?.Invoke(null);
            }
        });
    }
    public void onGetOCRstatus(System.Action<List<CRMData.OCRStatusData>> callack = null)
    {
        var payload = new Dictionary<string, object>();
        CloudFunction("crm_getOCRstatus", payload, (ok, result) =>
        {
            if (ok && result.ok)
            {
                var data = result.data["jobs"].DeserializeObjectSimple<List<CRMData.OCRStatusData>>();
                callack?.Invoke(data);
            }
            else
            {
                callack?.Invoke(null);
            }
        });
    }











    // [ Coupon ]
#if UNITY_EDITOR
    [VInspector.Button]
    void GetMyCoupon()
    {
        var payload = new Dictionary<string, object>();
        payload.Add("couponCode", "");
        EditorGUIService.Popup.OpenEditTextAreaBox("GetMyCoupon", payload.SerializeToJson(SerializeHandle.FormattingIndented), (json) =>
        {
            payload = json.DeserializeObjectSimple<Dictionary<string, object>>();
            var couponCode = payload.Find("couponCode").ToStr();
            if (couponCode.notnull())
            {
                onGetMyCoupon(couponCode, (datas) => { });
            }
            else
            {
                onGetMyCoupons(20, 0, (datas, more) => { });
            }
        });
    }
#endif
    public void onGetMyCoupon(string couponCode, System.Action<CRMData.CouponData> callack = null)
    {
        if (couponCode.isnull())
        {
            callack?.Invoke(null);
            return;
        }
        var payload = new Dictionary<string, object>();
        payload.Add("couponCode", couponCode);
        CloudFunction("crm_getMyCoupons", payload, (ok, result) =>
        {
            if (ok && result.ok)
            {
                var data = result.data.DeserializeObjectSimple<CRMData.CouponData>();
                callack?.Invoke(data);
            }
            else
            {
                callack?.Invoke(null);
            }
        });
    }
    public void onGetMyCoupons(int limit = 20, int offset = 0, System.Action<List<CRMData.CouponData>, bool> callack = null)
    {
        var payload = new Dictionary<string, object>();
        payload.Add("limit", limit);
        payload.Add("offset", offset);
        CloudFunction("crm_getMyCoupons", payload, (ok, result) =>
        {
            if (ok && result.ok)
            {
                var data = result.data["coupons"].DeserializeObjectSimple<List<CRMData.CouponData>>();
                callack?.Invoke(data, data.Count == limit);
            }
            else
            {
                callack?.Invoke(null, false);
            }
        });
    }









    // [ Reward ]
#if UNITY_EDITOR
    [VInspector.Button]
    void GetRewards()
    {
        onGetRewards((datas) =>
        {
            if (datas != null)
            {
                EditorGUIService.Popup.OpenEditTextAreaBox("GetRewards", datas.SerializeToJson(SerializeHandle.FormattingIndented));
            }
        });
    }
    [VInspector.Button]
    void GetShippingOrder()
    {
        onGetShippingOrder(20, 0, (datas, more) =>
        {
            if (datas != null)
            {
                EditorGUIService.Popup.OpenEditTextAreaBox("GetShippingOrder", datas.SerializeToJson(SerializeHandle.FormattingIndented));
            }
        });
    }
    [VInspector.Button]
    void RedeemReward()
    {
        var payload = new Dictionary<string, object>();
        payload.Add("rewardId", 0);
        payload.Add("quantity", 0);
        payload.Add("addressId", 0);
        EditorGUIService.Popup.OpenEditTextAreaBox("RedeemReward", payload.SerializeToJson(SerializeHandle.FormattingIndented), (json) =>
        {
            payload = json.DeserializeObjectSimple<Dictionary<string, object>>();
            onRedeemReward(payload["rewardId"].ToInt(), payload["quantity"].ToInt(), payload["addressId"].ToInt(), (datas, rewardId) =>
            {

            });
        });
    }
#endif
    public void onGetRewards(System.Action<List<CRMData.MarketReward>> callack = null)
    {
        var payload = new Dictionary<string, object>();
        CloudFunction("crm_getRewards", payload, (ok, result) =>
        {
            if (ok && result.ok)
            {
                var data = result.data["rewards"].DeserializeObjectSimple<List<CRMData.MarketReward>>();
                CRMData.MarketReward.current = data;
                CRMData.MarketData.current.rewards = data;
                callack?.Invoke(data);
            }
            else
            {
                callack?.Invoke(null);
            }
        });
    }
    public void onGetReward(int id, System.Action<CRMData.MarketReward> callack = null)
    {
        var payload = new Dictionary<string, object>();
        payload.Add("id", id);
        CloudFunction("crm_getReward", payload, (ok, result) =>
        {
            if (ok && result.ok)
            {
                var data = result.data["reward"].DeserializeObjectSimple<CRMData.MarketReward>();
                var index = CRMData.MarketData.current.rewards.FindIndex(x => x.id == id);
                if (index != -1)
                {
                    CRMData.MarketData.current.rewards[index] = data;
                }
                else
                {
                    CRMData.MarketData.current.rewards.Add(data);
                }
                callack?.Invoke(data);
            }
            else
            {
                callack?.Invoke(null);
            }
        });
    }
    public void onGetShippingOrder(int limit = 20, int offset = 0, System.Action<List<CRMData.ShippingOrderData>, bool> callack = null)
    {
        var payload = new Dictionary<string, object>();
        payload.Add("limit", limit);
        payload.Add("offset", offset);
        CloudFunction("crm_getShippingRewards", payload, (ok, result) =>
        {
            if (ok && result.ok)
            {
                var data = result.data["orders"].DeserializeObjectSimple<List<CRMData.ShippingOrderData>>();
                CRMData.ShippingOrderData.current = data;
                callack?.Invoke(data, data.Count == limit);
            }
            else
            {
                callack?.Invoke(null, false);
            }
        });
    }
    public void onRedeemReward(int rewardId, int quantity, int? addressId, System.Action<bool, string> callack = null)
    {
        var payload = new Dictionary<string, object>();
        payload.Add("rewardId", rewardId);
        payload.Add("quantity", quantity);
        if (addressId != null && addressId.Value >= 0)
            payload.Add("addressId", addressId.Value);
        CloudFunction("crm_redeemReward", payload, (ok, result) =>
        {
            if (ok && result.ok)
            {
                PatchingReward(result.data);

                var rewardId = "";
                callack?.Invoke(true, rewardId);
            }
            else
            {
                callack?.Invoke(false, result.message);
            }
        });
    }
    void PatchingReward(Dictionary<string, object> data)
    {
        //** Point Remaining
        data.Modify("points", val =>
        {
            Debug.Log($"Point Remaining: {val}");
            CRMData.User.user.points = val.ToInt();
        });

        //** Patching user
        if (data.ContainsKey("game"))
        {
            var game = data["game"].ToDictStringObject();
            game.Modify("currenct", val =>
            {
                GameStore.WebGLService.NetworkService.UpdateLocalData(currency: val.ToInt());
            });
            game.Modify("inventory", val =>
            {
                var items = val.DeserializeObjectSimple<Dictionary<string, int>>();
                GameStore.WebGLService.NetworkService.UpdateLocalData(items: items);
            });
            game.Modify("customProperties", val =>
            {
                var customProperties = val.ToDictStringObject();
                GameStore.WebGLService.NetworkService.UpdateLocalData(customProperties: customProperties);
            });
        }

    }






#if UNITY_EDITOR
    [VInspector.Button]
    void GetBanners()
    {
        onGetBanners((datas) =>
        {
            if (datas != null)
            {
                EditorGUIService.Popup.OpenEditTextAreaBox("GetBanners", datas.SerializeToJson(SerializeHandle.FormattingIndented));
            }
        });
    }
    [VInspector.Button]
    void GetTiers()
    {
        onGetTiers((datas) =>
        {
            if (datas != null)
            {
                EditorGUIService.Popup.OpenEditTextAreaBox("GetTiers", datas.SerializeToJson(SerializeHandle.FormattingIndented));
            }
        });
    }
    [VInspector.Button]
    void GetLeaderboard()
    {
        onGetLeaderboard((datas) =>
        {
            if (datas != null)
            {
                EditorGUIService.Popup.OpenEditTextAreaBox("GetLeaderboard", datas.SerializeToJson(SerializeHandle.FormattingIndented));
            }
        });
    }
    [VInspector.Button]
    void GetMarketData()
    {
        onGetMaketData(null, (datas) =>
        {
            if (datas != null)
            {
                EditorGUIService.Popup.OpenEditTextAreaBox("GetMarketData", datas.SerializeToJson(SerializeHandle.FormattingIndented));
            }
        });
    }
#endif

 
       public void onGetInfoData(System.Action<LiffBridge.LiffPayload> callack = null)
    {
        //** FOR SPEED TEST....
        var payload = new Dictionary<string, object>();
        payload.Add("gameId", GameId);
        CloudFunctionWithoutEncryption("crm_getpublicinfo", payload, (ok, result) =>
        {
            if (ok && result.ok)
            {
                var data = result.data.DeserializeObjectSimple<LiffBridge.LiffPayload>();
                callack?.Invoke(data); 
            }
            else callack?.Invoke(null);
        });
    }


    // [ Banner ]
    public void onGetBanners(System.Action<CRMData.BannerData> callack = null)
    {
        var payload = new Dictionary<string, object>();
        CloudFunction("crm_getBanners", payload, (ok, result) =>
        {
            if (ok && result.ok)
            {
                var data = result.data["banners"].DeserializeObjectSimple<CRMData.BannerData>();
                CRMData.BannerData.current = data;
                CRMData.MarketData.current.banners = data;
                callack?.Invoke(data);
            }
            else
            {
                callack?.Invoke(null);
            }
        });
    }


    // [ Tiers ]
    public void onGetTiers(System.Action<List<CRMData.TierData>> callack = null)
    {
        var payload = new Dictionary<string, object>();
        CloudFunction("crm_getTiers", payload, (ok, result) =>
        {
            if (ok && result.ok)
            {
                var data = result.data["tiers"].DeserializeObjectSimple<List<CRMData.TierData>>();
                CRMData.TierData.current = data;
                CRMData.MarketData.current.tiers = data;
                callack?.Invoke(data);
            }
            else
            {
                callack?.Invoke(null);
            }
        });
    }


    // [ Leaderboard ]
    public void onGetLeaderboard(System.Action<CRMData.LeaderboardData> callack = null)
    {
        var payload = new Dictionary<string, object>();
        CloudFunction("crm_getLeaderboard", payload, (ok, result) =>
        {
            if (ok && result.ok)
            {
                var data = result.data["leaderboard"].DeserializeObjectSimple<CRMData.LeaderboardData>();
                CRMData.LeaderboardData.current = data;
                callack?.Invoke(data);
            }
            else
            {
                callack?.Invoke(null);
            }
        });
    }


    // [ MarketData ]
    Service.Permanence permanenceUnixCache = new Service.Permanence("unixCache");
    Service.Permanence permanenceMarketData = new Service.Permanence("marketData");
    public void onGetMaketData(string unixCache, System.Action<CRMData.MarketData> callack = null)
    {

        //** cache data
        var localUnixCache = permanenceUnixCache.getString;
        var localMarketData = permanenceMarketData.getString;
        if (localUnixCache.notnull() && unixCache.notnull() && localMarketData.notnull() && localUnixCache == unixCache)
        {
            Debug.Log($"used marketdata cache... {unixCache}");
            var json = permanenceMarketData.getString;
            var data = json.DeserializeObjectSimple<CRMData.MarketData>();
            SetupMarket(data);
            Debug.Log(json);

            callack?.Invoke(data);
            return;
        }


        var payload = new Dictionary<string, object>();
        CloudFunction("crm_getMarketData", payload, (ok, result) =>
        {
            if (ok && result.ok)
            {
                Debug.Log($"used marketdata new download... {unixCache}");
                var data = result.data.DeserializeObjectSimple<CRMData.MarketData>();
                LocalImageCache.ClearAll();
                permanenceUnixCache.getString = unixCache;
                permanenceMarketData.getString = data.SerializeToJsonSimple(); // cache data...
                SetupMarket(data);
                callack?.Invoke(data);
            }
            else
            {
                callack?.Invoke(null);
            }
        });
    }
    void SetupMarket(CRMData.MarketData data)
    {
        CRMData.MarketData.current = data;
        CRMData.StoreSetting.current = data.setting;
        CRMData.MarketReward.current = data.rewards;
        CRMData.BannerData.current = data.banners;
        CRMData.TierData.current = data.tiers;
    }












    // [ Address ]
#if UNITY_EDITOR
    [VInspector.Button]
    void GetAddress()
    {
        onGetAddress((datas) =>
        {
            if (datas != null)
            {
                EditorGUIService.Popup.OpenEditTextAreaBox("GetAddress", datas.SerializeToJson(SerializeHandle.FormattingIndented));
            }
        });
    }
    [VInspector.Button]
    void CreateAddress()
    {
        var address = CRMData.UserData.UserAddressesData.DefaultDemo();
        EditorGUIService.Popup.OpenEditTextAreaBox("CreateAddress", address.SerializeToJson(SerializeHandle.FormattingIndented), (json) =>
        {
            address = json.DeserializeObjectSimple<CRMData.UserData.UserAddressesData>();
            onCreateAddress(address, (datas) =>
            {

            });
        });
    }
    [VInspector.Button]
    void UpdateAddress()
    {
        CRMData.UserData.UserAddressesData address = new CRMData.UserData.UserAddressesData();
        EditorGUIService.Popup.OpenEditTextAreaBox("UpdateAddress", address.SerializeToJson(SerializeHandle.FormattingIndented), (json) =>
        {
            var payload = json.DeserializeObjectSimple<Dictionary<string, object>>();
            payload = payload.Where(kvp => kvp.Value != null).ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
            address = payload.DeserializeObjectSimple<CRMData.UserData.UserAddressesData>();
            onUpdateAddress(address, (datas) =>
            {

            });
        });
    }
    [VInspector.Button]
    void SetDefaultAddress()
    {
        EditorGUIService.Popup.OpenEditTextAreaBox("SetDefaultAddress", "0", (json) =>
        {
            onSetDefaultAddress(json.ToInt(), (datas) =>
            {

            });
        });
    }
    [VInspector.Button]
    void RemoveAddress()
    {
        EditorGUIService.Popup.OpenEditTextAreaBox("RemoveAddress", "0", (json) =>
        {
            onRemoveAddress(json.ToInt(), (datas) =>
            {

            });
        });
    }
#endif
    public void onCreateAddress(CRMData.UserData.UserAddressesData address, System.Action<bool> callack = null)
    {
        var payload = address.DeserializeObjectSimple<Dictionary<string, object>>();
        payload.Remove("id");
        payload = payload.Where(kvp => kvp.Value != null).ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
        CloudFunction("crm_createAddresses", payload, (ok, result) =>
        {
            if (ok && result.ok)
            {
                onGetAddress((addressList) =>
                {
                    callack?.Invoke(true);
                });
            }
            else
            {
                callack?.Invoke(false);
            }
        });
    }
    public void onUpdateAddress(CRMData.UserData.UserAddressesData address, System.Action<bool> callack = null)
    {
        var payload = address.DeserializeObjectSimple<Dictionary<string, object>>();
        payload = payload.Where(kvp => kvp.Value != null).ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
        CloudFunction("crm_updateAddresses", payload, (ok, result) =>
        {
            if (ok && result.ok)
            {
                onGetAddress((addressList) =>
                {
                    callack?.Invoke(true);
                });
            }
            else
            {
                callack?.Invoke(false);
            }
        });
    }
    public void onSetDefaultAddress(int id, System.Action<bool> callack = null)
    {
        var payload = new Dictionary<string, object>();
        payload["id"] = id;
        payload["isDefault"] = true;
        CloudFunction("crm_updateAddresses", payload, (ok, result) =>
        {
            if (ok && result.ok)
            {
                onGetAddress((addressList) =>
                {
                    callack?.Invoke(true);
                });
            }
            else
            {
                callack?.Invoke(false);
            }
        });
    }
    public void onGetAddress(System.Action<List<CRMData.UserData.UserAddressesData>> callack = null)
    {
        var payload = new Dictionary<string, object>();
        CloudFunction("crm_getAddresses", payload, (ok, result) =>
        {
            if (ok && result.ok)
            {
                var data = result.data["address"].DeserializeObjectSimple<List<CRMData.UserData.UserAddressesData>>();
                CRMData.User.userAddresses = data;
                callack?.Invoke(data);
            }
            else
            {
                callack?.Invoke(null);
            }
        });
    }
    public void onRemoveAddress(int removeId, System.Action<bool> callack = null)
    {
        var payload = new Dictionary<string, object>();
        payload["id"] = removeId;
        CloudFunction("crm_removeAddresses", payload, (ok, result) =>
        {
            if (ok && result.ok)
            {
                onGetAddress((addressList) =>
                {
                    callack?.Invoke(true);
                });
            }
            else
            {
                callack?.Invoke(false);
            }
        });
    }




    // [ Link Marketplace ]
#if UNITY_EDITOR
    [VInspector.Button]
    void Linkmarketplace()
    {
        var payload = new Dictionary<string, object>();
        payload.Add("marketplace", 0);
        payload.Add("order_id", "xxxxxx");
        payload.Add("zip_code", "xxxxxx");
        payload.Add("confirm", false);
        EditorGUIService.Popup.OpenEditTextAreaBox("Link-marketplace", payload.SerializeToJson(SerializeHandle.FormattingIndented), (json) =>
        {
            payload = json.DeserializeObjectSimple<Dictionary<string, object>>();
            onLinkmarketplace(
                (marketplace)payload["marketplace"].ToInt(),
                payload["order_id"].ToString(),
                payload["zip_code"].ToString(),
                (ok, datas) =>
                {

                });
        });
    }
#endif
    public enum marketplace
    {
        shopee, lazada, tiktok
    }
    public void onLinkmarketplace(marketplace marketplace, string order_id, string zip_code, System.Action<bool, string> callack = null)
    {
        var payload = new Dictionary<string, object>();
        payload["marketplace"] = marketplace.ToString();
        payload["order_id"] = order_id;
        payload["zip_code"] = zip_code;
        //if (confirm)
        //   payload["confirm"] = confirm;
        CloudFunction("crm_linkmarketplace", payload, (ok, result) =>
        {
            if (ok && result.ok)
            {
                var linkStatus = result.data["linkStatus"].ToStr();
                callack?.Invoke(true, linkStatus);
            }
            else
            {
                callack?.Invoke(false, null);
            }
        });
    }



    //OneTime Token
    public void onGetOneTimeToken(System.Action<string> callack = null)
    {
        var payload = new Dictionary<string, object>();
        CloudFunction("crm_getOnetimeToken", payload, (ok, result) =>
        {
            if (ok && result.ok)
            {
                var token = result.data["token"].ToStr();
                callack?.Invoke(token);
            }
            else
            {
                callack?.Invoke(null);
            }
        });
    }






    // [ Game ]
#if UNITY_EDITOR
    [VInspector.Button]
    void GotoGame()
    {
        var payload = new Dictionary<string, object>();
        payload.Add("gameId", "xxxxxx");
        EditorGUIService.Popup.OpenEditTextAreaBox("GotoGame", payload.SerializeToJson(SerializeHandle.FormattingIndented), (json) =>
        {
            payload = json.DeserializeObjectSimple<Dictionary<string, object>>();
            onGotoGame(payload["gameId"].ToString(), (datas) =>
            {

            });
        });
    }
    [VInspector.Button]
    void OpenGacha()
    {
        var payload = new Dictionary<string, object>();
        payload.Add("gameId", "xxxxxx");
        EditorGUIService.Popup.OpenEditTextAreaBox("OpenGacha", payload.SerializeToJson(SerializeHandle.FormattingIndented), (json) =>
        {
            payload = json.DeserializeObjectSimple<Dictionary<string, object>>();
            var gameId = payload["gameId"].ToString();
            var gameCampaignData = CRMData.GameCampaignData.Get(gameId);
            onOpenGacha(gameCampaignData, (reward) => { });


        });
    }
    [VInspector.Button]
    void OnViewDailyCheckIn()
    {
        EditorGUIService.Popup.OpenEditTextAreaBox("DailyCheckIn", CRMData.DailyData.current.SerializeToJson(SerializeHandle.FormattingIndented));
    }
    [VInspector.Button]
    void onQrHunt()
    {
        var payload = new Dictionary<string, object>();
        payload.Add("code", "xxxxxx");
        EditorGUIService.Popup.OpenEditTextAreaBox("QR-Hunt", payload.SerializeToJson(SerializeHandle.FormattingIndented), (json) =>
        {
            payload = json.DeserializeObjectSimple<Dictionary<string, object>>();
            CloudFunction("crm_qrHunt", payload, (ok, result) =>
            {

            });
        });
    }
    [VInspector.Button]
    void onTestAPI()
    {
        var payload = new Dictionary<string, object>();
        payload.Add("apiName", "xxxxxx");
        payload.Add("body", new Dictionary<string, object>());
        EditorGUIService.Popup.OpenEditTextAreaBox("TestAPI", payload.SerializeToJson(SerializeHandle.FormattingIndented), (json) =>
        {
            var res = json.DeserializeObjectSimple<Dictionary<string, object>>();
            var apiName = res.GetString("apiName");
            CloudFunction(apiName, res.GetDict("body"), (ok, result) =>
            {
                if (ok && result.ok)
                {
                    EditorGUIService.Popup.OpenEditTextAreaBox(apiName, result.data.SerializeToJson(SerializeHandle.FormattingIndented));
                }
                else
                {
                    EditorGUIService.Popup.OpenEditTextAreaBox(apiName, "Error");
                }
            });
        });
    }
#endif
    [System.Serializable]
    public class GotoGameData
    {
        public GotoGameData() { }
        public string sessionId;
        public string gameURL;
        public string gameId;
        public string statId;
        public string token;
        public bool newUser;
    }
    public void onGotoGame(string gameId, System.Action<GotoGameData> callack = null)
    {
        var payload = new Dictionary<string, object>();
        payload["gameId"] = gameId;
        CloudFunction("crm_gotoGameCampaign", payload, (ok, result) =>
        {
            if (ok && result.ok)
            {
                var data = result.data.DeserializeObjectSimple<GotoGameData>();
                callack?.Invoke(data);
            }
            else
            {
                callack?.Invoke(null);
            }
        });
    }
    public void onOpenGacha(CRMData.GameCampaignData gameCampaignData, System.Action<GachaData> callack = null)
    {

        var gameId = gameCampaignData.gameId;
        var prepair = new Dictionary<string, object>();
        prepair.Add("gameId", gameId);
        CloudFunction("crm_prepairGacha", prepair, (ok, prepairResult) =>
        {
            if (ok && prepairResult.ok)
            {
                var token = prepairResult.data["token"].ToStr();
                if (token.notnull())
                {
                    var payload = new Dictionary<string, object>();
                    payload.Add("gameId", gameId);
                    payload.Add("token", token);
                    CloudFunction("crm_openGacha", payload, (ok, result) =>
                    {
                        if (ok && result.ok)
                        {
                            GachaData gachaResult = null;
                            PatchingReward(result.data);
                            result.data.Modify("gachaResult", val =>
                            {
                                gachaResult = val.DeserializeObjectSimple<GameStore.Core.GachaData>();
                            });
                            callack?.Invoke(gachaResult);
                        }
                        else
                        {
                            callack?.Invoke(null);
                        }
                    });
                }
            }
            else
            {
                callack?.Invoke(null);
            }
        });
    }


    Dictionary<string, List<GachaData>> gachaRates = new Dictionary<string, List<GachaData>>();
    public void onGetGachaRate(string gameId, System.Action<List<GachaData>> callack = null)
    {
        if (gachaRates.ContainsKey(gameId))
        {
            callack?.Invoke(gachaRates[gameId]);
            return;
        }
        GameStore.WebGLService.NetworkService.GetGachaRate(gameId, (datas) =>
        {
            if (datas != null)
                gachaRates.Update(gameId, datas);
            callack?.Invoke(datas);
        });
    }















    void CloudFunctionWithoutEncryption(string functionName, Dictionary<string, object> body, System.Action<bool, baseResult.Result> callback = null)
    {
        GameStore.WebGLService.NetworkService.OnCloudFunctionWithoutEncryption(functionName, body, (ok, rawJson, err) =>
        {
            Debug.Log($"Callback {ok} --> {rawJson}");
            if (ok)
            {
                var result = rawJson.DeserializeObjectSimple<baseResult>();
                Debug.Log($"Callback {result.result.SerializeToJsonSimple()}");
                Debug.Log($"Callback {result.result.code}");
                callback?.Invoke(true, result.result);
            }
            else
            {
                callback?.Invoke(false, null);
            }
        });
    }

    public void CloudFunctionIframe(string functionName, Dictionary<string, object> payload, System.Action<bool, Dictionary<string, object>> callack = null)
    {
        CloudFunction(functionName, payload, (ok, result) =>
        {
            if (ok && result.ok)
            {
                callack?.Invoke(true, result.data);
            }
            else
            {
                callack?.Invoke(false, null);
            }
        });
    }


    bool loadingYield;
    public void LoadingYield()
    {
        loadingYield = true;
    }
    void CloudFunction(string functionName, Dictionary<string, object> body, System.Action<bool, baseResult.Result> callback = null)
    {
        if (loadingYield)
            UIRoot.instance.OnBeginLoadingYield();

        GameStore.WebGLService.NetworkService.OnCloudFunction(functionName, body, (ok, rawJson, err) =>
        {

            UIRoot.instance.OnEndLoadingYield(() =>
            {

                Debug.Log($"Callback {ok} --> {rawJson}");
                if (ok)
                {
                    var result = rawJson.DeserializeObjectSimple<baseResult>();
                    switch (result.result.code)
                    {
                        case 20001:
                            Language.OpenPopup("EXPIRE").OnHideAllBtns();
                            break;
                        default:
                            callback?.Invoke(true, result.result);
                            break;
                    }
                }
                else
                {
                    callback?.Invoke(false, null);
                }
                loadingYield = false;

            });

        });
    }



}
