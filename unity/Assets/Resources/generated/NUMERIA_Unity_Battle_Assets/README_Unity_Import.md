# NUMERIA Unity Battle Asset Pack

## 文件内容

- `Characters/`: 两个透明背景战斗 Sprite（512×512）
- `Backgrounds/`: 2048×1152 战斗背景
- `UI/Panels/`: 可复用状态框、回合框、命令栏与通用面板
- `UI/Buttons/`: Normal / Selected / Pressed 三种按钮状态
- `UI/Bars/`: HP 条外框与填充
- `UI/Icons/`: Tackle、Flame Formula、Catch 与 Gem 图标
- `Unity_Import_Settings.json`: 推荐导入参数与 9-Slice Border

## Unity 导入建议

1. 除背景外，全部设为 `Texture Type: Sprite (2D and UI)`。
2. 像素素材使用 `Filter Mode: Point (no filter)`、`Compression: None`、关闭 `Generate Mip Maps`。
3. 透明素材打开 `Alpha Is Transparency`。
4. UI 面板在 Sprite Editor 中使用 `Unity_Import_Settings.json` 里的 Border，并把 Image Type 设为 `Sliced`。
5. Canvas Scaler 建议 `Scale With Screen Size`，Reference Resolution 为 `1920 × 1080`，Match 为 `0.5`。
6. UI 文本不要烘焙进面板，建议使用独立像素字体 TextMeshPro 元素，便于本地化和动态数值更新。
7. iOS 触控按钮建议保持至少 88×88 pt 的交互区域；视觉图标可以更小，但 Button RectTransform 不要缩小。

## 颜色

核心色板位于 `NUMERIA_Palette.png`：parchment、cream、forest green、sage、amber、orange、brown、HP green。
