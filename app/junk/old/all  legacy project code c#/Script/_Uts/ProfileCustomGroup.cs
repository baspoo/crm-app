using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
[UnityEngine.Scripting.Preserve]
public class ProfileCustomGroup : MonoBehaviour
{
    public AutoResizeToChildren root;
    public Transform addNewCustomFields;
    public Transform empty;
    public List<ProfileCustomField> customFields = new List<ProfileCustomField>();



   
    [Header("Opt Slide")]
    public UICtrPageDot pageSlide;
    public SimpleTween slideTween;
    public float slideRange;
    public float slideOffes;





    ProfilePage profilePage;
    public void Init(ProfilePage profilePage)
    {
        this.profilePage = profilePage;
        DisplayCustom();
    }
    public void DisplayCustom()
    {
        customFields.ForEach(x => x.OnEnable()); 
        UpdateListAndResize();
    }
    void UpdateListAndResize()
    {
        var avalible = customFields.FindAll(x => x.IsActive).Count;

        //** Order Index
        var orderCreateAt = customFields.OrderByDescending(x => x.CreatedAt);
        foreach (var item in orderCreateAt)
        {
            item.transform.SetAsFirstSibling();
        }

        addNewCustomFields.SetActive(avalible < customFields.Count);
        empty.SetActive(avalible == 0);
        root.WaitResizeToFitChildren();
        InitSlide(avalible);
    }
    void InitSlide(int count)
    {
        if (pageSlide != null)
        {
            if(count != 0)
            {
                pageSlide.SetActive(true);
                pageSlide.Init(count, (i) => {
                    var move = slideTween.rect.anchoredPosition;
                    slideTween.start = move;
                    move.x = (i == 0) ? slideOffes : slideOffes - (i * slideRange);
                    slideTween.end = move;
                    this.DoObjectRefresh(slideTween);
                });
            }
            else
            {
                pageSlide.SetActive(false);
            }
        }
    }







    IEnumerator DoRefreshData()
    {
        customFields.ForEach(x => x.OnEnable());
        yield return new WaitForEndOfFrame();
        ProfileCustomField.Refresh();
        yield return new WaitForEndOfFrame();
        UpdateListAndResize();
        yield return new WaitForEndOfFrame();
        profilePage.RefreshTable();
    }


/*

StreamingAssets/form/cus/theme_moochie/PetProfile.html
90/90 
%

StreamingAssets/form/cus/theme_onedrink/Fav.html
90/90
%
*/



    [Header("Iframe")]
    public Store.IframeData openIframe;
    public bool acttechRootServer;
    public bool acttechOneTimeToken;
    void OnetimeToken(System.Action<string> callback)
    {
        if (acttechOneTimeToken)
        {
            CRMApi.instance.onGetOneTimeToken(callback);
        }
        else
        {
            callback.Invoke(null);
        }
    }

    public void AddNew(   )
    {
        OpenIFrame(-1);
    }
    public void OpenIFrame(ProfileCustomField  profileCustomField)
    {
        OpenIFrame(profileCustomField.name.ToInt());
    }
    public void OpenIFrame(int index)
    {
        CRMApi.instance.LoadingYield();
        OnetimeToken((token) => {
            if (token.notnull())
            {


                var payload = new Dictionary<string, object>();
                if (acttechRootServer)
                {
                    payload.Add("rootUrl", GameStore.WebGLService.NetworkService.serverURL);
                    payload.Add("gameId", GameStore.Core.GameBundle.instance.gameConfigId);
                    payload.Add("statId", GameStore.WebGLService.STATID);
                }
                if (acttechOneTimeToken)
                {
                    payload.Add("token", token);
                }

                if(index>=0) payload.Add("index", index);
                payload.Add("customFields", CRMData.User.user.customFields);
                payload.Add("max", customFields.Count);


                UIRoot.instance.OpenIframe(openIframe, payload, (json) => {
                    Debug.Log(json);
                    if (json.notnull())
                    {
                        var result = json.ToDictStringObject();
                        if (result.Find("ok").ToBool())
                        {
                            CRMApi.instance.LoadingYield();
                            CRMApi.instance.onGetProfile((ok) => {
                                StartCoroutine(DoRefreshData());
                            });
                        }
                    }
                });

            }
        });
    }


    [VInspector.Button]
    void TestSubmit()
    {
        CRMApi.instance.LoadingYield();
            CRMApi.instance.onGetProfile((ok) => {
                StartCoroutine(DoRefreshData());
        });
    }















    #if UNITY_EDITOR
    [Header("EDITOR")]
    public GameObject prefab;
    [VInspector.Button]
    void GenNext()
    {
        customFields.Count.Loop(i => {

            if (customFields[i] != null)
            {
                DestroyImmediate(customFields[i].gameObject);
            }

            var index = i + 1;
            var newChild = Instantiate(prefab, prefab.transform.parent).GetComponent<ProfileCustomField>();
            newChild.SetActive(true);
            newChild.name = index.ToString();
            newChild.Key = newChild.Key.Replace("@", index.ToString());
            foreach (var t in newChild.transform.GetAllNode())
            {
                var f = t.GetComponent<ProfileCustomField>();
                if (f != null)
                {
                    f.Key = f.Key.Replace("@", index.ToString());
                }
            }
            customFields[i] = newChild;

        });

    }
    #endif

}
