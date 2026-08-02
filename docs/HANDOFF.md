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
- 孩子水平:10 以内加减熟练、20 以内练习中、喜欢图形规律(→ 三地图难度阶梯的依据)

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
- `PuzzleGenerator`:加法填空 / 减法填空(`A − □ = C`)/ 翻倍(`N + N = □`,SlotIsResult)/ 凑十(可指定 target,如 Boss 盾 12;**恰好一组解**保证)
- `BattleState`:宝石经济(+2/回合,上限 8)、护盾减半、破盾易伤×2(2 回合)、`PlayerAttackBonus`、零惩罚不变量(有测试锁定)
- `Progress`:等级(XpToNext = Level×10,每级 ATK+1)、图鉴、宝箱、道具、进化状态、当前地图、出战位 —— JsonUtility 序列化,**新字段必须带默认值**保证旧存档兼容
- `GridMap`:ASCII 地图解析('.'草地 'T'树 'b'草丛 'C'宝箱 'P'传送门 'S'出生)+ BFS 寻路
- `GameData`:数灵定义;`PlayerMon(id, evolved)` 出战位技能组;进化 Addmander→Sumdrake
- 测试:`unity/Assets/Tests/EditMode/`,**32 个**(最后一次 headless 验证为 28,之后加的 4 个未跑过 headless,接手先跑一遍)

### Game 层(`Numeria.Game`)
- **全程序化 UGUI,零场景文件**——所有界面代码搭建,SampleScene 只是空壳,`BattleBootstrap` 用 `RuntimeInitializeOnLoadMethod` 拉起 `MapController`
- `MapController`:多地图(`MapDefs` 注册表:forest tier1 / mountains tier2)、点触 BFS 移动、35% 草丛遇敌、宝箱谜题、Boss 守门、进化试炼(3 题)、菜单入口
- `BattleController`:`Init(enemy, progress, tier, battleBg, onEnd)` 回调式;收服(Catch)按钮;演出协程(冲撞/受击/伤害数字/火球/碎盾/震屏)
- `PuzzleUi`:谜题遮罩(战斗/宝箱/试炼共用),tier 选题,拖拽+点按双模式水晶(12px 阈值防手指抖动),零惩罚重试 + 数块提示(≤20 自动两行十格阵)
- `MenuUi`:TEAM/ITEMS/SETTINGS 三 tab,TEAM 双栏 master-detail,可滚动
- `Voice`:预烘焙语音播放,`VoiceKeys.Sanitize` 文本→文件名(**必须与 bake 脚本规则一致**);`Voice.Enabled` 全局开关(存档持久化)
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
  - ✅ 静寂山脉(tier2 题型:20 以内加减/翻倍/凑十二)、Doublit、Duplirock Elder Boss
  - ✅ 进化系统全链(进化石宝箱 + Lv.5 + 三题试炼 + 蜕变演出,全 UI/语音跟随)
  - ✅ 菜单(tab 化)、出战位切换、道具栏、重复捕捉转经验
  - ✅ 用户 AI 生成美术管线(`generated/` 约定 + NUMERIA Battle Asset Pack 全面接入战斗)
  - ⬜ 未做:蔚蓝天空城(图形/规律题型,需要新谜题交互:图案选择而非数字水晶)、15 只数灵剩余阵容、JSON 数据驱动落地(现在数值在 GameData/MapDefs 硬编码)、自适应难度引擎(错题变形复现/隐形升降档)、家长面板(PIN + 掌握度热图)
- ⬜ **P4**:iOS 构建、真机、TestFlight(免费 Apple ID 7 天签名 vs $99/年,已告知用户)

### 正在收敛的工作(接手第一件事)
战斗画面按参考图(`~/Documents/battle_design.png`)重构后处于**视觉打磨循环**中:用户截图 → 修排版 → 再截图。最后一轮修了名牌内缩/横幅宽度/按钮固定宽(`7a61472`),之后并行会话又重构了排版与字体(至 `35cd368`)。**用户对当前效果尚未最终满意**,预期还有几轮微调。菜单同理(参考图是用户提供的 mockup)。

### 未提交的工作区内容
- `unity/Assets/Casual Game Sounds U6/` —— 用户新导入的**音效包,还没接进游戏**(战斗命中/答对/胜利音效目前全空缺,这是现成的高价值任务)
- `sumdrake_large_icon.png.meta` 改动(用户似乎加了 Sumdrake 大图)

## 5. 资产管线(全部约定式,零代码接新资产)

- **手绘像素**:改 `prototype/js/sprites.js` 字符网格 → `node tools/export-sprites.mjs` → PNG 落到 `Resources/Art/Sprites/`
- **语音**:台词加进 `tools/bake-voice.sh` → 跑脚本(macOS `say`,Samantha,-r 150)→ wav 落到 `Resources/Voice/`;**C# 里说的每句台词必须有对应烘焙**,`VoiceKeys.Sanitize` = 脚本的 key 规则(小写、非字母数字折叠为 `-`);现有 ~490 条,含全部题目读法组合
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

1. **接入 Casual Game Sounds U6 音效包**(工作区已有):命中/答对叮咚/答错软音/碎盾/胜利号角/按钮点击;做一个 `Sfx` 静态类 + 约定路径,别忘了 Voice 与 SFX 的音量平衡
2. **战斗/菜单视觉收敛**:继续用户截图循环,直到满意
3. **提交工作区**(音效包 + sumdrake meta)
4. **蔚蓝天空城**:图形规律题型需要新的 PuzzleUi 形态(图案水晶:形状序列选下一个/对称配对)——Core 出 `PatternPuzzle` 生成器(可测)+ UI 出图案按钮;地图 tier3、新数灵 Mirrowl(设计文档有像素画描述)
5. **JSON 数据驱动**:GameData/MapDefs → StreamingAssets JSON(设计文档承诺的 DLC 架构)
6. **自适应难度**:Progress 记每题型正误 → 隐形升降档 + 错题变形复现(设计文档 §9)
7. **家长面板**:菜单加 PIN 门 tab(掌握度热图、时长)
8. **P4 iOS**:横屏锁定 ProjectSettings、Xcode 导出、真机;分发方案见 roadmap

## 9. 用户偏好速记

- 反馈风格直接("非常丑"=布局崩了,别玻璃心,修就行);喜欢参考图驱动开发,会自己用 AI 生成美术投喂
- 接受"原型简陋后期打磨"的节奏,但对**孩子体验细节**敏感(误触、语音、零惩罚)
- Obsidian 笔记规范:如果被要求写"笔记",要 HTML+Markdown 双版本(见用户全局 CLAUDE.md)
