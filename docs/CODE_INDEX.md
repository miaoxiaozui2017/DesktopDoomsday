# 项目代码索引（Code Index）

> 按 SPEC 第 3 节「目录结构」编排，记录当前工程已有代码中各文件已实现的主要功能。
> 图例：✅ 已实现（标注所属 Batch）｜🔲 规划中 ｜📐 接口/数据空壳 ｜ ⚫ 已下线

---

## DesktopDoomsday/

```
DesktopDoomsday/
├── Assets/
│   ├── Scenes/
│   │   └── Main.unity                    ✅ 入口场景（挂载 Bootstrapper + GameLoop + DebugHud）
│   ├── Scripts/
│   │   ├── Core/
│   │   │   ├── GameState.cs              ✅ Batch1  单一运行时数据源
│   │   │   ├── GameEvents.cs              ✅ Batch1  跨系统事件总线（ChestOpenResult 含遇怪标记）
│   │   │   ├── GameLoop.cs               ✅ Batch1  固定 tick 调度器（前台1s/后台5s）
│   │   │   ├── SaveSystem.cs             ✅ Batch1  存档/读档/离线时间计算
│   │   │   ├── ServiceLocator.cs         ✅ Batch1  轻量服务定位器
│   │   │   ├── Bootstrapper.cs           ✅ Batch1+2 入口编排
│   │   │   ├── OfflineSimulator.cs       ✅ Batch6  离线收益结算（0.3×消耗 + 拾荒）
│   │   │   └── DeterministicRandom.cs    ✅ Batch2  确定性随机数（xorshift32）
│   │   ├── Systems/
│   │   │   ├── SystemInterfaces.cs       📐 Batch1  各系统接口（ISurvivorSystem 含 FillAllNeeds）
│   │   │   ├── RoadSystem.cs             ✅ Batch2  公路推进 / 区域解锁
│   │   │   ├── ChestSystem.cs            ✅ Batch2  宝箱生成/开启/掉落 + 开箱遇怪
│   │   │   ├── InventorySystem.cs        ✅ Batch2  物资库存增删查
│   │   │   ├── SurvivorSystem.cs         ✅ Batch2  小人三需求/虚弱/一键补满
│   │   │   ├── WeatherSystem.cs          ✅ Batch3  天气轮换与数值修正
│   │   │   ├── VehicleSystem.cs          ✅ Batch3  车辆选择/改装/成本查询
│   │   │   ├── DungeonSystem.cs          ✅ Batch4  副本触发/超时/分支结算/收益估算
│   │   │   └── AchievementSystem.cs      ✅ Batch7  成就判定（10 条）
│   │   ├── Window/
│   │   │   ├── TransparentWindow.cs      ✅ Batch5  Win32 无边框/分层透明/穿透/底层/帧率联动/强制激活
│   │   │   ├── ClickThroughManager.cs    ✅ Batch5  全局热键 Ctrl+Shift+D 切穿透
│   │   │   └── TrayManager.cs            ⚫ Batch5（已下线，Bootstrapper 不再挂载）
│   │   ├── UI/
│   │   │   ├── UIHelper.cs               ✅ Batch6  IMGUI 工具集（缩放/字体/穿透引用计数/EnsureForeground）
│   │   │   ├── MainHud.cs                ✅ Batch6/8 正式状态面板 + 副本横幅 + 遇怪警示条 + 6按钮行
│   │   │   ├── GameView.cs               ✅ 可视冲刺 桌面宠物视图（底部路带+车+小人+宝箱）
│   │   │   ├── DebugHud.cs               ✅ Batch1+2 调试覆盖层 + 验收观测区（~ 键开关，D/C/R）
│   │   │   ├── FastForward.cs            ✅ 可视冲刺 开发期快进（F/G/J）
│   │   │   ├── DungeonUI.cs              ✅ Batch6  副本分支面板（锚定 MainHud 上方）
│   │   │   ├── OfflineReportUI.cs        ✅ Batch6  离线收益结算弹窗（启动即弹自动可点）
│   │   │   ├── AchievementToast.cs       ✅ Batch7  成就解锁吐司
│   │   │   ├── AchievementGallery.cs     ✅ Batch8  成就图鉴（A 键）
│   │   │   ├── InventoryPanel.cs         ✅ Batch8  背包/物资页（I 键，含一键补满）
│   │   │   ├── SurvivorInfoPanel.cs      ✅ Batch8+打磨 小人属性页（K 键，含一键补满+关闭按钮）
│   │   │   ├── VehicleInfoPanel.cs       ✅ Batch8+打磨 车辆属性页（V 键，含升级按钮+关闭按钮）
│   │   │   ├── HelpPanel.cs              ✅ Batch8  帮助页（H 键）
│   │   │   ├── GameHotkeys.cs            ✅ Batch8  正式快捷键路由（I/A/H）
│   │   │   └── TechHud.cs                ⚫ 可视冲刺（已下线，Bootstrapper 不再挂载）
│   │   └── Data/
│   │       ├── Enums.cs                  ✅ Batch1  全部枚举定义
│   │       ├── GameConfig.cs             ✅ Batch1  ScriptableObject 静态配置表
│   │       └── DTO/SaveData.cs           ✅ Batch1  存档 DTO
│   ├── Resources/Art/Sprites/            ✅ 程序化生成 28 张像素精灵
│   ├── Resources/Fonts/msyh.ttc          ✅ 中文字体（打包内嵌）
│   ├── Scripts/Editor/
│   │   ├── DefaultGameConfigCreator.cs   ✅ 菜单项创建默认 GameConfig
│   │   ├── PixelArtImporter.cs           ✅ 像素美术导入工具
│   │   └── CleanupMissingScripts.cs      ✅ 一键清理场景 Missing Script 组件
│   └── Resources/GameConfig.asset        ⚠ 需用户在 Unity 菜单生成/覆盖
├── docs/
│   ├── SPEC.md                           ✅ 实现契约
│   └── CODE_INDEX.md                     ✅ 本文件
└── .gitignore                            ✅ Unity 专用忽略规则
```

---

## 各文件主要功能

### Core/ — 核心框架
- **GameState.cs** ✅：单一运行时数据源。字段严格对应 SPEC 4.1，含 `rngSeed` 保证确定性随机。
- **GameEvents.cs** ✅：静态事件总线。`ChestOpenResult` 含 `monsterEncounter` 标记。
- **GameLoop.cs** ✅：固定 tick 调度，前台 1s / 后台 5s（`SetBackground` 切换）。
- **SaveSystem.cs** ✅：JSON 原子写（`.tmp` + `File.Replace`），离线上限 8h。
- **Bootstrapper.cs** ✅：入口编排。`Awake` 加载存档/注册服务/连接 tick；`EnsureVisualLayer` 自动挂载全部 UI 组件（TechHud 已下线）；`EnsureWindowLayer` 挂载透明窗口+穿透热键（**TrayManager 已移除**）；`EnsurePixelPerfect` 反射设 1920×1080；启动时移除 AudioListener（构建禁用 Audio 模块）；跑离线结算 + 注册成就系统。
- **OfflineSimulator.cs** ✅：离线结算（**0.3× 消耗**，不产宝箱/副本）。**路边拾荒**（每整小时 +4 食/+5 水/+2 药）抵消消耗后有少量正收益；零件每 2h +1 封顶 4。8h 离线三项物资各 +10 左右。

### Systems/ — 业务系统
- **RoadSystem.cs** ✅：公路推进、区域解锁、虚弱速度修正（0.7–0.9 插值）。
- **ChestSystem.cs** ✅：宝箱生成/开启/掉落。掉落收紧为 1–2 种×1–2 单位；里程箱阈值 3km。**开箱遇怪**：概率 = 区域 `monsterChancePerChest` × 天气 `monsterChanceMultiplier`；遇怪扣三需求各 10–20 点、`Math.Max(1f)` 钳制不致死。`ChestOpenResult.monsterEncounter` 标记。
- **InventorySystem.cs** ✅：物资增删查，变化触发 `OnInventoryChanged`。
- **SurvivorSystem.cs** ✅：三需求下降 + 自动补货（≤40 触发）+ 虚弱状态。`FillAllNeeds(GameState)`：按库存消耗食/水/药各 1 补满未满项，虚弱同步解除。
- **WeatherSystem.cs** ✅：天气轮换与数值修正。
- **VehicleSystem.cs** ✅：车辆选择/改装/成本查询。
- **DungeonSystem.cs** ✅：副本触发/超时/分支结算。`TryEstimatePendingReward`：供 UI 展示预计物资收益区间。
- **AchievementSystem.cs** ✅：对照 `GameConfig.achievements` 判定 10 条成就。

### Window/ — 桌面窗口层
- **TransparentWindow.cs** ✅：Win32 无边框/分层透明/穿透。默认 `HWND_BOTTOM`（仅桌面背景之上）。**`SetClickThrough` 加状态变化守卫**（`if (_clickThrough==on) return`，支持每帧安全调用零开销）+ **帧率联动**（穿透 20fps / 交互 30fps）。`ForceActivate`：AttachThreadInput 技巧绕过 Windows 防抢焦点限制，启动瞬间也能抢前台。`BringToFront`/`SendToBottom`/`Activate`/`SetTopmost`（后者 API 保留但 T 键已移除）。
- **ClickThroughManager.cs** ✅：Ctrl+Shift+D 全局热键切穿透，穿透态 tick 降 5s。
- **TrayManager.cs** ⚫：托盘图标/最小化隐藏/右键菜单。**已按用户要求从 Bootstrapper 移除**，文件保留但不挂载。

### UI/
- **UIHelper.cs** ✅：IMGUI 工具集。缩放（`Screen.height/1080f`）、中文字体（`Resources.Load("Fonts/msyh")`）、`SetInteractive` 引用计数穿透切换（交互开启时 `BringToFront` + `ForceActivate`）。**`EnsureForeground`**：每帧持续确保穿透关闭（只调 `SetClickThrough(false)`，状态守卫零开销），不碰引用计数，不调重操作——专供启动即弹的面板对抗延迟透明化覆盖。
- **MainHud.cs** ✅：正式状态面板（左下）。标题「末日公路」；行式布局，行高 = 字号 × ~1.8（杜绝竖向截字）；里程/区域/天气/三需求霓虹条/物资/蓝图/成就计数；**快捷按钮行 6 个**（背包/成就/小人/车辆/帮助/退出）；**面板上方浮动层**：副本横幅（琥珀边框+倒计时+查看入口，常驻）→ 遇怪警示条（红色，4s）。`StackTopY` 静态属性供 DungeonUI 分支面板向上锚定。
- **GameView.cs** ✅：桌面宠物视图。底部路带 + 车辆（半透明 0.75）+ 天气图标（左上、缩小半透明）+ 小人（仅副本时出车，其余隐藏）。
- **DebugHud.cs** ✅：调试覆盖层。**默认隐藏**（`_show=false`，按 `~` 开关）；显示里程/速度/区域/天气/物资/FPS/副本日志。**验收观测区**（右上角红色独立面板）：① CPU%<3%/MEM<300MB 实时采样；② 后台 tick 按 R 快照对比里程误差；③ 确定性 rngSeed + 最近 5 次开箱掉落日志。**已移除 U/P/B 测试键**，仅保留 D（触发副本）/C（完成分支0）/R（快照）。
- **FastForward.cs** ✅：F 长按 ×20 / G +60s / J +300s。
- **DungeonUI.cs** ✅：副本分支面板。横幅由 MainHud 画在状态面板上方；DungeonUI 只管分支选择面板，锚定 `MainHud.StackTopY` 继续向上堆叠。面板打开期间每帧 `EnsureForeground` 保证可点。
- **OfflineReportUI.cs** ✅：离线收益结算弹窗（随屏缩放）。启动即弹时每帧 `EnsureForeground` 对抗延迟透明化覆盖，`ForceActivate` 绕过 Windows 防抢焦点限制，直接可点「收下」。
- **AchievementToast.cs** ✅：成就解锁顶部吐司（3.5s 自动消失）。
- **AchievementGallery.cs** ✅：成就图鉴（A 键），列出 10 条中文名/说明/解锁状态。
- **InventoryPanel.cs** ✅：背包/物资页（I 键），含资源清单、车辆改装等级、小人三需求条 + 「一键补满」按钮 + 关闭按钮。
- **SurvivorInfoPanel.cs** ✅：小人属性页（K 键），含三需求条 + 「一键补满」按钮 + 物资库存 + 关闭按钮。`Instance` + `Toggle()` 与 InventoryPanel 同模式。
- **VehicleInfoPanel.cs** ✅：车辆属性页（V 键），含两条改装线各自独立「升级」按钮（显示成本，不足/满级置灰）+ 关闭按钮。`Instance` + `Toggle()` 与 InventoryPanel 同模式。
- **HelpPanel.cs** ✅：帮助页（H 键），玩法说明含新数值/怪物/拾荒；退出方式仅留按钮+Alt+F4；正式快捷键 I/A/H/K/V + Ctrl+Shift+D。
- **GameHotkeys.cs** ✅：正式快捷键路由（I/A/H），**T 置顶已移除**。
- **TechHud.cs** ⚫：旧 HUD 雏形。**已下线，Bootstrapper 不再挂载**。

### Data/
- **GameConfig.cs** ✅：ScriptableObject 配置表。`ZoneConfig` 含 `monsterChancePerChest`；`WeatherConfig` 含 `monsterChanceMultiplier`；`SurvivorSystem` 含 `FillAllNeeds`。`windowTopmost`（默认 false）决定窗口初始层级。
- **SaveData.cs** ✅：存档 DTO（version + state + savedAtUnixMs）。

### Editor/
- **DefaultGameConfigCreator.cs** ✅：菜单 `DesktopDoomsday → 创建默认 GameConfig`。基线数值：初始 35/35/20/80/10；baseChestChance 0.014；里程箱 3km；掉落 1-2种×1-2单位；零件权重 4%；开箱遇怪（城郊5%/荒漠15%/冻原25% × 天气系数）；10 条成就中文名/说明。
- **PixelArtImporter.cs** ✅：像素美术批量导入工具。
- **CleanupMissingScripts.cs** ✅：菜单 `DesktopDoomsday → 清理 Missing Script 组件`。一键扫描场景所有 GameObject，用 `GameObjectUtility.RemoveMonoBehavioursWithMissingScript` 移除失效脚本引用，报告清理数量。

---

## 批次进度一览
| Batch | 内容 | 状态 |
|------|------|------|
| 1 | 核心数据 / 循环骨架 / 事件 / 存档 | ✅ 合并 main |
| 2 | 公路 / 宝箱 / 库存 / 小人 | ✅ 合并 main |
| — | 可视冲刺：像素美术 + 可玩视图 + 科技 HUD + 快进 + 属性页 | ✅ 合并 main |
| 3 | 天气 / 车辆 / 区域 | ✅ 合并 main |
| 4 | 副本系统数据层 | ✅ 合并 main |
| 5 | 桌面窗口层（透明/穿透；**托盘已移除**） | ✅ 合并 main |
| 6 | 正式 UI / 离线报告 | ✅ 合并 main |
| 7 | 成就（10）+ 性能打磨 | ✅ 合并 main |
| 8 | 体验打磨（图鉴/背包/帮助/缩放/一键补满/升级按钮/副本横幅/退出按钮/数值重做/怪物机制/拾荒） | ✅ 合并 main |
| 打磨 | 6 轮反馈修复（行高/窗口焦点/帧率联动/按钮扩展/离线数值/CleanupMissingScripts） | ⏳ 待提交 |

> 单份全局 `save.json`。清档靠删除 `save.json`。
