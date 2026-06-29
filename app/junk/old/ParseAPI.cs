using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using GameStore.Core;
using System;
using static GameStore.Core.Networks.NetworkService;
using System.Linq;


public class ParseAPI : MonoBehaviour
{
    [SerializeField] public string URL;
    [SerializeField] string AppId;
    [SerializeField] string ApiKey;
    [SerializeField] string XParseMasterKey;
    [SerializeField] public string SessionToken;
    [SerializeField] public string XMLFormattedRSAKey;
    public string Name;
    public string GameBundle;
    public string GameConfigId;
    public string SessionId { get; private set; }
    public static ParseAPI Create(GameObject target)
    {
        var parseAPI = target.GetComponent<ParseAPI>();
        if (parseAPI == null)
            parseAPI = target.AddComponent<ParseAPI>();
        parseAPI.Init(GameStore.Core.GameBundle.instance);
        return parseAPI;
    }
    void Init(GameStore.Core.GameBundle gameBundle)
    {

        GameBundle = gameBundle.gameBundle;
        GameConfigId = gameBundle.gameConfigId;

        var localServerConfig = GetLocalServerConfig();
        if (localServerConfig != null)
        {
            //** use Extranl ServerConfig 
            ApplyServer(localServerConfig);
        }
        else
        {
            //** use by GameBundle
            ApplyServer(gameBundle.serverConfig);
        }

        retry = 3;
    }
    void ApplyServer(GameStore.Core.GameBundle.ServerConfig serverConfig)
    {
        Debug.Log($"ApplyServer : {serverConfig.Name}");
        URL = serverConfig.serverURL;
        //ENSURE THAT URL IS IN CORRECT FORMAT by NNNN
        if (!URL.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            URL = "https://" + URL;
        }
        Name = serverConfig.Name;
        AppId = serverConfig.AppId;
        ApiKey = serverConfig.ApiKey;
        XParseMasterKey = serverConfig.XParseMasterKey;
    }
    internal UserData userData => UserData.owner;


    internal Dictionary<string, string> device => GameStore.WebGLService.GetDevice;







    [UnityEngine.Scripting.Preserve]
    public class server
    {
        public string endpoint;
        public string hash;
        public bool isDefault = false;
        public bool skipVerifyGameBandle = false;
    }
    bool skipVerifyGameBandle = false;
    GameStore.Core.GameBundle.ServerConfig GetLocalServerConfig()
    {
        if (GameStore.WebGLService.Config.ContainsKey("serverConfig"))
        {
            var dict = GameStore.WebGLService.Config["serverConfig"].ToDictStringObject();
            if (dict.ContainsKey("assign") && dict.ContainsKey("servers"))
            {
                server serv = null;
                var assign = dict["assign"].ToString();
                var servers = dict["servers"].DeserializeObjectSimple<Dictionary<string, server>>();
                if (assign == "auto")
                {
                    if (GameStore.WebGLService.IsWebGL)
                    {
                        if (GameStore.WebGLService.URLInfomation.Params.ContainsKey("serv"))
                        {
                            //** USE BY Params ?serv=dev
                            var servName = GameStore.WebGLService.URLInfomation.Params["serv"];
                            if (servers.ContainsKey(servName))
                            {
                                serv = servers[servName];
                            }
                        }
                        else
                        {
                            // USE BY URL Web current address... www.reflex.com
                            var url = GameStore.WebGLService.URLInfomation.FullPath;
                            foreach (var server in servers)
                            {
                                if (url.Contains(server.Value.endpoint))
                                    serv = server.Value;
                            }
                        }


                        if (serv == null)
                        {
                            foreach (var find in servers.Values.ToList())
                            {
                                if (find.isDefault)
                                {
                                    serv = find;
                                }
                            }
                        }
                    }
                }
                else
                {
                    // dev / prod
                    if (servers.ContainsKey(assign))
                        serv = servers[assign];
                }

                if (serv != null)
                {
                    skipVerifyGameBandle = serv.skipVerifyGameBandle;
                    if (serv.hash.notnull())
                    {
                        return prase(serv.hash);
                    }
                }
            }
        }
        return null;
    }
    GameStore.Core.GameBundle.ServerConfig prase(string enCode)
    {
        var deCode = HASHEncrypt.Decrypt(enCode, "g@m!f!c@ti0n");
        try
        {
            var serverConfig = deCode.DeserializeObjectSimple<GameStore.Core.GameBundle.ServerConfig>();
            return serverConfig;
        }
        catch
        {
            return null;
        }
    }


    /*
    public RuntimeBtn TestHash = new RuntimeBtn((r) => {
        ParseAPI.FakeHash(r.String);
    });
    */
    public static int Hash(string input)
    {
        int hash = 0;
        if (input.Length == 0) return hash;

        foreach (char chr in input)
        {
            int charCode = chr;
            hash = ((hash << 5) - hash) + charCode;
            hash |= 0; // Convert to 32-bit integer
        }
        return Mathf.Abs(hash);
    }
    public static string FakeHash(string input)
    {
        var hash = Hash(input);
        var fake = $"F{UnityEngine.Random.Range(11111111, 99999999)}" +
            $"{hash}" +
            $"{UnityEngine.Random.Range(111111, 999999)}";
        //Debug.Log($"hash : {hash}");
        //Debug.Log($"fake : {fake}");
        return fake;
    }





    [UnityEngine.Scripting.Preserve]
    [System.Serializable]
    public class ObjectData
    {
        public string objectId;
        public string createdAt;
        public string updatedAt;
    }
    public void OnCreatingObject(string tableName, Dictionary<string, object> body, System.Action<ObjectData> callback = null)
    {
        StartCoroutine(GetRequest(Method.POST, $"classes/{tableName}", body, (complete, result, err) =>
        {
            if (complete)
            {
                var objectData = result.DeserializeObjectSimple<ObjectData>();
                callback?.Invoke(objectData);
            }
            else callback?.Invoke(null);
        }));
    }
    public void OnUpdateObject(string tableName, string objectId, Dictionary<string, object> body, System.Action<bool> callback = null)
    {
        StartCoroutine(GetRequest(Method.PUT, $"classes/{tableName}/{objectId}", body, (complete, result, err) =>
        {
            if (complete)
            {
                callback?.Invoke(true);
            }
            else callback?.Invoke(false);
        }));
    }
    public void OnIncrementObject(string tableName, string objectId, string key, int value, System.Action<bool> callback = null)
    {
        var form = new Dictionary<string, object>();
        form.Add(key, new Dictionary<string, object>() {
            { "__op","Increment"},
            { "amount", value }
        });
        StartCoroutine(GetRequest(Method.PUT, $"classes/{tableName}/{objectId}", form, (complete, result, err) =>
        {
            if (complete)
            {
                callback?.Invoke(true);
            }
            else callback?.Invoke(false);
        }));
    }
    public void OnDeleteObject(string tableName, string objectId, System.Action<bool> callback = null)
    {
        var form = new Dictionary<string, object>();
        StartCoroutine(GetRequest(Method.DELETE, $"classes/{tableName}/{objectId}", form, (complete, result, err) =>
        {
            if (complete)
            {
                callback?.Invoke(true);
            }
            else callback?.Invoke(false);
        }));
    }
    public void OnGetConfig(System.Action<Dictionary<string, object>> callback = null)
    {
        var form = new Dictionary<string, object>();
        StartCoroutine(GetRequest(Method.GET, $"config", null, (complete, result, err) =>
        {
            if (complete)
            {
                var configResult = result.DeserializeObjectSimple<Dictionary<string, object>>();
                callback?.Invoke(configResult["params"].DeserializeObjectSimple<Dictionary<string, object>>());
            }
            else callback?.Invoke(null);
        }));
    }
    public void OnGetMyObject(System.Action<bool, string> callback = null)
    {
        OnGetObject(GameBundle, GameConfigId, callback);
    }
    public void OnGetObject(string tableName, string objectId, System.Action<bool, string> callback = null)
    {
        var form = new Dictionary<string, object>();
        //?include=user
        StartCoroutine(GetRequest(Method.GET, $"classes/{tableName}/{objectId}", form, (complete, result, err) =>
        {
            if (complete)
            {
                callback?.Invoke(true, result);
            }
            else callback?.Invoke(false, null);
        }));
    }

    [UnityEngine.Scripting.Preserve]
    [System.Serializable]
    public class Param
    {
        public int? limit;
        public int? offset;
        public string sortBy;
        public bool desc;
        public string ToPath()
        {
            string path = null;
            if (sortBy != null)
            {
                if (path != null) path += "&";
                path += $"order={(desc ? "-" : "")}{System.Uri.EscapeDataString(sortBy)}";
            }
            if (limit != null)
            {
                if (path != null) path += "&";
                path += $"limit={limit}";
            }
            if (offset != null)
            {
                if (path != null) path += "&";
                path += $"offset={offset}";
            }
            return $"{path}";
        }
    }
    public void OnGetTable(string tableName, Param param, System.Action<bool, string> callback = null)
    {
        StartCoroutine(GetRequest(Method.GET, $"classes/{tableName}?{param.ToPath()}", new Dictionary<string, object>(), (complete, result, err) =>
        {
            if (complete)
            {
                callback?.Invoke(true, result);
            }
            else callback?.Invoke(false, null);
        }));
    }
    public void OnGetQueryTable(string tableName, System.Action<bool> callback = null)
    {
        #region Note
        //where={"arrayKey":{"$all":[2,3,4]}} --> Array 
        //where={"score":{"$in":[1,3,5,7,9]}} --> Contained In
        //where={"$or":[{"wins":{"$gt":150}},{"wins":{"$lt":5}}]} --> OR
        //where={"score":{"$gte":1000,"$lte":3000}} --> score ( >1000 && <3000 ) 
        //--> $gt = Greater Than
        //--> $gte = Greater Than Or Equal To
        //--> $lt = Less Than
        //--> $lte = Less Than Or Equal To

        //&order=-createdAt
        //&limit=100
        //&include=profile
        //&include=profile.user
        #endregion

        var form = new Dictionary<string, object>();
        form.Add("myField", "Hello World!");
        var json = form.SerializeToJsonSimple();


        string queryParams = $"?where={System.Uri.EscapeDataString(json)}&limit=1";
        StartCoroutine(GetRequest(Method.GET, $"classes/{tableName}{queryParams}", new Dictionary<string, object>(), (complete, result, err) =>
        {
            if (complete)
            {
                callback?.Invoke(true);
            }
            else callback?.Invoke(false);
        }));
    }












    [UnityEngine.Scripting.Preserve]
    [System.Serializable]
    public class baseResult
    {
        public Result result;

        // Default constructor
        public baseResult()
        {
            result = new Result(); // Initialize the nested Result class
        }

        // Parameterized constructor
        public baseResult(Result result)
        {
            this.result = result;
        }
        [UnityEngine.Scripting.Preserve]
        [System.Serializable]
        public class Result
        {
            public bool ok => code == 200;
            public int code;
            public string message;
            public Dictionary<string, object> data;

            // Default constructor
            public Result()
            {
                data = new Dictionary<string, object>(); // Initialize the dictionary
            }

            // Parameterized constructor
            public Result(int code, string message, Dictionary<string, object> data)
            {
                this.code = code;
                this.message = message;
                this.data = data ?? new Dictionary<string, object>();
            }
        }
    }



    public void OnGetInit(System.Action<Dictionary<string, object>> callback = null)
    {
        var body = new Dictionary<string, object>();
        body.Add("gameConfigId", GameConfigId);
        CloudFunction("getInitConfig", body, (complete, rawJson, err) =>
        {
            if (complete)
            {
                baseResult result = rawJson.DeserializeObjectSimple<baseResult>();
                if (result.result.code == 200)
                {
                    var initData = result.result.data["initData"].ToDictStringObject();
                    initData = initData != null ? initData : new Dictionary<string, object>();
                    GameStore.WebGLService.InitData = initData;
                    callback?.Invoke(initData);
                }
                else callback?.Invoke(null);
            }
            else
            {
                callback?.Invoke(null);
            }
        });
    }

    //*** [Login]
    public void OnCloudLogin(string uuid, System.Action<UserData> callback = null)
    {
        //if(password == null)
        //    password = Mathf.Abs(uuid.GetHashCode()).ToString();

        var body = new Dictionary<string, object>();
        //body.Add("username", uuid);
        //body.Add("password", password);
        //body.Add("autoCreate", autoCreate);
        //body.Add("gameBundle", GameBundle);

        body.Add("gameConfigId", GameConfigId);
        body.Add("uuid", uuid);
        body.Add("data", new Dictionary<string, object>{
            { "device", device }
        });



        CloudFunction("loginCustom", body, (complete, rawJson, err) =>
        {
            if (complete)
            {
                baseResult result = rawJson.DeserializeObjectSimple<baseResult>();

                if (result.result.code == 200)
                {
                    LoginDone(result.result.data, callback);
                }
                else callback?.Invoke(null);
            }
            else
            {
                callback?.Invoke(null);
            }
        });
    }


    public void OnValidateSession(string token, System.Action<UserData> callback = null )
    {
        URLParameters.JWT_Params jwt_params = null;
        try
        {
            jwt_params = JWT.DecodePayload(token).DeserializeObjectSimple<URLParameters.JWT_Params>();
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error on decoding JWT\n{e.Message} {e.StackTrace}");
            callback?.Invoke(null);
            return;
        }

        if (!jwt_params.IsValid())
        {
            Debug.LogError($"Invalid JWT parameters\n{jwt_params.SerializeToJsonSimple()}");
            callback?.Invoke(null);
            return;
        }

 

        if (!skipVerifyGameBandle && jwt_params.gameBundleId != GameBundle)
        {
            Debug.LogError($"Invalid gameBundle({GameBundle}) \n{jwt_params.SerializeToJsonSimple()}");
            callback?.Invoke(null);
            return;
        }

#if UNITY_EDITOR
        Debug.Log(jwt_params.SerializeToJsonSimple());
#endif



        GameConfigId = jwt_params.gameConfigId;
        SessionToken = jwt_params.userToken;





        var body = new Dictionary<string, object>();
        body.Add("gameToken", jwt_params.gameToken);
        body.Add("data", new Dictionary<string, object>{
            { "device", device }
        });
        CloudFunction("validateSession", body, (complete, rawJson, err) =>
        {
            if (complete)
            {
                var result = rawJson.DeserializeObjectSimple<baseResult>();
                if (result.result.code == 200)
                {
                    LoginDone(result.result.data, callback);
                }
                else callback?.Invoke(null);
            }
            else
            {
                callback?.Invoke(null);
            }
        });
    }







    public void LoginDone(Dictionary<string, object> data, System.Action<UserData> callback)
    {
        /*
        var user = result.result.data["user"].DeserializeObjectSimple<UserData>();
        var gameStat = result.result.data["stat"].DeserializeObjectSimple<GameStatData>();
        SessionToken = user.sessionToken;
        user.serverTime = result.result.data["serverTime"].ToLong();
        user.gameStat = gameStat;
        gameStat.displayName = user.displayName;
        if (result.result.data.ContainsKey("provider"))
        {
            user.providerData = result.result.data["provider"].DeserializeObjectSimple<ProviderData>();
        }
        */

        var user = GetValue<UserData>(data, "user");
        user.gameStat = GetValue<GameStatData>(data, "stat");
        user.providerData = GetValue<ProviderData>(data, "provider");
        user.redirect = GetValue<Redirect>(data, "redirect");
        user.serverTime = GetValue<long>(data, "serverTime");
        user.gameStat.displayName = user.displayName;
        user.isNewDay = GetValue<bool>(data, "isNewDay");
        SessionToken = user.sessionToken;
        UserData.owner = user;

        //SessionToken
        XMLFormattedRSAKey = data["publicKey"].ToString();
        Debug.Log("XMLFormattedRSAKey : " + XMLFormattedRSAKey);
        Debug.Log(user.SerializeToJsonSimple());
        Debug.Log(user.gameStat.SerializeToJsonSimple());
        GameStore.WebGLService.events.onLoginDone?.Invoke();
        callback?.Invoke(user);

    }
    public void Logout()
    {
        SessionToken = null;
        XMLFormattedRSAKey = null;
        GameStore.WebGLService.events.onExitOrSignout?.Invoke();
    }







    //*** [GameData (GameStat)]
    public void OnCloudGetGameStat(string gameStatId, System.Action<GameStore.Core.UserData> callback = null)
    {
        if (gameStatId.isnull())
        {
            gameStatId = userData.gameStat.statId;
        }

        var body = new Dictionary<string, object>();
        var payload = new Dictionary<string, object>();
        body.Add("payload", payload);
        payload.Add("gameConfigId", GameConfigId);
        payload.Add("statId", gameStatId);


        CloudFunction("getGameStat", body, (complete, rawJson, err) =>
        {
            GameStore.Core.UserData user = null;
            if (complete)
            {
                var result = rawJson.DeserializeObjectSimple<baseResult>();
                if (result.result.code == 200 && result.result.data != null && result.result.data.ContainsKey("stat"))
                {
                    user = GetValue<UserData>(result.result.data, "user");
                    user.gameStat = GetValue<GameStatData>(result.result.data, "stat");
                    user.gameStat.displayName = user.displayName;

                    if (user.IsSelf())
                    {
                        userData.displayName = user.displayName;
                        userData.gameStat = user.gameStat;
                    }

                    Debug.Log($"GetGameStat--> {user.SerializeToJsonSimple()}");
                }
            }
            callback?.Invoke(user);
        });
    }


    //*** [GameConfig]
    public void OnCloudGetGameConfig(System.Action<GameStore.Core.GameConfigData> callback = null)
    {
        var body = new Dictionary<string, object>();
        var payload = new Dictionary<string, object>();
        body.Add("payload", payload);
        CloudFunction("getGameConfig", body, (complete, rawJson, err) =>
        {
            GameStore.Core.GameConfigData config = null;
            if (complete)
            {
                var result = rawJson.DeserializeObjectSimple<baseResult>();
                if (result.result.code == 200 && result.result.data != null)
                {
                    if (result.result.data.ContainsKey("hash"))
                    {
                        var rawjson = result.result.data["hash"].ToString();
                        Debug.Log(rawjson);
                        rawjson = SimpleHashEncrypt.Decrypt(rawjson, GameConfigId);
                        Debug.Log(rawjson);
                        config = rawjson.DeserializeObjectSimple<GameConfigData>();
                    }
                    else
                    {
                        config = result.result.data.DeserializeObjectSimple<GameConfigData>();
                    }
                    if (userData != null) userData.gameConfig = config;
                    Debug.Log($"GetGameConfig--> {config.SerializeToJsonSimple()}");
                }
            }
            callback?.Invoke(config);
        });
    }









    //*** [Profile]
    public void OnCloudChangeDisplayName(string displayname, System.Action<bool, string> callback = null)
    {
        var body = new Dictionary<string, object>();
        var payload = new Dictionary<string, object>();
        body.Add("payload", payload);
        payload.Add("displayName", displayname);
        CloudFunction("changeDisplayName", body, (complete, rawJson, err) =>
        {

            if (complete)
            {
                var result = rawJson.DeserializeObjectSimple<baseResult>();
                if (result.result.code == 200)
                {
                    callback?.Invoke(true, null);
                }
                else callback?.Invoke(false, result.result.message);
            }
            else
            {
                callback?.Invoke(false, err.error);
            }
        });
    }






    //*** [CustomProperties]
    [System.Serializable]
    public class updateTableData
    {
        public enum operation { update, increment, delete }
        public operation op;
        public string key;
        public object value;
    }
    public enum TableName { profile, customProperties, daily }
    List<Dictionary<string, object>> ToUpdateTableField(List<updateTableData> updateTableDatas)
    {
        var opList = new List<Dictionary<string, object>>();
        foreach (var data in updateTableDatas)
        {
            var op = new Dictionary<string, object>();
            opList.Add(op);
            op.Add("op", data.op.ToString());
            op.Add("key", data.key);
            op.Add("value", data.value);
        }
        return opList;
    }
    public void OnCloudUpdateTable(TableName tableName, List<updateTableData> updateTableDatas, System.Action<bool, Dictionary<string, object>> callback = null)
    {
        var body = new Dictionary<string, object>();
        var payload = new Dictionary<string, object>();
        body.Add("payload", payload);
        payload.Add("fieldName", tableName.ToString());
        payload.Add("opList", ToUpdateTableField(updateTableDatas));


        CloudFunction("updateDict", body, (complete, rawJson, err) =>
        {
            if (complete)
            {
                var result = rawJson.DeserializeObjectSimple<baseResult>();
                if (result.result.code == 200)
                {
                    var customProperties = GetValue<Dictionary<string, object>>(result.result.data, tableName.ToString());
                    callback?.Invoke(true, customProperties);
                }
                else callback?.Invoke(false, null);
            }
            else
            {
                callback?.Invoke(false, null);
            }
        });
    }
    public void OnCloudUpdateShare(string statId, List<updateTableData> updateTableDatas, System.Action<bool, Dictionary<string, object>> callback = null)
    {
        var body = new Dictionary<string, object>();
        var payload = new Dictionary<string, object>();
        body.Add("payload", payload);
        payload.Add("statId", statId);
        payload.Add("opList", ToUpdateTableField(updateTableDatas));
        CloudFunction("updateShare", body, (complete, rawJson, err) =>
        {
            if (complete)
            {
                var result = rawJson.DeserializeObjectSimple<baseResult>();
                if (result.result.code == 200)
                {
                    var shareProperties = GetValue<Dictionary<string, object>>(result.result.data, "shareProperties");
                    callback?.Invoke(true, shareProperties);
                }
                else callback?.Invoke(false, null);
            }
            else
            {
                callback?.Invoke(false, null);
            }
        });
    }









    //*** [LeaderBoard]
    //public class leaderboardResult
    //{
    //    public List<Dictionary<string, object>> board;
    //    public long serverTime;
    //}

    //sortBy :  “topScore“,“score“, “win“, “lastWinTime“, “accScore“
    //period: “allTime”, “daily“, “weekly“, “monthly“
    //[optional] order: “ascending“, “descending“ default is “descending“
    //[optional] count: any number default is 100



    public void OnCloudLeaderboard(LeaderBoardRequest leaderBoardRequest, System.Action<List<GameStore.Core.LeaderboardData>> callback = null)
    {
        var payload = new Dictionary<string, object>();
        payload.Add("sortBy", leaderBoardRequest.sortBy.ToString());
        payload.Add("order", leaderBoardRequest.order.ToString());
        payload.Add("period", leaderBoardRequest.period.ToString());
        payload.Add("count", leaderBoardRequest.count);
        OnCloudBoard("getLeaderboard", payload, callback);
    }

    public void OnCloudDiscovery(int userCount, System.Action<List<GameStore.Core.LeaderboardData>> callback = null)
    {
        var payload = new Dictionary<string, object>();
        payload.Add("userCount", userCount);
        OnCloudBoard("getDiscovery", payload, callback);
    }

    void OnCloudBoard(string functionName, Dictionary<string, object> payload, System.Action<List<GameStore.Core.LeaderboardData>> callback = null)
    {
        var body = new Dictionary<string, object>();
        body.Add("payload", payload);

        CloudFunction(functionName, body, (complete, rawJson, err) =>
        {
            if (complete)
            {
                var result = rawJson.DeserializeObjectSimple<baseResult>();
                if (result.result.code == 200)
                {
                    //** score
                    //** displayName
                    //** statId
                    //** profile
                    var serverTime = result.result.data["serverTime"].ToLong();
                    LeaderboardData.Utls.ServerTime = serverTime;

                    var leaderboards = GetValue<List<LeaderboardData>>(result.result.data, "board");
                    if (leaderboards != null)
                    {
                        int index = 1;
                        foreach (var user in leaderboards)
                        {
                            user.index = index;
                            index++;
                        }
                    }
                    else
                    {
                        leaderboards = new List<LeaderboardData>();
                    }
                    Debug.Log($"leaderboards = {leaderboards.Count}");
                    callback?.Invoke(leaderboards);
                }
                else callback?.Invoke(null);
            }
            else
            {
                callback?.Invoke(null);
            }
        });
    }









    //*** [Purchase & Catalog]
    [System.Serializable]
    public class PurchaseItemData
    {
        public string itemId;
        public int amount;
    }
    public void OnPurchaseItem(Dictionary<string, int> items, List<updateTableData> opListCustomProperties, System.Action<bool, RewardResponse, Dictionary<string, object>> done = null)
    {
        OnAPIItem("purchaseItem", items, opListCustomProperties, done);
    }
    public void OnPurchaseCustomProperties(Dictionary<string, int> items, List<updateTableData> opListCustomProperties, System.Action<bool, RewardResponse, Dictionary<string, object>> done = null)
    {
        OnAPIItem("purchaseCustomProperties", items, opListCustomProperties, done);
    }
    public void OnConsumeItem(Dictionary<string, int> items, List<updateTableData> opListCustomProperties, System.Action<bool, RewardResponse, Dictionary<string, object>> done = null)
    {
        OnAPIItem("consumeItem", items, opListCustomProperties, done);
    }
    void OnAPIItem(string api, Dictionary<string, int> items, List<updateTableData> opListCustomProperties, System.Action<bool, RewardResponse, Dictionary<string, object>> done = null)
    {
        var body = new Dictionary<string, object>();
        var payload = new Dictionary<string, object>();
        body.Add("payload", payload);

        List<PurchaseItemData> itemDatas = new List<PurchaseItemData>();
        foreach (var data in items)
            itemDatas.Add(new PurchaseItemData()
            {
                itemId = data.Key,
                amount = data.Value
            });
        payload.Add("items", itemDatas);
        payload.Add("opListCustomProperties", ToUpdateTableField(opListCustomProperties));
        CloudFunction(api, body, (complete, rawJson, err) =>
        {
            if (complete)
            {
                var result = rawJson.DeserializeObjectSimple<baseResult>();
                if (result.result.code == 200)
                {
                    //var currency = GetValue<int>(result.result.data, "currency");
                    //var inventory = GetValue<Dictionary<string, int>>(result.result.data, "inventory");
                    var customProperties = GetValue<Dictionary<string, object>>(result.result.data, "customProperties");
                    var reward = new RewardResponse(result.result.data);
                    done?.Invoke(true, reward, customProperties);
                }
                else done?.Invoke(false, null, null);
            }
            else
            {
                done?.Invoke(false, null, null);
            }
        });
    }







    //*** [Gacha]
    public enum gachaStatus
    {
        failed,
        success,
        already,
        reachLimit,
        invalid
    }
    public void OnCloudOpenGahca(string gachaId, string code, System.Action<bool, string, gachaStatus> callback = null)
    {
        var body = new Dictionary<string, object>();
        var payload = new Dictionary<string, object>();
        body.Add("payload", payload);


        if (gachaId.notnull())
            payload.Add("gachaId", gachaId);

        if (code.notnull())
            payload.Add("code", code);


        CloudFunction("openGacha", body, (complete, rawJson, err) =>
        {

            if (complete)
            {
                var result = rawJson.DeserializeObjectSimple<baseResult>();
                if (result.result.code == 200)
                {
                    var reward = GetValue<string>(result.result.data, "reward");
                    var status = GetValue<string>(result.result.data, "status");
                    SessionId = GetValue<string>(result.result.data, "sessionId");
                    gachaStatus resultStatus = gachaStatus.success;
                    switch (status)
                    {
                        case "success": resultStatus = gachaStatus.success; break;
                        case "already": resultStatus = gachaStatus.already; break;
                        case "reachLimit": resultStatus = gachaStatus.reachLimit; break;
                        case "invalid": resultStatus = gachaStatus.invalid; break;
                    }
                    Debug.Log(reward);
                    callback?.Invoke(true, reward, resultStatus);
                }
                else callback?.Invoke(false, null, gachaStatus.failed);
            }
            else
            {
                callback?.Invoke(false, null, gachaStatus.failed);
            }
        });
    }
    public void OnCloudGetGahcaRate(string gachaId, System.Action<List<GameStore.Core.GachaData>> callback = null)
    {
        var body = new Dictionary<string, object>();
        var payload = new Dictionary<string, object>();
        body.Add("payload", payload);
        payload.Add("gachaId", gachaId);
        CloudFunction("getGachaRate", body, (complete, rawJson, err) =>
        {

            if (complete)
            {
                var result = rawJson.DeserializeObjectSimple<baseResult>();
                if (result.result.code == 200)
                {
                    var datas = GetValue<List<GameStore.Core.GachaData>>(result.result.data, "gachaRate");
                    callback?.Invoke(datas);
                }
                else callback?.Invoke(null);
            }
            else
            {
                callback?.Invoke(null);
            }
        });
    }







    //*** [Task]
    public void OnCloudCreateTask(List<string> tasks, System.Action<bool, List<UserTask>> callback = null)
    {
        var body = new Dictionary<string, object>();
        var payload = new Dictionary<string, object>();
        body.Add("payload", payload);
        payload.Add("types", tasks);
        CloudFunction("createTask", body, (complete, rawJson, err) =>
        {

            if (complete)
            {
                var result = rawJson.DeserializeObjectSimple<baseResult>();
                if (result.result.code == 200)
                {
                    var tasks = GetValue<List<UserTask>>(result.result.data, "tasks");
                    callback?.Invoke(true, tasks);
                }
                else callback?.Invoke(false, null);
            }
            else
            {
                callback?.Invoke(false, null);
            }
        });
    }
    public void OnCloudRemoveTask(List<string> ids, System.Action<bool> callback = null)
    {
        var body = new Dictionary<string, object>();
        var payload = new Dictionary<string, object>();
        body.Add("payload", payload);
        payload.Add("ids", ids);
        CloudFunction("removeTask", body, (complete, rawJson, err) =>
        {

            if (complete)
            {
                var result = rawJson.DeserializeObjectSimple<baseResult>();
                if (result.result.code == 200)
                {
                    callback?.Invoke(true);
                }
                else
                    callback?.Invoke(false);
            }
            else
            {
                callback?.Invoke(false);
            }
        });
    }
    public void OnCloudClearTask(System.Action<bool> callback = null)
    {
        var body = new Dictionary<string, object>();
        var payload = new Dictionary<string, object>();
        body.Add("payload", payload);
        CloudFunction("clearTask", body, (complete, rawJson, err) =>
        {

            if (complete)
            {
                var result = rawJson.DeserializeObjectSimple<baseResult>();
                if (result.result.code == 200)
                {
                    callback?.Invoke(true);
                }
                else
                    callback?.Invoke(false);
            }
            else
            {
                callback?.Invoke(false);
            }
        });
    }
    public void OnCloudSubmitTask(string id, List<updateTableData> customProperties, System.Action<bool, RewardResponse> callback = null)
    {
        var body = new Dictionary<string, object>();
        var payload = new Dictionary<string, object>();
        body.Add("payload", payload);
        payload.Add("id", id);
        payload.Add("opListCustomProperties", ToUpdateTableField(customProperties));
        CloudFunction("submitTask", body, (complete, rawJson, err) =>
        {

            if (complete)
            {
                var result = rawJson.DeserializeObjectSimple<baseResult>();
                if (result.result.code == 200)
                {

                    /*
                    //** currency
                    var currency = result.result.data.ContainsKey("currency") ?
                    result.result.data["currency"].ToInt() : 0;

                    //** item
                    var item = result.result.data.ContainsKey("item")?
                    result.result.data["item"].DeserializeObjectSimple<Dictionary<string, int>>() : null;
                    */

                    var reward = new RewardResponse(result.result.data);
                    Debug.Log(reward.SerializeToJsonSimple());
                    callback?.Invoke(true, reward);
                }
                else callback?.Invoke(false, null);
            }
            else
            {
                callback?.Invoke(false, null);
            }
        });
    }
    public void OnCloudSubmitAndCreateTask(List<string> ids, List<string> createTypes, List<updateTableData> customProperties, System.Action<bool, RewardResponse, List<UserTask>> callback = null)
    {
        var body = new Dictionary<string, object>();
        var payload = new Dictionary<string, object>();
        body.Add("payload", payload);
        payload.Add("submitTaskIds", ids);
        payload.Add("createTaskTypes", createTypes);
        payload.Add("opListCustomProperties", ToUpdateTableField(customProperties));
        CloudFunction("submitAndCreateTasks", body, (complete, rawJson, err) =>
        {

            if (complete)
            {
                var result = rawJson.DeserializeObjectSimple<baseResult>();
                if (result.result.code == 200)
                {
                    var reward = new RewardResponse(result.result.data);
                    var tasks = GetValue<List<UserTask>>(result.result.data, "tasks");
                    Debug.Log(reward.SerializeToJsonSimple());
                    callback?.Invoke(true, reward, tasks);
                }
                else callback?.Invoke(false, null, null);
            }
            else
            {
                callback?.Invoke(false, null, null);
            }
        });
    }





    //** TBS SERVICE
    public void OnOTPRequest(string phone, System.Action<bool, string, string> callback = null)
    {
        var body = new Dictionary<string, object>();
        body.Add("gameConfigId", GameConfigId);
        body.Add("phone", phone);
        CloudFunction("otpRequest", body, (complete, rawJson, err) =>
        {

            if (complete)
            {
                var result = rawJson.DeserializeObjectSimple<baseResult>();
                if (result.result.code == 200)
                {
                    var token = result.result.data.Find("token").ToString();
                    var refno = result.result.data.Find("refno").ToString();
                    Debug.Log($"token = {token}");
                    Debug.Log($"token = {refno}");
                    callback?.Invoke(true, token, refno);
                }
                else
                    callback?.Invoke(false, null, null);
            }
            else
            {
                callback?.Invoke(false, null, null);
            }
        });
    }
    public void OnOTPVerify(string pin, string token, System.Action<bool> callback = null)
    {
        var body = new Dictionary<string, object>();
        body.Add("gameConfigId", GameConfigId);
        body.Add("pin", pin);
        body.Add("token", token);
        CloudFunction("otpVerify", body, (complete, rawJson, err) =>
        {

            if (complete)
            {
                var result = rawJson.DeserializeObjectSimple<baseResult>();
                if (result.result.code == 200)
                {
                    callback?.Invoke(true);
                }
                else
                    callback?.Invoke(false);
            }
            else
            {
                callback?.Invoke(false);
            }
        });
    }



    // SubmitProfileMessage (Package SMS)
    public void OnSubmitProfileMessage(Dictionary<string, object> profile, System.Action<bool> callback = null)
    {
        var body = new Dictionary<string, object>();
        body.Add("gameConfigId", GameConfigId);
        foreach (var data in profile)
            body.Add(data.Key, data.Value);

        CloudFunction("submitProfile", body, (complete, rawJson, err) =>
        {
            if (complete)
            {
                var result = rawJson.DeserializeObjectSimple<baseResult>();
                if (result.result.code == 200)
                {
                    callback?.Invoke(true);
                }
                else callback?.Invoke(false);
            }
            else
            {
                callback?.Invoke(false);
            }
        });
    }
    // SMS
    public void OnSendSMS(string phone, string message, System.Action<bool> callback = null)
    {
        var body = new Dictionary<string, object>();
        body.Add("gameConfigId", GameConfigId);
        body.Add("msisdn", phone);
        body.Add("message", message);
        CloudFunction("sendSMS", body, (complete, rawJson, err) =>
        {
            if (complete)
            {
                var result = rawJson.DeserializeObjectSimple<baseResult>();
                if (result.result.code == 200)
                {
                    callback?.Invoke(true);
                }
                else callback?.Invoke(false);
            }
            else
            {
                callback?.Invoke(false);
            }
        });
    }
    // EMAIL
    public void OnSendEMAIL(string email, string subject = null, string template_uuid = null, Dictionary<string, string> payload = null, System.Action<bool> callback = null)
    {
        var body = new Dictionary<string, object>();
        body.Add("gameConfigId", GameConfigId);
        body.Add("email", email);
        body.AddIfExists("template_uuid", template_uuid);
        body.AddIfExists("subject", subject);
        body.AddIfExists("payload", payload);
        CloudFunction("sendEmail", body, (complete, rawJson, err) =>
        {
            if (complete)
            {
                var result = rawJson.DeserializeObjectSimple<baseResult>();
                if (result.result.code == 200)
                {
                    callback?.Invoke(true);
                }
                else callback?.Invoke(false);
            }
            else
            {
                callback?.Invoke(false);
            }
        });
    }








    //** [Session]
    public void OnCreateSession(System.Action<bool> done = null)
    {
        var body = new Dictionary<string, object>();
        var payload = new Dictionary<string, object>();
        body.Add("payload", payload);
        body.Add("data", new Dictionary<string, object>{
            { "device", device }
        });
        CloudFunction("createSession", body, (complete, result, error) =>
        {
            done?.Invoke(complete);
        });
    }
    public void OnStartSession(System.Action<bool> done = null)
    {
        var body = new Dictionary<string, object>();
        var payload = new Dictionary<string, object>();
        body.Add("payload", payload);
        CloudFunction("startSession", body, (complete, rawJson, error) =>
        {

            if (complete)
            {
                var result = rawJson.DeserializeObjectSimple<baseResult>();
                if (result.result.code == 200)
                {
                    SessionId = result.result.data.Find("sessionId").ToStr();
                    done?.Invoke(true);
                }
                else done?.Invoke(false);
            }
            else
            {
                done?.Invoke(false);
            }

        });
    }
    public void OnEndSession(EndSessionRequest endSessionRequest, System.Action<bool, RewardResponse> done = null)
    {
        var body = new Dictionary<string, object>();
        var payload = new Dictionary<string, object>();
        body.Add("payload", payload);

        payload.Add("score", endSessionRequest.score);
        payload.Add("duration", endSessionRequest.duration);
        payload.Add("isWin", endSessionRequest.win);
        if (endSessionRequest.rewardId.notnull())
            payload.Add("rewardId", endSessionRequest.rewardId);
        if (endSessionRequest.customProperties != null && endSessionRequest.customProperties.Count > 0)
            payload.Add("customProperties", endSessionRequest.customProperties);
        if (endSessionRequest.custom != null && endSessionRequest.custom.action.notnull())
            payload.Add("custom", endSessionRequest.custom);

        CloudFunction("endSession", body, (complete, rawJson, err) =>
        {

            if (complete)
            {
                var result = rawJson.DeserializeObjectSimple<baseResult>();
                if (result.result.code == 200)
                {
                    /*
                    //** currency
                    var currency = result.result.data.ContainsKey("currency") ?
                    result.result.data["currency"].ToInt() : 0;

                    //** item
                    var item = result.result.data.ContainsKey("item") ?
                    result.result.data["item"].DeserializeObjectSimple<Dictionary<string, int>>() : null;
                    */

                    var reward = new RewardResponse(result.result.data);
                    done?.Invoke(true, reward);
                }
                else done?.Invoke(false, null);
            }
            else
            {
                done?.Invoke(false, null);
            }
        });
    }

















    [Space(20)]
    [Header("Test-Api")]
    [SerializeField] string functionName;
    [SerializeField] bool skinEncryption;
    [SerializeField][TextArea(10, 10)] string jsonPayload;
    [VInspector.Button]
    void OnTestLogin()
    {
        OnCloudLogin(jsonPayload);
    }
    [VInspector.Button]
    void OnTest()
    {

        var payload = jsonPayload.ToDictStringObject();
        var body = new Dictionary<string, object>();
        body.Add("payload", payload);
        payload.Add("gameBundle", GameBundle);


        CloudFunction(functionName, skinEncryption ? payload : body, null, skinEncryption);
    }






    public void OnCloudFunction(string functionName, Dictionary<string, object> payload, System.Action<bool, string, errorRespond> callback = null, bool skinEncryption = false)
    {

        var body = new Dictionary<string, object>();
        body.Add("payload", payload);
        payload.Add("gameBundle", GameBundle);
        CloudFunction(functionName, body, callback, skinEncryption);
    }









    int retry = 0;
    List<string> mFunctionsCanRetry = new List<string>() {
        "createSession" ,
        "startSession",
        "endSession",
        "getLeaderboard"
    };
    //public void ActiveRetry(int amount)
    //{
    //    retry = amount;
    //}
    public void CloudFunction(string functionName, Dictionary<string, object> body, System.Action<bool, string, errorRespond> callback = null, bool skinEncryption = false)
    {
        StartCoroutine(DoCloudFunction(functionName, body, callback, skinEncryption));
    }
    public IEnumerator DoCloudFunction(string functionName, Dictionary<string, object> body, System.Action<bool, string, errorRespond> callback = null, bool skinEncryption = false)
    {
        bool loop = true;
        while (loop)
        {
            loop = false;
            bool complete = false;
            string rawJson = string.Empty;
            errorRespond error = null;
            yield return StartCoroutine(GetRequest(Method.POST, $"functions/{functionName}", body, (_complete, _rawJson, _error) =>
            {
                complete = _complete;
                rawJson = _rawJson;
                error = _error;
            }, skinEncryption));


            if (complete)
            {
                retry = 3;
                if (GameStore.WebGLService.IsLogged)
                {
                    //var network = GameStore.Core.Networks.NetworkService.instance;
                    //var currency = network.user.currency;
                    //var baseResult = rawJson.DeserializeObjectSimple<baseResult>();
                }
                callback?.Invoke(complete, rawJson, error);
            }
            else
            {
                if (retry > 0 && (error != null && error.retry))
                {
                    retry--;
                    loop = true;
                    Debug.Log($"Retry -->  {retry}");
                    yield return new WaitForSeconds(1);
                }
                else
                {
                    if (UIPopup.IsHasPrefab)
                    {
                        string erroemessage = string.Empty;
                        bool blockFlow = false;

                        if (rawJson.notnull() && rawJson.Contains("Invalid session token") && rawJson.Contains("209"))
                        {
                            // Session Token Failed
                            // - login panel..... relogin 
                            // - sso login..... block flow now 
                            erroemessage = $"Invalid or expired session token. Please log in again to continue.";
                            blockFlow = GameStore.Core.GameBundle.instance.enableLoginPanel ? false : true;
                        }
                        else
                        {
                            // Other Error
                            erroemessage = $"Something went wrong.";
                            if (error != null)
                            {
                                erroemessage += $"\n({error.code}) {error.error}";
                            }
                        }
                        var popup = UIPopup.Open("Oops!", erroemessage, null, () =>
                        {
                            GameStore.WebGLService.NetworkService.OnLogOut();
                        });
                        if (blockFlow) popup.OnHideAllBtns();

                        GameStore.WebGLService.events.onFatalFailed?.Invoke();
                    }
                    else
                    {
                        callback?.Invoke(false, null, error);
                    }
                }


            }
        }
    }








    T GetValue<T>(Dictionary<string, object> data, string key)
    {
        var tasks = data.ContainsKey(key) ?
        data[key].DeserializeObjectSimple<T>() : default;
        return tasks;
    }


    public enum Method
    {
        GET, POST, PUT, DELETE
    }
    [System.Serializable]
    public class errorRespond
    {
        public long code;
        public string error;
        public bool retry;
    }
    //errorRespond error = null;
    IEnumerator GetRequest(Method method, string function, Dictionary<string, object> form, System.Action<bool, string, errorRespond> callback, bool skinEncryption = false)
    {

        if (Application.internetReachability == NetworkReachability.NotReachable)
        {
            Debug.LogError("No Internet Connection");
            callback?.Invoke(false, null, new errorRespond { code = 0, error = "No Internet Connection" });
            yield break;
        }

        //EndPoint
        var path = $"{URL}/{function}";

        Debug.Log(path);
        UnityWebRequest webRequest = new UnityWebRequest(path, method.ToString());//UnityWebRequest.PostWwwForm( path , form.SerializeToJsonSimple() );       


        if (form != null)
        {


            //Add FakeKey + Encryption
            string strPayload = "";
            if (!skinEncryption && form.ContainsKey("payload") && SessionToken.notnull())
            {

                var payload = form["payload"].SerializeToJsonSimple();
                var fk = FakeHash(payload);
                var finalkey = $"{SessionToken}_{GenerateHalfFk(fk)}";
                finalkey = $"{Hash(finalkey)}";

                strPayload = payload;
                //Debug.Log($"CloudFunction :{function}  -->  {payload}");

                //Payload Encryption 
                //Payload => RSA Encryption
                //string RSAedPayload = RSAEncryption.EncryptRSA(payload, XMLFormattedRSAKey);
                //RSA Encrypted Payload => AES Encryption
                var encrypt = AESEncryption.Encrypt(payload, finalkey);


                // Clone (not replace origin)
                form = new Dictionary<string, object>(form);
                form.Remove("payload");
                form.Add("payload", encrypt.EncryptedText);
                form.Add("fk", RSAEncryption.EncryptOAEP(fk, XMLFormattedRSAKey));
                form.Add("cache", RSAEncryption.EncryptOAEP(encrypt.IV, XMLFormattedRSAKey));
                //Debug.Log($"<color=white>[<color=yellow>Request</color>] CloudFunction : {function} </color> -->  {payload}");

            }
            else
            {
                strPayload = form.SerializeToJsonSimple();
                //Debug.Log($"<color=white>[<color=yellow>Request</color>] CloudFunction : {function} </color> -->  {payload}");
            }


            var body = form.SerializeToJsonSimple();
            var bytes = System.Text.Encoding.UTF8.GetBytes(body);
            webRequest.uploadHandler = (UploadHandler)new UploadHandlerRaw(bytes);


            Debug.Log($"<color=white>[<color=yellow>Request</color>] : {function} </color> -->  {strPayload}");

        }
        else
        {

        }

        webRequest.downloadHandler = (DownloadHandler)new DownloadHandlerBuffer();


        //Header
        if (SessionToken.notnull())
            webRequest.SetRequestHeader("X-Parse-Session-Token", SessionToken);
        webRequest.SetRequestHeader("X-Parse-Application-Id", AppId);
        webRequest.SetRequestHeader("X-Parse-REST-API-Key", ApiKey);
        webRequest.SetRequestHeader("Content-Type", "application/json");


        yield return webRequest.SendWebRequest();


        try
        {
            switch (webRequest.result)
            {
                case UnityWebRequest.Result.ConnectionError:
                case UnityWebRequest.Result.DataProcessingError:
                case UnityWebRequest.Result.ProtocolError:
                    string responseText = webRequest.downloadHandler != null ? webRequest.downloadHandler.text : "";
                    Debug.LogError($"{webRequest.result}: ({webRequest.error}) {responseText}");
                    callback?.Invoke(false, responseText, new errorRespond()
                    {
                        code = webRequest.responseCode,
                        error = $"{webRequest.result} {webRequest.error}",
                        retry = webRequest.result == UnityWebRequest.Result.ConnectionError
                    });
                    break;
                case UnityWebRequest.Result.Success:
                    Debug.Log($"<color=white>[<color=green>Received</color>] : {function} </color> -->  {webRequest.downloadHandler.text}");
                    //Debug.Log("Received: " + webRequest.downloadHandler.text);
                    callback?.Invoke(true, webRequest.downloadHandler.text, null);
                    break;
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Network Fatal Error!  {e.Message} {e.StackTrace}");
            callback?.Invoke(false, null, new errorRespond()
            {
                code = 0,
                error = "Network Fatal Error!"
            });
        }

        webRequest.Dispose();
    }




    public IEnumerator OpenAPI(string gameKey, string gameSecret, string function, WWWForm form, System.Action<bool, string, errorRespond> callback)
    {
        //var payload = openform.DeserializeObject<Dictionary<string, object>>();
        // StartCoroutine(PostRequest(Method.POST, $"https://aws-bug-parse-gamification.thelastbug.co/open/{openAPI}", payload));


        //EndPoint
        var path = $"{URL}/open/{function}";
        Debug.Log(path);
        UnityWebRequest webRequest = UnityWebRequest.Post(path, form);
        webRequest.timeout = 60;
        webRequest.downloadHandler = (DownloadHandler)new DownloadHandlerBuffer();
        webRequest.SetRequestHeader("Content-Type", "application/json");
        webRequest.SetRequestHeader("GameKey", gameKey);
        webRequest.SetRequestHeader("GameSecret", gameSecret);



        yield return webRequest.SendWebRequest();
        if (webRequest.result != UnityWebRequest.Result.Success)
        {
            string responseText = webRequest.downloadHandler != null ? webRequest.downloadHandler.text : "";
            Debug.LogError($"{webRequest.result}: ({webRequest.error}) {responseText}");

            callback?.Invoke(false, null, new errorRespond()
            {
                code = webRequest.responseCode,
                error = $"{webRequest.result} {webRequest.error}",
                retry = webRequest.result == UnityWebRequest.Result.ConnectionError
            });
        }
        else
        {
            Debug.Log($"Upload success: {webRequest.downloadHandler.text}");
            callback?.Invoke(true, webRequest.downloadHandler.text, null);
        }
        webRequest.Dispose();
    }












    string GenerateHalfFk(string fk, bool isEven = false)
    {
        string result = "";
        foreach (char c in fk)
        {
            if (isEven)
            {
                result += c;
            }
            isEven = !isEven;
        }
        return result;
    }
}
