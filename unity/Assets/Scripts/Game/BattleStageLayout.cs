using UnityEngine;
using UnityEngine.UI;

namespace Numeria.Game
{
    /// <summary>Background-space foot placements, projected through the same cover crop as the artwork.</summary>
    [ExecuteAlways]
    public sealed class BattleStageLayout : MonoBehaviour
    {
        private RectTransform _root, _background, _plate;
        private Image _player, _enemy;
        private RectTransform _playerShadow, _enemyShadow;
        private Vector2 _lastSize;
        private string _resource;
        private float _aspect;

        public static Vector2 CoverSize(Vector2 viewport, float aspect)
        {
            float height = Mathf.Max(viewport.y, viewport.x / aspect);
            return new Vector2(height * aspect, height);
        }

        // UV origin is bottom-left. Each point is a standing surface, not a sprite center.
        public static Vector2 FootUv(string resource, bool enemy)
        {
            if (resource.Contains("Azure_Sky")) return enemy ? new Vector2(.655f, .485f) : new Vector2(.28f, .295f);
            if (resource.Contains("Fever_Desert")) return enemy ? new Vector2(.635f, .505f) : new Vector2(.28f, .325f);
            if (resource.Contains("Dark_Mines")) return enemy ? new Vector2(.64f, .465f) : new Vector2(.28f, .32f);
            if (resource.Contains("Silent_Peaks")) return enemy ? new Vector2(.65f, .50f) : new Vector2(.28f, .32f);
            if (resource.Contains("Underground")) return enemy ? new Vector2(.64f, .49f) : new Vector2(.28f, .32f);
            return enemy ? new Vector2(.65f, .50f) : new Vector2(.28f, .32f);
        }

        public static BattleStageLayout Build(RectTransform root, Image background, Image player, Image enemy,
            RectTransform plate, string resource)
        {
            var layout = root.gameObject.AddComponent<BattleStageLayout>();
            layout._root = root; layout._background = background.rectTransform; layout._plate = plate;
            layout._player = player; layout._enemy = enemy; layout._resource = resource;
            layout._aspect = background.sprite == null ? 16f / 9 : background.sprite.rect.width / background.sprite.rect.height;
            layout._playerShadow = layout.Shadow("PlayerContactShadow");
            layout._enemyShadow = layout.Shadow("EnemyContactShadow");
            layout.Apply();
            return layout;
        }

        private RectTransform Shadow(string name)
        {
            var image = Ui.SpriteImg(_root, name, SpellSequence.Glow());
            image.color = new Color(.08f, .07f, .06f, .36f);
            image.raycastTarget = false;
            image.transform.SetSiblingIndex(1); // Behind both combatants and all UI, above the backdrop.
            return image.rectTransform;
        }

        private void OnEnable() => Canvas.willRenderCanvases += Refresh;
        private void OnDisable() => Canvas.willRenderCanvases -= Refresh;
        private void Refresh()
        {
            if (_root != null && (_root.rect.size - _lastSize).sqrMagnitude > .01f) Apply();
        }

        public void Apply()
        {
            var size = _root.rect.size;
            if (size.x < 1 || size.y < 1) return;
            _lastSize = size;
            var cover = CoverSize(size, _aspect);
            _background.anchorMin = _background.anchorMax = Vector2.one * .5f;
            _background.pivot = Vector2.one * .5f;
            _background.anchoredPosition = Vector2.zero;
            _background.sizeDelta = cover;

            // Keep the card readable, but reserve the scene's right-hand platform for the opponent.
            _plate.localScale = Vector3.one * .78f;
            _plate.anchoredPosition = new Vector2(-28, 340);
            Place(_player, _playerShadow, false, size, cover);
            Place(_enemy, _enemyShadow, true, size, cover);
        }

        private void Place(Image image, RectTransform shadow, bool enemy, Vector2 viewport, Vector2 cover)
        {
            var uv = FootUv(_resource, enemy);
            Vector2 foot = Vector2.Scale(uv - Vector2.one * .5f, cover);
            // The command dock occupies the bottom 312 canvas units. Move only within the open surface.
            foot.y = Mathf.Max(foot.y, -viewport.y * .5f + 346);
            float side = enemy ? 320 : 440;
            float aspect = image.sprite == null ? 1 : image.sprite.rect.width / image.sprite.rect.height;
            var bounds = aspect >= 1 ? new Vector2(side, side / aspect) : new Vector2(side * aspect, side);
            float plateLeft = viewport.x * .5f - 28 - _plate.sizeDelta.x * .78f;
            float plateTop = -viewport.y * .5f + 340 + _plate.sizeDelta.y * .78f;
            if (enemy && foot.y < plateTop + 12)
                foot.x = Mathf.Min(foot.x, plateLeft - bounds.x * .5f - 18);
            foot.x = Mathf.Clamp(foot.x, -viewport.x * .5f + bounds.x * .5f + 24,
                viewport.x * .5f - bounds.x * .5f - 24);
            var rt = image.rectTransform;
            rt.anchorMin = rt.anchorMax = rt.pivot = Vector2.one * .5f;
            rt.sizeDelta = bounds;
            rt.anchoredPosition = foot + Vector2.up * bounds.y * .5f;
            shadow.anchorMin = shadow.anchorMax = shadow.pivot = Vector2.one * .5f;
            shadow.anchoredPosition = foot + Vector2.up * 5;
            shadow.sizeDelta = new Vector2(bounds.x * .60f, enemy ? 22 : 30);
        }
    }
}
