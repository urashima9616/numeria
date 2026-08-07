# Numeria 开发交接文档(Handoff Note)

> 日期:2026-08-05。本文档面向接手开发的 agent/工程师,涵盖项目现状、架构约定、踩坑记录与后续路线。
> 必读配套:[numeria-game-design.md](numeria-game-design.md)(游戏设计定稿)、[unity-roadmap.md](unity-roadmap.md)(P0–P4 路线图)。

---

## 1. 项目是什么

给用户 5 岁儿子做的**数学学习宝可梦式 RPG**(iOS 目标,Unity 开发,英文游戏 + 英文语音旁白)。核心设计原则(不可违背):

- **零惩罚**:答错数学题绝不扣玩家 HP,技能以 basePower 释放,数块提示引导重试一次
- **数学是魔法不是测验**:谜题以咒语/封印/符文形式存在于世界观内
- **三层数学融入**:环境浸泡(数字元素)→ 决策层(宝石经济心算)→ 高潮层(显式谜题)
- **听得懂就能玩**:全部台词有预烘焙英文语音,不依赖识字量
- 孩子水平:5 岁 / Kindergarten;第一关使用标准核心的 10 以内加减,第二至六关使用家长要求的 20 以内扩展;后期通过图形、对称、规律、拆分与等式提升难度,不再扩大到 30/40

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
- `PuzzleGenerator`:统一算术边界为第一关 10、后续关卡 20,且所有显式加减生成器内部再次硬限制到 20;十几数优先用 `10 + ones`、向前数最多 5、退回 10 或向后数最多 5 的 Kindergarten 支架;加减填空、凑目标、连加、点数/比较、图形识别、形状×颜色规律、四类图案匹配、等式平衡、正反数字路径与数列;候选答案唯一、减法无负数。`NumberWord` 仍安全覆盖 0–99
- `BattleState`:宝石经济、数字护盾、破盾眩晕一回合 + 每次破盾各自触发一次双倍 + 命中后护盾重置;`SkillResult.BreakBonusApplied` 是 UI 与测试的单一判据;正式 ATK/DEF 公式 `max(1, ATK − DEF + 1 + [-1,1])`,小幅可控波动、零惩罚不变量
- `Progress`:save schema v9;Lv.99 上限、物种成长曲线、捕捉个体 HP/ATK/DEF 偏移、动态经验、每家族独立成长、独占饰品装备、金币/限量库存、Digit Crystal 主线与冒险记录 —— **新字段必须带默认值和迁移**
- `GridMap`:语义 ASCII 地图解析('.'草地 'T'树 'b'草丛 'C'宝藏 'P'出口 'S'出生
  '~'水域 '='道路 'B'桥 '#'悬崖 'L'地标)+ BFS 寻路;水域/悬崖/树木/地标不可通行
- `GameData`:141 只数灵、48 条进化线;最新扩展为 Electric/Rock/Dragon/Fire 各 4 条三段线;各物种配置基础经验、HP/ATK/DEF 成长与数学亲和
- 测试:`unity/Assets/Tests/EditMode/`;当前四个 C# assembly 已单独编译通过,Unity EditMode **121/121**、Node 原型 **15/15**

### Game 层(`Numeria.Game`)
- **全程序化 UGUI,零场景文件**——所有界面代码搭建,SampleScene 只是空壳,`BattleBootstrap` 用 `RuntimeInitializeOnLoadMethod` 拉起 `MapController`
- `MapController`:六张 32×18 地图、点触 BFS + 跟随相机、带权多物种生态、宝箱谜题、全宝箱后 Boss 图标与三题开门试炼、掉落与进化试炼。六章统一由 `PaintedTerrainRenderer` 使用 Tiles and Hexes Painted Terrain:按底部锚点缩放到语义格、下方行覆盖上方行、主题配色 + 半透明窄道路;RPG Worlds Caves/Tiny Swords 只在 Painted catalog 不完整时回退
- `BattleController`:`Init(enemy, progress, tier, battleBg, onEnd)`;双方状态牌显示 ATK/DEF,普通怪与 Boss HP 均按关卡和物种成长合理浮动
- `SkillDef` 保存独立 `IconResource` + `SkillVisualKind`;11 条家族各有专属像素图标与战斗弹道/命中节奏,不要再硬编码 `Flame_Formula`
- `PuzzleUi`:谜题遮罩共用;第一关 10 内加减+图形/彩色规律/数字路径,第二关 20 内并加入三项连加/等式平衡/ABC-AAB,第三关 30 内并加入四项连加/正反数字路径/四类图案匹配,第四关扩展到 40 并混合高阶题型;旋转题已彻底淘汰;传送门三题必含算术
- `MenuUi`:TEAM/ITEMS/RECORDS/SAVES/SETTINGS 五 tab,TEAM 可为每只数灵装备/卸下饰品,SAVES 提供十槽存取;SETTINGS 返回主菜单前询问保存/不保存/取消
- `Voice`:预烘焙语音播放,`VoiceKeys.Sanitize` 文本→文件名(**必须与 bake 脚本规则一致**);`Voice.Enabled` 全局开关(存档持久化)
- `Sfx` / `Music`:独立短音效通道 + 双通道交叉淡化;九个 mood 已改用 8-bit Jukebox Lite 选曲,语音播放时自动 duck,Voice/SFX/Music 分别持久化开关
- `SpriteLib`:资产加载约定(见 §5);`SaveSystem`:persistentDataPath 十槽 JSON + 旧单文件无损迁移,当前槽自动保存;标题页明确提供新游戏/读取游戏,新游戏直接覆盖选中槽的干净 `Progress`(不要恢复旧版单文件状态)
- **文字系统近期已被并行会话改为 TextMeshPro + Jersey 10 字体**(`Ui.Label` 返回 TextMeshProUGUI,共享动态 SDF 字体;Jersey10 缺失时回退 PressStart2P → 系统字体)。**不要退回 legacy Text**

### Editor 层
- `PixelArtImporter`:`Resources/Art` 与 `Resources/generated` 自动 Sprite/Point/不压缩/AlphaIsTransparency;**素材包 `UI/` 目录强制 100 PPU**(见 §6 坑 3),9-slice border 表与包内 JSON 一致
- `PackReimportGuard`:域重载后自检素材包导入参数,不对就强制重导入(自愈时序坑)
- `TinySwordsCatalogBuilder`:将本地 `Assets/Terrain Tile Hex Samples`(以及可选旧素材)写入运行时 Sprite 引用表;导入/升级素材包后运行 **Numeria → Rebuild Map Asset Catalogs**
- `MapPreviewExporter`:使用与游戏相同的 `PaintedTerrainRenderer` + `MapArt` 逻辑离屏渲染六章全景图;菜单 **Numeria → Export Map Previews**,默认输出 `/tmp/numeria-map-previews`;批处理预览必须保留图形管线,不要加 `-nographics`

## 4. 当前进度(对照 roadmap)

- ✅ **P0** 环境/仓库/工程
- ✅ **P1** 战斗核心移植(逻辑层 + 全套演出 + 语音)
- ✅ **P2** 神秘森林垂直切片(探索/遇敌/收服/升级/存档/宝箱)
- 🔶 **P3 进行中**:
  - ✅ Kindergarten 难度:第一章 10 以内核心加减,第二至六章 20 以内扩展;用项数、拆分、图形、彩色规律、图案匹配、等式平衡、数字路径/数列递进
  - ✅ 进化系统全链(御三家 Lv.8/Lv.15、野生线 Lv.5 + 里程碑进化石 + 家族亲和三题试炼 + 蜕变演出)
  - ✅ 菜单(tab 化)、出战位切换、道具栏、99 只队伍上限与满员放走/替换流程；非首发伙伴可按等级换金币或当前出战伙伴经验（金币 = Lv.+2，经验 = 2×Lv.+4）
  - ✅ 捕捉成长继承:保留野生等级、进化阶段及战斗时 HP/ATK/DEF;个体偏移随升级/进化和存档延续;同家族更强个体可选择收编或转换为 125% 捕捉经验
  - ✅ 用户 AI 生成美术管线(`generated/` 约定 + NUMERIA Battle Asset Pack 全面接入战斗)
  - ✅ 蔚蓝天空城:独立浮空遗迹路线、Mirrowl/Symmetrix、多维规律/镜像顺序/数字路径与序列、Sky/Boss 音乐
  - ✅ 谜题稳定性与多样性:第四关 31–40 英文数字越界已修复;Find Pattern 混合形状/颜色/缺口位置,Match Pattern 有精确/镜像/只看形状/只看颜色;破盾循环每次均可靠触发并显示 2×
  - ✅ 141 只数灵 / 48 条线:在既有阵容上新增 Electric、Rock、Dragon、Fire 各 4 条三段线;全部有统一高清图标、技能、成长与进化语音
  - ✅ 狂热沙漠(Fever Desert):第四章地图、20 家族进化态生态、商人 Nia、守护者 Solara、Solar Totalisk Boss 与第四枚水晶
  - ✅ 黑暗矿山与地底隧道:两张独立 Painted Terrain 深层地图、暗色山岩/深水/火山视觉、商人 Mara/Rune、守护者 Vesper/Echo、两位新 Boss 与第五/第六枚水晶
  - ✅ 音频系统:10 种 SFX + 9 首 8-bit Jukebox Lite 本地 mood + 语音 duck/独立开关
  - ✅ 99级平衡:各物种 HP/ATK/DEF 成长、动态等级差经验、普通怪 HP 92%–108% 波动、动态 Boss 倍率、±1 伤害波动
  - ✅ 扩图与系统:带权生态、山脉重绘、全宝箱 Boss 条件、战斗消耗品/掉落、RECORDS 存档
  - ✅ 血量捕捉曲线:普通野生数灵全血量可尝试,按钮实时显示 10%–95% 成功率,低血量按幂曲线提高
  - ✅ 饰品与存档:每只数灵 2/3/4 格独占饰品装备、破盾眩晕与一次双倍循环、10 个存取档槽
  - ✅ 第一轮美术演出:11 套家族技能图标与专属 VFX;Lucas 像素主角已接入地图,生成提示词见 `docs/generated-visual-assets.md`
  - ✅ 探索经济:每图 4 个主题数学符文、战斗金币、六位商人挑战、永久限量库存、消耗品/饰品/进化石平衡,见 `docs/economy-design.md`
  - ✅ Lucas 主线:标题页与有声开场、六位 Crystal Guardian 的 Boss 前后对白、六枚 Digit Crystal、旧存档至 v9 无损兼容及结局节点,见 `docs/main-story.md`
  - ✅ 存档入口重构:标题页选择新游戏/读取游戏与十槽存档;新游戏真正清空宝箱等世界状态;设置页以保存提示返回主菜单
  - ✅ 六章地图重构:六章全部使用真正的 Tiles and Hexes Painted Terrain 方形素材;256×384 底锚点按语义格归一化并逐行排序,道路另加窄叠层保持可读;每章拥有独立的森林/雪山/海岛/沙漠/矿山/火山组合;六张 32×18 布局的每图 5 个宝藏、1 个章节出口和 1 个地标均有可达性测试
  - ⬜ 未做:JSON 数据驱动落地(现在数值在 GameData/MapDefs 硬编码)、自适应难度引擎(错题变形复现/隐形升降档)、家长面板(PIN + 掌握度热图)
- ⬜ **P4**:iOS 构建、真机、TestFlight(免费 Apple ID 7 天签名 vs $99/年,已告知用户)

### 正在收敛的工作(接手第一件事)
战斗画面按参考图(`~/Documents/battle_design.png`)重构后处于**视觉打磨循环**中:用户截图 → 修排版 → 再截图。最后一轮修了名牌内缩/横幅宽度/按钮固定宽(`7a61472`),之后并行会话又重构了排版与字体(至 `35cd368`)。**用户对当前效果尚未最终满意**,预期还有几轮微调。菜单同理(参考图是用户提供的 mockup)。

### 工作区注意事项
- Unity 可能刷新第三方素材或无关资源的 `.meta`;提交前始终按功能范围审查,不要把 `Assets/RPGW_Caves` / `Assets/Tiny Swords` 等授权源包加入 Git

## 5. 资产管线(全部约定式,零代码接新资产)

- **手绘像素**:改 `prototype/js/sprites.js` 字符网格 → `node tools/export-sprites.mjs` → PNG 落到 `Resources/Art/Sprites/`
- **语音**:台词加进 `tools/bake-voice.sh` → 跑脚本(macOS `say`,Samantha,-r 150)→ wav 落到 `Resources/Voice/`;**C# 里说的每句台词必须有对应烘焙**,`VoiceKeys.Sanitize` = 脚本规则;覆盖 0–20 加减读法、图形题、捕捉选择、141 个形态、进化、经济与六章主线对白
- **音乐**:`tools/install-jukebox-music.sh` 从本地授权的 8-bit Jukebox Lite 同步九首选曲到 ignored `Resources/Music/Jukebox`;`--restore-dynamic` 可将旧曲同步到同一运行槽;原 `LocalStore` 保持不变,完整曲目/署名见 `docs/music-attribution.md`
- **AI 生成图**(使用内置 ImageGen + imagegen skill,放 `Resources/generated/`):
  - `{id}_large_icon.png` → 菜单详情/回退链
  - `Backgrounds/{Fever_Desert,Dark_Mines,Underground_Tunnels}_2048x1152.png`
  - `Story/guardian_{solara,vesper,echo}.png`、`Economy/merchant_{nia,mara,rune}.png`
  - `Skills/{fairy_glimmer,dragon_spiral,electric_bolt,grass_bloom}.png`
  - 新三段进化表可用 `tools/import-evolution-sheet.sh` 抠背景并拆为三个 512px 图标
  - **NUMERIA_Unity_Battle_Assets/**(结构化素材包):`Characters/{Id}_Battle_Front|Back.png`(战斗立绘)、`UI/`(面板/按钮三态/血条/图标,9-slice)、`Backgrounds/`;包内有 README 和 Unity_Import_Settings.json
  - 加载优先级封装在 `SpriteLib`:pack → generated 单图 → 16px 像素图,**永远有回退**
  - 缺的图:Duplirock/Doublit/Sumdrake 的 Battle_Front、Sumdrake Battle_Back、山脉/天空城战斗背景、UI Icons 里的 Shield
  - 带色幕的生成图用 `node tools/key-out-bg.mjs <in> <out>` 抠透明(边缘泛洪 + 去色晕)
- **地图素材分层**:`Terrain Tile Hex Samples` 才是真正的 Tiles and Hexes Painted Terrain(不是 `Resources/Cainos`);它和 RPGW_Caves/Tiny Swords 都是本地素材包,已在 `.gitignore` 排除,不能强行提交。六章正常运行只需前者;导入后运行 `Numeria/Rebuild Map Asset Catalogs`;仓库只保存引用 catalog、逐行绘制/选图代码、地图布局和测试

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
10. **Painted Terrain 不是 Cainos**:`Resources/Cainos` 是另一套 Pixel Art Top Down Basic,不能拿它冒充 Tiles and Hexes。新 clone 必须从有许可的 Asset Store 帐号把 `Tiles and Hexes: 2D Painted Terrain Samples` 导入默认 `Assets/Terrain Tile Hex Samples`,再重建 map catalog。不要复制任何第三方源 PNG 到公开仓库

## 7. 工作流约定

- Core 逻辑一律 TDD(测试与 Web 原型语义对齐);UI 层靠用户编辑器验证 + 截图迭代
- 提交信息英文、讲清 why;commit 尾部带 Co-Authored-By + Claude-Session 链接;每个功能块 commit + push
- 用户是唯一visual QA:改 UI 后让他截图,按截图逐项修;参考图驱动(菜单和战斗都有用户提供的 mockup)
- 语言:与用户中文交流;游戏内文案/代码注释见现状(游戏英文,注释中文)

## 8. 建议的下一步(按价值排序)

1. **战斗/菜单/新谜题视觉收敛**:继续用户截图循环,尤其检查 iPad 4:3 下点数、比较、图案条、等式平衡与数字路径题
2. **JSON 数据驱动**:GameData/MapDefs → StreamingAssets JSON(设计文档承诺的 DLC 架构)
3. **自适应难度**:Progress 记每题型正误 → 隐形升降档 + 错题变形复现(设计文档 §9)
4. **家长面板**:菜单加 PIN 门 tab(掌握度热图、时长)
5. **P4 iOS**:横屏锁定 ProjectSettings、Xcode 导出、真机;分发方案见 roadmap

## 9. 用户偏好速记

- 反馈风格直接("非常丑"=布局崩了,别玻璃心,修就行);喜欢参考图驱动开发,会自己用 AI 生成美术投喂
- 接受"原型简陋后期打磨"的节奏,但对**孩子体验细节**敏感(误触、语音、零惩罚)
- Obsidian 笔记规范:如果被要求写"笔记",要 HTML+Markdown 双版本(见用户全局 CLAUDE.md)
