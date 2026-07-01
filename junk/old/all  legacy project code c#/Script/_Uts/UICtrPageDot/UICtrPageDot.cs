using System.Collections;
using System.Collections.Generic;
using UnityEngine;
[UnityEngine.Scripting.Preserve]
public class UICtrPageDot : MonoBehaviour
{
    public enum BtnHideAction
    {
        interactable,
        active
    }
    public BtnHideAction btnHideAction;
    public List<UnityEngine.UI.Button> btnPage;
    public Transform rootDot;
    public GameObject prefabdot;
    public RectTransform focusdot;
    public float speedDot;
    List<UIChild> dots;
    int amount, index;
    System.Action<int> change;
    public System.Action<int> onNext, onPrev;
    public void Init(int Amount, System.Action<int> change)
    {
        index = 0;
        amount = Amount;
        this.change = change;
        prefabdot.SetActive(false);
        if (dots == null || dots.Count == 0 || dots.Count != amount)
        {
            dots = new List<UIChild>();
            rootDot.DesAllParent();
            amount.Loop(i =>
            {

                var c = prefabdot.Create(rootDot).GetComponent<UIChild>();
                c.SetActive(true);
                dots.Add(c);
                c.onSelect = () =>
                {
                    index = i;
                    View();
                };

            });
        }


        View(false);
        if (amount >= 2)
        {
            //** show dot & btn < >
            btnPage.ForEach(x => x.SetActive(true));
            rootDot.SetActive(true);
            focusdot.SetActive(false);
            this.DoRefresh(() =>
            {
                focusdot.SetActive(true);
                focusdot.transform.position = dots[index].transform.position;
            });
        }
        else
        {
            //** hide all btn
            btnPage.ForEach(x => x.SetActive(false));
            rootDot.SetActive(false);
            focusdot.SetActive(false);
        }


    }

    public void OnDisplay(int newindex)
    {
        index = newindex;
        index = index.Min(0).Max(amount);
        View();
    }
    public void OnSlideNext()
    {
        index++;
        if (index >= amount)
            index = 0;
        View();
    }
    public void OnNext()
    {
        index++;
        index = index.Min(0).Max(amount);
        onNext?.Invoke(index);
        View();
    }
    public void OnPrev()
    {
        index--;
        index = index.Min(0).Max(amount - 1);
        onPrev?.Invoke(index);
        View();
    }
    private void View(bool anim = true)
    {
        if (btnHideAction == BtnHideAction.interactable)
        {
            btnPage[0].interactable = index < amount - 1;
            btnPage[1].interactable = index >= 1;
        }
        else
        {
            btnPage[0].SetActive(index < amount - 1);
            btnPage[1].SetActive(index >= 1);
        }

        if (anim)
            this.DoUIMove(focusdot, dots[index].transform.position, speedDot);
        change?.Invoke(index);

    }
}
