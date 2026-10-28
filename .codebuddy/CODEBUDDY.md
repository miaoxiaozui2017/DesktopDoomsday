# Desktop Doomsday — 项目记忆

引擎：Unity 2022.3 LTS / 2D URP / C#
平台：Windows 先行
架构：单一 GameState 单例 + 信号/事件驱动 UI
性能目标：CPU <3%，内存 <300MB，静止时 10 FPS
命名规范：PascalCase 公开成员，_camelCase 私有字段，I 前缀接口
禁止：在 Update 中每帧 new 对象、Find、GetComponent
存档：JSON，Application.persistentDataPath

## 四条硬架构约束（不可绕过）
1. 小人不可死亡（Weakened 不致死，仅降速惩罚）
2. 副本可忽略（20s 超时自动清除，不阻断公路）
3. 后台 tick 不中断（最小化/穿透时 5s tick，按真实时间补齐）
4. 窗口穿透可恢复（退出移除 WS_EX_LAYERED / WS_EX_TRANSPARENT）

## 视觉风格硬约束（不可绕过，详见 SPEC 第 9 节）
- 美术：高清像素风（High-Res Pixel Art），主相机 Pixel Perfect，精灵 Point 过滤、无压缩。
- 配色：鲜明，暗底（#0B0E14/#121826）+ 科技青（#22D3EE/#00E5FF）+ 琥珀（#FFB000）；调色板集中管理，禁止散落硬编码颜色。
- UI：科技感 HUD（暗色玻璃面板 + 霓虹描边 + 等宽字体 + 霓虹条形进度）；DebugHud(IMGUI) 仅开发期调试，不计入正式风格。
- 适用：所有涉及可见产出的批次（Batch 5/6/7 为主，2–4 临时可视化也建议参考）。

## 外部依赖
- Newtonsoft.Json（com.unity.nuget.newtonsoft-json）：GameState 含 HashSet，需用其序列化存档。

## 分批计划
- Batch 1 核心数据+循环+存档（已完成骨架）
- Batch 2 公路/宝箱/库存/小人
- Batch 3 天气/车辆/区域
- Batch 4 副本系统
- Batch 5 桌面窗口层
- Batch 6 UI 与离线报告
- Batch 7 成就+打磨
