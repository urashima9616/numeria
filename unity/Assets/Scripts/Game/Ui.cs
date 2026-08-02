using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Numeria.Game
{
    /// <summary>程序化 UGUI 构建辅助。整个战斗界面用代码搭建,不依赖场景文件。</summary>
    public static class Ui
    {
        public static Color Hex(string hex)
        {
            ColorUtility.TryParseHtmlString(hex, out var c);
            return c;
        }

        public static readonly Color Ink = Hex("#263238");
        public static readonly Color PlateBg = Hex("#fdf6e3");
        public static readonly Color Border = Hex("#2e4a24");
        public static readonly Color CellOn = Hex("#66bb6a");
        public static readonly Color GemOrange = Hex("#b06e00");
        public static readonly Color ShieldBlue = Hex("#1565c0");

        private static Font _uiFont;
        private static bool _uiFontLoaded;
        private static TMP_FontAsset _defaultTmpFont;

        /// <summary>
        /// 全局 UI 字体源。Jersey 10 是所有页面的唯一正常运行字体；保留旧字体和系统字体作为
        /// 资源损坏时的防御性回退，避免整套 UI 变成空白。
        /// </summary>
        public static Font DefaultFont
        {
            get
            {
                if (!_uiFontLoaded)
                {
                    _uiFont = Resources.Load<Font>("Fonts/Jersey10-Regular");
                    if (_uiFont == null) _uiFont = Resources.Load<Font>("Fonts/PressStart2P");
                    _uiFontLoaded = true;
                }
                return _uiFont != null ? _uiFont : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            }
        }

        /// <summary>
        /// 所有运行时文字共享同一个动态 SDF 字体资产，保证地图、菜单、谜题和战斗的
        /// 字形、抗锯齿与字距完全一致。
        /// </summary>
        public static TMP_FontAsset DefaultTmpFont
        {
            get
            {
                if (_defaultTmpFont != null) return _defaultTmpFont;
                var source = DefaultFont;
                _defaultTmpFont = TMP_FontAsset.CreateFontAsset(source);
                if (_defaultTmpFont != null) _defaultTmpFont.name = $"{source.name} UI SDF";
                return _defaultTmpFont;
            }
        }

        public static RectTransform Node(Transform parent, string name)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            return rt;
        }

        public static Image Img(Transform parent, string name, Color color)
        {
            var rt = Node(parent, name);
            var img = rt.gameObject.AddComponent<Image>();
            img.color = color;
            return img;
        }

        public static Image SpriteImg(Transform parent, string name, Sprite sprite)
        {
            var rt = Node(parent, name);
            var img = rt.gameObject.AddComponent<Image>();
            img.sprite = sprite;
            img.color = Color.white;
            return img;
        }

        public static TextMeshProUGUI Label(Transform parent, string name, string text, int size, Color color,
            TextAnchor anchor = TextAnchor.MiddleCenter)
        {
            var rt = Node(parent, name);
            var t = rt.gameObject.AddComponent<TextMeshProUGUI>();
            t.font = DefaultTmpFont;
            t.text = text;
            t.fontSize = size;
            t.fontStyle = FontStyles.Normal;
            t.color = color;
            t.alignment = ToTmpAlignment(anchor);
            t.textWrappingMode = TextWrappingModes.NoWrap;
            t.overflowMode = TextOverflowModes.Overflow;
            t.raycastTarget = false;
            t.extraPadding = true;
            return t;
        }

        /// <summary>
        /// 战斗场景保留 DisplayLabel 这一语义入口，但字体、字号规则和渲染方式与全局 Label 一致。
        /// 这样无需复制文字配置，也能让调用代码继续区分展示标题和普通控件。
        /// </summary>
        public static TextMeshProUGUI DisplayLabel(Transform parent, string name, string text, int size, Color color,
            TextAnchor anchor = TextAnchor.MiddleCenter)
        {
            return Label(parent, name, text, size, color, anchor);
        }

        private static TextAlignmentOptions ToTmpAlignment(TextAnchor anchor)
        {
            switch (anchor)
            {
                case TextAnchor.UpperLeft: return TextAlignmentOptions.TopLeft;
                case TextAnchor.UpperCenter: return TextAlignmentOptions.Top;
                case TextAnchor.UpperRight: return TextAlignmentOptions.TopRight;
                case TextAnchor.MiddleLeft: return TextAlignmentOptions.Left;
                case TextAnchor.MiddleRight: return TextAlignmentOptions.Right;
                case TextAnchor.LowerLeft: return TextAlignmentOptions.BottomLeft;
                case TextAnchor.LowerCenter: return TextAlignmentOptions.Bottom;
                case TextAnchor.LowerRight: return TextAlignmentOptions.BottomRight;
                default: return TextAlignmentOptions.Center;
            }
        }

        public static Button Btn(Transform parent, string name, string label, int fontSize)
        {
            var img = Img(parent, name, PlateBg);
            var btn = Sfx.WireClick(img.gameObject.AddComponent<Button>());
            var colors = btn.colors;
            colors.disabledColor = new Color(1f, 1f, 1f, 0.45f);
            btn.colors = colors;
            AddOutline(img.gameObject);
            var text = Label(img.transform, "Label", label, fontSize, Ink);
            Stretch(text.rectTransform);
            return btn;
        }

        /// <summary>粗边框(像素风 UI 描边的近似)。</summary>
        public static void AddOutline(GameObject go)
        {
            var outline = go.AddComponent<Outline>();
            outline.effectColor = Border;
            outline.effectDistance = new Vector2(3, -3);
        }

        public static void Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        /// <summary>anchor/pivot 同一点 + 指定尺寸与偏移。</summary>
        public static void Place(RectTransform rt, Vector2 anchor, Vector2 offset, Vector2 size)
        {
            rt.anchorMin = anchor;
            rt.anchorMax = anchor;
            rt.pivot = anchor;
            rt.anchoredPosition = offset;
            rt.sizeDelta = size;
        }

        /// <summary>以给定锚点作为视觉中心，适合角色、图标等自由构图元素。</summary>
        public static void PlaceCentered(RectTransform rt, Vector2 anchor, Vector2 offset, Vector2 size)
        {
            rt.anchorMin = anchor;
            rt.anchorMax = anchor;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = offset;
            rt.sizeDelta = size;
        }
    }
}
