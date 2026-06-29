using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using UnityEngine.UI;
using GameStore;

public class BasePage : MonoBehaviour
{
    public CRMData.UserData User => CRMData.User;
    public CRMData.MarketData MarketData => CRMData.MarketData.current;
    public CRMApi api => CRMApi.instance;
    public CustomerTheme theme => CustomerTheme.instance;
    protected float timeOpened = 0.0f;
    protected bool openAwake => timeOpened == Time.time;
    public virtual void Hide()
    {
        gameObject.SetActive(false);
    }


    public bool UILoading
    {
        set
        {
            UIRoot.instance.OnLoading(value);
        }
    }


    public void LoadImage(RawImage image, string url, System.Action<Texture2D> callback = null)
    {
        UIRoot.instance.LoadImage(image, url, callback);
    }


}

public class UIPage : BasePage
{
    static UIPage current;
    public PageType pageType;
    public string PageTittle;
    public enum PageType
    {
        Home, Leaderboard, GetPoint, Reward, Profile
    }
    public virtual void Begin()
    {
        Awake();
        Hide();
    }
    public virtual void Awake()
    {

    }
    public virtual void Open()
    {
        current = this;
        timeOpened = Time.time;
        gameObject.SetActive(true);
    }
}
public class UISubPage : BasePage
{
    internal static UISubPage current;
    public SubType subType;
    [VInspector.ShowIf("subType", SubType.Slide)]
    public string SubTitle;
    internal System.Action onHideEvent;

    public enum SubType
    {
        Popup, Slide
    }
    public static T Create<T>(MonoBehaviour instance, GameObject page)
    {
        if (instance == null)
        {
            var g = Instantiate(page, Console.instance.rootSubPage, false);
            g.transform.SetAsLastSibling();
            g.transform.ResetTransform();
            g.GetComponent<UISubPage>().Inited();
            return g.GetComponent<T>();
        }
        else
        {
            instance.SetActive(true);
            instance.transform.SetAsLastSibling();
            instance.transform.ResetTransform();
            instance.GetComponent<UISubPage>().Inited();
            return instance.GetComponent<T>();
        }
    }
    public virtual void HideSubPage()
    {
        Hide();
        onHideEvent?.Invoke();
    }
    void Inited()
    {
        current = this;
        if (subType == SubType.Slide)
            Console.instance.OpenSlidePage(this);
        timeOpened = Time.time;
    }
}
public class Console : MonoBehaviour
{
    #if UNITY_EDITOR
    public static Console FindAtScene()
    {
        if(DebugTool.IsIgnoreConsoleAtScene())
            return null;
            
        var list = GameObject.FindObjectsByType<Console>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        if (list == null || list.Length == 0)
        {
            return null;
        }
        else
        {
            if (list.Length > 1)
            {
                foreach (var console in list)
                    if (console.name == "Console")
                        return console;
            }
            return list[0];
        }
    }
    #endif

    public static Console Open()
    {
        var console = CustomerTheme.instance.Console.Create(UIRoot.instance.transform).GetComponent<Console>();
        console.Init();
        return console;
    }
    public static Console instance;

    [Header("Setting")]
    public Setting setting;
    [System.Serializable]
    public class Setting
    {
        [Header("Camera")]
        public bool orthographic = true;
        public float fieldOfView = 60;
        [Header("Loading")]
        public float minLoadingTime = 0.5f;
        internal void Apply()
        {
            UIRoot.instance.minLoadingTime = minLoadingTime;
            UIRoot.instance.maincam.orthographic = orthographic;
            UIRoot.instance.maincam.fieldOfView = fieldOfView;
        }
    }

    [Header("Console")]
    public Transform rootPage;
    public UIChild title;
    public List<UIPage> pages;
    public List<UIMainBtn> mainBtns;
    public Animator animPage;
    public Animator animBot;
    public SimpleTween tweenPointer;
    public Transform rootPointer;
    public Transform rootRefrshPointer;
    public ParticleSystem uIParticleSystem;

    [Header("SubPage")]
    public Transform rootSubPage;

    [Header("SlidePage")]
    public Transform rootSlidePage;
    public CanvasGroup canvasSlidePage;
    public Animator animSlidePage;
    public UILabel tittleSlidePage;
    public Transform originSlidePage;
    public float closeTimeSlidePage = 1.0f;

    Vector3 locationBtn;
    public void Init()
    {
        instance = this;
        StartCoroutine(DoInit());
    }
    IEnumerator DoInit()
    {
        setting.Apply();
        pages.ForEach(x => x.Begin());
        mainBtns.ForEach(x => x.Init(this));
        rootSlidePage.transform.SetActive(false);

        yield return new WaitForEndOfFrame();
        OpenPage(UIPage.PageType.Home);

        if (LoginPage.newUser && CRMData.User.isNewUser())
            StartCoroutine(DoFirstTimeFillInformation());

    }

    [VInspector.Button]
    void OnTestFirstTimeFill()
    {
        StartCoroutine(DoFirstTimeFillInformation());
    }


    IEnumerator DoFirstTimeFillInformation()
    {
        //** first-time filldata
        bool setting = false;
        SettingPage.SetupUserConsentNotif(LoginPage.consent_notif, () =>
        {
            setting = true;
        });
        yield return new WaitWhile(() => !setting);

        //** waiting check firsttime...
        yield return new WaitForSeconds(1);


        //** referral-invite
        bool referral = false;
        ProfilePage.OnOpenInviteReferal(() =>
        {
            referral = true;
        });
        yield return new WaitWhile(() => !referral);


        //** fill info user...
        Language.OpenPopup("FIRSTTIME_NEWUSER_FILLINFO", CustomerTheme.instance.iconEditProfile, () =>
        {
            //** yes
            EditProfilePage.Open((ok) => { });
        }, () =>
        {
            //** no

        }, null).ChangeBtnName("newuserinfo_btn_now", "newuserinfo_btn_skip");

    }



    public UIPage OpenPage(UIPage.PageType pageType)
    {

        //** main btns active
        UIMainBtn btn = null;
        mainBtns.ForEach(x =>
        {
            if (x.pageType == pageType)
            {
                btn = x;
                x.OnActive();
            }
            else x.OnDeactive();
        });

        //** open page
        UIPage page = null;
        pages.ForEach(x =>
        {
            if (x.pageType == pageType)
            {
                page = x;
                x.Open();
            }
            else x.Hide();
        });


        //** title
        if (page != null)
        {
            title.Label[0].text = Language.Get(page.PageTittle);
            title.SetActive(false);
            if (page.PageTittle.notnull())
                this.DoRefresh(() => { title.SetActive(true); });
        }


        //** location change
        animPage.Play("active", 0, 0);
        animBot.Play("active", 0, 0);
        tweenPointer.start = tweenPointer.transform.localPosition;
        tweenPointer.end = new Vector3(animBot.transform.InverseTransformPoint(btn.rectBase.position).x, tweenPointer.start.y, 0);
        tweenPointer.Play();
        rootRefrshPointer.SetActive(false);
        rootPointer.position = btn.rectPointer.position;
        if (uIParticleSystem != null)
        {
            uIParticleSystem.Stop();
            uIParticleSystem.Play();
        }
        this.DoRefresh(() =>
        {
            rootRefrshPointer.SetActive(true);
        });
        return page;
    }


    // Slide Page
    public void OpenSlidePage(UISubPage page)
    {
        CustomerTheme.instance.SfxOpen.Play();
        page.transform.SetParent(originSlidePage.transform, false);
        tittleSlidePage.text = Language.Get(page.SubTitle);
        rootSlidePage.transform.SetActive(true);
        canvasSlidePage.interactable = true;
        canvasSlidePage.blocksRaycasts = true;
        animSlidePage.Play("open", 0, 0);
    }
    public void CloseSlidePage()
    {
        if (UISubPage.current != null && UISubPage.current.subType == UISubPage.SubType.Slide)
        {
            CustomerTheme.instance.SfxClose.Play();
            canvasSlidePage.interactable = false;
            canvasSlidePage.blocksRaycasts = false;
            animSlidePage.Play("close", 0, 0);
            this.DoWait(closeTimeSlidePage, () =>
            {
                rootSlidePage.transform.SetActive(false);
                UISubPage.current.HideSubPage();
            });
        }
    }
}
