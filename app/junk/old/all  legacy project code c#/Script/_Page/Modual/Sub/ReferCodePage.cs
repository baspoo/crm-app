// using UnityEngine;
// using UnityEngine.UI;
// using System.Collections;
// using System.Collections.Generic;


// public class ReferCodePage : UISubPage
// {
//     public static ReferCodePage instance;
//     public static void Open()
//     {
//         //instance = Create<ReferCodePage>(instance, CustomerTheme.instance.ReferFriendPage);
//         instance.Init();
//     }
//     void Init()
//     {
//         InitIdle();
//     }
//     public void OnClosePage()
//     {
//         theme.SfxClose.Play();
//         Hide();
//     }


//     [Header("IDLE")]
//     public Transform rootIdle;
//     public TMPro.TMP_InputField inputText;

//     [Header("FRIENDS")]
//     public Transform rootFriend;
//     public UIChild your;
//     public UIChild friend;
//     string referCode;



//     void InitIdle()
//     {
//         rootIdle.Open(rootFriend);
//         inputText.text = string.Empty;
//     }
//     void FindNotFound()
//     {

//     }
//     public void OnNextFind()
//     {
//         if (inputText.text.isnull())
//         {
//             return;
//         }
//         referCode = inputText.text;
//         api.onReferCodeGetFriend(referCode, (data) =>
//         {
//             if (data != null)
//                 InitFriend(data);
//             else
//                 FindNotFound();
//         });
//     }




//     void InitFriend(Dictionary<string, object> data)
//     {
//         rootFriend.Open(rootIdle);
//         DisplayUser(your);
//         DisplayUser(friend);
//     }
//     void DisplayUser(UIChild user)
//     {
//         var rawProfile = user.RawImages[0];
//         var imgCoin = user.Images[0];
//         var txtName = user.Label[0];
//         var txtPoint = user.Label[1];
//     }


//     public void OnConfirm()
//     {
//         api.onReferCodeConfirm(referCode, (data) =>
//         {

//         });
//     }
//     public void OnBack()
//     {
//         InitIdle();
//     }



// }
