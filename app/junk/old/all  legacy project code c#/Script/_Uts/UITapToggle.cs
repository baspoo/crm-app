using UnityEngine;
using System.Collections.Generic;
public class UITapToggle : MonoBehaviour
{


    public List<UIToggle> uIToggles;
    public int currentIndex { get; private set; }
    System.Action<int> callback;
    public void Init(int index, System.Action<int> callback)
    {
        currentIndex = index;
        this.callback = callback;
        Toggle(currentIndex);
    }
    public void Toggle(int index)
    {
        currentIndex = index;
        uIToggles.SingleToggle(index);
        foreach (var tg in uIToggles)
        {
            if (tg == uIToggles[index]) tg.transform.SetAsLastSibling();
            else tg.transform.SetAsFirstSibling();
        }
        callback?.Invoke(index);
    }



}
