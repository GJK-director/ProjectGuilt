using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Read-only screen-space prototype HUD. No dice rolls or combat mutation.
public sealed class BattleCombatDiceHUD : MonoBehaviour
{
    RectTransform panel;
    TextMeshProUGUI label;

    public void Show(string message, Color color, TMP_FontAsset font)
    {
        if (panel == null)
        {
            var root = new GameObject("CombatDiceHUD", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
            root.transform.SetParent(transform, false);
            Canvas canvas = root.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 32000;
            CanvasScaler scaler = root.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            var background = new GameObject("DicePanel", typeof(RectTransform), typeof(Image));
            panel = background.GetComponent<RectTransform>();
            panel.SetParent(root.transform, false);
            panel.anchorMin = panel.anchorMax = new Vector2(0.5f, 1f);
            panel.pivot = new Vector2(0.5f, 1f);
            panel.anchoredPosition = new Vector2(0, -35);
            panel.sizeDelta = new Vector2(440, 180);
            Image image = background.GetComponent<Image>();
            image.color = new Color(0.04f, 0.05f, 0.07f, 0.94f);
            image.raycastTarget = false;
            label = new GameObject("DiceText", typeof(RectTransform), typeof(TextMeshProUGUI)).GetComponent<TextMeshProUGUI>();
            label.rectTransform.SetParent(panel, false);
            label.rectTransform.anchorMin = Vector2.zero;
            label.rectTransform.anchorMax = Vector2.one;
            label.rectTransform.offsetMin = new Vector2(15, 10);
            label.rectTransform.offsetMax = new Vector2(-15, -10);
            label.fontSize = 28;
            label.alignment = TextAlignmentOptions.Center;
            label.raycastTarget = false;
        }
        if (font != null) label.font = font;
        label.color = color;
        label.text = message;
        panel.gameObject.SetActive(true);
    }

    public void Hide()
    {
        if (panel != null) panel.gameObject.SetActive(false);
    }
}
