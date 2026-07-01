using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;


[UnityEngine.Scripting.Preserve]
public class PointHistoryPage : UISubPage
{
    public static PointHistoryPage instance;
    public static void Open()
    {
        instance = Create<PointHistoryPage>(instance, CustomerTheme.instance.PointHistoryPage);
        instance.Init();
    }
    void Init()
    {
        loaddedData = false;
        if (lastloaded == null || lastloaded.Value.AddMinutes(1) < System.DateTime.Now)
        {
            orderEarned = null;
            orderDeduct = null;
        }

        theList.ChildPrefab = theme.pointHistoryObj;
        tap.Init(0, (index) =>
        {
            switch (index)
            {
                case 0: EarnedList(); break;
                case 1: DeductList(); break;
            }
        });
        onHideEvent = () =>
        {
            // has loadded data .... sleep... update profile and note clean data on close page
            if (loaddedData)
            {
                api.onGetProfile((ok) =>
                {
                    UIUts.OnUpdateAll(false);
                });
            }
        };
    }

    public static void OnClearList()
    {
        if (instance == null)
            return;
        instance.orderEarned = null;
        instance.orderDeduct = null;
        instance.lastloaded = null;
    }






    // Filter
    int filterIndex = 0;
    void InitFilter()
    {
        /*
        dropDown.UpdateList(new List<string>() {
                "all".ToTitleCase(),
                CRMData.EarnedPointsTransactionData.Status.approved.ToTitleCase(),
                CRMData.EarnedPointsTransactionData.Status.pending.ToTitleCase()
            });
        */

        var listString = GameStore.Language.Get("transaction_dropdown_filter");
        Debug.Log(listString);
        dropDown.UpdateList(listString.DeserializeObjectSimple<List<string>>());




        dropDown.OnChange = (index, text) =>
        {
            filterIndex = index;
            OnRenderEarnedList();
        };
    }
    List<CRMData.EarnedPointsTransactionData> ListFilter()
    {
        fillterIcon.color = filterColor[filterIndex == 0 ? 0 : 1];
        return orderEarned.FindAll(x =>
        {

            if (x == null)
                return true;

            switch (filterIndex)
            {
                case 0:
                    return true;
                case 1:
                    return x.status == CRMData.EarnedPointsTransactionData.Status.approved;
                case 2:
                    return x.status == CRMData.EarnedPointsTransactionData.Status.pending;
                default:
                    return true;
            }

        });
    }









    bool loaddedData = false;
    public void Reload()
    {
        loaddedData = true;
        empty.SetActive(false);
        theList.StopCallback();
        if (tap.currentIndex == 0)
        {
            api.LoadingYield();
            api.onGetEarnedPoints(loadAmount, 0, (order, more) =>
            {
                orderEarned = order;
                lastloaded = System.DateTime.Now;

                if (more) orderEarned.Add(null);
                OnRenderEarnedList();
            });
        }
        if (tap.currentIndex == 1)
        {
            api.LoadingYield();
            api.onGetDeductPoints(loadAmount, 0, (order, more) =>
            {
                orderDeduct = order;
                lastloaded = System.DateTime.Now;

                if (more) orderDeduct.Add(null);
                OnRenderDeductList();
            });
        }
    }

    public void OnMore()
    {
        loaddedData = true;
        theList.StopCallback();
        if (tap.currentIndex == 0)
        {
            orderEarned.RemoveAll(x => x == null);
            api.LoadingYield();
            api.onGetEarnedPoints(loadAmount, orderEarned.Count, (datas, more) =>
            {
                Debug.Log(orderEarned.Count);
                Debug.Log(datas.Count);
                orderEarned.AddRange(datas);
                Debug.Log(orderEarned.Count);
                if (more) orderEarned.Add(null);
                OnRenderEarnedList();
            });
        }
        if (tap.currentIndex == 1)
        {
            orderDeduct.RemoveAll(x => x == null);
            api.LoadingYield();
            api.onGetDeductPoints(loadAmount, orderDeduct.Count, (datas, more) =>
            {
                orderDeduct.AddRange(datas);
                if (more) orderDeduct.Add(null);
                OnRenderDeductList();
            });
        }


    }
    public int loadAmount = 20;
    public UITapToggle tap;
    public RecyclingListView theList;
    public Transform empty;
    public Transform more;
    public Color[] pointColors;
    UIChild childmore;
    System.DateTime? lastloaded = null;


    [Header("Filter")]
    public UIReuseDropDown dropDown;
    public Image fillterIcon;
    public Color[] filterColor;




    void EarnedList()
    {
        if (orderEarned != null)
        {
            OnRenderEarnedList();
            return;
        }

        //** first time init
        more.SetActive(false);
        InitFilter();
        theList.Init(0, (n, m) => { });
        Reload();
    }
    List<CRMData.EarnedPointsTransactionData> orderEarned;
    private void OnRenderEarnedList()
    {
        dropDown.SetActive(true);
        var list = ListFilter();
        empty.SetActive(list.Count == 0);
        theList.Init(list.Count, (item, index) =>
        {
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

                var orderId = data.orderId;
                var channel = data.GetChannelName();
                var points = data.points;
                var created_at = data.createdAt;

                child.Label[0].AssignLanguage("transaction_obj_id", orderId);
                child.Label[1].AssignText(channel);
                child.Label[2].AssignLanguage("transaction_obj_timestamp", created_at.ToStringDateTimeNormalize());

                /*
                child.Label[0].AssignTextMergeName(orderId);
                child.Label[1].AssignText(channel);
                child.Label[2].AssignTextMergeName(created_at.ToStringDateTimeNormalize());
                */

                if (data.status == "pending")
                {
                    child.Label[3].text = $"{points.ToString("#,##0")}";
                    child.Label[3].color = pointColors[0];
                    child.Trans.Open(0);
                }
                else
                {
                    child.Label[3].text = $"+ {points.ToString("#,##0")}";
                    child.Label[3].color = pointColors[1];
                    child.Trans.Open(1);
                }
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


    void DeductList()
    {
        if (orderDeduct != null)
        {
            OnRenderDeductList();
            return;
        }

        //** first time init
        more.SetActive(false);
        theList.Init(0, (n, m) => { });
        Reload();
    }
    List<CRMData.DeductionHistoryTransactionData> orderDeduct;
    private void OnRenderDeductList()
    {
        dropDown.SetActive(false);
        empty.SetActive(orderDeduct.Count == 0);
        theList.Init(orderDeduct.Count, (item, index) =>
        {
            var child = item as UIChild;
            var data = orderDeduct[index];


            if (data != null)
            {
                child.transform.GetChild(0).SetActive(true);
                if (childmore == child)
                {
                    childmore = null;
                    more.SetActive(false);
                }


                var transaction_id = data.transaction_id;
                var name = data.GetTransactionName();
                var points_deducted = data.points_deducted;
                var created_at = data.created_at;

                child.Label[0].AssignTextMergeName(transaction_id);
                child.Label[1].AssignText(name);
                child.Label[2].AssignTextMergeName(created_at.ToStringDateTimeNormalize());
                child.Label[3].text = $"- {points_deducted.ToString("#,##0")}";
                child.Label[3].color = pointColors[2];
                child.Trans.Close();

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
