using UnityEngine;
using UnityEngine.UI;

[UnityEngine.Scripting.Preserve]
public class HomeObj : MonoBehaviour
{

    public RectTransform rect;
    public RectTransform rectLayout;
    public AutoResizeToChildren layout;
    public Button btn;
    public RawImage image;
    public UILabel text;
    public RectTransform imageMask;
    public UILabel textStatus;
    public Transform status;
    [SerializeField] UIExtension.SetFixRatioType imageRatio;
    public CRMData.MarketBanner banner { get; private set; }
    HomePage page;


    public void Init(HomePage page,CRMData.MarketBanner banner)
    {
       
        this.page = page;
        this.banner = banner;
        var action = banner.GetActionType();
        this.name = $"HomeObj-({banner.type}) : {banner.name} | {action}";


        // download image .....
        image.texture = page.theme.defaultImage;
        //image.DownloadTexture(banner.imageUrl, (img) => {
        page.LoadImage(image,banner.imageUrl, (img) => {


            // size image
            image.SetFixRatio(imageRatio);
            if (banner.type == CRMData.MarketBanner.Type.promotion)
                imageMask.SetSize(null, image.rectTransform.sizeDelta.y);

            // layout
            if (gameObject.activeInHierarchy && gameObject.activeSelf)
            {
                this.DoRefresh(
                    Resize,
                    page.resize.WaitResizeToFitChildren
                    );
            }


        });

        btn.interactable = action != CRMData.MarketBanner.ActionType.none;


        if (banner.type == CRMData.MarketBanner.Type.promotion)
        {
            text.text = $"<b>{banner.name}</b> {banner.description}";
            // status
            textStatus.AssignLanguage(action == CRMData.MarketBanner.ActionType.game ? "common_btn_join" : "common_btn_view" );
            status.SetActive(
                action == CRMData.MarketBanner.ActionType.none || action == CRMData.MarketBanner.ActionType.url? false : true
                );
           
        }
        else if (banner.type == CRMData.MarketBanner.Type.ads)
        {
            text.text = $"<b>{banner.name}</b> {banner.description}";
            status.SetActive(false);
        }
        else if (banner.type == CRMData.MarketBanner.Type.banner)
        {
            
        }
    }


    public void Resize()
    {
        // resize
        if (banner.type == CRMData.MarketBanner.Type.promotion)
        {
            layout.ResizeToFitChildren();
            rect.SetSize(null, rectLayout.sizeDelta.y);
        }
    }


    public void OnChoose()
    {
        page.OnChoose(banner);
    }
}
