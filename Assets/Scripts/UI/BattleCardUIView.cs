using TMPro;
using UnityEngine;

public class BattleCardUIView : MonoBehaviour
{
    [SerializeField] private TMP_Text cardNameText;
    [SerializeField] private TMP_Text pointText;
    [SerializeField] private TMP_Text typeText;
    [SerializeField] private TMP_Text descriptionText;
    [SerializeField] private TMP_Text cooldownText;

    public void SetCard(BattleCardUIPreviewData data)
    {
        if (data == null)
        {
            SetEmpty();
            return;
        }

        SetText(cardNameText, data.cardName);
        SetText(pointText, data.pointText);
        SetText(typeText, data.typeText);
        SetText(descriptionText, data.descriptionText);
        SetText(cooldownText, data.cooldownText);
    }

    public void SetEmpty()
    {
        SetText(cardNameText, "空");
        SetText(pointText, "—");
        SetText(typeText, "");
        SetText(descriptionText, "");
        SetText(cooldownText, "CD：—");
    }

    void SetText(TMP_Text text, string value)
    {
        if (text == null)
        {
            return;
        }

        text.text = value;
    }
}
