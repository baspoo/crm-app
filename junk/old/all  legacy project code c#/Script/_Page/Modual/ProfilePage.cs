using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Linq;
using GameStore;

public class ProfilePage : UIPage
{
    static ProfilePage instance;
    public override void Open()
    {
        instance = this;
        base.Open();
        Init();
    }
    void Init()
    {
        Render();
    }
    public static void OnUpdate()
    {
        instance?.Render();
    }








    [Header("Main")]
    public AutoResizeToChildren root;



    private void Render()
    {
        var user = User;

        Debug.Log(user.user.customFields.SerializeToJsonSimple());
        DisplayProfile(user.user);
        DisplayRank(user.user, user.statistics);
        DisplayDailyCheckin();
        DisplayReferal();
        DisplayAddress(user.userAddresses);
        DisplayCustom();
        RefreshTable();
    }

    public void RefreshTable()
    {
        //** resize content
        this.DoRefresh(
            rootAddress.WaitResizeToFitChildren,
            rootCustomFields != null ? rootCustomFields.root.WaitResizeToFitChildren : () => { },
            root.WaitResizeToFitChildren,
            root.WaitResizeToFitChildren
            );
    }




    [Header("Profile")]
    public RawImage imgProfile;
    public TMPro.TMP_InputField fullname;
    public TMPro.TMP_InputField phone;
    public TMPro.TMP_InputField email;
    public TMPro.TMP_InputField age;
    public TMPro.TMP_InputField gender;
    public List<Transform> genderIcon;
    public Transform tNotifInventory;
    private void DisplayProfile(CRMData.UserData.InfomationData infomation)
    {
        imgProfile.DownloadTexture(infomation.pictureUrl);
        fullname.text = (infomation.displayName);
        phone.text = (infomation.phone);
        gender.text = (infomation.getGenderDisplay());
        email.text = (infomation.email);
        age.text = (infomation.age.ToString());
        genderIcon.Open((int)infomation.getGenderType());
    }



    [Header("DailyCheckin")]
    public UIChild rootdailyCheckin;
    private void DisplayDailyCheckin()
    {
        if (rootdailyCheckin == null) return;
        if (CRMData.DailyData.isHasDaily())
        {
            rootdailyCheckin.SetActive(true);
            CRMData.DailyData dailyCheckin = CRMData.DailyData.current;
            rootdailyCheckin.Label[0].text = dailyCheckin.currentStreak.ToString();
            rootdailyCheckin.Toggles[0].IsEnable = dailyCheckin.canCheckIn;
        }
        else
        {
            rootdailyCheckin.SetActive(false);
        }
    }
    public void OnOpenDailyCheckin()
    {
        theme.SfxClick.Play();
        OnOpenDailyCheckinPanel();
    }
    public static void OnOpenDailyCheckinPanel()
    {
        var payload = new Dictionary<string, object>();
        UIRoot.instance.OpenIframe(Store.HtmlPages.DailyCheckIn, payload, (json) =>{
            if (json != null)
            {
                var data = json.ToDictStringObject();
                if (data.GetBool("checkin") && data.ContainsKey("current")){
                    CRMData.DailyData.current = data["current"].DeserializeObjectSimple<CRMData.DailyData>();
                }
            }
        });
    }



    [Header("Referal")]
    public UIChild rootReferal;
    private void DisplayReferal()
    {

        if (rootReferal == null) return;
        if (CRMData.MarketData.current.enableReferralCode)
        {
            rootReferal.SetActive(true);
        }
        else
        {
            rootReferal.SetActive(false);
        }
    }
    public void OnOpenReferal()
    {
        var payload = new Dictionary<string, object>();
        payload.Add("mode", "me");
        UIRoot.instance.OpenIframe(Store.HtmlPages.Referal, payload);
        theme.SfxClick.Play();
    }
    public static void OnOpenInviteReferal(System.Action done)
    {
        if (CRMData.MarketData.current.enableReferralCode)
        {
            var payload = new Dictionary<string, object>();
            payload.Add("mode", "inviteCode");
            UIRoot.instance.OpenIframe(Store.HtmlPages.Referal, payload, (json) =>
            {
                done?.Invoke();
            });
        }
        else
        {
            done?.Invoke();
        }
    }







    [Header("Rank")]
    public Transform rootRank;
    public UILabel remainPoint;
    public UILabel totalPoint;
    public UILabel totalRequirePoint;
    public UILabel totalSpending;
    public UILabel totalSpendingCurrency;
    public UILabel rankName;
    public Transform tRankup;
    public Transform expiringRoot;
    public UILabel expiringPoint;
    public UILabel expiringDate;
    public UILabel expiringDay;
    public RawImage iconRank;
    public Image progressRank;
    public Transform tNextRank;

    private void DisplayRank(CRMData.UserData.InfomationData infomation, CRMData.UserData.StatisticsData statisticsData)
    {

        //** rank
        if (!CRMData.TierData.isTierReady)
        {
            rootRank.SetActive(false);
        }
        else
        {
            rootRank.SetActive(true);
            var current = CRMData.TierData.GetMyCurrentRank();
            var next = CRMData.TierData.GetMyNextRank();

            //** load icon
            current.GetIcon(iconRank);

            if (current != null) rankName.AssignTextMergeName(current.name);
            else rankName.AssignText("None");


            //**point remain.
            if (User != null && User.user != null)
            {
                remainPoint.AssignText(User.user.points.ToString("#,##0"));
            }
            else
            {
                remainPoint.AssignText("0");
            }


            //** spending total.
            double totalSpending = 0;
            if (statisticsData.points != null)
            {
                //** is total spend... rank
                totalSpending = statisticsData.spending != null ? statisticsData.spending.total : 0;
                totalPoint.AssignText(totalSpending.ToString("#,##0"));
            }
            else
            {
                totalPoint.AssignText("0");
            }

            // next
            if (next != null)
            {
                var percent = ((float)totalSpending / (float)next.requireSpending);
                Debug.Log(percent);
                progressRank.fillAmount = percent;
                totalRequirePoint.AssignText($"/{next.requireSpending.ToString("#,##0")}");
                tNextRank.SetActive(true);
                tRankup.SetActive(percent >= 1.0f);
            }
            else
            {
                progressRank.fillAmount = 1.0f;
                totalRequirePoint.AssignText("/Max");
                tNextRank.SetActive(false);
                tRankup.SetActive(false);
            }

        }







        //** expiring point
        if (statisticsData.points.expiringSoon != null && statisticsData.points.expiringSoon.nextBatch != null)
        {
            expiringPoint.AssignText($"{statisticsData.points.expiringSoon.nextBatch.points}");
            expiringDay.AssignLanguage("profile_obj_point_expire_count", statisticsData.points.expiringSoon.nextBatch.daysLeft);
            expiringDate.AssignText($"{statisticsData.points.expiringSoon.nextBatch.expiresAt.ToStringDateTimeNormalize(false)}");
            expiringRoot.SetActive(true);
        }
        else
        {
            expiringRoot.SetActive(false);
        }




        // spending
        if (statisticsData.spending != null)
        {
            totalSpending.AssignText($"{statisticsData.spending.total.ToString("#,##0")}");
            totalSpendingCurrency.AssignText(statisticsData.spending.currency);
        }
        else
        {
            totalSpending.AssignText("0");
            totalSpendingCurrency.AssignText("");
        }




    }





    [Header("Address")]
    public AutoResizeToChildren rootAddress;
    public Transform rootAddressItem;
    public Transform emptyAddress;
    //public GameObject addressObj;
    public int maxAddressDisplay = 3;
    private void DisplayAddress(List<CRMData.UserData.UserAddressesData> addressesDatas)
    {
        UIChild.DeactivePool(rootAddressItem);
        if (addressesDatas != null && addressesDatas.Count > 0)
        {
            int createIndex = 0;
            emptyAddress.SetActive(false);
            foreach (var address in addressesDatas.OrderByDescending(x => x.isDefault))
            {
                if (createIndex < maxAddressDisplay)
                    DisplayAddress(rootAddressItem, address);
                createIndex++;
            }
        }
        else emptyAddress.SetActive(true);
    }
    public enum AddressAction
    {
        edit,
        choose
    }
    public static UIChild DisplayAddress(Transform root, CRMData.UserData.UserAddressesData address,
        AddressAction? action = null, System.Action onClick = null, System.Action onRemoved = null)
    {
        var ui = UIChild.Pool(CustomerTheme.instance.addressObj, root);
        ui.Label[0].AssignLanguage("adress_obj_title_address", address.FullString());
        ui.Trans.Close();
        ui.Trans[0].SetActive(address.isDefault);
        ui.Trans[1].SetActive(action != null && action.Value == AddressAction.edit);
        ui.Trans[2].SetActive(action != null && action.Value == AddressAction.choose);
        ui.Buttons[0].interactable = onClick != null;
        ui.Buttons[1].SetActive(onRemoved != null);
        ui.onSelect = onClick;
        ui.onAction = (act) =>
        {
            if (act == "remove")
            {
                GameStore.Language.OpenPopup("CONFIRM_REMOVE_ADDRESS", () =>
                {
                    // yes
                    UIRoot.instance.OnLoading(true); //** 2 api (remove & re-get)
                    CRMApi.instance.onRemoveAddress(address.id, (ok) =>
                    {
                        UIRoot.instance.OnLoading(false);
                        if (ok)
                        {
                            onRemoved?.Invoke();
                        }
                        else
                        {
                            GameStore.Language.OpenPopup("adress_msg_delete_faild", () => { });
                        }
                    });
                }, () =>
                {
                    // no
                });

            }
        };
        return ui;
    }
    public void OnAddNewAdress()
    {
        EditProfilePage.AddNewAddress(null, (modify) =>
        {
            if (modify)
                Init();
        });
    }
    public void OnOpenEditAddress()
    {
        theme.SfxClick.Play();
        EditProfilePage.Open((modify) =>
        {
            Init();
        }).OnSnapBot();
    }


    [Header("Custom")]
    public ProfileCustomGroup rootCustomFields;
    public void DisplayCustom()
    {
        if (rootCustomFields != null)
            rootCustomFields.Init(this);
    }









    public void OnmyQR()
    {
        theme.SfxClick.Play();
        OpenMyQR();
    }

    static CRMData.EarnedPointsTransactionData lastEarndPoint = null;
    public static void OpenMyQR()
    {
        var payload = new Dictionary<string, object>();
        var quary = $"text={GameStore.WebGLService.STATID}";
        quary += $"&name={CRMData.User.user.displayName}";
        quary += $"&email={CRMData.User.user.email}";
        quary += $"&phone={CRMData.User.user.phone}";
        quary += $"&birthday={CRMData.User.user.GetBirthdayDate().ToShortDateString()}";
        quary += $"&gender={CRMData.User.user.gender}";
        quary += $"&imageprofile={CRMData.User.user.pictureUrl}";
        quary += $"&storename ={CRMData.User.user.storeName}";
        quary += $"&primaryColor={CRMData.InitializeData.current.primaryColor}";
        quary += $"&secondaryColor={CRMData.InitializeData.current.secondaryColor}";
        var iframe = Store.IframeData.Get(Store.HtmlPages.QRcode).Clone();
        iframe.path = $"{iframe.path}?{quary}";

        // get last Earn....
        lastEarndPoint = null;
        CRMApi.instance.LoadingYield();
        CRMApi.instance.onGetEarnedPoints(1, 0, (order, more) =>
        {
            if (order != null && order.Count > 0)
                lastEarndPoint = order[0];

            //** Open Iframe
            UIRoot.instance.OpenIframe(iframe, payload, (json) =>
            {
                // get check last Earn.... again
                CRMApi.instance.onGetEarnedPoints(1, 0, (order, more) =>
                {
                    if (order != null && order.Count > 0)
                    {
                        if (lastEarndPoint == null || lastEarndPoint.id != order[0].id)
                        {
                            // last Earn....is new!!
                            OpenPopupGetPointRewards(order[0].points);
                        }
                    }
                    lastEarndPoint = null;
                });
            });
        });
    }

    public static void OpenPopupGetPointRewards(int points)
    {
        CRMApi.instance.onGetProfile((ok) =>
        {
            object[] language = new object[2] { "{value}", points };
            GameStore.Language.OpenPopup("EARNPOINT_COMPLETED", CustomerTheme.instance.iconCompleteImage, args: language);
            UIUts.OnUpdateAll();
        });
    }






    public void OnEditProfile()
    {
        theme.SfxClick.Play();
        EditProfilePage.Open((modify) =>
        {
            Init();
        }).OnSnapTop();
    }
    public void OnOpenExpiring()
    {
        theme.SfxClick.Play();

        var payload = new Dictionary<string, object>();
        var statisticsData = CRMData.User.statistics;
        if (statisticsData.points != null)
        {
            payload.Add("point_totalEarned", statisticsData.points.totalEarned);
            payload.Add("point_remainAvailable", statisticsData.points.available);
            payload.Add("point_pending", statisticsData.points.pending);
            payload.Add("point_redeemed", statisticsData.points.apiDeducted + statisticsData.points.redeemed);
        }
        // spending
        if (statisticsData.spending != null)
        {
            payload.Add("point_totalSpending", $"{statisticsData.spending.total.ToString("#,##0")} {statisticsData.spending.currency}");
        }
        // expiring point
        if (statisticsData.points.expiringSoon != null)
        {
            payload.Add("point_expiringSoon_point", statisticsData.points.expiringSoon.nextBatch.points);
            payload.Add("point_expiringSoon_daysLeft", statisticsData.points.expiringSoon.nextBatch.daysLeft);
            payload.Add("point_expiringSoon_expiresAt", statisticsData.points.expiringSoon.nextBatch.expiresAt);

            if (statisticsData.points.expiringSoon.batches != null)
            {
                payload.Add("point_expiringSoon_list", statisticsData.points.expiringSoon.batches);
            }
        }


        UIRoot.instance.OpenIframe(Store.HtmlPages.Expiring, payload, (json) =>
        {

        });

    }
    public void OnRankDetail()
    {
        if (CRMData.TierData.isTierReady)
        {
            theme.SfxClick.Play();
            var payload = new Dictionary<string, object>();

            // current rank
            var current = CRMData.TierData.GetMyCurrentRank();
            if (current != null)
                payload.Add("current", current.id.ToString());

            // all rank
            foreach (var tier in CRMData.TierData.current)
            {
                var tierData = new Dictionary<string, object>();
                tierData.Add("id", tier.id);
                tierData.Add("name", tier.name);
                tierData.Add("description", tier.description);
                tierData.Add("color", tier.color);
                tierData.Add("icon", tier.icon);
                tierData.Add("level", tier.level);
                tierData.Add("benefits", tier.benefits);
                tierData.Add("benefitCard", tier.benefitCard);
                tierData.Add("requireSpending", tier.requireSpending);
                tierData.Add("requireOrder", tier.requireOrder);
                payload.Add(tier.id.ToString(), tierData);
            }

            UIRoot.instance.OpenIframe(Store.HtmlPages.Rank, payload, (json) =>
            {

            });
        }
    }
    public void OnNotification()
    {
        theme.SfxClick.Play();
        InventoryPage.Open(false);
    }
    public void OnPointHistory()
    {
        theme.SfxClick.Play();
        PointHistoryPage.Open();
    }
    public void OnDelivertHistory()
    {
        theme.SfxClick.Play();
        ShippingStatusPage.Open();
    }

    public void OnSetting()
    {
        theme.SfxClick.Play();
        SettingPage.Open();
    }









}
