using GameStore.Core;
using UnityEngine;
using UnityEngine.UI;

[UnityEngine.Scripting.Preserve]
public class GameDetailPage : UISubPage
{
    public static GameDetailPage instance;
    public static void Open(CRMData.GameCampaignData gameCampaignData)
    {

        instance = Create<GameDetailPage>(instance, CustomerTheme.instance.GameDetailPage);
        instance.Init(gameCampaignData);
    }
    public static void Open(CRMData.MarketBanner banner, string gotoURL)
    {

        instance = Create<GameDetailPage>(instance, CustomerTheme.instance.GameDetailPage);
        instance.Init(banner, gotoURL);
    }

    public void ClosePage()
    {
        theme.SfxClose.Play();
        Hide();
    }



    [Header("Info")]
    public Transform rootInfo;
    public Animator animPage;
    public RawImage icon;
    public UILabel txtHeader;
    public UILabel txtDate;
    public Transform tTimeLeft;
    public UILabel txtTimeLeft;
    public UILabel txtDescription;
    public RawImage image;
    public Transform tImgPointer;
    public UIChild zone_game;
    public UIChild zone_gacha;
    public UIChild zone_contact;
    public UIChild zone_ending;
    public UIChild zone_detail;




    [Header("Gacha Rate")]
    public Transform rootGachaRate;
    public Transform contentGachaRate;
    public GameObject prefabGachaRate;







    CRMData.GameCampaignData gameCampaignData;
    void Init(CRMData.GameCampaignData gameCampaignData)
    {
        gameObject.SetActive(true);
        rootInfo.Open(rootGachaRate);
        theme.SfxOpen.Play();
        this.gameCampaignData = gameCampaignData;
        txtHeader.text = gameCampaignData.name;
        txtDescription.text = gameCampaignData.description;

        this.DoUpdate(1, (x) =>
        {
            txtDescription.transform.position = tImgPointer.position;
        });



        // icon
        icon.texture = theme.defaultCampaignImage;
        if (gameCampaignData.icon.notnull())
            icon.DownloadTexture(gameCampaignData.icon, (img) =>
            {
                if (img != null) icon.enabled = true;
            });



        // image
        //image.enabled = false;
        image.texture = theme.defaultImage;
        image.DownloadTexture(gameCampaignData.image, (img) =>
        {
            if (img != null)
            {
                //image.enabled = true;
                image.SetFixRatio(UIExtension.SetFixRatioType.lockWidth);
                image.DoRefresh(AdjustSize);
            }
        });

        var startAt = gameCampaignData.startAt.ToDateTime();
        var endAt = gameCampaignData.endAt.ToDateTime();
        var day = endAt - endAt;
        txtDate.text = $"{startAt.ToLocalTime().ToString("dd MMM yyyy")} - {endAt.ToLocalTime().ToString("dd MMM yyyy")}";
        txtTimeLeft.AssignLanguage("gameinfo_event_end_count", $"{day.TotalDays}");
        txtDate.SetActive(true);
        tTimeLeft.SetActive(true);


        zone_game.SetActive(false);
        zone_gacha.SetActive(false);
        zone_contact.SetActive(false);
        zone_ending.SetActive(false);
        zone_detail.SetActive(false);



        if (gameCampaignData.IsExprid())
        {
            // end season..
            if (gameCampaignData.contactCampaign.notnull())
            {
                zone_contact.SetActive(true);
            }
            else
            {
                zone_ending.SetActive(true);
            }
        }
        else
        {
            // avalible season..
            if (gameCampaignData.type == CRMData.GameCampaignData.Type.game)
            {
                zone_game.SetActive(true);
                zone_game.Toggles[0].IsEnable = gameCampaignData.IsCanPlay();
                zone_game.Toggles[1].SetActive(gameCampaignData.IsLeaderboard());
            }
            if (gameCampaignData.type == CRMData.GameCampaignData.Type.gacha)
            {
                zone_gacha.SetActive(true);
                zone_gacha.Toggles[0].IsEnable = gameCampaignData.IsCanPlay();
                zone_gacha.Label[0].AssignTextMergeName(gameCampaignData.GetPrice().ToString("#,##0"));
            }
        }
    }
    public void OnContact()
    {
        UIRoot.instance.OpenExtarnalLink(gameCampaignData.contactCampaign);
    }
    public void OnStartGame()
    {
        if (gameCampaignData.type == CRMData.GameCampaignData.Type.game)
        {
            //** onClick : start game
            api.LoadingYield();
            api.onGotoGame(gameCampaignData.gameId, (data) =>
            {
                GameStore.WebGLService.OpenURL(data.gameURL, false);
            });
        }
    }
    public void OnGameLeaderboard()
    {
        Hide();
        LeaderboardPage.OnDirect(gameCampaignData.gameId);
    }




    [SerializeField] float totalHight = 860;
    void AdjustSize()
    {
        this.DoRefresh(() =>
        {
            // คำนวณ TMP width ใหม่
            Debug.Log(totalHight);
            Debug.Log(image.rectTransform.sizeDelta.x);
            float newHight = totalHight - image.rectTransform.sizeDelta.x;
            Debug.Log(newHight);

            txtDescription.rectTransform.sizeDelta = new Vector2(txtDescription.rectTransform.sizeDelta.x, newHight);

        });

    }








    public void OnGachaRate()
    {
        theme.SfxClick.Play();

        api.LoadingYield();
        api.onGetGachaRate(gameCampaignData.gameId, (datas) =>
        {

            //** open page...
            animPage.Play("next", 0, 0);
            rootGachaRate.Open(rootInfo);
            UIChild.DeactivePool(contentGachaRate);
            foreach (var data in datas)
            {
                var ui = UIChild.Pool(prefabGachaRate, contentGachaRate);
                ui.SetActive(false);
                ui.Label[1].AssignLanguage("gameinfo_gacha_subtitle_rate", data.weight.ToString());

                if (data.type == GachaData.TYPE.shopdigital)
                {
                    //** shopdigital
                    var marketReward = CRMData.MarketReward.Get(data.reward.ToInt());
                    if (marketReward != null)
                    {
                        ui.SetActive(true);
                        ui.Label[0].text = marketReward.name;
                        ui.RawImages[0].texture = theme.defaultImage;
                        ui.RawImages[0].DownloadTexture(marketReward.image);
                    }
                }
                else
                {
                    ui.SetActive(true);
                    ui.Label[0].text = data.name;
                    ui.RawImages[0].texture = theme.defaultImage;
                    ui.RawImages[0].DownloadTexture(data.image);
                }


            }

        });
    }
    public void OnBackToInfo()
    {
        theme.SfxClick.Play();
        rootInfo.Open(rootGachaRate);
        animPage.Play("prev", 0, 0);
    }
    public void OnOpenGacha()
    {
        if (gameCampaignData.type == CRMData.GameCampaignData.Type.gacha)
        {
            Hide();
            UIGachaOpening.Open(gameCampaignData);
        }
    }












    string gotoURL;
    void Init(CRMData.MarketBanner banner, string gotoURL)
    {
        this.gotoURL = gotoURL;
        rootInfo.Open(rootGachaRate);
        theme.SfxOpen.Play();
        this.gameCampaignData = null;
        txtHeader.text = banner.name;
        txtDescription.text = banner.description;
        this.DoUpdate(1, (x) =>
        {
            txtDescription.transform.position = tImgPointer.position;
        });


        // icon
        icon.texture = theme.defaultImage;
        icon.DownloadTexture(CRMData.StoreInfo.current.logoURL);



        // image
        // image.enabled = false;
        image.texture = theme.defaultImage;
        image.DownloadTexture(banner.imageUrl, (img) =>
        {
            if (img != null)
            {
                //image.enabled = true;
                image.SetFixRatio(UIExtension.SetFixRatioType.lockWidth);
                image.DoRefresh(AdjustSize);
            }
        });


        var startAt = banner.startDate.ToDateTime();
        var endAt = banner.endDate.ToDateTime();
        txtDate.text = $"{startAt.ToLocalTime().ToString("dd MMM yyyy")} - {endAt.ToLocalTime().ToString("dd MMM yyyy")}";
        txtDate.SetActive(true);
        tTimeLeft.SetActive(false);

        zone_game.SetActive(false);
        zone_gacha.SetActive(false);
        zone_contact.SetActive(false);
        zone_ending.SetActive(false);
        zone_detail.SetActive(gotoURL.notnull());
    }

    public void OnOpenDetail()
    {
       UIRoot.instance.OpenExtarnalLink(gotoURL );
    }




}
