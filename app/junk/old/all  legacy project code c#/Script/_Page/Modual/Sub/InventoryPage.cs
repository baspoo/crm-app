using UnityEngine;
using System.Collections.Generic;
using System.Linq;

[UnityEngine.Scripting.Preserve]
public class InventoryPage : UISubPage
{
    public static InventoryPage instance;
    public static void Open( bool forceUpload )
    {
        instance = Create<InventoryPage>(instance, CustomerTheme.instance.InventoryPage);
        instance.Init(forceUpload);
    }

    public static void OnClearList()
    {
        if(instance == null)
            return;
        instance.m_couponDatas = null;
        instance.lastloaded = null;
    }


    public int loadAmount = 20;
    public RecyclingListView theList;
    public Transform empty;
    public Transform more;
    UIChild childmore;
    System.DateTime? lastloaded = null;
    void Init(bool forceUpload)
    {
        
        theList.ChildPrefab = theme.couponObj;
        if(lastloaded == null || forceUpload)
        {
            m_couponDatas = null;
        }
        else if(lastloaded.Value.AddMinutes(1) < System.DateTime.Now)
        {
            m_couponDatas = null;
        }


        if (m_couponDatas == null)
        {
            more.SetActive(false);
            Reload();
        }
        else
        {
            RenderList();
        }
    }




    List<CRMData.CouponData> m_couponDatas;
    public void Reload()
    {
        empty.SetActive(false);
        theList.StopCallback();
        api.LoadingYield();
        api.onGetMyCoupons(loadAmount, 0 , (datas,more) => {

            //** Notif RegisterItems
            CRMData.NotifInventory.RegisterItems(datas.Select(x=>x.id.ToString()).ToArray());
            lastloaded = System.DateTime.Now;

            m_couponDatas = datas;
            if(more) m_couponDatas.Add(null);
            RenderList();
        });
    }
    public void OnMore()
    {
        theList.StopCallback();
        m_couponDatas.RemoveAll(x => x == null);
        api.LoadingYield();
        api.onGetMyCoupons(loadAmount, m_couponDatas.Count , (datas, more) => {
            m_couponDatas.AddRange(datas);

            //** Notif RegisterItems
            CRMData.NotifInventory.RegisterItems(m_couponDatas.Select(x => x.id.ToString()).ToArray());

            if (more) m_couponDatas.Add(null);
            RenderList();
        });
    }
    private void RenderList()
    {
        empty.SetActive(m_couponDatas.Count == 0);
        theList.Init(m_couponDatas.Count, (item, index) => {
            var child = item as UIChild;
            var data = m_couponDatas[index];

            if (data != null)
            {
                child.transform.GetChild(0).SetActive(true);
                if (childmore == child)
                {
                    childmore = null;
                    more.SetActive(false);
                }

                child.Label[0].AssignText(data.rewardName);
                child.Label[1].AssignText(data.rewardDescription);
                child.Label[2].AssignLanguage("coupon_obj_timestamp", data.redeemedAt);
                child.Trans[0].SetActive(!CRMData.NotifInventory.IsRead(data.id.ToString()));
                LoadImage(child.RawImages[0], data.rewardImage);
                //child.RawImages[0].DownloadTexture(data.rewardImage);
                child.onSelect = () => {
                    CRMData.NotifInventory.ReadItem(data.id.ToString());
                    child.Trans[0].SetActive(false);
                    OpenCoupon(data);
                };

            }
            else
            {
                childmore = child;
                more.SetActive(true);
                more.transform.SetParentResetLocation(childmore.transform);
                child.transform.GetChild(0).SetActive(false);
            }

        });
    }
    void OpenCoupon(CRMData.CouponData couponData)
    {
        var payload = new Dictionary<string, object>();
        payload.Add("rewardImage", couponData.rewardImage);
        payload.Add("rewardName", couponData.rewardName);
        payload.Add("rewardDescription", couponData.rewardDescription);
        payload.Add("redeemedAt", couponData.redeemedAt);
        payload.Add("couponCode", couponData.couponCode);
        payload.Add("usedAt", couponData.usedAt);
        payload.Add("codeStatus", couponData.codeStatus);
        payload.Add("status", couponData.status);
        var reward = CRMData.MarketReward.Get(couponData.rewardId);
        if (reward != null)
        {
            payload.Add("termsConditions", reward.termsConditions);
        }
        UIRoot.instance.OpenIframe(Store.HtmlPages.MyCoupons, payload );

    }
}