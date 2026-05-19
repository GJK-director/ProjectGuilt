using UnityEngine;

// BattleTargeting = 战斗目标处理器
// 负责判断攻击目标、介入保护等逻辑
public static class BattleTargeting
{
    // CanInterceptAttack = 判断是否可以介入攻击
    // interceptor = 介入者，比如我方角色A
    // attacker = 攻击者，比如敌人
    // originalTarget = 原本被攻击的人，比如我方角色B
    // CanInterceptAttack = 判断是否可以介入攻击
    // interceptor = 介入者，比如我方角色A
    // attacker = 攻击者，比如敌人
    // originalTarget = 原本被攻击的人，比如我方角色B
    public static bool CanInterceptAttack(
        CharacterData interceptor,
        CharacterData attacker,
        CharacterData originalTarget
    )
    {
        int interceptorSpeed = interceptor.GetCurrentSpeed();
        int attackerSpeed = attacker.GetCurrentSpeed();

        if (BattleDebugSettings.ShowSpeedLog)
        {
            Debug.Log(
                interceptor.characterName +
                " 当前速度：" + interceptorSpeed +
                "；" +
                attacker.characterName +
                " 当前速度：" + attackerSpeed
            );
        }

        // 当前规则：
        // 介入者当前速度比攻击者当前速度高 1 或以上，就可以介入
        if (interceptorSpeed >= attackerSpeed + 1)
        {
            Debug.Log(
                interceptor.characterName +
                " 速度高于 " +
                attacker.characterName +
                "，可以介入保护 " +
                originalTarget.characterName
            );

            return true;
        }

        Debug.Log(
            interceptor.characterName +
            " 速度不足，无法介入保护 " +
            originalTarget.characterName
        );

        return false;
    }
}