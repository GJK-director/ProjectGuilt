using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BattleHpUIView : MonoBehaviour
{
    [SerializeField] private Image hpFillCurrentImage;
    [SerializeField] private Image hpBackLostImage;
    [SerializeField] private TMP_Text hpText;
    [SerializeField] private bool showMaxHp = false;

    public void SetCharacter(CharacterData characterData)
    {
        if (characterData == null)
        {
            Clear();
            return;
        }

        SetHp(characterData.currentHP, characterData.maxHP);
    }

    public void SetHp(int currentHp, int maxHp)
    {
        if (maxHp <= 0)
        {
            SetFillAmount(0f);
            SetHpText("-");
            return;
        }

        int clampedCurrentHp = Mathf.Clamp(currentHp, 0, maxHp);
        float fillAmount = Mathf.Clamp01((float)clampedCurrentHp / maxHp);

        SetFillAmount(fillAmount);

        string text = showMaxHp
            ? clampedCurrentHp + " / " + maxHp
            : clampedCurrentHp.ToString();

        SetHpText(text);
    }

    public void Clear()
    {
        SetFillAmount(0f);
        SetHpText("-");
    }

    private void SetFillAmount(float fillAmount)
    {
        if (hpFillCurrentImage != null)
        {
            hpFillCurrentImage.fillAmount = fillAmount;
        }

        if (hpBackLostImage != null)
        {
            hpBackLostImage.fillAmount = fillAmount;
        }
    }

    private void SetHpText(string text)
    {
        if (hpText != null)
        {
            hpText.text = text;
        }
    }
}
