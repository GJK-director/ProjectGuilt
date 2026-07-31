// 脚本中文说明：行动槽位。负责保存准备阶段放入槽位的角色、卡牌、目标和响应的敌人意图。
// BattleActionPlacementType = 玩家在准备阶段保存的原始放置范围
// 该字段只描述“放到哪里”，不描述当前是否成为主要响应者。
public enum BattleActionPlacementType
{
    None,
    ExactEnemyIntent,
    SpecificEnemy,
    Self
}

// BattleActionSlotType = 行动槽位当前生效类型
// RespondToEnemyIntent：响应敌人意图
// FreeAction：不直接响应敌人意图的自由行动，第一版只用于 Ability 罪卡测试
// PassiveGuard：被动守备，等待敌人攻击实际命中本槽位 owner 时触发
public enum BattleActionSlotType
{
    // RespondToEnemyIntent = 响应敌人意图
    // 例如：玩家 A 用槽位 1 的卡牌去拦截敌人意图 2。
    RespondToEnemyIntent,

    // FreeAction = 自由行动
    // 例如：玩家不响应敌人意图，而是自己使用 Ability 罪卡。
    FreeAction,

    // PassiveGuard = 被动守备
    // 例如：玩家把 Defense 放在自己的槽位中，等待无人响应的敌人攻击命中自己时触发。
    PassiveGuard,

    // EnemySpecificGuard = 针对指定敌人的守备
    // 本轮只保存准备阶段语义，不接入 Executor / Resolver。
    EnemySpecificGuard
}

public enum ContinuousDodgeSource
{
    None,
    ExactEnemyIntent,
    EnemySpecificGuard,
    PassiveGuard,
    ContinuousDodge
}

// BattleActionSlot = 行动槽位
// 槽位只记录准备阶段安排，不负责真正战斗结算
public class BattleActionSlot
{
    // owner = 槽位归属角色
    // 例如我方角色A的槽位1、我方角色B的槽位1。旧全局槽位可以保持为空。
    public CharacterData owner;

    // slotIndex = 槽位编号
    // 第一版约定从 1 开始。owner 不为空时表示角色内槽位编号。
    public int slotIndex;

    // slotType = 槽位类型
    // BattleActionSlotType = 行动槽位类型，表示这个槽位是响应敌人意图还是自由行动。
    public BattleActionSlotType slotType;

    // actor = 行动者
    // CharacterData = 角色数据，例如玩家 A、玩家 B。
    public CharacterData actor;

    // cardState = 战斗卡牌状态
    // BattleCardState = 战斗中的卡牌实例，记录这张卡当前 CD、使用次数等状态。
    public BattleCardState cardState;

    // target = 行动目标
    // 自由行动时通常是玩家选择的目标；响应敌人意图时当前暂存敌人。
    public CharacterData target;

    // enemyIntent = 绑定的敌人意图
    // BattleEnemyIntent = 敌人意图，记录敌人准备攻击谁、攻击哪个槽位。
    // 只有响应敌人意图时，这里才应该有值。
    public BattleEnemyIntent enemyIntent;

    // placementType = 玩家最终保存的原始放置范围
    public BattleActionPlacementType placementType;

    // requestedEnemyIntent = 玩家原本精确放置到的敌人意图
    // 即使当前不是主要响应者，也必须保留。
    public BattleEnemyIntent requestedEnemyIntent;

    // requestedEnemy = 精确意图或指定敌人放置所针对的敌人
    public CharacterData requestedEnemy;

    // 指定敌人放置时记录玩家点击的敌方UI槽位，仅用于稳定表现该正式放置关系。
    public int requestedTargetSlotIndex;

    // assignmentSequence = 本回合安排顺序，越大表示越晚放置
    public long assignmentSequence;

    // isUsed = 槽位本回合的正式行动是否已经完成一次提交
    // 注意：卡牌是否 Resolved 与槽位是否完成行动不是同一个概念。
    // 例如 Attack vs Attack 失败方卡牌不算成功使用，但响应槽位已经正式参与拼点。
    public bool isUsed;

    // 连续闪避在本回合内保持槽位未使用，直到失败或回合结束时统一结算。
    public bool isContinuousDodgeActive;
    public int successfulDodgeCount;
    public bool isCardUseFinalized;
    public ContinuousDodgeSource continuousDodgeSource;
    public int lastContinuousDodgePoint;
    public CharacterData lastContinuousDodgeOpponent;

    // BattleActionSlot = 行动槽位构造函数
    // slotIndex = 槽位编号。
    public BattleActionSlot(int slotIndex)
    {
        owner = null;
        this.slotIndex = slotIndex;
        Clear();
    }

    // BattleActionSlot = 角色独立行动槽位构造函数
    // owner = 这个槽位属于哪个角色，slotIndex = 角色内槽位编号。
    public BattleActionSlot(CharacterData owner, int slotIndex)
    {
        this.owner = owner;
        this.slotIndex = slotIndex;
        Clear();
    }

    // IsEmpty = 判断槽位是否为空
    // 当前用 cardState 是否为空来判断槽位有没有安排卡牌。
    public bool IsEmpty()
    {
        return cardState == null;
    }

    // AssignResponse = 安排响应敌人意图
    // actor = 使用这个槽位行动的角色。
    // cardState = 放入槽位的卡牌状态。
    // enemyIntent = 这个槽位要响应的敌人意图。
    // 该方法是受信任的底层槽位写入入口，不负责完整资格检查。
    // 正式准备阶段安排应通过 BattleActionSlotManager 执行。
    public void AssignResponse(
        CharacterData actor,
        BattleCardState cardState,
        BattleEnemyIntent enemyIntent,
        bool rewriteActualTarget
    )
    {
        slotType = BattleActionSlotType.RespondToEnemyIntent;
        this.actor = actor;
        this.cardState = cardState;
        this.enemyIntent = enemyIntent;
        placementType = BattleActionPlacementType.ExactEnemyIntent;
        requestedEnemyIntent = enemyIntent;
        requestedEnemy = enemyIntent != null ? enemyIntent.enemy : null;
        requestedTargetSlotIndex = enemyIntent != null
            ? enemyIntent.enemySlotIndex
            : 0;

        if (assignmentSequence <= 0)
        {
            assignmentSequence = 1;
        }

        isUsed = false;
        ResetContinuousDodgeState();

        if (enemyIntent != null)
        {
            // 响应敌人意图时，目标先记录为敌人。
            target = enemyIntent.enemy;

            // 高速介入时才改写敌人意图的实际目标。
            // 低速原目标槽位响应只绑定敌人意图，不改写 actualTarget。
            if (rewriteActualTarget)
            {
                enemyIntent.SetActualTarget(actor, slotIndex);
            }
        }
        else
        {
            target = null;
        }
    }

    // AssignFreeAction = 安排自由行动
    // 自由行动不绑定敌人意图，通常用于 Ability 罪卡或未来的普通主动行动。
    // 该方法是受信任的底层槽位写入入口，不负责完整资格检查。
    // 正式准备阶段安排应通过 BattleActionSlotManager 执行。
    public void AssignFreeAction(
        CharacterData actor,
        BattleCardState cardState,
        CharacterData target
    )
    {
        slotType = BattleActionSlotType.FreeAction;
        this.actor = actor;
        this.cardState = cardState;
        this.target = target;
        enemyIntent = null;
        requestedEnemyIntent = null;
        requestedTargetSlotIndex = 0;

        if (object.ReferenceEquals(actor, target))
        {
            placementType = BattleActionPlacementType.Self;
            requestedEnemy = null;
        }
        else
        {
            placementType = BattleActionPlacementType.SpecificEnemy;
            requestedEnemy = target;
        }

        if (assignmentSequence <= 0)
        {
            assignmentSequence = 1;
        }

        isUsed = false;
        ResetContinuousDodgeState();
    }

    // AssignPassiveGuard = 安排被动守备
    // 被动守备不绑定具体敌人意图，不主动进入执行队列，实际触发时才算使用。
    // 该方法是受信任的底层槽位写入入口，不负责完整资格检查。
    // 正式准备阶段安排应通过 BattleActionSlotManager 执行。
    public void AssignPassiveGuard(
        CharacterData actor,
        BattleCardState cardState
    )
    {
        slotType = BattleActionSlotType.PassiveGuard;
        this.actor = actor;
        this.cardState = cardState;
        target = actor;
        enemyIntent = null;
        placementType = BattleActionPlacementType.Self;
        requestedEnemyIntent = null;
        requestedEnemy = null;
        requestedTargetSlotIndex = 0;

        if (assignmentSequence <= 0)
        {
            assignmentSequence = 1;
        }

        isUsed = false;
        ResetContinuousDodgeState();
    }

    // AssignEnemySpecificGuard = 保存针对指定敌人的守备
    // 本轮不执行该类型，只建立准备阶段数据模型。
    public void AssignEnemySpecificGuard(
        CharacterData actor,
        BattleCardState cardState,
        CharacterData enemy
    )
    {
        slotType = BattleActionSlotType.EnemySpecificGuard;
        this.actor = actor;
        this.cardState = cardState;
        target = enemy;
        enemyIntent = null;
        placementType = BattleActionPlacementType.SpecificEnemy;
        requestedEnemyIntent = null;
        requestedEnemy = enemy;
        requestedTargetSlotIndex = 0;

        if (assignmentSequence <= 0)
        {
            assignmentSequence = 1;
        }

        isUsed = false;
        ResetContinuousDodgeState();
    }

    // MarkUsed = 标记槽位行动已提交
    // 表示这个槽位本回合已经完成正式行动；不等同于卡牌一定触发 Resolved。
    public void MarkUsed()
    {
        isUsed = true;
    }

    public void ActivateContinuousDodge(
        ContinuousDodgeSource source,
        int dodgePoint,
        CharacterData opponent
    )
    {
        isContinuousDodgeActive = true;
        successfulDodgeCount = 1;
        isCardUseFinalized = false;
        continuousDodgeSource = source;
        lastContinuousDodgePoint = dodgePoint;
        lastContinuousDodgeOpponent = opponent;
    }

    public void RegisterContinuousDodgeSuccess(int dodgePoint, CharacterData opponent)
    {
        if (!isContinuousDodgeActive || isCardUseFinalized)
        {
            return;
        }

        successfulDodgeCount++;
        lastContinuousDodgePoint = dodgePoint;
        lastContinuousDodgeOpponent = opponent;
    }

    public void FinishContinuousDodge()
    {
        isContinuousDodgeActive = false;
    }

    public void MarkCardUseFinalized()
    {
        isCardUseFinalized = true;
        isContinuousDodgeActive = false;
    }

    public void ResetContinuousDodgeState()
    {
        isContinuousDodgeActive = false;
        successfulDodgeCount = 0;
        isCardUseFinalized = false;
        continuousDodgeSource = ContinuousDodgeSource.None;
        lastContinuousDodgePoint = 0;
        lastContinuousDodgeOpponent = null;
    }

    // UnbindEnemyIntent = 解除敌人意图绑定
    // 用于“同一个敌人意图只能有一个主要响应槽位”时，解除旧槽位绑定。
    public void UnbindEnemyIntent()
    {
        enemyIntent = null;

        if (placementType == BattleActionPlacementType.ExactEnemyIntent ||
            placementType == BattleActionPlacementType.SpecificEnemy)
        {
            target = requestedEnemy;

            if (cardState != null &&
                cardState.cardData != null &&
                (cardState.cardData.cardType == CardType.Defense ||
                 cardState.cardData.cardType == CardType.Dodge))
            {
                slotType = BattleActionSlotType.EnemySpecificGuard;
            }
            else
            {
                slotType = BattleActionSlotType.FreeAction;
            }
        }
        else
        {
            target = null;
        }

        isUsed = false;
    }

    // Clear = 清空槽位
    // 把槽位恢复到没有行动者、没有卡牌、没有目标、没有敌人意图的状态。
    public void Clear()
    {
        slotType = BattleActionSlotType.FreeAction;
        actor = null;
        cardState = null;
        target = null;
        enemyIntent = null;
        placementType = BattleActionPlacementType.None;
        requestedEnemyIntent = null;
        requestedEnemy = null;
        requestedTargetSlotIndex = 0;
        assignmentSequence = 0;
        isUsed = false;
        ResetContinuousDodgeState();
    }

    // GetActorName = 获取行动者名字
    // 如果没有行动者，返回“无行动者”，避免打印日志时报错。
    public string GetActorName()
    {
        if (actor == null)
        {
            return "无行动者";
        }

        return actor.characterName;
    }

    // GetOwnerName = 获取槽位归属角色名字
    // 旧全局槽位没有 owner 时返回“无归属”。
    public string GetOwnerName()
    {
        if (owner == null)
        {
            return "无归属";
        }

        return owner.characterName;
    }

    // GetDisplaySlotName = 获取适合 UI / 日志显示的槽位名
    // owner 不为空时显示“角色名 槽位X”，否则保持旧的“槽位X”。
    public string GetDisplaySlotName()
    {
        if (owner == null)
        {
            return "槽位" + slotIndex;
        }

        return owner.characterName + " 槽位" + slotIndex;
    }

    // GetCardName = 获取卡牌名字
    // BattleCardState.GetCardName = 从战斗卡牌状态里取卡牌显示名。
    public string GetCardName()
    {
        if (cardState == null)
        {
            return "空";
        }

        return cardState.GetCardName();
    }

    // GetTargetName = 获取目标名字
    // 如果没有目标，返回“无目标”。
    public string GetTargetName()
    {
        if (target == null)
        {
            return "无目标";
        }

        return target.characterName;
    }
}

// BattleContinuousDodgeManager = 连续闪避激活与正式卡牌使用收尾的唯一入口。
public static class BattleContinuousDodgeManager
{
    public static void RegisterSuccess(
        BattleActionSlot slot,
        BattleResolveResult result,
        ContinuousDodgeSource source,
        BattleEnemyIntent enemyIntent
    )
    {
        if (slot == null || result == null || slot.isCardUseFinalized)
        {
            return;
        }

        if (slot.isContinuousDodgeActive)
        {
            slot.RegisterContinuousDodgeSuccess(
                result.playerPoint,
                enemyIntent != null ? enemyIntent.enemy : null
            );
            UnityEngine.Debug.Log(
                "[ContinuousDodge Success]\n" +
                "Actor: " + slot.GetActorName() + "\n" +
                "Slot: " + slot.slotIndex + "\n" +
                "SuccessfulCount: " + slot.successfulDodgeCount + "\n" +
                "RemainActive: True\n" +
                "CardUseFinalized: False"
            );
            return;
        }

        slot.ActivateContinuousDodge(
            source,
            result.playerPoint,
            enemyIntent != null ? enemyIntent.enemy : null
        );
        UnityEngine.Debug.Log(
            "[ContinuousDodge]\n" +
            "Actor: " + slot.GetActorName() + "\n" +
            "Slot: " + slot.slotIndex + "\n" +
            "Card: " + slot.GetCardName() + "\n" +
            "SuccessfulCount: " + slot.successfulDodgeCount + "\n" +
            "Source: " + source
        );
    }

    public static void RecordImmediateFinalization(
        BattleActionSlot slot,
        BattleResolveResult result
    )
    {
        if (slot == null || slot.isCardUseFinalized)
        {
            return;
        }

        slot.MarkCardUseFinalized();
        slot.MarkUsed();

        UnityEngine.Debug.Log(
            "[ContinuousDodge Failed]\n" +
            "Actor: " + slot.GetActorName() + "\n" +
            "Slot: " + slot.slotIndex + "\n" +
            "Damage: " + (result != null ? result.damage : 0) + "\n" +
            "RemainActive: False\n" +
            "CardUseFinalized: True"
        );
    }

    public static bool FinalizeActionCardUse(
        BattleRuntimeState runtimeState,
        BattleActionSlot slot,
        string reason
    )
    {
        if (slot == null ||
            !slot.isContinuousDodgeActive ||
            slot.successfulDodgeCount < 1 ||
            slot.isCardUseFinalized)
        {
            return false;
        }

        int oldUseCount = slot.cardState != null ? slot.cardState.currentUseCount : 0;
        int oldGuilt = runtimeState != null ? runtimeState.currentGuilt : 0;

        // 先锁定幂等状态，再触发 Resolved，避免任何回调重复结算同一槽位。
        slot.MarkCardUseFinalized();
        BattleResolver.FinalizeDeferredDodgeCardUse(slot);
        slot.MarkUsed();

        int newUseCount = slot.cardState != null ? slot.cardState.currentUseCount : oldUseCount;
        int newCooldown = slot.cardState != null ? slot.cardState.currentCooldown : 0;
        int newGuilt = runtimeState != null ? runtimeState.currentGuilt : oldGuilt;

        UnityEngine.Debug.Log(
            "[ContinuousDodge " + reason + " Finalize]\n" +
            "Actor: " + slot.GetActorName() + "\n" +
            "Slot: " + slot.slotIndex + "\n" +
            "SuccessfulCount: " + slot.successfulDodgeCount + "\n" +
            "UseCount delta: " + (newUseCount - oldUseCount) + "\n" +
            "Cooldown: " + newCooldown + "\n" +
            "Guilt delta: " + (newGuilt - oldGuilt)
        );
        return true;
    }

    public static int FinalizeActiveDodges(BattleRuntimeState runtimeState, string reason)
    {
        if (runtimeState == null || runtimeState.actionSlots == null)
        {
            return 0;
        }

        int finalizedCount = 0;

        foreach (BattleActionSlot slot in runtimeState.actionSlots)
        {
            if (FinalizeActionCardUse(runtimeState, slot, reason))
            {
                finalizedCount++;
            }
        }

        return finalizedCount;
    }
}
