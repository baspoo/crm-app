using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

[UnityEngine.Scripting.Preserve]
public class SettingPage : UISubPage
{
    public static SettingPage instance;
    public static void Open()
    {
        instance = Create<SettingPage>(instance, CustomerTheme.instance.SettingPage);
        instance.Init();
    }





    public UILabel storeName;
    public UILabel userId;
    public UILabel statId;
    public UILabel createAt;
    public UILabel appVersion;
    public UIToggle Sfx;
    public UIToggle ConsentNotif;
    public UIToggle Th;
    public UIToggle En;

    public void Init()
    {
        theme.SfxOpen.Play();
        storeName.AssignLanguage("setting_subtitle_id_store", $"{CRMData.StoreInfo.current.storeName}");
        userId.AssignText($"{CRMData.User.user.id}");
        statId.AssignText(GameStore.WebGLService.User.gameStat.statId);
        createAt.AssignLanguage("setting_subtitle_timestamp", User.user.memberSince.ToStringDateTimeNormalize());
        appVersion.AssignLanguage("setting_subtitle_version_app", $"v. {GameStore.WebGLService.Version}");
        RefreshInterface();
    }
    void RefreshInterface()
    {
        if (Sfx != null)
            Sfx.IsEnable = !GameStore.Core.Sound.instance.IsSfxMute;

        if (ConsentNotif != null)
             ConsentNotif.IsEnable = CRMData.User.user.consent_notif;
    

        if (Th != null)
        {
            Th.IsEnable = GameStore.Language.GetLanguage() == GameStore.Language.LanguageType.Th;
            Th.Btn.interactable = !Th.IsEnable;
        }

        if (En != null)
        {
            En.IsEnable = GameStore.Language.GetLanguage() == GameStore.Language.LanguageType.En;
            En.Btn.interactable = !En.IsEnable;
        }


    }
    public void OnClosePage()
    {
        theme.SfxClose.Play();
        Hide();
    }



    public void OnSfx()
    {
        GameStore.Core.Sound.instance.OnMuteSFX(!GameStore.Core.Sound.instance.IsSfxMute);
        RefreshInterface();
    }
    public void OnCondition()
    {
        OnOpenCondition();
    }
    public static void OnOpenCondition()
    {
        var iframe = Store.IframeData.Get(Store.HtmlPages.Condition); 
        var conditionURL = CRMData.StoreInfo.current?.conditionURL;
        if (conditionURL.notnull())
        {
            if (conditionURL.Contains("http"))
                GameStore.WebGLService.OpenDefaultBrowser(conditionURL);
            else
            {
                iframe = iframe.Clone();
                iframe.path = conditionURL;
                UIRoot.instance.OpenIframe(iframe);
            }
        }
        else
        {
            UIRoot.instance.OpenIframe(iframe);
        }
    }
    public void OnContact()
    {

    }


    public void OnConsentNotif()
    {
        ConsentNotif.IsEnable = !ConsentNotif.IsEnable;
        SetupUserConsentNotif(ConsentNotif.IsEnable, () =>
        {
            RefreshInterface();
        });
    }
    public static void SetupUserConsentNotif(bool consent_notif, System.Action done = null)
    {
        //** setup... consent_notif
        CRMApi.instance.LoadingYield();
        var customFields = new Dictionary<string, object>();
        customFields.Add(CRMApi.CustomFields.consent_notif, consent_notif);
        CRMApi.instance.onUpdateProfileCustomFields(customFields, (ok) =>
        {
            done?.Invoke();
        });
    }


    public void OnTh()
    {
        if (GameStore.Language.GetLanguage() == GameStore.Language.LanguageType.Th)
            return;

        GameStore.Language.OpenPopup("CONFIRM_CHANGE_LANGUAGE", () =>
        {
            GameStore.Language.SetLanguage(GameStore.Language.LanguageType.Th);
            RefreshInterface();
            GameStore.WebGLService.ReGame(true);
        }, () =>
        {

        });
    }
    public void OnEn()
    {
        if (GameStore.Language.GetLanguage() == GameStore.Language.LanguageType.En)
            return;

        GameStore.Language.OpenPopup("CONFIRM_CHANGE_LANGUAGE", () =>
        {
            GameStore.Language.SetLanguage(GameStore.Language.LanguageType.En);
            RefreshInterface();
            GameStore.WebGLService.LoadScene(0);
        }, () =>
        {

        });
    }
}