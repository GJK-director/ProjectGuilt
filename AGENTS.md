\# AGENTS.md



\## 项目说明



这是一个 Unity 回合制卡牌战斗原型项目。



当前重点是搭建战斗底层逻辑，包括：



\- 拼点

\- 卡牌 CD

\- Buff

\- 罪卡

\- 负罪感

\- 能力型罪卡



\## 协作规则



请小步修改，不要大范围重构。



每次开始实现前，先阅读：



\- Docs/BattleSystemSummary.md

\- Docs/CurrentTask.md



不要随便重命名已有类、字段、方法。



不要删除已有测试逻辑，除非任务明确要求。



尽量保持现有代码结构。



中文注释可以保留。



如果发现现有代码和任务描述冲突，先说明冲突，不要直接大改。



\## 当前重要类



\- CharacterData：角色 HP、速度、Buff、负罪感

\- CardTestData：卡牌 JSON 数据结构

\- CardDataLoader：读取和查找卡牌数据

\- BattleCardState：战斗中卡牌实例

\- BattleCardManager：卡牌使用检查、CD、消耗次数、卡牌状态打印

\- BattleResolver：拼点和战斗结算

\- BattleCalculator：伤害计算

\- BattleTargeting：目标与介入保护

\- BattleTurnProcessor：回合开始、回合结束、状态打印

\- CardEffectExecutor：执行卡牌效果

\- SinCardConditionChecker：罪卡使用条件检查

\- GuiltManager：负罪感管理

\- SinCardUseRule：罪卡使用规则

\- SinCardCategory：罪卡分类



\## 当前设计约束



负罪感不是消耗资源，而是从 0 开始累计增加。



使用罪卡会增加 guiltGain。



负罪感阈值效果暂时不做。



罪卡分为：



\- Clash：拼点型罪卡

\- Ability：能力型罪卡



罪卡使用规则分为：



\- UseCount：按次数消耗

\- Permanent：永久型罪卡



Ability 罪卡不应该参与拼点，应该直接执行 effects。

