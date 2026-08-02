# Numeria Unity/iOS 开发路线图

> 前置:设计定稿见 [numeria-game-design.md](numeria-game-design.md);核心数学玩法已经 Web 原型验证(`prototype/`)。
> 引擎:Unity(Personal License,业余/收入 < $200k 免费,合规)。目标平台:iOS(iPad 优先,横屏)。
> 仓库:https://github.com/urashima9616/numeria

## Phase 0 — 环境与仓库就绪(你 setup 完成后的第一次会话)

1. **推送现有仓库**:本地 `~/Projects/numeria`(设计文档 + Web 原型 + 12+ 提交历史)添加 remote 推送到 GitHub
2. **Unity 工程创建**:Unity Hub → **2D (URP) 模板**,LTS 版本(6000.x LTS 或 2022.3 LTS 均可,以你下载的为准);工程放仓库根 `unity/` 目录
3. **仓库配置**:Unity 专用 `.gitignore`(Library/、Temp/、UserSettings/ 等);像素图很小,暂不需要 Git LFS
4. **工程基础设置**:
   - 切换 iOS build target(验证 iOS Build Support 组件装好)
   - 安装 **2D Pixel Perfect** 包(像素完美相机)
   - 横屏锁定、目标分辨率适配 iPad
   - Filter Mode = Point、Compression = None(像素图导入规范,做成 Import Preset)

## Phase 1 — 核心战斗移植(把验证过的玩法搬进 Unity)

原则:**逻辑层直接照抄 Web 原型的设计**,它已经被测试和实玩验证过。

1. **纯 C# 逻辑层**(不依赖 UnityEngine,可单测):
   - `PuzzleGenerator`(算式填空、凑十;注入 System.Random 保持可测性)← 移植 `puzzles.js` 及其全部测试用例
   - `BattleState`(宝石、技能、护盾、易伤、胜负、零惩罚不变量)← 移植 `battle.js` 及测试
   - Unity Test Framework 跑 EditMode 测试,原 JS 测试逐条对应
2. **资产管道**:把 `sprites.js` 的像素字符网格导出为 PNG(写一次性导出脚本),Unity 按像素规范导入;后续新数灵先画字符网格再导出,保持风格统一
3. **战斗场景**:复刻原型布局(敌上右/我下左、十格阵 HP、宝石计数、三按钮),UGUI + 拖拽水晶(Unity EventSystem 的 Drag 接口,保留 8px 阈值点按/拖拽双模式)
4. **战斗演出**:移植现有特效(冲撞、受击闪白、伤害数字、火球弹道、碎盾飞溅、震屏)并升级为 Unity 粒子/动画帧;精灵加 2 帧待机动画
5. **语音方案(重要决策,推荐 A)**:
   - **A. 预烘焙语音(推荐)**:用 macOS `say -v Samantha` 或云 TTS 把所有台词批量生成音频文件打包进游戏。数字 0-20 + 固定句式,组合拼接,总量很小。零运行时依赖、离线可用、音质稳定、无 iOS 解锁问题
   - B. 原生插件调 AVSpeechSynthesizer:灵活但要维护原生桥接
6. **8-bit 音效**:命中、碎盾、答对叮咚、胜利号角(免费 CC0 音效或 sfxr 生成)

## Phase 2 — 神秘森林垂直切片(第一张完整地图)

1. Tilemap 像素地图(森林场景,复用背景美术语言)
2. 玩家移动:点触目的地寻路(低龄友好,优于虚拟摇杆)
3. 草丛遇敌 → 战斗场景切换(过场动画)
4. 收服机制(友谊谜题)+ 队伍管理(简化:1 只主战 + 收服图鉴)
5. 经验/升级(属性数字跳动 + 语音)
6. 存档系统(JSON 本地存档:队伍、等级、图鉴、掌握度数据)
7. 数学宝箱 → 装备(+2 Attack 心算确认)

## Phase 3 — 内容与养成闭环

1. 15 只数灵全阵容(像素立绘 + 属性/技能配置)
2. 进化系统(等级 + 进化石 + 进化试炼 + 蜕变动画)
3. 三张地图 + 传送门(静寂山脉 20 以内/凑十,天空城图形规律)
4. 数据驱动配置落地(JSON in StreamingAssets,兑现 DLC 架构承诺)
5. 自适应难度引擎 + 错题变形复现(间隔重复)
6. 家长面板(PIN 门 + 掌握度热图 + 时长提醒)

## Phase 4 — 上机与分发

1. Unity → Xcode 导出 → 真机调试
2. **分发方式(注意成本)**:
   - **免费 Apple ID**:可直接装到自己的 iPad,但签名 **7 天过期**需重装——日常给儿子玩会烦
   - **Apple Developer Program($99/年)**:TestFlight 分发,90 天有效自动更新,体验好得多
   - 建议:真机调试阶段用免费 ID,玩法稳定后再决定是否上 TestFlight
3. iPad 实测清单:触控手感、字号可读性、语音清晰度、5 岁玩家独立完成一场战斗

## 各阶段完成标准

| 阶段 | Definition of Done |
|---|---|
| P0 | 仓库推送完成;Unity 空工程能 Build 出 iOS Xcode 工程 |
| P1 | Unity 里能完整打一场 Duplirock 战斗,逻辑测试全绿,有音效语音 |
| P2 | 森林图能逛、能遇敌、能收服、能升级、能存档 |
| P3 | 三图全通,15 只数灵齐,家长面板可用 |
| P4 | 儿子的 iPad 上稳定运行 |
