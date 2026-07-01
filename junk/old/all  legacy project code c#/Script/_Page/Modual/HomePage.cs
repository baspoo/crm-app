using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;


public class HomePage : UIPage
{
    public override void Begin()
    {
        base.Begin();
        canvasGroup.alpha = 0;
    }
    public override void Open()
    {
        base.Open();
        Init();
    }
    void Init()
    {
        if (!rendered)
        {
            StartCoroutine(Render());
            rendered = true;
        }
        else
        {
            StartCoroutine(Resize());
        }
    }







    [Header("Main")]
    public Transform root;
    public CanvasGroup canvasGroup;
    public AutoResizeToChildren resize;
    public GameObject HomeObj;

    [Header("Header-Cover")]
    public Transform HeaderDefault;
    public RectTransform RootCover;
    public RawImage ImageCover;

    [Header("Banner")]
    public ScrollAutoMoving BannerScroll;
    public GameObject BannerObj;

    [Header("Order")]
    public List<Transform> Top;
    public List<Transform> Bot;

    List<HomeObj> marketBanner;
    List<HomeObj> marketBotBanner;
    bool rendered = false;





    IEnumerator Resize()
    {
        foreach (var banner in marketBanner)
        {
            banner.Resize();
        }
        yield return new WaitForEndOfFrame();
        resize.WaitResizeToFitChildren();
    }
    IEnumerator Render()
    {

        marketBanner = new List<HomeObj>();
        marketBotBanner = new List<HomeObj>();


        // cover
        SetupCover();

        foreach (var banner in CRMData.BannerData.current.marketBanners)
        {
            if (banner.type == CRMData.MarketBanner.Type.banner)
            {
                //** create banner
                var ui = BannerObj.Create(BannerScroll.root).GetComponent<HomeObj>();
                ui.Init(this, banner);
                marketBotBanner.Add(ui);
            }
            else
            {
                //** create ads & promotion
                var ui = HomeObj.Create(root).GetComponent<HomeObj>();
                ui.SetActive(true);
                ui.transform.SetAsFirstSibling();
                ui.Init(this, banner);
                marketBanner.Add(ui);
            }
        }

        // hide unuse
        HomeObj.SetActive(false);
        BannerObj.SetActive(false);

        // bot banner active
        if (marketBotBanner.Count > 0)
        {
            yield return new WaitForEndOfFrame();
            BannerScroll.OnSetup();
            BannerScroll.SetActive(true);
        }
        else BannerScroll.SetActive(false);

        yield return new WaitForEndOfFrame();

        // order by admin & resize
        marketBanner.ForEach(x =>
        {
            x.transform.SetSiblingIndex(x.banner.displayOrder);
            x.Resize();
        });
        // order top
        Top.Count.Loop(i =>
        {
            Top[i].SetSiblingIndex(i);
        });
        // order bot
        Bot.ForEach(x => x.SetAsLastSibling());


        // resize
        yield return new WaitForEndOfFrame();
        resize.ResizeToFitChildren();

        // done
        canvasGroup.alpha = 1;
    }


    void SetupCover()
    {
        if (CRMData.StoreInfo.current.storeCoverURL.notnull())
        {
            //** use cover image
            RootCover.SetActive(true);
            HeaderDefault.SetActive(false);
            ImageCover.enabled = false;
            var difSize = ImageCover.rectTransform.sizeDelta.y - RootCover.sizeDelta.y;
            LoadImage(ImageCover, CRMData.StoreInfo.current.storeCoverURL, (img) =>
            {

                // size image
                if (img != null)
                {
                    ImageCover.enabled = true;
                    ImageCover.SetFixRatio(UIExtension.SetFixRatioType.lockWidth);
                    if (ImageCover.gameObject.activeInHierarchy && ImageCover.gameObject.activeSelf)
                    {
                        this.DoRefresh(
                            () =>
                            {
                                RootCover.SetSize(null, ImageCover.rectTransform.sizeDelta.y - difSize);
                            },
                            resize.WaitResizeToFitChildren
                            );
                    }
                }
                else
                {
                    //** use header default
                    RootCover.SetActive(false);
                    HeaderDefault.SetActive(true);
                }

            });
        }
        else
        {
            //** use header default
            RootCover.SetActive(false);
            HeaderDefault.SetActive(true);
        }

    }



    /*
    public void OnChoose2(CRMData.MarketBanner banner)
    {
        var (action, data) = banner.GetAction();
        switch (action)
        {
            case CRMData.MarketBanner.ActionType.none:
                break;
            case CRMData.MarketBanner.ActionType.url:
                //** ask??? --> copy or open
                UIRoot.instance.OpenExtarnalLink(data);
                break;
            case CRMData.MarketBanner.ActionType.iframe:
                
                if (data.notnull())
                {
                    var iframe = new Store.IframeData();
                    if (data.Contains('|'))
                    {
                        //data == "www.1moby.com|0|0|%"
                        var split = data.Split('|');
                        if (split.Length >= 1) iframe.path = split[0];
                        if (split.Length >= 3) iframe.size = new float[] { split[1].ToFloat(), split[2].ToFloat() };  
                        if (split.Length >= 4) iframe.unit = split[3].ifnull("%");
                    }
                    else
                    {
                        //data == "www.1moby.com"
                        iframe.path = data;
                    }
                    UIRoot.instance.OpenIframe(
                        iframe, 
                        new Dictionary<string, object>());
                }

                break;
            case CRMData.MarketBanner.ActionType.game:
                var game = CRMData.GameCampaignData.Get(data);
                if (game != null) GameDetailPage.Open(game);
                break;
            case CRMData.MarketBanner.ActionType.detail:
                GameDetailPage.Open(banner, data);
                break;
            case CRMData.MarketBanner.ActionType.reward:
                var reward = CRMData.MarketReward.Get(data.ToInt());
                if (reward != null) RedeemPage.Open(reward);
                break;
            case CRMData.MarketBanner.ActionType.page:
                6.Loop(i =>
                {
                    if (((PageType)i).ToString().ToLower() == data.ToLower())
                        Console.instance.OpenPage((PageType)i);
                });
                break;
            case CRMData.MarketBanner.ActionType.leaderboard:
                LeaderboardPage.OnDirect(data);
                break;
            case CRMData.MarketBanner.ActionType.ocr:
                string tag = null;
                string label = null;
                string bannerImg = null;
                if (data.notnull())
                {
                    //TagName|LableName
                    //TagName|LableName|Color
                    var split = data.Split('|');
                    if (split.Length >= 1) tag = split[0].ifnull(null);
                    if (split.Length >= 2) label = split[1].ifnull(null);
                    bannerImg = banner.imageUrl;
                }
                GetPointPage.OnUploadSlip(new List<string>() { tag }, label, bannerImg);
                break;
            default:
                break;
        }
    }
    */

    public void OnChoose(CRMData.MarketBanner banner)
    {
        var action = banner.GetActionType();
        var data = banner.action ?? new Dictionary<string, object>();

        switch (action)
        {
            case CRMData.MarketBanner.ActionType.none:
                break;
            case CRMData.MarketBanner.ActionType.url:
                //** ask??? --> copy or open
                UIRoot.instance.OpenExtarnalLink(banner.bannerUrl);
                break;
            case CRMData.MarketBanner.ActionType.iframe:
       
                //{
                //    "url":"xxxx",
                //    "size":[100,100],
                //    "unit":"%"
                //}
                
                var iframe = new Store.IframeData();
                iframe.path = data.GetString("url");
                iframe.size = new float[] { data.GetFloat("x", 90), data.GetFloat("y", 90) };
                iframe.unit = data.GetString("unit", "%");
                UIRoot.instance.OpenIframe( iframe,  new Dictionary<string, object>());
                break;
            case CRMData.MarketBanner.ActionType.game:

                //{
                //    "gameId":"xxxx"
                //}
                
                var game = CRMData.GameCampaignData.Get(data.GetString("gameId"));
                if (game != null) GameDetailPage.Open(game);
                break;
            case CRMData.MarketBanner.ActionType.detail:
                //{
                //    "moreUrl":"xxxx"
                //}
                GameDetailPage.Open(banner, data.GetString("moreUrl"));
                break;
            case CRMData.MarketBanner.ActionType.reward:
                //{
                //    "rewardId":37
                //}
                var reward = CRMData.MarketReward.Get(data.GetInt("rewardId"));
                if (reward != null) RedeemPage.Open(reward);
                break;
            case CRMData.MarketBanner.ActionType.page:
                //{
                //    "page":"xxxx"
                //}
                var page = data.GetString("pageName");
                6.Loop(i =>
                {
                    if (((PageType)i).ToString().ToLower() == page.ToLower())
                        Console.instance.OpenPage((PageType)i);
                });
                break;
            case CRMData.MarketBanner.ActionType.leaderboard:
                //{
                //    "gameId":"xxxx"
                //}
                LeaderboardPage.OnDirect(data.GetString("gameId"));
                break;
            case CRMData.MarketBanner.ActionType.ocr:
                //{
                //   "tag":"xxxx",
                //    "label":"xxxx",
                //   "imageUrl":"xxxx"
                //}
                string tag = data.GetString("tag");
                string label = data.GetString("labelName");
                string bannerImg = data.GetString("imageUrl",banner.imageUrl);
                GetPointPage.OnUploadSlip(new List<string>() { tag }, label, bannerImg);
                break;
            default:
                break;
        }
    }









}
