// 脚本中文说明：负罪感管理器。负责增加角色负罪感并打印当前负罪感状态。
using UnityEngine;

// GuiltManager = 负罪感管理器
// 正式战斗写入 BattleRuntimeState；未绑定 RuntimeState 时兼容旧测试数据。
public static class GuiltManager
{
    // AddGuilt = 增加负罪感
    public static void AddGuilt(CharacterData character, int amount, string reason)
    {
        if (character == null)
        {
            Debug.LogWarning("增加负罪感失败：角色为空");
            return;
        }

        if (amount <= 0)
        {
            return;
        }

        BattleRuntimeState runtimeState = character.GetSharedGuiltRuntimeState();

        if (runtimeState != null)
        {
            runtimeState.AddGuilt(amount);
        }
        else
        {
            // 未接入 RuntimeState 的旧测试继续使用角色级兼容值。
            character.currentGuilt += amount;
        }

        Debug.Log(
            character.characterName +
            " 负罪感增加：" +
            amount +
            "，原因：" +
            reason +
            "，当前负罪感：" +
            GetCurrentGuilt(character)
        );

        // 阈值效果以后再做
        // CheckGuiltThreshold(character);
    }

    public static int GetCurrentGuilt(CharacterData character)
    {
        if (character == null)
        {
            return 0;
        }

        BattleRuntimeState runtimeState = character.GetSharedGuiltRuntimeState();
        return runtimeState != null ? runtimeState.currentGuilt : character.currentGuilt;
    }

    // PrintGuilt = 打印当前负罪感
    public static void PrintGuilt(CharacterData character)
    {
        if (character == null)
        {
            return;
        }

        Debug.Log(
            character.characterName +
            " 当前负罪感：" +
            GetCurrentGuilt(character)
        );
    }
}
