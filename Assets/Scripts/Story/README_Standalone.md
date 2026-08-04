# ProjectGuilt Story 独立模块

## 独立性边界

`Assets/Scripts/Story` 是一个可单独导出的 Unity 剧情模块。

- 独立程序集：`ProjectGuilt.Story`
- 内置 UGUI 程序集：`ProjectGuilt.Story.UGUI`
- 不引用 ProjectGuilt 的战斗、卡牌、角色、场景或 UI 代码
- 核心程序集不引用 TMPro、UGUI、Input System 或 Timeline
- 内置 `StoryPanelView` 只额外引用 Unity 官方 `UnityEngine.UI`
- 普通游戏流程只依赖 `IStorySceneService`
- 组装层按需依赖 `IStorySceneConfigurator`
- 默认面板可直接使用；UI、剧情来源和历史时间也仍可通过接口替换
- Story 不提供存档、读档或本地文件持久化，相关能力由宿主项目统一负责

`ProjectGuilt.Story` 继续只显式开放 `Newtonsoft.Json.dll`；`ProjectGuilt.Story.UGUI` 单向引用核心程序集与 `UnityEngine.UI`。两个程序集都不会反向引用默认的 `Assembly-CSharp`，因此宿主类型不会渗入 Story。

## 外部依赖

该模块需要 Unity 引擎、Unity 官方维护的 Newtonsoft JSON 包，以及内置面板使用的 UGUI 包。

本项目验证版本为：

```text
Unity 6000.3.7f1
com.unity.nuget.newtonsoft-json 3.2.2
com.unity.ugui 2.0.0
Newtonsoft.Json 13.0.2
```

其它 Unity 版本需要由接入项目自行做兼容性验证。

## Newtonsoft JSON 序列化约定

- `StoryJsonSerializer` 统一调用 `JsonConvert.DeserializeObject` / `JsonConvert.SerializeObject`。
- 剧情定义直接使用公开运行时 DTO，不再维护额外的 JSON 传输 DTO 映射层。
- `StringEnumConverter` 让枚举继续使用 `Int`、`Set`、`All` 这类字符串；整数枚举和无效枚举会在加载阶段返回明确错误。
- 自定义节点的 `parameters` 直接使用 JSON 对象，并反序列化为 `Dictionary<string, string>`。
- 未知字段继续忽略；剧情定义仍会经过入口、节点唯一性和路由静态校验。
- 每条剧情使用一个完整 JSON 文件，同时包含 `storyId`、入口、收尾和 `nodes`。

## 推荐接入方式：宿主只开关面板

1. 把 `Assets/Scripts/Story/Prefabs/StoryPanel.prefab` 放入宿主场景。
2. 宿主脚本只保存预制体上的 `StorySceneFacade` 引用。
3. 播放时调用 `OpenStoryPanel(storyId)`。
4. 需要中断、切场景或主动收回界面时调用 `CloseStoryPanel()`。

宿主不需要复制、继承或重写 `StoryPanelView`，也不需要逐个绑定对白、选项、自动、快进、历史和结束按钮：

```csharp
using ProjectGuilt.Story;
using UnityEngine;

public sealed class HostGameFlow : MonoBehaviour
{
    [SerializeField] private StorySceneFacade storyFacade;

    private IStorySceneService storyService;

    private void Awake()
    {
        storyService = storyFacade;
        storyService.StoryEnded += HandleStoryEnded;
    }

    public void StartStory(string storyId)
    {
        storyService.OpenStoryPanel(storyId);
    }

    public void StopStory()
    {
        storyService.CloseStoryPanel();
    }

    private void HandleStoryEnded(string storyId)
    {
        // 由宿主项目决定进入战斗、主菜单或下一个流程。
    }
}
```

`StoryPanel.prefab` 的根 Canvas 保持激活，但真正显示内容的 `StoryPresentationRoot` 初始隐藏。调用 `OpenStoryPanel` 后，Story 会自行读取 JSON、初始化状态并显示面板；调用 `CloseStoryPanel` 后会清理当前会话并隐藏面板。

宿主项目不应直接持有 `StoryFlowController`、`StoryRuntimeState`、节点执行器或按钮引用。

## 可替换的适配层

默认面板可以直接运行。只有存在特殊需求时才替换下列适配层：

- `IStoryContentProvider`：剧情 JSON 来源，例如 Addressables、服务器或自定义数据库。
- `IStoryView`：完全自定义 UI、测试替身或其它渲染框架。常规接入不需要实现。
- `IStoryClock`：历史记录时间来源。
- `IStoryNodeHandler`：跨系统节点，例如 StartBattle、Timeline 或语音。

这些依赖通过 `IStorySceneConfigurator` 在宿主项目的组装层注入。业务流程仍然只依赖 `IStorySceneService`。

自定义节点可以通过 `StoryNodeData.parameters` 接收宿主参数。例如：

```json
{
  "nodeId": "start_battle_001",
  "nodeType": "StartBattle",
  "parameters": {
    "encounterId": "encounter_test_001",
    "nextStoryId": "story_after_battle"
  }
}
```

推荐组装顺序：

1. 获取 `StorySceneFacade`。
2. 按需调用 `ConfigureDependencies(contentProvider, view, clock)`。
3. 调用 `RegisterNodeHandler(...)` 注册跨系统节点。
4. 订阅 `StoryStarted`、`StoryEnded`、`StoryError`。
5. 最后调用 `OpenStoryPanel(...)`。

依赖注入既可以发生在 `StorySceneFacade.Awake` 之前，也可以发生在其默认初始化之后；组件不会在 Awake 中覆盖已经完成的注入。重新配置时，已经注册的自定义节点处理器会被保留。剧情运行期间禁止重新配置依赖。

## 运行时安全约定

- 同一节点或选项的变量操作按事务执行；任一操作失败时，之前的写入会整体回滚。
- 自定义 `IStoryNodeHandler` 抛出的异常会转换为 `StoryError`，不会直接击穿宿主的 Unity `Update`。
- 注册空处理器或空 `NodeType` 时通过 `StoryError` 报告，不把参数异常抛给普通游戏流程。

## 默认目录约定

- 剧情 JSON：`Assets/Resources/Story/{storyId}.json`
- 内置面板预制体：`Assets/Scripts/Story/Prefabs/StoryPanel.prefab`

这是默认内容适配器的约定，不是核心逻辑的硬编码依赖。单文件根对象必须同时包含：

```json
{
  "formatVersion": 1,
  "storyId": "story_intro",
  "startNodeId": "start",
  "skipNodeId": "ending",
  "nodes": []
}
```

不再使用 `Configs`、`Stories` 或 `storyFile` 拆分结构。

## 宿主持久化边界

Story 模块不再包含：

- `SaveStory` / `LoadStory`
- `IStorySaveRepository`
- `LocalJsonStorySaveRepository`
- `StorySaveData` 与逐字、变量等存档快照 DTO

如果宿主项目需要全局存档，应由宿主保存自己的流程状态，并在恢复流程时重新决定调用哪条剧情、从哪个宿主业务节点继续。当前 Story API 不承诺从剧情中间恢复。

## 单独打包

后续导出 Unity Package 时，选择整个目录：

```text
Assets/Scripts/Story
```

目标项目需要安装 `com.unity.nuget.newtonsoft-json` 3.2.2，并保留 `ProjectGuilt.Story.asmdef` 对 `Newtonsoft.Json.dll` 的显式引用。不要手工复制另一份 Newtonsoft DLL，也不要把宿主项目的战斗脚本、场景或 `Assets/Temporary` 一并加入剧情功能包。

如果使用内置面板，还需要安装 Unity 官方 `com.unity.ugui`，并保留 `ProjectGuilt.Story.UGUI.asmdef`。若宿主完全替换 `IStoryView`，可以不使用 `StoryPanel.prefab`，但这是高级定制路径。
