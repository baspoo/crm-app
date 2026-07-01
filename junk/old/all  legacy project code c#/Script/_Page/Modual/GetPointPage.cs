using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using GameStore;


public class GetPointPage : UIPage
{
    public override void Open()
    {
        base.Open();
        Init();
    }



 
    public List<UIToggle> platforms;
    void Init()
    {
        if (User.statistics.orders.byMarketplace == null)
        {
            //rootPlatform.SetActive(false);
            foreach (var platform in platforms)
                PlatformAvalible(platform, false);
            return;
        }

        foreach (var platform in platforms)
        {
            var marketplace = platform.name;
            bool linked = User.statistics.orders.byMarketplace.ContainsKey(marketplace);
            bool storeConnect = false;

            var find = MarketData.marketplaces.Find(x => x.name == marketplace);
            if (find != null && find.connected)
            {
                // store ผูกร้านแล้วจ้าาา...
                storeConnect = true;
                PlatformAvalible(platform, storeConnect);
            }
            if (storeConnect)
            {
                // user link แล้วจ้าาา...
                platform.IsEnable = linked;
            }
        }
    }
    void PlatformAvalible(UIToggle platform, bool avalible)
    {
        platform.IsEnable = false;
        platform.GetComponent<CanvasGroup>().alpha = avalible ? 1 : 0.2f;
        platform.GetComponent<Button>().interactable = avalible;
    }

    public void OnHistory()
    {
        theme.SfxClick.Play();
        PointHistoryPage.Open();
    }
    public void Help(string help)
    {
        if (help == "upload_slip")
        {
            UIRoot.instance.OpenIframe(Store.HtmlPages.HowToUploadSlip, new Dictionary<string, object>(), (json) => { });
        }
        if (help == "link_platform")
        {
            UIRoot.instance.OpenIframe(Store.HtmlPages.HowToLinkmarketplace, new Dictionary<string, object>(), (json) => { });
        }
    }













    public void UploadSlip()
    {
        theme.SfxClick.Play();
        var howtoUploadSlip = new Service.Permanence($"howtoUploadSlip_{CRMApi.GameId}");
        if (howtoUploadSlip.isHas)
        {
            OnUploadSlip();
        }
        else
        {
            Language.OpenPopup("getpoint_firsttime_howtoupload", () =>
            {
                // yes need view how to...
                Help("upload_slip");
                howtoUploadSlip.getBool = true;

            }, () =>
            {
                // no
                OnUploadSlip();
                howtoUploadSlip.getBool = true;


            }).ChangeBtnName("getpoint_firsttime_howtoupload_yes", "getpoint_firsttime_howtoupload_no");
        }
    }


    public static void OnUploadSlip(List<string> tags = null, string lable = null, string bannerImg = null)
    {
        CRMApi.instance.LoadingYield();
        CRMApi.instance.onGetOneTimeToken((token) =>
        {
            if (token.notnull())
            {

                var server = GameStore.WebGLService.NetworkService.serverURL;
                var payload = new Dictionary<string, object>();
                payload.Add("rootUrl", server);
                payload.Add("token", token);
                payload.AddIfExists("tags", tags);
                //payload.Add("isCheck", LiffBridge.User.storeInfo.isCheckAfterUploadOCR);
                payload.AddIfExists("label", lable);
                payload.AddIfExists("bannerImg", bannerImg);
                payload.Add("gameId", GameStore.Core.GameBundle.instance.gameConfigId);
                payload.Add("statId", GameStore.WebGLService.STATID);
                // ใช้กรณีให้ User เลือก Tag เอง
                // choiseTagsName ==> tags ต้องมีจำนวนเท่ากัน
                // payload.Add("choiseTagsName", new string[1] { "Promotion" });

                UIRoot.instance.OpenIframe(Store.HtmlPages.UploadSlip, payload, (json) =>
                {
                    UIUts.OnUpdateAll();
                });

            }
        });
    }




    public void LinkPlatform(UIToggle platform)
    {
        theme.SfxClick.Play();
        var marketplace = platform.name.ToEnum<CRMApi.marketplace>();


        var payload = new Dictionary<string, object>();
        payload.Add("marketplace", $"{marketplace}");
        UIRoot.instance.OpenIframe(Store.HtmlPages.LinkMarketplace, payload, (json) =>
        {
            //Debug.Log(json);
            if (json.notnull())
            {
                var data = json.ToDictStringObject();
                var status = data.Find("status").ToStr();
                if (status == "link")
                {
                    var orderId = data["orderId"].ToStr();
                    var zipcode = data["zipcode"].ToStr();
                    //var confirm = data["confirm"].ToBool();
                    api.onLinkmarketplace(marketplace, orderId, zipcode , (ok,linkStatus) =>
                    {

                        var datacallback = new Dictionary<string, object>();
                        datacallback["ok"] = ok;
                        datacallback["linkStatus"] = linkStatus;
                        GameStore.WebGLService.UpdateIFrame(datacallback);

                    });
                }
                else if (status == "success")
                {
                    api.LoadingYield();
                    api.onGetProfile((ok) =>
                    {
                        Init();
                    });
                }
                else if (status == "failed")
                {

                }
            }
        });

        /*
        CRMApi.instance.LoadingYield();
        CRMApi.instance.onGetOneTimeToken((token) => {
            if (token.notnull())
            {

                var server = GameStore.WebGLService.NetworkService.serverURL;
                var payload = new Dictionary<string, object>();
                payload.Add("marketplace", $"{marketplace}");
                payload.Add("rootUrl", server);
                payload.Add("token", token);
                UIRoot.instance.OpenIframe(CustomerTheme.instance.LinkMarketplace, payload, (json) => {
                    if(json.notnull())
                    {
                        var data = json.ToDictStringObject();
                        if(data.ContainsKey("ok") && data["ok"].ToBool() == true)
                        {

                        }
                    }
                });

            }
        });
        */

        /*
        UIRoot.instance.OpenIframe(theme.LinkMarketplace, payload, (json) => {

            if (json.notnull())
            {
                var data = json.ToDictStringObject();
                string orderId = data.Find("orderId").ToStr();
                string zipcode = data.Find("zipcode").ToStr();
                api.LoadingYield();
                api.onLinkmarketplace(marketplace, orderId, zipcode, false, (ok) => {

                    if (ok)
                    {
                        api.LoadingYield();  
                        UIPopup.Open("พบคำสั่งซื้อที่ตรงกัน", "คุณสามารถเชื่อมต่อกับหมายเลขคำสั่งซื้อนี้ได้ ต้องการเชื่อมต่อกับร้านค้านี้หรือไม่",  theme.iconCompleteImage , () => {
                            api.onLinkmarketplace(marketplace, orderId, zipcode, true, (ok) => { });
                        }, () => { });
                    }
                    else
                    {
                        UIPopup.Open("ไม่สามารถเชื่อมต่อได้", "หมายเลขคำสั่งซื้อ (Order ID) หรือ รหัสไปรษณี (Zipcode) อาจไม่ถูกต้อง กรุณาตรวจสอบอีกครั้ง และทำการเชื่อมต่อใหม่");
                    }

                });
            }

        });
        */

    }










}
