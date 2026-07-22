using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BattleBuffIconUIView : MonoBehaviour
{
    [SerializeField] private Image iconImage;
    [SerializeField] private TMP_Text stackText;
    [SerializeField] private TMP_Text decayText;

    void Awake()
    {
        SetEmpty();
    }

    public void SetBuff(Sprite iconSprite, int stack, int endTurnDelta = 0)
    {
        if (stack <= 0)
        {
            SetEmpty();
            return;
        }

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

    public void SetEmpty()
    {
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
