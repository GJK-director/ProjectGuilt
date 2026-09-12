# StoryPanel Inspector 接口使用说明

Status: CURRENT  
Last Verified: 2026-09-12

本文说明 [StoryPanel.prefab](StoryPanel.prefab) 上 `StoryPanelView` 组件的 Inspector 接口。适用于程序员调试、剧情背景素材配置和常规 UI 换皮。

相关实现与数据：

- [StoryPanelView.cs](../../Scripts/Story/UGUI/StoryPanelView.cs)
- [prologue_501.json](../../Resources/Story/prologue_501.json)
- [Story System](../../ProjectDocs/Developer/Story/StorySystem.md)
- [Story 独立模块说明](../../Scripts/Story/README_Standalone.md)

## 1. 编辑前须知

1. 从 Project 窗口打开 `Assets/Prefabs/Story/StoryPanel.prefab`，优先在 Prefab Mode 中修改。
2. `StoryPanel` 根对象同时承载 Canvas、`StoryPanelView` 和 `StorySceneFacade`，必须保持 Active。
3. 面板的运行时显隐由 `StoryPanelView` 控制内部 `StoryPresentationRoot`。不要通过关闭 Prefab 根对象隐藏剧情。
4. 不要 Unpack 场景中的 StoryPanel，也不要在 `NewGameText`、`StorySample` 实例上重复覆盖字体、背景或素材字段。正常 Prefab 实例会自动继承这里的修改。
5. 当前文字组件是 UGUI `Text`，`Typography > Font` 必须使用 Unity `Font`（例如 `SIMHEI.TTF`），不能拖入 TMP Font Asset。
6. 编辑模式下保存的配置会在下一次 Play 时应用；Play Mode 中修改 Typography 或 Replaceable UI Assets 会立即重应用，但 Play Mode 修改通常不会自动保存。
7. Play Mode 中修改当前背景的 Layers 后，需要重新触发该 `backgroundId`，例如重新播放剧情或再次进入对应 ChangeBackground 节点。

## 2. Inspector 分组总览

| 分组 | 用途 | 常规是否修改 |
|---|---|---|
| Facade | 绑定 Story 对外入口 | 否 |
| Scene Roots | 绑定剧情、UI、历史、选项和结束面板根节点 | 否 |
| Background And Dialogue | 绑定背景底板和主要文本 | 通常不改引用 |
| Background Assets | 配置 `backgroundId` 与任意数量背景图层 | 是 |
| Programmer Debug - Typography | 统一更换字体和常用字号 | 是 |
| Programmer Debug - Replaceable UI Assets | 集中更换面板 Sprite、Material 和颜色 | 是 |
| Portraits And Choices | 绑定左右立绘占位和选项槽 | 增加槽位时修改 |
| Playback Buttons | 绑定自动、快进、历史等操作按钮 | 通常不改 |
| Dialogue And Overlay Buttons | 绑定点击推进、历史关闭和结束重播按钮 | 通常不改 |

## 3. Facade

### Story Facade

必须指向同一 Prefab 根对象上的 `StorySceneFacade`。

正常情况下不要替换。宿主只通过 Facade 调用 `OpenStoryPanel(storyId)`、`CloseStoryPanel()` 等接口；`StoryPanelView` 通过它订阅剧情开始、结束和错误事件。

如果该引用为空，组件会在 `Awake` 尝试从自身获取 Facade，但正式 Prefab 仍应保留显式绑定。

## 4. Scene Roots

| 字段 | 当前职责 | 注意事项 |
|---|---|---|
| Story Root | 完整剧情表现根 `StoryPresentationRoot` | 由程序开关；不要绑定 Prefab 最外层 Canvas 根 |
| Story Ui Root | 对话框、立绘、选项和操作按钮的共同 UI 根 | 隐藏 UI 时背景仍可保留 |
| Overlay Root | 历史记录遮罩根 | 由历史按钮打开/关闭 |
| Choice Panel | 选项容器 | 没有选项时自动隐藏 |
| End Panel | 剧情结束面板 | 开始新剧情时自动隐藏，结束时显示 |

这些字段是运行时引用，不是素材槽。删除或重命名层级对象后，必须重新绑定相应字段。

## 5. Background And Dialogue

| 字段 | 用途 |
|---|---|
| Background Image | 背景图层池的第 0 层；额外层由运行时按需创建 |
| Background Label | 找不到 `backgroundId` 或没有可渲染 Sprite 时显示的占位提示 |
| Speaker Text | 当前说话人名称 |
| Dialogue Text | 逐字显示的对白正文 |
| Continue Text | “点击继续”提示 |
| Status Text | 等待、运行、结束和错误状态提示 |

不要把 `Background Image` 换成场景外对象。所有动态背景层都会创建在它的父节点下，并保持在剧情 UI 后方。

## 6. Background Assets：背景与多图层

`Background Assets` 是 `Background Binding` 列表。每个 Element 对应一个剧情背景状态。

### 6.1 Background Binding

| 字段 | 用途 |
|---|---|
| Background Id | 与剧情 JSON 的 `background.backgroundId` 精确对应，查找时不区分大小写 |
| Layers | 该背景包含的图层列表，数量不限 |

Layers 按 Element 顺序从后向前绘制：`Element 0` 是最底层，最后一个 Element 是最前层。Sprite 为空的 Element 会被跳过，不占用运行时图层。

同一份 StoryPanel 中不要配置重复的 Background Id，否则只会命中列表中靠前的配置。

### 6.2 Background Layer

| 字段 | 用途 |
|---|---|
| Layer Name | 仅供 Inspector 辨认，不参与运行时逻辑 |
| Sprite | 当前层图片；为空时跳过 |
| Anchor Min / Anchor Max | RectTransform 锚点范围，使用 0–1 的父级归一化坐标 |
| Pivot | 缩放、定位使用的轴心 |
| Anchored Position | 相对锚点的位置偏移 |
| Size Delta | 相对锚点区域的尺寸增量；中心锚点模式下就是显示宽高 |
| Tint | 图片乘色；Alpha 同时控制该层基础透明度 |
| Preserve Aspect | 是否保持 Sprite 原始宽高比 |

切换背景时，当前所有可见层会一起淡出，新背景所有层再一起淡入。每层 Tint 的 Alpha 会被保留并乘上淡入淡出进度。

### 6.3 常用 RectTransform 配置

全屏底图：

```text
Anchor Min        (0, 0)
Anchor Max        (1, 1)
Pivot             (0.5, 0.5)
Anchored Position (0, 0)
Size Delta        (0, 0)
Preserve Aspect   On
```

居中差分图，例如手机、人物手部或前景物件：

```text
Anchor Min        (0.5, 0.5)
Anchor Max        (0.5, 0.5)
Pivot             (0.5, 0.5)
Anchored Position 按画面调整
Size Delta        填写期望显示宽高，例如 (680, 520)
Preserve Aspect   On
```

全屏半透明气氛层：使用全屏底图参数，并把 Tint Alpha 调到 `0–1` 之间的合适值。

### 6.4 新增多图层背景示例

假设需要“房间 + 手机 + 手”的三层叠加：

```text
Background Id: room_phone_hand

Layers / Element 0
  Layer Name: Room Base
  Sprite: room.png
  使用全屏底图参数

Layers / Element 1
  Layer Name: Phone
  Sprite: phone.png
  使用中心锚点和独立 Size Delta

Layers / Element 2
  Layer Name: Hand Foreground
  Sprite: hand.png
  使用中心锚点和独立 Size Delta
```

剧情 JSON 中使用相同 ID：

```json
{
  "nodeType": "ChangeBackground",
  "background": {
    "backgroundId": "room_phone_hand",
    "fadeSeconds": 0.35
  }
}
```

仅修改 Inspector 不会让剧情自动使用新背景，必须在 Story JSON 的 ChangeBackground 节点中引用该 ID。

## 7. Programmer Debug - Typography

Typography 用于集中修改 StoryPanel 下的 UGUI 字体和常用字号。

| 字段 | 影响对象 |
|---|---|
| Font | StoryPanel 下所有 UGUI `Text`；为空时保留各 Text 原字体 |
| Speaker Font Size | `Speaker Text` |
| Dialogue Font Size | `Dialogue Text` |
| Choice Font Size | `Choice Bindings` 中每个 Label |
| Portrait Font Size | 左右 `Portrait Binding` 的 Label |
| Control Font Size | Continue、自动、快进、历史、跳到结尾、关闭历史、重新开始等控件文字 |
| History Font Size | 历史正文 `History Text` |
| End Font Size | 剧情结束正文 `End Text` |
| Status Font Size | `Status Text` |
| Background Label Font Size | 背景缺失占位文字 |

Font 会统一作用于全部子级 UGUI Text，包括静态标题；上表的字号字段只覆盖对应运行时槽位。未列出的静态标题继续使用其 Text 组件自身的字号。

### 更换字体的推荐步骤

1. 确保字体文件以 Unity `Font` 导入。
2. 把字体拖到 Typography > Font。
3. 调整 Dialogue、Speaker、Choice 等字号。
4. 保存 Prefab，退出 Play Mode 后重新进入 `NewGameText`。
5. 检查中文、数字、英文、标点和换行是否完整显示。

如果字体框拒绝拖入资源，通常是误用了 `TMP_FontAsset`。当前 StoryPanel 尚未使用 TextMesh Pro。

## 8. Programmer Debug - Replaceable UI Assets

该列表把常用 UI Image 集中到 StoryPanelView Inspector，避免逐层查找。当前预置槽位包括：

- Dialogue Panel
- Choice Panel
- History Overlay
- History Window
- End Panel
- Left Portrait Panel
- Right Portrait Panel

可以继续增加 Element，并把其它 Prefab 内的 `Image` 拖入 Target。

### Image Asset Binding

| 字段 | 用途 |
|---|---|
| Slot Name | 仅供 Inspector 辨认 |
| Target | 要应用设置的 UGUI `Image` |
| Sprite | 要显示的图片或九宫格 Sprite |
| Material | UI 使用的 Material；通常为空表示默认材质 |
| Color | 图片乘色；Alpha 为整体透明度 |
| Image Type | `Simple`、`Sliced`、`Tiled` 或 `Filled` |
| Preserve Aspect | 是否保持 Sprite 宽高比 |

注意事项：

- 九宫格面板应在 Sprite Editor 中正确配置 Border，并使用 `Sliced`。
- 普通整图通常使用 `Simple`；需要填充进度的图片才使用 `Filled`。
- Color 会与 Sprite 颜色相乘。素材意外变暗时先检查 Color 是否为白色、Alpha 是否为 1。
- 同一个 Target 不要在列表中重复配置，靠后的重复项会覆盖靠前项。
- 左右 Portrait Panel 的 Sprite 和 Material 可以在这里替换，但运行时会根据当前发言人切换 Panel 颜色；不要依赖这里锁定立绘框最终颜色。
- Target 只能引用 StoryPanel Prefab 内的对象，不能引用某个场景中的临时实例。

## 9. Portraits And Choices

### 9.1 Left / Right Portrait

每侧包含：

| 字段 | 用途 |
|---|---|
| Root | 整个立绘占位根，用于显示、隐藏和缩放 |
| Panel | 立绘面板 Image，用于当前发言人高亮 |
| Label | 角色名、表情和占位信息 |

Story 数据的 `positionId` 为 `right` 时使用 Right Portrait，其它值默认走 Left Portrait。当前内置 View 显示的是文本占位面板；如果需要正式角色立绘映射，应先确认新的素材绑定契约，不要只把角色 Sprite 写进 JSON 的未知字段。

### 9.2 Choice Bindings

Choice Bindings 的 Element 顺序对应选项显示顺序。每项包含：

| 字段 | 用途 |
|---|---|
| Root | 该选项槽的显示根 |
| Button | 点击事件和 interactable 状态 |
| Label | 选项文字 |

`optionId` 是运行时临时值，不会出现在 Inspector 中。

当前 Prefab 预置 3 个选项槽。剧情选项数超过已绑定槽位数时，多出的选项无法显示，Status Text 会报告数量不匹配。增加槽位时需要复制完整选项对象，并在 Choice Bindings 中同时绑定 Root、Button 和 Label。

## 10. Playback Buttons

| 字段 | 功能 |
|---|---|
| Auto Button | 开关自动播放 |
| Auto Button Text | 显示“自动”或“自动：开” |
| Skip Button | 开关快进 |
| Skip Button Text | 显示“快进”或“快进：开” |
| History Button | 打开历史记录 |
| Skip To End Button | 直接前往剧情定义的 Skip Node |

按钮监听由 `StoryPanelView.Awake` 自动注册。不要再在 Button 的 On Click 列表中重复绑定同一调用，否则一次点击可能执行两次。

## 11. Dialogue And Overlay Buttons

| 字段 | 功能 |
|---|---|
| Advance Button | 点击对话区域时补全文字或推进下一节点 |
| History Text | 历史记录正文 |
| Close History Button | 关闭历史记录遮罩 |
| End Text | 剧情结束提示与结束 Story ID |
| Restart Button | 重新播放最近结束的剧情 |

Advance Button 通常覆盖整个 Dialogue Panel。调整布局或替换 Target Graphic 时，必须保留 Button 组件和射线可点击区域。

## 12. 推荐验证流程

1. 退出 Play Mode。
2. 打开 StoryPanel Prefab Mode，完成配置并保存。
3. 确认 Prefab 根对象保持 Active，Console 没有 Missing Script。
4. 如修改了序章背景绑定，运行菜单 `ProjectGuilt > Story > Validate Prologue 501`。Validate 会检查工具要求的基础素材层，同时允许在其后增加自定义图层。
5. 不要为了验证顺手运行 `Rebuild Prologue 501 Scene`；Rebuild 会按工具内置清单重写 Prefab、背景 Layers 和场景，可能覆盖手工增加的图层，只能在明确需要重建时使用。
6. 打开 `Assets/Scenes/Menu.unity` 并进入 Play Mode。
7. 点击“新游戏”，确认进入 `NewGameText` 后对白 UI 立即出现。
8. 序章第一段使用黑色背景是剧情设计；黑底上仍必须看见对白 UI，不能只有无界面的纯黑屏。
9. 推进到所有改动过的 Background Id，检查层序、锚点、裁切、透明度和淡入淡出。
10. 检查自动、快进、历史、跳到结尾、重新开始和选项点击。

## 13. 常见问题

### 点击新游戏后只有纯黑屏

- 检查 StoryPanel Prefab 根是否 Active。
- 检查 `NewGameText` 的 StoryPanel 实例是否存在 `m_IsActive` override；如有应 Revert 该 override。
- 检查 IntroStoryHost 的 Story Facade 是否指向当前实例。
- 第一段黑色背景是正常的，但 Dialogue Panel 和文字必须可见。

### 出现“背景占位”文字

- JSON 的 Background Id 与 Inspector 不一致。
- 对应 Background Binding 不存在。
- 对应 Layers 全部为空，或所有 Sprite 都未成功导入。

### 前景图完全看不见

- Sprite 为空。
- Tint Alpha 为 0。
- 使用中心锚点时 Size Delta 仍为 `(0, 0)`。
- 图层顺序错误，被后面的不透明层遮住。
- 图片导入类型不是 Sprite。

### 图片变形、留黑边或尺寸不对

- 全屏图先检查 Anchor Min/Max 是否为 `(0, 0)` / `(1, 1)`。
- 居中差分图先检查 Anchor Min/Max 是否都为 `(0.5, 0.5)`，并填写 Size Delta。
- 根据素材需求切换 Preserve Aspect。
- 检查原图透明边距是否过大。

### 修改字体后中文变方块

- 确认使用的是包含所需中文字形的 Unity Font。
- 不要把 TMP Font Asset 当作 UGUI Font 使用。
- 检查目标 Text 是否仍在 StoryPanel 根层级下。

### UI 素材替换后颜色不对

- Image 的 Color 会乘到 Sprite 上，先恢复白色和 Alpha 1 排查。
- 检查 Material 的 Shader 是否支持 UGUI 和透明混合。
- 九宫格素材检查 Border 与 Image Type 是否匹配。

### Inspector 修改没有生效

- 确认改的是 Prefab Asset，而不是带 override 的场景实例。
- 编辑模式修改后保存 Prefab，并重新进入 Play Mode。
- 背景 Layers 修改后重新触发对应 ChangeBackground 节点。
- 检查 Console 是否存在编译错误；有编译错误时新脚本逻辑不会加载。

## 14. 提交前检查清单

- StoryPanel Prefab 根保持 Active。
- 没有 Missing Script、Missing Object 或丢失的 Button/Text/Image 引用。
- 新 Background Id 与 Story JSON 完全对应。
- 每个背景至少有一个非空 Sprite 图层。
- 多图层顺序正确，透明图在需要的不透明底图之前或之后正确排列。
- 字体是 UGUI Font，中文和标点显示正常。
- 场景实例没有覆盖 Typography、Background Assets 或 Replaceable UI Assets。
- 已从 Menu 点击“新游戏”完成一次完整人工验证。
- 未经明确授权，不运行 Rebuild、不 stage、不 commit、不 push。
