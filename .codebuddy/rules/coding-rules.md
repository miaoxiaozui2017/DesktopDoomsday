# 编码规范 — Desktop Doomsday

## 命名
- 公开成员：`PascalCase`（类、方法、属性、公开字段）。
- 私有字段：`_camelCase` 前缀下划线。
- 接口：`I` 前缀（如 `IRoadSystem`）。
- 枚举值：`PascalCase`（如 `SurvivorState.Normal`）。
- 命名空间：`DesktopDoomsday.Core` / `.Systems` / `.Data` / `.Window` / `.UI`。

## 架构红线
- `GameState` 是唯一运行时数据源（单例），通过 `ServiceLocator` 获取。
- Systems 只含行为逻辑，不持有 UI 引用。
- UI 只订阅 `GameEvents` 刷新，禁止轮询 GameState，禁止在事件回调里直接改其他系统状态。
- 跨系统修改必须走对应 System 的接口方法。

## 性能禁令
- 禁止在 `Update` 中每帧 `new` 对象（池化或复用）。
- 禁止 `Find`、`GetComponent` 在运行时热路径调用（启动时缓存）。
- 帧率（`targetFrameRate`）与数值 tick 解耦；tick 用 `unscaledDeltaTime` 保证后台一致。

## 数值与随机
- 所有随机走 `System.Random` 确定性种子（种子存存档），保证读档可复现。
- 数值表集中在 `GameConfig`（ScriptableObject），逻辑不硬编码常量。

## 存档
- 路径：`Application.persistentDataPath/save.json`。
- 原子写：先写 `.tmp` 再 `File.Replace`。
- 版本不匹配走迁移或重置提示。
