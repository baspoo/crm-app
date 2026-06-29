/*
using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
*/

public class RankDetailPage : UISubPage
{
    /*
    public static RankDetailPage instance;
    public static void Open()
    {
        //instance = Create<RankDetailPage>(instance, CustomerTheme.instance.RankDetailPage);
       // instance.Init();
    }
    void Init()
    {
        //Render();
    }
    public void OnClosePage()
    {
        theme.SfxClose.Play();
        Hide();
    }


    //public Transform rootPage;
    public Animator animPage;
    public UICtrPageDot pageDot;
    public RawImage imgRank;
    public Transform isCurrenct;
    public UILabel txtRankName;
    public UILabel txtRankRequire;
    public Transform benefitRoot;
    public GameObject benefitObj;
    List<TMPro.TextMeshProUGUI> benefitObjs;
    CRMData.TierData current;

    /*
    void Render()
    {
        theme.SfxOpen.Play();
        var rankData = CRMData.TierData.current;
        current = CRMData.TierData.GetMyCurrentRank();

        if (benefitObjs == null)
        {
            benefitObj.SetActive(false);
            benefitObjs = new List<TMPro.TextMeshProUGUI>();
            6.Loop(()=> {
                var lb = benefitObj.Pool(benefitRoot).GetComponent<TMPro.TextMeshProUGUI>();
                lb.SetActive(false);
                benefitObjs.Add(lb);
            });
        }

        pageDot.Init(rankData.Count, (index)=> {
            DisplayRank(rankData[index]);
        });
        pageDot.onNext = (i) => { animPage.Play("next",0,0); };
        pageDot.onPrev = (i) => { animPage.Play("prev", 0, 0); };
    }
    void DisplayRank(CRMData.TierData data)
    {
        if(!openAwake)
            theme.SfxClick.Play();

        //** data
        imgRank.enabled = false;
        CRMData.TierData.GetIconRank(data,(img) => {
            imgRank.enabled = true;
            imgRank.texture = img;
        });
        txtRankName.AssignTextMergeName(data.name);

        //** benefits
        benefitObjs.ForEach(x => x.SetActive(false));
        data.benefits.For((i, message) => {
            benefitObjs[i].SetActive(true);
            benefitObjs[i].text = message;
        });

        //** currenct
        isCurrenct.SetActive(current == data);

        //** next
        if (!data.IsMaxRank())
        {
            txtRankRequire.AssignTextMergeName(data.requireSpending.ToString("#,##0"));
            txtRankRequire.SetActive(true);
        }
        else txtRankRequire.SetActive(false);
    }

    */
}
