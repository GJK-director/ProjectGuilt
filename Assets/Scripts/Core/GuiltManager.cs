// 脚本中文说明：负罪感管理器。负责增加角色负罪感并打印当前负罪感状态。
using UnityEngine;

// GuiltManager = 负罪感管理器
// 负责处理负罪感的增加、打印，以及未来的阈值检查
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

        character.currentGuilt += amount;

        Debug.Log(
            character.characterName +
            " 负罪感增加：" +
            amount +
            "，原因：" +
            reason +
            "，当前负罪感：" +
            character.currentGuilt
        );

        // 阈值效果以后再做
        // CheckGuiltThreshold(character);
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
            character.currentGuilt
        );
    }
}