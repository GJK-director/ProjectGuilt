using TMPro;
using UnityEngine;

// 战斗一级 UI：只显示当前回合数字，不负责推进回合。
public sealed class BattleRoundUIView : MonoBehaviour
{
    [SerializeField] private TMP_Text roundNumberText;

    public void SetRound(int roundNumber)
    {
        if (roundNumberText == null)
        {
            return;
        }

        roundNumberText.text = Mathf.Max(1, roundNumber).ToString();
    }

    public void Clear()
    {
        SetRound(1);
    }
}
