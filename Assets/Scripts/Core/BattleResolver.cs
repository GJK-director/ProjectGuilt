// 脚本中文说明：战斗结算器。负责处理卡牌使用、拼点、生效、命中、伤害和击杀事件。
using UnityEngine;

// BattleResolver = 战斗结算器
// 负责处理卡牌使用、拼点、生效、命中、伤害、击杀等流程
public static class BattleResolver
{
    // TestUseAbilitySinCard = 测试 / 使用能力型罪卡
    // 能力型罪卡不进入拼点，成功使用后直接触发 OnPlay，再进入 Resolved 处理负罪感和使用次数
    public static void TestUseAbilitySinCard(
        CharacterData user,
        BattleCardState abilityCardState,
        CharacterData target
    )
    {
        if (user == null)
        {
            Debug.LogWarning("能力型罪卡使用失败：使用者为空");
            return;
        }

        if (abilityCardState == null || abilityCardState.cardData == null)
        {
            Debug.LogWarning("能力型罪卡使用失败：卡牌状态或卡牌数据为空");
            return;
        }

        if (!abilityCardState.IsAbilitySinCard())
        {
            Debug.LogWarning(abilityCardState.GetCardName() + " 不是能力型罪卡，不能走 Ability 流程");
            return;
        }

        if (!BattleCardManager.CanUseCard(user, target, abilityCardState))
        {
            Debug.LogWarning(user.characterName + " 的能力型罪卡不能使用：" + abilityCardState.GetCardName());
            return;
        }

        Debug.Log(user.characterName + " 使用能力型罪卡：" + abilityCardState.GetCardName() + "，不进入拼点");

        // Ability 罪卡直接执行 OnPlay effects
        TriggerBattleEvent(BattleTiming.OnPlay, user, target, abilityCardState, 0, 0, false, false);

        // 成功使用后统一走 Resolved，让 BattleCardManager 处理 guiltGain / UseCount / Permanent
        TriggerBattleEvent(BattleTiming.Resolved, user, target, abilityCardState, 0, 0, false, false);
    }
    // TestClash = 测试一次战斗结算
    public static void TestClash(
        CharacterData allyUnit,
        BattleCardState allyCardState,
        CharacterData enemyUnit,
        BattleCardState enemyCardState
    )
    {
        if (allyUnit == null || enemyUnit == null)
        {
            Debug.LogWarning("战斗结算失败：角色为空");
            return;
        }

        if (allyCardState == null || enemyCardState == null)
        {
            Debug.LogWarning("战斗结算失败：战斗卡牌状态为空");
            return;
        }

        if (allyCardState.cardData == null || enemyCardState.cardData == null)
        {
            Debug.LogWarning("战斗结算失败：卡牌数据为空");
            return;
        }

        CardTestData allyCard = allyCardState.cardData;
        CardTestData enemyCard = enemyCardState.cardData;

        // 卡牌使用前事件
        // 旧 JSON 的 OnPlay 会在 CardEffectExecutor 里兼容成 BeforeUse
        TriggerBattleEvent(BattleTiming.BeforeUse, enemyUnit, allyUnit, enemyCardState, 0, 0, false, false);
        TriggerBattleEvent(BattleTiming.BeforeUse, allyUnit, enemyUnit, allyCardState, 0, 0, false, false);

        if (allyCard.cardType == CardType.Attack && enemyCard.cardType == CardType.Attack)
        {
            HandleAttackVsAttack(allyUnit, allyCardState, enemyUnit, enemyCardState);
            return;
        }

        if (allyCard.cardType == CardType.Dodge && enemyCard.cardType == CardType.Attack)
        {
            HandleDodgeVsMultipleAttacks(allyUnit, allyCardState, enemyUnit, enemyCardState);
            return;
        }

        if (allyCard.cardType == CardType.Defense && enemyCard.cardType == CardType.Attack)
        {
            HandleDefenseVsEnemyAttack(allyUnit, allyCardState, enemyUnit, enemyCardState);
            return;
        }

        Debug.LogWarning(
            "暂未处理的卡牌对抗类型：我方 " +
            allyCard.cardType +
            " / 敌人 " +
            enemyCard.cardType
        );
    }


    // ================================
    // 攻击 vs 攻击
    // ================================

    static void HandleAttackVsAttack(
        CharacterData allyUnit,
        BattleCardState allyCardState,
        CharacterData enemyUnit,
        BattleCardState enemyCardState
    )
    {
        CardTestData allyCard = allyCardState.cardData;
        CardTestData enemyCard = enemyCardState.cardData;

        enemyUnit.CheckBuffsByTiming(BattleTiming.ClashStart);
        allyUnit.CheckBuffsByTiming(BattleTiming.ClashStart);

        int allyPoint = RollCardPoint(allyCard);
        int enemyPoint = RollCardPoint(enemyCard);

        if (allyPoint > enemyPoint)
        {
            Debug.Log(allyUnit.characterName + " 拼点胜利");

            // 我方拼点胜利
            TriggerBattleEvent(BattleTiming.ClashWin, allyUnit, enemyUnit, allyCardState, allyPoint, 0, false, false, ClashResult.Win);

            // 敌人拼点失败
            TriggerBattleEvent(BattleTiming.ClashLose, enemyUnit, allyUnit, enemyCardState, enemyPoint, 0, false, false, ClashResult.Lose);

            // 我方攻击卡生效
            TriggerBattleEvent(BattleTiming.Resolved, allyUnit, enemyUnit, allyCardState, allyPoint, 0, false, false, ClashResult.Win);

            int damageScaled = BattleCalculator.GetFinalDamageScaled(allyUnit, enemyUnit, allyCard, allyPoint);
            int hpDamage = BattleCalculator.ConvertScaledDamageToHPDamage(damageScaled);

            TriggerBattleEvent(BattleTiming.Hit, allyUnit, enemyUnit, allyCardState, allyPoint, hpDamage, true, false, ClashResult.Win);

            ApplyDamageAndTriggerEvents(allyUnit, enemyUnit, allyCardState, hpDamage, allyPoint);
        }
        else if (allyPoint < enemyPoint)
        {
            Debug.Log(enemyUnit.characterName + " 拼点胜利");

            // 敌人拼点胜利
            TriggerBattleEvent(BattleTiming.ClashWin, enemyUnit, allyUnit, enemyCardState, enemyPoint, 0, false, false, ClashResult.Win);

            // 我方拼点失败
            TriggerBattleEvent(BattleTiming.ClashLose, allyUnit, enemyUnit, allyCardState, allyPoint, 0, false, false, ClashResult.Lose);

            // 敌人攻击卡生效
            TriggerBattleEvent(BattleTiming.Resolved, enemyUnit, allyUnit, enemyCardState, enemyPoint, 0, false, false, ClashResult.Win);

            int damageScaled = BattleCalculator.GetFinalDamageScaled(enemyUnit, allyUnit, enemyCard, enemyPoint);
            int hpDamage = BattleCalculator.ConvertScaledDamageToHPDamage(damageScaled);

            TriggerBattleEvent(BattleTiming.Hit, enemyUnit, allyUnit, enemyCardState, enemyPoint, hpDamage, true, false, ClashResult.Win);

            ApplyDamageAndTriggerEvents(enemyUnit, allyUnit, enemyCardState, hpDamage, enemyPoint);
        }
        else
        {
            Debug.Log("拼点平局，双方攻击抵消");
        }
    }


    // ================================
    // 闪避 vs 攻击
    // ================================

    static void HandleDodgeVsMultipleAttacks(
        CharacterData allyUnit,
        BattleCardState dodgeCardState,
        CharacterData enemyUnit,
        BattleCardState enemyCardState
    )
    {
        CardTestData dodgeCard = dodgeCardState.cardData;
        CardTestData enemyCard = enemyCardState.cardData;

        enemyUnit.CheckBuffsByTiming(BattleTiming.ClashStart);
        allyUnit.CheckBuffsByTiming(BattleTiming.ClashStart);

        int dodgePoint = RollCardPoint(dodgeCard);
        int enemyPoint = RollCardPoint(enemyCard);

        if (dodgePoint > enemyPoint)
        {
            Debug.Log(allyUnit.characterName + " 闪避成功，闪避卡继续保留");

            // 闪避卡拼点胜利
            TriggerBattleEvent(BattleTiming.ClashWin, allyUnit, enemyUnit, dodgeCardState, dodgePoint, 0, false, false, ClashResult.Win);

            // 攻击卡拼点失败
            TriggerBattleEvent(BattleTiming.ClashLose, enemyUnit, allyUnit, enemyCardState, enemyPoint, 0, false, false, ClashResult.Lose);

            // 闪避卡生效
            TriggerBattleEvent(BattleTiming.Resolved, allyUnit, enemyUnit, dodgeCardState, dodgePoint, 0, false, false, ClashResult.Win);
        }
        else if (enemyPoint > dodgePoint)
        {
            Debug.Log(allyUnit.characterName + " 闪避失败，闪避被打破");

            // 攻击卡拼点胜利
            TriggerBattleEvent(BattleTiming.ClashWin, enemyUnit, allyUnit, enemyCardState, enemyPoint, 0, false, false, ClashResult.Win);

            // 闪避卡拼点失败
            TriggerBattleEvent(BattleTiming.ClashLose, allyUnit, enemyUnit, dodgeCardState, dodgePoint, 0, false, false, ClashResult.Lose);

            // 攻击卡生效
            TriggerBattleEvent(BattleTiming.Resolved, enemyUnit, allyUnit, enemyCardState, enemyPoint, 0, false, false, ClashResult.Win);

            int damageScaled = BattleCalculator.GetFinalDamageScaled(enemyUnit, allyUnit, enemyCard, enemyPoint);
            int hpDamage = BattleCalculator.ConvertScaledDamageToHPDamage(damageScaled);

            TriggerBattleEvent(BattleTiming.Hit, enemyUnit, allyUnit, enemyCardState, enemyPoint, hpDamage, true, false, ClashResult.Win);

            ApplyDamageAndTriggerEvents(enemyUnit, allyUnit, enemyCardState, hpDamage, enemyPoint);
        }
        else
        {
            Debug.Log("闪避与攻击拼点平局，攻击被抵消");
        }
    }


    // ================================
    // 防御 vs 攻击
    // ================================

    static void HandleDefenseVsEnemyAttack(
        CharacterData allyUnit,
        BattleCardState defenseCardState,
        CharacterData enemyUnit,
        BattleCardState enemyCardState
    )
    {
        CardTestData defenseCard = defenseCardState.cardData;
        CardTestData enemyCard = enemyCardState.cardData;

        int defensePoint = RollCardPoint(defenseCard);
        int enemyPoint = RollCardPoint(enemyCard);

        Debug.Log(allyUnit.characterName + " 使用防御卡抵挡攻击");

        // 防御不是拼点，所以 clashResult 保持 None

        // 敌人攻击卡生效
        TriggerBattleEvent(BattleTiming.Resolved, enemyUnit, allyUnit, enemyCardState, enemyPoint, 0, false, false);

        // 我方防御卡生效
        TriggerBattleEvent(BattleTiming.Resolved, allyUnit, enemyUnit, defenseCardState, defensePoint, 0, false, false);

        int damageScaled = BattleCalculator.GetFinalDamageScaled(enemyUnit, allyUnit, enemyCard, enemyPoint);
        int hpDamage = BattleCalculator.ConvertScaledDamageToHPDamage(damageScaled);

        // 这里先用防御点数直接抵扣 HP 伤害
        // 后面如果 BattleCalculator 里做了正式防御公式，再迁移过去
        int finalDamage = Mathf.Max(0, hpDamage - defensePoint);

        // 打到防御也算命中，即使最终伤害为 0
        TriggerBattleEvent(BattleTiming.Hit, enemyUnit, allyUnit, enemyCardState, enemyPoint, finalDamage, true, false);

        if (finalDamage > 0)
        {
            ApplyDamageAndTriggerEvents(enemyUnit, allyUnit, enemyCardState, finalDamage, enemyPoint);
        }
        else
        {
            Debug.Log(allyUnit.characterName + " 完全挡下了攻击，没有受到伤害");
        }
    }


    // ================================
    // 伤害与后续事件
    // ================================

    static void ApplyDamageAndTriggerEvents(
        CharacterData attacker,
        CharacterData defender,
        BattleCardState attackCardState,
        int hpDamage,
        int clashPoint
    )
    {
        if (attacker == null || defender == null || attackCardState == null)
        {
            return;
        }

        if (hpDamage <= 0)
        {
            Debug.Log(defender.characterName + " 没有受到实际伤害");
            return;
        }

        defender.TakeDamage(hpDamage);

        bool isKill = defender.IsDead();

        // 造成伤害事件：只有实际扣血 > 0 才触发
        TriggerBattleEvent(
            BattleTiming.AfterDamage,
            attacker,
            defender,
            attackCardState,
            clashPoint,
            hpDamage,
            true,
            isKill
        );

        if (isKill)
        {
            // 击杀事件
            TriggerBattleEvent(
                BattleTiming.AfterKill,
                attacker,
                defender,
                attackCardState,
                clashPoint,
                hpDamage,
                true,
                true
            );
        }
    }


    // ================================
    // 事件入口
    // ================================

    // TriggerBattleEvent = 触发战斗事件
    static void TriggerBattleEvent(
        string timing,
        CharacterData user,
        CharacterData target,
        BattleCardState cardState,
        int clashPoint,
        int damage,
        bool isHit,
        bool isKill,
        string clashResult = ClashResult.None
    )
    {
        BattleEventContext context = new BattleEventContext(timing)
            .SetUserAndTarget(user, target)
            .SetCardState(cardState)
            .SetClashPoint(clashPoint)
            .SetClashResult(clashResult)
            .SetDamage(damage)
            .SetHit(isHit)
            .SetKill(isKill);

        // 先让事件系统处理
        // 例如 CD、消耗、以后成就/UI/负罪感等
        BattleEventProcessor.ProcessEvent(context);

        // 再让卡牌效果处理对应阶段
        ExecuteCardEffectsByTiming(user, target, cardState, timing, clashResult);
    }

    // ExecuteCardEffectsByTiming = 按战斗阶段执行卡牌效果
    static void ExecuteCardEffectsByTiming(
        CharacterData user,
        CharacterData target,
        BattleCardState cardState,
        string timing,
        string clashResult
    )
    {
        if (cardState == null || cardState.cardData == null)
        {
            return;
        }

        CardEffectExecutor.ExecuteCardEffects(user, target, cardState.cardData, timing, clashResult);
    }


    // ================================
    // 工具函数
    // ================================

    static int RollCardPoint(CardTestData card)
    {
        if (card == null)
        {
            return 0;
        }

        int min = card.minPoint;
        int max = card.maxPoint;

        if (max < min)
        {
            int temp = min;
            min = max;
            max = temp;
        }

        return Random.Range(min, max + 1);
    }
}