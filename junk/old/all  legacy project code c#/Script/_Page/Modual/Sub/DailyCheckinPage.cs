// using UnityEngine;
// using UnityEngine.UI;
// using System.Collections;
// using System.Collections.Generic;
// using GameStore;
// using System.Linq;
// using AllIn1SpriteShader;

// public class DailyCheckinPage : UISubPage
// {
//     public static DailyCheckinPage instance;
//     public static void Open()
//     {
//         //instance = Create<DailyCheckinPage>(instance, CustomerTheme.instance.DailyCheckinPage);
//         instance.Init();
//         instance.theme.SfxOpen.Play();
//     }
//     void Init()
//     {
//         RefreshUI(CRMData.DailyData.current);
//     }
//     public void OnClosePage()
//     {
//         if (checkingIn) return;
//         theme.SfxClose.Play();
//         Hide();
//     }



//     public UILabel rewardValueText;
//     public UILabel staminaValueText;
//     public Image staminaFillBar;
//     public Image staminaPreBar;
//     public Button checkInButton;
//     public Transform gridDay;
//     public float waitComplete = 1.2f;
//     List<UIChild> dailySlots;
//     //CRMData.DailyData.DailyConfigData.DailyRewardData currentItemData;
//     UIChild currentItemUI;
//     public void RefreshUI(CRMData.DailyData data)
//     {
//         /*
//         if (dailySlots == null || dailySlots.Count == 0)
//         {
//             dailySlots = gridDay.GetComponentsInChildren<UIChild>().ToList();
//         }


//         // 1. จัดการ Stamina Bar
//         rewardValueText.text = data.config.staminaReward.pointBonus.ToString("#,##0");
//         UpdateProgress(data.currentStamina, data.config.staminaReward.requireStamina, false);

//         dailySlots.ForEach(x => x.SetActive(false));
//         for (int i = 0; i < data.config.dailyReward.Count; i++)
//         {
//             var day = i + 1;
//             var itemData = data.config.dailyReward[day.ToStr()];

//             if (i < dailySlots.Count)
//             {
//                 var state = "";
//                 if (day < data.nextDayIndex)
//                 {
//                     // [STATE] Checked
//                     state = "Checked";
//                 }
//                 else if (day == data.nextDayIndex)
//                 {
//                     if (data.canCheckIn)
//                     {
//                         // [STATE] Current
//                         state = "Current";
//                     }
//                     else
//                     {
//                         // [STATE] Checked
//                         state = "Checked";
//                     }
//                     currentItemData = itemData;
//                     currentItemUI = dailySlots[i];
//                 }
//                 else
//                 {
//                     // [STATE] Next
//                     state = "Next";
//                 }
//                 Setup(dailySlots[i], day, itemData, state);
//             }
//         }
//         checkInButton.interactable = data.canCheckIn;
//         */
//     }




//     private void UpdateProgress(int current, double max, bool anim, System.Action complete = null)
//     {
//         staminaValueText.text = $"{current}/{max}";
//         if (anim)
//         {
//             var now = staminaFillBar.fillAmount;
//             var value = (float)current / (float)max;
//             staminaPreBar.fillAmount = value;
//             this.DoWait(0.35f, () =>
//             {
//                 this.DoUpdate(0.5f, (time) =>
//                     {
//                         Debug.Log(1.0f - (time * 2));
//                         staminaFillBar.fillAmount = Mathf.Lerp(now, value, 1.0f - (time * 2));
//                         complete?.Invoke();
//                     });
//             });
//         }
//         else
//         {
//             staminaFillBar.fillAmount = (float)current / (float)max;
//             staminaPreBar.fillAmount = 0;
//             complete?.Invoke();
//         }
//     }
//     /*
//     void Setup(UIChild ui, int day, CRMData.DailyData.DailyConfigData.DailyRewardData itemData, string state)
//     {
//         ui.SetActive(true);
//         ui.Label[0].AssignLanguage("dailycheckin_day", day);
//         ui.Label[1].AssignLanguage("dailycheckin_value", $"+{itemData.amount}");
//         ui.Trans.Open(state);
//     }
//     */

//     bool checkingIn = false;
//     public void OnCheckIn()
//     {
//         if (checkingIn) return;
//         checkingIn = true;
//         checkInButton.interactable = false;
//         StartCoroutine(DoCheckIn());
//     }


//     IEnumerator DoCheckIn()
//     {
//         //** Update UI ทันที เพื่อให้รู้สึกว่ากดแล้วมีผล....
//         var data = CRMData.DailyData.current;
//         var nextProgress = data.currentStamina + currentItemData.amount;
//         // wait 1 วินาทีเพื่อรอผลจาก API กลับมา + Animation....


//         bool? done = null;
//         int resultPointReward = 0;
//         api.LoadingYield();
//         api.onStampDailyCheckin((checkInReward, pointReward) =>
//         {
//             if (checkInReward != null)
//             {
//                 done = true;
//                 resultPointReward = pointReward;
//             }
//             else
//             {
//                 done = false;
//             }
//         });
//         yield return new WaitWhile(() => !done.HasValue);
//         if (done.Value == true)
//         {
//             theme.SfxComplete.Play();
//             currentItemUI.GetComponent<Animator>().SimplePlay("checkin");
//             currentItemUI.Trans.Open("Checked");
//             UIUts.OnUpdateAll();
//             UpdateProgress(nextProgress, data.config.staminaReward.requireStamina, true);
//             if (resultPointReward == 0)
//             {
//                 UITopMessage.Open(Language.Get("CHECKIN_SUCCESS"));
//                 yield return new WaitForSeconds(waitComplete);
//                 Init();
//             }
//             else
//             {
//                 object[] language = new object[2]{ "{value}", resultPointReward};
//                 yield return new WaitForSeconds(waitComplete);
//                 Language.OpenPopup("CHECKIN_REDEEM_POPUP", theme.iconCompleteImage, () =>
//                 {
//                     UITopMessage.Open(Language.Get("CHECKIN_REDEEM", language )).SetIcon(theme.iconCurrencyImage);
//                     Init();

//                 }, null, language );
//             }
//         }
//         checkingIn = false;
//     }





// }

