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

## 十一、ActionSlotInterceptFail 速度不足测试已完成

当前已完成小目标：

- 新增测试模式 ActionSlotInterceptFail。
- 用于验证速度不足时不能改变敌人卡牌目标。
- 该测试只验证安排阶段失败，不执行拼点，不触发 Resolved。

测试设定：

- 创建低速角色 slowAlly / 低速角色。
- 低速角色速度固定为 3-3。
- 敌人速度高于低速角色。
- 敌人意图为：enemy 使用 enemy_atk_001 攻击 allyB 的槽位2。
- 低速角色尝试用槽位1和 atk_001 响应敌人意图。

已通过的测试结果：

- 成功打印：低速角色 当前速度：3；敌人 当前速度：7。
- 成功打印：速度不足，无法改变该敌人卡牌目标。
- 成功打印：低速角色响应敌人意图失败，未执行拼点。
- 成功打印：敌人意图实际目标仍为：我方角色B 槽位2。
- 槽位1、槽位2均保持为空。
- 低速角色的 基础攻击 使用次数仍为 0 / 3。
- 没有触发拼点。
- 没有触发 Resolved。
- 没有增加负罪感。
- 没有消耗卡牌。

该测试验证的设计规则：

- 只有速度严格大于敌人时，才能改变敌人卡的目标槽位。
- 速度小于敌人时，不能改变目标。
- 速度不足时，响应安排失败。
- 响应安排失败不会把卡写入行动槽位。
- 响应安排失败不会消耗卡牌。

当前仍未实现：

- 敌人多意图队列。
- 防御 / 闪避。
- TargetedResponse / AutoResponse。
- 完整速度队列。
- 不可预测卡。
- 槽位 Buff。
- 特殊槽位。

## 十二、ActionSlotInterceptEqualFail 速度相等测试已完成

当前已完成小目标：

- 新增测试模式 ActionSlotInterceptEqualFail。
- 用于验证速度相等时不能改变敌人卡牌目标。
- 该测试只验证安排阶段失败，不执行拼点，不触发 Resolved。

测试设定：

- 创建同速角色 sameSpeedAlly / 同速角色。
- 同速角色速度固定为 6-6。
- 敌人速度也固定为 6-6。
- 敌人意图为：enemy 使用 enemy_atk_001 攻击 allyB 的槽位2。
- 同速角色尝试用槽位1和 atk_001 响应敌人意图。

已通过的测试结果：

- 成功打印：同速角色 当前速度：6；敌人 当前速度：6。
- 成功打印：速度不足，无法改变该敌人卡牌目标。
- 成功打印：同速角色响应敌人意图失败，未执行拼点。
- 成功打印：敌人意图实际目标仍为：我方角色B 槽位2。
- 槽位1、槽位2均保持为空。
- 同速角色的 基础攻击 使用次数仍为 0 / 3。
- 没有触发拼点。
- 没有触发 Resolved。
- 没有增加负罪感。
- 没有消耗卡牌。

该测试验证的设计规则：

- 只有速度严格大于敌人时，才能改变敌人卡的目标槽位。
- 速度等于敌人时，不能改变目标。
- 速度相等时，响应安排失败。
- 响应安排失败不会把卡写入行动槽位。
- 响应安排失败不会消耗卡牌。

当前速度规则测试状态：

- ActionSlotBasic 已验证：速度大于敌人时，可以成功响应并改变 actualTarget。
- ActionSlotInterceptFail 已验证：速度小于敌人时，不能响应，actualTarget 不变。
- ActionSlotInterceptEqualFail 已验证：速度等于敌人时，不能响应，actualTarget 不变。

当前仍未实现：

- 敌人多意图队列。
- 防御 / 闪避。
- TargetedResponse / AutoResponse。
- 完整速度队列。
- 不可预测卡。
- 槽位 Buff。
- 特殊槽位。

## 十三、CardLoadTest 中 Action Slot 测试区域已完成轻量整理

当前已完成小目标：

- 轻量整理 CardLoadTest.cs 中的 Action Slot 测试区域。
- 新增 Action Slot 测试相关注释分区。
- 提取少量私有辅助方法，减少重复代码。
- 不改变任何测试模式，不改变测试结果。

本次整理内容：

- 新增注释分区：// Action Slot 测试流程。
- 新增注释分区：// Action Slot 测试辅助方法。
- 新增注释分区：// Action Slot 执行辅助方法。
- 新增私有辅助方法：CreateTestAttackCardForCharacter(CharacterData owner, string instanceID)。
- 新增私有辅助方法：PrintEnemyIntentActualTarget(BattleEnemyIntent enemyIntent)。
- 新增私有辅助方法：PrintCharacterCardStates(CharacterData character)。

已确认保持不变：

- ClashUseCount。
- AbilityUseCount。
- ActionSlotBasic。
- ActionSlotInterceptFail。
- ActionSlotInterceptEqualFail。
- Start() 分支顺序。
- StartTurn() 调用位置。
- BattleResolver.TestClash(...) 调用。
- BattleResolver.TestUseAbilitySinCard(...) 调用。
- AssignResponseToEnemyIntent(...) 调用参数。
- AssignFreeAction(...) 调用参数。

已通过 Unity 轻量回归：

- ActionSlotBasic 通过：速度大于敌人时，槽位1可以响应敌人意图。
- ActionSlotBasic 通过：actualTarget 从 allyB 槽位2 改为 allyA 槽位1。
- ActionSlotBasic 通过：槽位1拼点正常。
- ActionSlotBasic 通过：槽位2 Ability 罪卡正常执行。
- ActionSlotBasic 通过：基础攻击和 Ability 罪卡 UseCount 正常增加。
- ActionSlotInterceptFail 通过：速度低于敌人时，响应失败。
- ActionSlotInterceptFail 通过：actualTarget 仍为 allyB 槽位2。
- ActionSlotInterceptFail 通过：槽位保持为空。
- ActionSlotInterceptFail 通过：卡牌使用次数仍为 0 / 3。
- ActionSlotInterceptEqualFail 通过：速度等于敌人时，响应失败。
- ActionSlotInterceptEqualFail 通过：actualTarget 仍为 allyB 槽位2。
- ActionSlotInterceptEqualFail 通过：槽位保持为空。
- ActionSlotInterceptEqualFail 通过：卡牌使用次数仍为 0 / 3。

当前结论：

- 本次整理只影响测试代码可读性。
- 没有改变战斗规则。
- 没有改变测试结果。
- Action Slot 相关基础测试整理后仍通过。

当前仍未实现：

- 敌人多意图队列。
- 防御 / 闪避。
- TargetedResponse / AutoResponse。
- 完整速度队列。
- 不可预测卡。
- 槽位 Buff。
- 特殊槽位。
- 目标槽位正式战斗结算系统。

## 十四、BattleActionSlot.isUsed 槽位使用状态已完成

当前已完成小目标：

- BattleActionSlot 新增 isUsed 字段。
- 新增 MarkUsed() 方法。
- 槽位被安排响应行动或自由行动时，isUsed 初始化为 false。
- Clear() 时重置 isUsed = false。
- 执行成功后标记 isUsed = true。

修改范围记录：

- BattleActionSlot.cs 新增 public bool isUsed;。
- BattleActionSlot.cs 新增 MarkUsed()。
- BattleActionSlot.cs 中 AssignResponse(...) / AssignFreeAction(...) 初始化未使用状态。
- BattleActionSlot.cs 中 Clear() 重置未使用状态。
- BattleActionSlotManager.cs 中 PrintSlotStates(...) 非空槽位日志追加 已使用：True/False。
- BattleActionSlotManager.cs 中空槽仍保持 槽位 X：空。
- CardLoadTest.cs 中 ExecuteResponseActionSlot(...) 成功调用 BattleResolver.TestClash(...) 后调用 actionSlot.MarkUsed()。
- CardLoadTest.cs 中 ExecuteFreeActionSlot(...) 成功调用 BattleResolver.TestUseAbilitySinCard(...) 后调用 actionSlot.MarkUsed()。

isUsed = true 的时机：

- 响应槽位成功执行 Clash 后。
- FreeAction 中 Ability 罪卡成功执行后。

isUsed 保持 false 的情况：

- 新建槽位后。
- 刚安排卡牌但还没执行。
- Clear() 后。
- 响应安排失败。
- CanUseCard 检查失败。
- FreeAction 不是 Ability 罪卡、未执行时。

已通过 Unity 测试：

- ActionSlotBasic 通过：执行前槽位1、槽位2均显示 已使用：False。
- ActionSlotBasic 通过：执行后槽位1、槽位2均显示 已使用：True。
- ActionSlotBasic 通过：响应槽位拼点正常。
- ActionSlotBasic 通过：Ability 罪卡正常执行。
- ActionSlotBasic 通过：UseCount / guiltGain 仍正常。
- ActionSlotInterceptFail 通过：速度低于敌人时安排失败。
- ActionSlotInterceptFail 通过：槽位1、槽位2保持为空。
- ActionSlotInterceptFail 通过：不出现 已使用：True。
- ActionSlotInterceptFail 通过：卡牌使用次数仍为 0 / 3。
- ActionSlotInterceptEqualFail 通过：速度等于敌人时安排失败。
- ActionSlotInterceptEqualFail 通过：槽位1、槽位2保持为空。
- ActionSlotInterceptEqualFail 通过：不出现 已使用：True。
- ActionSlotInterceptEqualFail 通过：卡牌使用次数仍为 0 / 3。

当前结论：

- isUsed 目前只作为 Action Slot 数据状态。
- 当前不参与正式战斗结算。
- 当前不处理“已使用槽位再次被敌人攻击”的完整规则。
- 没有改变战斗核心逻辑。
- 没有改变现有测试结果。

当前仍未实现：

- 已使用槽位再次被敌人攻击的正式结算。
- 敌人多意图队列。
- 防御 / 闪避。
- TargetedResponse / AutoResponse。
- 完整速度队列。
- 不可预测卡。
- 槽位 Buff。
- 特殊槽位。
- 目标槽位正式战斗结算系统。

## 十五、ActionSlotMultiIntentBasic 多敌人意图基础测试已完成

当前已完成小目标：

- BattleEnemyIntent 新增 intentOrder。
- 敌人意图编号从 1 开始。
- 新增轻量数据管理类 BattleEnemyIntentManager。
- 新增测试模式 ActionSlotMultiIntentBasic。
- 用于验证一回合多个敌人意图的数据绑定和目标改写。

修改范围记录：

- BattleEnemyIntent.cs 新增 public int intentOrder;。
- BattleEnemyIntent.cs 构造函数新增可选参数 int intentOrder = 1。
- BattleEnemyIntent.cs 旧测试默认仍为敌人意图 1。
- BattleEnemyIntentManager.cs 新增 CreateIntentQueue(...)。
- BattleEnemyIntentManager.cs 新增 FindIntentByOrder(...)。
- BattleEnemyIntentManager.cs 新增 PrintIntentQueue(...)。
- BattleEnemyIntentManager.cs 新增 PrintIntentState(...)。
- BattleEnemyIntentManager.cs 当前只做数据创建、保存、打印、按编号查找。
- CardLoadTest.cs 新增测试模式 ActionSlotMultiIntentBasic。
- CardLoadTest.cs 新增 RunActionSlotMultiIntentBasicTestSequence()。

测试设定：

- 敌人意图1：enemy_atk_001 攻击 allyB 槽位2。
- 敌人意图2：enemy_atk_001_copy_1 攻击 allyB 槽位1。
- allyA 使用槽位1和 atk_001 指定响应敌人意图2。

已通过 Unity 测试结果：

- 成功创建敌人意图队列，数量为 2。
- 初始状态：敌人意图1 原目标 / 实际目标均为 allyB 槽位2。
- 初始状态：敌人意图2 原目标 / 实际目标均为 allyB 槽位1。
- 成功通过 FindIntentByOrder(intentQueue, 2) 找到敌人意图2。
- allyA 成功用槽位1响应敌人意图2。
- 响应后：敌人意图1 实际目标仍为 allyB 槽位2。
- 响应后：敌人意图2 实际目标从 allyB 槽位1 改为 allyA 槽位1。
- 槽位1非空，绑定敌人意图2。
- 槽位1 isUsed 仍为 False。
- 槽位2为空。
- allyA 的基础攻击使用次数仍为 0 / 3。
- Ability 罪卡使用次数仍为 0 / 2。

当前结论：

- 多敌人意图队列的最小数据层已通过。
- 敌人意图编号从 1 开始可用。
- 玩家槽位可以指定响应某一个敌人意图。
- 当前只验证数据绑定和目标改写。
- 当前不执行拼点。
- 当前不触发 Resolved。
- 当前不增加负罪感。
- 当前不增加 UseCount。
- 当前不标记 slot.isUsed = true。

当前仍未实现：

- 敌人多意图正式结算。
- 完整速度队列。
- 敌人意图提前 / 延后处理。
- 防御 / 闪避。
- TargetedResponse / AutoResponse。
- 不可预测卡。
- 槽位 Buff。
- 特殊槽位。
- 已使用槽位再次被敌人攻击的正式结算。

## 十六、BattleEnemyIntent.isResponded 敌人意图响应状态已完成

当前已完成小目标：

- BattleEnemyIntent 新增 isResponded 字段。
- 新增 MarkResponded() 方法。
- 新建敌人意图时默认 isResponded = false。
- 玩家槽位成功响应某个敌人意图后，该意图标记为 isResponded = true。
- 敌人意图队列打印时显示 已响应：True/False。

修改范围记录：

- BattleEnemyIntent.cs 新增 public bool isResponded;。
- BattleEnemyIntent.cs 构造函数中初始化 isResponded = false。
- BattleEnemyIntent.cs 新增 MarkResponded()。
- BattleActionSlotManager.cs 在 AssignResponseToEnemyIntent(...) 成功调用 slot.AssignResponse(...) 后调用 enemyIntent.MarkResponded()。
- BattleActionSlotManager.cs 失败分支不标记。
- BattleEnemyIntentManager.cs 中 PrintIntentState(...) 追加打印 已响应：True/False。

isResponded = true 的时机：

- 只有玩家槽位成功安排响应敌人意图后才标记。
- 速度检查通过。
- 槽位检查通过。
- 重复卡检查通过。
- 敌人意图数据完整。
- 成功执行 slot.AssignResponse(...) 后。

isResponded 保持 false 的情况：

- 新建敌人意图后。
- 速度不足时。
- 速度相等时。
- 槽位已有行动时。
- 同一张卡重复安排失败时。
- 敌人意图数据不完整时。
- 没有玩家槽位指定响应该意图时。

已通过 Unity 测试：

- ActionSlotMultiIntentBasic 通过：响应前，敌人意图1 已响应：False。
- ActionSlotMultiIntentBasic 通过：响应前，敌人意图2 已响应：False。
- ActionSlotMultiIntentBasic 通过：allyA 槽位1指定响应敌人意图2后，敌人意图1 仍为 已响应：False。
- ActionSlotMultiIntentBasic 通过：allyA 槽位1指定响应敌人意图2后，敌人意图2 变为 已响应：True。
- ActionSlotMultiIntentBasic 通过：敌人意图2 actualTarget 从 allyB 槽位1 改为 allyA 槽位1。
- ActionSlotMultiIntentBasic 通过：槽位1 isUsed 仍为 False。
- ActionSlotMultiIntentBasic 通过：基础攻击使用次数仍为 0 / 3。
- ActionSlotMultiIntentBasic 通过：Ability 罪卡使用次数仍为 0 / 2。
- ActionSlotInterceptFail 通过：速度低于敌人时安排失败。
- ActionSlotInterceptFail 通过：槽位保持为空。
- ActionSlotInterceptFail 通过：卡牌使用次数仍为 0 / 3。
- ActionSlotInterceptFail 通过：不会误标记响应。
- ActionSlotInterceptEqualFail 通过：速度等于敌人时安排失败。
- ActionSlotInterceptEqualFail 通过：槽位保持为空。
- ActionSlotInterceptEqualFail 通过：卡牌使用次数仍为 0 / 3。
- ActionSlotInterceptEqualFail 通过：不会误标记响应。

当前结论：

- isResponded 目前只作为敌人意图数据状态。
- 当前不参与正式战斗结算。
- 当前不限制“已响应意图是否还能再次响应”。
- 当前不处理无人响应效果。
- 没有改变拼点、负罪感、UseCount、CD、Resolved 等核心逻辑。

当前仍未实现：

- 已响应意图不能再次响应的限制规则。
- 敌人多意图正式结算。
- 完整速度队列。
- 敌人意图提前 / 延后处理。
- 防御 / 闪避。
- TargetedResponse / AutoResponse。
- 无人响应效果。
- 不可预测卡。
- 槽位 Buff。
- 特殊槽位。
- 已使用槽位再次被敌人攻击的正式结算。

## 十七、PrintUnrespondedIntents 未响应敌人意图日志工具已完成

当前已完成小目标：

- BattleEnemyIntentManager 新增 PrintUnrespondedIntents(List<BattleEnemyIntent> intentQueue)。
- 用于打印当前敌人意图队列中尚未被玩家槽位响应的敌人意图。
- 该方法只做日志输出，不修改任何状态，不进入正式结算。

修改范围记录：

- BattleEnemyIntentManager.cs 新增 PrintUnrespondedIntents(...)。
- BattleEnemyIntentManager.cs 打印标题：===== 当前未响应敌人意图 =====。
- BattleEnemyIntentManager.cs 队列为空时打印：当前没有敌人意图。
- BattleEnemyIntentManager.cs 遍历队列，只打印 isResponded == false 的意图。
- BattleEnemyIntentManager.cs 复用现有 PrintIntentState(...)。
- BattleEnemyIntentManager.cs 如果没有未响应意图，打印：当前没有未响应敌人意图。
- CardLoadTest.cs 在 RunActionSlotMultiIntentBasicTestSequence() 中追加调用 BattleEnemyIntentManager.PrintUnrespondedIntents(intentQueue);。
- CardLoadTest.cs 调用位置在响应后完整队列打印之后、槽位状态打印之前。

已通过 Unity 测试：

- ActionSlotMultiIntentBasic 通过：完整队列显示敌人意图1 已响应：False。
- ActionSlotMultiIntentBasic 通过：完整队列显示敌人意图2 已响应：True。
- ActionSlotMultiIntentBasic 通过：新增的未响应列表只打印敌人意图1。
- ActionSlotMultiIntentBasic 通过：新增的未响应列表不打印敌人意图2。
- ActionSlotMultiIntentBasic 通过：槽位1仍显示 已使用：False。
- ActionSlotMultiIntentBasic 通过：基础攻击使用次数仍为 0 / 3。
- ActionSlotMultiIntentBasic 通过：Ability 罪卡使用次数仍为 0 / 2。

当前结论：

- 当前可以通过日志快速查看哪些敌人意图仍未被玩家响应。
- 该工具为后续无人响应效果、剩余敌人意图结算、防御/闪避自动处理未响应意图提供数据观察基础。
- 当前仍然只做数据层日志，不执行任何敌人意图。

当前不实现：

- 未响应敌人意图正式结算。
- 敌人攻击执行。
- 无人响应效果。
- 防御 / 闪避自动响应。
- 完整速度队列。
- isCompleted。
- Resolved。
- 负罪感增加。
- UseCount 增加。
- slot.MarkUsed()。

## 十八、GetUnrespondedIntents 未响应敌人意图查询方法已完成

当前已完成小目标：

- BattleEnemyIntentManager 新增 GetUnrespondedIntents(List<BattleEnemyIntent> intentQueue)。
- 用于返回当前敌人意图队列中 isResponded == false 的敌人意图列表。
- 让系统不只可以打印未响应敌人意图，也可以拿到未响应敌人意图数据。
- 当前只做数据层查询，不执行任何敌人意图。

修改范围记录：

- BattleEnemyIntentManager.cs 新增 GetUnrespondedIntents(...)。
- BattleEnemyIntentManager.cs 返回新的 List<BattleEnemyIntent>。
- BattleEnemyIntentManager.cs 只收集 intent != null && !intent.isResponded 的敌人意图。
- BattleEnemyIntentManager.cs null 或空队列返回空列表。
- BattleEnemyIntentManager.cs 不修改任何状态。
- BattleEnemyIntentManager.cs 不执行任何敌人意图。
- BattleEnemyIntentManager.cs 中 PrintUnrespondedIntents(...) 改为复用 GetUnrespondedIntents(...)。
- BattleEnemyIntentManager.cs 保持原有日志语义：队列为空打印 当前没有敌人意图；无未响应意图打印 当前没有未响应敌人意图。
- CardLoadTest.cs 在 RunActionSlotMultiIntentBasicTestSequence() 中轻量验证新方法。
- CardLoadTest.cs 响应后完整队列打印之后调用 GetUnrespondedIntents(intentQueue)。
- CardLoadTest.cs 打印 当前未响应敌人意图数量：1。
- CardLoadTest.cs 随后继续调用 PrintUnrespondedIntents(intentQueue)。

已通过 Unity 测试：

- ActionSlotMultiIntentBasic 通过：完整队列显示敌人意图1 已响应：False。
- ActionSlotMultiIntentBasic 通过：完整队列显示敌人意图2 已响应：True。
- ActionSlotMultiIntentBasic 通过：数量日志显示 当前未响应敌人意图数量：1。
- ActionSlotMultiIntentBasic 通过：未响应列表只打印敌人意图1。
- ActionSlotMultiIntentBasic 通过：未响应列表不打印敌人意图2。
- ActionSlotMultiIntentBasic 通过：槽位1仍显示 已使用：False。
- ActionSlotMultiIntentBasic 通过：基础攻击使用次数仍为 0 / 3。
- ActionSlotMultiIntentBasic 通过：Ability 罪卡使用次数仍为 0 / 2。

当前结论：

- 当前已经可以通过 GetUnrespondedIntents(...) 拿到未响应敌人意图列表。
- PrintUnrespondedIntents(...) 现在基于查询结果进行打印。
- 该方法为后续未响应敌人意图正式结算、无人响应效果、防御/闪避自动处理未响应意图提供数据入口。
- 当前仍然只做数据层查询，不执行这些意图。

当前不实现：

- 未响应敌人意图正式结算。
- 敌人攻击执行。
- 无人响应效果。
- 防御 / 闪避自动响应。
- 完整速度队列。
- isCompleted。
- Resolved。
- 负罪感增加。
- UseCount 增加。
- slot.MarkUsed()。

## 十九、PrintIntentHandlingPreview 敌人意图处理预览日志工具已完成

当前已完成小目标：

- BattleEnemyIntentManager 新增 PrintIntentHandlingPreview(List<BattleEnemyIntent> intentQueue)。
- 用于根据当前敌人意图队列打印每个敌人意图未来可能进入的处理路径。
- 当前只做数据层日志预览，不执行任何敌人意图。

修改范围记录：

- BattleEnemyIntentManager.cs 新增 PrintIntentHandlingPreview(...)。
- BattleEnemyIntentManager.cs 队列为空或 null 时打印：当前没有敌人意图，无法生成处理预览。
- BattleEnemyIntentManager.cs 遍历敌人意图队列。
- BattleEnemyIntentManager.cs 在 isResponded == false 时打印未响应路径。
- BattleEnemyIntentManager.cs 在 isResponded == true 时打印已响应路径。
- BattleEnemyIntentManager.cs 只读取 isResponded、intentOrder、actualTarget 文本。
- BattleEnemyIntentManager.cs 不修改任何字段。
- BattleEnemyIntentManager.cs 不执行任何敌人意图。
- CardLoadTest.cs 在 RunActionSlotMultiIntentBasicTestSequence() 中追加调用 BattleEnemyIntentManager.PrintIntentHandlingPreview(intentQueue);。
- CardLoadTest.cs 调用位置在响应后完整队列打印、未响应数量打印、未响应列表打印之后，槽位状态打印之前。

已通过 Unity 测试：

- ActionSlotMultiIntentBasic 通过：完整队列显示敌人意图1 已响应：False。
- ActionSlotMultiIntentBasic 通过：完整队列显示敌人意图2 已响应：True。
- ActionSlotMultiIntentBasic 通过：未响应数量显示 当前未响应敌人意图数量：1。
- ActionSlotMultiIntentBasic 通过：未响应列表只打印敌人意图1。
- ActionSlotMultiIntentBasic 通过：处理预览显示敌人意图1 未响应，未来将按当前 actualTarget 执行，目标为 allyB 槽位2。
- ActionSlotMultiIntentBasic 通过：处理预览显示敌人意图2 已响应，未来将进入玩家响应处理，当前实际目标为 allyA 槽位1。
- ActionSlotMultiIntentBasic 通过：槽位1仍显示 已使用：False。
- ActionSlotMultiIntentBasic 通过：基础攻击使用次数仍为 0 / 3。
- ActionSlotMultiIntentBasic 通过：Ability 罪卡使用次数仍为 0 / 2。

当前结论：

- 当前可以通过 PrintIntentHandlingPreview(...) 直观看到敌人意图未来可能进入的处理路径。
- 未响应意图会被标记为未来按当前 actualTarget 执行。
- 已响应意图会被标记为未来进入玩家响应处理。
- 该工具只是日志预览，不是正式结算。

当前不实现：

- 未响应敌人意图正式结算。
- 已响应敌人意图正式结算。
- 敌人攻击执行。
- 无人响应效果。
- 响应失败效果。
- 拼点执行。
- Resolved。
- 负罪感增加。
- UseCount 增加。
- slot.MarkUsed()。
- isCompleted。
- 完整速度队列。

命名记录：

- 当前使用 PrintIntentHandlingPreview(...)。
- 暂不使用 PrintIntentResolutionPreview(...)。
- 原因是当前只是处理路径预览，不是正式结算，并且避免和现有 Resolved 战斗事件混淆。

## 二十、PrintActionSlotIntentHandlingPreview 行动槽位处理敌人意图预览日志工具已完成

当前已完成小目标：

- BattleActionSlotManager 新增 PrintActionSlotIntentHandlingPreview(List<BattleActionSlot> actionSlots, List<BattleEnemyIntent> intentQueue)。
- 用于根据当前行动槽位列表和敌人意图队列，打印敌人意图未来由哪个槽位处理，或未响应时按当前 actualTarget 执行。
- 当前只做数据层日志预览，不执行任何玩家槽位或敌人意图。

修改范围记录：

- BattleActionSlotManager.cs 新增公共日志方法 PrintActionSlotIntentHandlingPreview(...)。
- BattleActionSlotManager.cs 新增私有辅助方法 FindSlotByEnemyIntent(...)。
- BattleActionSlotManager.cs 对 isResponded == false 的敌人意图，打印未响应路径：未来按当前 actualTarget 执行。
- BattleActionSlotManager.cs 对 isResponded == true 的敌人意图，通过扫描 actionSlots 查找 slot.enemyIntent == intent。
- BattleActionSlotManager.cs 找到绑定槽位时，打印未来由哪个角色的哪个槽位处理。
- BattleActionSlotManager.cs 找不到绑定槽位时，打印已响应但未找到绑定槽位。
- BattleActionSlotManager.cs 队列为空或 null 时打印无法生成预览。
- BattleActionSlotManager.cs 在 actionSlots 为空或 null 时不报错。
- CardLoadTest.cs 在 RunActionSlotMultiIntentBasicTestSequence() 中追加调用 BattleActionSlotManager.PrintActionSlotIntentHandlingPreview(actionSlots, intentQueue);。
- CardLoadTest.cs 调用位置在 BattleEnemyIntentManager.PrintIntentHandlingPreview(intentQueue); 之后，BattleActionSlotManager.PrintSlotStates(actionSlots); 之前。

已通过 Unity 测试：

- ActionSlotMultiIntentBasic 通过：敌人意图1 显示 已响应：False。
- ActionSlotMultiIntentBasic 通过：敌人意图2 显示 已响应：True。
- ActionSlotMultiIntentBasic 通过：未响应数量为 1。
- ActionSlotMultiIntentBasic 通过：PrintIntentHandlingPreview(...) 正常显示敌人意图1 未响应路径。
- ActionSlotMultiIntentBasic 通过：PrintIntentHandlingPreview(...) 正常显示敌人意图2 已响应路径。
- ActionSlotMultiIntentBasic 通过：PrintActionSlotIntentHandlingPreview(...) 显示敌人意图1 未响应，未来按当前 actualTarget 执行，目标为 allyB 槽位2。
- ActionSlotMultiIntentBasic 通过：PrintActionSlotIntentHandlingPreview(...) 显示敌人意图2 已响应，未来由 allyA 槽位1 处理，当前实际目标为 allyA 槽位1。
- ActionSlotMultiIntentBasic 通过：槽位1仍显示 已使用：False。
- ActionSlotMultiIntentBasic 通过：基础攻击使用次数仍为 0 / 3。
- ActionSlotMultiIntentBasic 通过：Ability 罪卡使用次数仍为 0 / 2。

当前结论：

- 当前已经可以通过日志看到哪些敌人意图未响应。
- 当前已经可以通过日志看到哪些敌人意图已响应。
- 当前已经可以通过日志看到已响应敌人意图未来由哪个行动槽位处理。
- 当前已经可以通过日志看到未响应敌人意图未来按当前 actualTarget 执行。
- 当前通过扫描 actionSlots 反查绑定关系，不新增 respondingSlot。
- 这样避免提前引入 BattleEnemyIntent -> BattleActionSlot 的双向引用。
- 当前仍然只是日志预览，不是正式执行队列。

当前不实现：

- 正式执行队列。
- 完整速度队列。
- 未响应敌人意图正式结算。
- 已响应敌人意图正式结算。
- 敌人攻击执行。
- 玩家槽位执行。
- 拼点执行。
- 无人响应效果。
- 响应失败效果。
- Resolved。
- 负罪感增加。
- UseCount 增加。
- slot.MarkUsed()。
- isCompleted。
- respondingSlot。
- 响应覆盖 / 旧槽位解除绑定。

## 二十一、行动槽位处理敌人意图预览日志说明已补充

当前已完成小目标：

- 在 BattleActionSlotManager.PrintActionSlotIntentHandlingPreview(...) 中补充说明性日志。
- 明确该日志工具当前只是“行动槽位与敌人意图绑定关系 / 处理路径预览”。
- 明确它不代表正式执行顺序。
- 明确未来正式执行队列采用“速度响应优先”方向。

修改范围记录：

- BattleActionSlotManager.cs 只在 PrintActionSlotIntentHandlingPreview(...) 中新增两条 Debug.Log。
- 日志位置在标题之后、空队列检查之前。
- 新增日志：提示：当前仅为行动槽位与敌人意图绑定关系 / 处理路径预览，不代表正式执行顺序。
- 新增日志：提示：未来正式执行队列采用速度响应优先方向，高速响应行动可能提前处理其指定敌人意图。

当前结论：

- PrintActionSlotIntentHandlingPreview(...) 仍然只是日志预览工具。
- 当前不做正式执行顺序。
- 当前不做速度排序。
- 当前不调整敌人意图顺序。
- 当前不执行玩家槽位或敌人意图。
- 当前只是防止后续误把预览日志理解成正式执行队列。

当前不实现：

- 正式执行队列。
- 速度排序。
- 敌人意图提前 / 延后正式处理。
- 玩家槽位执行。
- 敌人意图执行。
- 拼点。
- 敌人攻击。
- Resolved。
- 负罪感增加。
- UseCount 增加。
- slot.MarkUsed()。
- isCompleted。
- respondingSlot。
- 响应覆盖 / 旧槽位解除绑定。

基础编译检查：

- dotnet build ProjectGuilt.sln 已通过。
- 结果为 0 warnings / 0 errors。
- 普通运行曾因 sandbox 无法访问 Windows SDK 目录失败，提升权限后通过。

## 二十二、Action Slot / Enemy Intent 后续设计记录

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

### 6. 同一敌人意图的主要响应槽位规则

同一个敌人意图 / 敌人逻辑，在同一时间只能有一个主要响应槽位。

如果玩家后来选择另一个槽位响应该敌人意图，则以后一次选择为准。

- 新槽位覆盖旧响应槽位。
- 敌人意图的 actualTargetCharacter / actualTargetSlotIndex 改为新响应槽位。
- 旧槽位解除与该敌人意图的响应绑定。
- 旧槽位不再处理该敌人意图。

被覆盖的旧槽位后续如何处理，不能简单写死为“单方面攻击”，而要根据卡牌类型和玩家后续选择决定。

- 如果旧槽位是攻击卡，并且这张卡可以作为普通行动 / FreeAction 使用，则可以转为普通攻击敌人，也就是玩家俗称的“偷刀”。
- 如果旧槽位是防御卡、闪避卡、能力卡，或者必须绑定敌人意图才能使用的卡，则不应自动变成单方面攻击。
- 这些卡可能变为未响应 / 未使用，或者需要玩家重新指定目标。
- 具体规则等后续防御、闪避、能力卡、支援规则明确后再实现。

当前阶段只记录设计，不实现代码限制。

- 不实现“已响应意图不能再次响应”。
- 不实现“新响应覆盖旧响应”的正式代码。
- 不实现旧槽位自动转 FreeAction。
- 不实现正式敌人多意图结算。
- 不实现防御 / 闪避 / TargetedResponse / AutoResponse。
- 不实现完整速度队列。

当前设计倾向：

- 敌人意图最终只由一个主要响应槽位处理。
- 玩家最后一次选择决定该敌人意图的主要响应者。
- isResponded 当前只表示“这个敌人意图已有玩家槽位响应过”，暂时不作为硬限制。
- 后续如果实现覆盖逻辑，需要额外记录旧响应槽位，并处理旧槽位解除绑定后的状态。

### 7. BattleEnemyIntent 未来可能需要记录 respondingSlot

当前 BattleEnemyIntent 已有状态：

- actualTargetCharacter / actualTargetSlotIndex：表示敌人卡当前实际攻击谁的哪个槽位。
- isResponded：表示这个敌人意图是否已经被玩家槽位响应过。

但当前还缺少一个状态：当前到底是哪一个 BattleActionSlot 在作为这个敌人意图的主要响应槽位。

未来如果要实现“后一次响应覆盖旧响应”的正式逻辑，BattleEnemyIntent 可能需要记录 respondingSlot。

respondingSlot 含义：当前主要响应这个敌人意图的行动槽位。

respondingSlot 的用途：

- 找到旧响应槽位。
- 在玩家用新槽位响应同一敌人意图时，解除旧槽位绑定。
- 更新敌人意图的主要响应槽位。
- 辅助处理旧槽位后续状态，例如转为普通行动、未使用、重新指定目标等。

当前暂不实现 respondingSlot 字段。

原因是：一旦 BattleEnemyIntent 记录 BattleActionSlot，就会形成双向引用。

- BattleActionSlot 持有 enemyIntent。
- BattleEnemyIntent 持有 respondingSlot。

双向引用不是不能做，但会让清理逻辑变复杂。

例如：

- 清理槽位时是否同步清理 enemyIntent.respondingSlot。
- 覆盖响应时谁负责解除旧槽位绑定。
- 旧槽位解除绑定后是否转为 FreeAction。
- 防御 / 闪避 / 能力卡解除绑定后如何处理。

所以当前阶段只记录设计，不急着实现。

当前结论：

- actualTarget 负责记录敌人卡当前实际目标。
- isResponded 负责记录是否已有玩家槽位响应。
- respondingSlot 未来可能用于记录主要响应槽位对象。
- 当前仍只保留 actualTarget + isResponded，不进入覆盖 / 清理 / 正式结算逻辑。

当前不实现：

- respondingSlot 字段。
- 响应覆盖逻辑。
- 旧槽位解除绑定逻辑。
- 旧槽位自动转 FreeAction。
- 已响应意图禁止重复响应。
- 敌人多意图正式结算。
- 完整速度队列。
- 防御 / 闪避 / TargetedResponse / AutoResponse。

### 8. 敌人意图的响应状态与结算状态需要分开

当前已有 BattleEnemyIntent.isResponded。

isResponded 含义：这个敌人意图是否已经被玩家槽位指定响应过。

isResponded 不等于“已经结算完成”。

例如：

- 玩家在准备阶段用槽位1响应敌人意图2。
- 此时 isResponded = true。
- 但回合执行还没有开始。
- 拼点 / 防御 / 闪避 / 敌人攻击结算都还没有发生。
- 所以这个敌人意图不能视为已完成。

未来可能需要另一个状态，暂定名为 isCompleted。

isCompleted 含义：这个敌人意图是否已经完成结算。

不建议现在使用 isResolved 作为字段名。

原因：

- 当前战斗事件系统里已经有 Resolved 事件。
- 如果敌人意图也叫 isResolved，容易和卡牌事件混淆。
- 所以未来更倾向于使用 isCompleted 表示敌人意图完成。

未来可能让 isCompleted = true 的情况包括：

- 拼点完成。
- 无人响应时，敌人攻击结算完成。
- 防御处理完成。
- 闪避处理完成。
- 敌人意图被取消 / 打断后完成。
- 具体完成原因以后可能需要单独记录。

当前阶段不实现：

- isCompleted 字段。
- 敌人意图完成状态切换。
- 完成原因。
- 正式速度队列。
- 敌人多意图正式结算。
- 防御 / 闪避。
- 无人响应效果。
- 取消 / 打断逻辑。

当前结论：

- isResponded 只表示“是否被玩家响应”。
- 未来 isCompleted 才表示“是否完成结算”。
- 当前只保留 isResponded，不急着实现结算状态。

### 9. 未响应敌人意图的基础结算原则

未响应敌人意图的定义：

- 某个敌人意图没有被任何玩家槽位指定为主要响应目标。
- 也就是该敌人意图的 isResponded = false。

未响应敌人意图的未来基础结算原则：

- 未响应敌人意图未来默认按它当前的 actualTargetCharacter / actualTargetSlotIndex 执行。
- 如果没有玩家响应或改写目标，那么 actualTarget 通常等于 originalTarget。
- 因此敌人会按原计划攻击原目标槽位。

空槽位规则：

- 空槽位就是空。
- 空槽位不提供默认防御。
- 空槽位不提供默认闪避。
- 空槽位不自带额外惩罚。
- 空槽位只表示玩家没有用该槽位处理这个敌人意图。

无人响应效果：

- 如果敌人卡需要在无人响应时造成额外效果，应由敌人卡自身效果定义。
- 例如“无人响应此卡时，造成额外伤害 / 施加状态 / 破坏槽位”等。
- 这不属于空槽位自带规则。

未响应与响应失败需要区分：

- 未响应：玩家没有任何槽位指定响应该敌人意图，isResponded = false。
- 响应失败：玩家已经指定槽位响应，isResponded = true，但后续拼点 / 防御 / 闪避 / 处理结果失败。
- 未来敌人卡可能分别定义“无人响应时效果”和“响应失败时效果”，两者不应混淆。

当前阶段不实现：

- 未响应敌人意图正式结算。
- 敌人攻击执行。
- 无人响应效果。
- 响应失败效果。
- 防御 / 闪避结算。
- 完整速度队列。
- isCompleted。
- 槽位伤害 / 槽位破坏等后续规则。

当前结论：

- 当前只记录未响应敌人意图的设计原则。
- 现有 PrintUnrespondedIntents(...) 只用于观察哪些敌人意图尚未被响应。
- 它不执行这些未响应意图，也不触发任何效果。

### 10. 响应失败与未响应的区别

未响应的定义：

- 没有任何玩家槽位成功绑定该敌人意图。
- 该敌人意图的 isResponded = false。
- 例如玩家完全不处理该敌人意图，或者因为速度不足 / 槽位无效 / 重复卡等原因导致安排失败。

响应失败的定义：

- 已经有玩家槽位成功绑定该敌人意图。
- 该敌人意图的 isResponded = true。
- 但后续处理结果失败。
- 例如攻击卡拼点失败、防御不足、闪避失败等。

安排阶段失败不算响应失败：

- 速度不足导致无法响应。
- 槽位已有行动导致安排失败。
- 同一张卡重复安排失败。
- 敌人意图数据不完整导致安排失败。
- 这些都表示“没有成功响应”，因此仍应视为未响应，而不是响应失败。

结算阶段失败才算响应失败：

- 槽位已经成功绑定敌人意图。
- 后续在拼点 / 防御 / 闪避 / 其他处理过程中失败。
- 这才属于响应失败。

无人响应效果与响应失败效果需要区分：

- “无人响应时效果”只应在 isResponded = false 时触发。
- “响应失败时效果”只应在 isResponded = true 且处理失败时触发。
- 两者不应混淆。

当前阶段不实现：

- 响应失败状态字段。
- 响应失败效果。
- 无人响应效果。
- 拼点失败正式结算。
- 防御 / 闪避失败正式结算。
- 完整速度队列。
- 敌人多意图正式结算。
- isCompleted。
- 完成原因 / 失败原因记录。

当前结论：

- 当前只用 isResponded 区分“是否成功绑定过玩家响应槽位”。
- 未响应和响应失败是两个不同概念。
- 当前先记录设计边界，不进入正式实现。

### 11. 防御卡 / 闪避卡后续规则

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

### 12. 执行顺序设计记录

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

### 13. 正式执行队列未来采用速度响应优先方向

正式执行队列未来不采用简单的固定敌人意图顺序。

也就是说，不是永远按照：

- 敌人意图1。
- 敌人意图2。
- 敌人意图3。

依次固定处理。

未来正式执行队列采用“速度响应优先”的设计方向。

如果玩家角色速度高于敌人，并且该玩家槽位指定响应某个敌人意图，那么该响应行动未来可以提前处理它指定的敌人意图。

速度优势的含义：

- 不仅可以改变敌人意图的实际目标 actualTargetCharacter / actualTargetSlotIndex。
- 也可以改变该敌人意图的处理顺序。
- 高速角色可以提前介入敌人原本靠后的意图。

当前例子：

- 敌人意图1：攻击 allyB 槽位2，未响应。
- 敌人意图2：攻击 allyB 槽位1，被 allyA 槽位1 响应。
- allyA 速度高于敌人。

未来正式执行方向应允许：

- 先处理 allyA 槽位1 对敌人意图2的响应。
- 再处理敌人意图1的未响应路径。

这条规则与当前已有设计一致：

- 速度大可以改变敌人攻击顺序与拼点目标。
- 速度等于敌人时不能改变敌人的目标与顺序。
- 速度低于敌人时不能改变敌人的目标与顺序。

当前阶段不实现：

- 正式执行队列。
- 完整速度排序。
- 敌人意图提前 / 延后正式处理。
- 拼点执行。
- 敌人攻击执行。
- 未响应敌人意图正式结算。
- 已响应敌人意图正式结算。
- slot.MarkUsed()。
- isCompleted。
- 响应失败效果。
- 无人响应效果。

当前结论：

- 方案B为正式方向。
- 当前只记录设计方向。
- 当前已有的处理预览日志仍只是观察工具，不代表正式执行队列已经实现。

### 14. 未来扩展：不可预测卡

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

### 15. 未来扩展：槽位 Buff / 特殊槽位

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

### 16. 当前阶段不实现内容

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

## 二十三、下一步候选方向

后续候选方向：

- 继续完善敌人意图系统。
- 设计玩家“响应敌人意图”和“偷刀”的正式执行顺序。
- 后续再考虑速度队列。
- 后续再考虑 UI 拖拽和槽位显示。
- 敌人防御逻辑暂缓。
- 负罪感阈值暂缓。

## 二十四、当前规则提醒

- 负罪感不是消耗资源，而是从 0 开始累计增加。
- 使用罪卡会增加 guiltGain。
- 负罪感阈值惩罚暂时不做。
- 罪卡分为 Clash 和 Ability。
- Clash 罪卡参与拼点。
- Ability 罪卡不参与拼点，直接执行 effects。
- UseCount 罪卡按次数消耗。
- Permanent 罪卡不进入普通 CD，也不按次数消耗。
