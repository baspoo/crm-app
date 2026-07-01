using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using GameStore.Core;
[UnityEngine.Scripting.Preserve]
public class RedeemPage : UISubPage
{
    public static RedeemPage instance;
    public static void Open(CRMData.MarketReward marketReward)
    {
        instance = Create<RedeemPage>(instance, CustomerTheme.instance.RedeemPage); 
        instance.OnReset();
        instance.marketReward = marketReward;
        instance.Init();
    }
    public void ClosePage()
    {
        theme.SfxClose.Play();
        Hide();
    }

    void Init()
    {
        theme.SfxOpen.Play();
        amount = 1;
        checkReward = false;
        InitRedeem();
    }
    void OnReset()
    {
        chooseAddress = null;
        marketReward = null;
        //gameReward = null;
    }





    [Header("Redeem")]
    public Transform rootRedeem;
    public Transform redeemInfo;
    public Animator anim;
    public RawImage imageItem;
    public UILabel txtName;
    public UILabel txtDes;
    public UILabel txtType;
    public Image icoType;
    public Transform tTier;
    public RawImage icoTier;
    public UILabel txtCurrentTier;
    public UILabel txtRequireTier;
    public UILabel txtPrice;
    public UILabel txtTotalPrice;
    public UILabel txtTotalAmount;
    public UITapToggle tapDesc;
    public ScrollRect scrollRectDesc;
    public RectTransform rectPage;
    public Transform onceTime;
    public Transform loadingChecker;
    public CanvasGroup canvasGroupSubmit;
    public UIToggle btnSubmit;
    
    CRMData.UserData.UserAddressesData chooseAddress;
    CRMData.MarketReward marketReward;
    bool shipping = false;
    int amount = 1;
    int itemPrice = 0;
    bool checkReward = false;
    void InitRedeem()
    {
        rootRedeem.Open(rootChangeAddress);
        redeemInfo.Open(redeemConfirm);
        itemPrice = marketReward.points;
        shipping = marketReward.isHasShipping;

        LoadImage(imageItem, marketReward.image);
        //imageItem.DownloadTexture(marketReward.image);
        txtName.AssignText(marketReward.name);
        txtPrice.AssignLanguage("redeem_info_point_use", itemPrice.ToString("#,##0"));
        onceTime.SetActive(marketReward.oneTimeRedemption);
        txtType.AssignLanguage($"reward_type_{marketReward.rewardType.ToString().ToLower()}");
        icoType.AssignSprite(marketReward.GetRewardType());

        tapDesc.Init(0, (index) => {
            scrollRectDesc.verticalNormalizedPosition = 1;
            txtDes.AssignText(index == 0 ? marketReward.description : marketReward.termsConditions);
            this.DoObjectRefresh(scrollRectDesc);
        });


        if (marketReward.tierRestricted && CRMData.TierData.isTierReady)
        {
            var myTier = CRMData.TierData.GetMyCurrentRank();
            if (myTier != null)
            {
                myTier.GetIcon(icoTier);
                tTier.SetActive(true);
                txtCurrentTier.AssignLanguage("redeem_info_current_tier" , myTier.name);
                txtRequireTier.AssignLanguage("redeem_info_require_tier", marketReward.TierRequire());
            }
            else tTier.SetActive(false);
        }
        else tTier.SetActive(false);


        UpdateBtnPricing();


        //** recheck pricing....
        if (!checkReward)
        {
            UIRoot.instance.OnBeginLoadingYield(false);
            api.onGetReward(marketReward.id, (patchingReward) => {
                UIRoot.instance.OnEndLoadingYield(() => {
                    if (patchingReward != null)
                        marketReward = patchingReward;
                    checkReward = true;
                    UpdateBtnPricing();
                });
            });
        }

    }
    public void OnBackToRedeem()
    {
        theme.SfxClick.Play();
        anim.Play("prev", 0, 0);
        InitRedeem();
    }

    void UpdateBtnPricing()
    {
        loadingChecker.SetActive(!checkReward);
        canvasGroupSubmit.alpha = checkReward? 1f : 0.55f;
        canvasGroupSubmit.interactable = checkReward;
        canvasGroupSubmit.blocksRaycasts = checkReward;

        txtTotalAmount.AssignText(amount.ToStr());
        txtTotalPrice.AssignText(checkReward? $"{(amount * itemPrice).ToString("#,##0")}" : "" );
        bool isCanRedeem = marketReward.isCanRedeem() && marketReward.IsEnough(amount);
        btnSubmit.IsEnable = isCanRedeem && checkReward;
    }
    public void OnAddAmount(int add)
    {
        if (!checkReward) return;

        theme.SfxClick.Play();
        amount += add;
        amount = amount.Min(1);
        UpdateBtnPricing();
    }
    public void OnNextConfirm()
    {
        if (!checkReward) return;

        InitConfirm();
        theme.SfxClick.Play();
        anim.Play("next", 0, 0);
    }




    [Header("Confirm Order")]
    public Transform redeemConfirm;
    public Transform rootAddress;
    public Transform hasAddress;
    public Transform notHasAddress;
    public Transform notUseAddress;
    public Transform createAddress;
    public RawImage cf_imageItem;
    public UILabel cf_txtName;
    public UILabel cf_txtDes;
    public UILabel cf_txtType;
    public Image cf_icoType;
    public UILabel cf_txtPrice;
    public UILabel cf_txtTotalAmount;
    public UILabel cf_txtTotalPrice;
    public UIToggle btnConfirm;
    void InitConfirm()
    {
        rootRedeem.Open(rootChangeAddress);
        redeemConfirm.Open(redeemInfo);
        if (shipping)
        {
            if (CRMData.User.userAddresses != null && CRMData.User.userAddresses.Count > 0)
            {
                hasAddress.Open(notHasAddress, notUseAddress);
                if (chooseAddress == null)
                {
                    chooseAddress = CRMData.User.userAddresses.Find(x => x.isDefault);
                    if (chooseAddress == null)
                        chooseAddress = CRMData.User.userAddresses[0];
                }
                UIChild.DeactivePool(createAddress);
                ProfilePage.DisplayAddress(createAddress, chooseAddress , null, OnChangeAddress );
            }
            else
            {
                chooseAddress = null;
                notHasAddress.Open(hasAddress, notUseAddress);
            }
        }
        else
        {
            chooseAddress = null;
            notUseAddress.Open(hasAddress, notHasAddress);
        }


        btnConfirm.IsEnable = !shipping || chooseAddress != null;

        LoadImage(cf_imageItem, marketReward.image);
        //LoadImage(cf_imageItem, marketReward.image);
        cf_txtName.AssignText(marketReward.name);
        cf_txtDes.AssignText(marketReward.description);
        cf_txtPrice.AssignLanguage("redeem_comfirm_obj_point_use", itemPrice.ToString("#,##0"));
        cf_txtType.AssignLanguage($"reward_type_{marketReward.rewardType.ToString().ToLower()}");
        cf_icoType.AssignSprite(marketReward.GetRewardType());
        cf_txtTotalAmount.AssignTextMergeName(amount.ToStr());
        cf_txtTotalPrice.AssignLanguage("redeem_comfirm_obj_summarypoint_use", $"{(amount * itemPrice).ToString("#,##0")}");



    }
    public void BackToConfirm()
    {
        theme.SfxClick.Play();
        anim.Play("prev", 0, 0);
        InitConfirm();
    }
    public void OnAddNewAddress()
    {
        theme.SfxClick.Play();
        EditProfilePage.Open((modif) => {
            InitRedeem();
            InitConfirm();
        });
    }
    public void OnChangeAddress()
    {
        theme.SfxClick.Play();
        anim.Play("next",0,0);
        InitAddress();
    }
    public void OnSumbit()
    {
        // Redeem : Market Reward
        if (marketReward == null)
            return;

        if (shipping)
        {
            if (chooseAddress != null && chooseAddress.id != 0)
            {
                api.LoadingYield();
                api.onRedeemReward(marketReward.id, 1, chooseAddress.id, Complete);
            }
        }
        else
        {
            api.LoadingYield();
            api.onRedeemReward(marketReward.id, 1, null , Complete);
        }


        /*
        // Redeem : Game Reward
        if (gameReward != null)
        {
            api.onDeductPoints(gameReward.shopId, Complete);
        }
        */

    }
    void Complete(bool ok , string rewardId)
    {
        Hide();
        if (ok)
        {
            UIUts.OnUpdateAll();
            CustomerTheme.instance.SfxComplete.Play();

            if(marketReward.isHasShipping)
                ShippingStatusPage.OnClearList();


            //** open inventory now is a digital reward!!
            if (!marketReward.isHasShipping && marketReward.isDigital)
            {
                GameStore.Language.OpenPopup("REDEEM_COMPLETED_USENOW", theme.iconCompleteImage , () =>
                {
                    // use-now!
                    InventoryPage.Open(true);
                }, () =>
                {
                    // close
                }).ChangeBtnName("redeemed_btn_usenow", "redeemed_btn_skip");
               
            }
            //** normal reward
            else
            {
                GameStore.Language.OpenPopup("REDEEM_COMPLETED" , theme.iconCompleteImage );
            }

        }
        else
        {
            CustomerTheme.instance.SfxFail.Play();
            GameStore.Language.OpenPopup("REDEEM_FAILED");
        }
    }









    [Header("Change Address")]
    public Transform rootChangeAddress;
    public Transform rootAddressItem;
    Transform[] addressOffest = null;
    void InitAddress()
    {
        if (addressOffest == null)
            addressOffest = new Transform[2] 
            { 
                rootChangeAddress.GetChild(0),
                rootChangeAddress.GetChild(1) 
            };


        rootChangeAddress.Open(rootRedeem);
        var addressesDatas = CRMData.User.userAddresses;
        UIChild.DeactivePool(rootAddressItem);
        if (addressesDatas != null && addressesDatas.Count > 0)
        {
            foreach (var address in addressesDatas)
            {
                ProfilePage.AddressAction? action = chooseAddress == address?
                    ProfilePage.AddressAction.choose : null;

                ProfilePage.DisplayAddress(rootAddressItem, address, action , () => {
                    //** choose
                    chooseAddress = address;
                    BackToConfirm();
                });
            }
        }
        addressOffest[0].SetAsFirstSibling();
        addressOffest[1].SetAsLastSibling();
        AutoResizeToChildren.WaitResizeToFitChildren(rootAddressItem);
    }
    public void OnEditAddress()
    {
        theme.SfxClick.Play();
        EditProfilePage.Open((modif) => {
            if (marketReward != null)
            {
                //Open(marketReward);
                InitAddress();
            }
        });
    }


}
