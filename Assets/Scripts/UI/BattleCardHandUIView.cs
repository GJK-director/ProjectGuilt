using System.Collections.Generic;
using UnityEngine;

public class BattleCardHandUIView : MonoBehaviour
{
    [SerializeField] private BattleCardUIView cardViewPrefab;
    [SerializeField] private Transform cardContainer;
    [SerializeField] private BattleCardManualLayout manualLayout;
    [SerializeField] private bool hideTemplateOnAwake = true;

    private readonly List<BattleCardUIView> spawnedCardViews = new List<BattleCardUIView>();

    void Awake()
    {
        if (cardContainer == null)
        {
            cardContainer = transform;
        }

        if (hideTemplateOnAwake && cardViewPrefab != null)
        {
            cardViewPrefab.gameObject.SetActive(false);
        }
    }

    public void SetCards(
        CharacterData owner,
        CharacterData defaultTarget,
        List<BattleCardState> cardStates
    )
    {
        ClearCards();

        if (cardViewPrefab == null)
        {
            Debug.LogWarning("BattleCardHandUIView 缺少 cardViewPrefab，无法生成手牌UI。");
            return;
        }

        Transform targetContainer = cardContainer != null ? cardContainer : transform;

        if (cardStates == null)
        {
            return;
        }

        for (int i = 0; i < cardStates.Count; i++)
        {
            BattleCardState cardState = cardStates[i];

            if (cardState == null || cardState.cardData == null)
            {
                continue;
            }

            // cardViewPrefab 可以是场景里的模板对象，也可以是 Project 里的 Prefab。
            BattleCardUIView view = Instantiate(cardViewPrefab, targetContainer);
            view.gameObject.SetActive(true);

            BattleCardUIPreviewData previewData = BattleCardUIPreviewBuilder.Build(
                owner,
                defaultTarget,
                cardState
            );

            view.SetCard(previewData);
            spawnedCardViews.Add(view);
        }

        if (manualLayout != null)
        {
            manualLayout.ApplyLayout(spawnedCardViews);
        }
    }

    public void ClearCards()
    {
        for (int i = spawnedCardViews.Count - 1; i >= 0; i--)
        {
            BattleCardUIView view = spawnedCardViews[i];

            if (view == null)
            {
                continue;
            }

            Destroy(view.gameObject);
        }

        spawnedCardViews.Clear();
    }
}
