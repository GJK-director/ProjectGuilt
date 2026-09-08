# Presentation Documentation

Status: TRANSITIONAL
Last Verified: 2026-09-08
Repository Basis: 当前本地 HEAD (`ce43786241b06f41deb439c0729d151b86c20c27`)

## Responsibilities

记录 Battle Presentation、Camera、Animation、Effect、Turn Transition 的导航。

## Does Not Own

不拥有 Combat 数学、CardUsed 规则或正式数据定义。

## Current Main Files

见 `Camera.md` 与 `BattlePresentation.md`。

## Runtime Flow

Execution Runner 通过 Presentation Protocol 进入 Scene Presenter，再由 Interaction 路由到具体 Player/Camera。

## Data Sources

Presentation Profile ScriptableObject、Scene/Prefab 序列化引用。

## Related Tests

Modes 83–85、95–98、102–104 及 Presentation 文件中的 Tests。

## Known Technical Debt

Presentation、Camera、UI 绑定跨越代码、Prefab 和 Scene，不能仅靠文本引用判断完整用途。

## Migration Status

TRANSITIONAL；未物理迁移。
