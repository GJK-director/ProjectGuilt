# Test Architecture

Status: CURRENT
Role: CURRENT TEST ARCHITECTURE
Last Verified: 2026-09-11

## Layers

- Suites：按系统拥有 Cases；one system → one Suite，one condition → one Case。
- Shared：复用 Production fixture、synthetic factories 和 protocol test double。
- Harness：真实 Unity 环境下的人工/集成入口；目录外 Scene-bound support 也参与验证。
- Legacy：现存 runner、回归与 compatibility aggregation；存在不代表每个行为都是当前玩法。

路径看 [CodeMap](../Developer/CodeMap.md#test-and-support-locations)，数量只由 [Testing README](README.md) 维护，覆盖/caller 由 [RegressionTestMap](RegressionTestMap.md) 维护。

## Case Ownership and Running

目前 Case 使用 public static bool，由 compatibility caller/runner 聚合，不是 [Test] 自动发现。
EnemyIntent、Cards、Execution、Bootstrap 有实现；其他 Suite 目录是 placeholder。
Shared 有 BattleTestContext、BattleScenarioBuilder、三类 Factory 和 BattleConsolePresenter；Assertions 仍是 placeholder。没有统一 TestRunner/TestResult/TestAssertion API。
测试与 Battle 在默认 Assembly-CSharp，internal seams 可被直接访问；不隐含批准新增 asmdef。

## Migration Boundary

Formal Case ownership、standalone Mode entry、compatibility wrapper 是三个独立生命周期。
已退休 standalone 的 wrapper 仍可被其他 active caller 调用。不要恢复 one Mode → one regression 模式。
JIT 顺序：Formal coverage → RegressionTestMap → Legacy inventory → 必要时 Historical triage。只迁移相关、仍有效的契约，再改 Production。
人工环境看 [ManualHarnesses](ManualHarnesses.md)，写法看 [TestWritingGuide](TestWritingGuide.md)。代码存在不等于本次已运行通过。
