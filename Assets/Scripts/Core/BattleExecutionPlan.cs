using System.Collections.Generic;

public class BattleExecutionPlan
{
    public List<BattleExecutionItem> executionItems;
    public bool isCompleted;

    public BattleExecutionPlan()
    {
        executionItems = new List<BattleExecutionItem>();
        isCompleted = false;
    }

    public void AddItem(BattleExecutionItem item)
    {
        if (item != null)
        {
            executionItems.Add(item);
        }
    }
}
