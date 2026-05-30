// 脚本中文说明：敌人意图。负责记录敌人准备用哪张卡攻击哪个角色的哪个槽位，以及实际目标是否被改写。
// BattleEnemyIntent = 敌人行动意图
// 第一版只用于测试：敌人使用一张卡攻击一个角色的某个槽位
public class BattleEnemyIntent
{
    // intentID = 敌人意图 ID
    // 用来区分不同敌人意图，后续可以和关卡 / AI 数据关联。
    public string intentID;

    // intentOrder = 敌人意图顺序编号
    // 第一版从 1 开始，例如敌人意图 1、敌人意图 2。
    public int intentOrder;

    // enemy = 发出这个意图的敌人
    // CharacterData = 角色数据，这里也临时用于敌人数据。
    public CharacterData enemy;

    // enemyCardState = 敌人卡牌状态
    // BattleCardState = 战斗中的卡牌实例，记录敌人准备用哪张卡。
    public BattleCardState enemyCardState;

    // originalTargetCharacter = 原始目标角色
    // 敌人最开始打算攻击谁。
    public CharacterData originalTargetCharacter;

    // originalTargetSlotIndex = 原始目标槽位编号
    // 敌人最开始打算攻击目标角色的哪个槽位。
    public int originalTargetSlotIndex;

    // actualTargetCharacter = 实际目标角色
    // 如果玩家成功介入，这里会从原始目标改成介入者。
    public CharacterData actualTargetCharacter;

    // actualTargetSlotIndex = 实际目标槽位编号
    // 如果玩家用槽位 1 介入，这里会改成 1。
    public int actualTargetSlotIndex;

    // isResponded = 是否已经被玩家槽位响应
    // true 表示已经有一个玩家槽位绑定并响应了这个敌人意图。
    public bool isResponded;

    // BattleEnemyIntent = 敌人意图构造函数
    // 负责创建一条敌人意图，并把实际目标初始化为原始目标。
    public BattleEnemyIntent(
        // intentID = 敌人意图 ID。
        string intentID,

        // enemy = 发出意图的敌人。
        CharacterData enemy,

        // enemyCardState = 敌人准备用的卡牌状态。
        BattleCardState enemyCardState,

        // originalTargetCharacter = 敌人原本要攻击的角色。
        CharacterData originalTargetCharacter,

        // originalTargetSlotIndex = 敌人原本要攻击的槽位。
        int originalTargetSlotIndex,

        // intentOrder = 敌人意图顺序，默认是 1。
        int intentOrder = 1
    )
    {
        this.intentID = intentID;
        this.intentOrder = intentOrder;
        this.enemy = enemy;
        this.enemyCardState = enemyCardState;
        this.originalTargetCharacter = originalTargetCharacter;
        this.originalTargetSlotIndex = originalTargetSlotIndex;
        actualTargetCharacter = originalTargetCharacter;
        actualTargetSlotIndex = originalTargetSlotIndex;
        isResponded = false;
    }

    // SetActualTarget = 设置实际目标
    // 当玩家成功响应 / 介入时，用这个函数改写敌人真正会打到谁。
    public void SetActualTarget(CharacterData actualTargetCharacter, int actualTargetSlotIndex)
    {
        this.actualTargetCharacter = actualTargetCharacter;
        this.actualTargetSlotIndex = actualTargetSlotIndex;
    }

    // MarkResponded = 标记为已响应
    // 玩家槽位成功绑定这个敌人意图后调用。
    public void MarkResponded()
    {
        isResponded = true;
    }

    // GetEnemyName = 获取敌人名字
    // 如果 enemy 为空，返回“无敌人”，避免打印日志时报错。
    public string GetEnemyName()
    {
        if (enemy == null)
        {
            return "无敌人";
        }

        return enemy.characterName;
    }

    // GetCardName = 获取敌人卡牌名字
    // BattleCardState.GetCardName = 从卡牌状态里取卡牌显示名。
    public string GetCardName()
    {
        if (enemyCardState == null)
        {
            return "无卡牌";
        }

        return enemyCardState.GetCardName();
    }

    // GetOriginalTargetName = 获取原始目标名字
    // 原始目标 = 敌人最开始锁定的角色。
    public string GetOriginalTargetName()
    {
        if (originalTargetCharacter == null)
        {
            return "无目标";
        }

        return originalTargetCharacter.characterName;
    }

    // GetActualTargetName = 获取实际目标名字
    // 实际目标 = 经过响应 / 介入改写后真正会被攻击的角色。
    public string GetActualTargetName()
    {
        if (actualTargetCharacter == null)
        {
            return "无目标";
        }

        return actualTargetCharacter.characterName;
    }

    // GetOriginalTargetSlotText = 获取原始目标槽位文本
    // 例如：我方角色B 槽位2。
    public string GetOriginalTargetSlotText()
    {
        return GetTargetSlotText(originalTargetCharacter, originalTargetSlotIndex);
    }

    // GetActualTargetSlotText = 获取实际目标槽位文本
    // 例如：我方角色A 槽位1。
    public string GetActualTargetSlotText()
    {
        return GetTargetSlotText(actualTargetCharacter, actualTargetSlotIndex);
    }

    // GetTargetSlotText = 获取目标槽位文本
    // targetCharacter = 目标角色，slotIndex = 槽位编号。
    static string GetTargetSlotText(CharacterData targetCharacter, int slotIndex)
    {
        if (targetCharacter == null)
        {
            return "无目标";
        }

        return targetCharacter.characterName + " 槽位" + slotIndex;
    }
}
