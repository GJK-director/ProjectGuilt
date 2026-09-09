# Bootstrap Suite

Status: TRANSITIONAL
Last Verified: 2026-09-09
Repository Basis: 当前本地 HEAD (`3d75eef67b8dc9d1c90ac09d613e634d9d240441`)

目标范围：Definition Loader、Factory、RuntimeState、Deck preset 和 Scene Bootstrap。

Batch 5D 已建立 `DeckPresetBootstrapTests.cs` Formal Suite，包含 11 个 Case，覆盖 UnitFactory explicit card IDs、Deck preset Runtime bootstrap、preset isolation、initial resource state、JSON-independent Shooting Bullet bootstrap、definition immutability、card instance identity、runtime card order 和 legacy no-preset bootstrap compatibility。Cards Manifest 本身由现有 Cards Suite 负责。

Phase6B 后，Mode109 standalone entry 已退休；Mode114 仍通过 retained `BattleDeckManifestTests` historical compatibility wrapper 消费 Cards Manifest regression。Bootstrap Formal ownership 不变。
