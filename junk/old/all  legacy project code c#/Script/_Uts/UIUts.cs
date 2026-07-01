using System.Collections.Generic;


public class UIUts : UIChild
{
    static List<UIUts> list = new List<UIUts>();
    public enum Uts
    {
        currency, profile
    }

    bool init = false;
    public Uts uts;
    private void OnEnable()
    {
        if (!init)
        {
            list.Add(this);
            init = true;
        }


        if (uts == Uts.currency)
        {
            if (Label.Length > 0) Label[0].text = CRMData.User.user.points.ToString("#,##0");
            if (Images.Length > 0) Images[0].sprite = CustomerTheme.instance.iconCurrencyImage;
        }
        else if (uts == Uts.profile)
        {

            //** uid
            Label.AssignText("uid", $"Id : {GameStore.WebGLService.STATID}");

            //** crm user
            if (CRMData.User != null)
            {
                Label.AssignText("name", CRMData.User.user.displayName);
                Label.AssignText("phone", CRMData.User.user.phone);
                Label.AssignText("point", CRMData.User.user.points.ToString("#,##0"));
                RawImages.FindRawImage("profile", (ui) =>
                {
                    ui.DownloadTexture(CRMData.User.user.pictureUrl);
                });
            }

            //** rank
            var rank = CRMData.TierData.GetMyCurrentRank();
            if (rank != null)
            {
                Label.SetActive("tier", true);
                Label.SetActive("color", true);

                Label.AssignText("tier", rank.name);
                Images.FindImage("color", (ui) => { ui.color = rank.color.HexToColor(); });
                RawImages.FindRawImage("tier", (ui) =>
                {
                    rank.GetIcon(ui);
                });
            }
            else
            {
                Label.SetActive("tier", false);
                Label.SetActive("color", false);
            }

            //** check-in
            Buttons.SetActive("checkin", CRMData.DailyData.isCanCheckIn());

        }


    }




    public void OpenHome()
    {
        Console.instance.OpenPage(UIPage.PageType.Home);
    }
    public void OpenShop()
    {
        Console.instance.OpenPage(UIPage.PageType.Reward);
    }
    public void OpenProfile()
    {
        Console.instance.OpenPage(UIPage.PageType.Profile);
    }
    public void OpenGetPoint()
    {
        Console.instance.OpenPage(UIPage.PageType.GetPoint);
    }
    public void OpenLeaderboard()
    {
        Console.instance.OpenPage(UIPage.PageType.Leaderboard);
    }
    public void OpenDailyCheckin()
    {
        ProfilePage.OnOpenDailyCheckinPanel();
        //DailyCheckinPage.Open();
    }






    public static void OnUpdateAll(bool clean = true)
    {
        ProfilePage.OnUpdate();
        if (clean)
        {
            ShippingStatusPage.OnClearList();
            InventoryPage.OnClearList();
            PointHistoryPage.OnClearList();
        }
        foreach (var item in list)
        {
            item.OnEnable();
        }
    }




}
