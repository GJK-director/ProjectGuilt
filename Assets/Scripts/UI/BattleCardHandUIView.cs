using System.Collections.Generic;
using UnityEngine;

public class BattleCardHandUIView : MonoBehaviour
{
    [SerializeField] private BattleCardUIView cardViewPrefab;
    [SerializeField] private Transform cardContainer;
    [SerializeField] private BattleCardManualLayout manualLayout;
    [SerializeField] private BattleCardHandSpreadAnimator spreadAnimator;
    [SerializeField] private bool hideTemplateOnAwake = true;

    private readonly List<BattleCardUIView> spawnedCardViews =
        new List<BattleCardUIView>();
    private BattleCardSelectionController selectionController;
    private CharacterData lastDisplayedOwner;
    private bool hasDisplayedAnyHand;

    internal IReadOnlyList<BattleCardUIView> SpawnedCardViews =>
        spawnedCardViews;
    internal CharacterData LastDisplayedOwner => lastDisplayedOwner;
    internal bool HasDisplayedAnyHand => hasDisplayedAnyHand;

    void Awake()
    {
        if (cardContainer == null)
        {
            cardContainer = transform;
        }

        if (spreadAnimator == null)
        {
            spreadAnimator =
                GetComponent<BattleCardHandSpreadAnimator>();
        }

        if (hideTemplateOnAwake &&
            cardViewPrefab != null &&
            cardViewPrefab.gameObject.scene.IsValid())
        {
            cardViewPrefab.gameObject.SetActive(false);
        }
    }

    public void SetSelectionController(
        BattleCardSelectionController controller
    )
    {
        if (!object.ReferenceEquals(selectionController, controller))
        {
            selectionController?.ClearSelection();
        }

        selectionController = controller;
    }

    public void SetCards(
        CharacterData owner,
        CharacterData defaultTarget,
        List<BattleCardState> cardStates
    )
    {
        bool shouldPlaySpread =
            !hasDisplayedAnyHand ||
            !object.ReferenceEquals(lastDisplayedOwner, owner);

        ClearCards();

        if (cardViewPrefab == null)
        {
            Debug.LogWarning("BattleCardHandUIView 缺少 cardViewPrefab，无法生成手牌UI。");
            return;
        }

        Transform targetContainer =
            cardContainer != null ? cardContainer : transform;

        if (cardStates != null)
        {
            for (int i = 0; i < cardStates.Count; i++)
            {
                BattleCardState cardState = cardStates[i];
                if (cardState == null || cardState.cardData == null)
                {
                    continue;
                }

                // 克隆体先保持不可见，完成绑定后才允许接收指针事件。
                BattleCardUIView view = Instantiate(
                    cardViewPrefab,
                    targetContainer
                );
                view.gameObject.SetActive(false);

                BattleCardUIPreviewData previewData =
                    BattleCardUIPreviewBuilder.Build(
                        owner,
                        defaultTarget,
                        cardState
                    );

                view.BindCard(
                    owner,
                    cardState,
                    previewData,
                    selectionController
                );
                spawnedCardViews.Add(view);
                view.gameObject.SetActive(true);
            }
        }

        if (manualLayout != null)
        {
            manualLayout.ApplyLayout(spawnedCardViews);
        }

        if (shouldPlaySpread)
        {
            spreadAnimator?.PlaySpread(spawnedCardViews);
        }

        lastDisplayedOwner = owner;
        hasDisplayedAnyHand = owner != null;
    }

    public void ClearCards()
    {
        spreadAnimator?.StopAndClear();
        selectionController?.ClearSelection();

        for (int i = spawnedCardViews.Count - 1; i >= 0; i--)
        {
            BattleCardUIView view = spawnedCardViews[i];
            if (view == null)
            {
                continue;
            }

            // Destroy 延迟到帧末，先禁用可避免旧手牌继续接收点击。
            view.gameObject.SetActive(false);
            Destroy(view.gameObject);
        }

        spawnedCardViews.Clear();
        lastDisplayedOwner = null;
        hasDisplayedAnyHand = false;
    }
}
