# Execution Suite

Status: TRANSITIONAL
Last Verified: 2026-09-09
Repository Basis: 当前本地 HEAD (`bcdd67f63e9579851edfcee37d7fa6fb2a41b9dd`)

目标范围：ExecutionPlan、Item、Priority、Runner、Pause/Resume 和 ActionFinished。

Batch 5C 已建立 `FirstStrikeExecutionTests.cs` Formal Suite，包含 13 个 Case，覆盖 FreeAction priority、Unresponded Enemy priority、Responded priority、FirstStrike interaction preservation、FirstStrike sorting 和 Responded pairing preservation。该 Suite 只验证 Execution 计划，不执行 Combat；Mode89 仍保留为 Legacy compatibility runner。
