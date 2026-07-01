using System.Collections.Generic;
using GameStore;
using GameStore.Core;
using UnityEngine;

public class UIGachaOpening : MonoBehaviour
{


    public static void Open(CRMData.GameCampaignData gameCampaignData)
    {
        var payload = new Dictionary<string, object>();
        payload.Add("price", $"{gameCampaignData.GetPrice()}");
        var iframe = new Store.IframeData();
        iframe.path = gameCampaignData.url;
        iframe.size = new  float[] { 100, 100 };
        UIRoot.instance.OpenIframe(iframe, payload, (json) =>
        {
            Debug.Log($"OpenIframe : {json}");
            if (json.notnull())
            {
                var data = json.ToDictStringObject();
                var status = data.Find("action").ToStr();
                if (status == "open")
                {
                    OPENGACHA(gameCampaignData, (ok, displayName, image) =>
                    {
                        var datacallback = new Dictionary<string, object>();
                        datacallback["ok"] = ok;
                        if (ok)
                        {
                            datacallback["displayName"] = displayName;
                            datacallback["image"] = image;
                        }
                        GameStore.WebGLService.UpdateIFrame(datacallback);
                    });
                }
            }
        });
    }


    static void OPENGACHA(CRMData.GameCampaignData gameCampaignData, System.Action<bool, string, string> callback)
    {
        CRMApi.instance.onOpenGacha(gameCampaignData, (reward) =>
        {
            UIUts.OnUpdateAll();
            if (reward != null)
            {
                Debug.Log($"Gacha Result : --> {reward.reward}");
                Debug.Log($"Gacha Result : --> {reward.type}");
                if (reward.type == GachaData.TYPE.shopdigital)
                {
                    // shop-digital : use marketReward to display
                    var marketReward = CRMData.MarketReward.Get(reward.reward.ToInt());
                    if (marketReward != null)
                    {
                        var rewardName = marketReward.name;
                        var image = reward.image.ifnull(marketReward.image);
                        callback?.Invoke(true, rewardName , image );
                    }
                }
                else
                {
                    // other : use name/image by GachaData to display
                    callback?.Invoke(true, reward.name, reward.image);
                }
            }
            else
            {
                callback?.Invoke(false, null, null);
            }
        });
    }










}
