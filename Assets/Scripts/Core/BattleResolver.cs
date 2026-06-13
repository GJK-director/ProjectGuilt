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
    const string BuffNextClashPointUp = "NextClashPointUp";
    const string BuffNextCardPointUp = "NextCardPointUp";
    const string BuffGuardUp = "GuardUp";
    const string BuffGuardDown = "GuardDown";
    const string ConsumeRuleFormalClashResolved = "FormalClashResolved";
    const string ConsumeRuleSuccessfulPointCardUsed = "SuccessfulPointCardUsed";
    const string ResourceTypeBuffStack = "BuffStack";

    struct PointBuffSnapshot
    {
        public int nextCardPointStack;
        public int nextCardPointModifier;
        public int nextClashPointStack;
        public int nextClashPointModifier;
    }

    struct CardResourceSnapshot
    {
        public BattleCardState cardState;
        public string resourceID;
        public int capturedStack;
        public bool hasRule;
        public bool normalVersionEnabled;
        public int selectedMinPoint;
        public int selectedMaxPoint;
        public int pointModifierFromResource;
        public int plannedConsumeAmount;
        public bool shouldConsumeOnSuccess;
    }

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
            return CreateActionUnavailableResult(
                "ResolveFreeAction：行动执行时卡牌已不可用，本次行动跳过。" +
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
            return CreateActionUnavailableResult(
                "ResolveFreeAction：行动执行时卡牌已不可用，本次行动跳过。" +
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

        PointBuffSnapshot userPointBuffSnapshot = CapturePointBuffSnapshot(user);

        // PointBuffSnapshot 在 ActionStart 前捕获。
        // 因此 ActionStart 新增的 NextCardPointUp / NextClashPointUp
        // 不影响当前卡，只保留给后续卡牌或后续正式拼点。
        // ActionStart 中对资源的修改会影响随后捕获的 ResourceSnapshot。
        TriggerActionStart(user, target, actionSlot.cardState);
        // 资源快照在 ActionStart 结算后、BeforeUse 之前捕获。
        // ActionStart 中产生或减少的资源可以影响当前卡。
        // BeforeUse 中产生或减少的资源不会回头改变当前卡资源快照，
        // 只影响后续行动。卡牌设计应避免依赖 BeforeUse 修改自身资源计算。
        CardResourceSnapshot userResourceSnapshot = CaptureResourceSnapshot(user, actionSlot.cardState);

        TriggerBattleEvent(BattleTiming.BeforeUse, user, target, actionSlot.cardState, 0, 0, false, false);

        int playerAttackPoint = BattleCalculator.GetFinalAttackPointWithoutClash(
            user,
            attackCard,
            userPointBuffSnapshot.nextCardPointModifier,
            userResourceSnapshot.selectedMinPoint,
            userResourceSnapshot.selectedMaxPoint,
            userResourceSnapshot.pointModifierFromResource
        );
        int damageScaled = BattleCalculator.GetFinalDamageScaled(
            user,
            target,
            attackCard,
            playerAttackPoint
        );
        int finalHpDamage = BattleCalculator.ConvertScaledDamageToHPDamage(damageScaled);

        ConsumeSuccessfulPointCardBuffs(user, userPointBuffSnapshot);
        PayDefaultResourceCostOnSuccessfulUse(user, userResourceSnapshot);

        TriggerBattleEvent(BattleTiming.Resolved, user, target, actionSlot.cardState, playerAttackPoint, 0, false, false);
        TriggerBattleEvent(BattleTiming.Hit, user, target, actionSlot.cardState, playerAttackPoint, finalHpDamage, true, false);
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

            if (!BattleCardManager.CanUseCard(actionSlot.actor, enemyIntent.enemy, actionSlot.cardState))
            {
                return CreateActionUnavailableResult(
                    "ResolveRespondedEnemyIntent：响应卡执行时已不可用，本次响应变为空卡。" +
                    actionSlot.actor.characterName +
                    " 的卡牌不能使用：" +
                    actionSlot.cardState.GetCardName()
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

            if (!BattleCardManager.CanUseCard(actionSlot.actor, enemyIntent.enemy, actionSlot.cardState))
            {
                return CreateActionUnavailableResult(
                    "ResolveRespondedEnemyIntent：响应卡执行时已不可用，本次响应变为空卡。" +
                    actionSlot.actor.characterName +
                    " 的卡牌不能使用：" +
                    actionSlot.cardState.GetCardName()
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

            if (!BattleCardManager.CanUseCard(actionSlot.actor, enemyIntent.enemy, actionSlot.cardState))
            {
                return CreateActionUnavailableResult(
                    "ResolveRespondedEnemyIntent：响应卡执行时已不可用，本次响应变为空卡。" +
                    actionSlot.actor.characterName +
                    " 的卡牌不能使用：" +
                    actionSlot.cardState.GetCardName()
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
        PointBuffSnapshot enemyPointBuffSnapshot = CapturePointBuffSnapshot(enemyUnit);

        TriggerActionStart(enemyUnit, target, enemyIntent.enemyCardState);
        CardResourceSnapshot enemyResourceSnapshot = CaptureResourceSnapshot(enemyUnit, enemyIntent.enemyCardState);

        TriggerBattleEvent(BattleTiming.BeforeUse, enemyUnit, target, enemyIntent.enemyCardState, 0, 0, false, false);

        int enemyAttackPoint = BattleCalculator.GetFinalAttackPointWithoutClash(
            enemyUnit,
            enemyCard,
            enemyPointBuffSnapshot.nextCardPointModifier,
            enemyResourceSnapshot.selectedMinPoint,
            enemyResourceSnapshot.selectedMaxPoint,
            enemyResourceSnapshot.pointModifierFromResource
        );
        int damageScaled = BattleCalculator.GetFinalDamageScaled(
            enemyUnit,
            target,
            enemyCard,
            enemyAttackPoint
        );
        int finalHpDamage = BattleCalculator.ConvertScaledDamageToHPDamage(damageScaled);

        ConsumeSuccessfulPointCardBuffs(enemyUnit, enemyPointBuffSnapshot);
        PayDefaultResourceCostOnSuccessfulUse(enemyUnit, enemyResourceSnapshot);

        TriggerBattleEvent(BattleTiming.Resolved, enemyUnit, target, enemyIntent.enemyCardState, enemyAttackPoint, 0, false, false);
        TriggerBattleEvent(BattleTiming.Hit, enemyUnit, target, enemyIntent.enemyCardState, enemyAttackPoint, finalHpDamage, true, false);
        ApplyDamageAndTriggerEvents(enemyUnit, target, enemyIntent.enemyCardState, finalHpDamage, enemyAttackPoint);

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
        result.triggeredEventChain = true;
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
            "。已触发 Resolved / Hit，并按实际伤害触发 AfterDamage / AfterKill";

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

        PointBuffSnapshot playerPointBuffSnapshot = CapturePointBuffSnapshot(playerUnit);
        PointBuffSnapshot enemyPointBuffSnapshot = CapturePointBuffSnapshot(enemyUnit);

        TriggerActionStart(enemyUnit, actualTarget, enemyCardState);
        TriggerActionStart(playerUnit, enemyUnit, playerCardState);

        CardResourceSnapshot playerResourceSnapshot = CaptureResourceSnapshot(playerUnit, playerCardState);
        CardResourceSnapshot enemyResourceSnapshot = CaptureResourceSnapshot(enemyUnit, enemyCardState);

        TriggerBattleEvent(BattleTiming.BeforeUse, enemyUnit, actualTarget, enemyCardState, 0, 0, false, false);
        TriggerBattleEvent(BattleTiming.BeforeUse, playerUnit, enemyUnit, playerCardState, 0, 0, false, false);

        enemyUnit.CheckBuffsByTiming(BattleTiming.ClashStart, false);
        playerUnit.CheckBuffsByTiming(BattleTiming.ClashStart, false);

        int playerPoint = 0;
        int enemyPoint = 0;

        for (int attempt = 1; attempt <= MaxRespondedEnemyIntentClashAttempts; attempt++)
        {
            playerPoint = BattleCalculator.GetFinalClashPoint(
                playerUnit,
                playerCardState.cardData,
                playerPointBuffSnapshot.nextClashPointModifier,
                playerPointBuffSnapshot.nextCardPointModifier,
                playerResourceSnapshot.selectedMinPoint,
                playerResourceSnapshot.selectedMaxPoint,
                playerResourceSnapshot.pointModifierFromResource
            );
            enemyPoint = BattleCalculator.GetFinalClashPoint(
                enemyUnit,
                enemyCardState.cardData,
                enemyPointBuffSnapshot.nextClashPointModifier,
                enemyPointBuffSnapshot.nextCardPointModifier,
                enemyResourceSnapshot.selectedMinPoint,
                enemyResourceSnapshot.selectedMaxPoint,
                enemyResourceSnapshot.pointModifierFromResource
            );

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

            ConsumeClashPointBuffs(playerUnit, playerPointBuffSnapshot);
            ConsumeClashPointBuffs(enemyUnit, enemyPointBuffSnapshot);

            if (isPlayerWin)
            {
                ConsumeSuccessfulPointCardBuffs(playerUnit, playerPointBuffSnapshot);
                PayDefaultResourceCostOnSuccessfulUse(playerUnit, playerResourceSnapshot);
            }
            else
            {
                ConsumeSuccessfulPointCardBuffs(enemyUnit, enemyPointBuffSnapshot);
                PayDefaultResourceCostOnSuccessfulUse(enemyUnit, enemyResourceSnapshot);
            }

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
                true,
                false,
                ClashResult.Win
            );

            ApplyDamageAndTriggerEvents(attacker, defender, winnerCardState, hpDamage, winnerPoint);

            BattleResolveResult result = new BattleResolveResult();
            result.isSuccess = true;
            result.shouldCompleteItem = true;
            result.playerCardUsed = isPlayerWin;
            result.enemyCardUsed = !isPlayerWin;
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

        BattleResolveResult passiveGuardResult = null;
        bool selectedDodge = selectedPassiveGuardSlot.cardState != null &&
            selectedPassiveGuardSlot.cardState.cardData != null &&
            selectedPassiveGuardSlot.cardState.cardData.cardType == CardType.Dodge;

        if (selectedDodge)
        {
            passiveGuardResult = ResolveDodgeVsAttackWithKnownEnemyPoint(
                selectedPassiveGuardSlot,
                enemyIntent,
                enemyPoint
            );
        }
        else
        {
            passiveGuardResult = ResolveDefenseVsAttackWithKnownEnemyPoint(
                selectedPassiveGuardSlot,
                enemyIntent,
                enemyPoint
            );
        }

        if (passiveGuardResult == null)
        {
            Debug.LogWarning("EnemyWin PassiveGuard 结算失败：Resolver 返回空结果，回退原 EnemyWin 伤害流程");
            return null;
        }

        if (!passiveGuardResult.playerCardUsed && passiveGuardResult.resultType != "TieLimit")
        {
            Debug.LogWarning("EnemyWin PassiveGuard 未成功使用守备卡，回退原 EnemyWin 伤害流程：" + passiveGuardResult.message);
            return null;
        }

        if (!passiveGuardResult.isSuccess || !passiveGuardResult.shouldCompleteItem)
        {
            Debug.LogWarning("EnemyWin PassiveGuard 已进入守备使用流程但结果不可完成，不回退原始伤害：" + passiveGuardResult.message);
            passiveGuardResult.triggeredPassiveGuardSlot = passiveGuardResult.playerCardUsed
                ? selectedPassiveGuardSlot
                : null;
            return passiveGuardResult;
        }

        BattleResolveResult result = new BattleResolveResult();
        result.isSuccess = true;
        result.shouldCompleteItem = true;
        result.playerCardUsed = false;
        result.enemyCardUsed = true;
        result.hasDamage = passiveGuardResult.hasDamage;
        result.damage = passiveGuardResult.damage;
        result.damagedCharacter = passiveGuardResult.damagedCharacter;
        result.resultType = selectedDodge
            ? passiveGuardResult.resultType
            : passiveGuardResult.resultType == "DefenseFullBlock"
                ? "EnemyWinPassiveGuardFullBlock"
                : "EnemyWinPassiveGuardReducedDamage";
        result.playerPoint = playerPoint;
        result.enemyPoint = enemyPoint;
        result.clashAttemptCount = clashAttemptCount;
        result.isTieLimitReached = passiveGuardResult.isTieLimitReached;
        result.triggeredEventChain = true;
        result.triggeredPassiveGuardSlot = passiveGuardResult.playerCardUsed
            ? selectedPassiveGuardSlot
            : null;
        result.message =
            "ResolveRespondedEnemyIntent 完成：" +
            result.resultType +
            "，玩家最终拼点 " +
            playerPoint +
            "，敌人最终胜利点数 " +
            enemyPoint +
            "，实际触发 PassiveGuard 槽位 " +
            selectedPassiveGuardSlot.GetDisplaySlotName() +
            "，守备结果 " +
            passiveGuardResult.resultType +
            "，最终伤害 " +
            passiveGuardResult.damage +
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

        if (enemyIntent.actualTargetCharacter.IsDead())
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

        if (slot.owner == null || slot.actor == null || slot.target == null)
        {
            return false;
        }

        if (slot.owner.IsDead() || slot.actor.IsDead() || slot.target.IsDead())
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

        if (slot.cardState.cardData.cardType != CardType.Defense &&
            slot.cardState.cardData.cardType != CardType.Dodge)
        {
            return false;
        }

        return BattleCardManager.CanUseCard(slot.actor, enemyIntent.enemy, slot.cardState);
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

        PointBuffSnapshot playerPointBuffSnapshot = CapturePointBuffSnapshot(playerUnit);
        PointBuffSnapshot enemyPointBuffSnapshot = CapturePointBuffSnapshot(enemyUnit);

        TriggerActionStart(enemyUnit, actualTarget, enemyCardState);
        TriggerActionStart(playerUnit, enemyUnit, defenseCardState);

        CardResourceSnapshot playerResourceSnapshot = CaptureResourceSnapshot(playerUnit, defenseCardState);
        CardResourceSnapshot enemyResourceSnapshot = CaptureResourceSnapshot(enemyUnit, enemyCardState);

        TriggerBattleEvent(BattleTiming.BeforeUse, enemyUnit, actualTarget, enemyCardState, 0, 0, false, false);
        TriggerBattleEvent(BattleTiming.BeforeUse, playerUnit, enemyUnit, defenseCardState, 0, 0, false, false);

        enemyUnit.CheckBuffsByTiming(BattleTiming.ClashStart, false);
        playerUnit.CheckBuffsByTiming(BattleTiming.ClashStart, false);

        int enemyFinalAttackPoint = BattleCalculator.GetFinalAttackPointWithoutClash(
            enemyUnit,
            enemyCardState.cardData,
            enemyPointBuffSnapshot.nextCardPointModifier,
            enemyResourceSnapshot.selectedMinPoint,
            enemyResourceSnapshot.selectedMaxPoint,
            enemyResourceSnapshot.pointModifierFromResource
        );

        return ResolveDefenseVsAttackCore(
            actionSlot,
            enemyIntent,
            enemyFinalAttackPoint,
            true,
            true,
            "ResolveRespondedDefenseVsAttack",
            false,
            playerPointBuffSnapshot,
            enemyPointBuffSnapshot,
            playerResourceSnapshot,
            enemyResourceSnapshot
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

        if (!BattleCardManager.CanUseCard(defenseSlot.actor, enemyIntent.enemy, defenseSlot.cardState))
        {
            return CreateActionUnavailableResult(
                "ResolveDefenseVsAttackWithKnownEnemyPoint：防御卡执行时已不可用，本次守备跳过。" +
                defenseSlot.actor.characterName +
                " 的卡牌不能使用：" +
                defenseSlot.cardState.GetCardName()
            );
        }

        PointBuffSnapshot playerPointBuffSnapshot = CapturePointBuffSnapshot(defenseSlot.actor);

        TriggerActionStart(defenseSlot.actor, enemyIntent.enemy, defenseSlot.cardState);
        CardResourceSnapshot playerResourceSnapshot = CaptureResourceSnapshot(defenseSlot.actor, defenseSlot.cardState);

        TriggerBattleEvent(BattleTiming.BeforeUse, defenseSlot.actor, enemyIntent.enemy, defenseSlot.cardState, 0, 0, false, false);

        defenseSlot.actor.CheckBuffsByTiming(BattleTiming.ClashStart, false);

        int clampedKnownEnemyAttackPoint = Mathf.Max(0, knownEnemyAttackPoint);

        return ResolveDefenseVsAttackCore(
            defenseSlot,
            enemyIntent,
            clampedKnownEnemyAttackPoint,
            false,
            false,
            "ResolveDefenseVsAttackWithKnownEnemyPoint",
            true,
            playerPointBuffSnapshot,
            new PointBuffSnapshot(),
            playerResourceSnapshot,
            new CardResourceSnapshot()
        );
    }

    static BattleResolveResult ResolveDefenseVsAttackCore(
        BattleActionSlot defenseSlot,
        BattleEnemyIntent enemyIntent,
        int enemyFinalAttackPoint,
        bool shouldTriggerEnemyResolved,
        bool enemyCardUsed,
        string messagePrefix,
        bool isKnownPointContinuation,
        PointBuffSnapshot playerPointBuffSnapshot,
        PointBuffSnapshot enemyPointBuffSnapshot,
        CardResourceSnapshot playerResourceSnapshot,
        CardResourceSnapshot enemyResourceSnapshot
    )
    {
        CharacterData playerUnit = defenseSlot.actor;
        BattleCardState defenseCardState = defenseSlot.cardState;
        CharacterData enemyUnit = enemyIntent.enemy;
        BattleCardState enemyCardState = enemyIntent.enemyCardState;
        CharacterData actualTarget = enemyIntent.actualTargetCharacter;

        enemyFinalAttackPoint = Mathf.Max(0, enemyFinalAttackPoint);

        int playerFinalDefensePointScaled = BattleCalculator.GetFinalDefensePointScaled(
            playerUnit,
            defenseCardState.cardData,
            playerPointBuffSnapshot.nextCardPointModifier,
            playerResourceSnapshot.selectedMinPoint,
            playerResourceSnapshot.selectedMaxPoint,
            playerResourceSnapshot.pointModifierFromResource
        );
        int playerFinalDefensePoint = BattleCalculator.ConvertScaledDamageToHPDamage(playerFinalDefensePointScaled);
        int remainingAttackPoint = BattleCalculator.CalculateRemainingAttackPointAfterDefense(
            enemyFinalAttackPoint,
            playerFinalDefensePointScaled
        );

        ConsumeDefensePointBuffs(playerUnit);
        ConsumeSuccessfulPointCardBuffs(playerUnit, playerPointBuffSnapshot);
        PayDefaultResourceCostOnSuccessfulUse(playerUnit, playerResourceSnapshot);

        if (!isKnownPointContinuation)
        {
            ConsumeSuccessfulPointCardBuffs(enemyUnit, enemyPointBuffSnapshot);
            PayDefaultResourceCostOnSuccessfulUse(enemyUnit, enemyResourceSnapshot);
        }

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
        }

        TriggerBattleEvent(BattleTiming.Hit, enemyUnit, actualTarget, enemyCardState, remainingAttackPoint, finalHpDamage, true, false);
        ApplyDamageAndTriggerEvents(enemyUnit, actualTarget, enemyCardState, finalHpDamage, remainingAttackPoint);

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

    internal static BattleResolveResult ResolveDodgeVsAttackWithKnownEnemyPoint(
        BattleActionSlot dodgeSlot,
        BattleEnemyIntent enemyIntent,
        int knownEnemyAttackPoint
    )
    {
        if (dodgeSlot == null)
        {
            return CreateInvalidResolveResult("ResolveDodgeVsAttackWithKnownEnemyPoint 失败：闪避槽位为空");
        }

        if (dodgeSlot.actor == null)
        {
            return CreateInvalidResolveResult("ResolveDodgeVsAttackWithKnownEnemyPoint 失败：闪避者为空");
        }

        if (dodgeSlot.cardState == null || dodgeSlot.cardState.cardData == null)
        {
            return CreateInvalidResolveResult("ResolveDodgeVsAttackWithKnownEnemyPoint 失败：闪避卡为空");
        }

        if (enemyIntent == null)
        {
            return CreateInvalidResolveResult("ResolveDodgeVsAttackWithKnownEnemyPoint 失败：敌人意图为空");
        }

        if (enemyIntent.enemy == null)
        {
            return CreateInvalidResolveResult("ResolveDodgeVsAttackWithKnownEnemyPoint 失败：敌人为空");
        }

        if (enemyIntent.enemyCardState == null || enemyIntent.enemyCardState.cardData == null)
        {
            return CreateInvalidResolveResult("ResolveDodgeVsAttackWithKnownEnemyPoint 失败：敌人攻击卡为空");
        }

        if (enemyIntent.actualTargetCharacter == null)
        {
            return CreateInvalidResolveResult("ResolveDodgeVsAttackWithKnownEnemyPoint 失败：实际目标为空");
        }

        if (dodgeSlot.cardState.cardData.cardType != CardType.Dodge)
        {
            return CreateInvalidResolveResult(
                "ResolveDodgeVsAttackWithKnownEnemyPoint 失败：玩家卡牌不是 Dodge：" +
                dodgeSlot.cardState.cardData.cardType
            );
        }

        if (enemyIntent.enemyCardState.cardData.cardType != CardType.Attack)
        {
            return CreateInvalidResolveResult(
                "ResolveDodgeVsAttackWithKnownEnemyPoint 失败：敌人卡牌不是 Attack：" +
                enemyIntent.enemyCardState.cardData.cardType
            );
        }

        if (IsInvalidPointRange(dodgeSlot.cardState.cardData.minPoint, dodgeSlot.cardState.cardData.maxPoint))
        {
            return CreateInvalidResolveResult(
                "ResolveDodgeVsAttackWithKnownEnemyPoint 失败：玩家闪避卡点数范围异常：" +
                dodgeSlot.cardState.cardData.minPoint +
                "-" +
                dodgeSlot.cardState.cardData.maxPoint
            );
        }

        int fixedEnemyAttackPoint = Mathf.Max(0, knownEnemyAttackPoint);
        CharacterData playerUnit = dodgeSlot.actor;
        CharacterData enemyUnit = enemyIntent.enemy;
        CharacterData actualTarget = enemyIntent.actualTargetCharacter;
        BattleCardState dodgeCardState = dodgeSlot.cardState;
        BattleCardState enemyCardState = enemyIntent.enemyCardState;

        if (!BattleCardManager.CanUseCard(playerUnit, enemyUnit, dodgeCardState))
        {
            return CreateActionUnavailableResult(
                "ResolveDodgeVsAttackWithKnownEnemyPoint：闪避卡执行时已不可用，本次守备跳过。" +
                playerUnit.characterName +
                " 的卡牌不能使用：" +
                dodgeCardState.GetCardName()
            );
        }

        PointBuffSnapshot playerPointBuffSnapshot = CapturePointBuffSnapshot(playerUnit);

        TriggerActionStart(playerUnit, enemyUnit, dodgeCardState);
        CardResourceSnapshot playerResourceSnapshot = CaptureResourceSnapshot(playerUnit, dodgeCardState);

        TriggerBattleEvent(BattleTiming.BeforeUse, playerUnit, enemyUnit, dodgeCardState, 0, 0, false, false);

        playerUnit.CheckBuffsByTiming(BattleTiming.ClashStart, false);

        int playerDodgePoint = 0;

        for (int attempt = 1; attempt <= MaxRespondedEnemyIntentClashAttempts; attempt++)
        {
            playerDodgePoint = BattleCalculator.GetFinalClashPoint(
                playerUnit,
                dodgeCardState.cardData,
                playerPointBuffSnapshot.nextClashPointModifier,
                playerPointBuffSnapshot.nextCardPointModifier,
                playerResourceSnapshot.selectedMinPoint,
                playerResourceSnapshot.selectedMaxPoint,
                playerResourceSnapshot.pointModifierFromResource
            );

            if (playerDodgePoint == fixedEnemyAttackPoint)
            {
                Debug.Log(
                    "ResolveDodgeVsAttackWithKnownEnemyPoint 第" +
                    attempt +
                    "次平局：玩家 Dodge 点数 " +
                    playerDodgePoint +
                    "，固定敌人 Attack 点数 " +
                    fixedEnemyAttackPoint +
                    "。只重新 Roll Dodge"
                );

                continue;
            }

            if (playerDodgePoint > fixedEnemyAttackPoint)
            {
                ConsumeClashPointBuffs(playerUnit, playerPointBuffSnapshot);
                ConsumeSuccessfulPointCardBuffs(playerUnit, playerPointBuffSnapshot);
                PayDefaultResourceCostOnSuccessfulUse(playerUnit, playerResourceSnapshot);

                TriggerBattleEvent(BattleTiming.ClashWin, playerUnit, enemyUnit, dodgeCardState, playerDodgePoint, 0, false, false, ClashResult.Win);
                TriggerBattleEvent(BattleTiming.Resolved, playerUnit, enemyUnit, dodgeCardState, playerDodgePoint, 0, false, false, ClashResult.Win);

                BattleResolveResult successResult = new BattleResolveResult();
                successResult.isSuccess = true;
                successResult.shouldCompleteItem = true;
                successResult.playerCardUsed = true;
                successResult.enemyCardUsed = false;
                successResult.hasDamage = false;
                successResult.damage = 0;
                successResult.damagedCharacter = null;
                successResult.resultType = "DodgeSuccess";
                successResult.playerPoint = playerDodgePoint;
                successResult.enemyPoint = fixedEnemyAttackPoint;
                successResult.clashAttemptCount = attempt;
                successResult.isTieLimitReached = false;
                successResult.triggeredEventChain = true;
                successResult.message =
                    "ResolveDodgeVsAttackWithKnownEnemyPoint 完成：DodgeSuccess，玩家 Dodge 点数 " +
                    playerDodgePoint +
                    "，固定敌人 Attack 点数 " +
                    fixedEnemyAttackPoint +
                    "。使用已确定敌人点数，未重新 Roll，闪避成功，无伤害";

                Debug.Log(successResult.message);

                return successResult;
            }

            ConsumeClashPointBuffs(playerUnit, playerPointBuffSnapshot);
            ConsumeSuccessfulPointCardBuffs(playerUnit, playerPointBuffSnapshot);
            PayDefaultResourceCostOnSuccessfulUse(playerUnit, playerResourceSnapshot);

            TriggerBattleEvent(BattleTiming.ClashLose, playerUnit, enemyUnit, dodgeCardState, playerDodgePoint, 0, false, false, ClashResult.Lose);
            TriggerBattleEvent(BattleTiming.Resolved, playerUnit, enemyUnit, dodgeCardState, playerDodgePoint, 0, false, false, ClashResult.Lose);

            int damageScaled = BattleCalculator.GetFinalDamageScaled(
                enemyUnit,
                actualTarget,
                enemyCardState.cardData,
                fixedEnemyAttackPoint
            );
            int hpDamage = BattleCalculator.ConvertScaledDamageToHPDamage(damageScaled);

            TriggerBattleEvent(BattleTiming.Hit, enemyUnit, actualTarget, enemyCardState, fixedEnemyAttackPoint, hpDamage, true, false, ClashResult.Win);
            ApplyDamageAndTriggerEvents(enemyUnit, actualTarget, enemyCardState, hpDamage, fixedEnemyAttackPoint);

            BattleResolveResult failedResult = new BattleResolveResult();
            failedResult.isSuccess = true;
            failedResult.shouldCompleteItem = true;
            failedResult.playerCardUsed = true;
            failedResult.enemyCardUsed = false;
            failedResult.hasDamage = hpDamage > 0;
            failedResult.damage = hpDamage;
            failedResult.damagedCharacter = hpDamage > 0 ? actualTarget : null;
            failedResult.resultType = "DodgeFailed";
            failedResult.playerPoint = playerDodgePoint;
            failedResult.enemyPoint = fixedEnemyAttackPoint;
            failedResult.clashAttemptCount = attempt;
            failedResult.isTieLimitReached = false;
            failedResult.triggeredEventChain = true;
            failedResult.message =
                "ResolveDodgeVsAttackWithKnownEnemyPoint 完成：DodgeFailed，玩家 Dodge 点数 " +
                playerDodgePoint +
                "，固定敌人 Attack 点数 " +
                fixedEnemyAttackPoint +
                "，最终 HP 伤害 " +
                hpDamage +
                "。使用已确定敌人点数，未重新 Roll";

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
        tieLimitResult.enemyPoint = fixedEnemyAttackPoint;
        tieLimitResult.clashAttemptCount = MaxRespondedEnemyIntentClashAttempts;
        tieLimitResult.isTieLimitReached = true;
        tieLimitResult.triggeredEventChain = false;
        tieLimitResult.message =
            "ResolveDodgeVsAttackWithKnownEnemyPoint 连续 " +
            MaxRespondedEnemyIntentClashAttempts +
            " 次与固定敌人点数相等，进入 TieLimit。Dodge 不算使用，不回落 EnemyWin 伤害";

        Debug.Log(tieLimitResult.message);

        return tieLimitResult;
    }

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

        PointBuffSnapshot playerPointBuffSnapshot = CapturePointBuffSnapshot(playerUnit);
        PointBuffSnapshot enemyPointBuffSnapshot = CapturePointBuffSnapshot(enemyUnit);

        TriggerActionStart(enemyUnit, actualTarget, enemyCardState);
        TriggerActionStart(playerUnit, enemyUnit, dodgeCardState);

        CardResourceSnapshot playerResourceSnapshot = CaptureResourceSnapshot(playerUnit, dodgeCardState);
        CardResourceSnapshot enemyResourceSnapshot = CaptureResourceSnapshot(enemyUnit, enemyCardState);

        TriggerBattleEvent(BattleTiming.BeforeUse, enemyUnit, actualTarget, enemyCardState, 0, 0, false, false);
        TriggerBattleEvent(BattleTiming.BeforeUse, playerUnit, enemyUnit, dodgeCardState, 0, 0, false, false);

        enemyUnit.CheckBuffsByTiming(BattleTiming.ClashStart, false);
        playerUnit.CheckBuffsByTiming(BattleTiming.ClashStart, false);

        int playerDodgePoint = 0;
        int enemyAttackPoint = 0;

        for (int attempt = 1; attempt <= MaxRespondedEnemyIntentClashAttempts; attempt++)
        {
            playerDodgePoint = BattleCalculator.GetFinalClashPoint(
                playerUnit,
                dodgeCardState.cardData,
                playerPointBuffSnapshot.nextClashPointModifier,
                playerPointBuffSnapshot.nextCardPointModifier,
                playerResourceSnapshot.selectedMinPoint,
                playerResourceSnapshot.selectedMaxPoint,
                playerResourceSnapshot.pointModifierFromResource
            );
            enemyAttackPoint = BattleCalculator.GetFinalClashPoint(
                enemyUnit,
                enemyCardState.cardData,
                enemyPointBuffSnapshot.nextClashPointModifier,
                enemyPointBuffSnapshot.nextCardPointModifier,
                enemyResourceSnapshot.selectedMinPoint,
                enemyResourceSnapshot.selectedMaxPoint,
                enemyResourceSnapshot.pointModifierFromResource
            );

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
                ConsumeClashPointBuffs(playerUnit, playerPointBuffSnapshot);
                ConsumeSuccessfulPointCardBuffs(playerUnit, playerPointBuffSnapshot);
                ConsumeClashPointBuffs(enemyUnit, enemyPointBuffSnapshot);
                ConsumeSuccessfulPointCardBuffs(enemyUnit, enemyPointBuffSnapshot);
                PayDefaultResourceCostOnSuccessfulUse(playerUnit, playerResourceSnapshot);
                PayDefaultResourceCostOnSuccessfulUse(enemyUnit, enemyResourceSnapshot);

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

            ConsumeClashPointBuffs(playerUnit, playerPointBuffSnapshot);
            ConsumeSuccessfulPointCardBuffs(playerUnit, playerPointBuffSnapshot);
            ConsumeClashPointBuffs(enemyUnit, enemyPointBuffSnapshot);
            ConsumeSuccessfulPointCardBuffs(enemyUnit, enemyPointBuffSnapshot);
            PayDefaultResourceCostOnSuccessfulUse(playerUnit, playerResourceSnapshot);
            PayDefaultResourceCostOnSuccessfulUse(enemyUnit, enemyResourceSnapshot);

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

            TriggerBattleEvent(BattleTiming.Hit, enemyUnit, actualTarget, enemyCardState, enemyAttackPoint, hpDamage, true, false, ClashResult.Win);
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

    static int ConsumeClashPointBuffs(CharacterData unit)
    {
        if (unit == null)
        {
            return 0;
        }

        return unit.ConsumeBuffsByRule(ConsumeRuleFormalClashResolved);
    }

    static int ConsumeClashPointBuffs(CharacterData unit, PointBuffSnapshot snapshot)
    {
        if (unit == null)
        {
            return 0;
        }

        return unit.ConsumeBuffStackByRule(
            BuffNextClashPointUp,
            ConsumeRuleFormalClashResolved,
            snapshot.nextClashPointStack
        );
    }

    static int ConsumeSuccessfulPointCardBuffs(CharacterData unit)
    {
        if (unit == null)
        {
            return 0;
        }

        return unit.ConsumeBuffsByRule(ConsumeRuleSuccessfulPointCardUsed);
    }

    static int ConsumeSuccessfulPointCardBuffs(CharacterData unit, PointBuffSnapshot snapshot)
    {
        if (unit == null)
        {
            return 0;
        }

        return unit.ConsumeBuffStackByRule(
            BuffNextCardPointUp,
            ConsumeRuleSuccessfulPointCardUsed,
            snapshot.nextCardPointStack
        );
    }

    static int ConsumeDefensePointBuffs(CharacterData unit)
    {
        return ConsumeClashStartTriggeredBuffs(unit, BuffGuardUp, BuffGuardDown);
    }

    static int ConsumeClashStartTriggeredBuffs(CharacterData unit, params string[] buffIDs)
    {
        if (unit == null)
        {
            return 0;
        }

        return unit.ConsumeTriggeredBuffs(BattleTiming.ClashStart, buffIDs);
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

    static PointBuffSnapshot CapturePointBuffSnapshot(CharacterData unit)
    {
        PointBuffSnapshot snapshot = new PointBuffSnapshot();

        if (unit == null)
        {
            return snapshot;
        }

        snapshot.nextCardPointStack = unit.GetBuffStack(BuffNextCardPointUp);
        snapshot.nextCardPointModifier = GetBuffModifierFromStack(BuffNextCardPointUp, snapshot.nextCardPointStack);
        snapshot.nextClashPointStack = unit.GetBuffStack(BuffNextClashPointUp);
        snapshot.nextClashPointModifier = GetBuffModifierFromStack(BuffNextClashPointUp, snapshot.nextClashPointStack);

        return snapshot;
    }

    static CardResourceSnapshot CaptureResourceSnapshot(CharacterData unit, BattleCardState cardState)
    {
        CardResourceSnapshot snapshot = new CardResourceSnapshot();
        snapshot.cardState = cardState;

        if (cardState == null || cardState.cardData == null)
        {
            return snapshot;
        }

        snapshot.selectedMinPoint = cardState.cardData.minPoint;
        snapshot.selectedMaxPoint = cardState.cardData.maxPoint;

        CardResourceRuleData rule = GetFirstResourceRule(cardState.cardData);

        if (rule == null)
        {
            return snapshot;
        }

        if (rule.resourceType != ResourceTypeBuffStack ||
            string.IsNullOrEmpty(rule.resourceID))
        {
            Debug.LogWarning(cardState.GetCardName() + " 的软资源规则暂不支持：" + rule.resourceType + " / " + rule.resourceID);
            return snapshot;
        }

        snapshot.hasRule = true;
        snapshot.resourceID = rule.resourceID;
        snapshot.capturedStack = unit != null ? unit.GetBuffStack(rule.resourceID) : 0;
        snapshot.normalVersionEnabled = snapshot.capturedStack >= Mathf.Max(0, rule.requiredStackForNormalVersion);

        if (snapshot.normalVersionEnabled)
        {
            snapshot.selectedMinPoint = cardState.cardData.minPoint;
            snapshot.selectedMaxPoint = cardState.cardData.maxPoint;
        }
        else
        {
            snapshot.selectedMinPoint = rule.fallbackMinPoint;
            snapshot.selectedMaxPoint = rule.fallbackMaxPoint;
        }

        if (snapshot.selectedMaxPoint < snapshot.selectedMinPoint)
        {
            int temp = snapshot.selectedMinPoint;
            snapshot.selectedMinPoint = snapshot.selectedMaxPoint;
            snapshot.selectedMaxPoint = temp;
        }

        snapshot.pointModifierFromResource = snapshot.capturedStack * rule.pointPerStack;

        if (rule.exactStackForBonus > 0 &&
            snapshot.capturedStack == rule.exactStackForBonus)
        {
            snapshot.pointModifierFromResource += rule.exactStackPointBonus;
        }

        snapshot.plannedConsumeAmount = Mathf.Max(0, rule.consumeAmountOnSuccess);
        snapshot.shouldConsumeOnSuccess = snapshot.normalVersionEnabled && snapshot.plannedConsumeAmount > 0;

        return snapshot;
    }

    static CardResourceRuleData GetFirstResourceRule(CardTestData cardData)
    {
        if (cardData == null)
        {
            return null;
        }

        if (cardData.resourceRule != null)
        {
            return cardData.resourceRule;
        }

        if (cardData.resourceRules != null && cardData.resourceRules.Length > 0)
        {
            return cardData.resourceRules[0];
        }

        return null;
    }

    static void TriggerActionStart(CharacterData user, CharacterData target, BattleCardState cardState)
    {
        TriggerBattleEvent(BattleTiming.ActionStart, user, target, cardState, 0, 0, false, false);
    }

    static void PayDefaultResourceCostOnSuccessfulUse(CharacterData unit, CardResourceSnapshot snapshot)
    {
        // 默认资源成本只在本次卡牌被视为成功使用时支付。
        // Attack拼点失败、ActionUnavailable、TieLimit和死亡跳过不会支付。
        // 无资源降级版本即使成功使用，也不会凭空扣除资源。
        if (unit == null || !snapshot.hasRule || !snapshot.shouldConsumeOnSuccess)
        {
            return;
        }

        int consumedAmount;
        bool paid = unit.TryConsumeBuffStackAsResource(
            snapshot.resourceID,
            snapshot.plannedConsumeAmount,
            out consumedAmount
        );

        if (!paid)
        {
            string cardName = snapshot.cardState != null
                ? snapshot.cardState.GetCardName()
                : "未知卡牌";

            Debug.LogWarning(
                unit.characterName +
                " 支付卡牌资源不足：卡牌 " +
                cardName +
                " / resourceID " +
                snapshot.resourceID +
                " / 计划消耗 " +
                snapshot.plannedConsumeAmount +
                " / 实际消耗 " +
                consumedAmount +
                " / 快照层数 " +
                snapshot.capturedStack
            );
        }
    }

    static int GetBuffModifierFromStack(string buffID, int stack)
    {
        if (string.IsNullOrEmpty(buffID) || stack <= 0)
        {
            return 0;
        }

        BuffDefinitionData definition;

        if (!BuffDefinitionLoader.TryGetDefinition(buffID, out definition) || definition == null)
        {
            return 0;
        }

        return Mathf.RoundToInt(stack * definition.valuePerStack);
    }

    static bool IsInvalidPointRange(int minPoint, int maxPoint)
    {
        return minPoint < 0 || maxPoint < 0 || maxPoint < minPoint;
    }

    static BattleResolveResult CreateActionUnavailableResult(string message)
    {
        BattleResolveResult result = new BattleResolveResult();
        result.isSuccess = false;
        result.shouldCompleteItem = true;
        result.playerCardUsed = false;
        result.enemyCardUsed = false;
        result.hasDamage = false;
        result.damage = 0;
        result.damagedCharacter = null;
        result.resultType = "ActionUnavailable";
        result.playerPoint = 0;
        result.enemyPoint = 0;
        result.clashAttemptCount = 0;
        result.isTieLimitReached = false;
        result.triggeredEventChain = false;
        result.message = message;

        Debug.LogWarning(message);

        return result;
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
