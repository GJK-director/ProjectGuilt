// 脚本中文说明：战斗执行计划。负责保存本回合要按顺序处理的执行项列表和整体完成状态。
using System.Collections.Generic;

// BattleExecutionPlan = 战斗执行计划
// Execution = 执行，Plan = 计划。
// 它只保存“本回合要处理哪些执行项”，不负责真正打伤害。
public class BattleExecutionPlan
{
    // executionItems = 执行项列表
    // List<BattleExecutionItem> = 战斗执行项列表。
    // 这里保存本回合所有要处理的小步骤。
    public List<BattleExecutionItem> executionItems;

    // isCompleted = 整个执行计划是否已经完成
    // 只有里面所有执行项都处理完，整个计划才应该算完成。
    public bool isCompleted;

    // BattleExecutionPlan = 战斗执行计划构造函数
    // 创建执行计划时，先准备一个空的执行项列表。
    public BattleExecutionPlan()
    {
        executionItems = new List<BattleExecutionItem>();
        isCompleted = false;
    }

    // AddItem = 添加执行项
    // item = 要加入执行计划的一条 BattleExecutionItem。
    public void AddItem(BattleExecutionItem item)
    {
        // 空执行项没有意义，所以这里跳过 null。
        if (item != null)
        {
            executionItems.Add(item);
        }
    }
}
