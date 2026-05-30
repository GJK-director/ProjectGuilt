# 代码阅读问题记录

这个文档用来记录我在读代码时遇到的问题、疑惑和当前解释。

规则：

- 问题可以是代码含义、命名含义、Unity API、设计原因。
- 当前解释不一定是最终设计，只代表当前代码状态下的理解。
- 如果后续设计改变，可以补充新的回答，不要直接抹掉旧问题。

## 问题列表

### 1. 角色是不是也应该有 JSON 文件？

- 状态：已回答，后续可设计
- 提出位置：检查 `CharacterData.cs` 时
- 问题：
  - 角色是不是也应该有 JSON 文件？
  - 当前是不是直接生成角色，而不是读取角色？
- 当前回答：
  - 是的，当前角色是直接生成的，不是从 JSON 读取。
  - 当前代码在 `CardLoadTest.CreateTestCharacters()` 中用 `new CharacterData(...)` 创建测试角色。
  - 也就是说，现在角色数据是测试代码写死的。
- 当前相关代码：

```csharp
allyA = new CharacterData("我方角色A", 30, 20, 20);
allyB = new CharacterData("我方角色B", 30, 3, 5);
enemy = new CharacterData("敌人", 999, 5, 8);
```

- 当前理解：
  - `CharacterData` 是战斗运行时角色数据。
  - 未来角色 JSON 可以作为角色初始配置。
  - 未来可以通过读取 JSON 后再创建 `CharacterData`。

### 2. 注释里说“旧版构造函数”，为什么没找到？

- 状态：已回答，后续需要确认代码是否已保存
- 提出位置：检查 `CharacterData.cs` 的构造函数时
- 问题：
  - 注释写了“旧版构造函数：只有一个固定速度”。
  - 但是阅读代码时不确定它指的是哪一段。
- 当前回答：
  - “旧版构造函数”指的是三参数版本：

```csharp
public CharacterData(string name, int hp, int characterSpeed)
```

  - 这个版本只传一个速度，所以会把最低速度和最高速度都设成同一个值。
  - “新版构造函数”指的是四参数版本：

```csharp
public CharacterData(string name, int hp, int characterMinSpeed, int characterMaxSpeed)
```

  - 这个版本支持速度范围。
- 相关概念：
  - C# 里同一个类可以有多个同名构造函数，只要参数不同即可。
  - 这叫“函数重载”。
  - 重载 = 同一个函数名，根据传入参数数量或类型不同，执行不同版本。
- 当前代码检查记录：
  - 用户表示已经删除旧版固定速度构造函数。
  - 当前助手检查到的 `CharacterData.cs` 中仍能看到三参数构造函数。
  - 后续需要确认：代码是否已经保存，或者是否仍需要删除该旧版构造函数。
