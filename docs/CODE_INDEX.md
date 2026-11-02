# 项目代码索引（Code Index）

> 按 SPEC 第 3 节「目录结构」编排，记录当前工程已有代码中各文件已实现的主要功能。
> 图例：✅ 已实现（标注所属 Batch）｜🔲 规划中（标注计划 Batch，尚未实现）｜📐 接口/数据空壳

---

## DesktopDoomsday/

```
DesktopDoomsday/
├── Assets/
│   ├── Scenes/
│   │   └── Main.unity                    🔲 入口场景（用户已在 Unity 手动创建，挂载 Bootstrapper + GameLoop + DebugHud）
│   ├── Scripts/
│   │   ├── Core/
│   │   │   ├── GameState.cs              ✅ Batch1  单一运行时数据源
│   │   │   ├── GameEvents.cs             ✅ Batch1  跨系统事件总线
│   │   │   ├── GameLoop.cs               ✅ Batch1  固定 tick 调度器
│   │   │   ├── SaveSystem.cs             ✅ Batch1  存档/读档/离线时间计算
│   │   │   ├── ServiceLocator.cs         ✅ Batch1  轻量服务定位器
│   │   │   ├── Bootstrapper.cs           ✅ Batch1+2 入口编排（核心循环）
│   │   │   └── DeterministicRandom.cs    ✅ Batch2  确定性随机数（xorshift32）
│   │   ├── Systems/
│   │   │   ├── SystemInterfaces.cs       📐 Batch1  各系统接口空壳（签名取自 SPEC 5.x）
│   │   │   ├── RoadSystem.cs             ✅ Batch2  公路推进 / 区域解锁
│   │   │   ├── ChestSystem.cs            ✅ Batch2  宝箱生成 / 开启 / 掉落
│   │   │   ├── InventorySystem.cs        ✅ Batch2  物资库存增删查
│   │   │   ├── SurvivorSystem.cs         ✅ Batch2  小人三需求 / 虚弱状态（不致死）
│   │   │   ├── WeatherSystem.cs          ✅ Batch3  天气轮换与数值修正
│   │   │   ├── VehicleSystem.cs          ✅ Batch3  车辆选择 / 改装线 / 改装成本查询
│   │   │   ├── DungeonSystem.cs          ✅ Batch4  副本触发 / 超时忽略 / 分支结算
│   │   │   └── AchievementSystem.cs      🔲 Batch7  成就判定
│   │   ├── Window/
│   │   │   ├── TransparentWindow.cs      ✅ Batch5  Win32 无边框/分层透明/置顶（DWM 玻璃 + 相机 RGBA0）+ OnQuit 还原（红线4）
│   │   │   ├── ClickThroughManager.cs    ✅ Batch5  全局热键 Ctrl+Shift+D 切换点击穿透 + 联动 GameLoop 5s 后台 tick
│   │   │   └── TrayManager.cs            ✅ Batch5  托盘图标/最小化隐藏/右键菜单（显示·隐藏·穿透·退出）
│   │   ├── UI/
│   │   │   ├── DebugHud.cs               ✅ Batch1+2 开发期调试覆盖层（IMGUI，~ 键开关）
│   │   │   ├── GameView.cs               ✅ 可视冲刺 可玩视图（精灵/公路滚动/车辆/小人/宝箱/天气区域色调）
│   │   │   ├── TechHud.cs                ✅ 可视冲刺 科技感 HUD 雏形（IMGUI，M 键开关，左下）
│   │   │   ├── FastForward.cs            ✅ 可视冲刺 开发期快进（F 长按 ×20 / G +60s / J +300s）
│   │   │   ├── SurvivorInfoPanel.cs      ✅ 可视冲刺 小人属性页（K 键）
│   │   │   ├── VehicleInfoPanel.cs       ✅ 可视冲刺 车辆属性页（V 键）
│   │   │   ├── MainHud.cs                ✅ Batch6  正式 HUD（科技感 IMGUI）
│   │   │   ├── DungeonPrompt.cs          🔲 Batch6  非模态副本提示
│   │   │   ├── DungeonPanel.cs           🔲 Batch6  副本分支面板
│   │   │   └── OfflineReport.cs          🔲 Batch6  离线收益报告
│   │   └── Data/
│   │       ├── Enums.cs                  ✅ Batch1  全部枚举定义
│   │       ├── GameConfig.cs             ✅ Batch1  ScriptableObject 静态配置表及配套类
│   │       └── DTO/
│   │           └── SaveData.cs           ✅ Batch1  存档 DTO
│   ├── Resources/Art/Sprites/            ✅ 可视冲刺 程序化生成 28 张像素精灵（Vehicles/Items/Chests/Weather/Zones/Survivor），置于 Resources 下以保证打包构建也能加载
│   ├── Editor/
│   │   └── DefaultGameConfigCreator.cs   ✅ 可视冲刺 菜单项创建默认 GameConfig.asset（3车/3区/4天气/2副本/10成就）
│   ├── Audio/                            🔲 音频资源（后续批次）
│   └── Resources/
│       └── GameConfig.asset              🔲 GameConfig 资产（在 Unity Inspector 按 SPEC 4.2 填写）
├── docs/
│   ├── SPEC.md                           ✅ 实现契约（SPEC 驱动开发基准）
│   └── CODE_INDEX.md                     ✅ 本文件
├── .codebuddy/                           ✅ 项目记忆 / 编码规范 / Unity Agent 定义
├── ProjectSettings/                      ✅ Unity 工程元数据（ProjectVersion.txt 等，由编辑器生成）
├── Packages/
│   └── manifest.json                     ✅ 依赖清单（Newtonsoft + Unity 内置模块）
└── .gitignore                            ✅ Unity 专用忽略规则
```

---

## 各文件主要功能（已实现部分）

### Core/ — 核心框架
- **GameState.cs** ✅：单一运行时数据源。字段严格对应 SPEC 4.1（公路/车辆/物资/小人/天气/宝箱/副本/时间/成就），并额外增加 `rngSeed`（确定性随机种子，存入存档）。构造函数初始化默认值（小人三值=100、区域=城郊、天气=晴天、种子=12345）。
- **GameEvents.cs** ✅：静态事件总线。声明 10 个跨系统事件（距离/宝箱/物资/小人状态/天气/区域/副本/离线报告/成就），并提供 null-safe `Raise*` 触发器。UI 订阅、Systems 触发，禁止回调内改他系统状态。并定义传递用的结构体 `ResourceDelta / ChestOpenResult / DungeonResult / OfflineReport`。
- **GameLoop.cs** ✅：固定 tick 调度器。`Update` 用 `Time.unscaledDeltaTime` 累加真实时间，达到间隔（前台 1.0s / 后台 5.0s）触发 `OnTick(float deltaSeconds)`。帧率与 tick 解耦，后台按真实经过时间补齐。
- **SaveSystem.cs** ✅：存档读写。`Save` 用 Newtonsoft.Json 序列化 `SaveData` 到 `persistentDataPath/save.json`，先写 `.tmp` 再 `File.Replace`（原子替换 + `.bak` 备份）。`ComputeOfflineSeconds` 计算离线秒数，上限 8 小时，时间回拨（now < lastSave）返回 0。`Load` 校验 `version`，不匹配走 `Migrate`（当前占位）。
- **ServiceLocator.cs** ✅：轻量服务定位。`Register<T> / Get<T> / TryGet<T>` 按类型存取单例，避免热路径 `Find`/`GetComponent`（编码规范红线）。
- **Bootstrapper.cs** ✅：入口 `MonoBehaviour`。`Awake` 加载存档或新建 `GameState`、注册服务、连接 `GameLoop.OnTick → TickAll`、调用 `ConfigureRenderAndFrameRate`（设 `targetFrameRate=30`、`vSyncCount=0`、主相机 `SolidColor` + 背景 `RGBA(0,0,0,0)`）。`InitNewGame` 派生命定性种子 + 初始物资（读 `GameConfig` 或兜底常量）+ 首个里程宝箱阈值。`TickAll` 按核心循环顺序调用 公路→宝箱→小人。`OnApplicationQuit` 自动保存。
- **DeterministicRandom.cs** ✅：xorshift32 确定性随机。内部状态为单个 uint，可完整序列化进 `GameState.rngSeed`，保证读档后随机序列可复现（验收项 10）。提供 `NextFloat / NextInt / Range`，不依赖 `UnityEngine.Random`。

### Systems/ — 业务系统
- **SystemInterfaces.cs** 📐：Batch 1 接口空壳。含 `IRoadSystem / IChestSystem / IInventorySystem / ISurvivorSystem / IWeatherSystem / IVehicleSystem / IDungeonSystem / IAchievementSystem`，方法签名严格取自 SPEC 5.x。
- **RoadSystem.cs** ✅：实现 `IRoadSystem`。`GetCurrentSpeed` 推导速度 =（基础 + 升级加成）× 天气修正 × 小人修正；`Tick` 推进 `totalDistanceKm` 与 `distanceSinceLastChest` 并触发 `OnDistanceAdvanced`；`TryUnlockNextZone` 达到区域里程自动解锁、不可逆、末区后无限循环；`GetZoneProgress` 返回当前区域进度；`SurvivorSpeedFactor` 在 Weakened 时按虚弱程度 0.7–0.9 插值（不降为 0）。配置缺失时回退兜底默认速度。
- **ChestSystem.cs** ✅：实现 `IChestSystem`。`Tick` 处理里程宝箱（阈值触发，开启后重随机阈值，摩托 −10%）与随机宝箱（基础概率 × 天气加成 × 车辆加成）；`Open` 按 `ResourceConfig` 权重掉落 1–3 种物资，副本宝箱额外掉蓝图碎片。全程走 `DeterministicRandom(rngSeed)`，每次调用后写回 `state.rngSeed` 保证可复现。
- **InventorySystem.cs** ✅：实现 `IInventorySystem`。库存即 `GameState` 五个 int 字段（food/water/medicine/fuel/parts）。`Add / TryConsume / GetAmount`，变化时触发 `OnInventoryChanged`。被 ChestSystem（入库）与 SurvivorSystem（消耗）复用。
- **SurvivorSystem.cs** ✅：实现 `ISurvivorSystem`。每 tick 三需求（hunger/thirst/health）按配置或兜底速率下降，叠加 Weakened ×1.3 与天气消耗修正；任一值 ≤0 时尝试消耗 1 单位对应物资补货（food→饥饿 / water→口渴 / medicine→生命），库存不足则保持 0 并进入 `Weakened`（**仅降速惩罚，不致死**）；三值全部 >60 解除 Weakened。状态切换触发 `OnSurvivorStateChanged`。
- **WeatherSystem** ✅：实现 `IWeatherSystem`。按当前区域权重随机抽取天气、持续随机秒数；仅切换顶部小图标与场景色调（`ApplyWeather` 色调修正），不弹模态窗；`Initialize` 设首个天气，`Tick` 推进 `weatherRemainingSeconds` 归零时重抽。`GetWeatherSpeedFactor / GetWeatherChestChanceMod / GetWeatherConsumptionMod` 供公路/宝箱/小人取修正。
- **VehicleSystem** ✅：实现 `IVehicleSystem`。`SelectVehicle` 选定车辆（三选一、默认皮卡、不可换）；`GetSpeedBonus` 返改装加成；`GetUpgradeCost(track)` 返回下一级所需零件/蓝图（0→1: 15 件+1 蓝；1→2: 30 件+2 蓝）；`TryUpgrade` 校验并扣除、写 `upgradeLevels`。
- **DungeonSystem** ✅：实现 `IDungeonSystem`（Batch 4 数据层；面板 UI 留 Batch 6）。`Tick(state, dt)` 推进冷却/超时（按真实 dt），冷却结束后按 `triggerChancePerTick` 概率触发（从当前区域 `dungeonPool` 等权抽取）；`TryTrigger` 为概率触发入口。`Complete(id, branch)` 校验 pending 匹配后结算：奖励 = 同期基础挂机收益 × 分支 `rewardMultiplier`（1.8–3.0）+ 蓝图碎片，确定性随机按掉落权重分配（满足 SPEC 验收项 4「2–3 倍」）；`IgnorePending` 超时/主动忽略，重置冷却，公路照常。`GameState` 新增 `autoIgnoreDungeons` / `pauseRoadDuringDungeon`（SPEC 5.6 设置项，默认 false，Batch 6 接 UI）。另提供调试用 `ForceTrigger`。
- **AchievementSystem** ✅（Batch 7）：实现 `IAchievementSystem`。`Init(state)` 订阅 `GameEvents`（里程/宝箱完成/副本完成/区域变化/物资变化）统计 `_chestsOpened`/`_dungeonsCompleted`，并对照 `GameConfig.achievements` 的 `conditionType`（distance/chestsOpened/dungeons/zones/resources）判定；满足且未解锁则写入 `state.unlockedAchievementIds`（随存档持久化）并触发 `OnAchievementUnlocked`。加载旧档时 `Check(false)` 补判已达成项（不弹提示）。空闲态（桌面宠物）不消耗 CPU。（**共 10 条成就**，由 `DefaultGameConfigCreator` 定义：初出城郊/长途跋涉/末日旅人/第一桶金/拾荒能手/仓库管理员/初探副本/副本老手/跨越荒漠/极地远征。）

### Window/ — 桌面窗口层（全部 ✅ Batch 5）
- `TransparentWindow.cs`：Win32 无边框 + `WS_EX_LAYERED` 分层透明（DWM 扩展玻璃 + 相机已设 `RGBA(0,0,0,0)`）；`Start` 延迟一帧执行（SPEC 5.9）；**默认开启 `WS_EX_TRANSPARENT` 点击穿透**（桌面宠物模式，不挡桌面操作）。**默认不强制置顶**：按 `GameConfig.windowTopmost`（默认 false）决定 —— false 时把窗口发往 `HWND_BOTTOM`，**仅置于桌面背景之上，不遮挡其它应用**；true 时 `WS_EX_TOPMOST` 始终置顶。`SetClickThrough(bool)` 切换穿透、`SetTopmost(bool)` 运行时切置顶、`SendToBottom()` 回底层、`BringToFront()` 交互时提到前台；`OnApplicationQuit`/`Restore()` 恢复原始样式（架构红线 4）。全部 `#if UNITY_STANDALONE_WIN && !UNITY_EDITOR` 守卫。
- `ClickThroughManager.cs`：低层键盘钩子（`WH_KEYBOARD_LL`）实现全局热键 **Ctrl+Shift+D** 切换点击穿透（穿透态下仍可捕获）；开启时调 `TransparentWindow.SetClickThrough(true)` 并向 `GameLoop.SetBackground(true)` 把 tick 降到 5s，关闭时反之。
- `TrayManager.cs`：自建消息专用窗口接收托盘回调，`Shell_NotifyIcon` 托盘图标；最小化时隐藏主窗口并留托盘；右键菜单（显示窗口/隐藏窗口/切换穿透/退出）；`Update` 内手动泵消息。`OnApplicationQuit` 删除图标并销毁窗口。

### UI/
- **DebugHud.cs** ✅：开发期调试覆盖层（IMGUI，零预制体）。订阅 `GameEvents` 实时显示里程/速度/区域/天气/小人三需求条形/物资/宝箱计数/FPS/游玩时长，`~` 键开关；并提供调试键 `U`(升级线0) / `P`(+20 零件) / `B`(+5 蓝图)。发布前可隐藏或删除；正式 UI 在 Batch 6 按 SPEC 第 9 节实现。
- **GameView.cs** ✅（可视冲刺 + Batch 5/8 打磨）：桌面宠物视图。`Awake` 从 `Resources/Art/Sprites` 载入精灵（`Resources.Load`）；`Start` 设相机正交并居中，`DeferLayout` 等两帧后按**实际视野动态布局**：仅建**屏幕底部路带（按屏宽平铺、无缝滚动）+ 车辆 + 小人 + 宝箱 + 左上角天气图标**，**不画整屏不透明背景**（周围全透明，桌面可见），分辨率变化自动重排。`Update` 滚动公路、车辆轮子帧、宝箱弹出。**车辆半透明（alpha 0.75）**避免遮挡应用；**天气图标缩小（0.55）+ 半透明（0.7）并移至左上角**。小人**平时回到车里（隐藏，不跟随、不挡屏）**，**仅副本待处理时出车显示走路帧并左右轻摆**（满足「遇副本测试小人移动能力」，副本结束即隐藏回车），Weakened 变红。车辆大小由 `Bootstrapper` 的 PixelPerfect 参考分辨率（1920×1080）控制。
- **TechHud.cs** ✅（可视冲刺）：科技感 HUD 雏形（IMGUI，暗玻璃+霓虹描边+条形进度），`M` 键开关、默认显示左下；只读 `GameState` 渲染里程/天气/三项需求/物资，不反写。
- **FastForward.cs** ✅（可视冲刺）：开发期快进。`F` 长按加速 ×20、`G` 跳 60s、`J` 跳 300s，便于快速验收长线数值。
- **SurvivorInfoPanel.cs / VehicleInfoPanel.cs** ✅（可视冲刺）：`K`/`V` 键独立属性页（小人三维需求+预留项 / 车型+改装线等级与成本+速度加成）。
- **UIHelper.cs** ✅（Batch 6 基础设施）：IMGUI（`OnGUI`）运行时工具集（**不依赖 `UnityEngine.UI` 程序集**——部分工程 uGUI 模块未接入会导致 CS0234，故正式 UI 走 IMGUI）。提供：纯色纹理 `DrawRect`/`DrawBorder`、文本与按钮样式构造（统一 SPEC 9.3/9.4 暗玻璃 + 霓虹青描边视觉）、**运行时从 `Assets/Resources/Fonts/msyh.ttc` 加载中文 CJK 字体**（打包内嵌，避免缺字框）、`Scale`/`S`/`R` 随屏高等比缩放（避免小屏 UI 过小）、`SetInteractive(bool)` **引用计数**切换点击穿透（多个面板开/关互不干扰；最后一个关闭时恢复穿透并把窗口发往底层），交互开启时 `BringToFront`。
- **MainHud.cs** ✅（Batch 6/8）：正式科技感 HUD（IMGUI，随屏高缩放）。左下暗玻璃面板显示里程/区域/天气/小人三需求霓虹条/物资/蓝图/成就计数（`X/总数`），底部附快捷键提示 `[I]背包 [A]成就 [H]帮助 [T]置顶`，每帧 `OnGUI` 直接读 `GameState`（轻量、只读不反写）。
- **DungeonUI.cs** ✅（Batch 6）：副本交互（IMGUI）。轮询 `pendingDungeon` 弹底部非模态提示 + 倒计时，点击「查看」打开分支面板（`GameConfig` 分支列表），选择 → `DungeonSystem.Complete` 结算，或忽略；出现时关穿透、清除后恢复；副本超时自动关闭。不暂停公路（PauseRoadDuringDungeon 默认 false）。
- **OfflineReportUI.cs** ✅（Batch 6）：启动读取 `OfflineSimulator.Pending`，离线 ≥60s 弹「离线收益结算」面板（时长/里程/小人状态/物资变化），关闭恢复穿透。
- **AchievementToast.cs** ✅（Batch 7/8）：成就解锁顶部居中吐司，**优先读 `GameConfig.achievements[].displayName`**（`DefaultGameConfigCreator` 已填 10 条中文名），无则回退内置映射；3.5s 自动消失，非交互（不切穿透，保持宠物态）。
- **AchievementGallery.cs** ✅（Batch 8）：成就图鉴（`A` 键开/关）。列出 `GameConfig.achievements` 全部条目（中文名/说明/已解锁✓或未完成☆），顶部显示 `已解锁/总数`，关闭恢复穿透。
- **InventoryPanel.cs** ✅（Batch 8）：背包/物资页（`I` 键开/关）。详细列出全部资源数量（食/水/药/油/件/蓝图）、当前车辆与改装等级、小人三项需求霓虹条与状态，关闭恢复穿透。
- **HelpPanel.cs** ✅（Batch 8）：帮助页（`H` 键开/关）。GUILayout 滚动说明玩法、如何退出（托盘右键→退出 / Alt+F4）、正式快捷键、调试快捷键、窗口图层行为、重置存档。
- **GameHotkeys.cs** ✅（Batch 8）：正式玩法快捷键路由（始终生效）：`I`背包 / `A`成就图鉴 / `H`帮助 / `T`切换窗口置顶（调 `TransparentWindow.SetTopmost`）。与 `DebugHud` 调试键分离。
- **OfflineSimulator.cs** ✅（Batch 6，Core）：`Simulate(state, seconds)` 以 5s 步长、小人 0.5× 消耗速率模拟离线 tick，不生成宝箱/副本，返回 `OfflineReport`（距离增量 + 物资变化 + 小人状态）；`Bootstrapper` 加载后调用并暂存 `Pending`。

### Data/
- **Enums.cs** ✅：枚举 `VehicleId(Pickup/Van/Motorcycle)`、`WeatherId(Sunny/Rainstorm/Sandstorm/ExtremeCold)`、`DungeonId(WarehouseRaid/ConvoyChase)`、`ChestType(Mileage/Random/Dungeon)`、`SurvivorState(Normal/Weakened)`、`ZoneId(Suburb/Desert/Tundra)`。
- **GameConfig.cs** ✅：`ScriptableObject` 静态配置表。含实体类 `VehicleConfig / ZoneConfig / WeatherConfig / ResourceConfig / SurvivorConfig / DungeonConfig / AchievementConfig`（**新增 `displayName`/`description`**）及子结构 `UpgradeTrack / ZoneWeatherWeight / ResourceDropWeight / DungeonBranch`。`Instance` 从 `Resources.Load<GameConfig>("GameConfig")` 加载。`AchievementConfig` 现已填 10 条中文名。`windowTopmost`（默认 false）控制窗口是否强制置顶。所有系统优先读此表，缺失时回退兜底常量。
- **DTO/SaveData.cs** ✅：存档 DTO `version(int) + state(GameState) + savedAtUnixMs(long)`；依赖 Newtonsoft.Json 序列化 `HashSet` 等。

---

## 批次进度一览
| Batch | 内容 | 状态 |
|------|------|------|
| 1 | 核心数据 / 循环骨架 / 事件 / 存档 | ✅ 已实现并合并 main |
| 2 | 公路 / 宝箱 / 库存 / 小人 | ✅ 已实现并合并 main |
| — | 可视冲刺：像素美术 + 可玩视图 + 科技 HUD + 快进 + 属性页 | ✅ 已实现（随 feature/visual 合并 main） |
| 3 | 天气 / 车辆 / 区域修正 | ✅ 已实现（随 feature/visual 合并 main） |
| 4 | 副本系统（数据层：触发/超时忽略/分支结算） | ✅ 已实现（当前分支 feature/batch4） |
| 5 | 桌面窗口层 | ✅ 已实现（透明/置顶/穿透热键 + 托盘；仅 Windows 独立构建生效） |
| 6 | 正式 UI（MainHud/DungeonUI/OfflineReportUI）/ 离线报告 | ✅ 已实现（IMGUI / OnGUI，零预制体，规避 uGUI 模块依赖） |
| 7 | 成就（10）/ 性能打磨 | ✅ 已实现（AchievementSystem + 吐司；轻量节流刷新，CPU 友好） |
| 8 | 体验打磨：成就图鉴 / 背包页 / 帮助页 / 窗口图层 / UI 缩放 | ✅ 已实现（A·I·H·T 快捷键；默认仅置桌面背景之上；UI 随屏高缩放；车辆半透明；小人副本后才出车） |
> 注：曾评估「账号插槽 / 新游戏菜单」多存档方案（feature/visual 内），已按用户决定**回退为单份全局 `save.json`**，保持简单。清档仍靠删除 `save.json`。
