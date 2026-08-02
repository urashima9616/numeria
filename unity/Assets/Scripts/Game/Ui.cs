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

        private static Font _pixelFont;
        private static bool _pixelFontLoaded;

        /// <summary>像素字体(Press Start 2P, OFL),缺失时回退系统字体。</summary>
        public static Font DefaultFont
        {
            get
            {
                if (!_pixelFontLoaded)
                {
                    _pixelFont = Resources.Load<Font>("Fonts/PressStart2P");
                    _pixelFontLoaded = true;
                }
                return _pixelFont != null ? _pixelFont : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            }
        }

        /// <summary>Press Start 2P 同字号远宽于普通字体,全局缩放以适配既有布局。</summary>
        private static int ScaleFontSize(int size) =>
            _pixelFont != null || !_pixelFontLoaded ? Mathf.Max(9, Mathf.RoundToInt(size * 0.62f)) : size;

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

        public static Text Label(Transform parent, string name, string text, int size, Color color,
            TextAnchor anchor = TextAnchor.MiddleCenter, FontStyle style = FontStyle.Bold)
        {
            var rt = Node(parent, name);
            var t = rt.gameObject.AddComponent<Text>();
            t.font = DefaultFont;
            t.text = text;
            t.fontSize = ScaleFontSize(size);
            t.lineSpacing = 1.25f; // 像素字体行距偏紧,放宽一点
            t.color = color;
            t.alignment = anchor;
            // 像素字体单权重,伪加粗会糊;回退字体时保留原样式
            t.fontStyle = _pixelFont != null ? FontStyle.Normal : style;
            t.horizontalOverflow = HorizontalWrapMode.Overflow;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            return t;
        }

        public static Button Btn(Transform parent, string name, string label, int fontSize)
        {
            var img = Img(parent, name, PlateBg);
            var btn = img.gameObject.AddComponent<Button>();
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
    }
}
