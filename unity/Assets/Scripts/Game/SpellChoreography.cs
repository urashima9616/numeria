using System.Collections.Generic;
using Numeria.Core;
using UnityEngine;
using UnityEngine.UI;

namespace Numeria.Game
{
    /// <summary>Authored motion grammars for the remaining fourteen kinds; not a recolored projectile.</summary>
    public sealed class SpellChoreography
    {
        private static readonly Dictionary<string, Sprite> Materials = new Dictionary<string, Sprite>();
        private readonly SkillVisualKind _kind;
        private readonly Image[] _pieces, _lines;
        private readonly Image _hero, _ring;
        private readonly RectTransform _caster;
        private readonly Vector3 _casterPosition;
        private readonly Vector2 _from, _to;
        private readonly float _scale, _sign;
        private readonly bool _mega;
        private readonly SpellTrace _trace;

        public static Sprite Material(string atlas, int index)
        {
            string key = atlas + index;
            if (Materials.TryGetValue(key, out var sprite) && sprite != null) return sprite;
            var tex = Resources.Load<Texture2D>("generated/Exploration/spell_" + atlas);
            if (tex == null) return null;
            sprite = Sprite.Create(tex, new Rect(index % 2 * tex.width / 2, (1 - index / 2) * tex.height / 2,
                tex.width / 2, tex.height / 2), Vector2.one * .5f, 100, 0, SpriteMeshType.FullRect);
            Materials[key] = sprite;
            return sprite;
        }

        public SpellChoreography(Transform parent, RectTransform caster, SkillVisualKind kind, Vector2 from,
            Vector2 to, SpellTrace trace, float scale, bool mega)
        {
            _kind = kind; _from = from; _to = to; _scale = scale; _mega = mega; _trace = trace;
            _caster = caster; _casterPosition = caster.localPosition; _sign = Mathf.Sign(to.x - from.x);
            Sprite art;
            switch (kind)
            {
                case SkillVisualKind.PatternLeaf: art = Material("nature", 0); break;
                case SkillVisualKind.GrassBloom: art = Material("nature", 1); break;
                case SkillVisualKind.FlyingGust: art = Material("nature", 2); break;
                case SkillVisualKind.FairyGlimmer: art = Material("arcane", 0); break;
                case SkillVisualKind.DragonSpiral: art = Material("arcane", 1); break;
                case SkillVisualKind.ElectricBolt:
                case SkillVisualKind.SequenceSpark: art = Material("arcane", 2); break;
                case SkillVisualKind.DoubleBoulder:
                case SkillVisualKind.TallyStone: art = Material("impact", 2); break;
                case SkillVisualKind.GeometryPrism: art = Material("impact", 3); break;
                case SkillVisualKind.CountCrunch: art = Resources.Load<Sprite>("generated/Exploration/spell_bite"); break;
                case SkillVisualKind.MatchingPaws: art = Material("impact", 1); break;
                default: art = Material("impact", 0); break;
            }
            _ring = Make(parent, "ImpactSeal", Material("arcane", 3));
            _hero = Make(parent, "Signature-" + kind, art);
            int count = kind == SkillVisualKind.DoubleBoulder || kind == SkillVisualKind.MatchingPaws ? 2 :
                trace?.Pattern?.Length > 0 ? Mathf.Min(12, trace.Pattern.Length) :
                trace?.Numbers?.Length > 0 ? Mathf.Min(12, trace.Numbers.Length) :
                trace?.Total > 0 ? Mathf.Min(20, trace.Total) : 8;
            _pieces = new Image[count]; _lines = new Image[8];
            for (int i = 0; i < count; i++)
            {
                Sprite piece = art;
                if (kind == SkillVisualKind.ElectricBolt || kind == SkillVisualKind.SequenceSpark ||
                    kind == SkillVisualKind.SubtractionDash || kind == SkillVisualKind.Physical || kind == SkillVisualKind.CountCrunch) piece = SpellSequence.Glow();
                if (kind == SkillVisualKind.GrassBloom) piece = Material("nature", 3);
                _pieces[i] = Make(parent, "Motif-" + i, piece);
                if (trace?.Pattern?.Length > 0 && (kind == SkillVisualKind.PatternLeaf || kind == SkillVisualKind.GeometryPrism))
                    _pieces[i].sprite = PuzzleUi.PatternSpriteForSpell(trace.Pattern[i % trace.Pattern.Length]);
                if (trace?.Numbers?.Length > 0)
                {
                    var number = Ui.Label(_pieces[i].transform, "SolvedNumber", trace.Numbers[i % trace.Numbers.Length].ToString(), 34, Color.white);
                    Ui.Stretch(number.rectTransform);
                }
            }
            for (int i = 0; i < _lines.Length; i++) _lines[i] = Make(parent, "EnergySegment-" + i, SpellSequence.Glow());
            if (trace != null && trace.HasEquation)
            {
                var equation = Ui.Label(parent, "SolvedEquation", trace.Equation, 32, Ui.Hex("#fff3c9"));
                equation.rectTransform.sizeDelta = new Vector2(340, 48);
                equation.rectTransform.anchoredPosition = from + new Vector2(0, 215);
            }
        }

        private static Image Make(Transform parent, string name, Sprite sprite)
        { var image = Ui.SpriteImg(parent, name, sprite); image.raycastTarget = false; return image; }

        private void Put(Image image, Vector2 point, Vector2 size, float angle = 0, float alpha = 1)
        {
            image.rectTransform.anchoredPosition = point;
            image.rectTransform.sizeDelta = size * _scale;
            image.rectTransform.localRotation = Quaternion.Euler(0, 0, angle);
            image.color = new Color(1, 1, 1, Mathf.Clamp01(alpha));
        }

        private void Line(int index, Vector2 a, Vector2 b, Color tint, float width, float alpha)
        {
            var delta = b - a;
            Put(_lines[index], (a + b) * .5f, new Vector2(delta.magnitude, width), Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg, alpha);
            tint.a = alpha; _lines[index].color = tint;
        }

        public void Sample(float seconds)
        {
            float charge = Mathf.Clamp01(seconds / .65f);
            float release = Mathf.Clamp01((seconds - .65f) / (SpellSequence.ImpactTime - .65f));
            float after = Mathf.Clamp01((seconds - SpellSequence.ImpactTime) / (SpellSequence.Duration - SpellSequence.ImpactTime));
            float live = 1 - after;
            float flash = release > .94f ? live : 0;
            var head = Vector2.Lerp(_from, _to, release);
            Put(_hero, head, Vector2.zero, 0, 0);
            Put(_ring, _to, Vector2.one * (100 + after * 290), -seconds * 35, flash * (_mega ? .8f : .4f));
            for (int i = 0; i < _lines.Length; i++) _lines[i].color = Color.clear;
            _caster.localPosition = _casterPosition;

            switch (_kind)
            {
                case SkillVisualKind.Physical:
                case SkillVisualKind.SubtractionDash:
                    float dash = release < 1 ? release : 1 - after;
                    _caster.localPosition = _casterPosition + new Vector3((_to.x - _from.x) * .72f * dash,
                        (_to.y - _from.y) * .72f * dash + Mathf.Sin(dash * Mathf.PI) * 35, 0);
                    Put(_hero, _to, new Vector2(210, 260), _kind == SkillVisualKind.Physical ? -35 : 35, flash);
                    break;
                case SkillVisualKind.CountCrunch:
                    Put(_hero, _to, new Vector2(290, 280 - release * 180), 0, charge * live);
                    break;
                case SkillVisualKind.PatternLeaf:
                    Put(_hero, head, new Vector2(210, 265), seconds * 200, release * live);
                    break;
                case SkillVisualKind.GeometryPrism:
                    var prism = _from + new Vector2(_sign * 125, 90);
                    Put(_hero, prism, Vector2.one * (130 + charge * 35), 0, live);
                    for (int i = 0; i < 3; i++) Line(i, prism, Vector2.Lerp(prism, _to + new Vector2(0, (i - 1) * 75), release),
                        i == 0 ? Color.cyan : i == 1 ? Color.magenta : Color.yellow, 15, release * live);
                    break;
                case SkillVisualKind.DragonSpiral:
                    Put(_hero, head + Vector2.up * Mathf.Sin(release * Mathf.PI) * 75,
                        new Vector2(330, 200), Mathf.Sin(seconds * 6) * 18, charge * live);
                    _hero.rectTransform.localScale = new Vector3(_sign, 1, 1);
                    break;
                case SkillVisualKind.ElectricBolt:
                    Vector2 previous = _from;
                    float bolt = Mathf.Clamp01((release - .6f) / .4f);
                    for (int i = 0; i < 8; i++)
                    {
                        var next = Vector2.Lerp(_from, _to, (i + 1f) / 8 * bolt);
                        if (i < 7) next.y += Mathf.Sin(i * 17 + Mathf.Floor(seconds * 15)) * 40 * bolt;
                        Line(i, previous, next, new Color(1, .88f, .25f), 28, bolt * live); previous = next;
                    }
                    Put(_hero, _to, new Vector2(290, 260), 80, flash);
                    break;
                case SkillVisualKind.GrassBloom:
                    Put(_hero, _to + Vector2.down * (100 - release * 75), new Vector2(230, 300 * release), 0, release * live);
                    break;
                case SkillVisualKind.FlyingGust:
                    Put(_hero, head, new Vector2(280, 230), -seconds * 190 * _sign, release * live);
                    break;
                case SkillVisualKind.FairyGlimmer:
                    Put(_hero, Vector2.Lerp(_from + Vector2.up * 150, _to, release), Vector2.one * (100 + charge * 120),
                        seconds * 30, charge * live);
                    break;
            }
            for (int i = 0; i < _pieces.Length; i++)
            {
                float u = i / (float)_pieces.Length, angle = u * Mathf.PI * 2;
                Vector2 pos = head, size = Vector2.one * 50;
                float rotation = seconds * 90, alpha = live;
                switch (_kind)
                {
                    case SkillVisualKind.Physical:
                    case SkillVisualKind.SubtractionDash:
                        pos = Vector2.Lerp(_from, _to, Mathf.Clamp01(release - u * .5f));
                        size = new Vector2(70, 18); alpha = release * (1 - u) * live;
                        // Subtraction leaves a disappearing trail instead of spawning fake answer numbers.
                        if (_kind == SkillVisualKind.SubtractionDash) alpha *= 1 - charge * u;
                        break;
                    case SkillVisualKind.PatternLeaf:
                        pos = Vector2.Lerp(_from + new Vector2((i - _pieces.Length * .5f) * 45, 110), _to,
                            release) + new Vector2(0, Mathf.Sin(release * Mathf.PI) * (i % 2 == 0 ? 120 : -75));
                        size = Vector2.one * 58; rotation = _trace?.Pattern != null ? 0 : seconds * 220;
                        break;
                    case SkillVisualKind.CountCrunch:
                        pos = _to + new Vector2(Mathf.Cos(angle) * (135 - release * 90), Mathf.Sin(angle) * (100 - release * 65));
                        size = Vector2.one * 45; rotation = -u * 360; alpha *= charge;
                        break;
                    case SkillVisualKind.DoubleBoulder:
                        pos = Vector2.Lerp(_from + new Vector2((i == 0 ? -1 : 1) * 90, 80), _to, release);
                        pos.y += Mathf.Sin(release * Mathf.PI) * (i == 0 ? 175 : 90);
                        size = Vector2.one * (85 + release * 45); rotation = (i == 0 ? 1 : -1) * seconds * 120;
                        break;
                    case SkillVisualKind.MatchingPaws:
                        pos = Vector2.Lerp(_from + new Vector2((i == 0 ? -1 : 1) * 75, 120),
                            _to + new Vector2((i == 0 ? -1 : 1) * 55, (i == 0 ? 1 : -1) * 45), release);
                        size = Vector2.one * 120; rotation = i == 0 ? -25 : 25;
                        break;
                    case SkillVisualKind.TallyStone:
                        pos = Vector2.Lerp(_from + new Vector2((i % 5 - 2) * 33, 65 + (i / 5) * 40),
                            _to + new Vector2((i % 5 - 2) * 20, -i / 5 * 20), release);
                        pos.y += Mathf.Sin(release * Mathf.PI) * (50 + u * 130);
                        size = Vector2.one * 48; rotation = release * 160;
                        break;
                    case SkillVisualKind.GeometryPrism:
                        pos = _from + new Vector2(_sign * 125 + Mathf.Cos(angle + seconds) * 90,
                            90 + Mathf.Sin(angle + seconds) * 90);
                        size = Vector2.one * 42; rotation = 0;
                        break;
                    case SkillVisualKind.SequenceSpark:
                        pos = Vector2.Lerp(_from, _to, (i + 1f) / _pieces.Length);
                        pos.y += i == _pieces.Length - 1 ? 0 : (i % 2 == 0 ? 65 : -35);
                        size = Vector2.one * 70; alpha *= release >= (i + 1f) / _pieces.Length ? 1 : .12f;
                        rotation = 0;
                        if (i < _lines.Length) Line(i, i == 0 ? _from : _pieces[i - 1].rectTransform.anchoredPosition,
                            pos, Color.yellow, 16, release >= (i + 1f) / _pieces.Length ? live : 0);
                        break;
                    case SkillVisualKind.FairyGlimmer:
                        pos = Vector2.Lerp(_from + new Vector2(Mathf.Cos(angle + seconds * 2) * 110, 150 + Mathf.Sin(angle) * 45),
                            _to + new Vector2(Mathf.Cos(angle) * 100, 100 - after * 200), release);
                        size = Vector2.one * 45;
                        break;
                    case SkillVisualKind.DragonSpiral:
                        pos = Vector2.Lerp(_from, _to, Mathf.Clamp01(release - u * .3f)) +
                            new Vector2(Mathf.Cos(angle + seconds * 8) * 30, Mathf.Sin(angle + seconds * 8) * 65);
                        size = Vector2.one * (65 - u * 35); rotation = seconds * 180;
                        break;
                    case SkillVisualKind.ElectricBolt:
                        pos = _from + new Vector2(Mathf.Cos(angle + seconds * 10) * 55, Mathf.Sin(angle + seconds * 10) * 55);
                        size = Vector2.one * (12 + 20 * charge); alpha *= 1 - release;
                        break;
                    case SkillVisualKind.GrassBloom:
                        pos = _to + new Vector2(Mathf.Cos(angle) * 110 * release, Mathf.Sin(angle) * 40 - 65 + release * 80);
                        size = Vector2.one * (95 * release); alpha *= release; rotation = Mathf.Sin(seconds * 2 + i) * 15;
                        break;
                    case SkillVisualKind.FlyingGust:
                        pos = Vector2.Lerp(_from, _to, Mathf.Clamp01(release - u * .25f)) +
                            new Vector2(Mathf.Cos(angle + seconds * 9) * 30, Mathf.Sin(angle + seconds * 9) * 70);
                        size = Vector2.one * 70; rotation = seconds * 260 + i * 40;
                        break;
                }
                Put(_pieces[i], pos, size, rotation, alpha);
            }
        }
    }
}
