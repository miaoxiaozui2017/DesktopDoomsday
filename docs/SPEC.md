# SPEC：桌面末世 / Desktop Doomsday（MVP v1.0）

## 0. 文档使用说明

**目标读者**：WorkBuddy 代码生成 Agent。
**文档性质**：SPEC（可执行规格），不是设计提案。每个系统模块包含 职责 / 数据结构 / 接口 / 行为规则 / 验收标准。
**非目标（明确不做）**：战斗系统、多车辆编队、联机、创意工坊、剧情分支、内购系统。

---

## 1. 项目概述

一句话：Windows 桌面常驻的末日公路自动推进放置游戏，玩家开局选一辆车，车辆自动向前推进，宝箱开出物资，小人维持生存，随机副本可主动参与获取额外奖励，不参与不影响基础推进。

核心循环（供 Agent 理解系统因果关系）：

```
公路推进 tick → 里程累积 → 宝箱生成/开启 → 物资入库存
→ 小人消耗 tick → 需求状态更新 → 公路效率修正
→ 区域解锁 → 新天气权重更新 → 副本触发判定
→ 玩家参与副本 → 副本奖励结算 → 反哺车辆改装/物资
```

MVP 内容边界：3 辆车（各 2 级改装线）、3 个区域、4 种天气、5 种物资、3 条小人需求、3 类宝箱、2 个副本、10 个成就。

---

## 2. 技术选型与项目配置

### 2.1 引擎选择

推荐：Unity 2022.3 LTS 或 Unity 6 LTS（2D URP）。理由：Windows 透明窗口 + 点击穿透有成熟的 P/Invoke 方案，社区资料充分；如果团队更熟悉 Godot，Godot 4.x 也可行，但需要额外处理 Windows API 调用。

平台：Windows 先行，macOS 作为后续移植目标（透明窗口方案不同，MVP 不做）。

### 2.2 Unity 必须的项目设置

| 设置项 | 值 | 原因 |
| --- | --- | --- |
| Player → Run in Background | ✅ 勾选 | 失去焦点时公路继续推进 |
| Player → Fullscreen Mode | Windowed | 避免全屏独占导致 DWM 失效 |
| Player → Use DXGI Flip Model（如版本存在） | ❌ 禁用 | D3D11 后端会绕过 DWM，Alpha 通道失效 |
| Camera → Clear Flags | Solid Color | 透明窗口前提 |
| Camera → Background Color | RGBA (0,0,0,0) | 否则呈现黑色不透明背景 |
| Quality → Anti Aliasing | Disabled | 透明边缘可能出黑边 |
| Application.targetFrameRate | 30（可降至 10） | 桌面放置性能目标 CPU <3% |

### 2.3 WorkBuddy 项目配置

在项目根目录创建 `.codebuddy/` 目录，结构如下：

```
.codebuddy/
├── rules/
│   └── coding-rules.md        # 编码规范
├── agents/
│   └── unity-dev.md           # Unity 开发 Agent 定义（可选）
├── CODEBUDDY.md               # 项目记忆文件
└── skills/                    # 项目级技能（如有）
```

CODEBUDDY.md 内容应包含：

```
# Desktop Doomsday — 项目记忆

引擎：Unity 2022.3 LTS / 2D URP / C#
平台：Windows 先行
架构：单一 GameState 单例 + 信号/事件驱动 UI
性能目标：CPU <3%，内存 <300MB，静止时 10 FPS
命名规范：PascalCase 公开成员，_camelCase 私有字段，I 前缀接口
禁止：在 Update 中每帧 new 对象、Find、GetComponent
存档：JSON，Application.persistentDataPath
```

---

## 3. 目录结构

```
DesktopDoomsday/
├── Assets/
│   ├── Scenes/
│   │   └── Main.unity              # 仅作为入口，挂载 Bootstrapper
│   ├── Scripts/
│   │   ├── Core/
│   │   │   ├── GameState.cs         # 单一数据源（单例）
│   │   │   ├── GameLoop.cs          # tick 调度器
│   │   │   ├── SaveSystem.cs        # 存档/读档/离线结算
│   │   │   └── ServiceLocator.cs    # 轻量服务定位
│   │   ├── Systems/
│   │   │   ├── RoadSystem.cs        # 公路推进
│   │   │   ├── ChestSystem.cs       # 宝箱生成与开启
│   │   │   ├── InventorySystem.cs   # 物资库存
│   │   │   ├── SurvivorSystem.cs    # 小人需求
│   │   │   ├── WeatherSystem.cs     # 天气轮换与修正
│   │   │   ├── VehicleSystem.cs     # 车辆属性与改装
│   │   │   ├── DungeonSystem.cs     # 副本触发/结算
│   │   │   └── AchievementSystem.cs
│   │   ├── Window/
│   │   │   ├── TransparentWindow.cs # Win32 透明/穿透/置顶
│   │   │   ├── ClickThroughManager.cs
│   │   │   └── TrayManager.cs       # 托盘菜单
│   │   ├── UI/
│   │   │   ├── MainHud.cs
│   │   │   ├── DungeonPrompt.cs     # 非模态副本提示
│   │   │   ├── DungeonPanel.cs      # 副本面板
│   │   │   └── OfflineReport.cs
│   │   └── Data/
│   │       ├── GameConfig.cs        # ScriptableObject 静态表
│   │       └── DTO/                 # 存档 DTO
│   ├── Art/
│   ├── Audio/
│   └── Resources/
│       └── GameConfig.asset
├── docs/
│   └── SPEC.md                      # 本文件
└── .codebuddy/
```

原则：GameState 持有全部运行时状态，Systems 只包含行为逻辑，UI 只订阅事件/信号刷新，不反向写入状态。

---

## 4. 数据契约

### 4.1 GameState（单一数据源）

```csharp
[Serializable]
public class GameState
{
    // 公路
    public float totalDistanceKm;
    public float currentSpeedKmPerTick;
    public int currentZoneIndex;       // 0=城郊, 1=荒漠, 2=冻原

    // 车辆
    public VehicleId currentVehicle;
    public int[] upgradeLevels;        // 每辆车的每条改装线等级

    // 物资
    public int food, water, medicine, fuel, parts;

    // 小人
    public float hunger;               // 0-100，低于阈值触发虚弱
    public float thirst;
    public float health;
    public SurvivorState survivorState; // Normal / Weakened

    // 天气
    public WeatherId currentWeather;
    public float weatherRemainingSeconds;

    // 宝箱
    public float distanceSinceLastChest;
    public float nextChestThresholdKm;

    // 副本
    public float dungeonCooldownRemaining;
    public DungeonId? pendingDungeon;
    public float pendingDungeonTimeout;

    // 时间
    public long lastSaveUnixMs;
    public float playTimeSeconds;

    // 成就
    public HashSet<string> unlockedAchievementIds;
}
```

### 4.2 静态配置（ScriptableObject / JSON）

GameConfig 包含以下数据表，Agent 应生成对应的可序列化类并在 Inspector 中可编辑：

- VehicleConfig[]：id, displayName, baseSpeed, fuelPerKm, cargoCapacity, chestChanceModifier, dungeonRewardModifier, upgradeTracks[]
- ZoneConfig[]：id, name, unlockDistanceKm, weatherWeightTable, dungeonPool
- WeatherConfig[]：id, displayName, speedMultiplier, fuelMultiplier, chestChanceBonus, survivorDrainMultiplier, durationRange
- ResourceConfig：每种物资的初始量、宝箱掉落权重
- SurvivorConfig：消耗速率、虚弱阈值、恢复速率
- DungeonConfig[]：id, displayName, durationSeconds, branches[], rewardTable
- AchievementConfig[]：id, 条件类型, 参数

### 4.3 存档 DTO

```csharp
[Serializable]
public class SaveData
{
    public int version;                // 存档版本号，当前 = 1
    public GameState state;
    public long savedAtUnixMs;
}
```

规则：存档路径 `Application.persistentDataPath/save.json`；每次自动保存前先生成 DTO 快照，写入临时文件后原子替换；加载时如果 version 不匹配，走迁移函数或重置并提示。

---

## 5. 系统模块规格

### 5.1 GameLoop（调度器）

**职责**：以固定 tick 驱动所有 Systems，避免每个系统各自 Update。

**行为规则**：

- 基础 tick 间隔 1.0 秒；公路推进、小人消耗、天气倒计时、副本冷却都在 tick 中执行。
- 当窗口最小化到托盘或处于点击穿透模式时，tick 间隔降为 5.0 秒（性能优化），但逻辑 tick 按真实经过时间补齐，保证离线/后台收益一致。
- 帧率与 tick 解耦：帧率只影响 UI 动画，不影响数值推进。

**验收**：后台运行 10 分钟后切回，公路里程与在线同 tick 推进结果误差 < 1%。

### 5.2 RoadSystem（公路推进）

**接口**：

```csharp
public interface IRoadSystem
{
    float GetCurrentSpeed(GameState state, WeatherId weather, VehicleId vehicle);
    void Tick(GameState state, float deltaSeconds);
    float GetZoneProgress(GameState state);
    bool TryUnlockNextZone(GameState state);
}
```

**行为规则**：

- 每 tick 推进距离 = 当前速度 × 天气速度修正 × 小人状态修正 × deltaSeconds。
- 小人 Weakened 时速度乘以 0.7–0.9（根据虚弱程度插值），不降为 0。
- 达到区域解锁里程时，自动进入下一区域，触发 OnZoneChanged 事件；最后一区之后无限循环当前区域，里程继续累积。
- 区域解锁不可逆，不回退。

**验收**：初始车辆在晴天、正常小人状态下，1 小时在线推进里程与配置表预期误差 < 5%。

### 5.3 ChestSystem（宝箱）

**宝箱类型与触发**：

| 类型 | 触发条件 | 开启方式 |
| --- | --- | --- |
| 里程宝箱 | distanceSinceLastChest >= nextChestThresholdKm | 自动开启，播放短动画 |
| 随机宝箱 | 每 tick 以 baseChance × weatherChestBonus 判定 | 自动开启，托盘气泡可选提示 |
| 副本宝箱 | 副本结算后生成 | 在副本面板中手动领取 |

**接口**：

```csharp
public interface IChestSystem
{
    void Tick(GameState state, float deltaSeconds);
    ChestOpenResult Open(ChestType type, GameState state);
}
```

**规则**：

- 里程宝箱的 nextChestThresholdKm 在每次开启后重新随机，范围由当前区域配置决定。
- 宝箱内容按 ResourceConfig 权重掉落 1–3 种物资，副本宝箱额外掉落 BlueprintFragment（用于车辆改装）。
- 所有随机使用 System.Random 的确定性种子（种子存入存档），保证存档读档后随机序列可复现。

**验收**：连续 100 次开启的物资分布与配置权重偏差 < 10%。

### 5.4 SurvivorSystem（小人需求）

**数据结构**：

```csharp
public enum SurvivorState { Normal, Weakened }
```

**行为规则**：

- hunger / thirst / health 初始 100，每 tick 按配置速率下降。
- 任一值 ≤ 0 时，自动消耗对应库存物资补充；库存不足时，该值保持 0 并进入 Weakened。
- Weakened 状态下，三值下降速率 ×1.3（生存压力增加，但不死亡）。
- 三值全部 > 60 时，Weakened 解除，速度惩罚在 60 秒内线性恢复。
- 不实现死亡。

**验收**：库存充足时小人永不进入 Weakened；库存耗尽后进入 Weakened 且速度惩罚生效，补货后 60 秒内恢复 Normal。

### 5.5 WeatherSystem（天气）

**规则**：

- 天气按当前区域的权重表随机抽取，持续 durationRange 内随机秒数。
- 天气切换只触发 OnWeatherChanged 事件，不弹模态窗口。UI 更新顶部小图标 + 场景色调。
- 每种天气的数值修正见 WeatherConfig，作用于 RoadSystem / SurvivorSystem / ChestSystem。
- 极端天气（沙暴、极寒）首次出现时，允许触发一次托盘气泡，之后同一天不再重复。

**MVP 天气表**：

| 天气 | 速度 | 油耗 | 宝箱概率 | 小人消耗 |
| --- | --- | --- | --- | --- |
| 晴天 | ×1.0 | ×1.0 | 基准 | ×1.0 |
| 暴雨 | ×0.85 | ×1.0 | +10% | ×1.0 |
| 沙暴 | ×0.9 | ×1.25 | +5% | ×1.0 |
| 极寒 | ×0.9 | ×1.0 | 基准 | ×1.3 |

### 5.6 DungeonSystem（副本）

**触发规则**：

- 每个 tick 检查 dungeonCooldownRemaining 和概率；冷却结束后按区域 dungeonPool 随机候选。
- 触发时设置 pendingDungeon，生成 非模态提示：窗口底部一行文字 + 边缘轻微高亮。不暂停公路，不夺取焦点。
- pendingDungeonTimeout 默认 20 秒；超时未点击则自动清除，视为忽略。

**接口**：

```csharp
public interface IDungeonSystem
{
    bool TryTrigger(GameState state);
    DungeonResult Complete(DungeonId id, int chosenBranchIndex, GameState state);
    void IgnorePending(GameState state);
}
```

**副本面板规格**：

- 点击提示后打开 60–90 秒的轻量面板。
- 每个副本包含 2–3 个分支选项（如"搜刮仓库 / 检查加油站 / 追击车队"）。
- 玩家选择一个分支 → 立即结算奖励 → 面板关闭 → 公路继续。
- 副本进行中公路可以继续推进（或按设置暂停，见下文），不强制阻断挂机。
- 奖励量级：约等于 2–3 倍基础挂机同期收益，外加蓝图碎片。

**设置项**：

- AutoIgnoreDungeons（默认 false）：开启后副本提示不出现，冷却照常。
- PauseRoadDuringDungeon（默认 false）：开启后打开副本面板时暂停公路 tick。

**验收**：忽略副本后公路里程继续增长；参与副本获得的物资总量在同期基础收益的 2–3 倍区间内。

### 5.7 VehicleSystem（车辆与改装）

- 开局选择：Bootstrapper 显示三选一面板，选择后写入 currentVehicle，之后不可更换。
- 改装：每条改装线 2 级，消耗 parts 和 blueprintFragments。改装效果直接修改 VehicleConfig 的运行时副本，不修改 ScriptableObject 原始数据。

**MVP 车辆基线**：

| 车辆 | 速度 | 油耗/公里 | 载货 | 特性 |
| --- | --- | --- | --- | --- |
| 皮卡 | 中 | 中 | 中 | 宝箱概率 +5% |
| 厢式货车 | 慢 | 高 | 大 | 副本奖励 +10% |
| 摩托 | 快 | 低 | 小 | 里程宝箱阈值 -10% |

### 5.8 SaveSystem（存档与离线收益）

**行为规则**：

- 每 60 秒自动保存一次；关键事件（区域解锁、车辆改装、副本完成）后立即保存。
- 退出时在 OnApplicationQuit 保存。
- 加载时计算 offlineSeconds = (now - lastSaveUnixMs) / 1000，上限 8 小时。
- 离线收益 = 按基础速度 × 0.5 的消耗速率模拟 tick，不生成宝箱、不触发副本，只结算公路里程和物资消耗。
- 离线结算面板在回归时弹出一次，显示里程、物资变化、小人状态，可关闭。
- 使用 `DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()` 而非本机可回拨时间做防作弊底线；如果检测到 now < lastSave，离线收益按 0 处理。

**验收**：存档 → 修改系统时间 +2 小时 → 读档，离线收益不超过 2 小时上限且不重复计算。

### 5.9 TransparentWindow / ClickThrough（桌面窗口层）

**职责**：将 Unity 窗口变为无边框、透明、可置顶、可点击穿透的桌面窗口。

**必须实现的 Win32 调用（P/Invoke）**：

| API | 用途 |
| --- | --- |
| GetForegroundWindow / FindWindow | 获取 Unity 主窗口 HWND |
| SetWindowLongPtr(GWL_EXSTYLE, WS_EX_LAYERED \| WS_EX_TRANSPARENT) | 分层窗口 + 鼠标穿透 |
| SetLayeredWindowAttributes | 全局透明度或色键透明 |
| SetWindowPos(HWND_TOPMOST) | 置顶 |
| DwmExtendFrameIntoClientArea(-1) | 扩展客户区透明（可选，更平滑） |

**行为规则**：

- 所有 Win32 调用必须在 Start() 中 延迟一帧（Coroutine yield return null）执行，确保 Unity 已完成窗口创建。
- OnApplicationQuit 中恢复原始窗口样式，移除 WS_EX_LAYERED / WS_EX_TRANSPARENT，防止残留。
- 点击穿透通过全局快捷键切换（如 Ctrl+Shift+D），切换时同步启用/禁用 WS_EX_TRANSPARENT。
- 透明区域点击穿透，但 UI 按钮区域必须可点击：通过检测鼠标位置是否在交互 Rect 内，动态切换 WS_EX_TRANSPARENT，或使用不透明 UI 元素覆盖可交互区域。

**验收**：开启穿透后，桌面其他窗口可正常接收鼠标点击；关闭穿透后，游戏 UI 按钮可正常点击；退出后系统窗口样式恢复正常。

### 5.10 TrayManager（托盘）

- 最小化时隐藏主窗口，显示托盘图标。
- 托盘菜单：显示/隐藏、暂停/继续、穿透模式开关、自动忽略副本开关、退出。
- 托盘图标右键菜单使用 Win32 Shell_NotifyIcon 或轻量第三方封装，不引入重型依赖。

---

## 6. 事件与信号契约

所有跨系统通信通过事件，UI 不直接轮询 GameState。Agent 应生成一个静态 GameEvents 类：

```csharp
public static class GameEvents
{
    public static event Action<float> OnDistanceAdvanced;
    public static event Action<ChestOpenResult> OnChestOpened;
    public static event Action<ResourceDelta> OnInventoryChanged;
    public static event Action<SurvivorState> OnSurvivorStateChanged;
    public static event Action<WeatherId> OnWeatherChanged;
    public static event Action<int> OnZoneChanged;
    public static event Action<DungeonId> OnDungeonAvailable;
    public static event Action<DungeonResult> OnDungeonCompleted;
    public static event Action<OfflineReport> OnOfflineReportReady;
    public static event Action<string> OnAchievementUnlocked;
}
```

**规则**：事件只在状态真正变化时触发；UI 在 OnEnable 订阅、OnDisable 取消订阅；禁止在事件回调中直接修改其他系统的内部状态，修改必须通过对应 System 的接口。

---

## 7. 验收标准（WorkBuddy 生成代码的测试锚点）

| # | 验收项 | 通过条件 |
| --- | --- | --- |
| 1 | 公路推进 | 在线 60 分钟里程与配置预期误差 < 5% |
| 2 | 小人无死亡 | 任意库存状态下，小人不会进入不可恢复状态 |
| 3 | 副本可忽略 | 不点击提示时，20 秒后自动清除，公路里程继续增长 |
| 4 | 副本奖励量级 | 参与副本的同期物资收益是纯挂机的 2–3 倍 |
| 5 | 离线收益 | 上限 8 小时，不重复结算，时间回拨不产生收益 |
| 6 | 存档原子性 | 写入过程中强制结束进程，旧存档仍可加载 |
| 7 | 透明穿透 | 穿透模式下桌面其他窗口可点击；退出后样式恢复 |
| 8 | 性能 | 静止时 CPU < 3%，内存 < 300 MB（Profile 实测） |
| 9 | 后台 tick | 最小化 10 分钟后切回，里程与在线同时间结果误差 < 1% |
| 10 | 随机确定性 | 同一存档反复读档，宝箱掉落序列一致 |

---

## 8. 给 WorkBuddy 的分批开发指令（建议顺序）

不要一次性让 Agent 生成全部代码。按以下批次执行，每批完成后运行验收：

**Batch 1 — 核心数据与循环骨架**
- 生成 GameState、GameConfig（ScriptableObject）、GameLoop、GameEvents、SaveSystem 的 DTO 与接口空壳。
- 不实现具体系统逻辑，只保证编译通过、tick 调度和存档读写可运行。

**Batch 2 — 公路 + 宝箱 + 库存 + 小人**
- 实现 RoadSystem、ChestSystem、InventorySystem、SurvivorSystem。
- 用纯文本/控制台日志验证数值循环，UI 暂不做。

**Batch 3 — 天气 + 车辆 + 区域**
- 实现 WeatherSystem、VehicleSystem、区域解锁逻辑。
- 接入天气修正到公路/消耗/宝箱概率。

**Batch 4 — 副本系统**
- 实现 DungeonSystem 的触发、超时忽略、分支结算。
- 先做数据层，不做面板 UI。

**Batch 5 — 桌面窗口层**
- 实现 TransparentWindow、ClickThroughManager、TrayManager。
- 这是最容易出平台问题的模块，单独一批，逐项验收。
- **美术约束**：透明窗口只改桌面穿透，不改变场景绘制；必须验证"相机透明背景（2.2）+ 高清像素层（第 9.2 节）"共存时 Pixel Perfect 不冲突、不出现黑边（见 9.6）。

**Batch 6 — UI 与离线报告**
- 实现 MainHud、DungeonPrompt（非模态）、DungeonPanel、OfflineReport。
- 所有 UI 只订阅事件，不轮询。
- **美术约束**：全部正式 UI 必须实现第 9.4 节「科技感 UI 规范」（暗色玻璃面板、霓虹描边、等宽字体、霓虹条形进度）。`DebugHud.cs`（IMGUI）仅为开发期调试覆盖层，不计入正式风格。

**Batch 7 — 成就 + 打磨**
- 接入 AchievementSystem，补齐 10 个成就。
- 性能 Profile，降帧、合并 DrawCall、关闭不必要粒子。
- **美术约束**：统一调色板（第 9.3 节）、确认像素锐利度、科技 UI 性能验收（静止 CPU <3%）。

---

**一句话给 Agent**：这份 SPEC 的边界是硬的——数值表可以调，但"小人不可死亡、副本可忽略、后台 tick 不中断、窗口穿透可恢复"这四条是架构约束，生成代码时不得绕过。**美术与 UI 风格（第 9 节）同样为硬约束**：高清像素风、鲜明配色、科技感 UI 是统一基调，任何批次新增的视觉/界面产出都不得偏离第 9 节。

---

## 9. 美术与 UI 风格规格

> 本节为跨批次的视觉基线。所有美术资源、UI 控件、颜色与字体选择都不得偏离本节；数值/逻辑可迭代，但视觉语言必须一致。Agent 在每个涉及"可见产出"的批次（Batch 5/6/7 为主，Batch 2–4 的临时可视化也建议参考）都应对照本节自检。

### 9.1 总体基调

- **主题**：末日公路 + 科技仪表盘质感——冷峻、机械、但色彩鲜明不灰扑扑。
- **美术**：高清像素风（High-Res Pixel Art）。精灵以 2–4 倍目标分辨率手工绘制，运行时由 Pixel Perfect Camera 下采样到参考分辨率，保证放大后边缘锐利、不糊。
- **配色**：鲜明对比。暗色底 + 高饱和亮色块，用色块/霓虹条传达状态，避免写实灰调。
- **UI**：科技感（Sci-Fi HUD）。暗色半透明玻璃面板、霓虹描边、等宽字体、细线分隔，状态用色块与条形而非写实拟物图标。

### 9.2 高清像素风技术规范

- 主相机挂 **Pixel Perfect Camera**（Package Manager 安装 `2D Pixel Perfect`）；`Assets Pixels Per Unit` 与 `Reference Resolution` 统一（建议 PPU = 32 或 48，参考分辨率 1920×1080）。
- 精灵导入设置：`Texture Type = Sprite (2D and UI)`、`Filter Mode = Point (No Filter)`、`Compression = None`、`Max Size` 保留原图尺寸、`Sprite Mesh Type = Full Rect`。
- "高清"实现方式：角色/车辆/场景物件按实际大像素尺寸绘制，再由 Pixel Perfect 缩到参考分辨率——切勿直接放大低分辨率图。
- 调色板集中在 **GameConfig 或一个 Palette ScriptableObject** 中统一管理，禁止在代码/材质里散落硬编码颜色。

### 9.3 配色板（鲜明，集中管理）

| 角色 | 色值（Hex） | 用途 |
| --- | --- | --- |
| 背景 / 面板底 | `#0B0E14` / `#121826` | HUD 暗色玻璃底 |
| 主强调（科技青） | `#22D3EE` / `#00E5FF` | 主 UI 描边、进度、选中态 |
| 高亮 / 警示（琥珀） | `#FFB000` / `#FF8A00` | 重要数值、副本提示、奖励 |
| 正向 / 资源（绿） | `#34D399` | 物资充足、正常状态 |
| 危险 / 虚弱（红） | `#F43F5E` | 库存告急、Weakened、危险 |
| 文本主 / 次 | `#E5ECF5` / `#8A97A8` | 主文本 / 次要说明 |
| 区域色调 | 城郊灰蓝 / 荒漠橙黄 / 冻原冷白青 | 场景与天气氛围着色 |

规则：同一语义（如"危险"）在所有界面必须使用同一色值；区域/天气切换通过改变场景色调（叠加半透明色调层）体现，不替换精灵。

### 9.4 科技感 UI 规范

- **框架**：正式 UI 用 uGUI（`Screen Space - Camera`）或 UI Toolkit；HUD 为半透明暗面板 + 1px 霓虹描边（Image 边框或 Outline），不追求拟物。
- **字体**：等宽科技字体（如 Oxanium / Roboto Mono），数字与状态读数必须用等宽以保证对齐与科技感。
- **组件语言**：
  - 里程 / 小人需求（hunger/thirst/health）用**细长霓虹条形进度**，而非圆形或写实图标。
  - 天气 / 区域用**顶部小图标 + 文字标签**，切换只更新图标与场景色调（见 5.5）。
  - 分区、卡片用**细线分隔**（1px，半透明青）。
- **动效（轻量，受性能约束）**：数值滚动过渡、扫描线微动、边缘高亮脉冲；**禁止重粒子**（与 8 性能目标冲突）。
- **可读性底线**：文本对比度达标，任何动效不得妨碍数值即时读取。

### 9.5 资源目录与命名

```
Assets/
├── Art/
│   ├── Sprites/        # 像素精灵（车辆/小人/场景/宝箱）
│   ├── Palette/        # 调色板 ScriptableObject（9.3）
│   ├── UI/             # UI 图集/九宫格面板/图标
│   └── Weather/        # 天气色调叠加层
└── Audio/              # 音效（MVP 可后置，不在美术硬约束内）
```

命名：`{分类}_{对象}`，分类前缀 `veh_`（车辆）、`sur_`（小人）、`wth_`（天气）、`itm_`（物资/宝箱）、`ui_`（UI 元素）、`zn_`（区域）。例：`veh_pickup_0`、`ui_frame_panel`、`wth_sandstorm_tint`。

### 9.6 与批次的对应约束

- **Batch 5（窗口层）**：透明窗口（`WS_EX_LAYERED` / 相机 `RGBA(0,0,0,0)`）只影响桌面穿透，不改变场景绘制；必须验证 Pixel Perfect 与透明背景共存时不出现黑边、不破坏像素对齐。
- **Batch 6（UI）**：所有正式 UI 落地 9.4 科技感规范；`DebugHud`（IMGUI）仅开发期调试，不计入正式风格，二者职责分离。
- **Batch 7（打磨）**：统一调色板（9.3）、确认像素锐利度、科技 UI 性能验收（CPU <3% / 内存 <300MB）。

> 注：`Assets/Art`、`Assets/Audio` 已在第 3 节目录结构中预留；本节的 Palette/Weather 子目录与命名为其细化。实际像素精灵生成到 `Assets/Resources/Art/Sprites/`（置于 Resources 下以保证打包构建可加载，运行时经 `Resources.Load` 取用）。
