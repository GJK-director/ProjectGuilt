\# 当前任务：能力型罪卡 Ability 基础流程



\## 当前目标



实现能力型罪卡的基础使用流程。



能力型罪卡规则：



1\. cardType = Ability

2\. isSinCard = true

3\. sinCardCategory = Ability

4\. 不参与拼点

5\. 使用后直接执行 effects

6\. 成功后增加 guiltGain

7\. 按 sinCardUseRule 处理 UseCount / Permanent

8\. 使用条件仍然走现有 SinCardConditionChecker

9\. 不要做负罪感阈值惩罚，阈值效果后期再议



\## 当前测试卡



CardsTest.json 里已经准备测试卡：



\- cardID: sin\_ability\_001

\- cardName: 罪卡测试：能力强化

\- cardType: Ability

\- isClashable: false

\- isSinCard: true

\- sinCardUseRule: UseCount

\- sinCardCategory: Ability

\- guiltGain: 2

\- maxUseCount: 2

\- effects: OnPlay 给自己添加 AbilityPower / 能力强化 Buff



\## 预期测试结果



第 1 次使用能力型罪卡：



\- 检查使用条件

\- 不进入拼点

\- 直接触发 OnPlay effects

\- 我方角色A获得“能力强化”

\- 负罪感 +2

\- 使用次数 1 / 2



第 2 次使用：



\- 再次成功

\- 负罪感 +2

\- 使用次数 2 / 2

\- 已消耗 True



第 3 次使用：



\- 被 CanUseCard 拦住

\- 不触发效果

\- 不增加负罪感

