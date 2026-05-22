# 当前任务：Action Slot 阶段收尾记录

## 一、Ability 罪卡基础流程已完成

当前已经完成 Ability 罪卡的基础使用流程：

- Ability 罪卡不进入拼点。
- 使用后直接执行 OnPlay effects。
- 成功使用后触发 Resolved。
- Resolved 后增加 guiltGain。
- UseCount 罪卡按次数消耗。
- AbilityUseCount 测试中，第 3 次使用会被 CanUseCard 拦住。

当前 Ability 测试卡：

- cardID: sin_ability_001
- cardType: Ability
- isSinCard: true
- sinCardCategory: Ability
- sinCardUseRule: UseCount
- maxUseCount: 2
- guiltGain: 2
- effects: OnPlay 给自己添加 AbilityPower / 能力强化 Buff

## 二、回归测试已完成

当前已经完成以下回归测试：

- Clash UseCount 测试通过。
- Permanent 罪卡回归测试通过。
- CardsTest.json 已恢复为 UseCount。
- 恢复后 UseCount 再验证通过。

当前稳定测试状态：

- atk_001 作为 Clash UseCount 测试卡。
- sin_ability_001 作为 Ability UseCount 测试卡。
- Permanent 暂时不作为常驻测试模式，需要时再通过临时测试数据验证。

## 三、测试工具优化已完成

CardLoadTest 已增加测试模式选择：

- 新增 BattleTestMode。
- Inspector 可选 ClashUseCount / AbilityUseCount。
- 不需要每次手动修改 Start() 测试入口。

当前测试模式：

- ClashUseCount：测试 atk_001 的拼点型罪卡 UseCount 流程。
- AbilityUseCount：测试 sin_ability_001 的能力型罪卡 UseCount 流程。

## 四、备注

AbilityUseCount 模式里，同一张罪卡状态会在 Console 里多次打印。

当前判断原因是：

- PrintCardStates 被多个测试打印点调用。
- PrintAbilitySinCardTestState 会打印一次 allyA 的卡牌状态。
- EndTurn 里也会打印一次 allyA 的卡牌状态。
- 因此同一张 sin_ability_001 状态会被重复输出。

当前判断不是重复创建卡牌实例。

暂时不修这个日志问题。后续如果 Console 太乱，再单独优化测试日志。

## 五、Action Slot 当前新方向

当前不采用行动点 Action Point。

当前采用行动槽位 Action Slot。

Action Slot 不是费用系统，而是玩家在准备阶段对敌人意图进行编排的槽位。

当前基础规则：

- 不做不同卡牌占用不同槽位数。
- 一张主动卡占用一个槽位。
- 同一张 BattleCardState 同一回合不能重复安排进多个槽位。
- 如果同回合出现多张同名卡，应理解为多个独立卡牌实例，而不是同一张卡重复使用。

## 六、敌人意图规则

当前敌人意图规则：

- 敌人先生成自己的行动意图。
- 当前第一版测试中的敌人意图是：enemy 使用 enemy_atk_001 攻击 allyB。
- 玩家可以选择响应敌人意图，也可以选择不响应，做自由行动。
- 敌人卡牌暂时不做 CD。
- 敌人行为以后由 AI / 关卡逻辑决定。
- 敌人防御逻辑暂时不考虑。

## 七、玩家槽位行动类型

当前玩家槽位行动类型：

- RespondToEnemyIntent：响应敌人意图，例如 allyA 使用 atk_001 介入保护 allyB。
- 响应敌人意图前会检查 BattleTargeting.CanInterceptAttack(...)。
- 速度不足时不能安排该响应，未来 UI 表现为该敌人意图位置灰色不可点击。
- FreeAction：不响应敌人意图，例如使用 Ability 罪卡。
- 第一版 FreeAction 只测试 sin_ability_001，不实现完整偷刀速度队列。

## 八、Action Slot 已完成的代码结构

当前已完成的代码结构：

- 新增 BattleEnemyIntent.cs。
- 新增 BattleActionSlot.cs。
- 新增 BattleActionSlotManager.cs。
- CardLoadTest.cs 新增 ActionSlotBasic 测试模式。
- BattleActionSlotManager 只负责创建槽位、安排行动、检查重复安排、打印槽位状态。
- 真正战斗结算仍在 CardLoadTest 测试流程中调用现有 BattleResolver 方法。
- 没有修改 BattleResolver.cs、BattleCardManager.cs、GuiltManager.cs、CardEffectExecutor.cs、CardsTest.json。

## 九、ActionSlotBasic 测试已通过

已通过的 ActionSlotBasic 测试：

- 成功创建敌人意图。
- 成功创建 2 个行动槽位。
- 槽位 1 成功安排 allyA 使用 atk_001 响应敌人意图。
- 同一张 allyAAttackCardState 再次安排到槽位 2 被阻止。
- 槽位 2 成功安排 allyA 使用 sin_ability_001 作为 FreeAction。
- 槽位 1 执行后进入 Clash 拼点流程，allyA 拼点胜利。
- 槽位 2 执行后 Ability 罪卡不进入拼点，触发 OnPlay 和 Resolved。
- 执行后 基础攻击 使用次数为 1 / 3。
- 执行后 罪卡测试：能力强化 使用次数为 1 / 2。

## 十、BattleEnemyIntent 目标槽位数据结构升级已完成

当前已完成小目标：

- BattleEnemyIntent 已从“攻击某个角色”升级为“攻击某个角色的某个槽位”。
- 原目标记录为 originalTargetCharacter。
- 原目标槽位记录为 originalTargetSlotIndex。
- 实际目标记录为 actualTargetCharacter。
- 实际目标槽位记录为 actualTargetSlotIndex。

当前测试设定：

- ActionSlotBasic 中，敌人意图固定为：enemy 使用 enemy_atk_001 攻击 allyB 的槽位2。
- 当 allyA 使用槽位1响应时，actualTarget 会从 allyB 槽位2 改为 allyA 槽位1。

已通过的测试日志：

- 成功打印：创建敌人意图：敌人 使用 敌人爪击 攻击 我方角色B 的槽位2。
- 成功打印：敌人意图目标从 我方角色B 槽位2 改为 我方角色A 槽位1。
- ActionSlotBasic 执行后，基础攻击和 Ability 罪卡仍正常进入原有 UseCount / Resolved 逻辑。

当前仍未实现：

- 敌人多意图队列。
- 防御 / 闪避。
- TargetedResponse / AutoResponse。
- 完整速度队列。
- 不可预测卡。
- 槽位 Buff。
- 特殊槽位。

## 十一、Action Slot / Enemy Intent 后续设计记录

当前阶段只记录以下设计规则，不实现。

### 1. 敌人意图目标槽位规则

敌人攻击不是简单攻击某个角色，而是攻击某个角色的某个槽位。

例如：

- enemy 使用 enemy_atk_001 攻击 allyB 的槽位2。

敌人卡本身不能被玩家改变。玩家能改变的是这张敌人卡的目标槽位 / 接战对象，以及它的处理顺序。

敌人意图后续应从简单的 originalTarget 扩展为：

- originalTargetCharacter
- originalTargetSlotIndex
- actualTargetCharacter
- actualTargetSlotIndex

actualTarget 表示这张敌人卡最终被谁的哪个槽位接走。

例如：

- 原本 enemy_atk_001 攻击 allyB 槽位2。
- allyA 速度大于 enemy，用 allyA 槽位1介入。
- 那么 actualTarget 从 allyB 槽位2 改为 allyA 槽位1。

### 2. 速度与目标改变规则

只有速度严格大于敌人，才能改变敌人卡的目标槽位和处理顺序。

- actorSpeed > enemySpeed：可以改变。
- actorSpeed == enemySpeed：不可以改变。
- actorSpeed < enemySpeed：不可以改变。

当敌人意图已经全部展示后，速度大于敌人的角色可以指定响应任意敌人顺序卡。

例如敌人顺序1、2、3都已展示，allyA 速度大于 enemy，则 allyA 可以选择响应顺序3。

指定响应可以改变该敌人卡的处理顺序和接战目标，但不能改变敌人使用的卡本身。

速度等于敌人时，不算速度优势，不能改变敌人卡目标，也不能改变敌人处理顺序。

### 3. 玩家槽位与攻击卡响应规则

槽位有序号，序号靠前的槽位优先行动。例如槽位1先于槽位2。

攻击卡不会自动响应敌人攻击。即使敌人攻击的是某个槽位，而该槽位里有攻击卡，也不会自动拼点。

攻击卡必须由玩家明确指定与某张敌人顺序卡拼点。

- 指定敌人顺序1，就只处理顺序1。
- 指定敌人顺序2，就只处理顺序2。
- 没有被指定处理的敌人顺序卡，如果没有其他防御/闪避等响应，就算未响应。

玩家也可以选择不拼点，改做 FreeAction，例如偷刀、使用能力卡、上 Buff 等。

FreeAction 不处理敌人意图，敌人原本的攻击仍然存在。

Ability 卡、Buff 卡、治疗卡，以及标记为无法拼点的攻击卡，都不会响应敌人拼点卡。

这些卡可以作为 FreeAction 执行，但不会阻止敌人攻击。

### 4. 空槽位规则

空槽位就是空，表示玩家没有安排行动。

- 空槽位不提供默认防御、默认闪避、自动格挡。
- 防御和闪避必须由玩家携带的卡牌提供，并可能有 CD 或其他限制。
- 如果敌人攻击的目标槽位无人响应，则敌人卡正常命中。
- 如果无人响应时有额外效果，应写在敌人卡效果里，例如“无人响应此卡时触发 X”，而不是写成空槽位自带规则。
- 空槽可能代表玩家轮转失败、没卡可用、主动保留资源，或选择承受伤害换取其他行动。

### 5. 响应 / 未响应判断规则

每张敌人卡是否被响应，需要单独判断。

只有玩家卡与敌人这张卡发生了有效逻辑处理，才算这张敌人卡被响应。

以下情况算响应：

- 攻击卡与敌人卡发生拼点。
- 介入后与敌人卡发生拼点。
- 防御卡处理了敌人攻击。
- 闪避卡处理了敌人攻击。
- 其他未来明确标记为响应的卡牌处理了敌人卡。

以下情况不算响应：

- 槽位为空。
- 槽位里是 Ability / Buff / 治疗等非响应卡。
- 槽位里是无法拼点的攻击卡。
- 玩家偷刀。
- 空挂防御/闪避最终没有处理任何敌人卡。

### 6. 防御卡 / 闪避卡后续规则

防御卡 / 闪避卡是响应类卡，但后续需要区分两种使用方式：

- TargetedResponse：指定响应。
- AutoResponse：空挂响应。

指定响应 TargetedResponse：

- 当我方角色速度大于敌人时，可以用某个槽位的防御/闪避，指定处理敌人的某一张攻击卡。
- 被指定的防御/闪避只处理指定的敌人卡。
- 如果指定处理顺序2，那么顺序1即使拼输，也不会触发这张指定给顺序2的防御/闪避。

空挂响应 AutoResponse：

- 防御/闪避可以不指定目标，作为空挂响应。
- 空挂响应会在敌人攻击真正生效时，自动处理该角色即将受到的第一张未被处理攻击卡。
- 如果空挂防御/闪避最终没有处理任何敌人卡，则算未使用，不进入 CD，不扣使用次数。
- 如果空挂防御/闪避处理了敌人卡，则算已使用，并进入对应 CD / 消耗逻辑。

空挂防御：

- 默认只处理一张敌人卡。
- 处理一次后视为已使用。
- 如果敌人顺序2仍攻击同一槽位，但这张防御已经处理过顺序1，则顺序2不自动被这张防御响应。

空挂闪避：

- 空挂闪避如果成功处理 / 拼过敌人顺序1，可以继续尝试处理敌人顺序2。
- 如果闪避是高速指定响应某个顺序，例如指定处理顺序2，则它只处理顺序2，不会自动处理顺序1。
- 闪避连续处理能力属于后续设计记录，当前不实现。

### 7. 执行顺序设计记录

战斗执行顺序需要综合：

- 敌人意图顺序。
- 玩家槽位序号。
- 玩家角色速度。
- 敌人速度。
- 玩家卡是否指定响应某张敌人顺序卡。

敌人意图顺序不是绝对不可变。

速度大于敌人的角色可以通过指定响应改变某张敌人卡的处理时机。

如果玩家速度大于敌人，并指定响应敌人顺序2，则该拼点可以先于敌人顺序1处理。

如果玩家速度低于敌人，并指定响应敌人顺序2，则需要先处理敌人顺序1，再处理玩家与顺序2的拼点。

速度相等时，玩家不能改变敌人目标和顺序。

### 8. 未来扩展：不可预测卡

后续可以加入“不可预测卡”机制。

不可预测卡会显示：

- 敌人卡的顺序。
- 攻击目标角色。
- 攻击目标槽位。

但不会显示：

- 具体卡牌内容。
- 点数。
- 特殊效果。
- 无人响应惩罚等。

不可预测卡仍然可以被速度大于敌人的角色指定响应，并改变攻击顺序和目标。

但玩家需要承担未知卡牌效果的风险。

当前只记录，不实现。

### 9. 未来扩展：槽位 Buff / 特殊槽位

槽位本身未来也可以成为卡牌效果作用对象。

后续可以存在 Slot Buff / 槽位状态，例如：

- 槽位1拼点上升。
- 槽位2拼点下降。
- UI 上槽位1变红。
- 槽位被污染、强化、锁定等。

后续也可以存在特殊槽位，例如：

- 蓝色拓展槽位：只能放防御卡。
- 黄色先攻槽位：回合开始第一个行动，但不能拼点，只能给自己上 Buff 或偷刀。

这些都属于后续扩展方向，当前阶段只记录，不实现。

### 10. 当前阶段不实现内容

当前只做设计记录，不实现以下功能：

- 不实现敌人多意图队列。
- 不实现目标槽位正式战斗结算系统。
- 不实现 TargetedResponse / AutoResponse 代码。
- 不实现防御 / 闪避结算。
- 不实现完整速度队列。
- 不实现不可预测卡。
- 不实现槽位 Buff。
- 不实现特殊槽位。
- 不修改当前 ActionSlotBasic 测试。
- 不修改 BattleResolver、BattleCardManager、GuiltManager、CardEffectExecutor、CardsTest.json。

## 十二、下一步候选方向

后续候选方向：

- 继续完善敌人意图系统。
- 设计玩家“响应敌人意图”和“偷刀”的正式执行顺序。
- 后续再考虑速度队列。
- 后续再考虑 UI 拖拽和槽位显示。
- 敌人防御逻辑暂缓。
- 负罪感阈值暂缓。

## 十三、当前规则提醒

- 负罪感不是消耗资源，而是从 0 开始累计增加。
- 使用罪卡会增加 guiltGain。
- 负罪感阈值惩罚暂时不做。
- 罪卡分为 Clash 和 Ability。
- Clash 罪卡参与拼点。
- Ability 罪卡不参与拼点，直接执行 effects。
- UseCount 罪卡按次数消耗。
- Permanent 罪卡不进入普通 CD，也不按次数消耗。
