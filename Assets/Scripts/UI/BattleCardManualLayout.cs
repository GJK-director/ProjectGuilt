using System.Collections.Generic;
using UnityEngine;

public class BattleCardManualLayout : MonoBehaviour
{
    [SerializeField] private RectTransform normal01;
    [SerializeField] private RectTransform normal02;
    [SerializeField] private RectTransform normal03;
    [SerializeField] private RectTransform normal04;
    [SerializeField] private RectTransform normal05;
    [SerializeField] private RectTransform normal06;

    [SerializeField] private bool matchSlotSize = true;
    [SerializeField] private bool matchSlotRotation = true;
    [SerializeField] private bool matchSlotScale = true;

    public void ApplyLayout(IReadOnlyList<BattleCardUIView> cardViews)
    {
        if (cardViews == null)
        {
            return;
        }

        RectTransform[] placementOrder =
        {
            normal03,
            normal04,
            normal02,
            normal05,
            normal01,
            normal06
        };

        List<RectTransform> placedCards = new List<RectTransform>();

        for (int i = 0; i < cardViews.Count; i++)
        {
            BattleCardUIView cardView = cardViews[i];

            if (cardView == null)
            {
                continue;
            }

            if (i >= placementOrder.Length)
            {
                Debug.LogWarning("BattleCardManualLayout 当前最多显示6张手牌，超出的卡牌已隐藏。");
                cardView.gameObject.SetActive(false);
                continue;
            }

            RectTransform slot = placementOrder[i];

            if (slot == null)
            {
                Debug.LogWarning("BattleCardManualLayout 缺少第 " + (i + 1) + " 张卡对应的槽位引用。");
                cardView.gameObject.SetActive(false);
                continue;
            }

            RectTransform cardRect = cardView.transform as RectTransform;

            if (cardRect == null)
            {
                Debug.LogWarning("BattleCardManualLayout 只能排布带 RectTransform 的卡牌UI。");
                cardView.gameObject.SetActive(false);
                continue;
            }

            cardView.gameObject.SetActive(true);

            // 只复制 Unity 中已经摆好的槽位数据，不在代码里写固定坐标。
            cardRect.anchorMin = slot.anchorMin;
            cardRect.anchorMax = slot.anchorMax;
            cardRect.pivot = slot.pivot;
            cardRect.anchoredPosition = slot.anchoredPosition;

            Vector3 localPosition = cardRect.localPosition;
            localPosition.z = slot.localPosition.z;
            cardRect.localPosition = localPosition;

            if (matchSlotSize)
            {
                cardRect.sizeDelta = slot.sizeDelta;
            }

            if (matchSlotRotation)
            {
                cardRect.localRotation = slot.localRotation;
            }

            if (matchSlotScale)
            {
                cardRect.localScale = slot.localScale;
            }

            placedCards.Add(cardRect);
        }

        SortSiblingIndexByX(placedCards);
    }

    private void SortSiblingIndexByX(List<RectTransform> cardRects)
    {
        if (cardRects == null || cardRects.Count <= 1)
        {
            return;
        }

        cardRects.Sort((left, right) => left.anchoredPosition.x.CompareTo(right.anchoredPosition.x));

        for (int i = 0; i < cardRects.Count; i++)
        {
            cardRects[i].SetSiblingIndex(i);
        }
    }
}
