using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

[UnityEngine.Scripting.Preserve]
public class ShippingStatusPage : UISubPage
{
    public static ShippingStatusPage instance;
    public static void Open()
    {
        instance = Create<ShippingStatusPage>(instance,CustomerTheme.instance.ShippingPage);
        instance.Init();
    }


    public int loadAmount = 20;
    public RecyclingListView theList;
    public Transform empty;
    public Transform more;

    [Header("Filter")]
    public UIReuseDropDown dropDown;
    public Image fillterIcon;
    public Color[] filterColor;

    UIChild childmore;
    List<CRMData.ShippingOrderData> m_shippingOrders;
    System.DateTime? lastloaded = null;


    public static void OnClearList()
    {
        if(instance == null)
            return;
        instance.m_shippingOrders = null;
        instance.lastloaded = null;
    }

    void Init()
    {

        if (lastloaded == null || lastloaded.Value.AddMinutes(1) < System.DateTime.Now)
        {
            m_shippingOrders = null;
        }


        if (m_shippingOrders == null)
        {
            more.SetActive(false);
            InitFilter();
            Reload();
        }
        else
        {
            RenderList();
        }
    }
    public void Reload()
    {
        empty.SetActive(false);
        theList.ChildPrefab = theme.shippingObj;
        theList.StopCallback();
        api.LoadingYield();
        api.onGetShippingOrder(loadAmount , 0 , (datas,more) => {
            m_shippingOrders = datas;
            lastloaded = System.DateTime.Now;

            if (more) m_shippingOrders.Add(null);
            RenderList();
        });
    }
    public void OnMore()
    {
        theList.StopCallback();
        m_shippingOrders.RemoveAll(x => x == null);
        api.LoadingYield();
        api.onGetShippingOrder(loadAmount, m_shippingOrders.Count, (datas, more) => {
            m_shippingOrders.AddRange(datas);
            if (more) m_shippingOrders.Add(null);
            RenderList();
        });
    }




    // Filter
    int filterIndex = 0;
    void InitFilter()
    {
        /*
        dropDown.UpdateList(new List<string>() {
                "all".ToTitleCase(),
                CRMData.ShippingOrderData.Status.delivered.ToTitleCase(),
                CRMData.ShippingOrderData.Status.shipped.ToTitleCase(),
                CRMData.ShippingOrderData.Status.processing.ToTitleCase()
            });
        */

        var listString = GameStore.Language.Get("delivery_dropdown_filter");
        Debug.Log(listString);
        dropDown.UpdateList(listString.DeserializeObjectSimple<List<string>>());
        dropDown.OnChange = ( index, text)=> {
            filterIndex = index;
            RenderList();
        };
    }
    List<CRMData.ShippingOrderData> ListFilter()
    {
        fillterIcon.color = filterColor[filterIndex == 0 ? 0 : 1];
        return m_shippingOrders.FindAll(x => {

            if (x == null)
                return true;

            switch (filterIndex)
            {
                case 0:
                    return true;
                case 1:
                    return x.status == CRMData.ShippingOrderData.Status.delivered;
                case 2:
                    return x.status == CRMData.ShippingOrderData.Status.shipped;
                case 3:
                    return x.status == CRMData.ShippingOrderData.Status.processing;
                default:
                    return true;
            }

        });
    }











    void RenderList()
    {
        var list = ListFilter();
        empty.SetActive(list.Count == 0);
        theList.Init(list.Count, (item, index) => {


            var child = item as UIChild;
            var data = list[index];



            
            if (data != null)
            {
                child.transform.GetChild(0).SetActive(true);
                if (childmore == child)
                {
                    childmore = null;
                    more.SetActive(false);
                }


                child.Label[0].AssignText($"{data.items[0].name} x{data.items[0].quantity}");
                child.Label[1].AssignLanguage("delivery_obj_address", $"{data.shippingAddress.FullString(false)}");
                child.Label[2].AssignLanguage("delivery_obj_tel", $"{data.shippingAddress.contactPhone}");
                child.Images[0].enabled = false;
                if (data.trackingNumber.isnull())
                {
                    child.Label[3].text = $"In packing process";
                    child.Images[0].enabled = false;
                    child.onAction = null;
                }
                else
                {
                    child.Label[3].AssignLanguage("delivery_obj_shipping_no", $"<u>{data.trackingNumber}</u>");
                    child.Images[0].enabled = true;
                    child.onAction = (act) => {

                        if (act == "tracking")
                        {
                            data.trackingNumber.Copy();
                        }

                    };
                }
                child.Label[4].AssignLanguage("delivery_obj_timestamp",$"{data.createdAt.ToStringDateTimeNormalize()}");
                child.Trans.Open(data.status);

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
  



}
