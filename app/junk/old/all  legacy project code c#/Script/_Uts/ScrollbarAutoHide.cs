using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(CanvasGroup))]
public class ScrollbarAutoHide : MonoBehaviour
{
    [Header("Settings")]
    public ScrollRect scrollRect;      // ลากตัว Scroll Rect มาใส่ตรงนี้
    public float fadeDuration = 0.5f;  // ใช้เวลาจางหายนานแค่ไหน (วินาที)
    public float waitTime = 1.0f;      // หยุดนิ่งนานแค่ไหนถึงจะเริ่มจาง (วินาที)

    private CanvasGroup canvasGroup;
    private float lastScrollTime;
    private bool isFadingOut = false;

    void Start()
    {
        canvasGroup = GetComponent<CanvasGroup>();

        // ถ้าลืมใส่ ScrollRect ให้ลองหาจาก Parent ดู
        if (scrollRect == null)
            scrollRect = GetComponentInParent<ScrollRect>();

        // ดักจับ Event เมื่อมีการเลื่อน Scroll
        if (scrollRect != null)
        {
            scrollRect.onValueChanged.AddListener(OnScrollValueChanged);
        }

        // เริ่มต้นให้โชว์ก่อน หรือซ่อนก่อนก็ได้ (ในที่นี้ให้ซ่อนก่อน)
        canvasGroup.alpha = 0;
    }

    // ฟังก์ชันนี้จะถูกเรียกทุกครั้งที่มีการขยับ Scroll
    void OnScrollValueChanged(Vector2 val)
    {
        // 1. จำเวลาล่าสุดที่ขยับ
        lastScrollTime = Time.time;

        // 2. แสดง Scrollbar ทันที (Alpha = 1)
        canvasGroup.alpha = 1;
        isFadingOut = false;
    }

    void Update()
    {
        // ถ้าเวลาปัจจุบัน เกินเวลาล่าสุดที่ขยับ + เวลาที่ต้องรอ
        if (Time.time > lastScrollTime + waitTime)
        {
            // เริ่มจางหาย
            if (canvasGroup.alpha > 0)
            {
                // ลดค่า Alpha ลงเรื่อยๆ ตามเวลา
                canvasGroup.alpha -= Time.deltaTime / fadeDuration;
            }
        }
    }
}