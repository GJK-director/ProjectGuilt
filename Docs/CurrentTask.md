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

## 二十二、PrintSpeedPriorityHandlingPreview 速度响应优先处理顺序预览日志工具已完成

当前已完成小目标：

- BattleActionSlotManager 新增 PrintSpeedPriorityHandlingPreview(List<BattleActionSlot> actionSlots, List<BattleEnemyIntent> intentQueue)。
- 用于打印“速度响应优先方向下的第一版处理顺序预览”。
- 当前只做日志预览，不执行任何玩家槽位或敌人意图。

修改范围记录：

- BattleActionSlotManager.cs 新增公共日志方法 PrintSpeedPriorityHandlingPreview(...)。
- BattleActionSlotManager.cs 复用现有私有方法 FindSlotByEnemyIntent(...)。
- BattleActionSlotManager.cs 第一轮按 intentQueue 原顺序扫描，只打印 isResponded == true 的敌人意图。
- BattleActionSlotManager.cs 第二轮按 intentQueue 原顺序扫描，只打印 isResponded == false 的敌人意图。
- BattleActionSlotManager.cs 不改变 intentQueue。
- BattleActionSlotManager.cs 不改变 actionSlots。
- BattleActionSlotManager.cs 不创建正式执行队列对象。
- BattleActionSlotManager.cs 不做完整速度排序。
- CardLoadTest.cs 在 RunActionSlotMultiIntentBasicTestSequence() 中追加调用 BattleActionSlotManager.PrintSpeedPriorityHandlingPreview(actionSlots, intentQueue);。
- CardLoadTest.cs 调用位置在 PrintActionSlotIntentHandlingPreview(...) 之后、PrintSlotStates(...) 之前。

已通过 Unity 测试：

- ActionSlotMultiIntentBasic 通过：敌人意图1 显示 已响应：False。
- ActionSlotMultiIntentBasic 通过：敌人意图2 显示 已响应：True。
- ActionSlotMultiIntentBasic 通过：未响应数量为 1。
- ActionSlotMultiIntentBasic 通过：速度响应优先处理顺序预览第1项为 allyA 槽位1 处理敌人意图2，当前实际目标为 allyA 槽位1。
- ActionSlotMultiIntentBasic 通过：速度响应优先处理顺序预览第2项为敌人意图1 未响应，未来按当前 actualTarget 执行，目标为 allyB 槽位2。
- ActionSlotMultiIntentBasic 通过：槽位1仍显示 已使用：False。
- ActionSlotMultiIntentBasic 通过：基础攻击使用次数仍为 0 / 3。
- ActionSlotMultiIntentBasic 通过：Ability 罪卡使用次数仍为 0 / 2。

当前结论：

- 当前日志已经能体现方案B方向：已响应项优先。
- 当前日志已经能体现方案B方向：未响应项补后。
- 当前日志已经能体现方案B方向：高速响应行动在预览中排到未响应敌人意图前面。
- 当前只是第一版简化预览。
- 当前不代表最终完整速度队列。
- 当前不执行任何战斗逻辑。

当前不实现：

- 正式执行队列。
- 完整速度排序。
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

## 二十三、PrintSpeedPriorityHandlingPreview 预览顺序收集逻辑已完成轻量整理

当前已完成小目标：

- 将 BattleActionSlotManager.PrintSpeedPriorityHandlingPreview(...) 中原本内联的“两轮扫描敌人意图队列”逻辑提取为私有辅助方法。
- 新增私有方法 GetSpeedPriorityPreviewIntentOrder(List<BattleEnemyIntent> intentQueue)。
- 当前只是代码整理，不是新功能。

修改范围记录：

- BattleActionSlotManager.cs 新增私有方法 GetSpeedPriorityPreviewIntentOrder(...)。
- GetSpeedPriorityPreviewIntentOrder(...) 返回新的 List<BattleEnemyIntent>。
- GetSpeedPriorityPreviewIntentOrder(...) 第一轮收集 intent != null && intent.isResponded。
- GetSpeedPriorityPreviewIntentOrder(...) 第二轮收集 intent != null && !intent.isResponded。
- GetSpeedPriorityPreviewIntentOrder(...) 在 null 或空队列时返回空列表。
- GetSpeedPriorityPreviewIntentOrder(...) 不修改 intentQueue。
- GetSpeedPriorityPreviewIntentOrder(...) 不修改任何 BattleEnemyIntent 状态。
- PrintSpeedPriorityHandlingPreview(...) 改为复用该私有方法获取预览顺序。

保持不变：

- Console 输出语义不变。
- 标题和提示日志不变。
- 每条预览输出文本不变。
- previewIndex 仍从 1 开始。
- ActionSlotMultiIntentBasic 预期仍是第1项：allyA 槽位1 处理敌人意图2。
- ActionSlotMultiIntentBasic 预期仍是第2项：敌人意图1 未响应，按 actualTarget 执行。

当前结论：

- 当前仍然只是速度响应优先处理顺序预览。
- 现在“收集预览顺序”和“打印预览内容”的职责比之前更清晰。
- 该整理为未来升级到 BattleHandlingPreviewItem 或类似预览项数据结构预留入口。
- 当前没有真正创建预览项数据结构。

当前不实现：

- BattleHandlingPreviewItem。
- BattleExecutionItem。
- enum。
- 正式执行队列数据结构。
- 完整速度排序。
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

基础编译检查：

- dotnet build ProjectGuilt.sln 已通过。
- 结果为 0 warnings / 0 errors。
- 普通运行曾因 sandbox 无法访问 Windows SDK 目录失败，提升权限后通过。

## 二十四、BattleHandlingPreviewItem 处理预览项数据结构第一版已完成

当前已完成小目标：

- 新增 BattleHandlingPreviewItem.cs。
- 新增 BattleHandlingPreviewType enum。
- 新增 BattleHandlingPreviewItem 数据类。
- 将速度响应优先预览从“纯日志打印”推进为“先生成预览项数据，再打印预览项数据”。
- 当前仍然只是 Preview，不是正式 Execution。

修改范围记录：

- BattleHandlingPreviewItem.cs 新增 enum：BattleHandlingPreviewType。
- BattleHandlingPreviewType 当前包含 RespondedIntent。
- BattleHandlingPreviewType 当前包含 UnrespondedIntent。
- BattleHandlingPreviewItem.cs 新增数据类 BattleHandlingPreviewItem。
- BattleHandlingPreviewItem 包含 public int order;。
- BattleHandlingPreviewItem 包含 public BattleHandlingPreviewType handlingType;。
- BattleHandlingPreviewItem 包含 public BattleEnemyIntent enemyIntent;。
- BattleHandlingPreviewItem 包含 public BattleActionSlot actionSlot;。
- BattleActionSlotManager.cs 新增 CreateSpeedPriorityHandlingPreviewItems(List<BattleActionSlot> actionSlots, List<BattleEnemyIntent> intentQueue)。
- CreateSpeedPriorityHandlingPreviewItems(...) 返回 List<BattleHandlingPreviewItem>。
- CreateSpeedPriorityHandlingPreviewItems(...) 第一轮按 intentQueue 原顺序生成 RespondedIntent 预览项。
- CreateSpeedPriorityHandlingPreviewItems(...) 第二轮按 intentQueue 原顺序生成 UnrespondedIntent 预览项。
- CreateSpeedPriorityHandlingPreviewItems(...) 中 order 从 1 开始递增。
- 已响应意图通过 FindSlotByEnemyIntent(...) 查找绑定槽位。
- 未响应意图的 actionSlot 为 null。
- PrintSpeedPriorityHandlingPreview(...) 改为先生成 preview items，再遍历打印。
- CardLoadTest.cs 在 RunActionSlotMultiIntentBasicTestSequence() 中增加数量日志：速度响应优先处理预览项数量：2。
- CardLoadTest.cs 不新增测试模式。
- CardLoadTest.cs 不执行 preview items。

已通过 Unity 测试：

- ActionSlotMultiIntentBasic 通过：速度响应优先处理预览项数量：2。
- ActionSlotMultiIntentBasic 通过：敌人意图1 显示 已响应：False。
- ActionSlotMultiIntentBasic 通过：敌人意图2 显示 已响应：True。
- ActionSlotMultiIntentBasic 通过：未响应数量为 1。
- ActionSlotMultiIntentBasic 通过：速度响应优先处理顺序预览第1项为 allyA 槽位1 处理敌人意图2，当前实际目标为 allyA 槽位1。
- ActionSlotMultiIntentBasic 通过：速度响应优先处理顺序预览第2项为敌人意图1 未响应，未来按当前 actualTarget 执行，目标为 allyB 槽位2。
- ActionSlotMultiIntentBasic 通过：槽位1仍显示 已使用：False。
- ActionSlotMultiIntentBasic 通过：基础攻击使用次数仍为 0 / 3。
- ActionSlotMultiIntentBasic 通过：Ability 罪卡使用次数仍为 0 / 2。

当前结论：

- 当前已经从纯日志预览升级为“先生成处理预览项列表，再打印预览项”。
- BattleHandlingPreviewItem 只是处理预览项，不是正式执行项。
- 当前仍然不使用 BattleExecutionItem / BattleExecutionPlan / BattleExecutionQueue 命名。
- 当前生成的 preview items 不能被执行，只用于观察和后续过渡。
- 这为未来从预览项升级到正式执行队列打下基础。

当前不实现：

- BattleExecutionItem。
- BattleExecutionPlan。
- BattleExecutionQueue。
- ExecutePreviewItems(...)。
- 正式执行队列。
- 完整速度排序。
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

编译说明：

- 由于本次新增了 .cs 文件，普通 dotnet build ProjectGuilt.sln 一开始可能因为 Unity 工程文件未刷新而找不到 BattleHandlingPreviewItem。
- 本次不手动修改 .csproj。
- 需要回到 Unity 让 Unity 自动刷新 / 重新生成工程文件。
- 当前 Unity 测试已通过。

## 二十五、BattleHandlingPreviewItem 构造函数整理已完成

当前已完成小目标：

- 给 BattleHandlingPreviewItem 增加构造函数。
- 让 CreateSpeedPriorityHandlingPreviewItems(...) 使用构造函数创建 preview item。
- 当前只是代码可读性整理，不是新功能。

修改范围记录：

- BattleHandlingPreviewItem.cs 新增构造函数 BattleHandlingPreviewItem(int order, BattleHandlingPreviewType handlingType, BattleEnemyIntent enemyIntent, BattleActionSlot actionSlot)。
- BattleHandlingPreviewItem 构造函数只负责字段赋值。
- BattleHandlingPreviewItem 构造函数中 this.order = order。
- BattleHandlingPreviewItem 构造函数中 this.handlingType = handlingType。
- BattleHandlingPreviewItem 构造函数中 this.enemyIntent = enemyIntent。
- BattleHandlingPreviewItem 构造函数中 this.actionSlot = actionSlot。
- BattleHandlingPreviewItem.cs 没有新增字段。
- BattleHandlingPreviewItem.cs 没有新增 enum 值。
- BattleHandlingPreviewItem.cs 没有删除旧逻辑。
- BattleActionSlotManager.cs 中 CreateSpeedPriorityHandlingPreviewItems(...) 改为使用构造函数创建 BattleHandlingPreviewItem。
- BattleActionSlotManager.cs 删除局部对象逐字段赋值写法。
- BattleActionSlotManager.cs 没有改变 preview item 数量、顺序、内容。
- BattleActionSlotManager.cs 没有改变 PrintSpeedPriorityHandlingPreview(...) 的 Console 输出语义。

保持不变：

- BattleHandlingPreviewType enum 不变。
- BattleHandlingPreviewItem 字段不变。
- CreateSpeedPriorityHandlingPreviewItems(...) 生成规则不变。
- PrintSpeedPriorityHandlingPreview(...) 输出语义不变。
- ActionSlotMultiIntentBasic 预期结果不变。

当前结论：

- BattleHandlingPreviewItem 现在可以通过构造函数明确创建。
- 预览项数据结构比之前更清晰。
- 当前仍然只是处理预览项，不是正式执行项。
- 当前仍然不进入正式执行队列或正式结算。

当前不实现：

- BattleExecutionItem。
- BattleExecutionPlan。
- BattleExecutionQueue。
- ExecutePreviewItems(...)。
- 正式执行队列。
- 完整速度排序。
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

## 二十六、BattleActionSlot.UnbindEnemyIntent 槽位解除敌人意图绑定辅助方法已完成

当前已完成小目标：

- BattleActionSlot 新增 UnbindEnemyIntent()。
- UnbindEnemyIntent() 用于未来响应覆盖逻辑中解除旧槽位与敌人意图的绑定。
- 当前只是底层状态辅助方法。
- 当前没有接入覆盖逻辑。

修改范围记录：

- BattleActionSlot.cs 新增公共方法 public void UnbindEnemyIntent()。
- UnbindEnemyIntent() 内部执行 enemyIntent = null;。
- UnbindEnemyIntent() 内部执行 target = null;。
- UnbindEnemyIntent() 内部执行 isUsed = false;。

方法职责：

- 只解除当前槽位与敌人意图的绑定。
- 让该槽位不再被 FindSlotByEnemyIntent(...) 找到。
- 清除原响应目标 target，避免被误读为普通攻击 / 偷刀。
- 保持 isUsed = false，因为解除绑定不是执行。

保持不变：

- 不清除 actor。
- 不清除 cardState。
- 不改变 slotType。
- 不调用 Clear()。
- 不让槽位变空。
- 不自动转为 FreeAction。
- 不加日志。
- 不接入覆盖流程。

当前结论：

- UnbindEnemyIntent() 是未来响应覆盖规则的基础工具。
- UnbindEnemyIntent() 不等于 Clear()。
- UnbindEnemyIntent() 不会替玩家取消整张卡牌安排。
- UnbindEnemyIntent() 只让旧槽位暂时进入“已安排卡牌，但不再绑定敌人意图”的中间状态。
- 未来是否转为普通行动 / 重新指定 / 清空槽位，需要根据卡牌类型和玩家后续选择再设计。

当前不实现：

- 响应覆盖逻辑。
- FindSlotsByEnemyIntent(...)。
- ActionSlotResponseOverwriteBasic 测试模式。
- 旧槽位转 FreeAction。
- respondingSlot。
- UnmarkResponded()。
- 正式执行队列。
- 拼点。
- 敌人攻击。
- Resolved。
- 负罪感增加。
- UseCount 增加。
- slot.MarkUsed()。
- isCompleted。

基础编译检查：

- dotnet build ProjectGuilt.sln 已通过。
- 结果为 0 warnings / 0 errors。
- 普通运行曾因 sandbox 无法访问 Windows SDK 目录失败，提升权限后通过。

## 二十七、ActionSlotResponseOverwriteBasic 响应覆盖基础测试已完成

当前已完成小目标：

- 新增测试模式 ActionSlotResponseOverwriteBasic。
- 验证同一个敌人意图同一时间只能有一个主要响应槽位。
- 验证后一次成功响应会覆盖前一次成功响应。
- 验证旧槽位解除敌人意图绑定。
- 验证新槽位成为该敌人意图的主要响应槽位。
- 当前只验证安排阶段和 Preview，不进入正式结算。

修改范围记录：

- BattleActionSlotManager.cs 在 AssignResponseToEnemyIntent(...) 中接入响应覆盖清理逻辑。
- BattleActionSlotManager.cs 中覆盖清理逻辑放在速度检查通过后、slot.AssignResponse(...) 之前。
- BattleActionSlotManager.cs 中新响应失败时不会清理旧槽位。
- BattleActionSlotManager.cs 新增私有方法 FindSlotsByEnemyIntent(List<BattleActionSlot> slots, BattleEnemyIntent enemyIntent)。
- BattleActionSlotManager.cs 对绑定同一个 enemyIntent 的旧槽位调用 oldSlot.UnbindEnemyIntent()。
- BattleActionSlotManager.cs 跳过当前即将写入的新槽位。
- BattleActionSlotManager.cs 不调用 Clear()。
- BattleActionSlotManager.cs 不自动转 FreeAction。
- BattleActionSlotManager.cs 不清除旧槽位的 actor / cardState / slotType。
- BattleActionSlotManager.cs 新增覆盖日志：槽位 X 已解除对敌人意图Y的响应绑定。
- BattleActionSlotManager.cs 中 PrintSlotStates(...) 支持显示 RespondToEnemyIntent 但 enemyIntent == null 的中间状态：响应意图：无 / 已解除绑定。
- CardLoadTest.cs 中 BattleTestMode 新增 ActionSlotResponseOverwriteBasic。
- CardLoadTest.cs 中 Start() 增加该模式分支。
- CardLoadTest.cs 新增 RunActionSlotResponseOverwriteBasicTestSequence()。
- CardLoadTest.cs 中测试只做安排、覆盖、状态打印和 Preview，不调用 ExecuteActionSlots(...)。

Unity 测试结果：

- 第一次响应：allyA 槽位1成功响应敌人意图1。
- 第一次响应：敌人意图1的实际目标从 allyB 槽位1 改为 allyA 槽位1。
- 第一次响应：槽位1绑定敌人意图1。
- 第二次响应：allyA 槽位2成功响应同一个敌人意图1。
- 第二次响应：槽位1打印 槽位 1 已解除对敌人意图1的响应绑定。
- 第二次响应：敌人意图1的实际目标改为 allyA 槽位2。
- 第二次响应：槽位2成为该敌人意图的主要响应槽位。

最终敌人意图状态：

- 敌人意图1 已响应：True。
- 敌人意图1 实际目标为 allyA 槽位2。

最终槽位状态：

- 槽位1仍有行动者和卡牌。
- 槽位1目标显示为 无目标。
- 槽位1响应意图显示为 无 / 已解除绑定。
- 槽位1 已使用：False。
- 槽位2绑定敌人意图1。
- 槽位2 已使用：False。

Preview：

- 速度响应优先处理预览项数量：1。
- 只显示 allyA 槽位2 处理敌人意图1。
- 不再显示槽位1处理敌人意图1。

当前结论：

- 响应覆盖基础逻辑已验证通过。
- 后一次成功响应可以覆盖前一次成功响应。
- 旧槽位不会被 Clear()。
- 旧槽位不会自动转为 FreeAction。
- 旧槽位会进入“已安排卡牌，但不再绑定敌人意图”的中间状态。
- Preview 系统在覆盖后能正确反查到新槽位。
- 当前仍未引入 respondingSlot，继续通过扫描 actionSlots 反查绑定关系。

当前不实现：

- 旧槽位转 FreeAction。
- 旧槽位重新指定目标。
- 旧槽位自动清空。
- 取消所有响应。
- UnmarkResponded()。
- respondingSlot。
- 正式执行队列。
- 完整速度排序。
- 玩家槽位执行。
- 敌人意图执行。
- 拼点。
- 敌人攻击。
- 无人响应效果。
- 响应失败效果。
- Resolved。
- 负罪感增加。
- UseCount 增加。
- slot.MarkUsed()。
- isCompleted。

编译检查：

- dotnet build ProjectGuilt.sln 已通过。
- 结果为 0 warnings / 0 errors。
- 普通 sandbox 运行曾因 Windows SDK 目录权限失败，提升权限后通过。

## 二十八、ActionSlotResponseOverwriteFailKeepOld 响应覆盖失败保持旧响应测试已完成

当前已完成小目标：

- 新增测试模式 ActionSlotResponseOverwriteFailKeepOld。
- 验证后一次响应如果失败，不会误清理前一次已经成功的响应。
- 验证低速角色尝试覆盖失败时，旧响应槽位保持绑定。
- 验证敌人意图 actualTarget 保持旧响应槽位。
- 当前只验证安排阶段和 Preview，不进入正式结算。

修改范围记录：

- CardLoadTest.cs 中 BattleTestMode 新增 ActionSlotResponseOverwriteFailKeepOld。
- CardLoadTest.cs 中 Start() 增加该模式分支。
- CardLoadTest.cs 新增 RunActionSlotResponseOverwriteFailKeepOldTestSequence()。
- CardLoadTest.cs 测试中创建低速角色 覆盖失败角色，速度范围 3-3。
- CardLoadTest.cs 中低速角色使用独立攻击卡尝试覆盖。
- CardLoadTest.cs 中测试只做安排、失败验证、状态打印和 Preview。
- CardLoadTest.cs 中不调用 ExecuteActionSlots(...)。
- 未修改 BattleActionSlotManager.cs。
- 未修改覆盖逻辑本体。
- 未修改 BattleActionSlot.cs。
- 未修改 BattleResolver、BattleCardManager、CardsTest.json 或 .csproj。

Unity 测试结果：

- 第一次响应：allyA 槽位1成功响应敌人意图1。
- 第一次响应：敌人意图1的实际目标从 allyB 槽位1 改为 allyA 槽位1。
- 第一次响应：槽位1绑定敌人意图1。
- 第二次响应：覆盖失败角色 速度为 3。
- 第二次响应：敌人速度为 5。
- 第二次响应：覆盖失败角色 速度不足，无法介入保护 allyB。
- 第二次响应失败。
- 第二次响应打印 覆盖失败角色响应敌人意图失败，旧响应应保持不变。

最终敌人意图状态：

- 敌人意图1 已响应：True。
- 敌人意图1 实际目标仍为 allyA 槽位1。

最终槽位状态：

- 槽位1仍绑定敌人意图1。
- 槽位1 已使用：False。
- 槽位2为空。

Preview：

- 速度响应优先处理预览项数量：1。
- 只显示 allyA 槽位1 处理敌人意图1。
- 不显示 覆盖失败角色 或槽位2处理敌人意图1。

重点确认没有出现：

- 槽位 1 已解除对敌人意图1的响应绑定。
- 槽位1 响应意图：无 / 已解除绑定。
- 敌人意图 actualTarget 变成 覆盖失败角色 槽位2。
- 拼点日志。
- Resolved。
- 负罪感增加。
- UseCount 增加。
- 已使用：True。

当前结论：

- 响应覆盖逻辑的安全性得到验证。
- 成功响应会覆盖旧响应。
- 失败响应不会覆盖旧响应。
- 覆盖清理逻辑的位置是安全的。
- 当前覆盖逻辑可以作为进入正式执行队列设计阶段前的稳定基础。

当前不实现：

- 同速覆盖失败单独测试。
- 多敌人意图 + 覆盖组合测试。
- 旧槽位转 FreeAction。
- 旧槽位重新指定目标。
- 旧槽位自动清空。
- 取消所有响应。
- UnmarkResponded()。
- respondingSlot。
- 正式执行队列。
- 完整速度排序。
- 玩家槽位执行。
- 敌人意图执行。
- 拼点。
- 敌人攻击。
- 无人响应效果。
- 响应失败效果。
- Resolved。
- 负罪感增加。
- UseCount 增加。
- slot.MarkUsed()。
- isCompleted。

编译检查：

- dotnet build ProjectGuilt.sln 已通过。
- 结果为 0 warnings / 0 errors。
- 普通 sandbox 运行曾因 Windows SDK 目录权限失败，提升权限后通过。

## 二十九、BattleExecutionItem / BattleExecutionPlan 纯数据结构第一版已完成

当前已完成小目标：

- 新增 BattleExecutionItem.cs。
- 新增 BattleExecutionPlan.cs。
- 当前只是正式执行队列的数据结构第一版。
- 当前没有接入生成、打印、执行、测试模式或正式结算。

修改范围记录：

- BattleExecutionItem.cs 新增 enum：BattleExecutionItemType。
- BattleExecutionItemType 包含 RespondedEnemyIntent。
- BattleExecutionItemType 包含 UnrespondedEnemyIntent。
- BattleExecutionItemType 包含 FreeAction。
- BattleExecutionItem.cs 新增数据类 BattleExecutionItem。
- BattleExecutionItem 包含 public int order;。
- BattleExecutionItem 包含 public BattleExecutionItemType executionType;。
- BattleExecutionItem 包含 public BattleEnemyIntent enemyIntent;。
- BattleExecutionItem 包含 public BattleActionSlot actionSlot;。
- BattleExecutionItem 包含 public bool isCompleted;。
- BattleExecutionItem 新增构造函数，接收 order / executionType / enemyIntent / actionSlot。
- BattleExecutionItem 构造函数负责设置字段。
- BattleExecutionItem 构造函数中 isCompleted = false。
- BattleExecutionPlan.cs 新增 using System.Collections.Generic;。
- BattleExecutionPlan.cs 新增数据类 BattleExecutionPlan。
- BattleExecutionPlan 包含 public List<BattleExecutionItem> executionItems;。
- BattleExecutionPlan 包含 public bool isCompleted;。
- BattleExecutionPlan 构造函数中 executionItems = new List<BattleExecutionItem>();。
- BattleExecutionPlan 构造函数中 isCompleted = false;。
- BattleExecutionPlan 新增轻量方法 AddItem(BattleExecutionItem item)。
- AddItem(...) 只做 null 防护和加入列表。

当前保持不变：

- 没有修改 BattleActionSlotManager.cs。
- 没有修改 CardLoadTest.cs。
- 没有修改 BattleActionSlot.cs。
- 没有修改 BattleEnemyIntent.cs。
- 没有修改 BattleHandlingPreviewItem.cs。
- 没有修改 BattleResolver、BattleCardManager、GuiltManager、CardEffectExecutor。
- 没有修改 CardsTest.json。
- 没有修改 .csproj。

当前结论：

- BattleExecutionItem 是未来正式执行队列中的单个执行项。
- BattleExecutionPlan 是未来正式执行计划的容器。
- 当前只是数据承载层。
- 当前还没有生成计划。
- 当前还没有打印计划。
- 当前还没有执行计划。
- 当前还没有和 Preview 系统或 Action Slot Manager 接入。

当前不实现：

- CreateExecutionPlan(...)。
- PrintExecutionPlan(...)。
- ExecuteExecutionPlan(...)。
- 正式执行计划生成逻辑。
- 正式执行器。
- 完整速度排序。
- FreeAction 混排。
- 旧槽位中间状态处理。
- 玩家槽位执行。
- 敌人意图执行。
- 拼点。
- 敌人攻击。
- 无人响应效果。
- 响应失败效果。
- Resolved。
- 负罪感增加。
- UseCount 增加。
- slot.MarkUsed()。
- BattleEnemyIntent.isCompleted。
- respondingSlot。

基础编译检查：

- dotnet build ProjectGuilt.sln 已通过。
- 结果为 0 warnings / 0 errors。
- 普通 sandbox 运行曾因 Windows SDK 目录权限失败，提升权限后通过。

## 三十、ActionSlotExecutionPlanBasic / BattleExecutionPlanManager 第一版执行计划生成预览已完成

当前已完成小目标：

- 新增 BattleExecutionPlanManager.cs。
- 新增 CreateBasicExecutionPlan(...)。
- 新增 PrintExecutionPlan(...)。
- 新增测试模式 ActionSlotExecutionPlanBasic。
- 当前只生成并打印 BattleExecutionPlan。
- 当前不执行 BattleExecutionPlan。

修改范围记录：

- BattleExecutionPlanManager.cs 新增静态类 BattleExecutionPlanManager。
- BattleExecutionPlanManager.cs 新增公共方法 CreateBasicExecutionPlan(List<BattleActionSlot> actionSlots, List<BattleEnemyIntent> intentQueue)。
- CreateBasicExecutionPlan(...) 第一轮按 intentQueue 原顺序生成 RespondedEnemyIntent。
- CreateBasicExecutionPlan(...) 第二轮按 intentQueue 原顺序生成 UnrespondedEnemyIntent。
- CreateBasicExecutionPlan(...) 中 order 从 1 开始递增。
- CreateBasicExecutionPlan(...) 使用 executionPlan.AddItem(...) 加入计划。
- CreateBasicExecutionPlan(...) 暂不处理 FreeAction。
- CreateBasicExecutionPlan(...) 暂不处理旧槽位解除绑定后的中间状态。
- CreateBasicExecutionPlan(...) 暂不做完整速度排序。
- BattleExecutionPlanManager.cs 新增公共方法 PrintExecutionPlan(BattleExecutionPlan executionPlan)。
- PrintExecutionPlan(...) 只打印计划，不执行 item。
- PrintExecutionPlan(...) 支持空计划日志。
- PrintExecutionPlan(...) 支持 RespondedEnemyIntent / UnrespondedEnemyIntent / FreeAction 安全打印分支。
- BattleExecutionPlanManager.cs 新增私有辅助方法 FindSlotByEnemyIntent(...)。
- FindSlotByEnemyIntent(...) 只扫描槽位引用关系，不修改状态。
- CardLoadTest.cs 中 BattleTestMode 新增 ActionSlotExecutionPlanBasic。
- CardLoadTest.cs 中 Start() 增加该模式分支。
- CardLoadTest.cs 新增 RunActionSlotExecutionPlanBasicTestSequence()。
- RunActionSlotExecutionPlanBasicTestSequence() 中创建两个敌人意图。
- RunActionSlotExecutionPlanBasicTestSequence() 中 allyA 槽位1响应敌人意图2。
- RunActionSlotExecutionPlanBasicTestSequence() 中生成并打印 BattleExecutionPlan。
- RunActionSlotExecutionPlanBasicTestSequence() 中打印 allyA 卡牌状态。
- RunActionSlotExecutionPlanBasicTestSequence() 不调用 ExecuteActionSlots(...)。

Unity 测试结果：

- 测试模式：ActionSlotExecutionPlanBasic。
- 敌人意图1未响应。
- 敌人意图1原目标 / 实际目标为 allyB 槽位2。
- 敌人意图2已响应。
- allyA 槽位1处理敌人意图2。
- 敌人意图2实际目标为 allyA 槽位1。
- 打印 BattleExecutionPlan。
- BattleExecutionPlan 第1项为 RespondedEnemyIntent，allyA 槽位1处理敌人意图2。
- BattleExecutionPlan 第2项为 UnrespondedEnemyIntent，敌人意图1未响应，未来按 actualTarget 执行。
- ExecutionPlan 项数量：2。

当前验证结论：

- CreateBasicExecutionPlan(...) 可以根据 intentQueue + actionSlots 生成第一版执行计划。
- 第一版执行计划顺序为“已响应敌人意图优先、未响应敌人意图补后”。
- PrintExecutionPlan(...) 可以正确打印计划内容。
- 当前仍然只是计划生成和打印，不执行任何 item。

当前没有进入正式结算：

- 槽位已使用：False。
- allyA 基础攻击 UseCount 仍为 0 / 3。
- 罪卡测试卡 UseCount 仍为 0 / 2。
- 没有拼点日志。
- 没有 Resolved。
- 没有负罪感增加。
- 没有 UseCount 增加。
- 没有调用 slot.MarkUsed()。

当前不实现：

- ExecuteExecutionPlan(...)。
- 正式执行器。
- 完整速度排序。
- FreeAction 混排。
- 旧槽位中间状态处理。
- 玩家槽位执行。
- 敌人意图执行。
- 拼点。
- 敌人攻击。
- 无人响应效果。
- 响应失败效果。
- Resolved。
- 负罪感增加。
- UseCount 增加。
- slot.MarkUsed()。
- BattleEnemyIntent.isCompleted。
- respondingSlot。

编译说明：

- 本次新增了 BattleExecutionPlanManager.cs。
- 普通 dotnet build ProjectGuilt.sln 曾因 Unity 工程文件未刷新而找不到新增脚本。
- 没有手动修改 .csproj。
- 回到 Unity 自动刷新后，Unity 测试已通过。
- 如后续再次运行 dotnet build，应在 Unity 刷新工程文件后再检查。

## 三十一、ActionSlotExecutionPlanEmpty 空计划 / 空队列安全测试已完成

当前已完成小目标：

- 新增测试模式 ActionSlotExecutionPlanEmpty。
- 验证 BattleExecutionPlanManager 对空计划 / 空队列 / null 输入的安全处理。
- 当前只测试生成和打印。
- 当前不进入正式执行或正式结算。

修改范围记录：

- CardLoadTest.cs 中 BattleTestMode 新增 ActionSlotExecutionPlanEmpty。
- CardLoadTest.cs 中 Start() 增加该模式分支。
- CardLoadTest.cs 新增 RunActionSlotExecutionPlanEmptyTestSequence()。
- 没有修改 BattleExecutionPlanManager.cs。
- 没有修改 BattleExecutionPlan.cs。
- 没有修改 BattleExecutionItem.cs。
- 没有修改 BattleActionSlotManager.cs。
- 没有修改任何战斗核心逻辑。

测试覆盖内容：

- BattleExecutionPlanManager.PrintExecutionPlan(null)。
- BattleExecutionPlanManager.CreateBasicExecutionPlan(null, null) 后打印。
- 空 List<BattleActionSlot> + 空 List<BattleEnemyIntent> 生成后打印。
- new BattleExecutionPlan() 后打印。

Unity 测试结果：

- 4 组输入都能正常打印空计划。
- 每组都显示：===== 当前 BattleExecutionPlan =====。
- 每组都显示：提示：当前只生成并打印执行计划，不执行任何 item。
- 每组都显示：当前 BattleExecutionPlan 没有执行项。
- 每组都显示：ExecutionPlan 项数量：0。
- 没有红色报错。
- 没有空引用异常。

当前没有进入正式结算：

- 没有执行 BattleExecutionPlan。
- 没有调用 ExecuteActionSlots(...)。
- 没有调用 BattleResolver。
- 没有调用 slot.MarkUsed()。
- 没有拼点。
- 没有敌人攻击。
- 没有 Resolved。
- 没有负罪感增加。
- 没有 UseCount 增加。

当前结论：

- BattleExecutionPlanManager 对 null plan 能安全处理。
- BattleExecutionPlanManager 对 null actionSlots / null intentQueue 能安全处理。
- BattleExecutionPlanManager 对空 actionSlots / intentQueue 能安全处理。
- BattleExecutionPlanManager 对空 BattleExecutionPlan 能安全处理。
- 空计划 / 空队列边界已经有测试模式固定。
- 当前 ExecutionPlan 生成与打印阶段更加稳定。

当前不实现：

- ExecuteExecutionPlan(...)。
- 正式执行器。
- 完整速度排序。
- FreeAction 混排。
- 旧槽位中间状态处理。
- 玩家槽位执行。
- 敌人意图执行。
- 拼点。
- 敌人攻击。
- 无人响应效果。
- 响应失败效果。
- Resolved。
- 负罪感增加。
- UseCount 增加。
- slot.MarkUsed()。
- BattleEnemyIntent.isCompleted。
- respondingSlot。

编译检查：

- dotnet build ProjectGuilt.sln 已通过。
- 结果为 0 warnings / 0 errors。
- 普通 sandbox 运行曾因 Windows SDK 目录权限失败，提升权限后通过。

## 三十二、ActionSlotExecutionPlanMissingSlot 已响应但缺少绑定槽位安全测试已完成

当前已完成小目标：

- 新增测试模式 ActionSlotExecutionPlanMissingSlot。
- 验证敌人意图 isResponded == true，但 actionSlots 中找不到绑定槽位时，BattleExecutionPlanManager 能安全生成并打印计划。
- 当前只测试 ExecutionPlan 生成和打印。
- 当前不进入正式执行或正式结算。

修改范围记录：

- CardLoadTest.cs 中 BattleTestMode 新增 ActionSlotExecutionPlanMissingSlot。
- CardLoadTest.cs 中 Start() 增加该模式分支。
- CardLoadTest.cs 新增 RunActionSlotExecutionPlanMissingSlotTestSequence()。
- 没有修改 BattleExecutionPlanManager.cs。
- 没有修改 BattleExecutionPlan.cs。
- 没有修改 BattleExecutionItem.cs。
- 没有修改 BattleActionSlotManager.cs。
- 没有修改 BattleActionSlot.cs。
- 没有修改任何战斗核心逻辑。

测试构造方式：

- 创建一个敌人意图，攻击 allyB 槽位1。
- 创建 1 个行动槽位。
- 正常调用 AssignResponseToEnemyIntent(...)。
- 让 allyA 槽位1 成功响应该敌人意图。
- 敌人意图变为 已响应：True。
- 敌人意图实际目标变为 allyA 槽位1。
- 随后手动调用槽位的 UnbindEnemyIntent()。
- 构造出：敌人意图仍为 已响应：True。
- 构造出：敌人意图实际目标仍为 allyA 槽位1。
- 构造出：没有任何槽位绑定该敌人意图。

Unity 测试结果：

- 敌人意图1已响应：True。
- 敌人意图1实际目标为 allyA 槽位1。
- 槽位1仍有行动者和卡牌。
- 槽位1目标显示为 无目标。
- 槽位1已使用：False。
- 槽位1响应意图显示为 无 / 已解除绑定。
- BattleExecutionPlan 打印结果中，第1项为 RespondedEnemyIntent。
- BattleExecutionPlan 打印提示：敌人意图1已响应，但未找到绑定槽位。
- BattleExecutionPlan 打印当前实际目标为 allyA 槽位1。
- ExecutionPlan 项数量：1。

当前验证结论：

- CreateBasicExecutionPlan(...) 在“已响应但缺少绑定槽位”的异常状态下可以安全生成 item。
- 生成的 BattleExecutionItem 类型为 RespondedEnemyIntent。
- 该 item 的 actionSlot == null。
- PrintExecutionPlan(...) 能安全打印异常提示。
- 不会空引用报错。
- 这个测试固定了 ExecutionPlan 对异常数据边界的处理能力。

当前没有进入正式结算：

- 没有执行 BattleExecutionPlan。
- 没有调用 ExecuteActionSlots(...)。
- 没有调用 BattleResolver。
- 没有调用 slot.MarkUsed()。
- 没有拼点。
- 没有敌人攻击。
- 没有 Resolved。
- 没有负罪感增加。
- 没有 UseCount 增加。

当前不实现：

- ExecuteExecutionPlan(...)。
- 正式执行器。
- 完整速度排序。
- FreeAction 混排。
- 旧槽位中间状态正式处理。
- 玩家槽位执行。
- 敌人意图执行。
- 拼点。
- 敌人攻击。
- 无人响应效果。
- 响应失败效果。
- Resolved。
- 负罪感增加。
- UseCount 增加。
- slot.MarkUsed()。
- BattleEnemyIntent.isCompleted。
- respondingSlot。

编译检查：

- dotnet build ProjectGuilt.sln 已通过。
- 结果为 0 warnings / 0 errors。
- 普通 sandbox 运行曾因 Windows SDK 目录权限失败，提升权限后通过。

## 三十三、ActionSlotExecutionPlanMultiBasic 多项执行计划顺序测试已完成

当前已完成小目标：

- 新增测试模式 ActionSlotExecutionPlanMultiBasic。
- 验证多个已响应敌人意图 + 多个未响应敌人意图时，BattleExecutionPlanManager.CreateBasicExecutionPlan(...) 的第一版简化顺序稳定。
- 验证第一轮按 intentQueue 原顺序收集已响应项。
- 验证第二轮按 intentQueue 原顺序收集未响应项。
- 验证 order 从 1 开始递增。
- 当前只测试 ExecutionPlan 生成和打印。
- 当前不进入正式执行或正式结算。

修改范围记录：

- CardLoadTest.cs 中 BattleTestMode 新增 ActionSlotExecutionPlanMultiBasic。
- CardLoadTest.cs 中 Start() 增加该模式分支。
- CardLoadTest.cs 新增 RunActionSlotExecutionPlanMultiBasicTestSequence()。
- 没有修改 BattleExecutionPlanManager.cs。
- 没有修改 BattleExecutionPlan.cs。
- 没有修改 BattleExecutionItem.cs。
- 没有修改 BattleActionSlotManager.cs。
- 没有修改任何战斗核心逻辑。

测试构造方式：

- 创建 4 个敌人意图。
- 队列原顺序为：敌人意图1 未响应，攻击 allyB 槽位2。
- 队列原顺序为：敌人意图2 已响应，攻击 allyB 槽位1。
- 队列原顺序为：敌人意图3 未响应，攻击 allyB 槽位2。
- 队列原顺序为：敌人意图4 已响应，攻击 allyB 槽位1。
- 创建 2 个行动槽位。
- allyA 槽位1响应敌人意图2。
- allyA 槽位2使用另一张独立攻击卡响应敌人意图4。
- 使用独立攻击卡避免同一张 BattleCardState 重复安排限制。
- 调用 CreateBasicExecutionPlan(...)。
- 调用 PrintExecutionPlan(...)。

Unity 测试结果：

- 敌人意图1：已响应：False。
- 敌人意图2：已响应：True。
- 敌人意图3：已响应：False。
- 敌人意图4：已响应：True。
- BattleExecutionPlan 打印结果第1项：RespondedEnemyIntent，allyA 槽位1 处理敌人意图2。
- BattleExecutionPlan 打印结果第2项：RespondedEnemyIntent，allyA 槽位2 处理敌人意图4。
- BattleExecutionPlan 打印结果第3项：UnrespondedEnemyIntent，敌人意图1未响应，未来按 actualTarget 执行。
- BattleExecutionPlan 打印结果第4项：UnrespondedEnemyIntent，敌人意图3未响应，未来按 actualTarget 执行。
- ExecutionPlan 项数量：4。

当前验证结论：

- CreateBasicExecutionPlan(...) 的两轮扫描规则稳定。
- 第一轮会按 intentQueue 原顺序生成所有 RespondedEnemyIntent。
- 第二轮会按 intentQueue 原顺序生成所有 UnrespondedEnemyIntent。
- order 从 1 到 4 正确递增。
- 当前顺序仍是第一版简化规则，不代表最终完整速度队列。
- 当前暂不混入 FreeAction。

当前没有进入正式结算：

- 槽位1 已使用：False。
- 槽位2 已使用：False。
- allyA 第一张基础攻击 UseCount 仍为 0 / 3。
- allyA 第二张基础攻击 UseCount 仍为 0 / 3。
- 罪卡测试卡 UseCount 仍为 0 / 2。
- 没有拼点日志。
- 没有敌人攻击。
- 没有 Resolved。
- 没有负罪感增加。
- 没有 UseCount 增加。
- 没有调用 slot.MarkUsed()。
- 没有调用 ExecuteActionSlots(...)。

当前不实现：

- ExecuteExecutionPlan(...)。
- BattleExecutionPlanExecutor.cs。
- 正式执行器。
- 完整速度排序。
- FreeAction 混排。
- 旧槽位中间状态正式处理。
- 玩家槽位执行。
- 敌人意图执行。
- 拼点。
- 敌人攻击。
- 无人响应效果。
- 响应失败效果。
- Resolved。
- 负罪感增加。
- UseCount 增加。
- slot.MarkUsed()。
- BattleEnemyIntent.isCompleted。
- respondingSlot。

编译检查：

- dotnet build ProjectGuilt.sln 已通过。
- 结果为 0 warnings / 0 errors。
- 普通 sandbox 运行曾因 Windows SDK 目录权限失败，提升权限后通过。

## 三十四、ActionSlotExecutionPlanStepPreviewBasic / BattleExecutionPlanExecutor 第一版执行步骤预览已完成

当前已完成小目标：

- 新增 BattleExecutionPlanExecutor.cs。
- 新增 BattleExecutionPlanExecutor.PrintExecutionPlanStepPreview(BattleExecutionPlan executionPlan)。
- 新增测试模式 ActionSlotExecutionPlanStepPreviewBasic。
- 当前只打印执行步骤预览。
- 当前不执行 BattleExecutionPlan。
- 当前不进入正式结算。

修改范围记录：

- BattleExecutionPlanExecutor.cs 新增静态类 BattleExecutionPlanExecutor。
- BattleExecutionPlanExecutor.cs 新增公共方法 PrintExecutionPlanStepPreview(...)。
- BattleExecutionPlanExecutor.cs 新增私有辅助方法 PrintRespondedEnemyIntentStepPreview(...)。
- BattleExecutionPlanExecutor.cs 新增私有辅助方法 PrintUnrespondedEnemyIntentStepPreview(...)。
- BattleExecutionPlanExecutor.cs 当前只做日志预览。
- BattleExecutionPlanExecutor.cs 安全处理 null plan。
- BattleExecutionPlanExecutor.cs 安全处理 null executionItems。
- BattleExecutionPlanExecutor.cs 安全处理空列表。
- BattleExecutionPlanExecutor.cs 安全处理 null item。
- BattleExecutionPlanExecutor.cs 安全处理 null enemyIntent。
- BattleExecutionPlanExecutor.cs 安全处理 null actionSlot。
- BattleExecutionPlanExecutor.cs 根据 BattleExecutionItemType 打印未来处理方向。
- BattleExecutionPlanExecutor.cs 不执行 item。
- BattleExecutionPlanExecutor.cs 不修改任何状态。
- CardLoadTest.cs 中 BattleTestMode 新增 ActionSlotExecutionPlanStepPreviewBasic。
- CardLoadTest.cs 中 Start() 增加该模式分支。
- CardLoadTest.cs 新增 RunActionSlotExecutionPlanStepPreviewBasicTestSequence()。
- RunActionSlotExecutionPlanStepPreviewBasicTestSequence() 中创建 1 个未响应敌人意图和 1 个已响应敌人意图。
- RunActionSlotExecutionPlanStepPreviewBasicTestSequence() 中调用 CreateBasicExecutionPlan(...)。
- RunActionSlotExecutionPlanStepPreviewBasicTestSequence() 中调用 PrintExecutionPlan(...)。
- RunActionSlotExecutionPlanStepPreviewBasicTestSequence() 中调用 BattleExecutionPlanExecutor.PrintExecutionPlanStepPreview(...)。
- RunActionSlotExecutionPlanStepPreviewBasicTestSequence() 中打印 allyA 卡牌状态确认未执行。

Unity 测试结果：

- 测试模式：ActionSlotExecutionPlanStepPreviewBasic。
- ExecutionPlan 第1项为 RespondedEnemyIntent，allyA 槽位1 处理敌人意图2。
- ExecutionPlan 第2项为 UnrespondedEnemyIntent，敌人意图1未响应。
- ExecutionPlan 项数量：2。
- 执行步骤预览打印标题：===== BattleExecutionPlan 执行步骤预览 =====。
- 执行步骤预览打印提示：提示：当前只预览执行步骤，不执行任何 item，不修改任何状态。
- 第1项预览：RespondedEnemyIntent 未来将处理玩家槽位对敌人意图的响应。
- 第1项预览：槽位为 allyA 槽位1。
- 第1项预览：敌人意图为敌人意图2。
- 第2项预览：UnrespondedEnemyIntent 未来将处理无人响应敌人意图。
- 第2项预览：目标为 allyB 槽位2。

当前验证结论：

- BattleExecutionPlanExecutor.PrintExecutionPlanStepPreview(...) 可以作为第一版执行步骤预览器工作。
- BattleExecutionPlanExecutor.PrintExecutionPlanStepPreview(...) 与 BattleExecutionPlanManager.PrintExecutionPlan(...) 职责不同。
- BattleExecutionPlanManager.PrintExecutionPlan(...) 打印计划内容。
- BattleExecutionPlanExecutor.PrintExecutionPlanStepPreview(...) 打印未来执行阶段会如何处理。
- 当前没有误写成正式执行器。
- 当前没有新增 ExecuteExecutionPlan(...)。

当前没有进入正式结算：

- 槽位 已使用：False。
- allyA 基础攻击 UseCount 仍为 0 / 3。
- 罪卡测试卡 UseCount 仍为 0 / 2。
- 没有拼点日志。
- 没有敌人攻击。
- 没有 Resolved。
- 没有负罪感增加。
- 没有 UseCount 增加。
- 没有调用 slot.MarkUsed()。
- 没有调用 ExecuteActionSlots(...)。
- 没有切换 item.isCompleted。
- 没有切换 plan.isCompleted。

当前不实现：

- ExecuteExecutionPlan(...)。
- 正式执行器方法。
- BattleResolver 接入。
- ExecuteActionSlots(...) 接入。
- slot.MarkUsed()。
- 拼点。
- 敌人攻击。
- 无人响应效果。
- 响应失败效果。
- Resolved。
- 负罪感增加。
- UseCount 增加。
- item.isCompleted 切换。
- plan.isCompleted 切换。
- BattleEnemyIntent.isCompleted。
- respondingSlot。
- FreeAction 正式执行。

编译说明：

- 本次新增了 BattleExecutionPlanExecutor.cs。
- 普通 dotnet build ProjectGuilt.sln 曾因 Unity 工程文件未刷新而找不到新增脚本。
- 没有手动修改 .csproj。
- 回到 Unity 自动刷新后，Unity 测试已通过。
- 如后续再次运行 dotnet build，应在 Unity 刷新工程文件后再检查。

## 三十五、ActionSlotExecutionPlanStepPreviewEmpty 执行步骤预览空输入安全测试已完成

当前已完成小目标：

- 新增测试模式 ActionSlotExecutionPlanStepPreviewEmpty。
- 测试 PrintExecutionPlanStepPreview(null)。
- 测试 PrintExecutionPlanStepPreview(new BattleExecutionPlan())。
- 测试 CreateBasicExecutionPlan(null, null) 后调用步骤预览。
- 测试空 actionSlots / intentQueue 生成计划后调用步骤预览。

Unity 测试结果：

- 4 组都正常打印：===== BattleExecutionPlan 执行步骤预览 =====。
- 4 组都正常打印：提示：当前只预览执行步骤，不执行任何 item，不修改任何状态。
- 4 组都正常打印：当前 BattleExecutionPlan 没有可预览的执行步骤。
- 没有红色报错。
- 没有空引用异常。

当前没有进入正式结算：

- 没有拼点。
- 没有敌人攻击。
- 没有 Resolved。
- 没有 UseCount 增加。
- 没有负罪感增加。
- 没有 slot.MarkUsed()。

当前结论：

- PrintExecutionPlanStepPreview(...) 的空输入安全性已验证。
- 执行步骤预览层目前已有正常计划与空输入两类验证。
- 后续不再继续拆更多零散安全测试，除非出现具体问题。

## 三十六、UnrespondedEnemyIntent 命中目标预览文案增强已完成

修改内容：

- 只修改 BattleExecutionPlanExecutor.cs。
- 增强 PrintUnrespondedEnemyIntentStepPreview(...)。
- 将 UnrespondedEnemyIntent 的预览从单行概述改为更明确的多行命中目标预览。

当前新预览内容包括：

- 敌人意图编号。
- 敌人卡牌。
- 将命中角色。
- 将命中槽位。
- 当前仅预览命中目标，不造成伤害。

Unity 测试结果：

- 复用测试模式 ActionSlotExecutionPlanStepPreviewBasic。
- 已正确打印：敌人意图：敌人意图1。
- 已正确打印：敌人卡牌：敌人爪击。
- 已正确打印：将命中角色：我方角色B。
- 已正确打印：将命中槽位：槽位2。
- 已正确打印：当前仅预览命中目标，不造成伤害。

当前没有进入正式结算：

- 没有拼点。
- 没有敌人攻击。
- 没有 Resolved。
- 没有 UseCount 增加。
- 没有负罪感增加。
- 没有 slot.MarkUsed()。

当前结论：

- UnrespondedEnemyIntent 的命中目标预览已经更清楚。
- 当前仍只是执行步骤预览。
- 不代表正式敌人攻击或伤害结算。

## 三十七、UnrespondedEnemyIntent 敌人攻击点数范围预览完成

修改内容：

- 只修改 BattleExecutionPlanExecutor.cs。
- 增强 BattleExecutionPlanExecutor.PrintUnrespondedEnemyIntentStepPreview(...)。
- 现在会从 enemyIntent.enemyCardState.cardData.minPoint / maxPoint 只读显示敌人攻击点数范围。

Unity 测试结果：

- 复用测试模式 ActionSlotExecutionPlanStepPreviewBasic。
- Console 已确认显示：敌人攻击点数范围：2-8。
- Console 已确认显示：当前仅预览点数范围和命中目标，不 roll 点数，不造成伤害。

当前仍然不做：

- 不 roll 点数。
- 不计算伤害。
- 不扣血。
- 不调用 BattleResolver。
- 不调用 TakeDamage(...)。
- 不调用 slot.MarkUsed。
- 不切换 item.isCompleted。
- 不切换 plan.isCompleted。

当前结论：

- 本步骤只属于小安全测试记录。
- 当前仍然只是执行步骤预览。
- 当前不代表正式敌人攻击或伤害结算。

## 三十八、UnrespondedEnemyIntent 第一版正式执行通过

当前已完成小目标：

- BattleExecutionPlanExecutor.ExecuteExecutionPlan(...) 第一版已加入正式执行入口。
- 当前只正式执行 UnrespondedEnemyIntent。

UnrespondedEnemyIntent 第一版执行流程：

- 从敌人卡牌 minPoint / maxPoint roll enemyAttackPoint。
- 第一版 damage = enemyAttackPoint。
- 对 actualTargetCharacter 调用 TakeDamage(damage)。
- 成功后设置 item.isCompleted = true。
- 如果所有 item 完成，则设置 plan.isCompleted = true。

已通过测试模式 ActionSlotExecutionPlanExecuteUnrespondedBasic：

- 敌人攻击点数在 2-8 范围内。
- 目标角色 HP 正确下降。
- Console 显示 ExecutionPlan 是否完成：True。

已验证重复执行保护：

- 第二次执行同一个 BattleExecutionPlan 时，已完成 item 会跳过。
- HP 不会重复下降。
- Console 显示重复执行前后 HP 是否保持不变：True。

当前仍不做：

- 不处理 RespondedEnemyIntent。
- 不做玩家卡 vs 敌人卡拼点。
- 不调用 BattleResolver。
- 不触发 Resolved / Hit / AfterDamage / AfterKill。
- 不处理敌人卡牌 CD / UseCount。
- 不处理 Buff。

## 三十九、RespondedEnemyIntent 第一版攻击卡 vs 攻击卡执行通过

当前已完成小目标：

- BattleExecutionPlanExecutor.ExecuteRespondedEnemyIntent(...) 已加入第一版正式执行逻辑。
- 当前只支持 RespondedEnemyIntent 的攻击卡 vs 攻击卡。

第一版流程：

- 玩家卡 roll playerAttackPoint。
- 敌人卡 roll enemyAttackPoint。
- 点数高者获胜。
- 胜者点数作为基础伤害。
- 玩家胜时敌人 TakeDamage(playerAttackPoint)。
- 敌人胜时 actualTargetCharacter.TakeDamage(enemyAttackPoint)。

平局规则已实现：

- 平局会继续重 roll。
- 最多 10 次。
- 10 次仍平局则自动结束。
- 双方不造成伤害。
- 但当前 item 标记完成，避免 ExecutionPlan 卡住。

已通过 ActionSlotExecutionPlanExecuteRespondedBasic 测试中的玩家胜利分支：

- 玩家点数：10。
- 敌人点数：7。
- 敌人 HP：999 → 989。
- ExecutionPlan 是否完成：True。

当前仍不做：

- 不处理防御 / 闪避 / Ability / 特殊卡。
- 不调用 BattleResolver。
- 不触发 Resolved / Hit / AfterDamage / AfterKill。
- 不处理 CD / UseCount。
- 不处理 Buff。

后续仍需验证：

- 敌人胜利时我方目标扣血。
- 平局重 roll。
- 10 次平局自动结束。

## 四十、RespondedEnemyIntent 敌人胜利分支验证通过

当前已完成小目标：

- 新增并通过测试模式 ActionSlotExecutionPlanExecuteRespondedEnemyWin。
- 该测试使用内存测试卡牌数据稳定构造敌人胜利。

测试卡牌数据：

- 玩家攻击卡：minPoint = 1, maxPoint = 1。
- 敌人攻击卡：minPoint = 8, maxPoint = 8。

测试流程：

- 敌人原目标为我方角色B 槽位2。
- 我方角色A 槽位1响应该敌人意图。
- 响应后 actualTargetCharacter 改为我方角色A。

测试结果：

- 玩家点数：1。
- 敌人点数：8。
- 胜负结果：敌人胜利。
- 受伤角色：我方角色A。
- 造成伤害：8。
- 我方角色A HP：30 → 22。
- 我方角色B HP：30 → 30。
- 敌人 HP：999 → 999。
- ExecutionPlan 是否完成：True。

当前结论：

- RespondedEnemyIntent 第一版敌人胜利分支通过。
- 敌人胜利时会对 actualTargetCharacter 扣血，而不是回退到 originalTarget。

当前仍不做：

- 不处理防御 / 闪避 / Ability / 特殊卡。
- 不调用 BattleResolver。
- 不触发事件链。
- 不处理 CD / UseCount。
- 不处理 Buff。

后续仍需验证：

- 平局重 roll。
- 10 次平局自动结束。

## 四十一、RespondedEnemyIntent 平局重 roll 与 10 次上限验证通过

当前已完成小目标：

- 新增并通过测试模式 ActionSlotExecutionPlanExecuteRespondedTieLimit。
- 该测试使用内存测试卡牌稳定构造连续平局。

测试卡牌数据：

- 玩家攻击卡：minPoint = 5, maxPoint = 5。
- 敌人攻击卡：minPoint = 5, maxPoint = 5。

测试结果：

- 第 1 次到第 10 次拼点均为玩家点数 5、敌人点数 5。
- 连续 10 次仍未分出胜负后，RespondedEnemyIntent 自动结束。
- 双方都不造成伤害。
- 当前执行项标记完成，避免 ExecutionPlan 卡住。
- BattleExecutionPlan 已全部完成。

HP 验证：

- 我方角色A HP：30 → 30。
- 我方角色B HP：30 → 30。
- 敌人 HP：999 → 999。
- ExecutionPlan 是否完成：True。

当前结论：

- RespondedEnemyIntent 第一版平局重 roll 与 10 次上限分支通过。
- 当前规则按“连续拼点 10 次仍平局后自动结束”记录。

当前仍不做：

- 不处理防御 / 闪避 / Ability / 特殊卡。
- 不调用 BattleResolver。
- 不触发事件链。
- 不处理 CD / UseCount。
- 不处理 Buff。

## 四十二、BattleExecutionPlan 第一版正式执行闭环阶段总结

阶段背景：

- 当前项目处于 Action Slot / Enemy Intent / BattleExecutionPlan / BattleExecutionPlanExecutor 设计与实现阶段。
- 之前已经完成 BattleExecutionItem。
- 之前已经完成 BattleExecutionPlan。
- 之前已经完成 BattleExecutionPlanManager。
- 之前已经完成 BattleExecutionPlanExecutor 执行步骤预览。
- 本阶段从“只生成和预览执行计划”推进到“第一版正式执行”。

当前已完成的正式执行能力：

- BattleExecutionPlanExecutor.ExecuteExecutionPlan(...) 第一版已加入。
- 当前可以遍历 BattleExecutionPlan.executionItems。
- 当前可以根据执行项类型分派 RespondedEnemyIntent。
- 当前可以根据执行项类型分派 UnrespondedEnemyIntent。
- 当前会跳过已完成 item，避免重复执行重复扣血。
- 当前所有 item 完成后会设置 plan.isCompleted = true。

UnrespondedEnemyIntent 第一版正式执行已完成：

- 从敌人卡牌 minPoint / maxPoint roll enemyAttackPoint。
- 第一版 damage = enemyAttackPoint。
- 对 enemyIntent.actualTargetCharacter 调用 TakeDamage(damage)。
- 成功后设置 item.isCompleted = true。
- 已验证重复执行同一个 plan 时，已完成 item 会跳过，HP 不会重复下降。
- 已通过测试模式 ActionSlotExecutionPlanExecuteUnrespondedBasic。

RespondedEnemyIntent 第一版攻击卡 vs 攻击卡正式执行已完成：

- 当前只支持攻击卡 vs 攻击卡。
- 玩家卡 roll playerAttackPoint。
- 敌人卡 roll enemyAttackPoint。
- 点数高者获胜。
- 玩家胜利时，敌人受到 playerAttackPoint 伤害。
- 敌人胜利时，enemyIntent.actualTargetCharacter 受到 enemyAttackPoint 伤害。
- 平局时继续重新 roll。
- 连续 10 次仍未分出胜负时自动结束。
- 10 次平局自动结束时双方不造成伤害，但当前 item 标记完成，避免 ExecutionPlan 卡住。

已通过的 RespondedEnemyIntent 测试：

- ActionSlotExecutionPlanExecuteRespondedBasic：验证玩家胜利分支。
- ActionSlotExecutionPlanExecuteRespondedEnemyWin：验证敌人胜利分支。
- ActionSlotExecutionPlanExecuteRespondedEnemyWin 确认敌人胜利时扣 actualTargetCharacter，不回退 originalTarget。
- ActionSlotExecutionPlanExecuteRespondedTieLimit：验证连续 10 次平局后自动结束。
- ActionSlotExecutionPlanExecuteRespondedTieLimit 确认双方 HP 不下降，且 plan 能完成。

混合执行测试已通过：

- 已新增并通过 ActionSlotExecutionPlanExecuteMixedBasic。
- 同一个 BattleExecutionPlan 中同时包含 1 个 RespondedEnemyIntent。
- 同一个 BattleExecutionPlan 中同时包含 1 个 UnrespondedEnemyIntent。
- ExecuteExecutionPlan(...) 可以连续执行多个 item。
- 当前计划先执行 RespondedEnemyIntent。
- 当前计划再执行 UnrespondedEnemyIntent。
- 两个 item 都完成后，BattleExecutionPlan 完成。
- 测试中表现为玩家胜利导致敌人 HP 下降。
- 测试中表现为未响应敌人意图导致我方角色B HP 下降。
- Console 显示 ExecutionPlan 是否完成：True。

当前明确仍不做：

- 不调用 BattleResolver。
- 不触发 Resolved / Hit / AfterDamage / AfterKill。
- 不处理 CD / UseCount。
- 不处理 Buff。
- 不处理防御 / 闪避 / Ability / 特殊卡。
- 不处理正式伤害公式完整链路。
- 不处理动画和 UI。
- 不新增 BattleEnemyIntentResolver 或 DamageHandler。

当前阶段意义：

- 当前已经从“执行计划预览”进入“执行计划第一版正式执行”。
- Console 层面已经能跑通敌人意图生成。
- Console 层面已经能跑通玩家槽位响应。
- Console 层面已经能跑通 actualTarget 改写。
- Console 层面已经能跑通 BattleExecutionPlan 生成。
- Console 层面已经能跑通 Responded / Unresponded item 执行。
- Console 层面已经能跑通 roll 点。
- Console 层面已经能跑通基础伤害。
- Console 层面已经能跑通 HP 扣减。
- Console 层面已经能跑通 item 完成。
- Console 层面已经能跑通 plan 完成。
- 这代表战斗最小可玩闭环的核心执行部分已经迈过一大步。

下一步候选方向：

- 继续补 ExecuteExecutionPlan(...) 的回合结束相关逻辑，例如槽位使用状态、卡牌 CD / UseCount。
- 或先整理当前测试模式，减少测试代码膨胀。
- 或进入“基础回合流程闭环”设计：回合开始 → 生成敌人意图 → 玩家安排槽位 → 生成计划 → 执行计划 → 回合结束。
- 暂时不建议立刻接入复杂 Buff / 事件链 / 正式 UI。

## 四十三、Action Slot / Enemy Intent 后续设计记录

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

同一敌人意图的响应覆盖规则补充：

当前风险背景：

- 当前 BattleActionSlot.enemyIntent 可以指向某个敌人意图。
- 当前 FindSlotByEnemyIntent(...) 通过扫描槽位查找绑定某个敌人意图的槽位。
- 如果未来多个槽位同时绑定同一个 enemyIntent，会导致主要响应槽位不明确。
- 因此需要明确同一敌人意图的响应覆盖规则。

设计原则：

- 同一个敌人意图同一时间只能有一个主要响应槽位。
- 如果玩家后来选择另一个槽位响应该敌人意图，则以后一次成功选择为准。
- 新响应槽位覆盖旧响应槽位。
- 敌人意图的 actualTargetCharacter / actualTargetSlotIndex 改为新响应槽位。

旧响应槽位处理原则：

- 旧槽位应解除与该敌人意图的响应绑定。
- 旧槽位不再处理该敌人意图。
- 旧槽位解除绑定后不能简单自动变成“偷刀”或普通攻击。
- 旧槽位后续状态应取决于卡牌类型和玩家后续选择。
- 攻击卡未来可能允许转为普通行动 / FreeAction。
- 防御、闪避、能力卡或必须绑定敌人意图的卡，不应被硬写死为单方面攻击。

当前已有字段限制：

- 当前 BattleEnemyIntent 只有 actualTargetCharacter / actualTargetSlotIndex / isResponded。
- 当前没有 respondingSlot 字段。
- 当前通过扫描 actionSlots 反查绑定槽位。
- 当前不引入 BattleEnemyIntent -> BattleActionSlot 双向引用。

当前不实现：

- 不实现响应覆盖逻辑。
- 不实现旧槽位解除绑定。
- 不实现旧槽位转 FreeAction。
- 不新增 respondingSlot。
- 不新增正式执行队列。
- 不执行槽位。
- 不执行敌人意图。
- 不触发拼点。
- 不触发敌人攻击。
- 不触发 Resolved。
- 不增加负罪感。
- 不增加 UseCount。
- 不调用 slot.MarkUsed()。
- 不新增 isCompleted。

当前结论：

- 响应覆盖规则是未来正式执行队列前必须解决的设计点。
- 当前只记录设计边界。
- 后续如果要实现，需要先设计旧槽位清理和后续状态处理。

响应覆盖逻辑未来最小安全实现路径：

当前结构审查结论：

- AssignResponseToEnemyIntent(...) 是未来实现“后一次成功响应覆盖前一次成功响应”的最小安全切入点。
- 因为所有响应安排入口集中在 AssignResponseToEnemyIntent(...)。
- AssignResponseToEnemyIntent(...) 已经负责速度检查、槽位占用检查、重复卡检查、slot.AssignResponse(...)、enemyIntent.MarkResponded()。

覆盖逻辑未来插入位置：

- 应放在新响应所有失败检查之后。
- 应放在 slot.AssignResponse(...) 之前。
- 应放在 enemyIntent.MarkResponded() 之前。
- 这样可以避免新响应失败时误清理旧响应槽位。

未来查找旧响应槽位：

- 可以继续扫描 actionSlots。
- 短期可以新增 FindSlotsByEnemyIntent(...)。
- 不建议只依赖当前 FindSlotByEnemyIntent(...)，因为它只返回第一个匹配槽位。
- FindSlotsByEnemyIntent(...) 可以发现并清理历史异常状态。
- 当前暂不新增 respondingSlot，避免提前引入 BattleEnemyIntent -> BattleActionSlot 双向引用。

旧槽位解除绑定的未来最小安全方案：

- 不建议直接调用 Clear()。
- 因为 Clear() 会清掉 actor / cardState / slotType，等于替玩家取消该槽位安排。
- 更低风险方案是未来在 BattleActionSlot 增加 UnbindEnemyIntent()。
- UnbindEnemyIntent() 只解除该槽位与敌人意图的绑定。
- 例如清掉 enemyIntent 和相关目标引用。
- 保留 actor / cardState / slotType。
- 保持 isUsed = false。

旧槽位解除绑定后：

- 不应自动转为 FreeAction。
- 不应自动变成偷刀。
- 攻击卡未来可能允许重新指定普通行动。
- 防御 / 闪避 / 能力卡不应被硬转成普通攻击。
- 旧槽位后续状态需要根据卡牌类型和玩家后续选择再设计。

isResponded 判断：

- 覆盖场景下，新槽位仍然响应同一个敌人意图，所以 isResponded 保持 true 足够。
- 只有未来出现“取消所有响应”的功能时，才需要考虑 UnmarkResponded() 或更明确的响应状态管理。
- 当前不需要实现 UnmarkResponded()。

对 Preview 系统的影响：

- 如果未来覆盖逻辑正确清理旧绑定，FindSlotByEnemyIntent(...) 就不会遇到多个槽位绑定同一意图的正常情况。
- BattleHandlingPreviewItem 当前不需要修改。
- CreateSpeedPriorityHandlingPreviewItems(...) 当前不需要修改。
- Preview 系统继续通过扫描槽位反查绑定关系即可。

未来测试模式建议：

- 测试模式名称可用：ActionSlotResponseOverwriteBasic。
- 测试目标：敌人意图攻击 allyB 槽位1。
- 测试目标：创建 2 个行动槽位。
- 测试目标：allyA 速度高于 enemy。
- 测试目标：allyA 用槽位1响应该敌人意图，成功。
- 测试目标：再用另一张独立 BattleCardState 安排到槽位2，响应同一个敌人意图。
- 测试目标：第二次响应成功覆盖第一次响应。
- 测试目标：不执行拼点。
- 测试目标：不触发 Resolved。
- 测试目标：不增加负罪感。
- 测试目标：不增加 UseCount。

未来预期日志重点：

- 第一次响应成功，actualTarget 改为 allyA 槽位1。
- 第二次响应成功，覆盖旧响应。
- 旧槽位解除敌人意图绑定。
- 敌人意图最终 actualTarget 为新响应槽位。
- Preview 只显示该敌人意图由新槽位处理。
- 旧槽位不再处理该敌人意图。
- 没有拼点、没有 Resolved、没有负罪感变化、没有 UseCount 增加。

当前阶段不实现：

- 不实现响应覆盖逻辑。
- 不实现 FindSlotsByEnemyIntent(...)。
- 不实现 UnbindEnemyIntent()。
- 不实现旧槽位解除绑定。
- 不实现旧槽位转 FreeAction。
- 不新增 respondingSlot。
- 不新增 UnmarkResponded()。
- 不新增正式执行队列。
- 不执行槽位。
- 不执行敌人意图。
- 不触发拼点。
- 不触发敌人攻击。
- 不触发 Resolved。
- 不增加负罪感。
- 不增加 UseCount。
- 不调用 slot.MarkUsed()。
- 不新增 isCompleted。

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

### 14. 未来可能需要处理预览项数据结构

当前已有以下日志预览工具：

- PrintIntentHandlingPreview(...)。
- PrintActionSlotIntentHandlingPreview(...)。
- PrintSpeedPriorityHandlingPreview(...)。

它们目前都是直接通过 Debug.Log 打印处理路径或顺序预览。

当前没有真正生成队列数据结构。

未来如果要从日志预览升级到正式执行队列，中间可以先增加“处理预览项 / 处理计划项”数据结构。

命名倾向：

- 暂时倾向使用 BattleHandlingPreviewItem。
- 不建议现在直接叫 BattleExecutionItem。

原因是当前仍然只是预览，不是真正执行。

Execution 容易让人误解为已经进入正式战斗执行队列。

BattleHandlingPreviewItem 未来可能包含的信息：

- order：预览顺序编号。
- handlingType：处理类型，例如已响应意图 / 未响应意图。
- enemyIntent：关联的敌人意图。
- actionSlot：如果是已响应意图，则记录对应行动槽位；如果是未响应意图，可以为空。

handlingType 未来更建议使用 enum，而不是 string。

例如未来可能有：

- RespondedIntent。
- UnrespondedIntent。

原因是 string 容易写错，不利于后续扩展。

当前暂不实现该数据结构，原因：

- 还没有正式执行队列。
- 还没有 isCompleted。
- 还没有完整速度排序。
- 还没有未响应敌人意图正式结算。
- 还没有已响应敌人意图正式结算。
- 现在过早创建 BattleExecutionItem 容易把预览和执行混在一起。

当前结论：

- 未来确实可能需要从纯日志预览升级为“预览项列表”。
- 当前阶段仍然只保留日志预览。
- 等需要从预览走向正式队列时，再单独小步实现 BattleHandlingPreviewItem 或类似结构。

当前不实现：

- BattleHandlingPreviewItem。
- BattleExecutionItem。
- 正式执行队列。
- 处理计划列表。
- 完整速度排序。
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

### 15. BattleHandlingPreviewItem 当前保持纯数据结构

当前结论：

- BattleHandlingPreviewItem 当前只作为处理预览项数据结构。
- BattleHandlingPreviewItem 只负责保存预览项数据。
- BattleHandlingPreviewItem 暂时不负责生成 Console 文本。
- BattleHandlingPreviewItem 暂时不负责生成 UI 显示文本。
- BattleHandlingPreviewItem 暂时不负责执行任何战斗逻辑。

当前字段仍保持：

- order。
- handlingType。
- enemyIntent。
- actionSlot。

暂不增加：

- GetDescriptionText()。
- ToDisplayText()。
- Execute()。
- Resolve()。
- 任何显示文本生成方法。
- 任何执行方法。

设计原因：

- BattleHandlingPreviewItem 属于数据层。
- 如果让它生成中文日志文本，会让数据结构承担显示职责。
- 后续 Console 日志、战斗 UI、调试面板、正式执行系统可能需要不同格式。
- 因此当前不把显示逻辑写进 item 本身。

当前分工：

- BattleHandlingPreviewItem：只存数据。
- BattleActionSlotManager：当前负责生成 preview items，并负责 Console 调试打印。
- 未来 UI 层：可以根据 preview item 数据生成 UI 显示。
- 未来正式执行系统：可以根据更成熟的数据结构生成正式执行队列。

当前不实现：

- 显示文本方法。
- UI 显示。
- 正式执行队列。
- BattleExecutionItem。
- BattleExecutionPlan。
- BattleExecutionQueue。
- ExecutePreviewItems(...)。
- 拼点。
- 敌人攻击。
- Resolved。
- 负罪感增加。
- UseCount 增加。
- slot.MarkUsed()。
- isCompleted。
- respondingSlot。

当前结论：

- 暂时保持 BattleHandlingPreviewItem 为纯数据结构。
- 打印逻辑继续留在 BattleActionSlotManager.PrintSpeedPriorityHandlingPreview(...)。
- 等未来出现正式 UI 或正式执行队列时，再决定是否增加专门的格式化器或执行项结构。

### 16. BattleHandlingPreviewItem 未来可增加只读语义判断属性

当前 BattleHandlingPreviewItem 保持纯数据结构：

- 字段包括 order / handlingType / enemyIntent / actionSlot。
- 当前不负责显示文本。
- 当前不负责执行战斗逻辑。

未来可以考虑增加只读语义判断属性，例如：

- IsRespondedIntent。
- IsUnrespondedIntent。

这些属性的含义：

- IsRespondedIntent 只判断 handlingType == BattleHandlingPreviewType.RespondedIntent。
- IsUnrespondedIntent 只判断 handlingType == BattleHandlingPreviewType.UnrespondedIntent。

为什么这类属性可以接受：

- 它们只解释 item 自身数据。
- 不生成 Console 文本。
- 不生成 UI 文本。
- 不执行战斗。
- 不修改任何状态。
- 不触发拼点、敌人攻击、Resolved、UseCount 或负罪感。

和显示文本方法区分：

- 当前仍不增加 GetDescriptionText()。
- 当前仍不增加 ToDisplayText()。
- 原因是显示文本属于 Console / UI / 调试面板职责，不应提前塞进 Preview Item 数据类。

当前阶段不实现这些属性，原因：

- 第一版只有 RespondedIntent / UnrespondedIntent 两种类型。
- 外部直接判断 handlingType 仍然简单。
- 等未来 handlingType 增加更多类型时，再考虑添加这些只读判断属性更合适。

当前不实现：

- IsRespondedIntent。
- IsUnrespondedIntent。
- GetDescriptionText()。
- ToDisplayText()。
- Execute()。
- Resolve()。
- 正式执行队列。
- 拼点。
- 敌人攻击。
- Resolved。
- 负罪感增加。
- UseCount 增加。
- slot.MarkUsed()。
- isCompleted。
- respondingSlot。

### 17. BattleHandlingPreviewItem 不直接升级为正式执行项

当前结论：

- BattleHandlingPreviewItem 当前只作为处理预览项。
- BattleHandlingPreviewItem 用于调试、预览、未来 UI 展示。
- BattleHandlingPreviewItem 不负责正式战斗执行。
- BattleHandlingPreviewItem 不应该直接升级成正式执行项。

未来正式执行队列应另行设计：

- 未来如果需要正式执行队列，可以单独设计 BattleExecutionItem、BattleExecutionPlan 或类似结构。
- 这些结构才负责真正执行、结算、完成状态记录。

BattleHandlingPreviewItem 与未来 BattleExecutionItem 的职责区别：

- BattleHandlingPreviewItem 用于准备阶段或调试阶段。
- BattleHandlingPreviewItem 描述未来可能的处理顺序。
- BattleHandlingPreviewItem 不改变战斗状态。
- BattleHandlingPreviewItem 不执行拼点。
- BattleHandlingPreviewItem 不执行敌人攻击。
- BattleHandlingPreviewItem 不消耗卡牌。
- BattleHandlingPreviewItem 不触发 Resolved。
- BattleHandlingPreviewItem 不标记 slot.isUsed。
- BattleHandlingPreviewItem 不标记 enemyIntent.isCompleted。
- BattleExecutionItem 未来可能用于真正执行阶段。
- BattleExecutionItem 未来可能负责执行拼点、敌人攻击、无人响应效果、响应失败效果。
- BattleExecutionItem 未来可能负责记录完成状态、执行结果、失败原因等。
- BattleExecutionItem 当前阶段不实现。

为什么不混用：

- 如果 PreviewItem 同时负责预览和执行，容易造成职责混乱。
- UI 或调试日志只想查看顺序，不应有触发执行的风险。
- 正式执行会改变战斗状态，而预览不应改变战斗状态。
- 预览文本、UI 显示、正式执行需要的字段和格式未来可能不同。

未来可能流程：

- 准备阶段生成 BattleHandlingPreviewItem 列表，用于显示和调试。
- 回合正式开始执行时，再根据最终战斗数据生成正式执行队列。
- 正式执行队列未来可以使用独立的 BattleExecutionItem 或类似结构。

当前阶段不实现：

- BattleExecutionItem。
- BattleExecutionPlan。
- BattleExecutionQueue。
- ExecutePreviewItems(...)。
- ExecuteBattleQueue(...)。
- 正式执行队列。
- 完整速度排序。
- 玩家槽位执行。
- 敌人意图执行。
- 拼点。
- 敌人攻击。
- 无人响应效果。
- 响应失败效果。
- Resolved。
- 负罪感增加。
- UseCount 增加。
- slot.MarkUsed()。
- isCompleted。
- respondingSlot。

当前结论：

- BattleHandlingPreviewItem 继续保持 Preview 定位。
- 未来正式执行项另行设计。
- 当前只记录职责边界，不进入代码实现。

### 18. BattleExecutionItem / BattleExecutionPlan 第一版设计草案

当前阶段判断：

- 当前 Action Slot / Enemy Intent 安排阶段已经具备进入正式执行队列设计阶段的基础。
- 但当前只进入设计阶段。
- 当前不进入正式执行队列代码实现。
- 当前不执行拼点、敌人攻击、伤害、Resolved、UseCount、负罪感等逻辑。

Preview 与 Execution 的边界：

- BattleHandlingPreviewItem 继续作为准备阶段 / 调试 / UI 预览项。
- BattleHandlingPreviewItem 不直接升级为正式执行项。
- 未来正式执行队列应另行设计 BattleExecutionItem / BattleExecutionPlan。
- PreviewItem 不改变战斗状态。
- ExecutionItem 未来才代表真正要被系统执行的队列项。

BattleExecutionItem 第一版字段草案：

- int order：正式执行顺序编号。
- BattleExecutionItemType executionType：执行项类型。
- BattleEnemyIntent enemyIntent：关联敌人意图。
- BattleActionSlot actionSlot：关联行动槽位。
- bool isCompleted：该执行项是否已经完成正式结算。

BattleExecutionItemType 第一版枚举草案：

- RespondedEnemyIntent：玩家槽位响应敌人意图。
- UnrespondedEnemyIntent：敌人意图无人响应，未来按当前 actualTarget 执行。
- FreeAction：普通行动 / 偷刀。

字段含义补充：

- RespondedEnemyIntent 通常同时有 enemyIntent 和 actionSlot。
- UnrespondedEnemyIntent 通常有 enemyIntent，actionSlot 可以为空。
- FreeAction 通常有 actionSlot，enemyIntent 可以为空。

关于 isCompleted：

- 第一版设计倾向先放在 BattleExecutionItem 上。
- 因为正式执行队列首先需要知道“这个执行项是否完成”。
- BattleEnemyIntent.isCompleted 未来可能也需要，但当前暂不实现。
- 这样可以避免过早把敌人意图自身状态、响应状态和正式执行完成状态混在一起。

关于 FreeAction：

- 设计草案中应保留 FreeAction 类型。
- 因为玩家未来可以选择不响应敌人意图，转而普通攻击 / 偷刀。
- 但第一版代码原型可以暂时不实现完整 FreeAction 混排。
- 需要等正式执行队列排序规则更清楚后再实现。

当前不实现：

- BattleExecutionItem。
- BattleExecutionPlan。
- BattleExecutionItemType。
- 正式执行队列代码。
- 完整速度排序。
- FreeAction 混排。
- 玩家槽位执行。
- 敌人意图执行。
- 拼点。
- 敌人攻击。
- 无人响应效果。
- 响应失败效果。
- Resolved。
- 负罪感增加。
- UseCount 增加。
- slot.MarkUsed()。
- BattleEnemyIntent.isCompleted。
- respondingSlot。

当前结论：

- 下一阶段可以围绕 BattleExecutionItem / BattleExecutionPlan 继续设计。
- 当前只记录第一版字段和职责边界。
- 后续如果进入代码实现，需要继续小步推进，先生成执行计划，不直接执行。

BattleExecutionPlan 第一版职责草案：

BattleExecutionItem 与 BattleExecutionPlan 的关系：

- BattleExecutionItem 表示正式执行队列中的单个执行项。
- BattleExecutionPlan 表示一整个回合的正式执行计划。
- 一个 BattleExecutionPlan 内部包含多个 BattleExecutionItem。

BattleExecutionPlan 第一版可能字段草案：

- List<BattleExecutionItem> executionItems：保存本回合正式执行项列表。
- bool isCompleted：表示整个执行计划是否已经全部完成。

BattleExecutionPlan 未来可能扩展：

- 当前执行索引。
- 执行阶段。
- 执行结果统计。
- 是否中断。
- 调试打印信息。

BattleExecutionPlan 第一版职责：

- 只负责保存本回合最终执行顺序。
- 可以用于打印正式执行计划预览。
- 可以作为未来正式执行器的输入。
- 不直接执行战斗逻辑。
- 不直接调用拼点、敌人攻击、卡牌消耗、负罪感、Resolved 等逻辑。

BattleExecutionPlan 与 BattleHandlingPreviewItem / Preview 系统的区别：

- BattleHandlingPreviewItem 用于准备阶段、调试和 UI 预览。
- BattleExecutionPlan 用于正式回合执行阶段。
- Preview 可以辅助玩家理解未来处理方向。
- ExecutionPlan 应代表回合开始后系统最终确认的执行计划。
- 两者可以参考同一批数据，但不应混用为同一个结构。

BattleExecutionPlan 第一版生成方向：

- 未来可以根据 actionSlots 生成正式执行项列表。
- 未来可以根据 intentQueue 生成正式执行项列表。
- 未来可以根据速度规则生成正式执行项列表。
- 未来可以根据响应绑定关系生成正式执行项列表。
- 未来可以根据未响应敌人意图生成正式执行项列表。
- 未来可以根据 FreeAction 生成正式执行项列表。
- 第一版可以先生成计划并打印，不立即执行。
- 等计划生成稳定后，再进入逐项执行。

当前不实现：

- BattleExecutionItem。
- BattleExecutionPlan。
- BattleExecutionItemType。
- 正式执行计划生成代码。
- 正式执行器。
- 完整速度排序。
- FreeAction 混排。
- 玩家槽位执行。
- 敌人意图执行。
- 拼点。
- 敌人攻击。
- 无人响应效果。
- 响应失败效果。
- Resolved。
- 负罪感增加。
- UseCount 增加。
- slot.MarkUsed()。
- BattleEnemyIntent.isCompleted。
- respondingSlot。

当前结论：

- BattleExecutionPlan 应作为未来正式执行队列的容器。
- BattleExecutionPlan 和 BattleHandlingPreviewItem 分属不同阶段。
- 当前只记录职责边界和字段草案。
- 后续如果进入代码实现，应先生成并打印 ExecutionPlan，不直接执行。

BattleExecutionPlan 第一版生成规则草案：

当前阶段：

- 已经进入正式执行队列设计阶段。
- 当前只记录 BattleExecutionPlan 第一版生成规则。
- 当前不进入代码实现。
- 当前不执行战斗逻辑。

第一版生成目标：

- 根据 actionSlots + intentQueue 生成一个正式执行计划。
- 第一版只生成并打印计划。
- 第一版不执行计划。
- 第一版不触发拼点、敌人攻击、伤害、Resolved、UseCount、负罪感等正式结算。

第一版暂时只处理两类执行项：

- RespondedEnemyIntent：已经被玩家槽位成功响应的敌人意图。
- RespondedEnemyIntent 需要关联 enemyIntent 和对应 actionSlot。
- UnrespondedEnemyIntent：没有被任何玩家槽位响应的敌人意图。
- UnrespondedEnemyIntent 需要关联 enemyIntent。
- UnrespondedEnemyIntent 的 actionSlot 可以为空。

第一版暂不混入：

- FreeAction。
- 旧槽位解除绑定后的中间状态。
- 普通偷刀行动。
- 防御 / 闪避空挂。
- 完整速度排序。

第一版生成顺序草案：

- 可以沿用当前“速度响应优先方向”的简化规则。
- 先生成 RespondedEnemyIntent 项。
- 再生成 UnrespondedEnemyIntent 项。
- 当前仍不代表最终完整速度队列。
- 当前只是正式执行计划第一版生成规则。
- 后续完整速度排序需要单独设计。

与 BattleHandlingPreviewItem 的关系：

- BattleHandlingPreviewItem 用于准备阶段 / 调试 / UI 预览。
- BattleExecutionPlan 用于正式回合执行阶段。
- 第一版生成规则可能和 Preview 规则相似，但职责不同。
- 不应直接把 PreviewItem 当 ExecutionItem 执行。

关于旧槽位中间状态：

- 旧槽位解除绑定后可能存在 cardState != null。
- 旧槽位解除绑定后可能存在 actor != null。
- 旧槽位解除绑定后可能存在 slotType == RespondToEnemyIntent。
- 旧槽位解除绑定后可能存在 enemyIntent == null。
- 旧槽位解除绑定后可能存在 target == null。
- 第一版 ExecutionPlan 生成暂不处理这种槽位。
- 不把它自动转成 FreeAction。
- 不自动清空。
- 后续需要单独设计“未绑定旧槽位如何处理”。

关于 FreeAction：

- BattleExecutionItemType 草案中保留 FreeAction。
- 但第一版 ExecutionPlan 生成暂不加入 FreeAction。
- 因为普通行动 / 偷刀还涉及目标选择、速度排序、和敌人意图混排等问题。
- 后续单独设计。

当前不实现：

- BattleExecutionItem。
- BattleExecutionPlan。
- BattleExecutionItemType。
- ExecutionPlan 生成代码。
- ExecutionPlan 打印代码。
- 正式执行器。
- 完整速度排序。
- FreeAction 混排。
- 旧槽位中间状态处理。
- 玩家槽位执行。
- 敌人意图执行。
- 拼点。
- 敌人攻击。
- 无人响应效果。
- 响应失败效果。
- Resolved。
- 负罪感增加。
- UseCount 增加。
- slot.MarkUsed()。
- BattleEnemyIntent.isCompleted。
- respondingSlot。

当前结论：

- 第一版 BattleExecutionPlan 生成应先聚焦已响应 / 未响应敌人意图。
- 暂不混入 FreeAction。
- 暂不执行。
- 等生成规则稳定后，再进入代码原型。

### 19. BattleExecutionPlanExecutor 第一版职责草案

当前阶段判断：

- BattleExecutionPlan 生成 / 打印阶段可以暂时收口。
- 当前已经完成正常计划生成测试。
- 当前已经完成空计划 / 空队列安全测试。
- 当前已经完成已响应但缺少绑定槽位安全测试。
- 当前可以进入正式执行器设计阶段。
- 当前不进入正式执行器代码实现阶段。

BattleExecutionPlanManager 与未来 BattleExecutionPlanExecutor 的分工：

- BattleExecutionPlanManager 负责生成 BattleExecutionPlan。
- BattleExecutionPlanManager 负责打印 BattleExecutionPlan。
- BattleExecutionPlanManager 只读 actionSlots / intentQueue。
- BattleExecutionPlanManager 不执行战斗逻辑。
- BattleExecutionPlanManager 不修改战斗状态。
- BattleExecutionPlanExecutor 未来负责逐项处理 BattleExecutionPlan.executionItems。
- BattleExecutionPlanExecutor 未来才负责真正进入执行流程。
- BattleExecutionPlanExecutor 未来可能根据 BattleExecutionItemType 分派处理逻辑。
- 当前只做职责设计，不实现。

BattleExecutionPlanExecutor 第一版定位：

- 第一版可以先设计为执行计划遍历器 / 执行器。
- 第一版代码原型即使实现，也应先只遍历并打印“将要执行什么”。
- 第一版不应直接接入拼点、敌人攻击、Resolved、UseCount、负罪感等正式结算。
- 真正结算应在后续阶段逐步接入。

未来可能的方法名草案：

- ExecuteExecutionPlan(BattleExecutionPlan executionPlan)：未来正式执行入口。
- PreviewExecuteExecutionPlan(...)：更偏安全预览的第一版命名。
- PrintExecutionPlanExecutionPreview(...)：更明确地表示只打印执行预览。
- 当前暂不决定最终方法名。
- 当前不实现任何方法。

第一版执行器未来处理方向：

- 遍历 BattleExecutionPlan.executionItems。
- 根据 BattleExecutionItemType 判断类型。
- 可处理类型包括 RespondedEnemyIntent。
- 可处理类型包括 UnrespondedEnemyIntent。
- 可处理类型包括 FreeAction。
- 第一版可以先只打印每一项将如何处理。
- 第一版不改变 item.isCompleted。
- 第一版不改变 plan.isCompleted。
- 第一版不调用任何结算方法。

关于 isCompleted 的设计边界：

- BattleExecutionItem.isCompleted 未来用于记录单个执行项是否完成。
- BattleExecutionPlan.isCompleted 未来用于记录整个执行计划是否完成。
- 当前不切换这些状态。
- 未来只有真正执行 item 并完成结算后，才考虑把 item 标记为完成。
- 只有所有 item 完成后，才考虑把 plan 标记为完成。
- 当前暂不实现 MarkCompleted() 或类似方法。

关于正式结算的边界：

- 未来 RespondedEnemyIntent 可能进入拼点 / 响应处理。
- 未来 UnrespondedEnemyIntent 可能进入敌人无人响应处理。
- 未来 FreeAction 可能进入普通行动 / 偷刀处理。
- 当前不实现这些逻辑。
- 当前只记录未来方向。

当前不建议做：

- 不直接写 ExecuteExecutionPlan(...)。
- 不接入 BattleResolver。
- 不执行 RespondedEnemyIntent。
- 不执行 UnrespondedEnemyIntent。
- 不混入 FreeAction。
- 不处理旧槽位中间状态。
- 不新增 BattleEnemyIntent.isCompleted。
- 不新增 respondingSlot。
- 不触发 Resolved / UseCount / guiltGain / slot.MarkUsed()。

可选后续测试记录：

- ActionSlotExecutionPlanMultiBasic 已作为回归测试补充完成。
- ActionSlotExecutionPlanMultiBasic 已用于验证多个已响应 / 多个未响应意图的 order 顺序。
- ActionSlotExecutionPlanMultiBasic 仍不代表正式执行队列，只固定当前第一版简化顺序。
- 后续进入执行器代码原型前，可以继续把它作为回归测试参考。

当前结论：

- 下一阶段进入 BattleExecutionPlanExecutor 职责设计。
- 当前只记录职责边界。
- 当前不写执行器代码。
- 当前不进入正式结算。

### 20. BattleExecutionPlanExecutor 命名与第一版方法边界记录

当前结论：

- 当前已在后续阶段新增 BattleExecutionPlanExecutor.cs 的第一版执行步骤预览。
- 当前暂不实现 ExecuteExecutionPlan(...)。
- 当前 BattleExecutionPlanManager.PrintExecutionPlan(...) 仍负责打印计划内容。
- 当前仍不应引入真正执行 / 正式结算代码。

类名判断：

- BattleExecutionPlanExecutor 适合作为未来真正执行器的最终类名。
- 当前仅创建执行步骤预览版本，不代表正式执行器已经完成。
- Executor 语义较强，容易让人误以为已经进入正式执行 / 正式结算阶段。

方法名判断：

- 暂时不要使用 ExecuteExecutionPlan(...)。
- ExecuteExecutionPlan(...) 听起来像会真正执行计划、修改状态、触发结算。
- 如果第一版只做“遍历并打印将要执行什么”，更安全的方法名可以是 PrintExecutionPlanStepPreview(BattleExecutionPlan executionPlan)。
- 备选方法名可以是 PrintExecutionPreview(...)。
- 备选方法名也可以是 PrintExecutionPlanExecutionPreview(...)。
- 当前已采用 PrintExecutionPlanStepPreview(...) 作为第一版执行步骤预览方法名。

当前 BattleExecutionPlanExecutor.cs 第一版职责边界：

- 遍历 BattleExecutionPlan.executionItems。
- 根据 BattleExecutionItemType 打印每个 item 未来将如何处理。
- 不执行任何 item。
- 不修改 item.isCompleted。
- 不修改 plan.isCompleted。
- 不调用 BattleResolver。
- 不调用 slot.MarkUsed()。
- 不触发 Resolved / UseCount / guiltGain。
- 不混入 FreeAction 的正式执行。
- 不处理旧槽位中间状态的正式执行。

与 BattleExecutionPlanManager.PrintExecutionPlan(...) 的区别：

- PrintExecutionPlan(...) 当前打印的是计划内容。
- PrintExecutionPlan(...) 打印第几项。
- PrintExecutionPlan(...) 打印 item 类型。
- PrintExecutionPlan(...) 打印关联敌人意图。
- PrintExecutionPlan(...) 打印关联槽位。
- PrintExecutionPlan(...) 打印当前目标。
- 未来 PrintExecutionPlanStepPreview(...) 应打印执行阶段将如何处理。
- 未来 RespondedEnemyIntent 将进入玩家响应处理。
- 未来 UnrespondedEnemyIntent 将进入无人响应敌人意图处理。
- 未来 FreeAction 将进入普通行动处理。
- 当前已新增执行步骤预览类，用于固定两者职责差异。

下一步建议：

- 已新增执行步骤预览代码。
- ActionSlotExecutionPlanMultiBasic 已补充完成。
- ActionSlotExecutionPlanMultiBasic 已验证多个已响应 / 多个未响应敌人意图时，CreateBasicExecutionPlan(...) 的 order 顺序稳定。
- 后续再考虑正式执行器代码和 ExecuteExecutionPlan(...)。

当前仍不实现：

- ExecuteExecutionPlan(...)。
- 正式执行器。
- item.isCompleted 切换。
- plan.isCompleted 切换。
- BattleResolver 接入。
- ExecuteActionSlots(...) 接入。
- slot.MarkUsed()。
- 拼点。
- 敌人攻击。
- Resolved。
- 负罪感增加。
- UseCount 增加。
- BattleEnemyIntent.isCompleted。
- respondingSlot。
- FreeAction 混排。

### 21. BattleExecutionPlan 生成 / 打印阶段暂时收口记录

当前阶段结论：

- BattleExecutionPlan 生成 / 打印阶段可以暂时收口。
- 当前已经具备进入 BattleExecutionPlanExecutor 设计阶段的基础。
- 当前已完成 BattleExecutionPlanExecutor 第一版执行步骤预览代码。
- 当前仍不进入正式执行器 / 正式结算代码实现。
- 当前仍不接入拼点、敌人攻击、Resolved、UseCount、负罪感、slot.MarkUsed() 等正式结算逻辑。

已完成基础结构：

- BattleExecutionItem.cs 已包含 BattleExecutionItemType。
- BattleExecutionItem.cs 已包含 BattleExecutionItem。
- BattleExecutionPlan.cs 已包含 BattleExecutionPlan。
- BattleExecutionPlan.cs 已包含 executionItems。
- BattleExecutionPlan.cs 已包含 isCompleted。
- BattleExecutionPlan.cs 已包含 AddItem(...)。
- BattleExecutionPlanManager.cs 已包含 CreateBasicExecutionPlan(...)。
- BattleExecutionPlanManager.cs 已包含 PrintExecutionPlan(...)。

已完成测试覆盖：

- ActionSlotExecutionPlanBasic：验证 1 个已响应 + 1 个未响应时，ExecutionPlan 能正确生成和打印。
- ActionSlotExecutionPlanEmpty：验证 null plan、null 输入、空队列、空 plan 能安全打印。
- ActionSlotExecutionPlanMissingSlot：验证已响应但缺少绑定槽位时，ExecutionPlan 能安全生成异常 item 并打印提示。
- ActionSlotExecutionPlanMultiBasic：验证多个已响应 + 多个未响应时，两轮扫描顺序稳定，order 递增正确。

当前稳定规则：

- CreateBasicExecutionPlan(...) 第一版只处理 RespondedEnemyIntent。
- CreateBasicExecutionPlan(...) 第一版只处理 UnrespondedEnemyIntent。
- 第一轮按 intentQueue 原顺序收集所有已响应敌人意图。
- 第二轮按 intentQueue 原顺序收集所有未响应敌人意图。
- order 从 1 开始递增。
- 当前暂不混入 FreeAction。
- 当前暂不处理旧槽位中间状态。
- 当前暂不做完整速度排序。
- 当前规则仍是第一版简化规则，不代表最终完整执行队列。

当前职责边界：

- BattleExecutionPlanManager 负责生成计划。
- BattleExecutionPlanManager 负责打印计划。
- BattleExecutionPlanManager 只读 actionSlots / intentQueue。
- BattleExecutionPlanManager 不执行计划。
- BattleExecutionPlanManager 不修改战斗状态。
- BattleExecutionPlanExecutor 第一版只负责打印执行步骤预览。
- 未来 BattleExecutionPlanExecutor 才负责逐项正式处理计划。
- 未来 BattleExecutionPlanExecutor 可能根据 BattleExecutionItemType 分派执行逻辑。
- 当前只完成预览代码，不实现正式执行代码。

下一阶段方向：

- 进入 BattleExecutionPlanExecutor 设计阶段。
- 先讨论执行器职责、类名、方法名、状态切换边界。
- 当前已新增 BattleExecutionPlanExecutor.cs 的第一版执行步骤预览。
- 当前已记录：暂不实现 ExecuteExecutionPlan(...)。
- 当前已采用 PrintExecutionPlanStepPreview(...) 作为第一版执行步骤预览方法。
- 后续如果进入正式执行器代码原型，应继续小步推进，不直接结算。

当前仍不实现：

- ExecuteExecutionPlan(...)。
- 正式执行器代码。
- 完整速度排序。
- FreeAction 混排。
- 旧槽位中间状态正式处理。
- 玩家槽位执行。
- 敌人意图执行。
- 拼点。
- 敌人攻击。
- 无人响应效果。
- 响应失败效果。
- Resolved。
- 负罪感增加。
- UseCount 增加。
- slot.MarkUsed()。
- item.isCompleted 切换。
- plan.isCompleted 切换。
- BattleEnemyIntent.isCompleted。
- respondingSlot。

当前结论：

- ExecutionPlan 生成 / 打印阶段当前可视为阶段性完成。
- 后续重点从“计划能否生成”转向“计划未来如何安全执行”。
- 下一阶段仍从设计文档开始，不直接写正式执行代码。

### 22. BattleExecutionPlanExecutor 第一版执行预览规则草案

当前阶段：

- BattleExecutionPlan 生成 / 打印阶段已经暂时收口。
- 下一阶段进入 BattleExecutionPlanExecutor 设计阶段。
- 当前已完成第一版执行步骤预览代码。
- 当前仍不进入正式结算。

第一版 Executor 的定位：

- 未来 BattleExecutionPlanExecutor 负责处理 BattleExecutionPlan.executionItems。
- 第一版不应直接执行战斗逻辑。
- 第一版应先作为执行步骤预览器设计。
- 第一版只遍历计划并打印每个 item 未来将如何处理。
- 第一版不改变任何战斗状态。

第一版建议方法名：

- 暂不使用 ExecuteExecutionPlan(...)。
- Execute 容易被误解为正式执行、结算、修改状态。
- 如果第一版只是打印执行步骤预览，推荐未来方法名：PrintExecutionPlanStepPreview(BattleExecutionPlan executionPlan)。
- 当前已按该命名实现第一版执行步骤预览方法。

第一版执行预览规则：

- 遍历 BattleExecutionPlan.executionItems。
- 按 item 的 order 顺序打印。
- 根据 BattleExecutionItemType 区分 RespondedEnemyIntent。
- 根据 BattleExecutionItemType 区分 UnrespondedEnemyIntent。
- 根据 BattleExecutionItemType 区分 FreeAction。
- 对 RespondedEnemyIntent：打印未来将进入玩家响应敌人意图处理。
- 对 RespondedEnemyIntent：暂不拼点。
- 对 RespondedEnemyIntent：暂不调用 BattleResolver。
- 对 RespondedEnemyIntent：暂不调用 slot.MarkUsed()。
- 对 UnrespondedEnemyIntent：打印未来将进入无人响应敌人意图处理。
- 对 UnrespondedEnemyIntent：暂不执行敌人攻击。
- 对 UnrespondedEnemyIntent：暂不处理无人响应效果。
- 对 FreeAction：当前暂未混入第一版 ExecutionPlan。
- 对 FreeAction：如果未来出现，只打印暂未实现正式处理。

第一版不修改状态：

- 不修改 item.isCompleted。
- 不修改 plan.isCompleted。
- 不修改 slot.isUsed。
- 不修改 enemyIntent.isResponded。
- 不修改 actualTarget。
- 不增加 UseCount。
- 不增加负罪感。
- 不触发 Resolved。

与 BattleExecutionPlanManager.PrintExecutionPlan(...) 的区别：

- PrintExecutionPlan(...) 打印的是计划内容。
- PrintExecutionPlan(...) 打印 order。
- PrintExecutionPlan(...) 打印 item 类型。
- PrintExecutionPlan(...) 打印关联敌人意图。
- PrintExecutionPlan(...) 打印关联槽位。
- PrintExecutionPlan(...) 打印当前实际目标。
- PrintExecutionPlanStepPreview(...) 未来打印的是执行阶段将如何处理。
- 已响应意图将进入响应处理。
- 未响应意图将进入无人响应处理。
- FreeAction 将进入普通行动处理。
- 当前已新增执行步骤预览代码，但仍只做预览，不进入正式执行。

关于 isCompleted：

- BattleExecutionItem.isCompleted 未来应在该执行项真正完成结算后才切换。
- BattleExecutionPlan.isCompleted 未来应在所有 item 完成后才切换。
- 第一版执行预览不切换这些状态。
- 当前不新增 MarkCompleted()。

当前仍不实现：

- ExecuteExecutionPlan(...)。
- 正式执行器代码。
- item.isCompleted 切换。
- plan.isCompleted 切换。
- BattleResolver 接入。
- ExecuteActionSlots(...) 接入。
- slot.MarkUsed()。
- 拼点。
- 敌人攻击。
- 无人响应效果。
- 响应失败效果。
- Resolved。
- 负罪感增加。
- UseCount 增加。
- BattleEnemyIntent.isCompleted。
- respondingSlot。
- FreeAction 混排。

当前结论：

- 下一阶段可以围绕 BattleExecutionPlanExecutor 继续设计。
- 第一版应先从执行步骤预览开始。
- 不应直接进入正式执行。
- 后续如果进入代码原型，应先创建只打印、不结算、不改状态的方法。

### 23. BattleExecutionItem / BattleExecutionPlan isCompleted 状态切换边界草案

当前阶段：

- BattleExecutionPlanExecutor.PrintExecutionPlanStepPreview(...) 第一版执行步骤预览已完成。
- 当前仍未进入正式执行。
- 当前下一步需要先设计 isCompleted 的切换边界。
- 当前只做设计记录，不实现代码。

当前字段现状：

- BattleExecutionItem 当前已有 public bool isCompleted。
- BattleExecutionPlan 当前已有 public bool isCompleted。
- 当前构造函数中默认都是 false。
- 当前没有任何代码会把它们改为 true。
- 当前预览和打印阶段不应修改这些字段。

BattleExecutionItem.isCompleted 未来语义：

- 表示单个执行项是否已经完成正式处理。
- 只有当该 item 对应的正式结算流程真正结束后，才应切换为 true。
- 不是已经打印过。
- 不是已经进入预览。
- 不是已经生成计划。

不同 item 类型的完成条件草案：

- RespondedEnemyIntent 未来应在玩家响应处理完成后才标记完成。
- RespondedEnemyIntent 例如拼点 / 响应效果 / 相关卡牌结算完成后。
- RespondedEnemyIntent 当前不定义具体拼点细节。
- UnrespondedEnemyIntent 未来应在无人响应敌人意图处理完成后才标记完成。
- UnrespondedEnemyIntent 例如敌人攻击 / 无人响应效果处理完后。
- UnrespondedEnemyIntent 当前不定义具体敌人攻击细节。
- FreeAction 未来应在普通行动 / 偷刀处理完成后才标记完成。
- FreeAction 当前尚未混入第一版 ExecutionPlan。

BattleExecutionPlan.isCompleted 未来语义：

- 表示整份执行计划是否全部完成。
- 只有当 executionItems 中所有需要执行的 item 都完成后，才应切换为 true。
- 如果 plan 为空，未来是否直接视为完成需要单独决定。
- 当前暂不实现空 plan 自动完成。

异常 / 跳过 item 的完成状态边界：

- 如果 item 为 null，未来执行器应跳过还是报错，需要单独设计。
- 当前不把 null item 视为完成逻辑。
- 如果 RespondedEnemyIntent 缺少 actionSlot，未来可能应进入异常处理 / 跳过处理。
- RespondedEnemyIntent 缺少 actionSlot 时，是否标记完成需要根据错误处理策略决定。
- 当前不决定。
- 如果 enemyIntent == null，未来应进入异常处理。
- 当前不决定是否完成。
- 如果某个 item 执行失败，可能需要 Failed / Skipped / Completed 等更细状态。
- 当前只有 bool，不足以表达全部情况。
- 当前先记录风险，不新增 enum。

是否需要比 bool 更细的状态：

- 当前 bool isCompleted 足够第一版记录未完成 / 已完成。
- 未来正式执行器可能需要更细状态，例如 Pending。
- 未来正式执行器可能需要更细状态，例如 Running。
- 未来正式执行器可能需要更细状态，例如 Completed。
- 未来正式执行器可能需要更细状态，例如 Skipped。
- 未来正式执行器可能需要更细状态，例如 Failed。
- 当前不新增 BattleExecutionItemState。
- 等真正执行器遇到失败 / 跳过需求时再考虑升级。

第一版代码实现时的建议边界：

- 第一版执行步骤预览不能切换 isCompleted。
- 第一版真正执行器如果只遍历打印，也不能切换 isCompleted。
- 只有真正接入某类 item 的正式结算后，才允许考虑切换对应 item 的 isCompleted。
- 只有所有 item 都完成后，才允许考虑切换 plan 的 isCompleted。

当前不实现：

- MarkCompleted()。
- MarkPlanCompleted()。
- BattleExecutionItemState。
- BattleExecutionPlanState。
- ExecuteExecutionPlan(...)。
- 正式执行器。
- item.isCompleted 切换。
- plan.isCompleted 切换。
- BattleResolver 接入。
- slot.MarkUsed()。
- 拼点。
- 敌人攻击。
- Resolved。
- UseCount 增加。
- 负罪感增加。
- BattleEnemyIntent.isCompleted。
- respondingSlot。

当前结论：

- isCompleted 当前只是未来正式执行阶段的预留字段。
- 当前生成、打印、预览阶段都不能修改它。
- 下一步如果进入代码原型，应继续保持不切换 isCompleted。
- 等真正处理某类 item 的正式结算时，再设计完成标记逻辑。

### 24. BattleExecutionPlanExecutor 未来分派结构草案

当前阶段：

- BattleExecutionPlanExecutor.PrintExecutionPlanStepPreview(...) 已完成。
- isCompleted 状态切换边界已记录。
- 当前仍不实现 ExecuteExecutionPlan(...)。
- 当前只设计未来执行器的分派结构。

未来执行器入口草案：

- 未来可能存在 ExecuteExecutionPlan(BattleExecutionPlan executionPlan)。
- 当前暂不实现。
- 当前只记录它未来可能作为正式执行入口。
- 未来实现时必须确认是否真的进入正式结算。

未来分派结构草案：

- 未来执行器可能遍历 executionPlan.executionItems。
- 未来执行器可能根据 BattleExecutionItemType 分派 RespondedEnemyIntent。
- 未来执行器可能根据 BattleExecutionItemType 分派 UnrespondedEnemyIntent。
- 未来执行器可能根据 BattleExecutionItemType 分派 FreeAction。
- 每种类型应进入不同处理分支。
- 当前只记录分派方向，不写代码。

RespondedEnemyIntent 未来处理方向：

- RespondedEnemyIntent 表示玩家槽位已经成功响应某个敌人意图。
- 未来应进入玩家响应敌人意图处理。
- 可能涉及拼点。
- 可能涉及响应卡牌效果。
- 可能涉及敌人意图被处理后的结果。
- 可能涉及卡牌 UseCount / CD / 消耗。
- 可能涉及 slot.MarkUsed()。
- 可能涉及 item.isCompleted。
- 当前不实现这些逻辑。

UnrespondedEnemyIntent 未来处理方向：

- UnrespondedEnemyIntent 表示敌人意图无人响应。
- 未来应进入无人响应敌人意图处理。
- 可能涉及敌人攻击。
- 可能涉及无人响应额外效果。
- 可能涉及对 actualTarget 的处理。
- 可能涉及敌人卡牌效果。
- 可能涉及 item.isCompleted。
- 当前不实现这些逻辑。

FreeAction 未来处理方向：

- FreeAction 表示普通行动 / 偷刀。
- 当前第一版 CreateBasicExecutionPlan(...) 尚未生成 FreeAction。
- 未来如果加入，需要处理普通攻击目标。
- 未来如果加入，需要处理技能目标。
- 未来如果加入，需要处理偷刀行动与敌人意图的时序关系。
- 未来如果加入，需要处理与速度排序的关系。
- 未来如果加入，可能涉及 slot.MarkUsed()。
- 未来如果加入，可能涉及 item.isCompleted。
- 当前不实现这些逻辑。

异常 item 未来处理方向：

- item == null 时，未来执行器应跳过、报错还是标记失败，需要再定。
- enemyIntent == null 时，未来应进入异常处理。
- RespondedEnemyIntent 但 actionSlot == null 时，未来应进入异常处理或跳过处理。
- 当前只记录风险，不决定最终策略。
- 当前不新增 Failed / Skipped 状态。

与当前 PrintExecutionPlanStepPreview(...) 的关系：

- 当前 PrintExecutionPlanStepPreview(...) 只是打印未来处理方向。
- PrintExecutionPlanStepPreview(...) 不是真正分派执行器。
- PrintExecutionPlanStepPreview(...) 可以作为未来分派结构的文字预览参考。
- PrintExecutionPlanStepPreview(...) 不能直接等同于正式执行逻辑。

当前不实现：

- ExecuteExecutionPlan(...)。
- ExecuteRespondedEnemyIntent(...)。
- ExecuteUnrespondedEnemyIntent(...)。
- ExecuteFreeAction(...)。
- 正式执行器代码。
- BattleResolver 接入。
- ExecuteActionSlots(...) 接入。
- slot.MarkUsed()。
- 拼点。
- 敌人攻击。
- 无人响应效果。
- 响应失败效果。
- Resolved。
- UseCount 增加。
- 负罪感增加。
- item.isCompleted 切换。
- plan.isCompleted 切换。
- BattleEnemyIntent.isCompleted。
- respondingSlot。
- FreeAction 混排。

当前结论：

- 未来 BattleExecutionPlanExecutor 应采用按 BattleExecutionItemType 分派的结构。
- 当前只记录分派方向。
- 下一步如果继续推进，应先决定第一版真正执行器是否只处理某一种 item 类型，而不是一次性实现全部类型。
- 当前仍不进入正式执行代码。

### 25. UnrespondedEnemyIntent 第一版执行边界草案

当前阶段判断：

- BattleExecutionPlanExecutor.PrintExecutionPlanStepPreview(...) 已完成。
- 当前仍没有正式执行器。
- 当前仍没有 ExecuteExecutionPlan(...)。
- 当前仍未接入拼点、敌人攻击、Resolved、UseCount、负罪感、slot.MarkUsed()。
- 下一步如果进入正式执行器设计，建议优先从 UnrespondedEnemyIntent 开始，而不是 RespondedEnemyIntent 或 FreeAction。

三类 item 复杂度判断：

- RespondedEnemyIntent 会牵扯玩家槽位、敌人意图、拼点、响应成功 / 失败、卡牌 UseCount / CD、Resolved、负罪感、slot.MarkUsed()、item.isCompleted 等正式结算问题。
- RespondedEnemyIntent 当前不建议作为第一版执行入口。
- FreeAction 会牵扯普通攻击 / 偷刀目标、Ability 罪卡、速度混排、技能目标、卡牌消耗、槽位使用标记等问题。
- FreeAction 当前不建议作为第一版执行入口。
- UnrespondedEnemyIntent 相对更适合作为第一版执行器设计入口。
- UnrespondedEnemyIntent 没有玩家响应槽位，没有拼点，不涉及玩家卡牌消耗。
- UnrespondedEnemyIntent 主要表达“敌人意图无人响应，未来按当前 actualTarget 处理”。

UnrespondedEnemyIntent 第一版边界：

- 第一版仍不应直接造成伤害。
- 第一版应先识别 UnrespondedEnemyIntent item。
- 第一版应验证 enemyIntent / enemy / enemyCardState / actualTarget。
- 第一版应打印将进入无人响应敌人意图处理。
- 第一版暂不执行敌人攻击。
- 第一版暂不处理无人响应效果。
- 第一版暂不调用 BattleResolver。
- 第一版暂不切换 item.isCompleted。
- 第一版暂不切换 plan.isCompleted。

进入代码实现前需要先定的设计：

- 敌人攻击第一版走哪个入口：新逻辑，还是复用 BattleResolver，当前暂不决定。
- actualTargetCharacter / actualTargetSlotIndex 的正式含义：伤害打角色，是否也影响槽位，当前暂不决定。
- 空槽位被未响应敌人意图命中时的默认处理。
- 无人响应效果是否由敌人卡自身定义。
- 敌人卡是否有 OnPlay / Resolved 类似事件。
- item.isCompleted = true 的准确时机。
- 空 / 异常 item 是跳过、报错，还是未来引入 Failed / Skipped 状态。
- 无人响应时是否完全不调用 slot.MarkUsed()，当前倾向是不调用，因为没有玩家槽位被执行。

与当前 PrintExecutionPlanStepPreview(...) 的关系：

- 当前 PrintExecutionPlanStepPreview(...) 已能打印 UnrespondedEnemyIntent 未来将处理无人响应敌人意图。
- PrintExecutionPlanStepPreview(...) 仍只是预览，不是正式分派执行。
- 后续如果写代码，可以先做“不改状态的 Unresponded 分支预览”。
- 暂不直接伤害。
- 暂不完成标记。

当前不建议做：

- 不处理 RespondedEnemyIntent 拼点。
- 不混入 FreeAction。
- 不直接造成伤害。
- 不执行敌人攻击。
- 不切换 item.isCompleted。
- 不切换 plan.isCompleted。
- 不接入 BattleResolver。
- 不新增 BattleEnemyIntent.isCompleted。
- 不新增 respondingSlot。
- 不调用 slot.MarkUsed()。
- 不触发 Resolved。
- 不增加 UseCount。
- 不增加负罪感。

当前结论：

- 第一版正式执行器设计优先从 UnrespondedEnemyIntent 开始是合理的。
- 但第一步仍应停留在设计和分支预览层。
- 不应直接进入伤害和正式结算。
- 下一步如果继续推进，应先设计无人响应敌人意图的最小处理流程，而不是一次性实现完整执行器。

### 26. UnrespondedEnemyIntent actualTarget 第一版含义草案

当前阶段：

- 已记录第一版正式执行器设计优先从 UnrespondedEnemyIntent 开始。
- 当前仍不进入代码实现。
- 当前仍不造成伤害。
- 当前仍不执行敌人攻击。
- 当前仍不切换 item.isCompleted / plan.isCompleted。

actualTarget 第一版含义：

- 敌人意图不是单纯攻击角色。
- 敌人意图应理解为攻击某个角色的某个槽位。
- actualTargetCharacter 表示当前实际被敌人意图指向的角色。
- actualTargetSlotIndex 表示当前实际被敌人意图指向的槽位编号。
- 两者合起来表示：敌人意图最终命中的目标槽位。

无人响应时的理解：

- 如果 UnrespondedEnemyIntent 没有被玩家槽位响应。
- 那么未来敌人意图应按当前 actualTargetCharacter / actualTargetSlotIndex 处理。
- 例如：敌人意图1 当前实际目标是 我方角色B 槽位2。
- 则未来无人响应处理应理解为：敌人意图将命中 我方角色B 槽位2。

第一版执行边界：

- 第一版不要直接扣血。
- 第一版不要直接执行敌人攻击。
- 第一版不要触发无人响应效果。
- 第一版可以先打印敌人意图将命中哪个角色。
- 第一版可以先打印敌人意图将命中哪个槽位。
- 第一版可以先打印敌人卡牌是什么。
- 这仍然是执行前的边界确认，不是正式结算。

空槽位含义：

- 如果 actualTargetSlotIndex 指向的槽位没有玩家响应。
- 不代表槽位自带防御或闪避。
- 空槽位只是表示玩家没有处理该敌人意图。
- 未来是否触发额外效果，应由敌人卡自身定义。
- 不应由空槽位本身自动产生惩罚规则。

与响应改写的关系：

- 如果玩家速度足够并成功响应，actualTargetCharacter / actualTargetSlotIndex 可被改写为响应者槽位。
- 如果无人响应，则保持敌人原本目标。
- 如果响应后又解除绑定，但 actualTarget 仍保留，未来需要异常策略单独处理。
- 当前不在这里实现异常策略。

当前不实现：

- 敌人攻击结算。
- 伤害扣除。
- 无人响应效果。
- 敌人卡牌效果。
- 空槽位惩罚。
- BattleResolver 接入。
- slot.MarkUsed()。
- item.isCompleted 切换。
- plan.isCompleted 切换。
- BattleEnemyIntent.isCompleted。
- respondingSlot。

当前结论：

- UnrespondedEnemyIntent 第一版应先明确命中目标。
- 命中目标由 actualTargetCharacter + actualTargetSlotIndex 表示。
- 下一步如果继续推进，可以先做“无人响应命中目标预览”，仍然不造成伤害、不进入正式结算。

### 27. UnrespondedEnemyIntent 最小处理流程草案

当前阶段：

- UnrespondedEnemyIntent 的命中目标含义已经明确。
- actualTargetCharacter + actualTargetSlotIndex 表示敌人意图最终命中的目标槽位。
- PrintExecutionPlanStepPreview(...) 已能打印无人响应意图的命中目标预览。
- 当前仍不进入正式敌人攻击和伤害结算。

未来最小处理流程草案：

- 读取 BattleExecutionItem。
- 确认 item.executionType == UnrespondedEnemyIntent。
- 读取 item.enemyIntent。
- 确认 enemyIntent 不为空。
- 确认 enemyIntent.enemy 不为空。
- 确认 enemyIntent.enemyCardState 不为空。
- 确认 enemyIntent.actualTargetCharacter 不为空。
- 读取 enemyIntent.actualTargetSlotIndex。
- 打印敌人意图编号。
- 打印敌人卡牌。
- 打印将命中的角色。
- 打印将命中的槽位。
- 然后未来才进入敌人攻击 / 无人响应效果处理。

第一版仍不做：

- 不扣血。
- 不执行敌人攻击。
- 不触发无人响应效果。
- 不执行敌人卡牌效果。
- 不调用 BattleResolver。
- 不调用 slot.MarkUsed()。
- 不切换 item.isCompleted。
- 不切换 plan.isCompleted。

数据异常处理草案：

- 如果 item == null，未来应安全跳过或进入异常日志。
- item == null 时，当前不决定是否标记失败。
- 如果 enemyIntent == null，未来应打印异常，不能继续处理。
- 如果 enemy == null，未来应打印异常，不能继续处理。
- 如果 enemyCardState == null，未来应打印异常，不能继续处理。
- 如果 actualTargetCharacter == null，未来应打印异常，不能继续处理。
- 如果 actualTargetSlotIndex <= 0，未来应打印槽位无效，不能直接进入正式命中处理。
- 当前只记录异常方向，不新增 Failed / Skipped 状态。

空槽位处理边界：

- actualTargetSlotIndex 指向的槽位如果没有玩家响应，不代表槽位自带防御。
- 空槽位不自动闪避。
- 空槽位不自动减伤。
- 是否触发额外效果，应由敌人卡牌定义。
- 当前不实现空槽位惩罚。

敌人攻击入口暂不决定：

- 未来敌人攻击可能需要新逻辑。
- 未来也可能复用或扩展 BattleResolver。
- 当前不决定具体入口。
- 在入口明确前，不应直接写伤害结算。

item.isCompleted 边界：

- 只有当无人响应敌人意图的正式处理完整结束后，才考虑标记 item.isCompleted = true。
- 单纯打印命中目标不算完成。
- 单纯进入预览不算完成。
- 当前不切换完成状态。

当前不实现：

- HandleUnrespondedEnemyIntent(...)。
- ExecuteUnrespondedEnemyIntent(...)。
- 敌人攻击结算。
- 伤害扣除。
- 无人响应效果。
- 敌人卡牌效果。
- BattleResolver 接入。
- slot.MarkUsed()。
- item.isCompleted 切换。
- plan.isCompleted 切换。
- BattleEnemyIntent.isCompleted。
- respondingSlot。
- Failed / Skipped 状态。

当前结论：

- UnrespondedEnemyIntent 的第一版最小处理流程应先以“校验数据 + 明确命中目标 + 打印处理方向”为核心。
- 在敌人攻击入口、无人响应效果、完成状态边界进一步明确前，不应直接进入伤害结算。
- 下一步如果继续推进，可以先设计“无人响应敌人意图处理入口命名”，但仍不写正式执行代码。

### 28. UnrespondedEnemyIntent 处理入口命名草案

当前阶段：

- UnrespondedEnemyIntent 的 actualTarget 含义已明确。
- UnrespondedEnemyIntent 最小处理流程已记录。
- 当前仍不进入正式敌人攻击和伤害结算。
- 当前只设计未来处理入口命名。

当前不建议使用的命名：

- 暂不建议使用 ExecuteUnrespondedEnemyIntent(...)。
- Execute 容易被理解为已经正式执行敌人攻击、造成伤害、修改状态。
- 当前阶段还没有接入敌人攻击、无人响应效果、BattleResolver、item.isCompleted 等正式结算逻辑。

第一版更安全的命名候选：

- PreviewHandleUnrespondedEnemyIntent(...)：预览如何处理无人响应敌人意图，适合第一版只校验和打印、不结算。
- PrintUnrespondedEnemyIntentHandlingPreview(...)：打印无人响应敌人意图处理预览，语义更保守，更明确不执行。
- PreviewUnrespondedEnemyIntentHit(...)：预览无人响应敌人意图将命中的目标，更偏向命中目标预览，不一定覆盖完整处理流程。

当前倾向：

- 如果未来只是打印命中目标和处理方向，优先考虑 PrintUnrespondedEnemyIntentHandlingPreview(...)。
- 如果未来要作为正式处理函数的前置预览版本，优先考虑 PreviewHandleUnrespondedEnemyIntent(...)。
- 当前暂不决定最终函数名。
- 当前不实现任何函数。

未来第一版入口职责边界：

- 接收或读取 BattleExecutionItem。
- 确认 item 类型为 UnrespondedEnemyIntent。
- 校验 enemyIntent / enemy / enemyCardState / actualTargetCharacter / actualTargetSlotIndex。
- 打印敌人意图编号。
- 打印敌人卡牌。
- 打印将命中的角色。
- 打印将命中的槽位。
- 打印“当前仅预览，不造成伤害”。
- 不造成伤害。
- 不执行敌人攻击。
- 不触发无人响应效果。
- 不切换完成状态。

与现有 PrintExecutionPlanStepPreview(...) 的关系：

- 现有方法已经能打印 UnrespondedEnemyIntent 的命中目标预览。
- 未来如果无人响应逻辑变复杂，可以从现有 PrintUnrespondedEnemyIntentStepPreview(...) 中拆出更专门的方法。
- 当前不急着拆。
- 等确实需要独立入口时再拆更自然。

当前不实现：

- ExecuteUnrespondedEnemyIntent(...)。
- HandleUnrespondedEnemyIntent(...)。
- PreviewHandleUnrespondedEnemyIntent(...)。
- PrintUnrespondedEnemyIntentHandlingPreview(...)。
- 敌人攻击结算。
- 伤害扣除。
- 无人响应效果。
- 敌人卡牌效果。
- BattleResolver 接入。
- slot.MarkUsed()。
- item.isCompleted 切换。
- plan.isCompleted 切换。
- BattleEnemyIntent.isCompleted。
- respondingSlot。

当前结论：

- 第一版无人响应处理入口应避免使用 Execute。
- 当前仍保持预览 / 打印 / 校验语义。
- 等敌人攻击入口、无人响应效果和完成状态边界更明确后，再决定是否升级为正式 Handle 或 Execute 命名。

### 29. UnrespondedEnemyIntent 敌人攻击入口设计草案

当前阶段判断：

- 当前结构适合继续做设计判断。
- 当前还不适合直接写敌人攻击结算代码。
- 当前仍不直接造成伤害。
- 当前仍不执行敌人攻击。
- 当前仍不接入 BattleResolver 正式结算。
- 当前仍不切换 item.isCompleted / plan.isCompleted。

当前职责边界评价：

- BattleExecutionPlanManager 负责生成 / 打印计划，不执行。
- BattleExecutionPlanExecutor 当前只做执行步骤预览。
- BattleExecutionPlanExecutor 未来负责遍历 BattleExecutionPlan.executionItems。
- BattleExecutionPlanExecutor 未来根据 BattleExecutionItemType 分派。
- BattleExecutionPlanExecutor 不应直接处理伤害。
- BattleEnemyIntent 保存敌人意图数据，不执行。
- BattleActionSlotManager 负责槽位创建、安排响应、响应覆盖、预览，不负责正式结算。
- BattleResolver 当前是正式战斗结算器。
- BattleResolver 负责拼点、伤害、事件、卡牌效果等底层结算能力。

关于是否复用 / 扩展 BattleResolver：

- 未来可以复用 BattleResolver 的底层结算能力。
- 当前不建议直接扩展 BattleResolver。
- 不建议把 UnrespondedEnemyIntent 队列处理、敌人意图分派、ExecutionPlan item 分派都塞进 BattleResolver。
- 否则 BattleResolver 容易从结算器膨胀成队列执行器 + 意图处理器 + 结算器。
- 当前无人响应规则还没定清楚，不应过早固化。

未来是否需要敌人意图专用处理器：

- 长期可以考虑新增专门结构。
- 命名倾向：BattleEnemyIntentResolver。
- BattleEnemyIntentResolver 比 BattleEnemyIntentExecutor 更合适，因为 Executor 容易和 BattleExecutionPlanExecutor 混淆。
- 不建议使用过窄的 BattleUnrespondedIntentResolver，避免为单一分支过早建类。
- 当前仍只记录设计，不新增类。

未来三层分工草案：

- BattleExecutionPlanExecutor 负责遍历计划。
- BattleExecutionPlanExecutor 根据 item 类型分派。
- BattleExecutionPlanExecutor 协调整体执行流程。
- BattleExecutionPlanExecutor 未来在正式结算完成后协调 item.isCompleted / plan.isCompleted。
- BattleEnemyIntentResolver 未来负责处理敌人意图相关分支。
- BattleEnemyIntentResolver 未来包括 RespondedEnemyIntent 和 UnrespondedEnemyIntent 的敌人意图层处理。
- BattleEnemyIntentResolver 不负责遍历整个 ExecutionPlan。
- BattleResolver 负责底层战斗结算能力。
- BattleResolver 包括拼点、伤害、Hit / AfterDamage / Resolved、卡牌效果等。

UnrespondedEnemyIntent 未来正式处理步骤草案：

- 校验 BattleExecutionItem。
- 确认 executionType == UnrespondedEnemyIntent。
- 读取 enemyIntent。
- 校验 enemyIntent.enemy。
- 校验 enemyIntent.enemyCardState。
- 校验 enemyIntent.actualTargetCharacter。
- 校验 actualTargetSlotIndex 是否有效。
- 打印 / 确认命中目标。
- 未来进入敌人无人响应攻击处理。
- 未来处理无人响应效果。
- 未来处理敌人卡牌效果。
- 正式处理结束后，才考虑 item.isCompleted = true。

当前仍缺的关键规则：

- 敌人攻击是否复用 BattleResolver。
- 敌人无人响应攻击是否触发 Resolved。
- 敌人卡牌是否处理 CD / UseCount。
- 敌人卡牌 effects 的触发时机。
- 命中槽位是否只用于目标定位，还是会影响槽位状态。
- item.isCompleted 的切换时机。
- 异常 item 是跳过、失败，还是停止计划。

当前不建议做：

- 不直接写 ExecuteUnrespondedEnemyIntent(...)。
- 不直接造成伤害。
- 不执行敌人攻击。
- 不调用 BattleResolver 造成正式结算。
- 不调用 slot.MarkUsed()。
- 不切换 item.isCompleted。
- 不切换 plan.isCompleted。
- 不新增 BattleEnemyIntent.isCompleted。
- 不处理 RespondedEnemyIntent 拼点。
- 不混入 FreeAction。
- 不新增过重的 Manager / Resolver，除非确实有必要。

当前结论：

- 未来敌人攻击入口不应直接塞进 BattleExecutionPlanExecutor。
- BattleExecutionPlanExecutor 应负责遍历和分派。
- 敌人意图层处理未来可考虑 BattleEnemyIntentResolver。
- 底层伤害 / 事件 / 卡牌效果仍应由 BattleResolver 或其扩展能力承担。
- 当前阶段只记录设计，不写代码。

### 30. 敌人卡牌 UseCount / CD 第一版处理边界草案

当前阶段：

- UnrespondedEnemyIntent 敌人攻击入口仍在设计阶段。
- 当前仍不执行敌人攻击。
- 当前仍不造成伤害。
- 当前仍不接入 BattleResolver 正式结算。
- 当前只先明确敌人卡牌 UseCount / CD 边界。

当前设计倾向：

- 敌人卡牌第一版暂不走玩家式 UseCount / CD 轮转。
- 敌人出牌更接近 AI / 关卡逻辑 / 敌人意图生成。
- 敌人意图队列决定敌人本回合使用哪些卡。
- 不把敌人卡牌当作玩家手牌那样进行冷却、使用次数、卡组轮转管理。

与玩家卡牌的区别：

- 玩家卡牌会进入行动槽位。
- 玩家卡牌会受 UseCount / CD / 是否消耗等规则限制。
- 玩家卡牌使用后未来可能调用 BattleCardManager.ApplyCooldownOnResolved(...) 或相关逻辑。
- 玩家卡牌属于轮转资源。
- 敌人卡牌第一版由敌人意图直接引用。
- 敌人卡牌由 AI / 关卡设计决定是否出现。
- 敌人卡牌暂不消耗 UseCount。
- 敌人卡牌暂不进入 CD。
- 敌人卡牌暂不参与玩家式卡组轮转。

对 UnrespondedEnemyIntent 的影响：

- 未来无人响应敌人意图正式处理时。
- 即使敌人卡牌完成了攻击或效果。
- 第一版也不增加敌人卡牌 UseCount。
- 第一版也不让敌人卡牌进入 CD。
- 第一版不调用玩家卡牌式的 ApplyCooldownOnResolved(...)。

Resolved 边界：

- 敌人卡牌是否触发 Resolved 事件，需要单独设计。
- 即使未来敌人卡牌有类似 Resolved 的事件，也不等于一定要走玩家卡牌 UseCount / CD。
- 当前不决定敌人卡牌事件系统。
- 当前只记录：敌人卡牌 UseCount / CD 第一版不处理。

未来可能扩展：

- 后续如果需要精英怪 / Boss 卡牌轮转，可以另行设计敌人专用冷却或行动池。
- 例如敌人技能 CD。
- 例如敌人行为权重。
- 例如敌人意图池。
- 例如关卡脚本控制出牌。
- 这些不应直接复用玩家卡牌 UseCount / CD 规则，除非后续明确需要。

当前不实现：

- 敌人卡牌 UseCount 增加。
- 敌人卡牌 CD。
- 敌人卡组轮转。
- 敌人手牌。
- 敌人抽牌。
- BattleCardManager.ApplyCooldownOnResolved(...) 作用于敌人卡牌。
- 敌人卡牌 Resolved 事件。
- 敌人攻击正式结算。
- 无人响应效果。
- 伤害扣除。
- item.isCompleted 切换。
- plan.isCompleted 切换。

当前结论：

- 第一版敌人卡牌不处理玩家式 UseCount / CD。
- 敌人卡牌是否出现由敌人意图 / AI / 关卡逻辑决定。
- 这能避免把敌人行为过早塞进玩家卡牌轮转系统。
- 后续真正设计敌人攻击结算时，应继续遵守这个边界。

### 31. 敌人无人响应攻击事件触发边界草案

当前阶段：

- UnrespondedEnemyIntent 敌人攻击入口仍在设计阶段。
- 当前仍不执行敌人攻击。
- 当前仍不造成伤害。
- 当前仍不接入 BattleResolver 正式结算。
- 当前已经记录敌人卡牌第一版暂不处理玩家式 UseCount / CD。
- 当前继续明确敌人无人响应攻击是否触发 Resolved / Hit / AfterDamage 等事件。

当前设计倾向：

- 第一版敌人无人响应攻击暂不直接套用玩家卡牌式事件链。
- 第一版暂不默认触发 Resolved。
- 第一版暂不默认触发 Hit。
- 第一版暂不默认触发 AfterDamage。
- 第一版暂不默认触发 AfterKill。
- 原因是这些事件目前更接近玩家卡牌 / BattleResolver 正式结算流程。
- 敌人卡牌事件系统尚未单独设计，不应过早复用玩家卡牌事件。

与玩家卡牌事件的区别：

- 玩家卡牌未来可能在拼点、命中、造成伤害、结算完成时触发事件。
- 玩家卡牌可能与 UseCount / CD / 消耗 / 罪卡规则绑定。
- 敌人卡牌第一版由敌人意图引用。
- 敌人卡牌暂不走玩家式 UseCount / CD。
- 因此敌人卡牌事件触发点不应直接照搬玩家卡牌事件链。

对 UnrespondedEnemyIntent 第一版的影响：

- 未来即使进入无人响应敌人攻击处理。
- 第一版也可以先只处理命中目标确认。
- 第一版也可以先只处理敌人攻击基础处理。
- 第一版也可以先只处理是否造成伤害。
- 第一版暂不触发敌人卡牌 Resolved / Hit / AfterDamage。
- 第一版暂不执行敌人卡牌效果事件。
- 第一版暂不调用玩家卡牌式 CardEffectExecutor 事件链。

未来可能扩展：

- 后续如果敌人卡牌也需要事件系统，可以单独设计敌人卡牌事件触发点。
- 例如 EnemyIntentStart。
- 例如 EnemyIntentHit。
- 例如 EnemyIntentAfterDamage。
- 例如 EnemyIntentResolved。
- 是否复用现有 Resolved / Hit / AfterDamage 命名，需要后续判断。
- 不应在当前阶段直接混用。

与 BattleResolver 的关系：

- BattleResolver 未来仍可能提供底层伤害 / 命中 / 事件能力。
- 当前不直接调用 BattleResolver 来触发事件。
- 等敌人攻击入口和敌人卡牌事件系统明确后，再决定是否复用或扩展 BattleResolver。

当前不实现：

- 敌人攻击正式结算。
- 伤害扣除。
- 敌人卡牌 Resolved。
- 敌人卡牌 Hit。
- 敌人卡牌 AfterDamage。
- 敌人卡牌 AfterKill。
- 敌人卡牌效果执行。
- 敌人卡牌事件系统。
- CardEffectExecutor 作用于敌人卡牌。
- BattleResolver 接入。
- item.isCompleted 切换。
- plan.isCompleted 切换。
- UseCount 增加。
- CD 处理。
- 负罪感增加。
- slot.MarkUsed()。

当前结论：

- 第一版敌人无人响应攻击暂不触发玩家卡牌式事件链。
- 敌人卡牌事件系统后续单独设计。
- 当前先保持敌人意图处理与玩家卡牌结算事件解耦。
- 后续真正进入敌人攻击实现前，需要再决定敌人攻击是否需要自己的事件触发点。

### 32. UnrespondedEnemyIntent 伤害结算前置边界草案

当前阶段判断：

- 当前适合开始伤害结算设计。
- 当前不适合开始伤害结算实现。
- 当前仍不直接扣血。
- 当前仍不执行敌人攻击。
- 当前仍不切换 item.isCompleted / plan.isCompleted。
- 当前继续停留在伤害前预览。

当前已有伤害路径：

- 当前正式伤害路径主要在 BattleResolver。
- BattleResolver 负责或涉及卡牌点数伤害计算。
- BattleResolver 负责或涉及 BattleCalculator.GetFinalDamageScaled(...)。
- BattleResolver 负责或涉及转成 HP 伤害。
- BattleResolver 负责或涉及 CharacterData.TakeDamage(...)。
- BattleResolver 负责或涉及 Hit / AfterDamage / AfterKill / Resolved 等事件。
- BattleResolver 负责或涉及卡牌效果执行。
- CharacterData.TakeDamage(...) 本身更接近直接扣 HP 和打印日志。
- 如果 UnrespondedEnemyIntent 现在直接调用 TakeDamage(...)，会绕过 BattleResolver 的伤害公式、Buff 影响、事件链和击杀判断。

当前为什么不直接扣血：

- 会绕过已有 BattleResolver 伤害 / 事件 / 效果流程。
- 会和已有拼点胜负伤害逻辑形成重复路径。
- 会让 BattleExecutionPlanExecutor 从分派器变成伤害结算器。
- 会过早引入 item.isCompleted / plan.isCompleted 切换问题。
- 会绕开未来敌人卡牌效果、无人响应效果、死亡处理的设计。
- 后续如果要统一伤害规则，会很难回收。

进入伤害结算前必须明确的规则：

- 敌人卡牌伤害值从哪里来。
- 当前 CardTestData / BattleCardState 的 minPoint / maxPoint / damageFormula 是否用于无人响应攻击。
- 无人响应时是否仍然 roll 一个敌人攻击点数。
- 伤害目标是直接作用于 actualTargetCharacter 的 HP，还是先作用于槽位。
- actualTargetSlotIndex 是否只用于目标定位 / 槽位效果触发。
- 是否考虑防御、减伤、护盾、槽位 Buff。
- 死亡、AfterDamage、AfterKill 是否暂不处理。
- 是否需要统一伤害方法。
- 是否复用或扩展 BattleResolver 的伤害能力。

当前实现层建议：

- 继续停留在伤害前预览。
- 只打印敌人意图将命中哪个角色。
- 只打印敌人意图将命中哪个槽位。
- 只打印敌人卡牌是什么。
- 只打印未来这里会进入敌人伤害处理。
- 只打印当前不扣血。
- 不调用 TakeDamage(...)。
- 不调用 BattleResolver 造成正式结算。

职责分工建议：

- BattleExecutionPlanExecutor 负责遍历执行计划。
- BattleExecutionPlanExecutor 根据 item 类型分派。
- BattleExecutionPlanExecutor 不直接算伤害。
- BattleEnemyIntentResolver 未来处理敌人意图层逻辑。
- BattleEnemyIntentResolver 未来校验 enemyIntent。
- BattleEnemyIntentResolver 未来判断无人响应 / 已响应。
- BattleEnemyIntentResolver 未来决定是否进入敌人攻击处理。
- BattleResolver 或其扩展方法负责底层伤害、命中、事件、卡牌效果。
- BattleResolver 未来可提供“敌人单方面攻击目标”的结算入口。
- 当前不急着接入 BattleResolver。

对 UnrespondedEnemyIntent 的当前边界：

- 当前可以确认命中目标。
- 当前可以打印伤害前预览。
- 当前不扣 HP。
- 当前不处理死亡。
- 当前不触发 Resolved / Hit / AfterDamage / AfterKill。
- 当前不处理敌人卡牌 UseCount / CD。
- 当前不处理敌人卡牌效果。
- 当前不切换完成状态。

当前不实现：

- 敌人基础伤害结算。
- TakeDamage(...) 调用。
- BattleResolver 接入。
- 敌人单方面攻击结算入口。
- 无人响应效果。
- 敌人卡牌效果。
- 死亡处理。
- AfterDamage / AfterKill / Resolved / Hit。
- 敌人 UseCount / CD。
- slot.MarkUsed()。
- item.isCompleted 切换。
- plan.isCompleted 切换。
- BattleEnemyIntent.isCompleted。
- respondingSlot。
- RespondedEnemyIntent 拼点。
- FreeAction 混排。

当前结论：

- 现在可以开始伤害结算的设计。
- 实现层继续停在伤害前预览。
- 在敌人伤害值来源、是否 roll 点数、是否复用 BattleResolver、事件触发和完成状态边界明确前，不应直接扣血。
- 下一步可以继续设计“无人响应攻击是否 roll 敌人卡点数”。

### 33. UnrespondedEnemyIntent 无人响应攻击点数规则草案

当前阶段判断：

- 当前适合继续做无人响应攻击点数 / 伤害设计。
- 当前仍不进入代码实现。
- 当前不实际 roll 点数。
- 当前不扣血。
- 当前不接入 BattleResolver 正式结算。
- 当前不切换 item.isCompleted / plan.isCompleted。

当前设计倾向：

- 未来无人响应攻击仍然应该 roll 敌人卡点数。
- 敌人卡牌已有 minPoint / maxPoint。
- 如果无人响应攻击完全不用点数，敌人卡牌的点数范围会失去核心意义。
- 点数可以表示敌人单方面攻击强度。
- 这个点数未来可以作为伤害公式输入。

命名边界：

- 无人响应没有双方拼点。
- 所以设计上不建议继续称为 clashPoint。
- 更建议称为 enemyAttackPoint。
- 或称为 attackPoint。
- 即使现有 BattleCalculator.GetFinalDamageScaled(...) 参数名可能仍叫 clashPoint，设计语义上也应区分“拼点点数”和“单方面攻击点数”。

当前数据基础：

- CardTestData / 卡牌数据中已有 minPoint。
- CardTestData / 卡牌数据中已有 maxPoint。
- CardTestData / 卡牌数据中已有 damageFormula。
- BattleCardState 持有 cardData。
- BattleEnemyIntent 持有 enemyCardState。
- 这些数据足够支持未来无人响应攻击点数设计。
- 但当前不实现 roll。

未来点数流程草案：

- BattleExecutionPlanExecutor 只负责分派。
- 未来 BattleEnemyIntentResolver 或敌人意图处理器负责校验 enemyIntent / enemy / enemyCardState / actualTarget。
- 正式处理时 roll 敌人攻击点数。
- 该点数作为伤害公式输入。
- 后续交给 BattleCalculator 或 BattleResolver 的扩展入口。
- 由统一伤害路径处理倍率、Buff、HP 伤害转换、死亡、事件等。
- 完整处理结束后，才考虑 item.isCompleted = true。

当前预览层建议：

- 当前可以先只做点数范围预览。
- 例如打印敌人卡点数范围。
- 例如打印未来无人响应时会 roll 敌人攻击点数。
- 例如打印当前仅预览点数范围。
- 例如打印当前不实际 roll。
- 例如打印当前不扣血。
- 当前不应直接生成随机点数。
- 当前不应进行伤害计算。

进入实现前仍需明确的规则：

- 无人响应时 roll 的点数是否受 Buff / Debuff 影响。
- Strength / Weakness / NextClashPointUp 等拼点相关 Buff 是否适用于单方面攻击。
- 是否需要区分“拼点 Buff”和“攻击点数 Buff”。
- BattleCalculator.GetFinalClashPoint(...) 是否能复用，还是需要新增更中性的攻击点数计算方法。
- 实际 roll 是否只在正式执行时发生。
- 预览阶段是否只显示范围。
- 随机测试是否需要固定种子或可注入随机源。
- damageFormula 是否继续使用当前 PointAsDamage / DoublePointDamage。
- 伤害是否直接打 actualTargetCharacter HP。
- actualTargetSlotIndex 是否只用于定位 / 槽位效果。
- 敌人无人响应攻击未来是否触发独立事件链。

当前不实现：

- 实际 roll 敌人攻击点数。
- 伤害计算。
- 扣血。
- TakeDamage(...)。
- BattleResolver 正式结算接入。
- 敌人单方面攻击伤害入口。
- Buff / Debuff 对敌人攻击点数的处理。
- 固定随机种子。
- 敌人卡牌事件。
- UseCount / CD。
- item.isCompleted 切换。
- plan.isCompleted 切换。
- RespondedEnemyIntent 拼点。
- FreeAction 混排。

当前结论：

- 未来无人响应攻击应保留敌人卡点数意义。
- 当前先记录为“未来会 roll 敌人攻击点数”。
- 当前实现层仍停留在点数范围 / 伤害前预览。
- 下一步如果继续推进，可以考虑增强 UnrespondedEnemyIntent 预览日志，显示敌人卡点数范围，但仍不实际 roll。

### 34. UnrespondedEnemyIntent 无人响应攻击第一版结算设计草案

当前背景：

- UnrespondedEnemyIntent 目前已经能在 BattleExecutionPlanExecutor.PrintUnrespondedEnemyIntentStepPreview(...) 中预览敌人意图。
- UnrespondedEnemyIntent 目前已经能预览敌人卡牌。
- UnrespondedEnemyIntent 目前已经能预览敌人攻击点数范围。
- UnrespondedEnemyIntent 目前已经能预览将命中角色。
- UnrespondedEnemyIntent 目前已经能预览将命中槽位。
- 当前仍然只预览，不执行。
- 当前不 roll。
- 当前不结算。
- 当前不改状态。

未来处理流程建议：

- BattleExecutionPlanExecutor 未来识别 UnrespondedEnemyIntent 执行项后，只负责分派。
- 具体无人响应敌人攻击结算不建议全部塞进 Executor。
- 未来可以交给专门的 Resolver / DamageHandler 处理。
- 具体类名暂不决定。

敌人攻击点数设计：

- 未来正式结算时，无人响应敌人攻击需要从敌人卡牌点数范围 roll 点。
- 字段来源为 enemyIntent.enemyCardState.cardData.minPoint。
- 字段来源为 enemyIntent.enemyCardState.cardData.maxPoint。
- 建议语义命名为 enemyAttackPoint。
- 或命名为 attackPoint。
- 不建议叫 clashPoint，因为无人响应攻击没有发生拼点。

damageFormula: PointAsDamage 第一版解释：

- 第一版可以理解为：最终伤害 = 敌人攻击点数。
- 例如敌人攻击点数 roll 出 6，则造成 6 点伤害。
- 当前只记录设计，不实现扣血。

伤害目标来源：

- 未来无人响应攻击命中目标应使用 enemyIntent.actualTargetCharacter。
- 未来无人响应攻击命中槽位应使用 enemyIntent.actualTargetSlotIndex。
- 不应重新回退使用 originalTarget。
- 因为未来可能存在介入、目标改写、槽位效果等逻辑，正式结算应尊重 actualTarget。

当前阶段明确不做：

- 不实现正式结算。
- 不 roll 点数。
- 不计算伤害。
- 不扣血。
- 不调用 BattleResolver。
- 不调用 TakeDamage(...)。
- 不触发 Hit / AfterDamage / AfterKill。
- 不处理敌人卡牌 UseCount / CD。
- 不调用 slot.MarkUsed。
- 不切换 item.isCompleted。
- 不切换 plan.isCompleted。

下一步候选方向：

- 先设计 BattleExecutionPlanExecutor 正式执行入口空壳。
- 或先设计无人响应攻击 Resolver / DamageHandler 的职责边界。
- 或先设计 damageFormula 的数据解释规则。
- 当前建议优先设计 Resolver / DamageHandler 职责边界，避免后续把所有逻辑塞进 Executor。

### 35. UnrespondedEnemyIntent 结算职责边界结构审查

现有结构审查结论：

- 当前项目已有 BattleResolver.cs，它是主要战斗结算器。
- BattleResolver 已包含拼点、卡牌生效、伤害计算、命中、扣血、击杀事件等相关流程。
- BattleResolver 内已有 RollCardPoint(...)、ApplyDamageAndTriggerEvents(...)、TriggerBattleEvent(...) 等能力。
- 但这些关键方法目前多为 private/static。
- 当前公开入口主要偏测试性质，例如 TestClash(...)、TestUseAbilitySinCard(...)。
- 因此当前还没有一个适合 UnrespondedEnemyIntent 直接调用的公开“单方面攻击结算入口”。

BattleCalculator.cs 现有能力：

- 有 Rollpoint(minPoint, maxPoint)。
- 有 GetFinalClashPoint(...)，目前偏拼点相关 Buff 计算。
- 有 GetFinalDamageScaled(...)。
- 有 ConvertScaledDamageToHPDamage(...)。
- 未来无人响应攻击若进入正式伤害结算，需要决定是否复用这些 scaled damage 逻辑。

CharacterData.cs 现有能力：

- 有 currentHP / maxHP。
- 有 TakeDamage(int damage)。
- TakeDamage(...) 当前只负责直接扣 HP 和打印日志。
- TakeDamage(...) 不负责 Hit / AfterDamage / AfterKill / Resolved / 卡牌效果。

BattleExecutionPlanExecutor.cs 当前边界：

- 当前只做执行步骤预览。
- 当前不执行 item。
- 当前不修改状态。
- 未来不建议把 roll 点、伤害计算、扣血、事件触发都塞进 Executor。

推荐未来三层分工：

- BattleExecutionPlanExecutor 遍历 BattleExecutionPlan.executionItems。
- BattleExecutionPlanExecutor 根据 BattleExecutionItemType 分派。
- BattleExecutionPlanExecutor 不直接 roll 点。
- BattleExecutionPlanExecutor 不直接算伤害。
- BattleExecutionPlanExecutor 不直接扣 HP。
- BattleExecutionPlanExecutor 不直接触发事件。
- 未来敌人意图处理层，例如暂称 BattleEnemyIntentResolver，处理 UnrespondedEnemyIntent 规则。
- BattleEnemyIntentResolver 校验 enemyIntent / enemy / enemyCardState / actualTargetCharacter / actualTargetSlotIndex。
- BattleEnemyIntentResolver 使用 actualTargetCharacter / actualTargetSlotIndex。
- BattleEnemyIntentResolver 不回退 originalTarget。
- BattleEnemyIntentResolver 决定是否进入敌人单方面攻击结算。
- BattleEnemyIntentResolver 不遍历整个 ExecutionPlan。
- BattleResolver 或未来拆出的 DamageHandler 负责真正 roll 点。
- BattleResolver 或 DamageHandler 解释 damageFormula。
- BattleResolver 或 DamageHandler 计算伤害。
- BattleResolver 或 DamageHandler 扣血。
- BattleResolver 或 DamageHandler 处理死亡判断。
- BattleResolver 或 DamageHandler 处理事件链和卡牌效果。

当前不建议实现正式结算的原因：

- enemyAttackPoint 是否受 Strength / Weakness / NextClashPointUp 影响尚未决定。
- PointAsDamage 是直接等于 HP 伤害，还是仍走 scaled damage 尚未决定。
- 无人响应攻击是否触发 Resolved / Hit / AfterDamage / AfterKill 尚未决定。
- 敌人卡牌是否触发 effects 尚未决定。
- 敌人卡牌是否完全跳过 UseCount / CD 尚未决定。
- item.isCompleted / plan.isCompleted 的切换时机尚未决定。
- 异常 item 是跳过、失败还是中止 plan 尚未决定。
- 现在直接实现容易形成第二套伤害路径。

下一步最小安全设计动作：

- 先设计 BattleEnemyIntentResolver 或“敌人意图处理层”的职责边界。
- 再设计 BattleResolver 未来是否需要新增公开“单方面攻击结算”入口。
- 暂时不写正式执行代码。

当前不实现：

- 不新增类。
- 不写正式执行逻辑。
- 不 roll 点数。
- 不扣血。
- 不调用 TakeDamage(...)。
- 不切换 item.isCompleted / plan.isCompleted。

### 36. UnrespondedEnemyIntent 第一版伤害结算关键规则决策

- 本节只记录第一版无人响应伤害结算的关键规则决策；
- 当前不实现代码；
- 当前不新增类；
- 当前不 roll 点数；
- 当前不计算伤害；
- 当前不扣血；
- 当前不进入正式执行。

#### 36.1 enemyAttackPoint 是否吃 Buff

- 第一版暂定：`enemyAttackPoint` 不吃拼点类 Buff；
- 无人响应攻击没有发生拼点；
- 因此不直接走 `BattleCalculator.GetFinalClashPoint(...)`；
- `enemyAttackPoint` 第一版只来自敌人卡牌点数范围 roll：

  - `enemyIntent.enemyCardState.cardData.minPoint`
  - `enemyIntent.enemyCardState.cardData.maxPoint`

- 当前暂不处理：

  - `Strength`
  - `Weakness`
  - `NextClashPointUp`
  - 槽位拼点 Buff

- 后续如果需要，可以单独设计攻击点数或受击修正类 Buff；
- 例如：

  - `AttackPointUp`
  - `EnemyAttackPointUp`
  - `SlotIncomingDamageUp`

- 具体命名当前暂不决定。

#### 36.2 PointAsDamage 第一版语义

- 第一版可以理解为：以 `enemyAttackPoint` 作为基础伤害来源；
- 例如敌人攻击点数 roll 出 `6`，则基础伤害来源为 `6`；
- 但当前不在 `BattleEnemyIntentResolver` 中直接解释全部伤害公式；
- 未来应由 `BattleResolver` 或 DamageHandler 统一解释 `damageFormula`；
- 具体数值计算尽量复用 `BattleCalculator`；
- `PointAsDamage` 未来到底是：

  - 直接等于最终 HP 伤害；
  - 还是仍然经过 scaled damage / damage multiplier；

- 需要在正式伤害入口设计时再决定。

#### 36.3 无人响应攻击第一版是否触发事件链

- 第一版暂定：不触发玩家卡牌式事件链；
- 当前暂不触发：

  - `Resolved`
  - `Hit`
  - `AfterDamage`
  - `AfterKill`

- 继续维持之前记录的边界：

  - 敌人无人响应攻击第一版暂不触发玩家卡牌式 `Resolved / Hit / AfterDamage / AfterKill`；

- 后续如果敌人卡牌需要 effects，可以再设计敌人专用事件链或通用事件链；
- 例如：

  - `EnemyHit`
  - `EnemyAfterDamage`
  - `EnemyAfterKill`

- 具体命名当前暂不决定。

#### 36.4 当前仍然不实现

- 不新增类；
- 不写 `BattleEnemyIntentResolver`；
- 不写正式执行入口；
- 不 roll 点数；
- 不计算伤害；
- 不扣血；
- 不调用 `BattleResolver`；
- 不调用 `TakeDamage(...)`；
- 不切换 `item.isCompleted`；
- 不切换 `plan.isCompleted`。

#### 36.5 下一步候选

- 在这些规则决策基础上，继续设计 `BattleEnemyIntentResolver` 的第一版职责边界和方法输入输出；
- 当前仍然先只做设计；
- 当前不急着写代码。

### 37. 未来扩展：不可预测卡

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

### 38. 未来扩展：槽位 Buff / 特殊槽位

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

### 39. 当前阶段不实现内容

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

## 四十四、下一步候选方向

后续候选方向：

- 继续完善敌人意图系统。
- 设计玩家“响应敌人意图”和“偷刀”的正式执行顺序。
- 后续再考虑速度队列。
- 后续再考虑 UI 拖拽和槽位显示。
- 敌人防御逻辑暂缓。
- 负罪感阈值暂缓。

## 四十五、当前规则提醒

- 负罪感不是消耗资源，而是从 0 开始累计增加。
- 使用罪卡会增加 guiltGain。
- 负罪感阈值惩罚暂时不做。
- 罪卡分为 Clash 和 Ability。
- Clash 罪卡参与拼点。
- Ability 罪卡不参与拼点，直接执行 effects。
- UseCount 罪卡按次数消耗。
- Permanent 罪卡不进入普通 CD，也不按次数消耗。

## 四十六、BattleExecutionPlan 速度规则生成第一版完成

当前完成内容：

- 已新增 `BattleExecutionPlanManager.CreateSpeedBasedExecutionPlan(...)`。
- 保留 `CreateBasicExecutionPlan(...)` 不变。
- `CreateSpeedBasedExecutionPlan(...)` 按三阶段生成执行计划：
  1. 高速玩家行动阶段。
  2. 敌人意图节奏阶段。
  3. 低速自由行动阶段。

已确认的速度规则：

- `actor.GetCurrentSpeed() > enemy.GetCurrentSpeed()` 视为高速，可以抢先。
- `actor.GetCurrentSpeed() <= enemy.GetCurrentSpeed()` 视为低速，不能抢先。
- 速度相等不能抢先。
- 高速响应可以提前处理敌人意图。
- 低速响应可以响应原目标槽位，但不提前。
- 高速自由行动可以排在敌人攻击前。
- 低速自由行动排在敌人攻击后。
- 无人响应敌人意图按 `intentOrder` 处理。

Unity 测试已通过：

- `ActionSlotLowSpeedOriginalSlotResponseBasic`：低速原目标槽位响应成功，不改写 `actualTarget`。
- `ActionSlotLowSpeedIllegalResponseFail`：低速非法响应失败，不绑定，不改写 `actualTarget`。
- `ActionSlotExecutionPlanSpeedHighResponseOrderBasic`：高速响应可以让后编号敌人意图提前进入计划。
- `ActionSlotExecutionPlanSpeedHighFreeActionBasic`：高速自由行动排在无人响应敌人意图前。
- `ActionSlotExecutionPlanSpeedLowFreeActionBasic`：低速自由行动排在无人响应敌人意图后。
- `ActionSlotExecutionPlanSpeedLowResponseOrderBasic`：低速响应不提前，按敌人意图原顺序进入计划。

当前仍未处理：

- 本阶段只生成和打印计划。
- 不执行 plan。
- 不 roll 点。
- 不扣血。
- 不调用 `BattleResolver`。
- 不处理正式伤害公式、CD、UseCount、Buff、事件链。

阶段意义：

- 第 11 条“基于槽位、敌人意图和速度的执行顺序规则”第一版可以标记为完成。
- 后续 `BattleExecutionPlanExecutor` 应只按生成好的 plan 顺序执行，不在执行时临时决定顺序。
- 下一阶段可以进入第 10 条：研究如何把 `BattleExecutionPlanExecutor` 接回正式 `BattleResolver` 结算。

## 四十七、BattleExecutionPlanExecutor 接回 BattleResolver 第一版通过

当前完成内容：

- `BattleResolver` 已新增正式入口：
  - `ResolveRespondedEnemyIntent(BattleActionSlot actionSlot, BattleEnemyIntent enemyIntent)`
- 已新增 `BattleResolveResult` 结果结构。
- `BattleExecutionPlanExecutor.ExecuteRespondedEnemyIntent(...)` 已改为调用：
  - `BattleResolver.ResolveRespondedEnemyIntent(item.actionSlot, item.enemyIntent)`
- Executor 的 `RespondedEnemyIntent` 分支不再自己 roll 点。
- Executor 的 `RespondedEnemyIntent` 分支不再自己比较点数。
- Executor 的 `RespondedEnemyIntent` 分支不再自己计算伤害。
- Executor 的 `RespondedEnemyIntent` 分支不再直接调用 `TakeDamage(...)`。
- Executor 现在根据 `BattleResolveResult.isSuccess && BattleResolveResult.shouldCompleteItem` 决定是否标记 `item.isCompleted = true`。

当前支持范围：

- 第一版只支持 `RespondedEnemyIntent` 的攻击卡 vs 攻击卡。
- 正常胜负会走 Resolver 事件链。
- 10 次平局上限时不触发事件链。
- `UnrespondedEnemyIntent` 仍保持第一版简化结算，暂未接回 Resolver。
- 暂不处理防御、闪避、Ability、FreeAction、`slot.MarkUsed()`。

已通过测试：

- `BattleResolverResolveRespondedAttackVsAttackBasic`
  - 直接测试 Resolver 新入口。
  - 玩家胜利、敌人胜利、10 次平局上限均通过。
- `ActionSlotExecutionPlanExecuteRespondedBasic`
  - Executor 调用 Resolver 后，玩家胜利分支通过。
  - 敌人受伤，`ExecutionPlan.isCompleted == true`。
- `ActionSlotExecutionPlanExecuteRespondedEnemyWin`
  - 敌人胜利分支通过。
  - 伤害打到 `actualTargetCharacter`，不回退 `originalTarget`。
  - `ExecutionPlan.isCompleted == true`。
- `ActionSlotExecutionPlanExecuteRespondedTieLimit`
  - 10 次平局上限通过。
  - 双方 HP 不下降。
  - `playerCardUsed = false`，`enemyCardUsed = false`，`triggeredEventChain = false`。
  - `ExecutionPlan.isCompleted == true`。

阶段意义：

- `BattleExecutionPlanExecutor` 开始从“临时结算器”回到“执行计划调度员”定位。
- `BattleResolver` 开始承担正式 Responded 结算入口。
- 第 10 条“将执行计划执行器接回正式战斗结算”已完成第一版 Responded 攻击 vs 攻击接入。

后续未处理：

- `UnrespondedEnemyIntent` 接回 Resolver。
- 防御 / 闪避接入 Resolver。
- Ability / FreeAction 正式执行。
- `slot.MarkUsed()`。
- 更完整的 CD / UseCount / Buff / 事件链整理。
- 旧 `TestClash(...)` 与正式入口关系整理。

## 四十八、BattleExecutionPlanExecutor 接回 BattleResolver：敌人意图分支第一版通过

当前完成内容：

- `BattleExecutionPlanExecutor` 的 `RespondedEnemyIntent` 分支已调用：
  - `BattleResolver.ResolveRespondedEnemyIntent(item.actionSlot, item.enemyIntent)`
- `BattleExecutionPlanExecutor` 的 `UnrespondedEnemyIntent` 分支已调用：
  - `BattleResolver.ResolveUnrespondedEnemyIntent(item.enemyIntent)`
- Executor 不再在这两个分支里自行 roll 点。
- Executor 不再在这两个分支里自行比较点数。
- Executor 不再在这两个分支里自行计算伤害。
- Executor 不再在这两个分支里直接调用 `TakeDamage(...)`。
- Executor 当前负责：
  - 遍历 `BattleExecutionPlan`。
  - 根据 item 类型分派给 Resolver。
  - 读取 `BattleResolveResult`。
  - 根据 `result.isSuccess && result.shouldCompleteItem` 标记 `item.isCompleted`。
  - 全部 item 完成后标记 `plan.isCompleted`。

当前 Resolver 支持范围：

- `ResolveRespondedEnemyIntent(...)`
  - 第一版支持攻击卡 vs 攻击卡。
  - 支持玩家胜利、敌人胜利、10 次平局上限。
  - 正常胜负触发事件链。
  - 10 次平局上限不触发事件链。
- `ResolveUnrespondedEnemyIntent(...)`
  - 第一版支持无人响应敌人攻击。
  - 使用敌人卡牌点数 roll `enemyAttackPoint`。
  - 使用 `BattleCalculator.GetFinalDamageScaled(...)` 和 `ConvertScaledDamageToHPDamage(...)`。
  - 扣 `actualTargetCharacter` 的 HP。
  - 不触发事件链。
  - 不处理敌人卡牌 CD / UseCount。

已通过测试：

- `ActionSlotExecutionPlanExecuteRespondedBasic`
- `ActionSlotExecutionPlanExecuteRespondedEnemyWin`
- `ActionSlotExecutionPlanExecuteRespondedTieLimit`
- `ActionSlotExecutionPlanExecuteUnrespondedBasic`
  - `UnrespondedEnemyIntent` 通过 Resolver 执行。
  - `allyB HP` 下降。
  - `ExecutionPlan.isCompleted == true`。
  - 重复执行同一个 plan 时已完成 item 被跳过，不重复扣血。
- `ActionSlotExecutionPlanExecuteMixedBasic`
  - 混合 plan 中 `RespondedEnemyIntent` 和 `UnrespondedEnemyIntent` 都通过 Resolver 执行。
  - `ExecutionPlan.isCompleted == true`。

阶段意义：

- `BattleExecutionPlanExecutor` 已经从临时结算器进一步回到“执行计划调度员”定位。
- `BattleResolver` 开始承担敌人意图相关正式结算入口。
- 第 10 条“将执行计划执行器接回正式战斗结算”已完成敌人意图分支第一版接入。

当前仍未处理：

- 防御 / 闪避接入 Resolver。
- Ability / FreeAction 正式执行。
- `slot.MarkUsed()`。
- 敌人事件链、敌人 CD / UseCount。
- 更完整的 Buff / CD / UseCount / 负罪感整理。
- 旧 `TestClash(...)` 与正式入口关系整理。
- UI / 动画表现。

## 四十九、ActionSlot MarkUsed 与卡牌使用状态边界记录

当前现状：

- `BattleActionSlot.MarkUsed()` 已存在，作用是将 `slot.isUsed = true`。
- 当前主线 `BattleExecutionPlanExecutor` 还没有调用 `MarkUsed()`。
- 当前 ExecutionPlan 主线主要靠 `item.isCompleted` 和 `plan.isCompleted` 表示执行进度。
- 旧测试流程中可能仍有 `actionSlot.MarkUsed()` 调用，但不属于当前 ExecutionPlan 主线。

CD / UseCount / guiltGain 当前机制：

- 当前主要通过事件链触发。
- 触发路径为：
  - `BattleResolver.TriggerBattleEvent(...)`
  - `BattleEventProcessor.ProcessEvent(...)`
  - `BattleCardManager.HandleEvent(...)`
  - `BattleTiming.Resolved`
  - `BattleCardManager.ApplyCooldownOnResolved(...)`
- `Resolved` 触发时，普通卡进入 CD。
- 罪卡触发 `Resolved` 时，根据规则增加 UseCount / guiltGain。
- 10 次平局上限不触发事件链，因此不会触发 CD / UseCount / guiltGain。
- `ResolveUnrespondedEnemyIntent(...)` 不触发事件链，因此不会触发玩家卡牌式 CD / UseCount / guiltGain。

几个状态的区别：

- `item.isCompleted`：执行计划项是否已经处理完，属于执行队列进度状态。
- `playerCardUsed`：Resolver 返回的玩家卡是否算本次被使用 / 参与处理。
- `enemyCardUsed`：敌人卡是否算本次参与处理；第一版只作为结果记录，不接敌人 CD / UseCount。
- `slot.isUsed`：玩家行动槽位是否已经实际执行过，偏 UI / 回合状态，不代替 `item.isCompleted`。

第一版职责边界：

- `Resolver` 负责结算，不直接调用 `slot.MarkUsed()`。
- `Executor` 拥有 `BattleExecutionItem`，能同时看到 `item.actionSlot` 和 `BattleResolveResult`。
- 因此第一版建议由 `BattleExecutionPlanExecutor` 根据 `result.playerCardUsed` 调用 `item.actionSlot.MarkUsed()`。

第一版 MarkUsed 规则建议：

- `RespondedEnemyIntent` 正常 `PlayerWin / EnemyWin`：
  - 如果 `result.playerCardUsed == true` 且 `item.actionSlot != null`，调用 `MarkUsed()`。
- `TieLimit`：
  - 不调用 `MarkUsed()`，因为双方都视作没有成功使用卡牌。
- `UnrespondedEnemyIntent`：
  - 不调用 `MarkUsed()`，因为没有玩家 actionSlot。
- 未来 `FreeAction`：
  - 成功执行后调用 `MarkUsed()`。
- 未来防御 / 闪避空挂但没有处理任何敌人攻击：
  - 不调用 `MarkUsed()`。

注意事项：

- 暂时不要把 `playerCardUsed` 直接等同于 CD / UseCount / guiltGain。
- CD / UseCount / guiltGain 当前仍继续通过 `Resolved` 事件链处理。
- 敌人卡牌 CD / UseCount 暂不处理。
- 暂不重命名 `BattleResolveResult` 字段。
- 暂不处理 FreeAction / 防御 / 闪避的 MarkUsed。

下一步候选：

- 小范围修改 `BattleExecutionPlanExecutor.ExecuteRespondedEnemyIntent(...)`。
- 在 Resolver 返回成功后，如果 `result.playerCardUsed == true && item.actionSlot != null`，调用 `item.actionSlot.MarkUsed()`。
- 回归测试：
  - 玩家胜利：槽位 `isUsed == true`。
  - 敌人胜利：槽位 `isUsed == true`。
  - 10 次平局：槽位 `isUsed == false`。

## 五十、ActionSlot MarkUsed 第一版接入通过

当前完成内容：

- `BattleExecutionPlanExecutor.ExecuteRespondedEnemyIntent(...)` 已根据 `BattleResolveResult.playerCardUsed` 接入 `item.actionSlot.MarkUsed()`。
- 当 Resolver 返回 `isSuccess == true`、`shouldCompleteItem == true`，并且 `playerCardUsed == true`、`item.actionSlot != null` 时，Executor 会将玩家行动槽位标记为已使用。
- `item.isCompleted` 仍然由 Executor 根据 `BattleResolveResult` 标记。
- `UnrespondedEnemyIntent` 未接入 MarkUsed，因为没有玩家 actionSlot。
- 没有修改 CD / UseCount / guiltGain 逻辑，这些仍然通过 `Resolved` 事件链处理。

已通过测试：

- `ActionSlotExecutionPlanExecuteRespondedBasic`
  - 玩家胜利。
  - `playerCardUsed == true`。
  - 槽位显示 `已使用：True`。
  - `ExecutionPlan.isCompleted == true`。
- `ActionSlotExecutionPlanExecuteRespondedEnemyWin`
  - 敌人胜利。
  - 当前规则下 `playerCardUsed == true`。
  - 槽位显示 `已使用：True`。
  - `ExecutionPlan.isCompleted == true`。
- `ActionSlotExecutionPlanExecuteRespondedTieLimit`
  - 10 次平局上限。
  - `playerCardUsed == false`。
  - 槽位保持 `已使用：False`。
  - 双方 HP 不下降。
  - `ExecutionPlan.isCompleted == true`。

当前确认的规则：

- `PlayerWin`：玩家卡算使用，槽位 MarkUsed。
- `EnemyWin`：玩家卡参与正式处理，槽位 MarkUsed。
- `TieLimit`：执行项完成，但双方卡牌都不算成功使用，槽位不 MarkUsed。
- `UnrespondedEnemyIntent`：没有玩家槽位，不 MarkUsed。
- 未来 `FreeAction` 成功执行后再考虑 MarkUsed。
- 未来防御 / 闪避空挂但未触发时，不应 MarkUsed。

阶段意义：

- `item.isCompleted` 和 `slot.isUsed` 的职责开始分离。
- `item.isCompleted` 用于防止执行项重复执行。
- `slot.isUsed` 用于表示玩家槽位实际被消耗，未来可供 UI 和回合结束流程读取。
- 这一步为后续接入简易 UI 的槽位状态显示打下基础。

当前仍未处理：

- FreeAction MarkUsed。
- 防御 / 闪避 MarkUsed。
- 回合结束统一清理槽位。
- 敌人卡牌 Used / CD / UseCount。
- UI 状态刷新。

## 五十一、FreeAction 第一版接入设计审查记录

当前 FreeAction 结构现状：

- `BattleActionSlotType.FreeAction` 已存在。
- `BattleExecutionItemType.FreeAction` 已存在。
- `BattleActionSlot.AssignFreeAction(...)` 已能记录 `actor / cardState / target / slotType = FreeAction / enemyIntent = null / isUsed = false`。
- `BattleActionSlotManager.AssignFreeAction(...)` 已能安排自由行动，并检查槽位、行动者、卡牌重复安排。
- `CreateSpeedBasedExecutionPlan(...)` 已会把 FreeAction 加入执行计划。
- `BattleExecutionPlanExecutor` 当前遇到 `FreeAction` 只打印暂未实现，并让该 item 保持未完成。

FreeAction 当前排序规则：

- `CreateSpeedBasedExecutionPlan(...)` 第一阶段会加入高速 FreeAction。
- 第二阶段处理敌人意图。
- 第三阶段加入低速 FreeAction。
- `IsHighSpeedFreeActionSlot(...)` 当前判断为：`slotType == FreeAction`、`target != null`、`actor.GetCurrentSpeed() > target.GetCurrentSpeed()`。
- 当前排序符合第一版规则：高速 FreeAction 在敌人意图前，低速 FreeAction 在敌人意图后。

第一版支持范围建议：

- 支持 Ability FreeAction。
- 支持 Attack FreeAction，也就是偷刀攻击 `actionSlot.target`。
- 暂不处理防御、闪避、复杂特殊卡、多目标、群体效果、槽位效果、目标重新选择。

Resolver 正式入口建议：

- 新增 public 入口：
  - `BattleResolver.ResolveFreeAction(BattleActionSlot actionSlot)`
- 不建议让 Executor 直接调用 `TestUseAbilitySinCard(...)`，因为这是测试命名方法，且返回 `void`，不利于 Executor 根据 `BattleResolveResult` 判断是否完成、是否 MarkUsed。
- 建议 `ResolveFreeAction(...)` 内部根据卡牌类型分派到 private 方法，例如 Ability / Attack 分支。
- Executor 未来只认识一个 FreeAction Resolver 入口。

Ability FreeAction 规则建议：

- 不 roll 点。
- 不造成伤害。
- 触发 `OnPlay`。
- 触发 `Resolved`。
- 由 `Resolved` 带动 UseCount / guiltGain / CD。
- 成功后返回 `playerCardUsed = true`、`shouldCompleteItem = true`。
- Executor 根据结果调用 `slot.MarkUsed()`。

Attack FreeAction 规则建议：

- 玩家使用攻击卡直接攻击 `actionSlot.target`。
- 不与敌人拼点。
- roll 玩家攻击点数。
- 走 `BattleCalculator.GetFinalDamageScaled(...)` 和 `ConvertScaledDamageToHPDamage(...)`。
- 正常触发事件链。
- 成功后返回 `playerCardUsed = true`、`shouldCompleteItem = true`。
- Executor 根据结果调用 `slot.MarkUsed()`。
- `enemyPoint = 0`，`clashAttemptCount = 0`。

`BattleResolveResult` 返回建议：

- Ability FreeAction：
  - `resultType = "FreeAbility"`
  - `isSuccess = true`
  - `shouldCompleteItem = true`
  - `playerCardUsed = true`
  - `enemyCardUsed = false`
  - `hasDamage = false`
  - `damage = 0`
  - `damagedCharacter = null`
  - `triggeredEventChain = true`
- Attack FreeAction：
  - `resultType = "FreeAttack"`
  - `isSuccess = true`
  - `shouldCompleteItem = true`
  - `playerCardUsed = true`
  - `enemyCardUsed = false`
  - `hasDamage = damage > 0`
  - `damage = finalHpDamage`
  - `damagedCharacter = actionSlot.target`
  - `triggeredEventChain = true`
- Invalid / Unsupported：
  - `isSuccess = false`
  - `shouldCompleteItem = false`
  - `playerCardUsed = false`
  - `triggeredEventChain = false`

职责边界：

- Executor：
  - 遍历 plan。
  - 遇到 FreeAction 时调用 `BattleResolver.ResolveFreeAction(item.actionSlot)`。
  - 根据 `result.isSuccess && result.shouldCompleteItem` 设置 `item.isCompleted = true`。
  - 根据 `result.playerCardUsed && item.actionSlot != null` 调用 `item.actionSlot.MarkUsed()`。
  - 不 roll 点、不算伤害、不触发事件。
- Resolver：
  - 校验 FreeAction 数据。
  - 判断 Attack / Ability。
  - 执行正式结算。
  - 返回 `BattleResolveResult`。
- BattleCardManager：
  - 继续通过 `Resolved` 事件处理 CD / UseCount / guiltGain。

最小安全接入步骤：

- 第一步：新增 `BattleResolver.ResolveFreeAction(BattleActionSlot actionSlot)`，先只用 `CardLoadTest` 直接测试 Resolver。
- 第二步：先测试 Ability FreeAction。
- 第三步：再测试 Attack FreeAction。
- 第四步：Resolver 入口稳定后，再改 Executor 的 FreeAction 分支。
- 第五步：回归高速 FreeAction / 低速 FreeAction 的执行计划顺序和实际执行。

暂时不建议处理：

- 防御 / 闪避 FreeAction。
- 群体目标 / 多目标选择。
- 目标为空时自动选敌人。
- 敌人 FreeAction。
- FreeAction 与敌人意图的额外打断规则。
- 槽位 Buff / 复杂 Buff。
- UI 刷新。
- 动画。
- 新的 CD / UseCount 触发路径。
- 直接在 Executor 里写伤害逻辑。

## 五十二、ResolveFreeAction Ability 第一版直接测试通过

当前完成内容：

- `BattleResolver` 已新增正式入口：
  - `ResolveFreeAction(BattleActionSlot actionSlot)`
- 第一版 `ResolveFreeAction(...)` 只支持 `Ability FreeAction`。
- 本次没有修改 `BattleExecutionPlanExecutor.cs`。
- 本次没有将 FreeAction 接入 Executor。
- 本次没有处理 Attack FreeAction。
- 本次没有处理 MarkUsed。
- 本次没有修改 JSON。

Ability FreeAction 当前规则：

- 不进入拼点。
- 不 roll 点。
- 不造成伤害。
- 触发 `BattleTiming.OnPlay`。
- 触发 `BattleTiming.Resolved`。
- `Resolved` 继续交给现有事件链处理 UseCount / guiltGain / CD。
- `ResolveFreeAction(...)` 不手动修改 UseCount / guiltGain。
- 如果 `actionSlot.target == null`，当前最小安全方案是使用 actor 自己作为 target。

`BattleResolveResult` 返回结果：

- `resultType = FreeAbility`
- `isSuccess = true`
- `shouldCompleteItem = true`
- `playerCardUsed = true`
- `enemyCardUsed = false`
- `hasDamage = false`
- `damage = 0`
- `triggeredEventChain = true`

已通过测试：

- 新增测试模式：
  - `BattleResolverResolveFreeAbilityBasic`
- 测试直接调用：
  - `BattleResolver.ResolveFreeAction(actionSlot)`
- 不生成 ExecutionPlan。
- 不调用 Executor。
- 测试结果：
  - `Ability UseCount：0 / 2 → 1 / 2`
  - `allyA 负罪感：0 → 2`
  - `actionSlot.isUsed` 保持 `False`
  - 所有预期判断均为 `True`

阶段意义：

- FreeAction 的 Resolver 正式入口已经开始建立。
- Ability FreeAction 已能脱离旧测试命名入口，走正式 `ResolveFreeAction(...)`。
- UseCount / guiltGain 继续保持事件链驱动。
- `MarkUsed()` 仍然留给 Executor 未来根据 `playerCardUsed` 处理。

当前仍未处理：

- Executor 接入 FreeAction。
- FreeAction 成功后由 Executor 调用 `slot.MarkUsed()`。
- Attack FreeAction / 偷刀。
- 防御 / 闪避 FreeAction。
- 多目标 / 群体目标。
- UI 状态刷新。

## 五十三、FreeAction Ability 第一版 Executor 接入通过

当前完成内容：

- `BattleExecutionPlanExecutor` 的 `FreeAction` 分支已接入。
- `FreeAction` 分支现在会调用私有方法：
  - `ExecuteFreeAction(BattleExecutionItem item)`
- `ExecuteFreeAction(...)` 内部调用：
  - `BattleResolver.ResolveFreeAction(item.actionSlot)`
- Executor 不判断 Ability / Attack。
- Executor 不直接处理伤害、UseCount、guiltGain。
- Executor 只负责：
  - 调用 Resolver。
  - 读取 `BattleResolveResult`。
  - 根据 `result.isSuccess && result.shouldCompleteItem` 标记 `item.isCompleted`。
  - 根据 `result.playerCardUsed && item.actionSlot != null` 调用 `item.actionSlot.MarkUsed()`。

当前支持范围：

- 第一版只验证 `Ability FreeAction`。
- `Attack FreeAction / 偷刀` 暂未处理。
- 防御 / 闪避 / 多目标 / UI / 动画暂未处理。
- 没有修改 `BattleResolver.cs`。
- 没有修改 JSON。
- 没有新增 CD / UseCount / guiltGain 触发路径，仍走 `Resolved` 事件链。

已通过测试：

- 新增测试模式：
  - `ActionSlotExecutionPlanExecuteFreeAbilityBasic`
- 测试流程：
  - 创建 Ability FreeAction 槽位。
  - 生成只包含 FreeAction 的 `BattleExecutionPlan`。
  - 调用 `BattleExecutionPlanExecutor.ExecuteExecutionPlan(executionPlan)`。
  - Executor 调用 `BattleResolver.ResolveFreeAction(...)`。
- 测试结果：
  - `resultType = FreeAbility`
  - `isSuccess = true`
  - `shouldCompleteItem = true`
  - `playerCardUsed = true`
  - `enemyCardUsed = false`
  - `hasDamage = false`
  - `damage = 0`
  - `triggeredEventChain = true`
  - `Ability UseCount：0 / 2 → 1 / 2`
  - `allyA 负罪感：0 → 2`
  - `actionSlot.isUsed：False → True`
  - `ExecutionPlan.isCompleted = true`

阶段意义：

- `FreeAction` 已经开始进入正式 ExecutionPlan 执行路径。
- `Ability FreeAction` 已完成从 Resolver 直接测试到 Executor 接入测试的闭环。
- Executor 的定位继续保持为“调度员”：
  - 不写 Ability 具体逻辑。
  - 不判断卡牌类型。
  - 不手动处理 UseCount / guiltGain。
- Resolver 负责正式结算，BattleCardManager 继续通过 `Resolved` 事件处理卡牌消耗与负罪感。

当前仍未处理：

- Attack FreeAction / 偷刀。
- FreeAction 的速度混合回归测试。
- 防御 / 闪避。
- 回合结束清理槽位。
- UI 状态刷新。
- 多目标 / 群体目标。
- 动画表现。

## 五十四、FreeAction 速度排序边界记录

当前 FreeAction 速度排序现状：

- `CreateSpeedBasedExecutionPlan(...)` 当前通过 `IsHighSpeedFreeActionSlot(...)` 判断高速自由行动。
- 当前核心判断是：
  - `slot.actor.GetCurrentSpeed() > slot.target.GetCurrentSpeed()`
- 也就是说，当前 `slot.target` 同时承担了两个语义：
  - 卡牌效果目标。
  - 速度比较目标。

Attack FreeAction / 偷刀：

- 当前结构对 Attack FreeAction 是可用的。
- 例如：
  - `actor = allyA`
  - `target = enemy`
  - 如果 `allyA speed > enemy speed`
- 当前排序会把该 FreeAction 放到敌人意图前。
- 这符合第一版规则：
  - 想在敌人攻击前偷刀，必须速度大于敌人。

Ability FreeAction：

- Ability self target 当前存在已知边界。
- 例如：
  - `actor = allyA`
  - `target = allyA`
- 当前速度判断会变成：
  - `allyA speed > allyA speed`
- 结果永远为 false。
- 因此自我 Buff 类 Ability FreeAction 当前会默认进入低速 FreeAction 阶段。
- 如果当前计划里有敌人意图，它会排在敌人意图后面。

当前判断：

- 这不是代码 bug，而是规则边界尚未拆清楚。
- Ability 的效果目标和速度参考目标不一定是同一个对象。
- 自我 Buff 的效果目标是自己，但它是否能在敌人前生效，未来可能应该比较角色与敌人的速度，而不是自己与自己比较。

第一版采用方案：

- 当前先采用方案 D：
  - 第一版暂时只让 Attack FreeAction 参与高速 / 低速排序。
  - Ability FreeAction 暂时保持现状，默认按当前规则处理。
  - 等 UI 原型和 Ability 时序需求更清楚后，再单独设计 Ability 的速度参考规则。
- 当前不引入 `speedReferenceTarget / timingTarget`。
- 当前不修改 `CreateSpeedBasedExecutionPlan(...)`。

采用该方案的原因：

- 不破坏已经通过的 Ability FreeAction 测试。
- 不提前引入多敌人时“Ability 和谁比速度”的问题。
- 不为 self Buff 过早增加新字段。
- 可以先继续推进 Attack FreeAction / 偷刀。

后续路线：

- 下一步实现 `ResolveFreeAction(...)` 的 Attack 分支。
- 先只直接测试 Resolver。
- 再让 Executor 接入 Attack FreeAction。
- 再补高速 / 低速 Attack FreeAction 混合执行测试。
- Ability FreeAction 的速度参考规则等 UI 交互和战斗时序更清楚后再设计。

## 五十五、ResolveFreeAction Attack 第一版直接测试通过

当前完成内容：

- `BattleResolver.ResolveFreeAction(...)` 已新增 `Attack FreeAction` 分支。
- `ResolveFreeAction(...)` 当前会分派到：
  - `ResolveFreeAbilityAction(...)`
  - `ResolveFreeAttackAction(...)`
- 本次没有修改 `BattleExecutionPlanExecutor.cs`。
- 本次没有接入 Executor。
- 本次没有修改速度排序。
- 本次没有修改 JSON。

Attack FreeAction 当前规则：

- 玩家使用攻击卡直接攻击 `actionSlot.target`。
- 不进入拼点。
- 不读取敌人卡牌。
- 不比较敌人点数。
- 不触发 `ClashWin / ClashLose`。
- roll 玩家攻击点数。
- 使用 `BattleCalculator.GetFinalDamageScaled(...)`。
- 使用 `BattleCalculator.ConvertScaledDamageToHPDamage(...)`。
- 对 `actionSlot.target` 造成伤害。
- 触发 `BeforeUse / Resolved / Hit`。
- 通过 `ApplyDamageAndTriggerEvents(...)` 触发 `AfterDamage / AfterKill`。
- 不手动修改 UseCount / guiltGain，仍由 `Resolved` 事件链处理。

`BattleResolveResult` 返回结果：

- `resultType = FreeAttack`
- `isSuccess = true`
- `shouldCompleteItem = true`
- `playerCardUsed = true`
- `enemyCardUsed = false`
- `hasDamage = true`
- `damage = 20`
- `damagedCharacter = enemy`
- `playerPoint = 10`
- `enemyPoint = 0`
- `clashAttemptCount = 0`
- `triggeredEventChain = true`

已通过测试：

- 新增测试模式：
  - `BattleResolverResolveFreeAttackBasic`
- 测试直接调用：
  - `BattleResolver.ResolveFreeAction(actionSlot)`
- 不生成 ExecutionPlan。
- 不调用 Executor。
- 测试结果：
  - 敌人 HP：`999 → 979`
  - allyA HP：`30 → 30`
  - Attack UseCount：`0 / 3 → 1 / 3`
  - allyA 负罪感：`0 → 2`
  - `actionSlot.isUsed` 保持 `False`
  - 所有预期判断均为 `True`

阶段意义：

- `FreeAction` 的 Resolver 正式入口已经同时支持 Ability 与 Attack。
- Attack FreeAction / 偷刀已经在 Resolver 层跑通。
- 伤害、事件链、UseCount、负罪感仍保持由 Resolver / BattleEventProcessor / BattleCardManager 分工处理。
- `MarkUsed()` 仍然留给 Executor 未来根据 `playerCardUsed` 处理。

当前仍未处理：

- Executor 接入 Attack FreeAction。
- Attack FreeAction 成功后由 Executor 调用 `slot.MarkUsed()`。
- 高速 / 低速 Attack FreeAction 混合执行测试。
- 防御 / 闪避。
- 回合结束清理槽位。
- UI 状态刷新。

## 五十六、FreeAction Attack 第一版 Executor 接入通过

当前完成内容：

- `Attack FreeAction / 偷刀` 已通过 `BattleExecutionPlanExecutor` 正式执行测试。
- 测试模式：
  - `ActionSlotExecutionPlanExecuteFreeAttackBasic`
- 测试生成只包含 `Attack FreeAction` 的 `BattleExecutionPlan`。
- Executor 调用：
  - `BattleResolver.ResolveFreeAction(item.actionSlot)`
- Resolver 进入：
  - `ResolveFreeAttackAction(...)`
- Executor 根据 `BattleResolveResult` 标记：
  - `item.isCompleted = true`
  - `item.actionSlot.MarkUsed()`

测试结果：

- `resultType = FreeAttack`
- `isSuccess = true`
- `shouldCompleteItem = true`
- `playerCardUsed = true`
- `enemyCardUsed = false`
- `hasDamage = true`
- `damage = 20`
- `triggeredEventChain = true`
- 敌人 HP：`999 → 979`
- allyA HP：`30 → 30`
- Attack UseCount：`0 / 3 → 1 / 3`
- allyA 负罪感：`0 → 2`
- `actionSlot.isUsed：False → True`
- `ExecutionPlan.isCompleted = true`

当前规则确认：

- Attack FreeAction 不进入拼点。
- 不读取敌人卡。
- 不比较敌人点数。
- 不触发 `ClashWin / ClashLose`。
- 触发 `BeforeUse / Resolved / Hit / AfterDamage / AfterKill`。
- UseCount / guiltGain 仍通过 `Resolved` 事件链处理。
- `MarkUsed()` 仍由 Executor 根据 `playerCardUsed` 处理。

阶段意义：

- FreeAction 当前已经支持 Ability 与 Attack 两条第一版执行路径。
- Ability FreeAction 已完成 Resolver 直接测试与 Executor 接入测试。
- Attack FreeAction 已完成 Resolver 直接测试与 Executor 接入测试。
- 这一步为后续“偷刀”和简易 UI 战斗操作打下基础。

注意事项：

- 当前 `PrintExecutionPlan(...)` 和 `PrintExecutionPlanStepPreview(...)` 中的 FreeAction 文案仍可能显示“暂未实现”或“未来将处理普通行动 / 偷刀”。
- 这是旧提示文案，不影响当前正式执行逻辑。
- 后续可单独小范围更新打印文案。

当前仍未处理：

- 高速 / 低速 Attack FreeAction 与敌人意图混合执行测试。
- Ability FreeAction 的速度参考规则。
- 防御 / 闪避。
- 回合结束清理槽位。
- UI 状态刷新。
- 多目标 / 群体目标。
- 动画表现。

## 五十七、高速 Attack FreeAction 混合执行测试通过

当前完成内容：

- 新增测试模式：
  - `ActionSlotExecutionPlanExecuteHighSpeedFreeAttackMixedBasic`
- 该测试验证：
  - 高速 `Attack FreeAction / 偷刀`
  - 与无人响应敌人意图混合执行时
  - 高速偷刀会排在敌人意图前执行

测试条件：

- allyA 当前速度：20
- enemy 当前速度：8
- allyA 速度大于 enemy
- allyA 槽位 1 安排 `Attack FreeAction`
- target = enemy
- 敌人意图 1 攻击 allyB 槽位 1
- 敌人意图无人响应

执行计划顺序：

- 第 1 项：`FreeAction`
- 第 2 项：`UnrespondedEnemyIntent`
- 该顺序符合第一版规则：
  - 速度高于敌人时，可以在敌人攻击前偷刀。

实际执行结果：

- `FreeAction` 先执行：
  - Resolver 返回 `FreeAttack`
  - enemy HP：`999 → 979`
  - allyA Attack UseCount：`0 / 3 → 1 / 3`
  - allyA 负罪感：`0 → 2`
  - allyA actionSlot.isUsed：`False → True`
- `UnrespondedEnemyIntent` 后执行：
  - allyB HP：`30 → 23`
  - 敌人意图完成
- 最终：
  - `ExecutionPlan.isCompleted = true`
  - 所有 item 均完成
  - 所有预期判断均为 `True`

阶段意义：

- 验证了 `CreateSpeedBasedExecutionPlan(...)` 对高速 Attack FreeAction 的排序有效。
- 验证了 FreeAction 与敌人意图可以混合执行。
- 验证了“高速偷刀先于敌人攻击”这条核心战斗规则。
- 这一步为后续简易 UI 中“玩家选择不响应敌人，改为高速偷刀”打下基础。

注意事项：

- 当前 `PrintExecutionPlan(...)` / `PrintExecutionPlanStepPreview(...)` 中 FreeAction 仍有旧文案，例如“暂未实现”或“未来将处理普通行动 / 偷刀”。
- 这是打印提示未更新，不影响正式执行逻辑。
- 后续可单独小范围修正文案。

当前仍未处理：

- 低速 Attack FreeAction 混合执行测试。
- Ability FreeAction 的速度参考规则。
- 防御 / 闪避。
- 回合结束清理槽位。
- UI 状态刷新。
- 多目标 / 群体目标。
- 动画表现。

## 五十八、低速 Attack FreeAction 混合执行测试通过

当前完成内容：

- 新增测试模式：
  - `ActionSlotExecutionPlanExecuteLowSpeedFreeAttackMixedBasic`
- 该测试验证：
  - 低速 `Attack FreeAction / 偷刀`
  - 与无人响应敌人意图混合执行时
  - 低速偷刀会排在敌人意图后执行

测试条件：

- allyA 当前速度：3
- enemy 当前速度：8
- allyA 速度低于 enemy
- allyA 槽位 1 安排 `Attack FreeAction`
- target = enemy
- 敌人意图 1 攻击 allyB 槽位 1
- 敌人意图无人响应

执行计划顺序：

- 第 1 项：`UnrespondedEnemyIntent`
- 第 2 项：`FreeAction`
- 该顺序符合第一版规则：
  - 速度低于敌人时，偷刀不能抢在敌人攻击前。

实际执行结果：

- `UnrespondedEnemyIntent` 先执行：
  - allyB HP：`30 → 24`
  - 敌人意图完成
- `FreeAction` 后执行：
  - Resolver 返回 `FreeAttack`
  - enemy HP：`999 → 979`
  - allyA Attack UseCount：`0 / 3 → 1 / 3`
  - allyA 负罪感：`0 → 2`
  - allyA actionSlot.isUsed：`False → True`
- 最终：
  - `ExecutionPlan.isCompleted = true`
  - 所有 item 均完成
  - 所有预期判断均为 `True`

阶段意义：

- 验证了 `CreateSpeedBasedExecutionPlan(...)` 对低速 Attack FreeAction 的排序有效。
- 与高速测试形成对照：
  - 高速偷刀：`FreeAction → UnrespondedEnemyIntent`
  - 低速偷刀：`UnrespondedEnemyIntent → FreeAction`
- 这一步确认了“想在敌人攻击前偷刀，必须速度大于敌人”的第一版核心规则。

注意事项：

- 当前 `PrintExecutionPlan(...)` / `PrintExecutionPlanStepPreview(...)` 中 FreeAction 仍有旧文案，例如“暂未实现”或“未来将处理普通行动 / 偷刀”。
- 这是打印提示未更新，不影响正式执行逻辑。
- 后续可单独小范围修正文案。

当前仍未处理：

- Ability FreeAction 的速度参考规则。
- 防御 / 闪避。
- 回合结束清理槽位。
- UI 状态刷新。
- 多目标 / 群体目标。
- 动画表现。

## 五十九、FreeAction 打印文案清理完成

当前完成内容：

- 修正了 FreeAction 相关打印文案。
- 修改文件：
  - `BattleExecutionPlanManager.cs`
  - `BattleExecutionPlanExecutor.cs`
- 当前 `PrintExecutionPlan(...)` 中 FreeAction 不再显示“当前暂未实现打印细节”。
- 当前 `PrintExecutionPlanStepPreview(...)` 中 FreeAction 不再显示“未来将处理普通行动 / 偷刀，但当前暂未实现正式处理”。

新文案大意：

- `PrintExecutionPlan(...)`：
  - `FreeAction：玩家自由行动，执行时将交给 BattleResolver.ResolveFreeAction(...) 处理`
  - 会安全打印行动者、槽位、卡牌、目标。
- `PrintExecutionPlanStepPreview(...)`：
  - `FreeAction：执行时将调用 BattleResolver.ResolveFreeAction(...)，当前支持 Ability FreeAction 与 Attack FreeAction`
  - 会安全打印行动者、槽位、卡牌、目标。
  - 明确说明当前只是预览，不执行 item，不修改状态。

未修改内容：

- 没有修改 `CreateSpeedBasedExecutionPlan(...)` 排序逻辑。
- 没有修改 `ExecuteFreeAction(...)`。
- 没有修改 `ResolveFreeAction(...)`。
- 没有修改伤害、拼点、事件链、MarkUsed、UseCount、guiltGain。
- 没有修改 JSON。
- 没有修改测试入口。

阶段意义：

- 当前 FreeAction 已经不是未实现状态。
- Ability FreeAction 与 Attack FreeAction 都已经进入正式 Resolver / Executor 执行路径。
- 修正文案后，后续查看 ExecutionPlan 打印和步骤预览时不会被旧提示误导。
- 这一步属于调试体验清理，不改变战斗规则。

基础编译检查：

- 普通 sandbox 因 Windows SDK 目录权限失败。
- 提升权限后 `dotnet build ProjectGuilt.sln` 通过。
- 结果：`0 warnings / 0 errors`。
