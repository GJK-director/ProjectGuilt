using TMPro;
using UnityEngine;

// 战斗一级 UI：只显示负罪感数值，不修改角色数据。
public sealed class BattleGuiltUIView : MonoBehaviour
{
    [SerializeField] private TMP_Text guiltValueText;

    public void SetGuilt(int guiltValue)
    {
        if (guiltValueText == null)
        {
            return;
        }

        guiltValueText.text = guiltValue.ToString();
    }

    public void Clear()
    {
        SetGuilt(0);
    }
}
