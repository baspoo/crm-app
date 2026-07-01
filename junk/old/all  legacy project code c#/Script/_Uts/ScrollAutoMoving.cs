using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class ScrollAutoMoving : MonoBehaviour
{
    [Header("Settings")]
    public Transform root;
    public float itemWidth = 500f;
    public float spacing = 20f;
    public float waitTime = 3.0f;
    public float moveDuration = 0.5f;

    [Header("Start Position")]
    [Tooltip("Index ที่จะเริ่มอยู่กลางจอ")]
    public int startIndex = 0;

    [Header("Ease Type")]
    public AnimationCurve moveCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    private List<RectTransform> items = new List<RectTransform>();

    private float stepSize;
    private bool setup = false;
    private bool anim = false;

    Coroutine loopRoutine;

    // ----------------------------

    public void OnSetup()
    {
        items.Clear();

        foreach (Transform child in root)
        {
            if (child.gameObject.activeSelf)
            {
                RectTransform rt = child.GetComponent<RectTransform>();
                if (rt != null)
                    items.Add(rt);
            }
        }

        if (items.Count == 0)
            return;

        stepSize = itemWidth + spacing;

        setup = true;

        ArrangeItemsInitially();

        OnRun();
    }

    void OnEnable()
    {
        if (setup)
            OnRun();
    }

    void OnDisable()
    {
        StopLoop();
    }

    // ----------------------------

    void OnRun()
    {
        if (!setup || items.Count <= 1)
            return;

        StopLoop();

        if (anim)
        {
            anim = false;
            ArrangeItemsInitially();
        }

        if (gameObject.activeInHierarchy)
            loopRoutine = StartCoroutine(ProcessBannerLoop());
    }

    void StopLoop()
    {
        if (loopRoutine != null)
        {
            StopCoroutine(loopRoutine);
            loopRoutine = null;
        }
    }

    // ----------------------------

    void ArrangeItemsInitially()
    {
        if (items.Count == 0)
            return;

        int safeIndex = Mathf.Clamp(startIndex, 0, items.Count - 1);

        for (int i = 0; i < items.Count; i++)
        {
            float startX = (i - safeIndex) * stepSize;
            items[i].anchoredPosition = new Vector2(startX, 0);
        }
    }

    // ----------------------------

    IEnumerator ProcessBannerLoop()
    {
        while (true)
        {
            yield return new WaitForSeconds(waitTime);

            yield return AnimateMove();

            CheckAndReposition();
        }
    }

    // ----------------------------

    IEnumerator AnimateMove()
    {
        anim = true;

        float timer = 0f;

        int count = items.Count;

        List<float> startXPositions = new List<float>(count);

        for (int i = 0; i < count; i++)
            startXPositions.Add(items[i].anchoredPosition.x);

        while (timer < moveDuration)
        {
            timer += Time.deltaTime;

            float progress = timer / moveDuration;
            float curveValue = moveCurve.Evaluate(progress);

            for (int i = 0; i < count; i++)
            {
                float targetX = startXPositions[i] - stepSize;

                float currentX = Mathf.Lerp(startXPositions[i], targetX, curveValue);

                items[i].anchoredPosition = new Vector2(currentX, 0);
            }

            yield return null;
        }

        for (int i = 0; i < count; i++)
        {
            items[i].anchoredPosition = new Vector2(startXPositions[i] - stepSize, 0);
        }

        anim = false;
    }

    // ----------------------------

    void CheckAndReposition()
    {
        if (items.Count == 0)
            return;

        float leftThreshold = -stepSize * 1.5f;

        float maxRightX = float.MinValue;

        for (int i = 0; i < items.Count; i++)
        {
            float x = items[i].anchoredPosition.x;

            if (x > maxRightX)
                maxRightX = x;
        }

        for (int i = 0; i < items.Count; i++)
        {
            RectTransform item = items[i];

            if (item.anchoredPosition.x < leftThreshold)
            {
                float newX = maxRightX + stepSize;

                item.anchoredPosition = new Vector2(newX, 0);

                maxRightX = newX;
            }
        }
    }
}