# Project Guilt Git 协作规范 v1

Status: CURRENT  
Role: HUMAN GIT COLLABORATION GUIDE

本文主要给 Project Guilt 的两位项目开发者阅读，帮助大家用同一套方式进行日常开发、Unity 验收和 GitHub 协作。

`AGENTS.md` 仍是 AI / Local Executor 执行协议的最高规则。本文只说明人类协作流程，不覆盖 AGENTS.md 中的 Source Gate、Plan Gate、Unity Gate 或提交授权规则。

## 5 分钟版：平时只照这里做

`main` 是公共稳定版本。新任务默认不要直接在 `main` 上开发，而是从最新 `main` 创建对应功能分支。

1. 先在 GitHub Desktop 确认 Current Branch，并 Fetch origin 获取最新远端状态。
2. 从 `main` 创建本次任务的功能分支：战斗用 `battle/*`，剧情用 `story/*`，文档用 `docs/*`。
3. 在自己的分支开发，并在 Unity Editor 中完成需要的刷新、编译和人工验收。
4. 查看 Changes 列表，逐个确认每个文件为什么变化。
5. 只勾选本次任务产生、且确实要交给其他人的文件。
6. 填写清楚的 Summary，创建 Commit。Commit 是记录在本地的版本，不等于已经上传。
7. Push 当前功能分支到 GitHub，不要把未审查的工作直接 Push 到 `main`。
8. 由负责审查的人进行 Review；在当前 Project Guilt 流程中，远端代码与提交范围由 Sol 进行 Remote Review。确认无误后，功能分支才允许进入 `main`。
9. 将已经通过 Review 的功能分支 Merge into `main`；Merge 前再次 Fetch，确认远端 `main` 没有出现新的未处理提交。
10. Push 更新后的 `main` 到 GitHub。
11. 由负责 Remote Gate 的人再次核对远端 `main` 的提交、文件和范围；Remote Gate 通过后，本次功能才算关闭。

常用分支含义：

- `main`：公共稳定版本。
- `battle/*`：战斗任务。
- `story/*`：剧情任务。
- `docs/*`：文档任务。

## 日常模式与异常模式

### 日常模式：绝大多数开发都走这里

正常情况下，开发者不需要每次上传前打开终端执行一系列 Git 命令。日常工作主要通过 GitHub Desktop：

1. Fetch origin。
2. 确认 Current Branch。
3. 从最新 `main` 创建功能分支。
4. 开发与 Unity 验收。
5. 查看 Changes。
6. 逐项确认并勾选本任务文件。
7. Commit。
8. Push origin。
9. 告诉 Sol 已 Push 功能分支。
10. Sol 进行 Remote Review。
11. Review PASS 后再 Merge `main`。
12. Push `main`。
13. Sol 进行 Final Remote Gate。

当 GitHub Desktop 的 Changes、Branch、Fetch 状态清晰，且没有冲突、diverged 或可疑 dirty 时，不要求开发者额外打开终端重复验证。

### 异常模式：这时才打开终端

出现以下情况时，停止普通流程：

- Push 被拒绝。
- ahead / behind 状态无法理解。
- 本地与远端 diverged。
- Merge / Rebase conflict。
- 需要进行 stash 相关操作。
- Font / Scene / Prefab 等 dirty 来源不明。
- 不确定某个文件是否已经进入 Commit。
- GitHub Desktop 提示无法安全切换分支。
- 需要 Reset / Rebase / Force Push。
- Sol 明确要求进一步诊断。

这时按下面的顺序处理：

STOP
→ 不凭感觉点按钮
→ 把状态发给 Sol
→ 再按具体命令诊断

终端是诊断工具，不是正常开发的必经界面。

对人类开发者，正常的 GitHub Desktop 日常操作可以简化。对 AI / Local Executor，仍然必须按 `AGENTS.md` 执行 branch、HEAD、working tree 等 Source Gate 检查。

## 核心概念：在本项目中它们做什么

- **Commit**：把当前选中的改动保存成一个有说明的本地版本节点。创建 Commit 不会自动上传。
- **Push**：把本地分支上的 Commit 上传到 GitHub。Push 功能分支不会让它自动进入 `main`。
- **Fetch**：只获取 GitHub 的最新信息，不把远端改动合进当前文件。多人协作时，准备 Push 或 Merge 前先 Fetch。
- **Pull**：获取远端改动并尝试合入当前分支。遇到本地 dirty 或复杂冲突前，先确认要保护的文件。
- **Merge**：把一个分支的历史合入另一个分支。Merge into `main` 才表示功能正式进入公共版本。
- **Branch**：从某个版本分出独立工作线，让不同任务互不干扰。
- **Stash**：把暂时不能提交的本地改动放进临时保险箱，以便短暂切换分支或处理远端同步。
- **Rebase**：把自己的提交重新应用到更新后的基线之后。它会重写这些提交的历史，Commit SHA 通常会变化，所以发生分歧时必须先保护 dirty，并由项目协作者确认是否应该 Rebase。

最重要的区别是：Commit ≠ Push，Push branch ≠ 进入 `main`，Merge into `main` 才是正式进入公共版本。

## 哪些文件应该上传？

判断标准只有一个：**它是不是本次任务有意产生、并且需要交给其他人的成果？**

正常可以上传：

- 本次任务修改的源码。
- 本次任务修改的正式 JSON / Data。
- 本次任务修改的正式文档。
- 有意修改的 Scene / Prefab。
- 新资源及其对应的 `.meta`。

默认不要混入：

- Unity 自动变化的 `TMP_Font_CN_Runtime.asset`。
- 因测试 mode 产生 dirty 的 `SampleScene.unity`。
- 与任务无关的 Scene / Prefab。
- 别人的工作文件。
- Unity 自动缓存目录。
- IDE / Build 输出。

不存在“Scene 永远不能上传”或“Font 永远不能上传”的规则。只要它是本次任务有意产生、需要交给其他人的成果，就应按任务范围审查后上传。

## Unity 特殊规则

`.meta` 是 Unity 资源的身份证，里面包含资源的 GUID。Unity 通过它保持脚本、图片、Prefab、Scene 等资源的引用关系。

- 新增资源时，asset 与 `.meta` 必须对应处理。
- 移动资源时，必须连同原 `.meta` 一起移动，保留原 GUID。
- 有意删除 Unity 资源时，应同时删除它对应的 `.meta`；不要留下孤立 `.meta`，也不要只删 `.meta` 后让 Unity 为原资源重新生成新的 GUID。
- 已有 `.meta` 不得删除后重新生成，也不要手工改 GUID。

Scene、Prefab 和 `.asset` 都是序列化资产，冲突风险较高。两个人尽量不要同时修改同一个文件；如果确实需要同时修改，先约定负责人和合并顺序。

## GitHub Desktop 实际操作

在 GitHub Desktop 中：

- **Changes 列表**显示的是本地发生变化的文件，不代表这些文件都应该上传。
- **Checkbox** 用来选择本次 Commit 要包含的文件。提交前逐项判断，禁止无脑全选。
- **Summary** 写本次实际完成的短标题；需要时在描述中补充验证结果。
- **Commit** 把勾选内容保存到当前本地分支。
- **Push origin** 把当前分支的本地 Commit 上传到 GitHub。
- **Current Branch** 用来确认当前正在操作的是 `main` 还是功能分支。
- **Fetch origin** 用来查看远端是否出现新提交；准备 Push 或 Merge 前都应先 Fetch。

Changes 列表是“本地发生变化的文件”，不是“这些文件都应该上传”。如果看到 Font、SampleScene 或其他无关文件，先取消勾选，并确认它们的来源和保护方式。

## 项目硬规则

- 禁止无检查地使用 `git add .`。提交时只选择本次任务文件。
- 在 AI Executor 场景中，Commit / Push 必须经过用户明确授权。
- 创建 Commit 前先看 `git status --short`。
- 需要确认暂存区时，再看 `git diff --cached --name-only`。
- Push 或 Merge `main` 前先 `git fetch origin`。
- 多人项目中，不知道远端最新状态时不要直接 Push `main`。
- 不要为了清理工作树而删除、覆盖或恢复别人的 dirty 文件。

## 两人同时开发怎么办

例如一位开发者在 `battle/damage-number` 处理伤害数字，另一位在 `story/prologue-dialogue` 处理序章对话。双方各自在自己的分支开发、验收和提交，完成后分别通过 Review 进入 `main`。

同一个 Scene 或 Prefab 尽量提前约定负责人。若两个任务都需要它，先协调修改顺序，避免在 GitHub Desktop 里面对高风险序列化冲突时才临时决定。

## 出冲突怎么办

第一原则：**STOP。**

看到 `Use Mine`、`Use Theirs`、`Accept Current` 或 `Accept Incoming` 时，不要凭感觉点击。先执行 `git status`，确认哪些文件冲突、自己当前在哪个分支、远端操作是什么。

- `.cs`、`.json`、`.md` 通常可以人工分析冲突内容。
- `.unity`、Prefab、`.asset` 风险较高，需要结合 Unity 和序列化引用谨慎检查。
- Merge / Rebase 场景下，`Current` / `Incoming` 的含义不能只靠按钮名字猜。

先记录冲突文件和当前状态，再请项目协作者判断解决方式。没有确认前，不要提交冲突结果。

## Stash：临时保险箱

Stash 适合暂存暂时不能提交的本地改动，例如本地 Font dirty，但又需要切换分支、Pull 或处理 Rebase。

Stash 不属于某个分支的永久内容；列表里的 `On main` 只是创建 stash 时所在的分支。存在多个 stash 时，先确认编号和说明，再决定是否操作。不要随便 `apply`、`pop` 或 `drop`，也不要把别人的 stash 当成自己的工作。

## 真实事故案例

### Case 1：TMP Runtime Font 自动 dirty

发生了什么：Unity 自动修改了 `TMP_Font_CN_Runtime.asset`，但任务实际是 Deck 功能。  
为什么：这是 Unity 运行或导入产生的无关序列化变化。  
以后怎么判断：如果 Font 不是本次任务有意修改的成果，就取消勾选或放入已确认的 stash，不让它进入功能 Commit。

### Case 2：SampleScene 因 Test Mode dirty

发生了什么：为了运行测试，`SampleScene.unity` 中的 Test Mode 发生变化。  
为什么：测试入口的 Inspector 状态被序列化到 Scene，但它不是功能本身。  
以后怎么判断：先确认本次任务是否有意交付 Scene 配置；若只是临时测试选择，就排除提交并保护本地工作。

### Case 3：本地提交领先，远端又有队友提交

发生了什么：本地已有提交，但远端同时出现了队友的新提交，分支发生分歧。  
为什么：本地和远端各自前进，不能再把当前历史当成唯一最新状态。  
以后怎么判断：先保护 dirty，Fetch 远端并确认分支关系；不要自行决定 Rebase、Merge、Reset 或强制 Push。把状态交给 Sol / 项目协作者判断同步方案，完成后再重新验证文件范围。

### Case 4：`battle/deck-selection`

发生了什么：Deck Selection 在 `battle/deck-selection` 独立完成，先 Push 功能分支并进行 Remote Review，再 fast-forward `main`，最后 Push `main`。  
为什么：功能分支隔离了开发过程，让 Review 能检查实际提交和文件范围。  
以后怎么判断：战斗任务从 `battle/*` 开始；功能分支 Review 通过后才进入公共 `main`，Remote Gate 通过后才关闭任务。

## 提交前 30 秒检查

1. 我现在在哪个 branch？
2. 这些 changed files 为什么变了？
3. 每个勾选文件都是本次任务吗？
4. 有没有 Font、SampleScene 或无关 Scene？
5. 新 Asset 的 `.meta` 是否正确？
6. Unity 是否已经完成需要的验收？
7. 远端 `main` 是否刚刚 Fetch？
8. 我现在是在 Push feature branch，还是准备修改 `main`？

## 遇到问题时发给 Sol 什么

请复制下面的模板，并附上完整错误信息和必要截图：

```text
Branch:
HEAD:
git status --short:
git stash list:
错误全文:
GitHub Desktop 截图:
```

不要只说“Git 出错了”或“Unity 有问题”。当前分支、HEAD、dirty 文件、stash 列表和完整错误，能让协作者先判断事实再决定下一步。
