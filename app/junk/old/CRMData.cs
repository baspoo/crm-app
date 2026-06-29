using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Scripting;




[System.Serializable]
[UnityEngine.Scripting.Preserve]
public class CRMData
{


    public static CRMData instance;
    public static void Init()
    {
        instance = new CRMData();
    }






    public static NotifManager NotifInventory = new NotifManager("notif_inventory");
    public static NotifManager NotifDelivery = new NotifManager("notif_delivery");
    public static NotifManager NotifPoint = new NotifManager("notif_point");



    [System.Serializable]
    public class InitializeData
    {
        public static InitializeData current
        {
            get
            {
                if (MarketData.current == null || MarketData.current.initializeData == null)
                    return LiffBridge.User.initializeData;
                else
                    return MarketData.current.initializeData;
            }
        }
        public InitializeData() { }

        public string defaultLanguage;
        public string loadingBgColor;
        public string primaryColor;
        public string secondaryColor;
        public string bgColor;
        public string themeId;
        public bool skipIframeThemeColor;
        public bool skipInappThemeColor;

        /*
            "loadingImg" : "path/....jpg",
            "loadingBg" : "path/....jpg",
            "consoleBtns" : "path/....jpg",
            "profileBtns" : "path/....jpg",
            "pointIcon" : "path/....jpg",
            "topspenderHeaderImg" : "path/....jpg",
            "bgImg" : "path/....jpg"
        */

        [System.Serializable]
        public class CustomAssetsName
        {
            public const string loadingImg = "loadingImg";
            public const string loadingBg = "loadingBg";
            public const string consoleBtns = "consoleBtns";
            public const string profileBtns = "profileBtns";
            public const string pointIcon = "pointIcon";
            public const string topspenderHeaderImg = "topspenderHeaderImg";
            public const string bgImg = "bgImg";
        }
        public Dictionary<string, string> customAssets;
        public Dictionary<string, Store.IframeData> htmlPages;
    }

    [System.Serializable]
    public class StoreInfo
    {
        public static StoreInfo current
        {
            get
            {
                if (MarketData.current == null || MarketData.current.storeInfo == null)
                    return LiffBridge.User.storeInfo;
                else
                    return MarketData.current.storeInfo;
            }
        }
        public StoreInfo() { }
        public string storeName;
        public string conditionURL;
        public string logoURL;
        public string storeCoverURL;

    }


    [System.Serializable]
    public class StoreSetting
    {
        public static StoreSetting current;

    }



    /*
     * {
    "user": {
        "id": 1102,
        "lineUserId": "U13ce7cd85dc3a7ad026c0878621932d5",
        "displayName": "Benz. 🏀",
        "pictureUrl": "https://profile.line-scdn.net/0hjMtsclj8NWJJCidQZkVKNXVPOw8-JDMqMT4vBD4IbAZiaXcyIjt9Vm4CP1I3aiY9ImV_BzsIagVh",
        "email": "",
        "phone": "092****966",
        "phoneVerified": 1,
        "points": 0,
        "storeName": "Mookept.shop (demo)",
        "memberSince": "2025-11-27T04:12:45.000Z",
        "tier": null
    },
    "deductionHistory": [],
    "redeemHistory": [],
    "userAddresses": []
     */


    public static bool IsLoaded { get { return User != null; } }
    public static UserData User;
    [System.Serializable]
    public class UserData
    {
        public UserData() { }
        public InfomationData user;
        public StatisticsData statistics;
        public List<DeductionHistoryData> deductionHistory;
        public List<RedeemHistoryData> redeemHistory;
        public List<UserAddressesData> userAddresses;
        //public Dictionary<string, object> customFields;  

        public bool isNewUser()
        {
            return true;
        }

        [System.Serializable]
        public class InfomationData
        {
            public InfomationData() { }
            public int id;
            public string lineUserId;
            public string displayName;
            public string pictureUrl;
            public string email;
            public string phone;
            public int phoneVerified;
            public int points;
            public string storeName;
            public string logoURL;
            public string memberSince;
            public TierData tier;
            public Dictionary<string, object> customFields;


            public enum genderType
            {
                male = 0, female = 1, other = 2
            }
            public genderType getGenderType()
            {
                switch (gender)
                {
                    case "male": return genderType.male;
                    case "female": return genderType.female;
                    default: return genderType.other;
                }
            }
            public string getGenderDisplay()
            {
                var g = getGenderType();
                var Items = GameStore.Language.Get("editprofile_dropdown_gender").DeserializeObjectSimple<List<string>>();
                return Items[(int)g];
            }




            //** customFields
            public string gender => customFields != null ? customFields.Find("gender").ToStr() : string.Empty;
            public bool hasbirthday => customFields != null && customFields.ContainsKey("birthday");
            public bool consent_notif => customFields != null ? customFields.Find(CRMApi.CustomFields.consent_notif).ToBool() : false;

            public System.DateTime GetBirthdayDate()
            {

                if (customFields.ContainsKey("birthday") && customFields["birthday"] is System.DateTime dt)
                {
                    return dt;
                }
                else
                {
                    if (System.DateTime.TryParse(
                        customFields["birthday"].ToStr(),
                        System.Globalization.CultureInfo.InvariantCulture,
                        System.Globalization.DateTimeStyles.None,
                        out System.DateTime birthdayDate))
                        return birthdayDate;
                    else
                        return System.DateTime.Now;
                }
            }


            public int age
            {
                get
                {
                    if (!hasbirthday) return 0;
                    var birth = GetBirthdayDate();
                    return Service.Time.TimeServer.master.Time.Year - birth.Year;
                }
            }

        }
        [System.Serializable]
        public class StatisticsData
        {
            public StatisticsData() { }
            public PointData points;
            public SpendingData spending;
            public OrderData orders;

            [System.Serializable]
            public class PointData
            {
                public PointData() { }
                /*
                    "available": 36,
                    "totalEarned": 46,
                    "pending": 53,
                    "redeemed": 5,
                    "apiDeducted": 10,
                    expiringSoon : {}
                 * */
                public int available;
                public int totalEarned;
                public int pending;
                public int redeemed;
                public int apiDeducted;
                public EpiringData expiringSoon;

                [System.Serializable]
                public class EpiringData
                {
                    public EpiringData() { }
                    public int total;
                    public BatchData nextBatch; // ได้ครบ
                    public BatchData[] batches; // ไม่ได้ daysLeft
                    [System.Serializable]
                    public class BatchData
                    {
                        public BatchData() { }
                        public int points;          // 12
                        public string expiresAt;    // 2026-02-11T00:00:00.000Z
                        public int daysLeft;        // 2
                    }
                }
            }
            [System.Serializable]
            public class SpendingData
            {
                public SpendingData() { }
                /*            
                    "total": 7000,
                    "currency": "THB"
                */
                public double total;
                public string currency;
            }
            [System.Serializable]
            public class OrderData
            {
                public OrderData() { }
                /*
                    "total": 2,
                    "approved": 1,
                    "pending": 1,
                    "byMarketplace": {}
                */
                public int total;
                public int approved;
                public int pending;
                public Dictionary<string, Marketplace> byMarketplace;
            }
            [System.Serializable]
            public class Marketplace
            {
                public Marketplace() { }
                /*
                    "count": 2,
                    "spending": 7000.00,
                    "points": 46
                */
                public int count;
                public double spending;
                public int points;

            }



        }
        [System.Serializable]
        public class DeductionHistoryData
        {
            public DeductionHistoryData() { }
            /*
            "id": 5,
            "transactionId": "REDEEM_25_1764327049815",
            "points": 2,
            "reason": "Redeem reward: Lin Kritiyanee01 (x1)",
            "status": "completed",
            "date": "2025-11-28T10:50:49.000Z"
            */
            public int id;
            public string transactionId;
            public int points;
            public string reason;
            public string status;
            public string date;
        }
        [System.Serializable]
        public class RedeemHistoryData
        {
            public RedeemHistoryData() { }
            /*
            "id": 26,
            "rewardId": 35,
            "rewardName": "Lin Kritiyanee01",
            "rewardDescription": "ของรางวัล",
            "rewardImage": "/uploads/rewards/1749803446950.jpeg",
            "pointsUsed": 2,
            "redeemedAt": "2025-11-28T10:50:49.000Z",
            "bagStatus": "shipped",
            "bagQuantity": 1
            */
            public int id;
            public int rewardId;
            public string rewardName;
            public string rewardDescription;
            public string rewardImage;
            public int pointsUsed;
            public string redeemedAt;
            public string bagStatus;
            public int bagQuantity;
        }
        [System.Serializable]
        public class UserAddressesData
        {
            public UserAddressesData() { }
#if UNITY_EDITOR
            public static UserAddressesData DefaultDemo()
            {
                CRMData.UserData.UserAddressesData address = new CRMData.UserData.UserAddressesData();
                address.contactPerson = "John Doe";
                address.addressLine1 = "123 Main Street";
                address.addressLine2 = "Building A, Floor 5";
                address.subdistrict = "Phaya Thai";
                address.district = "Phaya Thai";
                address.province = "Bangkok";
                address.postalCode = "10400";
                address.contactPhone = "0812345678";
                address.isDefault = false;
                return address;
            }
#endif
            /*
            "id": 32,
            "contactPerson": "Jane Updated",
            "addressLine1": "456 Updated Street",
            "addressLine2": "Building A, Floor 5",
            "subdistrict": "Phaya Thai",
            "district": "Phaya Thai",
            "province": "Bangkok",
            "postalCode": "10400",
            "contactPhone": "0812345678",
            "isDefault": true,
            "createdAt": "2025-11-28T06:33:30.000Z",
            "updatedAt": "2025-11-28T06:37:36.000Z"
            */
            public int id;
            public string contactPerson;
            public string addressLine1;
            public string addressLine2;
            public string subdistrict;
            public string district;
            public string province;
            public string postalCode;
            public string contactPhone;
            public bool isDefault;
            public string createdAt;
            public string updatedAt;

            public string FullString(bool hasPhone = true)
            {
                // สร้าง List เพื่อเก็บส่วนประกอบที่ไม่ว่างเปล่า
                var parts = new List<string>();

                if (!string.IsNullOrEmpty(addressLine1)) parts.Add(addressLine1);
                if (!string.IsNullOrEmpty(addressLine2)) parts.Add(addressLine2);
                if (!string.IsNullOrEmpty(subdistrict)) parts.Add(subdistrict);
                if (!string.IsNullOrEmpty(district)) parts.Add(district);

                // จังหวัดกับรหัสไปรษณีย์มักจะอยู่ติดกัน
                string provZip = "";
                if (!string.IsNullOrEmpty(province)) provZip += province;
                if (!string.IsNullOrEmpty(postalCode)) provZip += " " + postalCode;
                if (!string.IsNullOrEmpty(provZip)) parts.Add(provZip);

                // รวมทุกอย่างด้วยช่องว่าง (Space)
                string addressPart = string.Join(" ", parts);

                // คืนค่าพร้อมชื่อและเบอร์โทร
                if (hasPhone) return $"{contactPerson} {addressPart} (Tel: {contactPhone})";
                else return $"{contactPerson} {addressPart}";
            }
        }
    }

    [System.Serializable]
    public class GameProfile
    {
        public GameProfile() { }
        public string lineUserId;
        public int id;
        public string displayName;
        public string pictureUrl;
        public string email;
        public string phone;
    }
    [System.Serializable]
    public class Channel
    {
        public Channel() { }
        public const string own_channel = "own_channel";
        public const string shopee = "shopee";
        public const string lazada = "lazada";
        public const string tiktok = "tiktok";
    }

    [System.Serializable]
    public class MarketplaceData
    {
        public MarketplaceData() { }
        public string name;
        public string status; // connected
        public string connectedAt;
        public string updatedAt;
        public bool connected => status == "connected";
    }


    [System.Serializable]
    public class EarnedPointsTransactionData
    {
        public EarnedPointsTransactionData() { }
        public static List<EarnedPointsTransactionData> current;
        /*
         *               "id": 667,
                "orderId": "ORD-1764301834014-311",
                "channel": "own_channel",
                "amount": 8000,
                "points": 53,
                "status": "pending","approved"
                "orderStatus": null,
                "orderDate": "2025-11-28T03:50:34.000Z",
                "customerName": "Benz. 🏀",
                "customerPhone": "092****966",
                "createdAt": "2025-11-28T03:50:34.000Z",
                "updatedAt": "2025-11-28T03:50:34.000Z"
         * */

        [System.Serializable]
        public class Status
        {
            public const string pending = "pending";
            public const string approved = "approved";
        }
        public int id;
        public string orderId;
        public string channel;
        public double amount;
        public int points;
        public string status;
        public string orderStatus;
        public string orderDate;
        public string customerName;
        public string customerPhone;
        public string createdAt;
        public string updatedAt;


        public string GetChannelName()
        {
            switch (channel)
            {
                case Channel.own_channel: return "Owner Channel";
                case Channel.shopee: return "Shopee Channel";
                case Channel.lazada: return "Lazada Channel";
                case Channel.tiktok: return "Tiktok Channel";
                default:
                    return "Other Channel";
            }
        }


    }


    [System.Serializable]
    public class DeductionHistoryTransactionData
    {
        public DeductionHistoryTransactionData() { }
        public static List<DeductionHistoryTransactionData> current;
        public int id;
        public string transaction_id;
        public string reference_id;
        public int points_deducted;
        public int points_before;
        public int points_after;
        public string reason;
        public MetaData metadata;
        public string status;
        public string created_at;
        [System.Serializable]
        public class MetaData
        {
            // ใช้ Fields แทน Properties
            //public int order_id;
            //public int quantity;
            //public int reward_id;
            //public int address_id;
            public string reward_name;
        }
        public string GetTransactionName()
        {
            if (metadata != null && metadata.reward_name.notnull())
                return metadata.reward_name;
            else if (reason.notnull())
                return reason;
            return reference_id;
        }
    }


    [System.Serializable]
    public class MarketReward
    {
        public MarketReward() { }
        public static List<MarketReward> current;
        [System.Serializable]
        public class RewardType
        {
            public const string physical = "physical";
            public const string digital = "digital";
            public const string game = "game";
        }
        public int id;
        public string name;
        public string description;
        public string termsConditions;
        public string image;
        public int points;
        public int quantity;
        public string rewardType;
        public bool oneTimeRedemption;
        public bool tierRestricted;
        public bool tierEligible;
        public List<TierData> requiredTiers;
        public string startDate;
        public string endDate;
        public string status;// active / 
        public bool alreadyRedeemed;
        public bool canRedeem;
        public bool displayAtShop;
        public MetaData gameMetadata;
        [System.Serializable]
        public class MetaData
        {
            public MetaData() { }
            [System.Serializable]
            public class GameRewardType
            {
                public const string item = "item";
                public const string currency = "currency";
                public const string _event = "event";
                public const string gacha = "gacha";
            }
            public string gameId;
            public string itemId;
            public string type;
            public int amount;
            public bool displayAtShop;
        }

        public bool isPhysical => rewardType == RewardType.physical;
        public bool isDigital => rewardType == RewardType.digital;
        public bool isGame => rewardType == RewardType.game;
        public bool isGacha => isGame && gameMetadata != null && gameMetadata.type == MetaData.GameRewardType.gacha;
        public string gameId => gameMetadata != null ? gameMetadata.gameId : string.Empty;
        public bool isHasShipping => isPhysical;


        public Sprite GetRewardType()
        {
            if (isPhysical) return CustomerTheme.instance.iconRewardPhysical;
            if (isDigital) return CustomerTheme.instance.iconRewardDigital;
            if (isGame) return CustomerTheme.instance.iconRewardGame;
            return null;
        }



        public static MarketReward Get(int id)
        {
            return current.Find(x => x.id == id);
        }
        public bool IsCanDisplayAtShop()
        {
            return displayAtShop;
            
            #if UNITY_EDITOR
            return status == "active" && (gameMetadata == null || gameMetadata.displayAtShop);;
            #else
                if (isGame)
                    return status == "active" && points > 0 && (gameMetadata == null || gameMetadata.displayAtShop);
                else
                {
                    if (status == "active")
                    {
                        if (points > 0)
                        {
                            // ไม่ฟรี แสดงเลย
                            return true;
                        }
                        else
                        {
                            // ฟรี แต่มีเงื่อนไข tiers และ onetime... แสดงได้
                            return 
                            requiredTiers != null && 
                            requiredTiers.Count > 0 && 
                            oneTimeRedemption && 
                            tierEligible && 
                            !alreadyRedeemed;
                        }
                    }
                    return false;
                }
            #endif
        }
        public bool IsEnough(int amount = 1)
        {
            return User.user.points >= (points * amount);
        }
        public bool isCanRedeem()
        {
            bool ok = status == "active";
            if (!ok) return false;

            if (canRedeem)
            {
                ok = canRedeem;
                if (!ok) return false;
            }

            if (tierRestricted)
            {
                ok = tierEligible;
                if (!ok) return false;
            }


            if (oneTimeRedemption)
            {
                if (alreadyRedeemed)
                    return false;
            }


            return ok;
        }

        public string TierRequire()
        {
            string tireList = "";
            foreach (var tire in requiredTiers)
            {
                if (tireList.isnull())
                    tireList += tire.name;
                else
                    tireList += $", {tire.name}";
            }
            return tireList;
        }



    }




    [System.Serializable]
    [UnityEngine.Scripting.Preserve]
    public class ShippingOrderData
    {
        public ShippingOrderData() { }
        public class Status
        {
            public const string processing = "processing";
            public const string shipped = "shipped";
            public const string delivered = "delivered";
        }
        public static List<ShippingOrderData> current;
        public int id;
        public string status; //processing,shipped,delivered 
        public string trackingNumber;
        public string createdAt;
        public string updatedAt;
        public UserData.UserAddressesData shippingAddress;
        public List<RedemptionItem> items;

        [System.Serializable]
        public class RedemptionItem
        {
            public RedemptionItem() { }
            public int rewardId;
            public string name;
            public string description;
            public string image;
            public int points;
            public int quantity;
        }
    }

    [System.Serializable]
    public class CouponData
    {
        public CouponData() { }
        public int id;
        public int rewardId;
        public string rewardName;
        public string rewardDescription;
        public string rewardImage;
        public string rewardType;
        public int rewardPoints;
        public string couponCode;
        public string status;
        public string codeStatus;
        public string notes;
        public string usedAt;
        public string redeemedAt;
    }




    [System.Serializable]
    public class MarketData
    {
        public MarketData() { }
        public InitializeData initializeData;
        public StoreInfo storeInfo;
        public StoreSetting setting;
        public static MarketData current = new MarketData();
        public List<MarketReward> rewards;
        public BannerData banners;
        public List<TierData> tiers;
        public List<MarketplaceData> marketplaces;
        public List<GameCampaignData> gameCampaigns;
        public bool enableDailyCheckIn;
        public bool enableReferralCode;
    }




    [System.Serializable]
    public class BannerData
    {
        public BannerData() { }
        public static BannerData current;
        public List<CRMData.MarketBanner> marketBanners;
        public List<CRMData.GameBanner> gameBanners;
    }
    [System.Serializable]
    public class MarketBanner
    {
        public MarketBanner() { }
        public class Type
        {
            public const string ads = "ads";
            public const string promotion = "promotion";
            public const string banner = "banner";
        }
        public int id;
        public string name;
        public string description;
        public string type;
        public string imageUrl;
        public string bannerUrl;
        public int displayOrder;
        public string startDate;
        public string endDate;
        public string createdAt;
        public string updatedAt;
        public Dictionary<string, object> action;

        public enum ActionType
        {
            none, url, iframe, detail, game, reward, page, leaderboard, ocr
        }


        /*
        public (ActionType, Dictionary<string,object>) GetAction()
        {
            if (bannerUrl.notnull())
            {
                if (bannerUrl.Contains("action:"))
                {
                    var cmd = bannerUrl.Split(":")[1];
                    if (cmd.notnull() &&  CRMData.StoreSetting.current.bannersAction.ContainsKey(cmd))
                    {
                       var result = CRMData.StoreSetting.current.bannersAction[cmd];
                       var type = result.GetString("type").ToEnum<ActionType>(ActionType.none);
                       return (type, result );
                    }
                }
                else if (bannerUrl.Contains("http") || bannerUrl.Contains("www"))
                {
                    // htttps:\\www.gamestore.com/image/icon.png
                    var result = new Dictionary<string, object>();
                    result.Add("url",bannerUrl);
                    return (ActionType.url, result );
                }
            }
            return (ActionType.none, null);
        }
        */

        [JsonIgnore]
        ActionType? m_actionType = null;
        public ActionType GetActionType()
        {
            if (m_actionType == null)
            {
                if (action != null && action.ContainsKey("type"))
                {
                    m_actionType = action["type"].ToString().ToEnum<ActionType>(ActionType.none);
                }
                else
                {
                    m_actionType = bannerUrl.notnull() ? ActionType.url : ActionType.none;
                }
            }
            return m_actionType.Value;
        }

        /*
        public (ActionType, string) GetAction2()
        {
            if (bannerUrl.notnull())
            {
                if (bannerUrl.Contains("@") && bannerUrl.Contains("="))
                {
                    var cmd = bannerUrl.Split("@")[1];
                    var act = cmd.Split('=');
                    if (act[0] == "iframe")
                    {
                        // iframe:@https://www.1moby.com|0|0|%  
                        return (ActionType.iframe, act[1]);
                    }
                    if (act[0] == "detail")
                    {
                        // action:@detail
                        return (ActionType.detail, act.Length > 1 ? act[1] : string.Empty);
                    }
                    if (act[0] == "game")
                    {
                        // action:@game=game_mobirun
                        return (ActionType.game, act[1]);
                    }
                    if (act[0] == "reward")
                    {
                        // action:@game=game_mobirun
                        return (ActionType.reward, act[1]);
                    }
                    if (act[0] == "page")
                    {
                        // action:@page=getpoint
                        return (ActionType.page, act[1]);
                    }
                    if (act[0] == "leaderboard")
                    {
                        // action:@leaderboard=game_mobirun
                        return (ActionType.leaderboard, act[1]);
                    }
                    if (act[0] == "ocr")
                    {
                        // TagName|LableName
                        // TagName|LableName|Color
                        // action:@ocr=promotion|New Year Reward|#000000
                        return (ActionType.ocr, act[1]);
                    }
                }
                else if (bannerUrl.Contains("http") || bannerUrl.Contains("www"))
                {
                    // htttps:\\www.gamestore.com/image/icon.png
                    return (ActionType.url, bannerUrl);
                }
            }
            return (ActionType.none, string.Empty);
        }
        */
    }
    [System.Serializable]
    public class GameBanner
    {

    }





    [System.Serializable]
    public class TierData
    {
        public TierData() { }
        public static List<TierData> current;
        public static bool isTierReady => current != null && current.Count > 0;
        public int id;
        public string name;
        public int level;
        public string color;
        public string icon;
        public string benefitCard;
        public string description;
        public List<string> benefits;
        public List<RulesData> rules;
        public string createdAt;
        public string updatedAt;


        public int requireSpending
        {
            get
            {
                if (rules != null && rules.Count > 0)
                {
                    var find = rules.Find(x => x.ruleType == RulesData.Type.total_spending);
                    if (find != null)
                        return (int)find.value.ToDouble();
                }
                return 0;
            }
        }
        public int requireOrder
        {
            get
            {
                if (rules != null && rules.Count > 0)
                {
                    var find = rules.Find(x => x.ruleType == RulesData.Type.total_order);
                    if (find != null)
                        return find.value.ToInt();
                }
                return 0;
            }
        }

        [System.Serializable]
        public class RulesData
        {
            public RulesData() { }
            public class Type
            {
                public const string total_spending = "total_spending";
                public const string total_order = "total_order";
            }
            public string ruleType;
            public string value;
            public string includeChannels;
            public int isUpgradeRule;
        }


        /*
        public static void GetIconRank(string name, System.Action<Texture2D, Color> rankImage)
        {
            GetIconRank(current.Find(x => x.name == name), rankImage);
        }
        public static void GetIconRank(int level, System.Action<Texture2D, Color> rankImage)
        {
            GetIconRank(current.Find(x=>x.level == level) ,rankImage) ;
        }
        public static void GetIconRank(TierData data , System.Action<Texture2D,Color> rankImage)
        {
            if(data == null)
            {
                rankImage?.Invoke(CustomerTheme.instance.defaultRankImage, data.color.HexToColor());
            }
            else
            {
                GameStore.WebGLService.Download.OnLoadImage(data.icon, (img)=> {
                    if (img != null ?) rankImage?.Invoke(  img , Color.white );
                    else rankImage?.Invoke( CustomerTheme.instance.defaultRankImage, data.color.HexToColor());
                });
            }
        }
        */
        public void GetIcon(UnityEngine.UI.RawImage rawImage)
        {
            if (rawImage != null)
            {
                rawImage.texture = CustomerTheme.instance.defaultRankImage;
                rawImage.color = color.notnull() ? color.HexToColor() : Color.white;
                if (icon.notnull())
                {
                    GameStore.WebGLService.Download.OnLoadImage(icon, (img) =>
                    {
                        if (img != null)
                        {
                            rawImage.texture = img;
                            rawImage.color = Color.white;
                        }
                    });
                }

            }
        }
        public static TierData GetMyCurrentRank()
        {
            if (!isTierReady)
                return null;

            if (User != null && User.user.tier != null)
            {
                var myRank = current.Find(x => x.id == User.user.tier.id);
                if (myRank == null) return current[0];
                return myRank;
            }
            else
            {
                return current[0];
            }

        }
        public static TierData GetMyNextRank()
        {
            if (!isTierReady)
                return null;

            var myRank = GetMyCurrentRank();
            var next = myRank == null ? current[0] : current.Find(x => x.level > myRank.level);
            return next;
        }
        public static TierData GetRankByLevel(int level)
        {
            var myRank = current.Find(x => x.level == level);
            return myRank;
        }
        public static int CurrentLevelRank()
        {
            var currentRank = GetMyCurrentRank();
            return currentRank != null ? currentRank.level : 0;
        }
        public bool IsMaxRank()
        {
            return current.Find(x => x.level > this.level) == null;
        }
    }




    [System.Serializable]
    public class OCRStatusData
    {
        public OCRStatusData() { }
        public string jobId;
        public string status;
        public string createdAt;
        public string orderId; //... has only getlist
        public OCROrderData order;
        [System.Serializable]
        public class OCROrderData
        {
            public OCROrderData() { }
            public string orderId;
            public double totalAmount;
            public int pointsEarned;
            public string pointsStatus; // pending , approved  (EarnedPointsTransactionData.Status)
        }
    }


    [System.Serializable]
    public class ReferralCodeData
    {
        public int invitee;
        public int owner;
    }

    [System.Serializable]
    public class DailyData
    {
        public static DailyData current;

        public DailyData() { }
        public bool canCheckIn;
        public int currentStreak;
        public int currentStamina;
        public int nextDayIndex;
        //public DailyConfigData config;
        public static bool isHasDaily()
        {
            if (MarketData.current == null) return false;
            return MarketData.current.enableDailyCheckIn;
        }
        public static bool isCanCheckIn()
        {
            if (!isHasDaily()) return false;
            return current.canCheckIn;
        }


        /*
        [System.Serializable]
        public class DailyConfigData
        {
            public DailyConfigData() { }
            public int maxDaily;
            public bool restartAtCompleted;
            public DailyStaminaExchangeData staminaReward;
            public Dictionary<string, DailyRewardData> dailyReward;

            [System.Serializable]
            public class DailyStaminaExchangeData
            {
                public DailyStaminaExchangeData() { }
                public int requireStamina; // requireStamina
                public int pointBonus; // value for display
                public int pointItemId; // id item in reward
            }
            [System.Serializable]
            public class DailyRewardData
            {
                public DailyRewardData() { }
                public string type;
                public int amount;
            }
        }
        */

    }



    [System.Serializable]
    public class LeaderboardData
    {
        public LeaderboardData() { }
        public static LeaderboardData current;
        public List<TopspendingData> topspending;
        public Dictionary<string, List<GameLeaderboardData>> games;
    }
    [System.Serializable]
    public class TopspendingData
    {
        public TopspendingData() { }
        public int rank;
        public int userId;
        public double totalSpending;
        public int totalOrders;
        public int totalPointsEarned;
        public string memberSince;
        public string lastOrderDate;
        public string displayName;
        public string phone;
        public string pictureUrl;
        public TierData tier;
    }
    [System.Serializable]
    public class GameLeaderboardData
    {
        public GameLeaderboardData() { }
        public int index;
        public string statId;
        public string displayName;
        public long score;
        public long win;
        public long lose;
        public long lastWinTime;
        public GameProfile profile;
    }
    [System.Serializable]
    public class GameCampaignData
    {
        public GameCampaignData() { }
        public static GameCampaignData Get(string gameId)
        {
            return MarketData.current.gameCampaigns.Find(x => x.gameId == gameId);
        }
        [System.Serializable]
        public class Type
        {
            public const string game = "game";
            public const string gacha = "gacha";
        }
        [System.Serializable]
        public class Source
        {
            public const string bundle = "bundle";
            public const string webmodal = "webmodal";
        }
        public string gameId;
        public string model;
        public string url;
        public string icon;
        public string image;
        public string leaderboardCoverImage;
        public Dictionary<string, string> assets;
        public string type; //game,gacha
        public string source; // bundle/webmodal,
        public string name;
        public string description;
        public string contactCampaign;
        public string redeemRewardId;
        public long startAt;
        public long endAt;
        public bool leaderboard;
        public bool enable;

        public bool IsExprid()
        {
            var unix = GameStore.WebGLService.UnixTime;
            return !(unix >= startAt && unix <= endAt);
        }
        public bool IsCampaignAvalible()
        {
            return enable && !IsExprid();
        }
        public bool IsCanPlay()
        {
            if (type == Type.game)
                return IsCampaignAvalible();
            if (type == Type.gacha)
                return IsCampaignAvalible() && User.user.points >= GetPrice();
            return false;
        }
        public bool IsLeaderboard()
        {
            return leaderboard;
        }
        public int GetPrice()
        {
            if (redeemRewardId.notnull())
            {
                // gacha game use price by game reward....
                var find = MarketData.current.rewards != null ?
                    MarketData.current.rewards.Find(x => x.id.ToString() == redeemRewardId) : null;
                if (find != null) return find.points;
            }
            return 0;
        }
    }



}
















/*
public class Language
{
    private static Dictionary<string, string> Data = new Dictionary<string, string>();
    public static string Get(string key)
    {
        if (Data.ContainsKey(key))
            return Data[key];
        else
            return key;
    }

    public static void Set(string json)
    {
        Data = json.DeserializeObjectSimple<Dictionary<string, string>>();
    }
}
*/