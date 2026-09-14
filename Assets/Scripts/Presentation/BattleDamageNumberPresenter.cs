using TMPro;
using UnityEngine;

// 独立的伤害数字显示器：只读取已结算Impact的resolvedDamage，不参与战斗计算。
public sealed class BattleDamageNumberPresenter : MonoBehaviour
{
    [SerializeField, Min(0f)] private float fontSize = 32f;
    [SerializeField] private Color textColor = Color.white;
    [SerializeField, Min(0f)] private float lifetime = 0.6f;

    bool warnedMissingBinding;

    public void Present(
        BattleImpact impact,
        Transform worldAnchor,
        Canvas targetCanvas,
        Camera worldCamera
    )
    {
        if (impact == null || worldAnchor == null || targetCanvas == null ||
            worldCamera == null)
        {
            WarnMissingBinding();
            return;
        }

        RectTransform canvasRect = targetCanvas.transform as RectTransform;
        if (canvasRect == null)
        {
            WarnMissingBinding();
            return;
        }

        Vector3 screenPoint = worldCamera.WorldToScreenPoint(worldAnchor.position);
        if (screenPoint.z <= 0f)
        {
            return;
        }

        Camera eventCamera = targetCanvas.renderMode == RenderMode.ScreenSpaceOverlay
            ? null
            : worldCamera;
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvasRect,
                screenPoint,
                eventCamera,
                out Vector2 localPoint
            ))
        {
            return;
        }

        GameObject numberObject = new GameObject("BattleDamageNumber");
        RectTransform numberRect = numberObject.AddComponent<RectTransform>();
        numberRect.SetParent(canvasRect, false);
        numberRect.sizeDelta = new Vector2(160f, 60f);
        numberRect.anchoredPosition = localPoint;

        TextMeshProUGUI text = numberObject.AddComponent<TextMeshProUGUI>();
        text.text = impact.resolvedDamage.ToString();
        text.fontSize = fontSize;
        text.color = textColor;
        text.alignment = TextAlignmentOptions.Center;
        text.raycastTarget = false;

        if (lifetime <= 0f)
        {
            Destroy(numberObject);
        }
        else
        {
            Destroy(numberObject, lifetime);
        }
    }

    void WarnMissingBinding()
    {
        if (warnedMissingBinding)
        {
            return;
        }

        warnedMissingBinding = true;
        Debug.LogWarning(
            "Damage Number显示失败：缺少Impact、World Anchor、Canvas或World Camera绑定。",
            this
        );
    }
}
