using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public sealed class BattleCardHandSpreadAnimator : MonoBehaviour
{
    [Header("手牌散开")]
    [SerializeField] private bool enableSpreadAnimation = true;

    [Tooltip("位置贴近最终布局的速度。13.3886在60 FPS下约等于每帧移动剩余距离20%。")]
    [Min(0f)]
    [SerializeField] private float positionSharpness = 13.3886f;

    [Tooltip("旋转贴近最终布局的速度。13.3886在60 FPS下约等于每帧移动剩余距离20%。")]
    [Min(0f)]
    [SerializeField] private float rotationSharpness = 13.3886f;

    [SerializeField] private Vector2 spreadCenterOffset = Vector2.zero;
    [SerializeField] private float spreadStartRotationZ = 0f;

    [Tooltip("位置进入该距离后精确吸附到最终布局。")]
    [Min(0f)]
    [SerializeField] private float positionSnapDistance = 0.1f;

    [Tooltip("旋转进入该角度后精确吸附到最终布局。")]
    [Min(0f)]
    [SerializeField] private float rotationSnapAngle = 0.05f;

    private sealed class SpreadTarget
    {
        public RectTransform cardRoot;
        public Vector2 finalAnchoredPosition;
        public Quaternion finalLocalRotation;
        public Vector3 finalLocalScale;
    }

    private readonly List<SpreadTarget> targets =
        new List<SpreadTarget>();
    private Coroutine spreadCoroutine;

    internal int ActiveTransitionCount =>
        spreadCoroutine != null ? 1 : 0;
    internal int CachedCardCount => targets.Count;

    public void PlaySpread(
        IReadOnlyList<BattleCardUIView> cardViews
    )
    {
        StopAndClear();

        if (!enableSpreadAnimation ||
            cardViews == null ||
            cardViews.Count == 0)
        {
            return;
        }

        Vector2 finalPositionSum = Vector2.zero;

        for (int index = 0; index < cardViews.Count; index++)
        {
            BattleCardUIView cardView = cardViews[index];
            if (cardView == null || !cardView.gameObject.activeSelf)
            {
                continue;
            }

            RectTransform cardRoot =
                cardView.transform as RectTransform;
            if (cardRoot == null)
            {
                continue;
            }

            SpreadTarget target = new SpreadTarget
            {
                cardRoot = cardRoot,
                finalAnchoredPosition = cardRoot.anchoredPosition,
                finalLocalRotation = cardRoot.localRotation,
                finalLocalScale = cardRoot.localScale
            };
            targets.Add(target);
            finalPositionSum += target.finalAnchoredPosition;
        }

        if (targets.Count == 0)
        {
            return;
        }

        Vector2 spreadCenter =
            finalPositionSum / targets.Count +
            spreadCenterOffset;
        Quaternion startRotation =
            Quaternion.Euler(0f, 0f, spreadStartRotationZ);

        for (int index = 0; index < targets.Count; index++)
        {
            SpreadTarget target = targets[index];
            target.cardRoot.anchoredPosition = spreadCenter;
            target.cardRoot.localRotation = startRotation;
            target.cardRoot.localScale = target.finalLocalScale;
        }

        if (!isActiveAndEnabled || !gameObject.activeInHierarchy)
        {
            SnapToFinalAndClear();
            return;
        }

        spreadCoroutine = StartCoroutine(TrackSpread());
    }

    public void StopAndClear()
    {
        StopSpreadCoroutine();
        targets.Clear();
    }

    internal bool AdvanceSpreadForTesting(float unscaledDeltaTime)
    {
        if (targets.Count == 0)
        {
            return true;
        }

        bool completed = AdvanceSpread(unscaledDeltaTime);
        if (completed)
        {
            StopSpreadCoroutine();
            SnapToFinalAndClear();
        }

        return completed;
    }

    internal void CompleteSpreadImmediatelyForTesting()
    {
        StopSpreadCoroutine();
        SnapToFinalAndClear();
    }

    private IEnumerator TrackSpread()
    {
        while (true)
        {
            yield return null;

            if (AdvanceSpread(Time.unscaledDeltaTime))
            {
                spreadCoroutine = null;
                SnapToFinalAndClear();
                yield break;
            }
        }
    }

    private bool AdvanceSpread(float unscaledDeltaTime)
    {
        bool allReached = true;

        for (int index = 0; index < targets.Count; index++)
        {
            SpreadTarget target = targets[index];
            if (target.cardRoot == null)
            {
                continue;
            }

            Vector2 nextPosition =
                BattleUIExponentialSmoothing.Smooth(
                    target.cardRoot.anchoredPosition,
                    target.finalAnchoredPosition,
                    positionSharpness,
                    unscaledDeltaTime
                );
            Quaternion nextRotation =
                BattleUIExponentialSmoothing.Smooth(
                    target.cardRoot.localRotation,
                    target.finalLocalRotation,
                    rotationSharpness,
                    unscaledDeltaTime
                );

            bool positionReached =
                Vector2.Distance(
                    nextPosition,
                    target.finalAnchoredPosition
                ) <= Mathf.Max(0f, positionSnapDistance);
            bool rotationReached =
                Quaternion.Angle(
                    nextRotation,
                    target.finalLocalRotation
                ) <= Mathf.Max(0f, rotationSnapAngle);

            target.cardRoot.anchoredPosition = positionReached
                ? target.finalAnchoredPosition
                : nextPosition;
            target.cardRoot.localRotation = rotationReached
                ? target.finalLocalRotation
                : nextRotation;

            allReached &= positionReached && rotationReached;
        }

        return allReached;
    }

    private void SnapToFinalAndClear()
    {
        for (int index = 0; index < targets.Count; index++)
        {
            SpreadTarget target = targets[index];
            if (target.cardRoot == null)
            {
                continue;
            }

            target.cardRoot.anchoredPosition =
                target.finalAnchoredPosition;
            target.cardRoot.localRotation =
                target.finalLocalRotation;
            target.cardRoot.localScale =
                target.finalLocalScale;
        }

        targets.Clear();
    }

    private void StopSpreadCoroutine()
    {
        if (spreadCoroutine == null)
        {
            return;
        }

        StopCoroutine(spreadCoroutine);
        spreadCoroutine = null;
    }

    void OnDisable()
    {
        StopAndClear();
    }

    void OnDestroy()
    {
        StopAndClear();
    }
}
