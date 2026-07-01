using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;
 
[UnityEngine.Scripting.Preserve]
public class LoginPage : MonoBehaviour
{
  
    public static LoginPage instance;
    public static LoginPage Open(System.Action<bool> done = null)
    {
        if(instance == null)
        {
            instance = CustomerTheme.instance.Login.Create(UIRoot.instance.transform).GetComponent<LoginPage>();
        }
        instance.SetActive(true);
        instance.Init(done);
        return instance;
    }
    public void OnClose()
    {
        instance.SetActive(false);
    }












    [Header("login")]
    public Transform root_login;
    public RawImage imgIcon;
    public RawImage imgShop;
    public UILabel txtShop;
    public UILabel txtName;
    public TMPro.TMP_InputField inputPhone;
    public UIToggle toggleAgree;
    public UIToggle toggleNotif;
    public Button btnLogin;


    [Space(15)]
    [Header("otp")]
    public Transform root_otp;
    public TMPro.TMP_InputField inputOTP;
    public Button btnOTP;
    public Transform tOtpFail;
    public Button btnRestart;
    public TMPro.TextMeshProUGUI txtResert;
    public TMPro.TextMeshProUGUI txtRef;


    [Space(15)]
    [Header("done")]
    public Transform root_final;






    System.Action<bool> done;
    List<Transform> pages;
    private void Init(System.Action<bool> done)
    {
        newUser = false;
        consent_notif = false;
        this.mPhone = null;
        this.done = done;
        pages = new List<Transform>();
        pages.Add(root_login);
        pages.Add(root_otp);
        pages.Add(root_final);
        Auth();
    }





    public void OpenLogin()
    {
        pages.Open(root_login);
        txtShop.AssignText(LiffBridge.User.storeInfo.storeName);
        txtName.AssignText(LiffBridge.User.displayName);
        imgIcon.DownloadTexture(LiffBridge.User.pictureUrl);
        imgShop.DownloadTexture(LiffBridge.User.storeInfo.logoURL);
        inputPhone.text = string.Empty;
        toggleAgree.OnChange = (ok) => {
            btnLogin.interactable = ok;
        };
        toggleAgree.IsEnable = false;
        toggleNotif.IsEnable = false;
    }
    public void OnCondition()
    {
        SettingPage.OnOpenCondition();
    }
    public void OnLogin()
    {
        mPhone = inputPhone.text;
        Auth();
    }



    CRMApi.AuthResult authResult;
    void Auth(string choice = null)
    {
        pages.Close();

        UIRoot.instance.OnLoading(true);

        try
        {
            CRMApi.instance.onAuth(mPhone, (auth) => {
                UIRoot.instance.OnLoading(false);

                GameStore.WebGLService.OnWebGLReday();
                Debug.Log($"Auth {auth != null}");

                if (auth != null)
                {
                    authResult = auth;
                    if (auth.action == CRMApi.AuthResult.ActionType.OPEN_REGISTER)
                    {
                        OpenLogin();
                    }
                    if (auth.action == CRMApi.AuthResult.ActionType.OPEN_OTP_PIN)
                    {
                        OpenOtpPage(authResult);
                    }
                    if (auth.action == CRMApi.AuthResult.ActionType.LOGIN_DONE)
                    {
                        Done();
                    }
                    if (auth.action == CRMApi.AuthResult.ActionType.INVALID)
                    {
                        OnFailed(CRMApi.AuthResult.ActionType.INVALID);
                    }
                    if (auth.action == CRMApi.AuthResult.ActionType.EXPIRE)
                    {
                        OnFailed(CRMApi.AuthResult.ErrorType.EXPIRE);
                    }
                }
                else
                {
                    OnFailed(CRMApi.AuthResult.ErrorType.LOGIN_FAILED);
                }

            });
        }
        catch
        {
            UIRoot.instance.OnLoading(false);
            OnFailed(CRMApi.AuthResult.ErrorType.LOGIN_FAILED);
        }
       
    }
    private void OnFailed(string error)
    {
        Debug.LogError(error);
        CustomerTheme.instance.SfxFail.Play();

        System.Action action = ()=>{ };
        object[] args = null;
        string messageKey = "";
        switch (error)
        {
            case CRMApi.AuthResult.ErrorType.PHONE_ALREADY_LINKED:
                messageKey = CRMApi.AuthResult.ErrorType.PHONE_ALREADY_LINKED;
                action = OpenLogin;
                break;
            case CRMApi.AuthResult.ErrorType.LINE_ID_EXISTS:
                args = new string[] { "{existingPhone}" , authResult.existingPhone };
                messageKey = CRMApi.AuthResult.ErrorType.LINE_ID_EXISTS;
                action = OpenLogin;
                break;
            case CRMApi.AuthResult.ErrorType.EXPIRE:
                messageKey = CRMApi.AuthResult.ErrorType.EXPIRE;
                action = LiffBridge.Relogin;
                break;
            default:
                messageKey = CRMApi.AuthResult.ErrorType.LOGIN_FAILED;
                action = LiffBridge.Relogin;
                break;
        }

        Manager.mg.OnFailed(messageKey, action , args);

    
    }






    string mPhone;
    string _token;
    Coroutine _restart;
    void OpenOtpPage(CRMApi.AuthResult authResult)
    {
        pages.Open(root_otp);

        tOtpFail.SetActive(false);
        inputOTP.text = string.Empty;
        btnOTP.interactable = false;
        txtRef.text = $"Ref no. {authResult.refNo}";
        _token = authResult.verificationToken;

        if (_restart != null) 
            StopCoroutine(_restart);

        btnRestart.interactable = false;
        _restart = this.DoUpdate(59, (time) => {
            //** runtime
            txtResert.text = $"00:{(int)time}";
            //Debug.Log("run");
        }, () => {
            //** done
            txtResert.text = $"00:00";
            btnRestart.interactable = true;
            Debug.Log("done");
        });
    }
    public void OnUpdateOtp()
    {
        var message = inputOTP.text;
        tOtpFail.SetActive(false);
        btnOTP.interactable = message.Length > 0;
    }
    public void ReRequest()
    {
        Auth();
    }
    public void OnBackOTP()
    {
        if (_restart != null)
            StopCoroutine(_restart);

        OpenLogin();
        //transform.SetActive(false);
        //done?.Invoke(false);
    }
    public void SubmitOtp()
    {
        if (inputOTP.text.Length > 0)
        {
            tOtpFail.SetActive(false);
            OnVerifyOtp(inputOTP.text);
        }
    }





    public static bool newUser = false;
    public static bool consent_notif = false;
    void OnVerifyOtp(string otp)
    {
        if (_token.isnull())
        {
            Debug.LogError("OnVerifyOtp : Token is Empty!");
            return;
        }

        UIRoot.instance.OnLoading(true);
        CRMApi.instance.onOTPVerify(_token,otp, (ok) => {
            UIRoot.instance.OnLoading(false);
            if (ok)
            {
                consent_notif = toggleNotif.IsEnable;
                newUser = true;
                pages.Open(root_final);
            }
            else 
                tOtpFail.SetActive(true);
        });

    }

    public void OnReAuth()
    {
        this.mPhone = null;
        Auth();
    }



    void Done( )
    {
        UIRoot.instance.OnLoading(false);
        transform.SetActive(false);
        done?.Invoke(true);
    }







}
