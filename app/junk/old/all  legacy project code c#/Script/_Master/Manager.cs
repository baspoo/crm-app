using UnityEngine;
using System.Collections;
 

#if UNITY_EDITOR
using UnityEditor;
using System.IO;
using System.Linq;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
#endif
public class Manager : MonoBehaviour
{

     

    public static Manager mg;
    public LiffBridge line;
    public CRMApi crmApi;
    public UIRoot uiRoot;
 
    IEnumerator Start()
    {
        mg = this;
         

        Store.Init();
        CRMData.Init();
        crmApi.Init();
        uiRoot.Init();
        UIRoot.instance.OnLoading(true);

        GameStore.WebGLService.Init();
        yield return new WaitWhile(() => !GameStore.WebGLService.IsPreReady);
        StartCoroutine(GameStore.Language.InitCorotine());
        yield return new WaitWhile(() => !GameStore.WebGLService.IsInitialized || !GameStore.Language.IsReady);
        yield return StartCoroutine(line.OnGetData());



        crmApi.Inited();
        uiRoot.Inited();

        StartCoroutine(WelcomeScreen());
    }

 
    IEnumerator WelcomeScreen()
    {
        //** load theme
        yield return StartCoroutine(CustomerTheme.Load());
        yield return new WaitForEndOfFrame();
        UIRoot.instance.OnLoading(false);
        

        //** login
        var logged = false;
        LoginPage.Open((ok) => {
            logged = ok;
        });
        yield return new WaitWhile(()=> !logged || !CRMData.IsLoaded);
       

        //** open interface
        UIRoot.instance.OnLoading(false);
        uiRoot.StartApp();
    }


    public void OnFailed(string cause, System.Action action = null, params object[] args)
    {

        if(CustomerTheme.instance!=null)
            CustomerTheme.instance.SfxFail.Play();

        UIRoot.instance.OnLoading(false);
        GameStore.WebGLService.OnWebGLReday();

        if (action == null) GameStore.Language.OpenPopup(cause, args: args).OnHideAllBtns();
        else GameStore.Language.OpenPopup(cause, action, args: args);
    }



}
