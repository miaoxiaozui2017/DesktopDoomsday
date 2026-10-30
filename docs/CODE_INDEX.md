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
│   │   │   ├── MainHud.cs                🔲 Batch6  正式 HUD（科技感 uGUI）
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
- **AchievementSystem** 🔲：接口已在 `SystemInterfaces.cs` 定义，实现在 Batch 7。

### Window/ — 桌面窗口层（全部 ✅ Batch 5）
- `TransparentWindow.cs`：Win32 无边框 + `WS_EX_LAYERED` 分层透明（DWM 扩展玻璃 + 相机已设 `RGBA(0,0,0,0)`）+ `WS_EX_TOPMOST` 置顶；`Start` 延迟一帧执行（SPEC 5.9）；**默认开启 `WS_EX_TRANSPARENT` 点击穿透**（桌面宠物模式，不挡桌面操作）；`SetClickThrough(bool)` 切换穿透；`OnApplicationQuit`/`Restore()` 恢复原始样式，移除分层/穿透/置顶（架构红线 4）。全部 `#if UNITY_STANDALONE_WIN && !UNITY_EDITOR` 守卫，仅独立构建生效，编辑器 Play 不受影响。
- `ClickThroughManager.cs`：低层键盘钩子（`WH_KEYBOARD_LL`）实现全局热键 **Ctrl+Shift+D** 切换点击穿透（穿透态下仍可捕获）；开启时调 `TransparentWindow.SetClickThrough(true)` 并向 `GameLoop.SetBackground(true)` 把 tick 降到 5s，关闭时反之。
- `TrayManager.cs`：自建消息专用窗口接收托盘回调，`Shell_NotifyIcon` 托盘图标；最小化时隐藏主窗口并留托盘；右键菜单（显示窗口/隐藏窗口/切换穿透/退出）；`Update` 内手动泵消息。`OnApplicationQuit` 删除图标并销毁窗口。

### UI/
- **DebugHud.cs** ✅：开发期调试覆盖层（IMGUI，零预制体）。订阅 `GameEvents` 实时显示里程/速度/区域/天气/小人三需求条形/物资/宝箱计数/FPS/游玩时长，`~` 键开关；并提供调试键 `U`(升级线0) / `P`(+20 零件) / `B`(+5 蓝图)。发布前可隐藏或删除；正式 UI 在 Batch 6 按 SPEC 第 9 节实现。
- **GameView.cs** ✅（可视冲刺 + Batch 5 打磨）：桌面宠物视图。`Awake` 从 `Resources/Art/Sprites` 载入精灵（`Resources.Load`，编辑器与 Windows 构建通用）；`Start` 设相机正交并居中，`DeferLayout` 等两帧（待 PixelPerfect 设定 `orthographicSize`）后按**实际视野动态布局**：仅建**屏幕底部路带（按屏宽动态平铺、无缝滚动）+ 车辆（轮子落路面）+ 小人（站车左侧路面）+ 宝箱 + 右上角天气图标**，**不画整屏不透明背景**（周围全透明，桌面可见）。分辨率变化自动重排。`Update` 滚动公路、车辆轮子帧、宝箱弹出。小人**常驻待机**（idle 帧），**副本待处理时切换走路帧并左右轻摆**（满足「遇副本测试小人移动能力」），Weakened 变红。车辆大小由 `Bootstrapper` 的 PixelPerfect 参考分辨率（1920×1080）控制。
- **TechHud.cs** ✅（可视冲刺）：科技感 HUD 雏形（IMGUI，暗玻璃+霓虹描边+条形进度），`M` 键开关、默认显示左下；只读 `GameState` 渲染里程/天气/三项需求/物资，不反写。
- **FastForward.cs** ✅（可视冲刺）：开发期快进。`F` 长按加速 ×20、`G` 跳 60s、`J` 跳 300s，便于快速验收长线数值。
- **SurvivorInfoPanel.cs / VehicleInfoPanel.cs** ✅（可视冲刺）：`K`/`V` 键独立属性页（小人三维需求+预留项 / 车型+改装线等级与成本+速度加成）。
- **MainHud / DungeonPrompt / DungeonPanel / OfflineReport** 🔲：Batch 6 正式 UI（uGUI + Canvas Scaler，分辨率自适应），订阅事件不轮询。

### Data/
- **Enums.cs** ✅：枚举 `VehicleId(Pickup/Van/Motorcycle)`、`WeatherId(Sunny/Rainstorm/Sandstorm/ExtremeCold)`、`DungeonId(WarehouseRaid/ConvoyChase)`、`ChestType(Mileage/Random/Dungeon)`、`SurvivorState(Normal/Weakened)`、`ZoneId(Suburb/Desert/Tundra)`。
- **GameConfig.cs** ✅：`ScriptableObject` 静态配置表。含实体类 `VehicleConfig / ZoneConfig / WeatherConfig / ResourceConfig / SurvivorConfig / DungeonConfig / AchievementConfig` 及子结构 `UpgradeTrack / ZoneWeatherWeight / ResourceDropWeight / DungeonBranch`。`Instance` 从 `Resources.Load<GameConfig>("GameConfig")` 加载（对应 `Resources/GameConfig.asset`）。所有系统优先读此表，缺失时回退兜底常量，保证空配置也能运行。
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
| 6 | 正式 UI / 离线报告 | 🔲 待开发 |
| 7 | 成就 / 性能打磨 | 🔲 待开发 |
> 注：曾评估「账号插槽 / 新游戏菜单」多存档方案（feature/visual 内），已按用户决定**回退为单份全局 `save.json`**，保持简单。清档仍靠删除 `save.json`。
