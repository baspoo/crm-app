using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

public class RewardPage : UIPage
{
    public override void Open()
    {
        base.Open();
        Init();
    }
    void Init()
    {
        Render();
    }






    public CanvasGroup canvasGroup;
    public AutoResizeToChildren autoResize;
    public UILabel myPoint;
    public UITapToggle tap;
    public List<Transform> offsetBots;
    public ScrollRect scrollRect;
    public Transform contentReward;
    public Transform empty;
    //public GameObject perfabReward;
    bool initedPage = false;
    private void Render()
    {
        myPoint.text = User.user.points.ToString("#,##0");

        if (!initedPage)
        {
            api.LoadingYield();
            api.onGetRewards((rewards) =>
            {
                initedPage = true;
                tap.Init(0, Toggle);
            });
        }
        else
        {
            tap.Init(0, Toggle);
        }
    }
    public void Toggle(int index)
    {
        if (!openAwake) theme.SfxClick.Play();
        StartCoroutine(DoToggle(index));
    }

    List<UIChild> uis = new List<UIChild>();
    IEnumerator DoToggle(int index)
    {

        canvasGroup.alpha = 0;
        scrollRect.verticalNormalizedPosition = 1;
        UIChild.DeactivePool(contentReward);


        List<CRMData.MarketReward> displays = null;
        //** marketReward
        if (index == 0) displays = MarketData.rewards.FindAll(x => !x.isGame && x.IsCanDisplayAtShop());
        //** gameReward
        if (index == 1) displays = MarketData.rewards.FindAll(x => x.isGame && x.IsCanDisplayAtShop());


        empty.SetActive(displays == null || displays.Count == 0);
        foreach (var data in displays)
        {
            var ui = UIChild.Pool(theme.rewardObj, contentReward);
            ui.RawImages[0].texture = theme.defaultImage;
            LoadImage(ui.RawImages[0], data.image);
            //ui.RawImages[0].DownloadTexture(data.image);
            ui.Label[0].AssignText(data.name);
            ui.Label[1].AssignText(data.description);
            //ui.Label[2].AssignTextMergeName(data.quantity.ToString("#,##0"));

            if (data.points > 0)
                ui.Label[2].AssignLanguage("redeem_obj_btn_pay", $"{data.points.ToString("#,##0")}");
            else
                ui.Label[2].AssignLanguage("redeem_btn_free");

            //game
            ui.Trans[0].SetActive(false);
            if (data.isGame)
            {
                var gameCampaign = CRMData.GameCampaignData.Get(data.gameId);
                if (gameCampaign != null)
                {
                    ui.RawImages[1].texture = theme.defaultCampaignImage;
                    ui.Trans[0].SetActive(true);
                    if (gameCampaign.icon.notnull())
                    {
                        LoadImage(ui.RawImages[1], gameCampaign.icon);
                    }
                }
            }
            ui.Trans[1].SetActive(data.oneTimeRedemption);

            ui.onSelect = () =>
            {

                if (data.isGacha)
                {
                    var game = CRMData.GameCampaignData.Get(data.gameMetadata.gameId);
                    if (game != null) GameDetailPage.Open(game);
                }
                else RedeemPage.Open(data);

            };
            uis.Add(ui);
        }



        offsetBots.ForEach(x => x.SetAsLastSibling());
        yield return new WaitForEndOfFrame();
        autoResize.ResizeToFitChildren();
        yield return new WaitForEndOfFrame();
        this.DoObjectRefresh(contentReward.gameObject);
        yield return new WaitForEndOfFrame();
        canvasGroup.alpha = 1;
    }










    public void OnOrder()
    {

    }

    public void OpenHistory()
    {
        theme.SfxClick.Play();
        ShippingStatusPage.Open();
    }




}
