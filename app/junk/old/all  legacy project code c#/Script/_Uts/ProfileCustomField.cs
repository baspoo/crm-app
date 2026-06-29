using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
[UnityEngine.Scripting.Preserve]
public class ProfileCustomField : MonoBehaviour
{

    public static List<ProfileCustomField> currents = new List<ProfileCustomField>();
    public static void Clear()
    {
        currents = new List<ProfileCustomField>();
    }
    public static void Refresh()
    {
        foreach (var ui in currents)
            ui.OnEnable();
    }

    [Header("Key")]
    public string Key;
    public bool HasKeyOfEnable;
    [VInspector.ShowIf("HasKeyOfEnable", true)]
    public string DeactivateSymbol;
    [VInspector.EndIf()]


    public bool Root;
    [VInspector.ShowIf("Root",true)]
    public List<ProfileCustomField> childs;
    [VInspector.EndIf()]
    [VInspector.ShowIf("Root", false)]
    public bool Child;
    [VInspector.EndIf()]

    [VInspector.ShowIf("Root", false)]
    [Header("UI-UILabel")]
    public UILabel UILabel;
    public string textDefault;


    [Header("UI-Texture")]
    public RawImage UIImage;
    public Texture2D imgDefault;
    public UIExtension.SetFixRatioType setFixRatio;



    [Header("UI-Active")]
    public Transform UIActive;
    public string dataActive;
    [VInspector.EndIf()]

 
    public bool IsActive { get; private set;}
    public long CreatedAt { get; private set;}
    bool added = false;


    public void OnEnable()
    {
        if (Child)
            return;

        if(!added)
        {
            currents.Add(this);
            added = true;
        }
       
        if (Root)
        {
            var strData = CRMData.User.user.customFields.Find(Key).ToStr();
            Active(strData);
            if (strData.notnull())
            {
                var data = strData.ToDictStringObject();
                CreatedAt = data.Find("createdAt").ToLong();
                foreach (var ui in childs)
                {
                    ui.Render(data);
                }
            }
        }
        else
        {
            Render(CRMData.User.user.customFields);
        }
    }
    void Active(string data)
    {
        if (HasKeyOfEnable)
        {
            if (data.isnull() || data == DeactivateSymbol )
            {
                gameObject.SetActive(false);
                IsActive = false;
                return;
            }
            gameObject.SetActive(true);
            IsActive = true;
        }
    }
    public void Render(Dictionary<string, object> mydata)
    {
 
        var data = mydata.FindPath(Key).ToStr();
        Active(data);
 

         

       
        if (UILabel != null)
        {
            UILabel.AssignText(data.notnull()? data : textDefault);
        }
        if (UIImage != null)
        {
           UIImage.texture = imgDefault;
           if (data.notnull())
                UIImage.DownloadTexture(data, (tex) =>
                {
                    UIImage.SetFixRatio(setFixRatio);
                });
        }
        if (UIActive != null)
        {
            UIActive.SetActive(data.ToLower() == dataActive.ToLower());
        }
    }


   


















}
