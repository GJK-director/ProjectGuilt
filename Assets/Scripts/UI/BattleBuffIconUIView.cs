using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class BattleBuffIconUIView : MonoBehaviour, IPointerClickHandler
{
    [SerializeField] private Image iconImage;
    [SerializeField] private TMP_Text stackText;
    [SerializeField] private TMP_Text decayText;

    private bool isOverflow;
    private int overflowHiddenCount;
    private Action<int> overflowClickHandler;
    private bool hasExplicitVisualState;

    public bool IsOverflow => isOverflow;
    public int OverflowHiddenCount => overflowHiddenCount;
    public bool HasRequiredVisualReferences =>
        iconImage != null && stackText != null;

    void Awake()
    {
        if (!hasExplicitVisualState)
        {
            SetEmpty();
        }
    }

    public void SetBuff(Sprite iconSprite, int stack, int endTurnDelta = 0)
    {
        hasExplicitVisualState = true;

        if (stack <= 0)
        {
            SetEmpty();
            return;
        }

        isOverflow = false;
        overflowHiddenCount = 0;
        gameObject.SetActive(true);

        if (iconImage != null)
        {
            iconImage.sprite = iconSprite;
            iconImage.enabled = iconSprite != null;
        }

        if (stackText != null)
        {
            stackText.gameObject.SetActive(true);
            stackText.text = stack.ToString();
        }

        if (decayText != null)
        {
            bool showDecay = endTurnDelta < 0;
            decayText.gameObject.SetActive(showDecay);
            decayText.text = showDecay ? endTurnDelta.ToString() : "";
        }
    }

    public void SetOverflow(
        Sprite iconSprite,
        int hiddenCount,
        string prefix = "...+"
    )
    {
        hasExplicitVisualState = true;

        if (hiddenCount <= 0)
        {
            SetEmpty();
            return;
        }

        isOverflow = true;
        overflowHiddenCount = hiddenCount;
        gameObject.SetActive(true);

        if (iconImage != null)
        {
            iconImage.sprite = iconSprite;
            iconImage.enabled = iconSprite != null;
        }

        if (stackText != null)
        {
            stackText.gameObject.SetActive(true);
            stackText.text =
                (prefix ?? string.Empty) + hiddenCount;
        }

        if (decayText != null)
        {
            decayText.text = "";
            decayText.gameObject.SetActive(false);
        }
    }

    public void SetOverflowClickHandler(Action<int> handler)
    {
        overflowClickHandler = handler;
    }

    internal void ConfigureTestVisuals(
        Image image,
        TMP_Text stack,
        TMP_Text decay
    )
    {
        iconImage = image;
        stackText = stack;
        decayText = decay;
        SetEmpty();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData == null ||
            eventData.button != PointerEventData.InputButton.Left ||
            !isOverflow)
        {
            return;
        }

        overflowClickHandler?.Invoke(overflowHiddenCount);
    }

    public void SetEmpty()
    {
        hasExplicitVisualState = true;
        isOverflow = false;
        overflowHiddenCount = 0;

        if (iconImage != null)
        {
            iconImage.sprite = null;
            iconImage.enabled = false;
        }

        if (stackText != null)
        {
            stackText.text = "";
            stackText.gameObject.SetActive(false);
        }

        if (decayText != null)
        {
            decayText.text = "";
            decayText.gameObject.SetActive(false);
        }

        gameObject.SetActive(false);
    }
}
