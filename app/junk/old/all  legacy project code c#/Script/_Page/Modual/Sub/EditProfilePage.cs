using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using GameStore;

[UnityEngine.Scripting.Preserve]
public class EditProfilePage : UISubPage
{
    public static EditProfilePage instance;
    public static EditProfilePage Open(System.Action<bool> onDone)
    {
        instance = Create<EditProfilePage>(instance, CustomerTheme.instance.EditProfilePage);
        instance.Init();
        instance.onHideEvent = () =>
        {
            Debug.Log("Back Editor");
            onDone?.Invoke(instance.modify);
        };
        return instance;
    }





    [Header("Main")]
    public ScrollRect scroll;
    public AutoResizeToChildren resize;
    bool modify = false;
    //System.Action<bool> onDone;
    void Init( )
    {
        InitInfotamtion();
        InitAddress();
        InitCustom();

        OnModifyData();
        modify = false;
    }
    public void OnSnapTop()
    {
        scroll.verticalNormalizedPosition = 1;
    }
    public void OnSnapBot()
    {
        scroll.verticalNormalizedPosition = 0;
    }
    void Resize()
    {
        AutoResizeToChildren.WaitResizeToFitChildren(
            rootAddress,
            resize
            );
    }
    public void OnModifyData()
    {
        Resize();
        modify = true;
    }


    [Header("Information")]
    public TMPro.TMP_InputField phone;
    public TMPro.TMP_InputField email;
    public UIReuseDropDown gender;
    public UIReuseDropDown day;
    public UIReuseDropDown month;
    public UIReuseDropDown year;

 
    void InitInfotamtion()
    {
        // infotamtion
        phone.text = CRMData.User.user.phone;
        email.text = CRMData.User.user.email;


        // gender
        gender.Items = GameStore.Language.Get("editprofile_dropdown_gender").DeserializeObjectSimple<List<string>>(); //new List<string>() { "Male","Female","Other" };
        Debug.Log(CRMData.User.user.gender);
        if (CRMData.User.user.gender.notnull())
        {
            var g = CRMData.User.user.getGenderType();
            if (g == CRMData.UserData.InfomationData.genderType.male) gender.Modify(0, false);
            else if (g == CRMData.UserData.InfomationData.genderType.female) gender.Modify(1, false);
            else gender.Modify(2, false);
        }
        else gender.Modify(2, false);



        // datetime
        System.DateTime now = System.DateTime.Now;
        if (CRMData.User.user.hasbirthday)
            now = CRMData.User.user.GetBirthdayDate();


        var culture = System.Globalization.CultureInfo.InvariantCulture;
        int choose_day = now.Day;
        int choose_year = culture.Calendar.GetYear(now);
        int choose_month = now.Month;

        month.Items = GameStore.Language.Get("editprofile_dropdown_month").DeserializeObjectSimple<List<string>>(); //new List<string>() { "January", "February", "March", "April", "May", "June", "July", "August", "September", "October", "November", "December" };
        month.Modify(choose_month - 1, false);

        void SetDay()
        {
            day.Items = new List<string>();
            int days = System.DateTime.DaysInMonth(choose_year, choose_month);
            days.Loop(i =>
            {
                day.Items.Add($"{i + 1}");
            });
             if (CRMData.User.user.hasbirthday && choose_day <= days)
                day.Modify(choose_day - 1, false); 
              else 
                day.Modify(0, false);
        }
        SetDay();

        int now_year = culture.Calendar.GetYear(System.DateTime.Now);
        int start_year = now_year - 150;
        int count_year = (now_year - start_year) + 1;
        year.Items = new List<string>();
        count_year.Loop(i =>
        {
            year.Items.Add($"{now_year - i}");
        });
        int start_year_index = year.Items.FindIndex(x => x == choose_year.ToString());
        if (start_year_index == -1) start_year_index = 0;
        year.Modify(start_year_index, false);


        day.OnChange = ((i, text) =>
        {
            choose_day = i + 1;
        });
        month.OnChange = ((i, text) =>
        {
            choose_month = i + 1;
            SetDay();
        });
        year.OnChange = ((i, text) =>
        {
            choose_year = text.ToInt();
            SetDay();
        });
    }
    public void UpdateInformation()
    {



        var _email = email.text;

        string d = day.Text.Trim().PadLeft(2, '0');
        string m = (month.Index + 1).ToString().PadLeft(2, '0');
        string y = year.Text.Trim();
        string dateString = $"{d}/{m}/{y}";
        dateString.Log();
        var _birthday = System.DateTime.ParseExact(dateString, "dd/MM/yyyy", System.Globalization.CultureInfo.InvariantCulture).ToString("s");
        var _gender = ((CRMData.UserData.InfomationData.genderType)gender.Index).ToString();
        Debug.Log(_email);
        Debug.Log(_birthday);
        Debug.Log(_gender);


        modify = true;
        var data = new Dictionary<string, object>();
        data.Add( CRMApi.ProfileFields.email , _email);

        var customFields = new Dictionary<string, object>();
        data.Add(CRMApi.ProfileFields.customFields , customFields);
        customFields.Add(CRMApi.CustomFields.gender , _gender);
        customFields.Add(CRMApi.CustomFields.birthday, _birthday);

        api.LoadingYield();
        api.onUpdateProfile(data, (ok) =>
        {
            if (ok)
            {
                Language.OpenPopup("profile_update_complete",theme.iconCompleteImage);
            }
        });
    }
   


    //[Header("Custom")]
    //public List<ProfileCustomField> customFields = new List<ProfileCustomField>();
    private void InitCustom()
    {
        // customFields.ForEach(x => x.Init());
    }


    [Header("Address")]
    public AutoResizeToChildren rootAddress;
    public Transform rootAddressItem;
    public Transform btnAddNewAdress;
    //public Color addressTheme;
    void InitAddress()
    {
        var addressesDatas = CRMData.User.userAddresses;
        UIChild.DeactivePool(rootAddressItem);
        if (addressesDatas != null && addressesDatas.Count > 0)
        {
            foreach (var address in addressesDatas)
            {
                ProfilePage.DisplayAddress(rootAddressItem, address, ProfilePage.AddressAction.edit, () =>
                {
                    // add or edit
                     modify = true;
                    AddNewAddress(address, (ok) =>
                    {
                        InitAddress();
                    });

                }, () =>
                {
                    // removed
                     modify = true;
                    InitAddress();
                });
            }
        }
        btnAddNewAdress.SetAsLastSibling();
        Resize();
    }
    public void OnBtnAddNewAddress()
    {
        AddNewAddress(null, (ok) =>
        {
            if (ok)
            {
                modify = true;
                InitAddress();
            }
        });
    }
    public static void AddNewAddress(CRMData.UserData.UserAddressesData userAddressesData = null, System.Action<bool> done = null)
    {

        /* {
           "apiURL": "https://aws-bug-parse-gamification.thelastbug.co/open/getAddress",
           "headerName": "Create New Address",
           "submitBtnName": "บันทึกข้อมูล",
           "colorTheme": "#000000",
           "address": "315 หมู่6 ปางหมอปวง ฟาร์มบัวหลวง",
           "province": "เชียงราย",
           "amphoe": "เชียงแสน",
           "district": "ป่าสัก",
           "zipcode": "57150",
           "makeDefault": true
       }*/

        var payload = new Dictionary<string, object>();
        payload.Add("apiURL", System.IO.Path.Combine(GameStore.WebGLService.NetworkService.serverURL, "open/getAddress"));
        //payload.Add("colorTheme", addressTheme.ToHexString());
        if (userAddressesData == null)
        {
            payload.Add("headerName", "Create New Address");
            //payload.Add("submitBtnName", "สร้างที่อยู่ใหม่");
            payload.Add("makeDefault", CRMData.User.userAddresses == null || CRMData.User.userAddresses.Count == 0);
            userAddressesData = new CRMData.UserData.UserAddressesData();
            #if UNITY_EDITOR
            userAddressesData = CRMData.UserData.UserAddressesData.DefaultDemo();
            #endif
        }
        else
        {
            payload.Add("headerName", "Update Address");
            //payload.Add("submitBtnName", "บันทึกช้อมูล");
            payload.Add("makeDefault", userAddressesData.isDefault);
        }

        payload.Add("contactPerson", userAddressesData.contactPerson);
        payload.Add("contactPhone", userAddressesData.contactPhone);
        payload.Add("address", userAddressesData.addressLine1);
        payload.Add("province", userAddressesData.province);
        payload.Add("amphoe", userAddressesData.district);
        payload.Add("district", userAddressesData.subdistrict);
        payload.Add("zipcode", userAddressesData.postalCode);


        UIRoot.instance.OpenIframe(Store.HtmlPages.AddressForm, payload, (json) =>
        {
            if (json.notnull())
            {
                //modify = true;

                var data = json.ToDictStringObject();

                data.Modify("contactPerson", (val) =>
                {
                    userAddressesData.contactPerson = val.ToString();
                });
                data.Modify("contactPhone", (val) =>
                {
                    userAddressesData.contactPhone = val.ToString();
                });
                data.Modify("address", (val) =>
                {
                    userAddressesData.addressLine1 = val.ToString();
                });
                data.Modify("province", (val) =>
                {
                    userAddressesData.province = val.ToString();
                });
                data.Modify("amphoe", (val) =>
                {
                    userAddressesData.district = val.ToString();
                });
                data.Modify("district", (val) =>
                {
                    userAddressesData.subdistrict = val.ToString();
                });
                data.Modify("zipcode", (val) =>
                {
                    userAddressesData.postalCode = val.ToString();
                });
                data.Modify("makeDefault", (val) =>
                {
                    userAddressesData.isDefault = val.ToBool();
                });

                Debug.Log(userAddressesData.SerializeToJsonSimple());

                UIRoot.instance.OnLoading(true); //** 2 api (remove & re-get)
                if (userAddressesData.id == 0) CRMApi.instance.onCreateAddress(userAddressesData, (ok) =>
                {
                    //InitAddress();
                    Debug.Log("Create Address");
                    UIRoot.instance.OnLoading(false);
                    if(ok) Language.OpenPopup("adress_update_complete",CustomerTheme.instance.iconCompleteImage);
                    done?.Invoke(ok);
                });
                else CRMApi.instance.onUpdateAddress(userAddressesData, (ok) =>
                {
                    // InitAddress();
                    Debug.Log("Update Address");
                    UIRoot.instance.OnLoading(false);
                    if(ok) Language.OpenPopup("adress_update_complete",CustomerTheme.instance.iconCompleteImage);
                    done?.Invoke(ok);
                });
            }
            else
            {
                done?.Invoke(false);
            }

        });


    }











    //[Header("CustomField")]
    //public ProfileCustomField customField;
    //void InitCustomFields()
    //{
    //customField?.DisplayCustom(this);
    //}

}
