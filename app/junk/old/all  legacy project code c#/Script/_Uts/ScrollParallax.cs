//using UnityEngine;

//public class ScrollParallax : MonoBehaviour
//{
//    static ScrollParallax target;
//    [Header("At Target")]
//    public UnityEngine.UI.ScrollRect scrollRect;
//    private void OnEnable()
//    {
//        if(!isMain)
//            target = this;
//    }


//    [Header("Main")]
//    public bool isMain;
//    public Canvas canvas;
//    public float speed;
//    void Start()
//    {
//        if (isMain)
//        {
//            transform.parent = null;
//            transform.SetSiblingIndex(1);
//            this.DoRefresh(() => {
//                canvas.renderMode = RenderMode.ScreenSpaceCamera;
//                canvas.worldCamera = Camera.main;
//            });
//        }
//    }

    
//    void Update()
//    {
        
//    }
//}
