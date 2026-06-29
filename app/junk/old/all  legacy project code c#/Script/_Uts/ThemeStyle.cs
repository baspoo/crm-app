using System;
using UnityEngine;
using UnityEngine.UI;

public class ThemeStyle : MonoBehaviour
{

    //** IMAGE
    [VInspector.ShowIf("AssignColor" , false)]
    public bool AssignImage;
     [VInspector.EndIf]
    [VInspector.ShowIf("AssignImage")]
    public Image ImageCustom;
    public Transform ImageDefault;
    public string ImageName;
    [VInspector.EndIf]
    bool isImageType => AssignImage && ImageName.isnull();
    [VInspector.ShowIf("isImageType",true)]
    public CustomerTheme.CusThemeSpriteType ImageType;
    [VInspector.EndIf]
    [VInspector.ShowIf("AssignImage")]
    public int Index;
    [VInspector.EndIf]


    [Space(10)]


    //** COLOR
    [VInspector.ShowIf("AssignImage" , false)]
    public bool AssignColor;
    [VInspector.EndIf]
    [VInspector.ShowIf("AssignColor")]
    public MaskableGraphic Graphics;
    public UnityEngine.UI.Outline Outline;
    public UIToggle Toggle;


    [Header("Color Style")]
    public bool IsPrimaryColor;
    public bool IsSecondaryColor;
    public bool IsBgColor;
    [VInspector.EndIf]


     bool isStyle => AssignColor && (IsPrimaryColor || IsSecondaryColor || IsBgColor);
    [VInspector.ShowIf("isStyle")]
    [Header("Color Effect")]
    public bool InvertBackWhite;
    public bool FollowBackWhite;
    public bool Negative;
    public bool Darken; 
    public bool Lighten;
    public bool Pastel;
    [VInspector.EndIf]
    bool changeColorShade => AssignColor && (Darken || Lighten || Pastel);
    [VInspector.ShowIf("changeColorShade",true)]
    public float ShadeFactor = 0.5f;
    [VInspector.EndIf]
 
    bool inited = false;
    public void OnStart()
    {
        Start();
    }
    void Start()
    { 
        if(inited)
            return;

        if (AssignImage)
        {
            var theme = CustomerTheme.instance.FindCustomeThemeSprite(isImageType ? ImageType.ToString() : ImageName) ;
            if (theme != null && theme.ready)
            {
                var sprite = theme.GetSprite(Index);
                if (sprite != null)
                {
                    ImageCustom.color = Color.white;
                    ImageCustom.sprite = sprite;
                }
                ImageDefault.SetActive(false);
                gameObject.SetActive(true);
            }
            else
            {
                ImageDefault.SetActive(true);
                if(ImageDefault != null) 
                    gameObject.SetActive(false);
            }
        }
        else  if (AssignColor && CRMData.InitializeData.current.skipInappThemeColor == false)
        {
            var coloStr = "#FFFFFF";
            if (IsPrimaryColor)
                coloStr = CRMData.InitializeData.current.primaryColor;
            else if (IsSecondaryColor)
                coloStr = CRMData.InitializeData.current.secondaryColor;
            else if (IsBgColor)
                coloStr = CRMData.InitializeData.current.bgColor;
            var color = coloStr.HexToColor();

            if(InvertBackWhite) color = color.IsDark() ? Color.white : Color.black;
            else if(FollowBackWhite) color = color.IsDark() ? Color.black : Color.white;
            else if (Negative) color = color.ToChangeShade( VariableService.ShadeType.Complement , 0);
            else if (Darken) color =color.ToChangeShade( VariableService.ShadeType.Darken , ShadeFactor); // 0.5
            else if (Lighten) color = color.ToChangeShade( VariableService.ShadeType.Lighten , ShadeFactor); //0.5
            else if (Pastel) color = color.ToChangeShade( VariableService.ShadeType.Pastel , ShadeFactor); // 0.2
    

            if(Graphics!=null)
                Graphics.color = color;

            if(Outline!=null)
                Outline.effectColor = color;

            if(Toggle!=null)
            {
                Toggle.Enable.color = color;
                if( Toggle.Text != null)
                {
                    var colorText = color.IsDark() ? Color.white : Color.black;
                    Toggle.Enable.text = colorText;
                }
            }

        }
        inited = true;

    }



 


 



}
