\# 战斗系统当前进度总结



\## 一、项目目标



这是一个 Unity 回合制卡牌战斗原型项目。



当前目标是搭建战斗底层逻辑，包括：



\- 角色 HP

\- 速度

\- 介入保护

\- 卡牌读取

\- 卡牌实例状态

\- 拼点

\- 伤害

\- Buff

\- 卡牌效果

\- CD

\- 罪卡

\- 负罪感

\- 能力型罪卡



目前重点是继续实现：能力型罪卡 Ability 的基础使用流程。



\---



\## 二、角色系统



角色数据主要由 CharacterData 管理。



当前角色拥有：



\- characterName：角色名

\- maxHP：最大生命值

\- currentHP：当前生命值

\- 速度范围

\- 当前回合速度

\- buffs：当前状态列表

\- pendingBuffs：待生效状态列表

\- battleCards：战斗卡牌列表

\- currentGuilt：当前负罪感



当前支持：



\- 受到伤害

\- 回合开始投掷速度

\- 获得 Buff

\- 获得待生效 Buff

\- 回合开始处理待生效 Buff

\- 回合结束处理 Buff 持续时间

\- 打印当前状态

\- 打印当前负罪感



\---



\## 三、速度与介入保护



当前有基础介入保护逻辑：



敌人原本攻击我方角色B。



如果我方角色A速度高于敌人，则我方角色A可以介入保护我方角色B。



也就是：



\- 原始目标：我方角色B

\- 实际接战者：我方角色A



相关类：



\- BattleTargeting



\---



\## 四、卡牌数据系统



卡牌数据通过 JSON 读取。



主要数据类：



\- CardTestData

\- CardDataLoader



当前卡牌字段包括：



\- cardID

\- cardName

\- rarity

\- cardType

\- isClashable

\- minPoint

\- maxPoint

\- cooldown

\- damageFormula

\- defenseFormula

\- effects

\- useConditions



当前卡牌类型包括：



\- Attack

\- Defense

\- Dodge

\- Ability



\---



\## 五、战斗卡牌状态



卡牌模板进入战斗后，会创建 BattleCardState。



BattleCardState 负责记录战斗中的卡牌实例状态。



当前记录：



\- owner：持有者

\- cardData：原始卡牌数据

\- instanceID：实例ID

\- currentCooldown：当前CD

\- currentUseCount：当前使用次数

\- maxUseCount：最大使用次数

\- isConsumed：是否已经消耗

\- sinCardUseRule：罪卡使用规则

\- sinCardCategory：罪卡分类



这样同一张卡牌模板可以创建多个战斗实例。



\---



\## 六、拼点系统



当前已经实现基础拼点逻辑。



目前主要测试：



\- Attack vs Attack



大致流程：



1\. 双方进入拼点

2\. 检查 ClashStart 阶段 Buff

3\. 计算双方点数

4\. 比较点数

5\. 判断胜利 / 失败

6\. 胜利方造成伤害

7\. 触发相关效果



当前有拼点结果：



\- Win

\- Lose



后续需要继续支持：



\- 拼点胜利效果

\- 拼点失败效果

\- 拼点失败奖励

\- 拼点相关 CD 逻辑



\---



\## 七、伤害系统



伤害主要由 BattleCalculator 处理。



当前已有伤害公式：



\- PointAsDamage

\- DoublePointDamage



当前伤害支持倍率计算。



例如：



\- 基础伤害内部值：2000

\- 伤害倍率：110%

\- 最终伤害内部值：2200

\- 最终造成 22 点伤害



\---



\## 八、Buff 系统



Buff 当前由 CharacterData 管理。



BuffData 当前包含：



\- buffID

\- buffName

\- buffCategory

\- stack

\- duration

\- checkTiming

\- expireRule



当前支持：



\- 立即获得 Buff

\- 延迟获得 Buff

\- 回合开始处理待生效 Buff

\- 回合结束减少持续时间

\- 打印当前 Buff

\- 打印待生效 Buff



当前测试过的 Buff：



\- Bullet / 子弹

\- Strength / 强壮

\- DamageUp / 伤害提升

\- AbilityPower / 能力强化



\---



\## 九、卡牌效果系统



卡牌效果由 CardEffectExecutor 执行。



当前 effects 支持不同 trigger。



已经用到的触发时机：



\- OnPlay

\- AfterDamage

\- Clash

\- Resolved

\- ClashWin

\- ClashLose



当前效果类型包括：



\- ApplyBuff

\- ReduceCooldown



后续可以继续扩展：



\- 回复 HP

\- 增加速度

\- 改变拼点点数

\- 改变伤害

\- 改变回合数

\- 操作卡牌 CD

\- 操作负罪感



\---



\## 十、CD 系统



普通卡牌有 CD。



普通卡结算后可以进入冷却。



CD 可以通过效果减少。



当前设计重点：



CD 逻辑不能只绑定“是否参与拼点”。



后续需要支持：



\- 拼点胜利后减少 CD

\- 拼点失败后减少 CD

\- 给自己减少 CD

\- 给指定卡牌减少 CD

\- 给全部卡牌减少 CD



\---



\## 十一、罪卡系统



罪卡是当前重点系统。



\### 1. 负罪感规则



负罪感不是消耗资源。



负罪感从 0 开始累计增加。



使用罪卡后增加 guiltGain。



当前规则：



\- 战斗开始：负罪感 = 0

\- 使用罪卡成功结算后：负罪感 + guiltGain

\- 使用失败、条件不满足、卡牌已消耗时，不增加负罪感



负罪感阈值效果暂时不做，后期再议。



相关类：



\- GuiltManager



\---



\### 2. 罪卡使用条件



当前已经支持 useConditions。



已完成条件：



\- HasBuff

\- BuffStackAtLeast

\- HpAbovePercent

\- HpBelowPercent

\- GuiltAtLeast

\- GuiltBelow



相关类：



\- SinCardConditionChecker



\---



\### 3. 罪卡使用规则



当前有 SinCardUseRule。



规则包括：



\- UseCount

\- Permanent



UseCount：



\- 使用成功后增加使用次数

\- 达到 maxUseCount 后，本场战斗不能再使用



Permanent：



\- 不增加使用次数

\- 不进入普通 CD

\- 不会因为次数消耗

\- 仍然会增加负罪感



\---



\### 4. 罪卡分类



当前有 SinCardCategory。



分类包括：



\- Clash

\- Ability



Clash：



\- 拼点型罪卡

\- 参与拼点

\- 类似攻击、防御、闪避卡



Ability：



\- 能力型罪卡

\- 不一定参与拼点

\- 主要通过 effects 影响 Buff、属性、速度、伤害、CD 等



\---



\## 十二、当前重要类



当前主要类：



\- CharacterData

\- CardTestData

\- CardDataLoader

\- BattleCardState

\- BattleCardManager

\- BattleResolver

\- BattleCalculator

\- BattleTargeting

\- BattleTurnProcessor

\- CardEffectExecutor

\- SinCardConditionChecker

\- GuiltManager

\- SinCardUseRule

\- SinCardCategory



\---



\## 十三、已经完成的功能



已完成：



\- 角色 HP

\- 速度投掷

\- 速度介入保护

\- JSON 读取卡牌

\- 战斗卡牌实例化

\- Attack vs Attack 拼点

\- 拼点胜负判断

\- 基础伤害结算

\- 伤害倍率计算

\- Buff 添加

\- Buff 持续时间处理

\- 延迟 Buff

\- 卡牌效果触发

\- 普通 CD 基础逻辑

\- 罪卡 UseCount

\- 罪卡 Permanent

\- 负罪感增加

\- 负罪感显示

\- 罪卡使用条件

\- 罪卡规则显示

\- 罪卡分类显示



\---



\## 十四、当前正在做的功能



当前正在做：



能力型罪卡 Ability 基础流程。



目标：



1\. Ability 罪卡不参与拼点

2\. 使用后直接触发 OnPlay effects

3\. 成功后增加 guiltGain

4\. 按 sinCardUseRule 处理 UseCount / Permanent

5\. 使用条件继续沿用 CanUseCard / SinCardConditionChecker

6\. 不做负罪感阈值惩罚

7\. 不大范围重构



当前测试卡：



\- sin\_ability\_001

\- 罪卡测试：能力强化

\- cardType = Ability

\- sinCardCategory = Ability

\- sinCardUseRule = UseCount

\- maxUseCount = 2

\- guiltGain = 2

