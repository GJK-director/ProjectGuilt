// 脚本中文说明：战斗结算器。负责处理卡牌使用、拼点、生效、命中、伤害和击杀事件。
using System.Collections.Generic;
using UnityEngine;

public class BattleResolveResult
{
    public bool isSuccess;
    public bool shouldCompleteItem;

    public bool playerCardUsed;
    public bool enemyCardUsed;

    public bool hasDamage;
    public int damage;
    public CharacterData damagedCharacter;

    public string resultType;
    public int playerPoint;
    public int enemyPoint;
    public int clashAttemptCount;

    public bool isTieLimitReached;
    public bool triggeredEventChain;

    public BattleActionSlot triggeredPassiveGuardSlot;

    public string message;
}

// BattleResolver = 战斗结算器
// 负责处理卡牌使用、拼点、生效、命中、伤害、击杀等流程
public static class BattleResolver
{
    const int MaxRespondedEnemyIntentClashAttempts = 10;

    // ResolveFreeAction = 正式结算自由行动
    // 第一版支持 Ability FreeAction 和 Attack FreeAction，不处理防御、闪避等自由行动。
    public static BattleResolveResult ResolveFreeAction(BattleActionSlot actionSlot)
    {
        if (actionSlot == null)
        {
            return CreateInvalidResolveResult("ResolveFreeAction 失败：行动槽位为空");
        }

        if (actionSlot.slotType != BattleActionSlotType.FreeAction)
        {
            return CreateInvalidResolveResult("ResolveFreeAction 失败：行动槽位不是 FreeAction");
        }

        if (actionSlot.actor == null)
        {
            return CreateInvalidResolveResult("ResolveFreeAction 失败：行动者为空");
        }

        if (actionSlot.cardState == null)
        {
            return CreateInvalidResolveResult("ResolveFreeAction 失败：卡牌状态为空");
        }

        if (actionSlot.cardState.cardData == null)
        {
            return CreateInvalidResolveResult("ResolveFreeAction 失败：卡牌数据为空");
        }

        bool isAbilityCard = actionSlot.cardState.cardData.cardType == "Ability" || actionSlot.cardState.IsAbilitySinCard();
        bool isAttackCard = actionSlot.cardState.cardData.cardType == CardType.Attack;

        if (isAbilityCard)
        {
            return ResolveFreeAbilityAction(actionSlot);
        }

        if (isAttackCard)
        {
            return ResolveFreeAttackAction(actionSlot);
        }

        return CreateUnsupportedResolveResult(
            "ResolveFreeAction 暂不支持该 FreeAction 卡牌类型：" +
            actionSlot.cardState.cardData.cardType
        );
    }

    static BattleResolveResult ResolveFreeAbilityAction(BattleActionSlot actionSlot)
    {
        CharacterData user = actionSlot.actor;
        CharacterData target = actionSlot.target != null ? actionSlot.target : user;

        if (!BattleCardManager.CanUseCard(user, target, actionSlot.cardState))
        {
            return CreateInvalidResolveResult(
                "ResolveFreeAction 失败：" +
                user.characterName +
                " 的卡牌不能使用：" +
                actionSlot.cardState.GetCardName()
            );
        }

        Debug.Log(
            user.characterName +
            " 使用 FreeAction Ability：" +
            actionSlot.cardState.GetCardName() +
            "，不进入拼点"
        );

        TriggerBattleEvent(BattleTiming.OnPlay, user, target, actionSlot.cardState, 0, 0, false, false);
        TriggerBattleEvent(BattleTiming.Resolved, user, target, actionSlot.cardState, 0, 0, false, false);

        BattleResolveResult result = new BattleResolveResult();
        result.isSuccess = true;
        result.shouldCompleteItem = true;
        result.playerCardUsed = true;
        result.enemyCardUsed = false;
        result.hasDamage = false;
        result.damage = 0;
        result.damagedCharacter = null;
        result.resultType = "FreeAbility";
        result.playerPoint = 0;
        result.enemyPoint = 0;
        result.clashAttemptCount = 0;
        result.isTieLimitReached = false;
        result.triggeredEventChain = true;
        result.message =
            "ResolveFreeAction 完成：Ability FreeAction 已触发 OnPlay / Resolved，不造成伤害";

        Debug.Log(result.message);

        return result;
    }

    static BattleResolveResult ResolveFreeAttackAction(BattleActionSlot actionSlot)
    {
        CharacterData user = actionSlot.actor;
        CharacterData target = actionSlot.target;
        CardTestData attackCard = actionSlot.cardState.cardData;

        if (target == null)
        {
            return CreateInvalidResolveResult("ResolveFreeAction 失败：Attack FreeAction 目标为空");
        }

        if (IsInvalidPointRange(attackCard.minPoint, attackCard.maxPoint))
        {
            return CreateInvalidResolveResult(
                "ResolveFreeAction 失败：Attack FreeAction 点数范围异常：" +
                attackCard.minPoint +
                "-" +
                attackCard.maxPoint
            );
        }

        if (!BattleCardManager.CanUseCard(user, target, actionSlot.cardState))
        {
            return CreateInvalidResolveResult(
                "ResolveFreeAction 失败：" +
                user.characterName +
                " 的卡牌不能使用：" +
                actionSlot.cardState.GetCardName()
            );
        }

        Debug.Log(
            user.characterName +
            " 使用 FreeAction Attack：" +
            actionSlot.cardState.GetCardName() +
            " 攻击 " +
            target.characterName +
            "，不进入拼点"
        );

        TriggerBattleEvent(BattleTiming.BeforeUse, user, target, actionSlot.cardState, 0, 0, false, false);

        int playerAttackPoint = BattleCalculator.Rollpoint(attackCard.minPoint, attackCard.maxPoint);
        int damageScaled = BattleCalculator.GetFinalDamageScaled(
            user,
            target,
            attackCard,
            playerAttackPoint
        );
        int finalHpDamage = BattleCalculator.ConvertScaledDamageToHPDamage(damageScaled);

        TriggerBattleEvent(BattleTiming.Resolved, user, target, actionSlot.cardState, playerAttackPoint, 0, false, false);
        TriggerBattleEvent(BattleTiming.Hit, user, target, actionSlot.cardState, playerAttackPoint, finalHpDamage, finalHpDamage > 0, false);
        ApplyDamageAndTriggerEvents(user, target, actionSlot.cardState, finalHpDamage, playerAttackPoint);

        BattleResolveResult result = new BattleResolveResult();
        result.isSuccess = true;
        result.shouldCompleteItem = true;
        result.playerCardUsed = true;
        result.enemyCardUsed = false;
        result.hasDamage = finalHpDamage > 0;
        result.damage = finalHpDamage;
        result.damagedCharacter = target;
        result.resultType = "FreeAttack";
        result.playerPoint = playerAttackPoint;
        result.enemyPoint = 0;
        result.clashAttemptCount = 0;
        result.isTieLimitReached = false;
        result.triggeredEventChain = true;
        result.message =
            "ResolveFreeAction 完成：Attack FreeAction 使用 " +
            actionSlot.cardState.GetCardName() +
            " 命中 " +
            target.characterName +
            "，玩家攻击点数 " +
            playerAttackPoint +
            "，造成伤害 " +
            finalHpDamage +
            "。不触发 ClashWin / ClashLose";

        Debug.Log(result.message);

        return result;
    }

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

    // ResolveRespondedEnemyIntent = 正式结算已响应敌人意图
    // 当前支持玩家 Attack / Defense / Dodge 指定响应敌人 Attack。
    public static BattleResolveResult ResolveRespondedEnemyIntent(
        BattleActionSlot actionSlot,
        BattleEnemyIntent enemyIntent
    )
    {
        return ResolveRespondedEnemyIntent(actionSlot, enemyIntent, null);
    }

    public static BattleResolveResult ResolveRespondedEnemyIntent(
        BattleActionSlot actionSlot,
        BattleEnemyIntent enemyIntent,
        IReadOnlyList<BattleActionSlot> passiveGuardCandidates
    )
    {
        if (actionSlot == null)
        {
            return CreateInvalidResolveResult("ResolveRespondedEnemyIntent 失败：行动槽位为空");
        }

        if (enemyIntent == null)
        {
            return CreateInvalidResolveResult("ResolveRespondedEnemyIntent 失败：敌人意图为空");
        }

        if (actionSlot.actor == null)
        {
            return CreateInvalidResolveResult("ResolveRespondedEnemyIntent 失败：玩家行动者为空");
        }

        if (actionSlot.cardState == null)
        {
            return CreateInvalidResolveResult("ResolveRespondedEnemyIntent 失败：玩家卡牌状态为空");
        }

        if (actionSlot.cardState.cardData == null)
        {
            return CreateInvalidResolveResult("ResolveRespondedEnemyIntent 失败：玩家卡牌数据为空");
        }

        if (enemyIntent.enemy == null)
        {
            return CreateInvalidResolveResult("ResolveRespondedEnemyIntent 失败：敌人为空");
        }

        if (enemyIntent.enemyCardState == null)
        {
            return CreateInvalidResolveResult("ResolveRespondedEnemyIntent 失败：敌人卡牌状态为空");
        }

        if (enemyIntent.enemyCardState.cardData == null)
        {
            return CreateInvalidResolveResult("ResolveRespondedEnemyIntent 失败：敌人卡牌数据为空");
        }

        if (enemyIntent.actualTargetCharacter == null)
        {
            return CreateInvalidResolveResult("ResolveRespondedEnemyIntent 失败：实际目标角色为空");
        }

        CardTestData playerCard = actionSlot.cardState.cardData;
        CardTestData enemyCard = enemyIntent.enemyCardState.cardData;

        if (enemyCard.cardType != CardType.Attack)
        {
            return CreateUnsupportedResolveResult(
                "ResolveRespondedEnemyIntent 暂不支持该卡牌对抗类型：玩家 " +
                playerCard.cardType +
                " / 敌人 " +
                enemyCard.cardType
            );
        }

        if (IsInvalidPointRange(enemyCard.minPoint, enemyCard.maxPoint))
        {
            return CreateInvalidResolveResult(
                "ResolveRespondedEnemyIntent 失败：敌人卡牌点数范围异常：" +
                enemyCard.minPoint +
                "-" +
                enemyCard.maxPoint
            );
        }

        if (playerCard.cardType == CardType.Attack)
        {
            if (IsInvalidPointRange(playerCard.minPoint, playerCard.maxPoint))
            {
                return CreateInvalidResolveResult(
                    "ResolveRespondedEnemyIntent 失败：玩家卡牌点数范围异常：" +
                    playerCard.minPoint +
                    "-" +
                    playerCard.maxPoint
                );
            }

            return ResolveRespondedAttackVsAttack(actionSlot, enemyIntent, passiveGuardCandidates);
        }

        if (playerCard.cardType == CardType.Dodge)
        {
            if (IsInvalidPointRange(playerCard.minPoint, playerCard.maxPoint))
            {
                return CreateInvalidResolveResult(
                    "ResolveRespondedEnemyIntent 失败：玩家闪避卡点数范围异常：" +
                    playerCard.minPoint +
                    "-" +
                    playerCard.maxPoint
                );
            }

            return ResolveRespondedDodgeVsAttack(actionSlot, enemyIntent);
        }

        if (playerCard.cardType == CardType.Defense)
        {
            if (IsInvalidPointRange(playerCard.minPoint, playerCard.maxPoint))
            {
                return CreateInvalidResolveResult(
                    "ResolveRespondedEnemyIntent 失败：玩家防御卡点数范围异常：" +
                    playerCard.minPoint +
                    "-" +
                    playerCard.maxPoint
                );
            }

            return ResolveRespondedDefenseVsAttack(actionSlot, enemyIntent);
        }

        return CreateUnsupportedResolveResult(
            "ResolveRespondedEnemyIntent 暂不支持该卡牌对抗类型：玩家 " +
            playerCard.cardType +
            " / 敌人 " +
            enemyCard.cardType
        );
    }

    // ResolveUnrespondedEnemyIntent = 正式结算无人响应敌人意图
    // 第一版只处理敌人攻击命中 actualTarget，不触发玩家卡牌式事件链。
    public static BattleResolveResult ResolveUnrespondedEnemyIntent(
        BattleEnemyIntent enemyIntent
    )
    {
        if (enemyIntent == null)
        {
            return CreateInvalidResolveResult("ResolveUnrespondedEnemyIntent 失败：敌人意图为空");
        }

        if (enemyIntent.enemy == null)
        {
            return CreateInvalidResolveResult("ResolveUnrespondedEnemyIntent 失败：敌人为空");
        }

        if (enemyIntent.enemyCardState == null)
        {
            return CreateInvalidResolveResult("ResolveUnrespondedEnemyIntent 失败：敌人卡牌状态为空");
        }

        if (enemyIntent.enemyCardState.cardData == null)
        {
            return CreateInvalidResolveResult("ResolveUnrespondedEnemyIntent 失败：敌人卡牌数据为空");
        }

        if (enemyIntent.actualTargetCharacter == null)
        {
            return CreateInvalidResolveResult("ResolveUnrespondedEnemyIntent 失败：实际目标角色为空");
        }

        if (enemyIntent.actualTargetSlotIndex <= 0)
        {
            return CreateInvalidResolveResult(
                "ResolveUnrespondedEnemyIntent 失败：实际目标槽位异常：" +
                enemyIntent.actualTargetSlotIndex
            );
        }

        CardTestData enemyCard = enemyIntent.enemyCardState.cardData;

        if (enemyCard.cardType != CardType.Attack)
        {
            return CreateUnsupportedResolveResult(
                "ResolveUnrespondedEnemyIntent 暂不支持非攻击敌人卡牌：" +
                enemyCard.cardType
            );
        }

        if (IsInvalidPointRange(enemyCard.minPoint, enemyCard.maxPoint))
        {
            return CreateInvalidResolveResult(
                "ResolveUnrespondedEnemyIntent 失败：敌人卡牌点数范围异常：" +
                enemyCard.minPoint +
                "-" +
                enemyCard.maxPoint
            );
        }

        CharacterData enemyUnit = enemyIntent.enemy;
        CharacterData target = enemyIntent.actualTargetCharacter;

        int enemyAttackPoint = BattleCalculator.Rollpoint(enemyCard.minPoint, enemyCard.maxPoint);
        int damageScaled = BattleCalculator.GetFinalDamageScaled(
            enemyUnit,
            target,
            enemyCard,
            enemyAttackPoint
        );
        int finalHpDamage = BattleCalculator.ConvertScaledDamageToHPDamage(damageScaled);

        target.TakeDamage(finalHpDamage);

        BattleResolveResult result = new BattleResolveResult();
        result.isSuccess = true;
        result.shouldCompleteItem = true;
        result.playerCardUsed = false;
        result.enemyCardUsed = true;
        result.hasDamage = finalHpDamage > 0;
        result.damage = finalHpDamage;
        result.damagedCharacter = target;
        result.resultType = "UnrespondedEnemyAttack";
        result.playerPoint = 0;
        result.enemyPoint = enemyAttackPoint;
        result.clashAttemptCount = 0;
        result.isTieLimitReached = false;
        result.triggeredEventChain = false;
        result.message =
            "ResolveUnrespondedEnemyIntent 完成：敌人意图" +
            enemyIntent.intentOrder +
            " 使用 " +
            enemyCard.cardName +
            " 命中 " +
            target.characterName +
            " 槽位" +
            enemyIntent.actualTargetSlotIndex +
            "，敌人攻击点数 " +
            enemyAttackPoint +
            "，造成伤害 " +
            finalHpDamage +
            "。第一版不触发事件链，不处理敌人卡牌 CD / UseCount";

        Debug.Log(result.message);

        return result;
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

    static BattleResolveResult ResolveRespondedAttackVsAttack(
        BattleActionSlot actionSlot,
        BattleEnemyIntent enemyIntent,
        IReadOnlyList<BattleActionSlot> passiveGuardCandidates
    )
    {
        CharacterData playerUnit = actionSlot.actor;
        BattleCardState playerCardState = actionSlot.cardState;
        CharacterData enemyUnit = enemyIntent.enemy;
        BattleCardState enemyCardState = enemyIntent.enemyCardState;
        CharacterData actualTarget = enemyIntent.actualTargetCharacter;

        enemyUnit.CheckBuffsByTiming(BattleTiming.ClashStart);
        playerUnit.CheckBuffsByTiming(BattleTiming.ClashStart);

        int playerPoint = 0;
        int enemyPoint = 0;

        for (int attempt = 1; attempt <= MaxRespondedEnemyIntentClashAttempts; attempt++)
        {
            playerPoint = BattleCalculator.GetFinalClashPoint(playerUnit, playerCardState.cardData);
            enemyPoint = BattleCalculator.GetFinalClashPoint(enemyUnit, enemyCardState.cardData);

            if (playerPoint == enemyPoint)
            {
                Debug.Log(
                    "ResolveRespondedEnemyIntent 第" +
                    attempt +
                    "次拼点平局：玩家点数 " +
                    playerPoint +
                    "，敌人点数 " +
                    enemyPoint
                );

                continue;
            }

            bool isPlayerWin = playerPoint > enemyPoint;
            CharacterData attacker = isPlayerWin ? playerUnit : enemyUnit;
            CharacterData defender = isPlayerWin ? enemyUnit : actualTarget;
            BattleCardState winnerCardState = isPlayerWin ? playerCardState : enemyCardState;
            BattleCardState loserCardState = isPlayerWin ? enemyCardState : playerCardState;
            CharacterData loser = isPlayerWin ? enemyUnit : playerUnit;
            int winnerPoint = isPlayerWin ? playerPoint : enemyPoint;
            int loserPoint = isPlayerWin ? enemyPoint : playerPoint;
            string resultType = isPlayerWin ? "PlayerWin" : "EnemyWin";

            TriggerBattleEvent(
                BattleTiming.ClashWin,
                attacker,
                defender,
                winnerCardState,
                winnerPoint,
                0,
                false,
                false,
                ClashResult.Win
            );

            TriggerBattleEvent(
                BattleTiming.ClashLose,
                loser,
                attacker,
                loserCardState,
                loserPoint,
                0,
                false,
                false,
                ClashResult.Lose
            );

            TriggerBattleEvent(
                BattleTiming.Resolved,
                attacker,
                defender,
                winnerCardState,
                winnerPoint,
                0,
                false,
                false,
                ClashResult.Win
            );

            TriggerBattleEvent(
                BattleTiming.Resolved,
                loser,
                attacker,
                loserCardState,
                loserPoint,
                0,
                false,
                false,
                ClashResult.Lose
            );

            if (!isPlayerWin)
            {
                BattleResolveResult passiveGuardResult = TryResolveEnemyWinPassiveGuard(
                    passiveGuardCandidates,
                    enemyIntent,
                    playerPoint,
                    enemyPoint,
                    attempt
                );

                if (passiveGuardResult != null)
                {
                    return passiveGuardResult;
                }
            }

            int damageScaled = BattleCalculator.GetFinalDamageScaled(
                attacker,
                defender,
                winnerCardState.cardData,
                winnerPoint
            );
            int hpDamage = BattleCalculator.ConvertScaledDamageToHPDamage(damageScaled);

            TriggerBattleEvent(
                BattleTiming.Hit,
                attacker,
                defender,
                winnerCardState,
                winnerPoint,
                hpDamage,
                hpDamage > 0,
                false,
                ClashResult.Win
            );

            ApplyDamageAndTriggerEvents(attacker, defender, winnerCardState, hpDamage, winnerPoint);

            BattleResolveResult result = new BattleResolveResult();
            result.isSuccess = true;
            result.shouldCompleteItem = true;
            result.playerCardUsed = true;
            result.enemyCardUsed = true;
            result.hasDamage = hpDamage > 0;
            result.damage = hpDamage;
            result.damagedCharacter = hpDamage > 0 ? defender : null;
            result.resultType = resultType;
            result.playerPoint = playerPoint;
            result.enemyPoint = enemyPoint;
            result.clashAttemptCount = attempt;
            result.isTieLimitReached = false;
            result.triggeredEventChain = true;
            result.message =
                "ResolveRespondedEnemyIntent 完成：" +
                resultType +
                "，玩家点数 " +
                playerPoint +
                "，敌人点数 " +
                enemyPoint +
                "，造成伤害 " +
                hpDamage;

            Debug.Log(result.message);

            return result;
        }

        BattleResolveResult tieLimitResult = new BattleResolveResult();
        tieLimitResult.isSuccess = true;
        tieLimitResult.shouldCompleteItem = true;
        tieLimitResult.playerCardUsed = false;
        tieLimitResult.enemyCardUsed = false;
        tieLimitResult.hasDamage = false;
        tieLimitResult.damage = 0;
        tieLimitResult.damagedCharacter = null;
        tieLimitResult.resultType = "TieLimit";
        tieLimitResult.playerPoint = playerPoint;
        tieLimitResult.enemyPoint = enemyPoint;
        tieLimitResult.clashAttemptCount = MaxRespondedEnemyIntentClashAttempts;
        tieLimitResult.isTieLimitReached = true;
        tieLimitResult.triggeredEventChain = false;
        tieLimitResult.message =
            "ResolveRespondedEnemyIntent 连续拼点 " +
            MaxRespondedEnemyIntentClashAttempts +
            " 次仍未分出胜负，自动结束，双方不造成伤害，双方卡牌不算成功使用";

        Debug.Log(tieLimitResult.message);

        return tieLimitResult;
    }

    static BattleResolveResult TryResolveEnemyWinPassiveGuard(
        IReadOnlyList<BattleActionSlot> passiveGuardCandidates,
        BattleEnemyIntent enemyIntent,
        int playerPoint,
        int enemyPoint,
        int clashAttemptCount
    )
    {
        BattleActionSlot selectedPassiveGuardSlot = FindFirstValidPassiveGuardSlot(
            passiveGuardCandidates,
            enemyIntent
        );

        if (selectedPassiveGuardSlot == null)
        {
            return null;
        }

        Debug.Log(
            "EnemyWin 触发 PassiveGuard 候选：" +
            selectedPassiveGuardSlot.GetDisplaySlotName() +
            " / " +
            selectedPassiveGuardSlot.GetCardName() +
            "，将复用敌人拼赢点数 " +
            enemyPoint +
            "，未重新 Roll"
        );

        BattleResolveResult defenseResult = ResolveDefenseVsAttackWithKnownEnemyPoint(
            selectedPassiveGuardSlot,
            enemyIntent,
            enemyPoint
        );

        if (defenseResult == null)
        {
            Debug.LogWarning("EnemyWin PassiveGuard 结算失败：Defense Resolver 返回空结果，回退原 EnemyWin 伤害流程");
            return null;
        }

        if (!defenseResult.playerCardUsed)
        {
            Debug.LogWarning("EnemyWin PassiveGuard 未成功使用 Defense，回退原 EnemyWin 伤害流程：" + defenseResult.message);
            return null;
        }

        if (!defenseResult.isSuccess || !defenseResult.shouldCompleteItem)
        {
            Debug.LogWarning("EnemyWin PassiveGuard 已进入 Defense 使用流程但结果不可完成，不回退原始伤害：" + defenseResult.message);
            defenseResult.triggeredPassiveGuardSlot = selectedPassiveGuardSlot;
            return defenseResult;
        }

        BattleResolveResult result = new BattleResolveResult();
        result.isSuccess = true;
        result.shouldCompleteItem = true;
        result.playerCardUsed = true;
        result.enemyCardUsed = true;
        result.hasDamage = defenseResult.hasDamage;
        result.damage = defenseResult.damage;
        result.damagedCharacter = defenseResult.damagedCharacter;
        result.resultType = defenseResult.resultType == "DefenseFullBlock"
            ? "EnemyWinPassiveGuardFullBlock"
            : "EnemyWinPassiveGuardReducedDamage";
        result.playerPoint = playerPoint;
        result.enemyPoint = enemyPoint;
        result.clashAttemptCount = clashAttemptCount;
        result.isTieLimitReached = false;
        result.triggeredEventChain = true;
        result.triggeredPassiveGuardSlot = selectedPassiveGuardSlot;
        result.message =
            "ResolveRespondedEnemyIntent 完成：" +
            result.resultType +
            "，玩家最终拼点 " +
            playerPoint +
            "，敌人最终胜利点数 " +
            enemyPoint +
            "，实际触发 PassiveGuard 槽位 " +
            selectedPassiveGuardSlot.GetDisplaySlotName() +
            "，防御结果 " +
            defenseResult.resultType +
            "，最终伤害 " +
            defenseResult.damage +
            "。复用敌人拼赢点数，未重新 Roll";

        Debug.Log(result.message);

        return result;
    }

    static BattleActionSlot FindFirstValidPassiveGuardSlot(
        IReadOnlyList<BattleActionSlot> passiveGuardCandidates,
        BattleEnemyIntent enemyIntent
    )
    {
        if (passiveGuardCandidates == null || passiveGuardCandidates.Count == 0)
        {
            return null;
        }

        foreach (BattleActionSlot slot in passiveGuardCandidates)
        {
            if (!IsPassiveGuardSlotStillValid(slot, enemyIntent))
            {
                continue;
            }

            return slot;
        }

        return null;
    }

    static bool IsPassiveGuardSlotStillValid(
        BattleActionSlot slot,
        BattleEnemyIntent enemyIntent
    )
    {
        if (slot == null || enemyIntent == null || enemyIntent.actualTargetCharacter == null)
        {
            return false;
        }

        if (slot.IsEmpty())
        {
            return false;
        }

        if (slot.slotType != BattleActionSlotType.PassiveGuard)
        {
            return false;
        }

        if (slot.isUsed)
        {
            return false;
        }

        if (!object.ReferenceEquals(slot.owner, enemyIntent.actualTargetCharacter) ||
            !object.ReferenceEquals(slot.actor, enemyIntent.actualTargetCharacter) ||
            !object.ReferenceEquals(slot.target, enemyIntent.actualTargetCharacter))
        {
            return false;
        }

        if (slot.cardState == null || slot.cardState.cardData == null)
        {
            return false;
        }

        if (slot.cardState.cardData.cardType != CardType.Defense)
        {
            return false;
        }

        return BattleCardManager.CanUseCard(slot.cardState);
    }


    // ================================
    // 防御响应敌人攻击
    // ================================

    static BattleResolveResult ResolveRespondedDefenseVsAttack(
        BattleActionSlot actionSlot,
        BattleEnemyIntent enemyIntent
    )
    {
        CharacterData playerUnit = actionSlot.actor;
        BattleCardState defenseCardState = actionSlot.cardState;
        CharacterData enemyUnit = enemyIntent.enemy;
        BattleCardState enemyCardState = enemyIntent.enemyCardState;
        CharacterData actualTarget = enemyIntent.actualTargetCharacter;

        if (defenseCardState == null || defenseCardState.cardData == null)
        {
            return CreateInvalidResolveResult("ResolveRespondedDefenseVsAttack 失败：玩家防御卡为空");
        }

        if (enemyCardState == null || enemyCardState.cardData == null)
        {
            return CreateInvalidResolveResult("ResolveRespondedDefenseVsAttack 失败：敌人攻击卡为空");
        }

        if (defenseCardState.cardData.cardType != CardType.Defense)
        {
            return CreateInvalidResolveResult(
                "ResolveRespondedDefenseVsAttack 失败：玩家卡牌不是 Defense：" +
                defenseCardState.cardData.cardType
            );
        }

        if (enemyCardState.cardData.cardType != CardType.Attack)
        {
            return CreateInvalidResolveResult(
                "ResolveRespondedDefenseVsAttack 失败：敌人卡牌不是 Attack：" +
                enemyCardState.cardData.cardType
            );
        }

        enemyUnit.CheckBuffsByTiming(BattleTiming.ClashStart);
        playerUnit.CheckBuffsByTiming(BattleTiming.ClashStart);

        int enemyFinalAttackPoint = BattleCalculator.GetFinalClashPoint(enemyUnit, enemyCardState.cardData);

        return ResolveDefenseVsAttackCore(
            actionSlot,
            enemyIntent,
            enemyFinalAttackPoint,
            true,
            true,
            "ResolveRespondedDefenseVsAttack",
            false
        );
    }

    // ResolveDefenseVsAttackWithKnownEnemyPoint = 使用外层已经确定的敌人最终攻击点数继续防御结算。
    // 不重新 Roll 敌人点数，不触发敌人 ClashStart / ClashWin / ClashLose / Resolved。
    internal static BattleResolveResult ResolveDefenseVsAttackWithKnownEnemyPoint(
        BattleActionSlot defenseSlot,
        BattleEnemyIntent enemyIntent,
        int knownEnemyAttackPoint
    )
    {
        if (defenseSlot == null)
        {
            return CreateInvalidResolveResult("ResolveDefenseVsAttackWithKnownEnemyPoint 失败：防御槽位为空");
        }

        if (defenseSlot.actor == null)
        {
            return CreateInvalidResolveResult("ResolveDefenseVsAttackWithKnownEnemyPoint 失败：防御者为空");
        }

        if (defenseSlot.cardState == null || defenseSlot.cardState.cardData == null)
        {
            return CreateInvalidResolveResult("ResolveDefenseVsAttackWithKnownEnemyPoint 失败：防御卡为空");
        }

        if (enemyIntent == null)
        {
            return CreateInvalidResolveResult("ResolveDefenseVsAttackWithKnownEnemyPoint 失败：敌人意图为空");
        }

        if (enemyIntent.enemy == null)
        {
            return CreateInvalidResolveResult("ResolveDefenseVsAttackWithKnownEnemyPoint 失败：敌人为空");
        }

        if (enemyIntent.enemyCardState == null || enemyIntent.enemyCardState.cardData == null)
        {
            return CreateInvalidResolveResult("ResolveDefenseVsAttackWithKnownEnemyPoint 失败：敌人攻击卡为空");
        }

        if (enemyIntent.actualTargetCharacter == null)
        {
            return CreateInvalidResolveResult("ResolveDefenseVsAttackWithKnownEnemyPoint 失败：实际目标为空");
        }

        if (defenseSlot.cardState.cardData.cardType != CardType.Defense)
        {
            return CreateInvalidResolveResult(
                "ResolveDefenseVsAttackWithKnownEnemyPoint 失败：玩家卡牌不是 Defense：" +
                defenseSlot.cardState.cardData.cardType
            );
        }

        if (IsInvalidPointRange(defenseSlot.cardState.cardData.minPoint, defenseSlot.cardState.cardData.maxPoint))
        {
            return CreateInvalidResolveResult(
                "ResolveDefenseVsAttackWithKnownEnemyPoint 失败：玩家防御卡点数范围异常：" +
                defenseSlot.cardState.cardData.minPoint +
                "-" +
                defenseSlot.cardState.cardData.maxPoint
            );
        }

        if (enemyIntent.enemyCardState.cardData.cardType != CardType.Attack)
        {
            return CreateInvalidResolveResult(
                "ResolveDefenseVsAttackWithKnownEnemyPoint 失败：敌人卡牌不是 Attack：" +
                enemyIntent.enemyCardState.cardData.cardType
            );
        }

        int clampedKnownEnemyAttackPoint = Mathf.Max(0, knownEnemyAttackPoint);

        return ResolveDefenseVsAttackCore(
            defenseSlot,
            enemyIntent,
            clampedKnownEnemyAttackPoint,
            false,
            false,
            "ResolveDefenseVsAttackWithKnownEnemyPoint",
            true
        );
    }

    static BattleResolveResult ResolveDefenseVsAttackCore(
        BattleActionSlot defenseSlot,
        BattleEnemyIntent enemyIntent,
        int enemyFinalAttackPoint,
        bool shouldTriggerEnemyResolved,
        bool enemyCardUsed,
        string messagePrefix,
        bool isKnownPointContinuation
    )
    {
        CharacterData playerUnit = defenseSlot.actor;
        BattleCardState defenseCardState = defenseSlot.cardState;
        CharacterData enemyUnit = enemyIntent.enemy;
        BattleCardState enemyCardState = enemyIntent.enemyCardState;
        CharacterData actualTarget = enemyIntent.actualTargetCharacter;

        enemyFinalAttackPoint = Mathf.Max(0, enemyFinalAttackPoint);

        int playerFinalDefensePointScaled = BattleCalculator.GetFinalDefensePointScaled(playerUnit, defenseCardState.cardData);
        int playerFinalDefensePoint = BattleCalculator.ConvertScaledDamageToHPDamage(playerFinalDefensePointScaled);
        int remainingAttackPoint = BattleCalculator.CalculateRemainingAttackPointAfterDefense(
            enemyFinalAttackPoint,
            playerFinalDefensePointScaled
        );

        TriggerBattleEvent(BattleTiming.BeforeUse, playerUnit, enemyUnit, defenseCardState, 0, 0, false, false);

        if (shouldTriggerEnemyResolved)
        {
            TriggerBattleEvent(BattleTiming.Resolved, enemyUnit, actualTarget, enemyCardState, enemyFinalAttackPoint, 0, false, false);
        }

        TriggerBattleEvent(BattleTiming.Resolved, playerUnit, enemyUnit, defenseCardState, playerFinalDefensePoint, 0, false, false);

        int finalHpDamage = 0;
        bool isFullBlock = remainingAttackPoint == 0;

        if (!isFullBlock)
        {
            int damageScaled = BattleCalculator.GetFinalDamageScaled(
                enemyUnit,
                actualTarget,
                enemyCardState.cardData,
                remainingAttackPoint
            );

            finalHpDamage = BattleCalculator.ConvertScaledDamageToHPDamage(damageScaled);

            if (finalHpDamage > 0)
            {
                TriggerBattleEvent(BattleTiming.Hit, enemyUnit, actualTarget, enemyCardState, remainingAttackPoint, finalHpDamage, true, false);
                ApplyDamageAndTriggerEvents(enemyUnit, actualTarget, enemyCardState, finalHpDamage, remainingAttackPoint);
            }
        }

        BattleResolveResult result = new BattleResolveResult();
        result.isSuccess = true;
        result.shouldCompleteItem = true;
        result.playerCardUsed = true;
        result.enemyCardUsed = enemyCardUsed;
        result.hasDamage = finalHpDamage > 0;
        result.damage = finalHpDamage;
        result.damagedCharacter = finalHpDamage > 0 ? actualTarget : null;
        result.resultType = isFullBlock ? "DefenseFullBlock" : "DefenseReducedDamage";
        result.playerPoint = playerFinalDefensePoint;
        result.enemyPoint = enemyFinalAttackPoint;
        result.clashAttemptCount = 0;
        result.isTieLimitReached = false;
        result.triggeredEventChain = true;
        string enemyPointLabel = isKnownPointContinuation ? "knownEnemyAttackPoint " : "敌人最终攻击点数 ";

        result.message =
            messagePrefix +
            " 完成：" +
            result.resultType +
            "，" +
            enemyPointLabel +
            enemyFinalAttackPoint +
            "，玩家最终防御点数 " +
            playerFinalDefensePoint +
            "，剩余攻击点数 " +
            remainingAttackPoint +
            "，最终 HP 伤害 " +
            finalHpDamage;

        if (isKnownPointContinuation)
        {
            result.message += "。使用已确定敌人点数，未重新 Roll";
        }

        Debug.Log(result.message);

        return result;
    }


    // ================================
    // 闪避 vs 攻击
    // ================================

    static BattleResolveResult ResolveRespondedDodgeVsAttack(
        BattleActionSlot playerSlot,
        BattleEnemyIntent enemyIntent
    )
    {
        if (playerSlot == null)
        {
            return CreateInvalidResolveResult("ResolveRespondedDodgeVsAttack 失败：玩家响应槽位为空");
        }

        if (enemyIntent == null)
        {
            return CreateInvalidResolveResult("ResolveRespondedDodgeVsAttack 失败：敌人意图为空");
        }

        CharacterData playerUnit = playerSlot.actor;
        BattleCardState dodgeCardState = playerSlot.cardState;
        CharacterData enemyUnit = enemyIntent.enemy;
        BattleCardState enemyCardState = enemyIntent.enemyCardState;
        CharacterData actualTarget = enemyIntent.actualTargetCharacter;

        if (playerUnit == null)
        {
            return CreateInvalidResolveResult("ResolveRespondedDodgeVsAttack 失败：玩家单位为空");
        }

        if (enemyUnit == null)
        {
            return CreateInvalidResolveResult("ResolveRespondedDodgeVsAttack 失败：敌人单位为空");
        }

        if (dodgeCardState == null || dodgeCardState.cardData == null)
        {
            return CreateInvalidResolveResult("ResolveRespondedDodgeVsAttack 失败：玩家闪避卡为空");
        }

        if (enemyCardState == null || enemyCardState.cardData == null)
        {
            return CreateInvalidResolveResult("ResolveRespondedDodgeVsAttack 失败：敌人攻击卡为空");
        }

        if (actualTarget == null)
        {
            return CreateInvalidResolveResult("ResolveRespondedDodgeVsAttack 失败：实际目标为空");
        }

        if (dodgeCardState.cardData.cardType != CardType.Dodge)
        {
            return CreateInvalidResolveResult(
                "ResolveRespondedDodgeVsAttack 失败：玩家卡牌不是 Dodge：" +
                dodgeCardState.cardData.cardType
            );
        }

        if (enemyCardState.cardData.cardType != CardType.Attack)
        {
            return CreateInvalidResolveResult(
                "ResolveRespondedDodgeVsAttack 失败：敌人卡牌不是 Attack：" +
                enemyCardState.cardData.cardType
            );
        }

        if (IsInvalidPointRange(dodgeCardState.cardData.minPoint, dodgeCardState.cardData.maxPoint))
        {
            return CreateInvalidResolveResult(
                "ResolveRespondedDodgeVsAttack 失败：玩家闪避卡点数范围异常：" +
                dodgeCardState.cardData.minPoint +
                "-" +
                dodgeCardState.cardData.maxPoint
            );
        }

        if (IsInvalidPointRange(enemyCardState.cardData.minPoint, enemyCardState.cardData.maxPoint))
        {
            return CreateInvalidResolveResult(
                "ResolveRespondedDodgeVsAttack 失败：敌人攻击卡点数范围异常：" +
                enemyCardState.cardData.minPoint +
                "-" +
                enemyCardState.cardData.maxPoint
            );
        }

        enemyUnit.CheckBuffsByTiming(BattleTiming.ClashStart);
        playerUnit.CheckBuffsByTiming(BattleTiming.ClashStart);

        int playerDodgePoint = 0;
        int enemyAttackPoint = 0;

        for (int attempt = 1; attempt <= MaxRespondedEnemyIntentClashAttempts; attempt++)
        {
            playerDodgePoint = BattleCalculator.GetFinalClashPoint(playerUnit, dodgeCardState.cardData);
            enemyAttackPoint = BattleCalculator.GetFinalClashPoint(enemyUnit, enemyCardState.cardData);

            if (playerDodgePoint == enemyAttackPoint)
            {
                Debug.Log(
                    "ResolveRespondedDodgeVsAttack 第" +
                    attempt +
                    "次平局：玩家 Dodge 点数 " +
                    playerDodgePoint +
                    "，敌人 Attack 点数 " +
                    enemyAttackPoint
                );

                continue;
            }

            if (playerDodgePoint > enemyAttackPoint)
            {
                TriggerBattleEvent(BattleTiming.ClashWin, playerUnit, enemyUnit, dodgeCardState, playerDodgePoint, 0, false, false, ClashResult.Win);
                TriggerBattleEvent(BattleTiming.ClashLose, enemyUnit, playerUnit, enemyCardState, enemyAttackPoint, 0, false, false, ClashResult.Lose);
                TriggerBattleEvent(BattleTiming.Resolved, playerUnit, enemyUnit, dodgeCardState, playerDodgePoint, 0, false, false, ClashResult.Win);
                TriggerBattleEvent(BattleTiming.Resolved, enemyUnit, playerUnit, enemyCardState, enemyAttackPoint, 0, false, false, ClashResult.Lose);

                BattleResolveResult result = new BattleResolveResult();
                result.isSuccess = true;
                result.shouldCompleteItem = true;
                result.playerCardUsed = true;
                result.enemyCardUsed = true;
                result.hasDamage = false;
                result.damage = 0;
                result.damagedCharacter = null;
                result.resultType = "DodgeSuccess";
                result.playerPoint = playerDodgePoint;
                result.enemyPoint = enemyAttackPoint;
                result.clashAttemptCount = attempt;
                result.isTieLimitReached = false;
                result.triggeredEventChain = true;
                result.message =
                    "ResolveRespondedDodgeVsAttack 完成：DodgeSuccess，玩家 Dodge 点数 " +
                    playerDodgePoint +
                    "，敌人 Attack 点数 " +
                    enemyAttackPoint +
                    "。闪避成功，不触发 Hit / AfterDamage / AfterKill";

                Debug.Log(result.message);

                return result;
            }

            TriggerBattleEvent(BattleTiming.ClashWin, enemyUnit, actualTarget, enemyCardState, enemyAttackPoint, 0, false, false, ClashResult.Win);
            TriggerBattleEvent(BattleTiming.ClashLose, playerUnit, enemyUnit, dodgeCardState, playerDodgePoint, 0, false, false, ClashResult.Lose);
            TriggerBattleEvent(BattleTiming.Resolved, enemyUnit, actualTarget, enemyCardState, enemyAttackPoint, 0, false, false, ClashResult.Win);
            TriggerBattleEvent(BattleTiming.Resolved, playerUnit, enemyUnit, dodgeCardState, playerDodgePoint, 0, false, false, ClashResult.Lose);

            int damageScaled = BattleCalculator.GetFinalDamageScaled(
                enemyUnit,
                actualTarget,
                enemyCardState.cardData,
                enemyAttackPoint
            );
            int hpDamage = BattleCalculator.ConvertScaledDamageToHPDamage(damageScaled);

            TriggerBattleEvent(BattleTiming.Hit, enemyUnit, actualTarget, enemyCardState, enemyAttackPoint, hpDamage, hpDamage > 0, false, ClashResult.Win);
            ApplyDamageAndTriggerEvents(enemyUnit, actualTarget, enemyCardState, hpDamage, enemyAttackPoint);

            BattleResolveResult failedResult = new BattleResolveResult();
            failedResult.isSuccess = true;
            failedResult.shouldCompleteItem = true;
            failedResult.playerCardUsed = true;
            failedResult.enemyCardUsed = true;
            failedResult.hasDamage = hpDamage > 0;
            failedResult.damage = hpDamage;
            failedResult.damagedCharacter = hpDamage > 0 ? actualTarget : null;
            failedResult.resultType = "DodgeFailed";
            failedResult.playerPoint = playerDodgePoint;
            failedResult.enemyPoint = enemyAttackPoint;
            failedResult.clashAttemptCount = attempt;
            failedResult.isTieLimitReached = false;
            failedResult.triggeredEventChain = true;
            failedResult.message =
                "ResolveRespondedDodgeVsAttack 完成：DodgeFailed，玩家 Dodge 点数 " +
                playerDodgePoint +
                "，敌人 Attack 点数 " +
                enemyAttackPoint +
                "，最终 HP 伤害 " +
                hpDamage +
                "。复用敌人最终胜利点数，未重新 Roll";

            Debug.Log(failedResult.message);

            return failedResult;
        }

        BattleResolveResult tieLimitResult = new BattleResolveResult();
        tieLimitResult.isSuccess = true;
        tieLimitResult.shouldCompleteItem = true;
        tieLimitResult.playerCardUsed = false;
        tieLimitResult.enemyCardUsed = false;
        tieLimitResult.hasDamage = false;
        tieLimitResult.damage = 0;
        tieLimitResult.damagedCharacter = null;
        tieLimitResult.resultType = "TieLimit";
        tieLimitResult.playerPoint = playerDodgePoint;
        tieLimitResult.enemyPoint = enemyAttackPoint;
        tieLimitResult.clashAttemptCount = MaxRespondedEnemyIntentClashAttempts;
        tieLimitResult.isTieLimitReached = true;
        tieLimitResult.triggeredEventChain = false;
        tieLimitResult.message =
            "ResolveRespondedDodgeVsAttack 连续 " +
            MaxRespondedEnemyIntentClashAttempts +
            " 次平局仍未分出胜负，自动结束，双方不造成伤害，双方卡牌不算成功使用";

        Debug.Log(tieLimitResult.message);

        return tieLimitResult;
    }

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

    static bool IsInvalidPointRange(int minPoint, int maxPoint)
    {
        return minPoint < 0 || maxPoint < 0 || maxPoint < minPoint;
    }

    static BattleResolveResult CreateInvalidResolveResult(string message)
    {
        BattleResolveResult result = new BattleResolveResult();
        result.isSuccess = false;
        result.shouldCompleteItem = false;
        result.playerCardUsed = false;
        result.enemyCardUsed = false;
        result.hasDamage = false;
        result.damage = 0;
        result.damagedCharacter = null;
        result.resultType = "Invalid";
        result.playerPoint = 0;
        result.enemyPoint = 0;
        result.clashAttemptCount = 0;
        result.isTieLimitReached = false;
        result.triggeredEventChain = false;
        result.message = message;

        Debug.LogWarning(message);

        return result;
    }

    static BattleResolveResult CreateUnsupportedResolveResult(string message)
    {
        BattleResolveResult result = CreateInvalidResolveResult(message);
        result.resultType = "Unsupported";

        return result;
    }
}
