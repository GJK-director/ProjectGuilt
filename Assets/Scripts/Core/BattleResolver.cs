// 脚本中文说明：战斗结算器。负责处理卡牌使用、拼点、生效、命中、伤害和击杀事件。
using System.Collections.Generic;
using UnityEngine;

public enum BattleCardUseDisposition
{
    None,
    FinalizeImmediately,
    DeferForContinuousDodge
}

public class BattleResolveResult
{
    public bool isSuccess;
    public bool shouldCompleteItem;

    public bool playerCardUsed;
    public bool enemyCardUsed;
    public bool playerCardParticipated;
    public BattleCardUseDisposition playerCardUseDisposition;

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
    const string BuffNextClashPointUp = "NextClashPointUp";
    const string BuffNextCardPointUp = "NextCardPointUp";
    const string BuffGuardUp = "GuardUp";
    const string BuffGuardDown = "GuardDown";
    const string ConsumeRuleFormalClashResolved = "FormalClashResolved";
    const string ConsumeRuleSuccessfulPointCardUsed = "SuccessfulPointCardUsed";
    const string ResourceTypeBuffStack = "BuffStack";

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

        bool isAbilityCard = actionSlot.cardState.cardData.cardType == CardType.Ability || actionSlot.cardState.IsAbilitySinCard();
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
        BattleResolveResult failureResult;
        BattleResolutionPlan plan = BuildFreeAttackResolutionPlan(
            null,
            actionSlot,
            out failureResult
        );
        if (plan == null)
        {
            return failureResult ?? CreateInvalidResolveResult(
                "ResolveFreeAction 失败：无法建立 FreeAttack ResolutionPlan"
            );
        }

        if (!TryRollFreeAttackResolutionPlan(plan, out int rolledPoint))
        {
            return CreateInvalidResolveResult(
                "ResolveFreeAction 失败：无法生成 FreeAttack 点数"
            );
        }

        return CommitResolutionSynchronously(plan);
    }

    internal static BattleResolutionPlan BuildFreeAttackResolutionPlan(
        BattleExecutionItem executionItem,
        BattleActionSlot actionSlot,
        out BattleResolveResult failureResult
    )
    {
        if (actionSlot == null || actionSlot.actor == null ||
            actionSlot.cardState == null ||
            actionSlot.cardState.cardData == null)
        {
            failureResult = CreateInvalidResolveResult(
                "ResolveFreeAction 失败：FreeAttack行动数据不完整"
            );
            return null;
        }

        BattleExecutionAction attackAction = new BattleExecutionAction(
            actionSlot.actor,
            actionSlot.cardState,
            actionSlot,
            null,
            actionSlot.target
        );
        return BuildUnilateralAttackResolutionPlan(
            attackAction,
            executionItem,
            null,
            out failureResult
        );
    }

    internal static BattleResolveResult ResolveUnilateralAttack(
        BattleExecutionAction attackAction
    )
    {
        BattleResolutionPlan plan = BuildUnilateralAttackResolutionPlan(
            attackAction,
            null,
            null,
            out BattleResolveResult failureResult
        );
        if (plan == null)
        {
            return failureResult ?? CreateInvalidResolveResult(
                "ResolveUnilateralAttack 失败：无法建立ResolutionPlan"
            );
        }

        if (!TryRollUnilateralAttackResolutionPlan(plan, out _))
        {
            return CreateInvalidResolveResult(
                "ResolveUnilateralAttack 失败：无法生成Attack点数"
            );
        }

        return CommitResolutionSynchronously(plan);
    }

    internal static BattleResolutionPlan BuildUnilateralAttackResolutionPlan(
        BattleExecutionAction attackAction,
        BattleExecutionItem executionItem,
        BattleActionSlot compatibilityActionSlot,
        out BattleResolveResult failureResult
    )
    {
        failureResult = null;
        if (!IsValidExecutionAction(attackAction) ||
            attackAction.cardState.cardData.cardType != CardType.Attack)
        {
            failureResult = CreateInvalidResolveResult(
                "ResolveUnilateralAttack 失败：Attack Action无效"
            );
            return null;
        }

        CharacterData user = attackAction.actor;
        CharacterData target = attackAction.target;
        CardTestData attackCard = attackAction.cardState.cardData;
        if (target == null)
        {
            failureResult = CreateInvalidResolveResult(
                "ResolveUnilateralAttack 失败：Attack目标为空"
            );
            return null;
        }
        if (IsInvalidPointRange(attackCard.minPoint, attackCard.maxPoint))
        {
            failureResult = CreateInvalidResolveResult(
                "ResolveUnilateralAttack 失败：Attack点数范围异常：" +
                attackCard.minPoint +
                "-" +
                attackCard.maxPoint
            );
            return null;
        }
        if (!BattleCardManager.CanUseCard(user, target, attackAction.cardState))
        {
            failureResult = CreateActionUnavailableResult(
                "ResolveUnilateralAttack：Attack执行时已不可用，本次行动跳过。" +
                user.characterName +
                " 的卡牌不能使用：" +
                attackAction.cardState.GetCardName()
            );
            return null;
        }

        BattleClashPointSnapshot pointSnapshot = CapturePointBuffSnapshot(user);

        // BattleClashPointSnapshot 在 ActionStart 前捕获。
        // 因此 ActionStart 新增的 NextCardPointUp / NextClashPointUp
        // 不影响当前卡，只保留给后续卡牌或后续正式拼点。
        // ActionStart 中对资源的修改会影响随后捕获的 ResourceSnapshot。
        TriggerActionStart(user, target, attackAction.cardState);
        // 资源快照在 ActionStart 结算后、BeforeUse 之前捕获。
        // ActionStart 中产生或减少的资源可以影响当前卡。
        // BeforeUse 中产生或减少的资源不会回头改变当前卡资源快照，
        // 只影响后续行动。卡牌设计应避免依赖 BeforeUse 修改自身资源计算。
        BattleClashResourceSnapshot resourceSnapshot = CaptureResourceSnapshot(
            user,
            attackAction.cardState,
            false
        );
        if (IsResourceUnavailableForExecution(resourceSnapshot))
        {
            failureResult = CreateActionUnavailableResult(
                "ResolveUnilateralAttack：执行资源不足，本次攻击不进入成功结算"
            );
            return null;
        }

        TriggerBattleEvent(
            BattleTiming.BeforeUse,
            user,
            target,
            attackAction.cardState,
            0,
            0,
            false,
            false
        );

        bool usesEnemyCompatibilityMetadata =
            attackAction.actionSlot == null && attackAction.enemyIntent != null;

        BattleResolutionPlan plan = new BattleResolutionPlan(
            executionItem,
            attackAction.actionSlot ?? compatibilityActionSlot,
            attackAction.enemyIntent,
            null
        );
        plan.planKind = usesEnemyCompatibilityMetadata
            ? BattleResolutionPlanKind.UnrespondedEnemyAttack
            : BattleResolutionPlanKind.FreeActionAttack;
        plan.resultType = usesEnemyCompatibilityMetadata
            ? "UnrespondedEnemyAttack"
            : "FreeAttack";
        plan.playerCardUsed = !usesEnemyCompatibilityMetadata;
        plan.enemyCardUsed = usesEnemyCompatibilityMetadata;
        plan.triggeredEventChain = true;
        plan.attacker = user;
        plan.target = target;
        plan.sourceCardState = attackAction.cardState;
        // 两组字段暂时同步写入，兼容既有Plan/Presenter读取；Combat只使用同一份快照。
        plan.freeActionPointSnapshot = pointSnapshot;
        plan.freeActionResourceSnapshot = resourceSnapshot;
        plan.unrespondedPointSnapshot = pointSnapshot;
        plan.unrespondedResourceSnapshot = resourceSnapshot;
        return plan;
    }

    internal static bool TryRollFreeAttackResolutionPlan(
        BattleResolutionPlan plan,
        out int rolledPoint
    )
    {
        if (plan == null ||
            plan.planKind != BattleResolutionPlanKind.FreeActionAttack)
        {
            rolledPoint = 0;
            return false;
        }

        return TryRollUnilateralAttackResolutionPlan(plan, out rolledPoint);
    }

    internal static bool TryRollUnilateralAttackResolutionPlan(
        BattleResolutionPlan plan,
        out int rolledPoint
    )
    {
        rolledPoint = 0;
        if (plan == null ||
            (plan.planKind != BattleResolutionPlanKind.FreeActionAttack &&
                plan.planKind != BattleResolutionPlanKind.UnrespondedEnemyAttack) ||
            plan.State != BattleResolutionPlanState.Pending ||
            plan.freeActionHasRolled || plan.impacts.Count > 0 ||
            plan.attacker == null || plan.target == null ||
            plan.sourceCardState == null ||
            plan.sourceCardState.cardData == null)
        {
            return false;
        }

        CardTestData attackCard = plan.sourceCardState.cardData;
        BattleClashPointSnapshot pointSnapshot =
            plan.freeActionPointSnapshot ?? new BattleClashPointSnapshot();
        BattleClashResourceSnapshot resourceSnapshot =
            plan.freeActionResourceSnapshot ??
            new BattleClashResourceSnapshot
            {
                cardState = plan.sourceCardState,
                selectedMinPoint = attackCard.minPoint,
                selectedMaxPoint = attackCard.maxPoint
            };
        plan.freeActionPointSnapshot = pointSnapshot;
        plan.freeActionResourceSnapshot = resourceSnapshot;
        rolledPoint = BattleCalculator.GetFinalAttackPointWithoutClash(
            plan.attacker,
            attackCard,
            pointSnapshot.nextCardPointModifier,
            resourceSnapshot.selectedMinPoint,
            resourceSnapshot.selectedMaxPoint,
            resourceSnapshot.pointModifierFromResource
        );
        int damageScaled = BattleCalculator.GetFinalDamageScaled(
            plan.attacker,
            plan.target,
            attackCard,
            rolledPoint
        );
        int finalHpDamage = BattleCalculator.ConvertScaledDamageToHPDamage(
            damageScaled
        );

        plan.freeActionPoint = rolledPoint;
        plan.unrespondedEnemyPoint = rolledPoint;
        plan.freeActionHasRolled = true;

        BattleImpact impact = new BattleImpact(
            0,
            plan.attacker,
            plan.target,
            plan.sourceCardState,
            rolledPoint,
            rolledPoint,
            ClashResult.None,
            true,
            true
        );
        // Roll时固定点数与伤害，HP仍只在Impact提交边界写入。
        impact.SetPrecalculatedDamage(finalHpDamage);
        plan.impacts.Add(impact);
        return true;
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
    // 当前支持玩家 Attack / Defense / Dodge 指定响应敌人 Attack；
    // LongRangeShoot 另支持与敌人 Defense / Dodge 进行 AttackVsAttack 式拼点。
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
        BattleClashSession session;
        BattleResolveResult beginFailure = TryBeginRespondedClash(
            actionSlot,
            enemyIntent,
            out session
        );
        if (beginFailure != null)
        {
            return beginFailure;
        }

        // 旧同步入口保持兼容：内部持续推进Session，直到可以沿用正式提交逻辑。
        while (!session.IsFinalized)
        {
            session.RollNextAttempt();
        }

        return FinalizeRespondedClash(actionSlot, enemyIntent, session);
    }

    internal static bool TryGetAttackAndDefenseActions(
        BattleExecutionInteractionContext context,
        out BattleExecutionAction attackAction,
        out BattleExecutionAction defenseAction
    )
    {
        attackAction = null;
        defenseAction = null;
        if (context == null || context.sideA == null || context.sideB == null)
        {
            return false;
        }

        BattleExecutionAction[] actions = { context.sideA, context.sideB };
        foreach (BattleExecutionAction action in actions)
        {
            CardTestData cardData = action != null && action.cardState != null
                ? action.cardState.cardData
                : null;
            if (cardData == null)
            {
                return false;
            }

            if (cardData.cardType == CardType.Attack)
            {
                if (attackAction != null)
                {
                    return false;
                }
                attackAction = action;
            }
            else if (cardData.cardType == CardType.Defense)
            {
                if (defenseAction != null)
                {
                    return false;
                }
                defenseAction = action;
            }
            else
            {
                return false;
            }
        }

        return attackAction != null && defenseAction != null;
    }

    internal static bool TryGetUnilateralAttackAction(
        BattleExecutionInteractionContext context,
        out BattleExecutionAction attackAction
    )
    {
        attackAction = null;
        if (context == null)
        {
            return false;
        }

        bool hasSideA = context.sideA != null;
        bool hasSideB = context.sideB != null;
        if (hasSideA == hasSideB)
        {
            return false;
        }

        BattleExecutionAction action = hasSideA
            ? context.sideA
            : context.sideB;
        if (!IsValidExecutionAction(action) ||
            action.cardState.cardData.cardType != CardType.Attack)
        {
            return false;
        }

        attackAction = action;
        return true;
    }

    internal static bool TryGetAttackAndDodgeActions(
        BattleExecutionInteractionContext context,
        out BattleExecutionAction attackAction,
        out BattleExecutionAction dodgeAction
    )
    {
        attackAction = null;
        dodgeAction = null;
        if (context == null || context.sideA == null || context.sideB == null)
        {
            return false;
        }

        BattleExecutionAction[] actions = { context.sideA, context.sideB };
        foreach (BattleExecutionAction action in actions)
        {
            CardTestData cardData = action != null && action.cardState != null
                ? action.cardState.cardData
                : null;
            if (cardData == null)
            {
                return false;
            }

            if (cardData.cardType == CardType.Attack)
            {
                if (attackAction != null)
                {
                    return false;
                }
                attackAction = action;
            }
            else if (cardData.cardType == CardType.Dodge)
            {
                if (dodgeAction != null)
                {
                    return false;
                }
                dodgeAction = action;
            }
            else
            {
                return false;
            }
        }

        return attackAction != null && dodgeAction != null;
    }

    static bool IsValidExecutionAction(BattleExecutionAction action)
    {
        return action != null && action.actor != null &&
            action.cardState != null && action.cardState.cardData != null;
    }

    static BattleExecutionInteractionContext CreateRespondedInteractionContext(
        BattleActionSlot actionSlot,
        BattleEnemyIntent enemyIntent
    )
    {
        BattleExecutionAction slotAction = actionSlot != null
            ? new BattleExecutionAction(
                actionSlot.actor,
                actionSlot.cardState,
                actionSlot,
                enemyIntent,
                enemyIntent != null ? enemyIntent.enemy : actionSlot.target
            )
            : null;
        BattleExecutionAction intentAction = enemyIntent != null
            ? new BattleExecutionAction(
                enemyIntent.enemy,
                enemyIntent.enemyCardState,
                null,
                enemyIntent,
                enemyIntent.actualTargetCharacter
            )
            : null;
        return new BattleExecutionInteractionContext(
            null,
            slotAction,
            intentAction
        );
    }

    internal static BattleResolveResult ResolveAttackVsDefense(
        BattleExecutionAction attackAction,
        BattleExecutionAction defenseAction
    )
    {
        BattleClashSession session;
        BattleResolveResult beginFailure = TryBeginAttackVsDefense(
            attackAction,
            defenseAction,
            out session
        );
        if (beginFailure != null)
        {
            return beginFailure;
        }

        if (!session.RollNextAttempt() || !session.IsFinalized)
        {
            return CreateInvalidResolveResult(
                "ResolveAttackVsDefense 失败：Defense Clash无法完成"
            );
        }

        return FinalizeAttackVsDefense(attackAction, defenseAction, session);
    }

    internal static BattleResolveResult TryBeginAttackVsDefense(
        BattleExecutionAction attackAction,
        BattleExecutionAction defenseAction,
        out BattleClashSession session
    )
    {
        session = null;
        if (!IsValidExecutionAction(attackAction) ||
            attackAction.cardState.cardData.cardType != CardType.Attack)
        {
            return CreateInvalidResolveResult(
                "TryBeginAttackVsDefense 失败：Attack Action无效"
            );
        }
        if (!IsValidExecutionAction(defenseAction) ||
            defenseAction.cardState.cardData.cardType != CardType.Defense)
        {
            return CreateInvalidResolveResult(
                "TryBeginAttackVsDefense 失败：Defense Action无效"
            );
        }
        if (IsInvalidPointRange(
                attackAction.cardState.cardData.minPoint,
                attackAction.cardState.cardData.maxPoint
            ) ||
            IsInvalidPointRange(
                defenseAction.cardState.cardData.minPoint,
                defenseAction.cardState.cardData.maxPoint
            ))
        {
            return CreateInvalidResolveResult(
                "TryBeginAttackVsDefense 失败：Attack或Defense点数范围异常"
            );
        }

        session = CreateAttackVsDefenseClashSession(
            attackAction,
            defenseAction
        );
        return session != null
            ? null
            : CreateInvalidResolveResult(
                "TryBeginAttackVsDefense 失败：无法建立ClashSession"
            );
    }

    internal static BattleResolveResult ResolveAttackVsDodge(
        BattleExecutionAction attackAction,
        BattleExecutionAction dodgeAction,
        bool isContinuousDodgeContinuation = false
    )
    {
        BattleResolveResult beginFailure = TryBeginAttackVsDodge(
            attackAction,
            dodgeAction,
            isContinuousDodgeContinuation,
            out BattleClashSession session
        );
        if (beginFailure != null)
        {
            return beginFailure;
        }

        if (!session.RollNextAttempt() || !session.IsFinalized)
        {
            return CreateInvalidResolveResult(
                "ResolveAttackVsDodge 失败：Dodge Clash无法完成"
            );
        }

        return FinalizeAttackVsDodge(attackAction, dodgeAction, session);
    }

    internal static BattleResolveResult TryBeginAttackVsDodge(
        BattleExecutionAction attackAction,
        BattleExecutionAction dodgeAction,
        out BattleClashSession session
    )
    {
        return TryBeginAttackVsDodge(
            attackAction,
            dodgeAction,
            false,
            out session
        );
    }

    internal static BattleResolveResult TryBeginAttackVsDodge(
        BattleExecutionAction attackAction,
        BattleExecutionAction dodgeAction,
        bool isContinuousDodgeContinuation,
        out BattleClashSession session
    )
    {
        session = null;
        if (!IsValidExecutionAction(attackAction) ||
            attackAction.cardState.cardData.cardType != CardType.Attack)
        {
            return CreateInvalidResolveResult(
                "TryBeginAttackVsDodge 失败：Attack Action无效"
            );
        }
        if (!IsValidExecutionAction(dodgeAction) ||
            dodgeAction.cardState.cardData.cardType != CardType.Dodge)
        {
            return CreateInvalidResolveResult(
                "TryBeginAttackVsDodge 失败：Dodge Action无效"
            );
        }
        if (IsInvalidPointRange(
                attackAction.cardState.cardData.minPoint,
                attackAction.cardState.cardData.maxPoint
            ) ||
            IsInvalidPointRange(
                dodgeAction.cardState.cardData.minPoint,
                dodgeAction.cardState.cardData.maxPoint
            ))
        {
            return CreateInvalidResolveResult(
                "TryBeginAttackVsDodge 失败：Attack或Dodge点数范围异常"
            );
        }

        session = CreateAttackVsDodgeClashSession(
            attackAction,
            dodgeAction,
            isContinuousDodgeContinuation
        );
        return session != null
            ? null
            : CreateInvalidResolveResult(
                "TryBeginAttackVsDodge 失败：无法建立ClashSession"
            );
    }

    // Runner与同步入口共用同一套验证和初始化，等待期间不会重复触发初始化事件。
    internal static BattleResolveResult TryBeginRespondedClash(
        BattleActionSlot actionSlot,
        BattleEnemyIntent enemyIntent,
        out BattleClashSession session
    )
    {
        session = null;
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

        if (IsResourceUnavailableForExecution(CaptureResourceSnapshot(
                actionSlot.actor,
                actionSlot.cardState
            )))
        {
            return CreateActionUnavailableResult(
                "ResolveRespondedEnemyIntent：响应卡执行资源不足，本次响应变为空卡。" +
                actionSlot.actor.characterName +
                " 的卡牌不能使用：" +
                actionSlot.cardState.GetCardName()
            );
        }

        BattleExecutionInteractionContext interactionContext =
            CreateRespondedInteractionContext(actionSlot, enemyIntent);
        if (interactionContext.effectiveInteractionType ==
                BattleInteractionType.AttackVsDefense)
        {
            if (!BattleCardManager.CanUseCard(
                    actionSlot.actor,
                    enemyIntent.enemy,
                    actionSlot.cardState
                ))
            {
                return CreateActionUnavailableResult(
                    "ResolveRespondedEnemyIntent：响应卡执行时已不可用，本次响应变为空卡。" +
                    actionSlot.actor.characterName +
                    " 的卡牌不能使用：" +
                    actionSlot.cardState.GetCardName()
                );
            }

            if (!TryGetAttackAndDefenseActions(
                    interactionContext,
                    out BattleExecutionAction attackAction,
                    out BattleExecutionAction defenseAction
                ))
            {
                return CreateInvalidResolveResult(
                    "ResolveRespondedEnemyIntent 失败：无法归一化AttackVsDefense Action"
                );
            }

            return TryBeginAttackVsDefense(
                attackAction,
                defenseAction,
                out session
            );
        }

        if (interactionContext.effectiveInteractionType ==
                BattleInteractionType.AttackVsDodge)
        {
            if (!BattleCardManager.CanUseCard(
                    actionSlot.actor,
                    enemyIntent.enemy,
                    actionSlot.cardState
                ))
            {
                return CreateActionUnavailableResult(
                    "ResolveRespondedEnemyIntent：响应卡执行时已不可用，本次响应变为空卡。" +
                    actionSlot.actor.characterName +
                    " 的卡牌不能使用：" +
                    actionSlot.cardState.GetCardName()
                );
            }

            if (!TryGetAttackAndDodgeActions(
                    interactionContext,
                    out BattleExecutionAction attackAction,
                    out BattleExecutionAction dodgeAction
                ))
            {
                return CreateInvalidResolveResult(
                    "ResolveRespondedEnemyIntent 失败：无法归一化AttackVsDodge Action"
                );
            }

            return TryBeginAttackVsDodge(
                attackAction,
                dodgeAction,
                false,
                out session
            );
        }

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

            session = CreateRespondedAttackClashSession(actionSlot, enemyIntent);
            return null;
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

            session = CreateRespondedDodgeClashSession(actionSlot, enemyIntent);
            return null;
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

            session = CreateRespondedDefenseClashSession(actionSlot, enemyIntent);
            return null;
        }

        return CreateUnsupportedResolveResult(
            "ResolveRespondedEnemyIntent 暂不支持该卡牌对抗类型：玩家 " +
            playerCard.cardType +
            " / 敌人 " +
            enemyCard.cardType
        );
    }

    // Shoot响应Melee时，在ClashSession创建前锁定本次执行的资源状态。
    internal static bool TryCaptureShootResponseResourceSnapshot(
        BattleActionSlot actionSlot,
        BattleEnemyIntent enemyIntent,
        out BattleClashResourceSnapshot resourceSnapshot
    )
    {
        resourceSnapshot = null;
        if (actionSlot == null || actionSlot.actor == null ||
            actionSlot.cardState == null ||
            (!actionSlot.cardState.IsLongRangeShoot() &&
                !actionSlot.cardState.IsCloseRangeShoot()) ||
            enemyIntent == null || enemyIntent.enemy == null ||
            enemyIntent.enemyCardState == null ||
            !enemyIntent.enemyCardState.IsMeleeAttack() ||
            enemyIntent.actualTargetCharacter == null ||
            IsInvalidPointRange(
                actionSlot.cardState.cardData.minPoint,
                actionSlot.cardState.cardData.maxPoint
            ) ||
            IsInvalidPointRange(
                enemyIntent.enemyCardState.cardData.minPoint,
                enemyIntent.enemyCardState.cardData.maxPoint
            ) ||
            !BattleCardManager.CanUseCard(
                actionSlot.actor,
                enemyIntent.enemy,
                actionSlot.cardState
            ))
        {
            return false;
        }

        resourceSnapshot = CaptureResourceSnapshot(
            actionSlot.actor,
            actionSlot.cardState
        );
        return true;
    }

    internal static bool IsResourceUnavailableForExecution(
        BattleClashResourceSnapshot resourceSnapshot
    )
    {
        return resourceSnapshot != null &&
            resourceSnapshot.hasRule &&
            !resourceSnapshot.normalVersionEnabled &&
            GetInsufficientBehavior(resourceSnapshot) ==
                CardResourceInsufficientBehavior.ActionUnavailable;
    }

    // 同步入口也走同一套Plan提交路径，避免Pausable与旧API形成两套结算规则。
    internal static BattleResolveResult FinalizeRespondedClash(
        BattleActionSlot actionSlot,
        BattleEnemyIntent enemyIntent,
        BattleClashSession session
    )
    {
        if (session == null)
        {
            return CreateInvalidResolveResult("FinalizeRespondedClash 失败：ClashSession为空");
        }

        if (!session.IsFinalized)
        {
            return CreateInvalidResolveResult("FinalizeRespondedClash 失败：ClashSession尚未完成");
        }

        BattleResolutionPlan plan = BuildRespondedClashResolutionPlan(
            actionSlot,
            enemyIntent,
            session
        );
        if (plan == null)
        {
            return CreateUnsupportedResolveResult(
                "FinalizeRespondedClash 暂不支持Clash类型：" + session.ClashType
            );
        }

        return CommitResolutionSynchronously(plan);
    }

    // Calculate阶段只解释Finalized Session并建立计划，不触发任何post-clash mutation。
    internal static BattleResolutionPlan BuildRespondedClashResolutionPlan(
        BattleActionSlot actionSlot,
        BattleEnemyIntent enemyIntent,
        BattleClashSession session,
        BattleExecutionItem executionItem = null
    )
    {
        if (actionSlot == null || enemyIntent == null || session == null ||
            !session.IsFinalized || session.SideA == null || session.SideB == null)
        {
            return null;
        }

        BattleResolutionPlan plan = new BattleResolutionPlan(
            executionItem,
            actionSlot,
            enemyIntent,
            session
        );

        if (session.ClashType == BattleClashType.AttackVsAttack)
        {
            BuildAttackResolutionPlan(plan);
            return plan;
        }

        if (session.ClashType == BattleClashType.DefenseVsAttack)
        {
            BattleExecutionInteractionContext context =
                CreateRespondedInteractionContext(actionSlot, enemyIntent);
            if (!TryGetAttackAndDefenseActions(
                    context,
                    out BattleExecutionAction attackAction,
                    out BattleExecutionAction defenseAction
                ))
            {
                return null;
            }

            return BuildAttackVsDefenseResolutionPlan(
                attackAction,
                defenseAction,
                session,
                executionItem
            );
        }

        if (session.ClashType == BattleClashType.DodgeVsAttack)
        {
            BattleExecutionInteractionContext context =
                CreateRespondedInteractionContext(actionSlot, enemyIntent);
            if (!TryGetAttackAndDodgeActions(
                    context,
                    out BattleExecutionAction attackAction,
                    out BattleExecutionAction dodgeAction
                ))
            {
                return null;
            }

            return BuildAttackVsDodgeResolutionPlan(
                attackAction,
                dodgeAction,
                session,
                executionItem
            );
        }

        return null;
    }

    internal static BattleResolveResult FinalizeAttackVsDefense(
        BattleExecutionAction attackAction,
        BattleExecutionAction defenseAction,
        BattleClashSession session
    )
    {
        if (session == null || !session.IsFinalized)
        {
            return CreateInvalidResolveResult(
                "FinalizeAttackVsDefense 失败：ClashSession尚未完成"
            );
        }

        BattleResolutionPlan plan = BuildAttackVsDefenseResolutionPlan(
            attackAction,
            defenseAction,
            session
        );
        return plan != null
            ? CommitResolutionSynchronously(plan)
            : CreateInvalidResolveResult(
                "FinalizeAttackVsDefense 失败：无法建立ResolutionPlan"
            );
    }

    internal static BattleResolutionPlan BuildAttackVsDefenseResolutionPlan(
        BattleExecutionAction attackAction,
        BattleExecutionAction defenseAction,
        BattleClashSession session,
        BattleExecutionItem executionItem = null
    )
    {
        if (!IsValidExecutionAction(attackAction) ||
            !IsValidExecutionAction(defenseAction) ||
            session == null || !session.IsFinalized ||
            session.ClashType != BattleClashType.DefenseVsAttack ||
            session.SideA == null || session.SideB == null ||
            !object.ReferenceEquals(
                session.SideA.cardState,
                defenseAction.cardState
            ) ||
            !object.ReferenceEquals(
                session.SideB.cardState,
                attackAction.cardState
            ))
        {
            return null;
        }

        BattleActionSlot actionSlot = attackAction.actionSlot != null
            ? attackAction.actionSlot
            : defenseAction.actionSlot;
        BattleEnemyIntent enemyIntent = attackAction.enemyIntent != null
            ? attackAction.enemyIntent
            : defenseAction.enemyIntent;
        BattleResolutionPlan plan = new BattleResolutionPlan(
            executionItem,
            actionSlot,
            enemyIntent,
            session
        );
        BuildDefenseResolutionPlan(plan);
        return plan;
    }

    internal static BattleResolveResult FinalizeAttackVsDodge(
        BattleExecutionAction attackAction,
        BattleExecutionAction dodgeAction,
        BattleClashSession session
    )
    {
        if (session == null || !session.IsFinalized)
        {
            return CreateInvalidResolveResult(
                "FinalizeAttackVsDodge 失败：ClashSession尚未完成"
            );
        }

        BattleResolutionPlan plan = BuildAttackVsDodgeResolutionPlan(
            attackAction,
            dodgeAction,
            session
        );
        return plan != null
            ? CommitResolutionSynchronously(plan)
            : CreateInvalidResolveResult(
                "FinalizeAttackVsDodge 失败：无法建立ResolutionPlan"
            );
    }

    internal static BattleResolutionPlan BuildAttackVsDodgeResolutionPlan(
        BattleExecutionAction attackAction,
        BattleExecutionAction dodgeAction,
        BattleClashSession session,
        BattleExecutionItem executionItem = null
    )
    {
        if (!IsValidExecutionAction(attackAction) ||
            !IsValidExecutionAction(dodgeAction) ||
            session == null || !session.IsFinalized ||
            session.ClashType != BattleClashType.DodgeVsAttack ||
            session.SideA == null || session.SideB == null ||
            !object.ReferenceEquals(
                session.SideA.cardState,
                dodgeAction.cardState
            ) ||
            !object.ReferenceEquals(
                session.SideB.cardState,
                attackAction.cardState
            ))
        {
            return null;
        }

        BattleActionSlot actionSlot = attackAction.actionSlot != null
            ? attackAction.actionSlot
            : dodgeAction.actionSlot;
        BattleEnemyIntent enemyIntent = attackAction.enemyIntent != null
            ? attackAction.enemyIntent
            : dodgeAction.enemyIntent;
        BattleResolutionPlan plan = new BattleResolutionPlan(
            executionItem,
            actionSlot,
            enemyIntent,
            session
        );
        BuildDodgeResolutionPlan(plan);
        return plan;
    }

    static void BuildAttackResolutionPlan(BattleResolutionPlan plan)
    {
        BattleClashSession session = plan.clashSession;
        if (session.FinalResult == BattleClashFinalResult.TieLimit)
        {
            plan.resultType = "TieLimit";
            return;
        }

        bool playerWon = session.FinalResult == BattleClashFinalResult.SideAWin;
        plan.resultType = playerWon ? "PlayerWin" : "EnemyWin";
        plan.playerCardUsed = playerWon;
        plan.enemyCardUsed = !playerWon;
        plan.triggeredEventChain = true;
        plan.attacker = playerWon ? session.SideA.actor : session.SideB.actor;
        plan.target = playerWon
            ? session.SideB.actor
            : session.ActualTarget;
        plan.sourceCardState = playerWon
            ? session.SideA.cardState
            : session.SideB.cardState;
        int winnerPoint = playerWon
            ? session.SideADamagePoint
            : session.SideBDamagePoint;
        bool winnerIsAttack = plan.sourceCardState != null &&
            plan.sourceCardState.cardData != null &&
            plan.sourceCardState.cardData.cardType == CardType.Attack;
        if (winnerIsAttack)
        {
            BattleImpact impact = new BattleImpact(
                0,
                plan.attacker,
                plan.target,
                plan.sourceCardState,
                winnerPoint,
                winnerPoint,
                ClashResult.Win,
                winnerIsAttack,
                winnerIsAttack
            );
            BattleClashSideState loser = playerWon
                ? session.SideB
                : session.SideA;
            if (loser.cardState != null &&
                loser.cardState.HasTrait(BattleCardTrait.IaiAnger))
            {
                impact.damageMultiplierPercent = 150;
            }
            plan.impacts.Add(impact);
        }
    }

    static void BuildDefenseResolutionPlan(BattleResolutionPlan plan)
    {
        BattleClashSession session = plan.clashSession;
        plan.resultType = session.IsFullBlock
            ? "DefenseFullBlock"
            : "DefenseReducedDamage";
        plan.playerCardUsed = true;
        plan.enemyCardUsed = !session.UsesKnownSideBPoint;
        plan.triggeredEventChain = true;
        plan.attacker = session.SideB.actor;
        plan.target = session.ActualTarget;
        plan.sourceCardState = session.SideB.cardState;
        plan.guardUpStackToConsume = GetTriggeredBuffStack(
            session.SideA.actor,
            BattleTiming.ClashStart,
            BuffGuardUp
        );
        plan.guardDownStackToConsume = GetTriggeredBuffStack(
            session.SideA.actor,
            BattleTiming.ClashStart,
            BuffGuardDown
        );
        BattleImpact impact = new BattleImpact(
            0,
            plan.attacker,
            plan.target,
            plan.sourceCardState,
            session.RemainingAttackPoint,
            session.RemainingAttackPoint,
            ClashResult.None,
            !session.IsFullBlock,
            true
        );
        plan.impacts.Add(impact);
    }

    static void BuildDodgeResolutionPlan(BattleResolutionPlan plan)
    {
        BattleClashSession session = plan.clashSession;
        bool success = session.FinalResult == BattleClashFinalResult.DodgeSuccess;
        bool playerActionIsDodge = plan.actionSlot != null &&
            object.ReferenceEquals(
                plan.actionSlot.cardState,
                session.SideA.cardState
            );
        plan.resultType = success ? "DodgeSuccess" : "DodgeFailed";
        plan.triggeredEventChain = true;

        if (session.UsesKnownSideBPoint)
        {
            plan.playerCardUsed = true;
            plan.enemyCardUsed = false;
        }
        else if (!playerActionIsDodge)
        {
            // 反向配对时玩家是Attack；双方卡牌都按普通单次行动完成。
            plan.playerCardUsed = true;
            plan.enemyCardUsed = true;
        }
        else
        {
            plan.playerCardUsed = !success;
            plan.enemyCardUsed = true;
            plan.playerCardParticipated = true;
            plan.playerCardUseDisposition = success
                ? BattleCardUseDisposition.DeferForContinuousDodge
                : BattleCardUseDisposition.FinalizeImmediately;
        }

        if (success)
        {
            return;
        }

        plan.attacker = session.SideB.actor;
        plan.target = session.ActualTarget;
        plan.sourceCardState = session.SideB.cardState;
        BattleImpact impact = new BattleImpact(
            0,
            plan.attacker,
            plan.target,
            plan.sourceCardState,
            session.SideBDamagePoint,
            session.SideBPoint,
            ClashResult.Win,
            true,
            true
        );
        if (session.SideA.cardState != null &&
            session.SideA.cardState.HasTrait(
                BattleCardTrait.ReloadBulletOnDodgeResolution))
        {
            impact.damageMultiplierPercent = 150;
        }
        plan.impacts.Add(impact);
    }

    internal static bool TryCommitNextResolutionStep(
        BattleResolutionPlan plan,
        out BattleResolveResult completedResult
    )
    {
        completedResult = null;
        if (plan == null)
        {
            return false;
        }

        if (plan.State == BattleResolutionPlanState.Completed)
        {
            completedResult = plan.CompletedResult;
            return true;
        }

        if (plan.State == BattleResolutionPlanState.Pending &&
            !ActivateResolution(plan))
        {
            return false;
        }

        BattleImpact pendingImpact = plan.GetNextPendingImpact();
        if (pendingImpact != null && !CommitImpact(plan, pendingImpact))
        {
            return false;
        }

        if (plan.HasPendingImpact())
        {
            return true;
        }

        completedResult = CompleteResolution(plan);
        return completedResult != null;
    }

    internal static bool CommitImpact(
        BattleResolutionPlan plan,
        BattleImpact impact
    )
    {
        if (plan == null || impact == null)
        {
            return false;
        }
        if (impact.state != BattleImpactState.Pending)
        {
            return true;
        }
        if (plan.State == BattleResolutionPlanState.Pending &&
            !ActivateResolution(plan))
        {
            return false;
        }
        if (plan.State != BattleResolutionPlanState.Activated)
        {
            return plan.State == BattleResolutionPlanState.Completed;
        }

        if (impact.attacker == null || impact.target == null ||
            impact.sourceCardState == null ||
            impact.sourceCardState.cardData == null)
        {
            impact.state = BattleImpactState.Skipped;
            return true;
        }

        bool wasDeadBeforeImpact = impact.target.IsDead();
        if (impact.allowsDamage && wasDeadBeforeImpact)
        {
            impact.state = BattleImpactState.Skipped;
            return true;
        }

        int hpDamage = impact.usesPrecalculatedDamage
            ? Mathf.Max(0, impact.precalculatedDamage)
            : 0;
        if (impact.allowsDamage && !impact.usesPrecalculatedDamage)
        {
            int damageScaled = BattleCalculator.GetFinalDamageScaled(
                impact.attacker,
                impact.target,
                impact.sourceCardState.cardData,
                impact.basePower
            );
            damageScaled = damageScaled *
                Mathf.Max(0, impact.damageMultiplierPercent) / 100;
            hpDamage = BattleCalculator.ConvertScaledDamageToHPDamage(damageScaled);
        }

        if (impact.shouldTriggerHit)
        {
            TriggerBattleEvent(
                BattleTiming.Hit,
                impact.attacker,
                impact.target,
                impact.sourceCardState,
                impact.clashPoint,
                hpDamage,
                true,
                false,
                impact.clashResult
            );
        }

        if (impact.allowsDamage && hpDamage > 0)
        {
            int hpBefore = impact.target.currentHP;
            int preHitAnger = BattleAngerRules.GetAnger(impact.target);
            impact.target.TakeDamage(hpDamage);
            int actualDamage = Mathf.Max(0, hpBefore - impact.target.currentHP);
            BattleAngerRules.ApplyCommittedDamage(
                impact.attacker,
                impact.target,
                preHitAnger,
                actualDamage
            );
            bool didKill = !wasDeadBeforeImpact && impact.target.IsDead();
            TriggerBattleEvent(
                BattleTiming.AfterDamage,
                impact.attacker,
                impact.target,
                impact.sourceCardState,
                impact.clashPoint,
                hpDamage,
                true,
                didKill
            );
            if (didKill)
            {
                TriggerBattleEvent(
                    BattleTiming.AfterKill,
                    impact.attacker,
                    impact.target,
                    impact.sourceCardState,
                    impact.clashPoint,
                    hpDamage,
                    true,
                    true
                );
            }
            impact.didKill = didKill;
        }

        impact.committedDamage = hpDamage;
        impact.state = BattleImpactState.Committed;
        return true;
    }

    static bool ActivateResolution(BattleResolutionPlan plan)
    {
        if (plan == null)
        {
            return false;
        }
        if (plan.State != BattleResolutionPlanState.Pending)
        {
            return true;
        }

        if (plan.planKind == BattleResolutionPlanKind.UnrespondedEnemyAttack ||
            plan.planKind == BattleResolutionPlanKind.FreeActionAttack)
        {
            if (!plan.freeActionHasRolled)
            {
                return false;
            }

            plan.State = BattleResolutionPlanState.Activated;
            ActivateUnilateralAttackResolution(plan);
            return true;
        }

        BattleClashSession session = plan.clashSession;
        if (session == null || !session.IsFinalized)
        {
            return false;
        }

        // 先锁定状态，确保事件回调或重复入口不会再次消费同一批结算数据。
        plan.State = BattleResolutionPlanState.Activated;

        if (session.ClashType == BattleClashType.AttackVsAttack)
        {
            ActivateAttackResolution(plan);
        }
        else if (session.ClashType == BattleClashType.DefenseVsAttack)
        {
            ActivateDefenseResolution(plan);
        }
        else if (session.ClashType == BattleClashType.DodgeVsAttack)
        {
            ActivateDodgeResolution(plan);
        }
        else
        {
            return false;
        }

        return true;
    }

    static void ActivateUnilateralAttackResolution(
        BattleResolutionPlan plan
    )
    {
        ConsumeSuccessfulPointCardBuffs(
            plan.attacker,
            plan.freeActionPointSnapshot
        );
        PayDefaultResourceCostOnSuccessfulUse(
            plan.attacker,
            plan.freeActionResourceSnapshot
        );
        PayResolvedParticipationResourceCost(
            plan.attacker,
            plan.freeActionResourceSnapshot
        );
        TriggerBattleEvent(
            BattleTiming.Resolved,
            plan.attacker,
            plan.target,
            plan.sourceCardState,
            plan.freeActionPoint,
            0,
            false,
            false
        );
    }

    static void ActivateAttackResolution(BattleResolutionPlan plan)
    {
        BattleClashSession session = plan.clashSession;
        if (session.FinalResult == BattleClashFinalResult.TieLimit)
        {
            return;
        }

        ConsumeClashPointBuffs(session.SideA.actor, session.SideA.pointSnapshot);
        ConsumeClashPointBuffs(session.SideB.actor, session.SideB.pointSnapshot);

        bool playerWon = session.FinalResult == BattleClashFinalResult.SideAWin;
        BattleClashSideState winner = playerWon ? session.SideA : session.SideB;
        BattleClashSideState loser = playerWon ? session.SideB : session.SideA;
        CharacterData defender = playerWon ? session.SideB.actor : session.ActualTarget;
        int winnerPoint = playerWon ? session.SideAPoint : session.SideBPoint;
        int loserPoint = playerWon ? session.SideBPoint : session.SideAPoint;

        ConsumeSuccessfulPointCardBuffs(winner.actor, winner.pointSnapshot);
        PayDefaultResourceCostOnSuccessfulUse(winner.actor, winner.resourceSnapshot);
        PayResolvedParticipationResourceCost(winner.actor, winner.resourceSnapshot);
        // LongRangeShoot无论拼点胜负都代表实际开火；只有资源支付随终局发生，
        // 不因此改变胜负卡牌、Damage或事件归属。
        PayLongRangeShootResourceOnTerminalUse(loser);
        TriggerBattleEvent(BattleTiming.ClashWin, winner.actor, defender,
            winner.cardState, winnerPoint, 0, false, false, ClashResult.Win);
        TriggerBattleEvent(BattleTiming.ClashLose, loser.actor, winner.actor,
            loser.cardState, loserPoint, 0, false, false, ClashResult.Lose);
        TriggerBattleEvent(BattleTiming.Resolved, winner.actor, defender,
            winner.cardState, winnerPoint, 0, false, false, ClashResult.Win);
    }

    static void ActivateDefenseResolution(BattleResolutionPlan plan)
    {
        BattleClashSession session = plan.clashSession;
        CharacterData player = session.SideA.actor;
        player.ConsumeTriggeredBuffStack(
            BattleTiming.ClashStart,
            BuffGuardUp,
            plan.guardUpStackToConsume
        );
        player.ConsumeTriggeredBuffStack(
            BattleTiming.ClashStart,
            BuffGuardDown,
            plan.guardDownStackToConsume
        );
        ConsumeSuccessfulPointCardBuffs(player, session.SideA.pointSnapshot);
        PayDefaultResourceCostOnSuccessfulUse(player, session.SideA.resourceSnapshot);
        PayResolvedParticipationResourceCost(
            player,
            session.SideA.resourceSnapshot
        );

        if (!session.UsesKnownSideBPoint)
        {
            ConsumeSuccessfulPointCardBuffs(
                session.SideB.actor,
                session.SideB.pointSnapshot
            );
            PayDefaultResourceCostOnSuccessfulUse(
                session.SideB.actor,
                session.SideB.resourceSnapshot
            );
            PayResolvedParticipationResourceCost(
                session.SideB.actor,
                session.SideB.resourceSnapshot
            );
            TriggerBattleEvent(BattleTiming.Resolved, session.SideB.actor,
                session.ActualTarget, session.SideB.cardState,
                session.SideBPoint, 0, false, false);
        }

        TriggerBattleEvent(BattleTiming.Resolved, player, session.SideB.actor,
            session.SideA.cardState, session.SideAPoint, 0, false, false);
    }

    static void ActivateDodgeResolution(BattleResolutionPlan plan)
    {
        BattleClashSession session = plan.clashSession;
        bool success = session.FinalResult == BattleClashFinalResult.DodgeSuccess;

        if (!session.IsContinuousDodgeContinuation)
        {
            ConsumeClashPointBuffs(session.SideA.actor, session.SideA.pointSnapshot);
            ConsumeSuccessfulPointCardBuffs(session.SideA.actor, session.SideA.pointSnapshot);
            PayDefaultResourceCostOnSuccessfulUse(
                session.SideA.actor,
                session.SideA.resourceSnapshot
            );
            PayResolvedParticipationResourceCost(
                session.SideA.actor,
                session.SideA.resourceSnapshot
            );
        }

        if (session.UsesKnownSideBPoint)
        {
            TriggerBattleEvent(
                success ? BattleTiming.ClashWin : BattleTiming.ClashLose,
                session.SideA.actor,
                session.SideB.actor,
                session.SideA.cardState,
                session.SideAPoint,
                0,
                false,
                false,
                success ? ClashResult.Win : ClashResult.Lose
            );
            TriggerBattleEvent(BattleTiming.Resolved, session.SideA.actor,
                session.SideB.actor, session.SideA.cardState,
                session.SideAPoint, 0, false, false,
                success ? ClashResult.Win : ClashResult.Lose);
            return;
        }

        ConsumeClashPointBuffs(session.SideB.actor, session.SideB.pointSnapshot);
        ConsumeSuccessfulPointCardBuffs(session.SideB.actor, session.SideB.pointSnapshot);
        PayDefaultResourceCostOnSuccessfulUse(
            session.SideB.actor,
            session.SideB.resourceSnapshot
        );
        PayResolvedParticipationResourceCost(
            session.SideB.actor,
            session.SideB.resourceSnapshot
        );

        CharacterData winner = success ? session.SideA.actor : session.SideB.actor;
        CharacterData loser = success ? session.SideB.actor : session.SideA.actor;
        BattleCardState winnerCard = success
            ? session.SideA.cardState
            : session.SideB.cardState;
        BattleCardState loserCard = success
            ? session.SideB.cardState
            : session.SideA.cardState;
        int winnerPoint = success ? session.SideAPoint : session.SideBPoint;
        int loserPoint = success ? session.SideBPoint : session.SideAPoint;
        CharacterData winnerTarget = success ? session.SideB.actor : session.ActualTarget;

        TriggerBattleEvent(BattleTiming.ClashWin, winner, winnerTarget,
            winnerCard, winnerPoint, 0, false, false, ClashResult.Win);
        TriggerBattleEvent(BattleTiming.ClashLose, loser, winner,
            loserCard, loserPoint, 0, false, false, ClashResult.Lose);
        TriggerBattleEvent(BattleTiming.Resolved, session.SideB.actor,
            success ? session.SideA.actor : session.ActualTarget,
            session.SideB.cardState, session.SideBPoint, 0, false, false,
            success ? ClashResult.Lose : ClashResult.Win);
        bool deferDodgeResolution = success &&
            plan.playerCardUseDisposition ==
                BattleCardUseDisposition.DeferForContinuousDodge;
        if (!deferDodgeResolution)
        {
            TriggerBattleEvent(BattleTiming.Resolved, session.SideA.actor,
                session.SideB.actor, session.SideA.cardState,
                session.SideAPoint, 0, false, false,
                success ? ClashResult.Win : ClashResult.Lose);
        }
    }

    static BattleResolveResult CompleteResolution(BattleResolutionPlan plan)
    {
        if (plan == null)
        {
            return null;
        }
        if (plan.State == BattleResolutionPlanState.Completed)
        {
            return plan.CompletedResult;
        }
        if (plan.State != BattleResolutionPlanState.Activated ||
            plan.HasPendingImpact())
        {
            return null;
        }

        int totalDamage = 0;
        CharacterData damagedCharacter = null;
        foreach (BattleImpact impact in plan.impacts)
        {
            if (impact == null || impact.committedDamage <= 0)
            {
                continue;
            }
            totalDamage += impact.committedDamage;
            damagedCharacter = impact.target;
        }

        BattleClashSession session = plan.clashSession;
        FinalizeCardInteractionRules(plan);
        BattleResolveResult result = new BattleResolveResult
        {
            isSuccess = true,
            shouldCompleteItem = true,
            playerCardUsed = plan.playerCardUsed,
            enemyCardUsed = plan.enemyCardUsed,
            playerCardParticipated = plan.playerCardParticipated,
            playerCardUseDisposition = plan.playerCardUseDisposition,
            hasDamage = totalDamage > 0,
            damage = totalDamage,
            damagedCharacter = plan.planKind ==
                    BattleResolutionPlanKind.UnrespondedEnemyAttack ||
                plan.planKind == BattleResolutionPlanKind.FreeActionAttack
                ? plan.target
                : damagedCharacter,
            resultType = plan.resultType,
            triggeredEventChain = plan.triggeredEventChain
        };
        if (plan.planKind == BattleResolutionPlanKind.UnrespondedEnemyAttack)
        {
            result.playerPoint = 0;
            result.enemyPoint = plan.unrespondedEnemyPoint;
            result.clashAttemptCount = 0;
            result.isTieLimitReached = false;
        }
        else if (plan.planKind == BattleResolutionPlanKind.FreeActionAttack)
        {
            result.playerPoint = plan.freeActionPoint;
            result.enemyPoint = 0;
            result.clashAttemptCount = 0;
            result.isTieLimitReached = false;
        }
        else
        {
            if (session.ClashType == BattleClashType.DefenseVsAttack)
            {
                bool playerActionIsDefense = plan.actionSlot != null &&
                    object.ReferenceEquals(
                        plan.actionSlot.cardState,
                        session.SideA.cardState
                    );
                result.playerPoint = playerActionIsDefense
                    ? session.SideAPoint
                    : session.SideBPoint;
                result.enemyPoint = playerActionIsDefense
                    ? session.SideBPoint
                    : session.SideAPoint;
            }
            else if (session.ClashType == BattleClashType.DodgeVsAttack)
            {
                bool playerActionIsDodge = plan.actionSlot != null &&
                    object.ReferenceEquals(
                        plan.actionSlot.cardState,
                        session.SideA.cardState
                    );
                result.playerPoint = playerActionIsDodge
                    ? session.SideAPoint
                    : session.SideBPoint;
                result.enemyPoint = playerActionIsDodge
                    ? session.SideBPoint
                    : session.SideAPoint;
            }
            else
            {
                result.playerPoint = session.SideAPoint;
                result.enemyPoint = session.SideBPoint;
            }
            result.clashAttemptCount =
                session.ClashType == BattleClashType.DefenseVsAttack
                    ? 0
                    : session.AttemptIndex;
            result.isTieLimitReached =
                session.FinalResult == BattleClashFinalResult.TieLimit;
        }
        result.message = BuildResolutionMessage(plan, result);

        plan.CompletedResult = result;
        plan.State = BattleResolutionPlanState.Completed;
        Debug.Log(result.message);
        return result;
    }

    static void FinalizeCardInteractionRules(BattleResolutionPlan plan)
    {
        if (plan == null)
        {
            return;
        }

        if (plan.clashSession != null)
        {
            if (plan.clashSession.FinalResult != BattleClashFinalResult.TieLimit)
            {
                BattleKnifeCardRules.FinalizeCompletedInteraction(
                    plan.clashSession.SideA.cardState
                );
                BattleKnifeCardRules.FinalizeCompletedInteraction(
                    plan.clashSession.SideB.cardState
                );
            }
            if (plan.clashSession.ClashType == BattleClashType.DodgeVsAttack &&
                plan.clashSession.FinalResult == BattleClashFinalResult.DodgeSuccess &&
                plan.clashSession.SideA.cardState.HasTrait(
                    BattleCardTrait.GrantNextClashPointUpOnSuccessfulDodge))
            {
                plan.clashSession.SideA.actor.AddBuff(
                    BuffNextClashPointUp,
                    2,
                    1
                );
            }
            if (plan.clashSession.ClashType == BattleClashType.DodgeVsAttack)
            {
                BattleClashSideState dodgeSide = plan.clashSession.SideA;
                if (dodgeSide.cardState != null &&
                    dodgeSide.cardState.HasTrait(
                        BattleCardTrait.GrantBulletOnSuccessfulDodge) &&
                    plan.clashSession.FinalResult ==
                        BattleClashFinalResult.DodgeSuccess)
                {
                    BattleBulletRules.AddBulletCapped(dodgeSide.actor, 1);
                }

                if (dodgeSide.cardState != null &&
                    dodgeSide.cardState.HasTrait(
                        BattleCardTrait.ReloadBulletOnDodgeResolution))
                {
                    BattleBulletRules.ReloadToCapacity(dodgeSide.actor);
                }
            }
            return;
        }

        BattleKnifeCardRules.FinalizeCompletedInteraction(plan.sourceCardState);
    }

    static string BuildResolutionMessage(
        BattleResolutionPlan plan,
        BattleResolveResult result
    )
    {
        BattleClashSession session = plan.clashSession;
        if (plan.planKind == BattleResolutionPlanKind.UnrespondedEnemyAttack)
        {
            BattleEnemyIntent enemyIntent = plan.enemyIntent;
            CardTestData enemyCard = plan.sourceCardState != null
                ? plan.sourceCardState.cardData
                : null;
            return "ResolveUnrespondedEnemyIntent 完成：敌人意图" +
                (enemyIntent != null ? enemyIntent.intentOrder : 0) +
                " 使用 " +
                (enemyCard != null ? enemyCard.cardName : "未知卡牌") +
                " 命中 " +
                (plan.target != null ? plan.target.characterName : "未知目标") +
                " 槽位" +
                (enemyIntent != null ? enemyIntent.actualTargetSlotIndex : 0) +
                "，敌人攻击点数 " +
                result.enemyPoint +
                "，造成伤害 " +
                result.damage +
                "。已触发 Resolved / Hit，并按实际伤害触发 AfterDamage / AfterKill";
        }

        if (plan.planKind == BattleResolutionPlanKind.FreeActionAttack)
        {
            return "ResolveFreeAction 完成：Attack FreeAction 使用 " +
                (plan.sourceCardState != null
                    ? plan.sourceCardState.GetCardName()
                    : "未知卡牌") +
                " 命中 " +
                (plan.target != null
                    ? plan.target.characterName
                    : "未知目标") +
                "，玩家攻击点数 " +
                result.playerPoint +
                "，造成伤害 " +
                result.damage +
                "。不触发 ClashWin / ClashLose";
        }

        if (result.resultType == "TieLimit")
        {
            return "ResolveRespondedEnemyIntent 连续拼点 " +
                session.AttackTieCount +
                " 次仍未分出胜负，自动结束，双方不造成伤害，双方卡牌不算成功使用";
        }

        if (session.ClashType == BattleClashType.DefenseVsAttack)
        {
            string prefix = session.UsesKnownSideBPoint
                ? "ResolveDefenseVsAttackWithKnownEnemyPoint"
                : "ResolveAttackVsDefense";
            string message = prefix +
                " 完成：" +
                result.resultType +
                "，最终攻击点数 " +
                session.SideBPoint +
                "，最终防御点数 " +
                session.SideAPoint +
                "，剩余攻击点数 " +
                session.RemainingAttackPoint +
                "，最终 HP 伤害 " +
                result.damage;
            if (session.UsesKnownSideBPoint)
            {
                message += "。使用已确定敌人点数，未重新 Roll";
            }
            return message;
        }

        if (session.ClashType == BattleClashType.DodgeVsAttack)
        {
            string prefix = session.UsesKnownSideBPoint
                ? "ResolveDodgeVsAttackWithKnownEnemyPoint"
                : "ResolveAttackVsDodge";
            string message = prefix +
                " 完成：" +
                result.resultType +
                "，最终 Dodge 点数 " +
                session.SideAPoint +
                "，最终 Attack 点数 " +
                session.SideBPoint;
            if (result.resultType == "DodgeSuccess")
            {
                message += "。闪避成功，不触发 Hit / AfterDamage / AfterKill";
            }
            else
            {
                message += "，最终 HP 伤害 " + result.damage;
            }
            if (session.UsesKnownSideBPoint)
            {
                message += "。使用已确定敌人点数，未重新 Roll";
            }
            return message;
        }

        return "ResolveRespondedEnemyIntent 完成：" +
            result.resultType +
            "，玩家点数 " +
            result.playerPoint +
            "，敌人点数 " +
            result.enemyPoint +
            "，造成伤害 " +
            result.damage;
    }

    static BattleResolveResult CommitResolutionSynchronously(
        BattleResolutionPlan plan
    )
    {
        BattleResolveResult result = null;
        while (plan != null && plan.State != BattleResolutionPlanState.Completed)
        {
            if (!TryCommitNextResolutionStep(plan, out result))
            {
                return CreateInvalidResolveResult("BattleResolutionPlan 同步提交失败");
            }
        }

        return result ?? (plan != null ? plan.CompletedResult : null);
    }

    static int GetTriggeredBuffStack(
        CharacterData unit,
        string timing,
        string buffID
    )
    {
        if (unit == null || unit.buffs == null)
        {
            return 0;
        }

        int stack = 0;
        foreach (BuffData buff in unit.buffs)
        {
            if (buff != null &&
                buff.buffID == buffID &&
                buff.checkTiming == timing &&
                buff.expireRule == "ConsumeOnTrigger")
            {
                stack += buff.stack;
            }
        }

        return stack;
    }

    // ResolveUnrespondedEnemyIntent = 正式结算无人响应敌人意图
    // 第一版只处理敌人攻击命中 actualTarget，不触发玩家卡牌式事件链。
    public static BattleResolveResult ResolveUnrespondedEnemyIntent(
        BattleEnemyIntent enemyIntent
    )
    {
        if (!TryCreateUnrespondedAttackAction(
                enemyIntent,
                out BattleExecutionAction attackAction
            ))
        {
            return CreateInvalidResolveResult(
                "ResolveUnrespondedEnemyIntent 失败：敌人Attack Action无效"
            );
        }

        return ResolveUnilateralAttack(attackAction);
    }

    // 无响应攻击与Pausable路径共用同一份Calculate结果，伤害只在Impact提交时发生。
    internal static BattleResolutionPlan BuildUnrespondedEnemyIntentResolutionPlan(
        BattleExecutionItem executionItem,
        BattleActionSlot responseActionSlot,
        BattleEnemyIntent enemyIntent
    )
    {
        if (!TryCreateUnrespondedAttackAction(
                enemyIntent,
                out BattleExecutionAction attackAction
            ))
        {
            return null;
        }

        BattleResolutionPlan plan = BuildUnilateralAttackResolutionPlan(
            attackAction,
            executionItem,
            responseActionSlot,
            out _
        );
        return plan != null &&
            TryRollUnilateralAttackResolutionPlan(plan, out _)
            ? plan
            : null;
    }

    static bool TryCreateUnrespondedAttackAction(
        BattleEnemyIntent enemyIntent,
        out BattleExecutionAction attackAction
    )
    {
        attackAction = null;
        if (enemyIntent == null || enemyIntent.enemy == null ||
            enemyIntent.enemyCardState == null ||
            enemyIntent.enemyCardState.cardData == null ||
            enemyIntent.actualTargetCharacter == null ||
            enemyIntent.actualTargetSlotIndex <= 0 ||
            enemyIntent.enemyCardState.cardData.cardType != CardType.Attack)
        {
            return false;
        }

        attackAction = new BattleExecutionAction(
            enemyIntent.enemy,
            enemyIntent.enemyCardState,
            null,
            enemyIntent,
            enemyIntent.actualTargetCharacter
        );
        return true;
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
        BattleEnemyIntent enemyIntent
    )
    {
        BattleClashSession session = CreateRespondedAttackClashSession(
            actionSlot,
            enemyIntent
        );

        // 同步兼容入口仍会主动推进至Finalized；单次Roll能力保留在Session中。
        while (!session.IsFinalized)
        {
            session.RollNextAttempt();
            if (session.AttemptResult == BattleClashAttemptResult.AttackTie)
            {
                Debug.Log(
                    "ResolveRespondedEnemyIntent 第" +
                    session.AttemptIndex +
                    "次拼点平局：玩家点数 " +
                    session.SideAPoint +
                    "，敌人点数 " +
                    session.SideBPoint
                );
            }
        }

        return FinalizeRespondedClash(actionSlot, enemyIntent, session);
    }

    // 只初始化一次正式Attack Clash，供同步Resolver与后续逐Attempt执行入口共用。
    internal static BattleClashSession CreateRespondedAttackClashSession(
        BattleActionSlot actionSlot,
        BattleEnemyIntent enemyIntent
    )
    {
        CharacterData playerUnit = actionSlot.actor;
        BattleCardState playerCardState = actionSlot.cardState;
        CharacterData enemyUnit = enemyIntent.enemy;
        BattleCardState enemyCardState = enemyIntent.enemyCardState;
        CharacterData actualTarget = enemyIntent.actualTargetCharacter;

        BattleClashPointSnapshot playerPointBuffSnapshot =
            CapturePointBuffSnapshot(playerUnit);
        BattleClashPointSnapshot enemyPointBuffSnapshot =
            CapturePointBuffSnapshot(enemyUnit);

        TriggerActionStart(enemyUnit, actualTarget, enemyCardState);
        TriggerActionStart(playerUnit, enemyUnit, playerCardState);

        BattleClashResourceSnapshot playerResourceSnapshot =
            CaptureResourceSnapshot(playerUnit, playerCardState);
        BattleClashResourceSnapshot enemyResourceSnapshot =
            CaptureResourceSnapshot(enemyUnit, enemyCardState);

        TriggerBattleEvent(BattleTiming.BeforeUse, enemyUnit, actualTarget,
            enemyCardState, 0, 0, false, false);
        TriggerBattleEvent(BattleTiming.BeforeUse, playerUnit, enemyUnit,
            playerCardState, 0, 0, false, false);

        enemyUnit.CheckBuffsByTiming(BattleTiming.ClashStart, false);
        playerUnit.CheckBuffsByTiming(BattleTiming.ClashStart, false);

        return BattleClashSession.CreateAttackVsAttack(
            new BattleClashSideState(
                playerUnit,
                playerCardState,
                playerPointBuffSnapshot,
                playerResourceSnapshot
            ),
            new BattleClashSideState(
                enemyUnit,
                enemyCardState,
                enemyPointBuffSnapshot,
                enemyResourceSnapshot
            ),
            actualTarget
        );
    }

    // 旧兼容方法：正式 Responded Attack 路径已不再调用精确响应后的后备守备。
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

        return ResolveDefenseVsAttackCore(
            actionSlot,
            enemyIntent,
            "ResolveRespondedDefenseVsAttack",
            CreateRespondedDefenseClashSession(actionSlot, enemyIntent)
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

        return ResolveDefenseVsAttackCore(
            defenseSlot,
            enemyIntent,
            "ResolveDefenseVsAttackWithKnownEnemyPoint",
            CreateKnownPointDefenseClashSession(
                defenseSlot,
                enemyIntent,
                knownEnemyAttackPoint
            )
        );
    }

    internal static BattleClashSession CreateRespondedDefenseClashSession(
        BattleActionSlot defenseSlot,
        BattleEnemyIntent enemyIntent
    )
    {
        BattleExecutionInteractionContext context =
            CreateRespondedInteractionContext(defenseSlot, enemyIntent);
        return TryGetAttackAndDefenseActions(
                context,
                out BattleExecutionAction attackAction,
                out BattleExecutionAction defenseAction
            )
            ? CreateAttackVsDefenseClashSession(attackAction, defenseAction)
            : null;
    }

    internal static BattleClashSession CreateAttackVsDefenseClashSession(
        BattleExecutionAction attackAction,
        BattleExecutionAction defenseAction
    )
    {
        if (!IsValidExecutionAction(attackAction) ||
            !IsValidExecutionAction(defenseAction))
        {
            return null;
        }

        BattleClashPointSnapshot defensePointSnapshot =
            CapturePointBuffSnapshot(defenseAction.actor);
        BattleClashPointSnapshot attackPointSnapshot =
            CapturePointBuffSnapshot(attackAction.actor);

        TriggerActionStart(
            attackAction.actor,
            defenseAction.actor,
            attackAction.cardState
        );
        TriggerActionStart(
            defenseAction.actor,
            attackAction.actor,
            defenseAction.cardState
        );

        BattleClashResourceSnapshot defenseResourceSnapshot =
            CaptureResourceSnapshot(
                defenseAction.actor,
                defenseAction.cardState
            );
        BattleClashResourceSnapshot attackResourceSnapshot =
            CaptureResourceSnapshot(
                attackAction.actor,
                attackAction.cardState
            );

        TriggerBattleEvent(
            BattleTiming.BeforeUse,
            attackAction.actor,
            defenseAction.actor,
            attackAction.cardState,
            0,
            0,
            false,
            false
        );
        TriggerBattleEvent(
            BattleTiming.BeforeUse,
            defenseAction.actor,
            attackAction.actor,
            defenseAction.cardState,
            0,
            0,
            false,
            false
        );
        attackAction.actor.CheckBuffsByTiming(BattleTiming.ClashStart, false);
        defenseAction.actor.CheckBuffsByTiming(BattleTiming.ClashStart, false);

        return BattleClashSession.CreateDefenseVsAttack(
            new BattleClashSideState(
                defenseAction.actor,
                defenseAction.cardState,
                defensePointSnapshot,
                defenseResourceSnapshot
            ),
            new BattleClashSideState(
                attackAction.actor,
                attackAction.cardState,
                attackPointSnapshot,
                attackResourceSnapshot
            ),
            defenseAction.actor
        );
    }

    internal static BattleClashSession CreateKnownPointDefenseClashSession(
        BattleActionSlot defenseSlot,
        BattleEnemyIntent enemyIntent,
        int knownEnemyAttackPoint
    )
    {
        CharacterData playerUnit = defenseSlot.actor;
        BattleCardState defenseCardState = defenseSlot.cardState;
        CharacterData enemyUnit = enemyIntent.enemy;
        BattleCardState enemyCardState = enemyIntent.enemyCardState;
        BattleClashPointSnapshot playerPointBuffSnapshot =
            CapturePointBuffSnapshot(playerUnit);

        TriggerActionStart(playerUnit, enemyUnit, defenseCardState);
        BattleClashResourceSnapshot playerResourceSnapshot =
            CaptureResourceSnapshot(playerUnit, defenseCardState);
        TriggerBattleEvent(BattleTiming.BeforeUse, playerUnit, enemyUnit,
            defenseCardState, 0, 0, false, false);
        playerUnit.CheckBuffsByTiming(BattleTiming.ClashStart, false);

        return BattleClashSession.CreateDefenseVsAttack(
            new BattleClashSideState(
                playerUnit,
                defenseCardState,
                playerPointBuffSnapshot,
                playerResourceSnapshot
            ),
            new BattleClashSideState(
                enemyUnit,
                enemyCardState,
                new BattleClashPointSnapshot(),
                new BattleClashResourceSnapshot
                {
                    cardState = enemyCardState
                }
            ),
            enemyIntent.actualTargetCharacter,
            true,
            Mathf.Max(0, knownEnemyAttackPoint)
        );
    }

    static BattleResolveResult ResolveDefenseVsAttackCore(
        BattleActionSlot defenseSlot,
        BattleEnemyIntent enemyIntent,
        string messagePrefix,
        BattleClashSession session
    )
    {
        if (!session.IsFinalized && !session.RollNextAttempt())
        {
            return CreateInvalidResolveResult(messagePrefix + " 失败：Defense Clash无法Roll");
        }

        if (!session.IsFinalized)
        {
            return CreateInvalidResolveResult(messagePrefix + " 失败：Defense Clash尚未完成");
        }

        BattleResolutionPlan plan = BuildRespondedClashResolutionPlan(
            defenseSlot,
            enemyIntent,
            session
        );
        return plan != null
            ? CommitResolutionSynchronously(plan)
            : CreateInvalidResolveResult(messagePrefix + " 失败：无法建立ResolutionPlan");
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

        CharacterData playerUnit = dodgeSlot.actor;
        CharacterData enemyUnit = enemyIntent.enemy;
        BattleCardState dodgeCardState = dodgeSlot.cardState;

        if (!BattleCardManager.CanUseCard(playerUnit, enemyUnit, dodgeCardState))
        {
            return CreateActionUnavailableResult(
                "ResolveDodgeVsAttackWithKnownEnemyPoint：闪避卡执行时已不可用，本次守备跳过。" +
                playerUnit.characterName +
                " 的卡牌不能使用：" +
                dodgeCardState.GetCardName()
            );
        }

        BattleClashSession session = CreateKnownPointDodgeClashSession(
            dodgeSlot,
            enemyIntent,
            knownEnemyAttackPoint
        );
        session.RollNextAttempt();
        return FinalizeRespondedClash(dodgeSlot, enemyIntent, session);
    }

    internal static BattleClashSession CreateKnownPointDodgeClashSession(
        BattleActionSlot dodgeSlot,
        BattleEnemyIntent enemyIntent,
        int knownEnemyAttackPoint
    )
    {
        CharacterData playerUnit = dodgeSlot.actor;
        BattleCardState dodgeCardState = dodgeSlot.cardState;
        CharacterData enemyUnit = enemyIntent.enemy;
        BattleCardState enemyCardState = enemyIntent.enemyCardState;
        BattleClashPointSnapshot playerPointBuffSnapshot =
            CapturePointBuffSnapshot(playerUnit);

        TriggerActionStart(playerUnit, enemyUnit, dodgeCardState);
        BattleClashResourceSnapshot playerResourceSnapshot =
            CaptureResourceSnapshot(playerUnit, dodgeCardState);
        TriggerBattleEvent(BattleTiming.BeforeUse, playerUnit, enemyUnit,
            dodgeCardState, 0, 0, false, false);
        playerUnit.CheckBuffsByTiming(BattleTiming.ClashStart, false);

        return BattleClashSession.CreateDodgeVsAttack(
            new BattleClashSideState(
                playerUnit,
                dodgeCardState,
                playerPointBuffSnapshot,
                playerResourceSnapshot
            ),
            new BattleClashSideState(
                enemyUnit,
                enemyCardState,
                new BattleClashPointSnapshot(),
                new BattleClashResourceSnapshot
                {
                    cardState = enemyCardState
                }
            ),
            enemyIntent.actualTargetCharacter,
            true,
            Mathf.Max(0, knownEnemyAttackPoint)
        );
    }

    public static BattleResolveResult ResolveContinuousDodgeVsAttack(
        BattleActionSlot playerSlot,
        BattleEnemyIntent enemyIntent
    )
    {
        return ResolveRespondedDodgeVsAttack(playerSlot, enemyIntent, true);
    }

    public static void FinalizeDeferredDodgeCardUse(BattleActionSlot slot)
    {
        if (slot == null ||
            slot.actor == null ||
            slot.cardState == null ||
            slot.cardState.cardData == null ||
            slot.cardState.cardData.cardType != CardType.Dodge)
        {
            Debug.LogWarning("连续闪避正式结算失败：槽位、行动者或Dodge卡牌无效");
            return;
        }

        CharacterData target = slot.lastContinuousDodgeOpponent != null
            ? slot.lastContinuousDodgeOpponent
            : slot.requestedEnemy;

        TriggerBattleEvent(
            BattleTiming.Resolved,
            slot.actor,
            target,
            slot.cardState,
            slot.lastContinuousDodgePoint,
            0,
            false,
            false,
            ClashResult.Win
        );
    }

    static BattleResolveResult ResolveRespondedDodgeVsAttack(
        BattleActionSlot playerSlot,
        BattleEnemyIntent enemyIntent,
        bool isContinuousDodgeContinuation = false
    )
    {
        BattleResolveResult beginFailure = TryBeginDodgeClash(
            playerSlot,
            enemyIntent,
            isContinuousDodgeContinuation,
            out BattleClashSession session
        );
        if (beginFailure != null)
        {
            return beginFailure;
        }

        if (!session.RollNextAttempt())
        {
            return CreateInvalidResolveResult("ResolveRespondedDodgeVsAttack 失败：Dodge Clash无法Roll");
        }

        return FinalizeRespondedClash(playerSlot, enemyIntent, session);
    }

    internal static BattleResolveResult TryBeginContinuousDodgeClash(
        BattleActionSlot playerSlot,
        BattleEnemyIntent enemyIntent,
        out BattleClashSession session
    )
    {
        return TryBeginDodgeClash(
            playerSlot,
            enemyIntent,
            true,
            out session
        );
    }

    // 同步与Pausable连续闪避共用同一套校验和Session初始化。
    static BattleResolveResult TryBeginDodgeClash(
        BattleActionSlot playerSlot,
        BattleEnemyIntent enemyIntent,
        bool isContinuousDodgeContinuation,
        out BattleClashSession session
    )
    {
        session = null;
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

        BattleExecutionInteractionContext context =
            CreateRespondedInteractionContext(playerSlot, enemyIntent);
        if (!TryGetAttackAndDodgeActions(
                context,
                out BattleExecutionAction attackAction,
                out BattleExecutionAction dodgeAction
            ))
        {
            return CreateInvalidResolveResult(
                "ResolveRespondedDodgeVsAttack 失败：无法归一化AttackVsDodge Action"
            );
        }

        return TryBeginAttackVsDodge(
            attackAction,
            dodgeAction,
            isContinuousDodgeContinuation,
            out session
        );
    }

    internal static BattleClashSession CreateRespondedDodgeClashSession(
        BattleActionSlot playerSlot,
        BattleEnemyIntent enemyIntent,
        bool isContinuousDodgeContinuation = false
    )
    {
        BattleExecutionInteractionContext context =
            CreateRespondedInteractionContext(playerSlot, enemyIntent);
        return TryGetAttackAndDodgeActions(
                context,
                out BattleExecutionAction attackAction,
                out BattleExecutionAction dodgeAction
            )
            ? CreateAttackVsDodgeClashSession(
                attackAction,
                dodgeAction,
                isContinuousDodgeContinuation
            )
            : null;
    }

    internal static BattleClashSession CreateAttackVsDodgeClashSession(
        BattleExecutionAction attackAction,
        BattleExecutionAction dodgeAction,
        bool isContinuousDodgeContinuation = false
    )
    {
        if (!IsValidExecutionAction(attackAction) ||
            !IsValidExecutionAction(dodgeAction))
        {
            return null;
        }

        BattleClashPointSnapshot dodgePointBuffSnapshot =
            isContinuousDodgeContinuation
                ? new BattleClashPointSnapshot()
                : CapturePointBuffSnapshot(dodgeAction.actor);
        BattleClashPointSnapshot attackPointBuffSnapshot =
            CapturePointBuffSnapshot(attackAction.actor);

        TriggerActionStart(
            attackAction.actor,
            dodgeAction.actor,
            attackAction.cardState
        );
        if (!isContinuousDodgeContinuation)
        {
            TriggerActionStart(
                dodgeAction.actor,
                attackAction.actor,
                dodgeAction.cardState
            );
        }

        BattleClashResourceSnapshot dodgeResourceSnapshot =
            isContinuousDodgeContinuation
                ? new BattleClashResourceSnapshot
                {
                    cardState = dodgeAction.cardState,
                    selectedMinPoint = dodgeAction.cardState.cardData.minPoint,
                    selectedMaxPoint = dodgeAction.cardState.cardData.maxPoint
                }
                : CaptureResourceSnapshot(
                    dodgeAction.actor,
                    dodgeAction.cardState
                );
        BattleClashResourceSnapshot attackResourceSnapshot =
            CaptureResourceSnapshot(
                attackAction.actor,
                attackAction.cardState
            );

        TriggerBattleEvent(
            BattleTiming.BeforeUse,
            attackAction.actor,
            dodgeAction.actor,
            attackAction.cardState,
            0,
            0,
            false,
            false
        );
        if (!isContinuousDodgeContinuation)
        {
            TriggerBattleEvent(
                BattleTiming.BeforeUse,
                dodgeAction.actor,
                attackAction.actor,
                dodgeAction.cardState,
                0,
                0,
                false,
                false
            );
        }

        attackAction.actor.CheckBuffsByTiming(BattleTiming.ClashStart, false);
        if (!isContinuousDodgeContinuation)
        {
            dodgeAction.actor.CheckBuffsByTiming(
                BattleTiming.ClashStart,
                false
            );
        }

        return BattleClashSession.CreateDodgeVsAttack(
            new BattleClashSideState(
                dodgeAction.actor,
                dodgeAction.cardState,
                dodgePointBuffSnapshot,
                dodgeResourceSnapshot
            ),
            new BattleClashSideState(
                attackAction.actor,
                attackAction.cardState,
                attackPointBuffSnapshot,
                attackResourceSnapshot
            ),
            dodgeAction.actor,
            false,
            0,
            isContinuousDodgeContinuation
        );
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

        if (dodgePoint >= enemyPoint)
        {
            Debug.Log(allyUnit.characterName + " 闪避成功，闪避卡继续保留");

            // 闪避卡拼点胜利
            TriggerBattleEvent(BattleTiming.ClashWin, allyUnit, enemyUnit, dodgeCardState, dodgePoint, 0, false, false, ClashResult.Win);

            // 攻击卡拼点失败
            TriggerBattleEvent(BattleTiming.ClashLose, enemyUnit, allyUnit, enemyCardState, enemyPoint, 0, false, false, ClashResult.Lose);

            // 闪避卡生效
            TriggerBattleEvent(BattleTiming.Resolved, allyUnit, enemyUnit, dodgeCardState, dodgePoint, 0, false, false, ClashResult.Win);
        }
        else
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

    static int ConsumeClashPointBuffs(CharacterData unit, BattleClashPointSnapshot snapshot)
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

    static int ConsumeSuccessfulPointCardBuffs(CharacterData unit, BattleClashPointSnapshot snapshot)
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

    static BattleClashPointSnapshot CapturePointBuffSnapshot(CharacterData unit)
    {
        BattleClashPointSnapshot snapshot = new BattleClashPointSnapshot();

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

    static BattleClashResourceSnapshot CaptureResourceSnapshot(
        CharacterData unit,
        BattleCardState cardState,
        bool formalClash = true
    )
    {
        BattleClashResourceSnapshot snapshot = new BattleClashResourceSnapshot();
        snapshot.cardState = cardState;

        if (cardState == null || cardState.cardData == null)
        {
            return snapshot;
        }

        BattleKnifeCardRules.ResolveRuntimePointRange(
            cardState,
            formalClash,
            out snapshot.selectedMinPoint,
            out snapshot.selectedMaxPoint
        );

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
            BattleKnifeCardRules.ResolveRuntimePointRange(
                cardState,
                formalClash,
                out snapshot.selectedMinPoint,
                out snapshot.selectedMaxPoint
            );
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

        snapshot.pointModifierFromResource += BattleModificationRules.GetCardPointBonus(
            unit,
            cardState.cardData
        );

        if (rule.exactStackForBonus > 0 &&
            snapshot.capturedStack == rule.exactStackForBonus)
        {
            snapshot.pointModifierFromResource += rule.exactStackPointBonus;
        }

        snapshot.plannedConsumeAmount = Mathf.Max(0, rule.consumeAmountOnSuccess);
        snapshot.shouldConsumeOnSuccess = snapshot.normalVersionEnabled && snapshot.plannedConsumeAmount > 0;
        snapshot.consumeTiming = string.IsNullOrEmpty(rule.consumeTiming)
            ? CardResourceConsumeTiming.OnSuccessfulUse
            : rule.consumeTiming;

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
        BattleKnifeCardRules.CaptureActionStart(cardState);
        TriggerBattleEvent(BattleTiming.ActionStart, user, target, cardState, 0, 0, false, false);
    }

    static void PayDefaultResourceCostOnSuccessfulUse(CharacterData unit, BattleClashResourceSnapshot snapshot)
    {
        // 默认资源成本只在本次卡牌被视为成功使用时支付。
        // Attack拼点失败、ActionUnavailable、TieLimit和死亡跳过不会支付。
        // 无资源降级版本即使成功使用，也不会凭空扣除资源。
        if (GetConsumeTiming(snapshot) ==
            CardResourceConsumeTiming.OnSuccessfulUse)
        {
            PayCapturedResourceCost(unit, snapshot);
        }
    }

    static void PayResolvedParticipationResourceCost(
        CharacterData unit,
        BattleClashResourceSnapshot snapshot
    )
    {
        if (GetConsumeTiming(snapshot) ==
            CardResourceConsumeTiming.OnResolvedParticipation)
        {
            PayCapturedResourceCost(unit, snapshot);
        }
    }

    static void PayLongRangeShootResourceOnTerminalUse(BattleClashSideState side)
    {
        if (side == null || side.cardState == null ||
            !side.cardState.IsLongRangeShoot())
        {
            return;
        }

        // 仅 resolved participation 语义的远程射击会在败方终局支付资源。
        // OnSuccessfulUse 仍只由胜方的成功使用路径支付。
        if (GetConsumeTiming(side.resourceSnapshot) ==
            CardResourceConsumeTiming.OnResolvedParticipation)
        {
            PayCapturedResourceCost(side.actor, side.resourceSnapshot);
        }
    }

    static string GetConsumeTiming(BattleClashResourceSnapshot snapshot)
    {
        return snapshot != null && !string.IsNullOrEmpty(snapshot.consumeTiming)
            ? snapshot.consumeTiming
            : CardResourceConsumeTiming.OnSuccessfulUse;
    }

    static string GetInsufficientBehavior(
        BattleClashResourceSnapshot snapshot
    )
    {
        CardResourceRuleData rule = snapshot != null && snapshot.cardState != null &&
            snapshot.cardState.cardData != null
            ? GetFirstResourceRule(snapshot.cardState.cardData)
            : null;
        return rule != null && !string.IsNullOrEmpty(rule.insufficientBehavior)
            ? rule.insufficientBehavior
            : CardResourceInsufficientBehavior.SoftFallback;
    }

    static void PayCapturedResourceCost(
        CharacterData unit,
        BattleClashResourceSnapshot snapshot
    )
    {
        if (unit == null || snapshot == null ||
            !snapshot.hasRule || !snapshot.shouldConsumeOnSuccess)
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
