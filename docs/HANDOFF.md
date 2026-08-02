# Numeria 开发交接文档(Handoff Note)

> 日期:2026-08-02。本文档面向接手开发的 agent/工程师,涵盖项目现状、架构约定、踩坑记录与后续路线。
> 必读配套:[numeria-game-design.md](numeria-game-design.md)(游戏设计定稿)、[unity-roadmap.md](unity-roadmap.md)(P0–P4 路线图)。

---

## 1. 项目是什么

给用户 5 岁儿子做的**数学学习宝可梦式 RPG**(iOS 目标,Unity 开发,英文游戏 + 英文语音旁白)。核心设计原则(不可违背):

- **零惩罚**:答错数学题绝不扣玩家 HP,技能以 basePower 释放,数块提示引导重试一次
- **数学是魔法不是测验**:谜题以咒语/封印/符文形式存在于世界观内
- **三层数学融入**:环境浸泡(数字元素)→ 决策层(宝石经济心算)→ 高潮层(显式谜题)
- **听得懂就能玩**:全部台词有预烘焙英文语音,不依赖识字量
- 孩子水平:5 岁 / Kindergarten;三关严格使用 10/20/30 以内加减,并让图形、对称、规律同步递进

## 2. 仓库与环境

- 本地:`~/Projects/numeria`;远端:https://github.com/urashima9616/numeria(main 分支,gh CLI 已认证)
- Unity **6000.5.6f1**(`/Applications/Unity/Hub/Editor/6000.5.6f1`),iOS Build Support 已装,工程在 `unity/`
- Unity Personal License;Node ≥ 18(brew 装的,零 npm 依赖)
- 结构:
  - `docs/` 设计文档、路线图、本文档
  - `prototype/` 已验证的 Web 战斗原型(玩法真理源,逻辑照它移植;含 `prototype/js/sprites.js` = 手绘像素资产的字符网格源)
  - `tools/` 资产管线脚本(见 §5)
  - `unity/Assets/Scripts/{Core,Game,Editor}` + `unity/Assets/Tests/EditMode`
  - `unity/Assets/Resources/` 运行时资产(注意:Resources 里的**所有东西都会打进包**,文档类放 `unity/Assets/ArtDocs/`)

## 3. 代码架构(关键约定)

### Core 层(`Numeria.Core`,纯 C#,noEngineReferences,全部 TDD)
- `Rng`:确定性 LCG(与 Web 原型逐位一致),所有生成函数注入 rng
- `PuzzleGenerator`:三关 10/20/30 上限;加减填空、凑目标、连加、点数/比较、图形识别、AB/ABC/ABCD 规律、对称、旋转、数列;候选答案唯一
- `BattleState`:宝石经济、数字护盾、破盾易伤;正式 ATK/DEF 公式 `max(1, ATK − DEF + 1 + [-1,1])`,小幅可控波动、零惩罚不变量
- `Progress`:save schema v4;等级(XpToNext = Level×10,每级 ATK+1、每偶数级 DEF+1)、每家族独立成长、图鉴/宝箱/道具/进化/地图 —— **新字段必须带默认值和迁移**
- `GridMap`:ASCII 地图解析('.'草地 'T'树 'b'草丛 'C'宝箱 'P'传送门 'S'出生)+ BFS 寻路
- `GameData`:首发 15 只数灵的统一图鉴与六条进化线;三条御三家为三段(Lv.8/Lv.15),三条地图野生线为两段;每家族配置独立数学亲和
- 测试:`unity/Assets/Tests/EditMode/`,**59 个**;隔离工程副本最后一次完整 headless 为 **59/59**

### Game 层(`Numeria.Game`)
- **全程序化 UGUI,零场景文件**——所有界面代码搭建,SampleScene 只是空壳,`BattleBootstrap` 用 `RuntimeInitializeOnLoadMethod` 拉起 `MapController`
- `MapController`:三地图(`forest` / `mountains` / `sky`)、点触 BFS 移动、35% 草丛遇敌、宝箱谜题、Boss + 三题综合开门试炼、家族亲和进化试炼、菜单入口
- `BattleController`:`Init(enemy, progress, tier, battleBg, onEnd)`;双方状态牌显示 ATK/DEF,普通怪 HP 按关卡在 8–12 / 14–20 / 22–30 浮动,Boss HP 20 / 36 / 54
- `PuzzleUi`:谜题遮罩共用;第一关 10 内加减+图形/对称/规律,第二关 20 内并加入三项连加/转向/ABC,第三关 30 内并加入四项连加/2–5 步数列/ABCD;传送门三题必含算术
- `MenuUi`:TEAM/ITEMS/SETTINGS 三 tab,TEAM 双栏 master-detail,可滚动
- `Voice`:预烘焙语音播放,`VoiceKeys.Sanitize` 文本→文件名(**必须与 bake 脚本规则一致**);`Voice.Enabled` 全局开关(存档持久化)
- `Sfx` / `Music`:独立短音效通道 + Dynamic Music 双通道交叉淡化;地图/战斗/Boss/进化切换 mood,语音播放时自动 duck,Voice/SFX/Music 分别持久化开关
- `SpriteLib`:资产加载约定(见 §5);`SaveSystem`:persistentDataPath JSON
- **文字系统近期已被并行会话改为 TextMeshPro + Jersey 10 字体**(`Ui.Label` 返回 TextMeshProUGUI,共享动态 SDF 字体;Jersey10 缺失时回退 PressStart2P → 系统字体)。**不要退回 legacy Text**

### Editor 层
- `PixelArtImporter`:`Resources/Art` 与 `Resources/generated` 自动 Sprite/Point/不压缩/AlphaIsTransparency;**素材包 `UI/` 目录强制 100 PPU**(见 §6 坑 3),9-slice border 表与包内 JSON 一致
- `PackReimportGuard`:域重载后自检素材包导入参数,不对就强制重导入(自愈时序坑)

## 4. 当前进度(对照 roadmap)

- ✅ **P0** 环境/仓库/工程
- ✅ **P1** 战斗核心移植(逻辑层 + 全套演出 + 语音)
- ✅ **P2** 神秘森林垂直切片(探索/遇敌/收服/升级/存档/宝箱)
- 🔶 **P3 进行中**:
  - ✅ 三段 Kindergarten 难度:10/20/30 内加减 + 同步递进的图形、对称、规律、旋转、数列
  - ✅ 进化系统全链(御三家 Lv.8/Lv.15、野生线 Lv.5 + 里程碑进化石 + 家族亲和三题试炼 + 蜕变演出)
  - ✅ 菜单(tab 化)、出战位切换、道具栏、重复捕捉转经验
  - ✅ 用户 AI 生成美术管线(`generated/` 约定 + NUMERIA Battle Asset Pack 全面接入战斗)
  - ✅ 蔚蓝天空城:独立浮空遗迹路线、Mirrowl/Symmetrix、规律/对称/旋转/数字序列、Sky/Boss 音乐
  - ✅ 首发 15 只数灵:9 只御三家三段 + 6 只地图野生两段,全部有统一高清图标、技能、成长与进化语音
  - ✅ 音频系统:10 种 SFX + 6 种本地 Dynamic Music mood + 语音 duck/独立开关
  - ✅ 攻防平衡:15 物种 DEF、升级防御成长、普通怪 HP 区间、Boss 20/36/54 曲线、±1 伤害波动
  - ⬜ 未做:JSON 数据驱动落地(现在数值在 GameData/MapDefs 硬编码)、自适应难度引擎(错题变形复现/隐形升降档)、家长面板(PIN + 掌握度热图)
- ⬜ **P4**:iOS 构建、真机、TestFlight(免费 Apple ID 7 天签名 vs $99/年,已告知用户)

### 正在收敛的工作(接手第一件事)
战斗画面按参考图(`~/Documents/battle_design.png`)重构后处于**视觉打磨循环**中:用户截图 → 修排版 → 再截图。最后一轮修了名牌内缩/横幅宽度/按钮固定宽(`7a61472`),之后并行会话又重构了排版与字体(至 `35cd368`)。**用户对当前效果尚未最终满意**,预期还有几轮微调。菜单同理(参考图是用户提供的 mockup)。

### 未提交的工作区内容
- 用户当前 Unity 会持续刷新多张 `Resources/generated/**/*.png.meta`;这些导入参数改动不属于功能提交,后续 agent 不应擅自覆盖或提交

## 5. 资产管线(全部约定式,零代码接新资产)

- **手绘像素**:改 `prototype/js/sprites.js` 字符网格 → `node tools/export-sprites.mjs` → PNG 落到 `Resources/Art/Sprites/`
- **语音**:台词加进 `tools/bake-voice.sh` → 跑脚本(macOS `say`,Samantha,-r 150)→ wav 落到 `Resources/Voice/`;**C# 里说的每句台词必须有对应烘焙**,`VoiceKeys.Sanitize` = 脚本规则;现有 **982 条有效 WAV**,覆盖 0–30 加减全部读法和图形题
- **AI 生成图**(用户负责生成,放 `Resources/generated/`):
  - `{id}_large_icon.png` → 菜单详情/回退链
  - **NUMERIA_Unity_Battle_Assets/**(结构化素材包):`Characters/{Id}_Battle_Front|Back.png`(战斗立绘)、`UI/`(面板/按钮三态/血条/图标,9-slice)、`Backgrounds/`;包内有 README 和 Unity_Import_Settings.json
  - 加载优先级封装在 `SpriteLib`:pack → generated 单图 → 16px 像素图,**永远有回退**
  - 缺的图:Duplirock/Doublit/Sumdrake 的 Battle_Front、Sumdrake Battle_Back、山脉/天空城战斗背景、UI Icons 里的 Shield
  - 带色幕的生成图用 `node tools/key-out-bg.mjs <in> <out>` 抠透明(边缘泛洪 + 去色晕)

## 6. 踩过的坑(接手必读,别再踩)

1. **Unity 编辑器占用工程时 headless 命令全部失败**(same project 锁)。工作模式:用户经常开着编辑器 → headless 测试跑不了就明说"本轮由编辑器验证",让用户看 Console 红错;编辑器空闲时再补跑
2. **同批刷新的资源导入可能先于新导入器脚本编译**——改 AssetPostprocessor 后删 meta 强制重导入也可能用旧规则。已用 `PackReimportGuard` 自愈,同类问题照此模式
3. **9-slice PPU 坑**:UGUI 切片边角 = border × (Canvas referencePPU 100 ÷ 精灵 PPU)。像素风惯用 16 PPU 会把边角放大 6.25 倍挤塌面板。**UI 精灵必须 100 PPU**
4. **LayoutGroup flexible 泄漏**:行内放 HorizontalLayoutGroup(childForceExpand)会让该行对外汇报可伸缩,抢占 VerticalLayoutGroup 的剩余空间。固定行必须 `LayoutElement.flexibleHeight = 0`
5. **childControl=false 时 LayoutElement 尺寸不生效**(子元素退回默认 100×100)。内层布局组要 `childControlWidth/Height = true` + forceExpand false
6. **headless 测试命令**(编辑器关闭时):
   ```
   /Applications/Unity/Hub/Editor/6000.5.6f1/Unity.app/Contents/MacOS/Unity -batchmode \
     -projectPath <repo>/unity -runTests -testPlatform EditMode \
     -testResults /tmp/r.xml -logFile /tmp/l.log
   # 然后 grep 'total=.*passed=' /tmp/r.xml;编译错误 grep 'error CS' 日志
   ```
7. 用户 shell 的 `rm` 是 `rm -i` 别名,脚本里用 `rm -f`;Node 26 的 `node --test <目录>` 坏的,用 glob(package.json 已配好)
8. Web Speech/浏览器音频需要用户手势解锁——Unity 版已绕开(预烘焙 wav),但 Web 原型如再动要记得
9. 语音新句子:**先加 bake 脚本跑掉,再写 C#**,否则静默无声

## 7. 工作流约定

- Core 逻辑一律 TDD(测试与 Web 原型语义对齐);UI 层靠用户编辑器验证 + 截图迭代
- 提交信息英文、讲清 why;commit 尾部带 Co-Authored-By + Claude-Session 链接;每个功能块 commit + push
- 用户是唯一visual QA:改 UI 后让他截图,按截图逐项修;参考图驱动(菜单和战斗都有用户提供的 mockup)
- 语言:与用户中文交流;游戏内文案/代码注释见现状(游戏英文,注释中文)

## 8. 建议的下一步(按价值排序)

1. **战斗/菜单/新谜题视觉收敛**:继续用户截图循环,尤其检查 iPad 4:3 下点数、比较、对称与旋转题
2. **JSON 数据驱动**:GameData/MapDefs → StreamingAssets JSON(设计文档承诺的 DLC 架构)
3. **自适应难度**:Progress 记每题型正误 → 隐形升降档 + 错题变形复现(设计文档 §9)
4. **家长面板**:菜单加 PIN 门 tab(掌握度热图、时长)
5. **P4 iOS**:横屏锁定 ProjectSettings、Xcode 导出、真机;分发方案见 roadmap

## 9. 用户偏好速记

- 反馈风格直接("非常丑"=布局崩了,别玻璃心,修就行);喜欢参考图驱动开发,会自己用 AI 生成美术投喂
- 接受"原型简陋后期打磨"的节奏,但对**孩子体验细节**敏感(误触、语音、零惩罚)
- Obsidian 笔记规范:如果被要求写"笔记",要 HTML+Markdown 双版本(见用户全局 CLAUDE.md)
