using UnityEngine;

[UnityEngine.Scripting.Preserve]
public class UIMainBtn : MonoBehaviour
{
    public UIPage.PageType pageType;
    public UnityEngine.UI.Button button;
    public Animator animator;
    public RectTransform rectPointer;
    public RectTransform rectBase;
    public bool isActive {  get; private set; }
    Console console;
    public void Init(Console console)
    {
        this.console = console;
    }
    public void OnSeletion()
    {
        CustomerTheme.instance.SfxSelect.Play();
        console.OpenPage(pageType);
    }
    public void OnActive()
    {
        isActive = true;
        animator.SetBool("active",true);
    }
    public void OnDeactive()
    {
        isActive = false;
        animator.SetBool("active", false);
    }
}
