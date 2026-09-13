using Numeria.Core;
using UnityEngine;
using UnityEngine.UI;

namespace Numeria.Game
{
    /// <summary>Six distinct motion grammars. The shared SpellSequence owns timing and the single damage callback.</summary>
    public sealed class AfterstorySpell
    {
        private readonly SkillVisualKind _kind;
        private readonly Vector2 _from, _to;
        private readonly float _scale;
        private readonly Image[] _motifs = new Image[8];
        private readonly Image _hero, _beam;

        public AfterstorySpell(Transform parent, SkillVisualKind kind, Vector2 from, Vector2 to, SpellTrace trace, float scale)
        {
            _kind = kind; _from = from; _to = to; _scale = scale;
            string id = kind == SkillVisualKind.MosaicMarch ? "tessellion" : kind == SkillVisualKind.BalanceBeam ? "abacoracle" :
                kind == SkillVisualKind.PatternFlight ? "clockondor" : kind == SkillVisualKind.LanternCurrent ? "lanternray" :
                kind == SkillVisualKind.MirrorWaltz ? "moonveil" : "lotustide";
            _hero = Make(parent, "SpellFamiliar", SpriteLib.LargeIcon(id));
            _beam = Make(parent, "ConvergingLight", SpellSequence.Glow());
            for (int i = 0; i < _motifs.Length; i++)
            {
                Sprite art = kind == SkillVisualKind.MosaicMarch ? SpellChoreography.Material("impact", 3) :
                    kind == SkillVisualKind.MirrorWaltz ? SpriteLib.LargeIcon("veilkit") :
                    kind == SkillVisualKind.PatternFlight ? WorldScene.Prop("ancient_ruins", 3) :
                    kind == SkillVisualKind.LotusSequence ? WorldScene.Prop("veilmoon_lake", 2) : SpellSequence.Glow();
                _motifs[i] = Make(parent, "MathMotif-" + i, art);
                if (trace?.Pattern?.Length > 0 && (kind == SkillVisualKind.MosaicMarch || kind == SkillVisualKind.PatternFlight))
                    _motifs[i].sprite = PuzzleUi.PatternSpriteForSpell(trace.Pattern[i % trace.Pattern.Length]);
            }
            if (trace != null && trace.HasEquation)
            {
                var label = Ui.Label(parent, "ActualSolvedEquation", trace.Equation, 32, Ui.Hex("#fff3c9"));
                label.rectTransform.sizeDelta = new Vector2(350, 55);
                label.rectTransform.anchoredPosition = from + Vector2.up * 210;
            }
        }

        private static Image Make(Transform parent, string name, Sprite sprite)
        { var image = Ui.SpriteImg(parent, name, sprite); image.raycastTarget = false; image.preserveAspect = true; return image; }

        private void Put(Image image, Vector2 at, float size, float alpha, float angle = 0)
        {
            image.rectTransform.anchoredPosition = at;
            image.rectTransform.sizeDelta = Vector2.one * size * _scale;
            image.rectTransform.localRotation = Quaternion.Euler(0, 0, angle);
            image.color = new Color(1, 1, 1, Mathf.Clamp01(alpha));
        }

        public void Sample(float seconds)
        {
            float charge = Mathf.Clamp01(seconds / .65f);
            float flight = Mathf.Clamp01((seconds - .65f) / (SpellSequence.ImpactTime - .65f));
            float fade = 1 - Mathf.Clamp01((seconds - SpellSequence.ImpactTime) / (SpellSequence.Duration - SpellSequence.ImpactTime));
            var head = Vector2.Lerp(_from, _to, flight);
            Put(_hero, head, 0, 0); Put(_beam, head, 0, 0);
            for (int i = 0; i < _motifs.Length; i++)
            {
                float u = i / 7f, side = i % 2 == 0 ? -1 : 1;
                Vector2 point = head; float size = 55, alpha = fade, rotation = 0;
                switch (_kind)
                {
                    case SkillVisualKind.MosaicMarch:
                        // Tiles assemble into a lion, then the completed mosaic runs along the ground.
                        point = Vector2.Lerp(_from + new Vector2((i % 4 - 1.5f) * 80, (i / 4) * 100),
                            head + new Vector2((i % 4 - 1.5f) * 28, (i / 4) * 28 - 35), charge);
                        rotation = (1 - charge) * side * 120; alpha *= 1 - flight * .8f;
                        Put(_hero, head + Vector2.up * Mathf.Abs(Mathf.Sin(flight * Mathf.PI * 3)) * 30,
                            220, flight * fade);
                        break;
                    case SkillVisualKind.BalanceBeam:
                        // Two equal bead columns settle before their light becomes one directed beam.
                        point = _from + new Vector2(side * 95, (i / 2 - 1.5f) * 40 + side * (1 - charge) * 75);
                        point = Vector2.Lerp(point, _from, flight); size = 32;
                        var delta = (head - _from);
                        _beam.preserveAspect = false;
                        _beam.rectTransform.anchoredPosition = (_from + head) * .5f;
                        _beam.rectTransform.sizeDelta = new Vector2(delta.magnitude, 20 * flight) * _scale;
                        _beam.rectTransform.localRotation = Quaternion.Euler(0, 0, Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg);
                        _beam.color = new Color(1, .82f, .3f, flight * fade);
                        break;
                    case SkillVisualKind.PatternFlight:
                        point = Vector2.Lerp(_from, _to, u) + Vector2.up * Mathf.Sin(u * Mathf.PI * 2) * 85;
                        size = 55; alpha *= flight >= u ? .9f : .15f; rotation = seconds * 90;
                        Put(_hero, head + Vector2.up * Mathf.Sin(flight * Mathf.PI * 2) * 85, 200, charge * fade);
                        break;
                    case SkillVisualKind.LanternCurrent:
                        point = Vector2.Lerp(_from, _to, Mathf.Clamp01(flight - u * .3f)) +
                            new Vector2(Mathf.Cos(u * 6.28f + seconds * 3) * 30, Mathf.Sin(u * 6.28f + seconds * 3) * 55);
                        size = 30; alpha *= charge;
                        Put(_hero, head + Vector2.up * Mathf.Sin(flight * Mathf.PI) * 65, 245, flight * fade);
                        break;
                    case SkillVisualKind.MirrorWaltz:
                        // Exactly two mirrored silhouettes spiral toward the same impact, not random starfall.
                        point = head + new Vector2(side * Mathf.Sin(flight * Mathf.PI) * 110,
                            side * Mathf.Cos(flight * Mathf.PI) * 85 * (1 - flight));
                        size = 170; alpha *= i < 2 ? charge * .65f : 0;
                        _motifs[i].rectTransform.localScale = new Vector3(side, 1, 1);
                        Put(_hero, _to, 240, flight > .95f ? fade : 0);
                        break;
                    case SkillVisualKind.LotusSequence:
                        point = Vector2.Lerp(_from, _to, u) + Vector2.down * 65;
                        float bloom = Mathf.Clamp01((flight - u * .8f) * 5);
                        size = 95 * bloom; alpha *= bloom;
                        Put(_hero, _to + Vector2.up * (flight * 50 - 50), 220, flight > .9f ? fade : 0);
                        break;
                }
                Put(_motifs[i], point, size, alpha, rotation);
            }
        }
    }
}
