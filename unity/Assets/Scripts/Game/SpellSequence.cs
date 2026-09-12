using System;
using System.Collections;
using Numeria.Core;
using UnityEngine;
using UnityEngine.UI;

namespace Numeria.Game
{
    /// <summary>Three authored spell timelines, evaluated in canvas units at any aspect ratio.</summary>
    public sealed class SpellSequence : MonoBehaviour
    {
        public const float Duration = 1.9f;
        public const float ImpactTime = 1.22f;
        public SkillVisualKind Kind { get; private set; }
        private RectTransform _caster, _target;
        private Vector3 _casterScale, _targetPosition;
        private Vector2 _start, _end;
        private Image[] _particles;
        private Image[] _frame;
        private Image _ribbon, _core;
        private Image _elementArt;
        private static readonly Sprite[] Materials = new Sprite[3];
        private CanvasGroup _group;
        private bool _powered;
        private int _groupA;
        private bool _subtract;
        private static Sprite _glow;
        private static readonly AudioClip[] Clips = new AudioClip[3];

        public static bool Supports(SkillVisualKind visual) => visual == SkillVisualKind.EquationFlame ||
            visual == SkillVisualKind.MakeTenWave || visual == SkillVisualKind.SymmetryBeam;

        public static SpellSequence Create(RectTransform canvas, RectTransform caster, RectTransform target,
            SkillVisualKind kind, SpellTrace trace, bool powered)
        {
            var root = Ui.Node(canvas, "Spell-" + kind);
            Ui.Stretch(root);
            var sequence = root.gameObject.AddComponent<SpellSequence>();
            sequence.Kind = kind;
            sequence._caster = caster;
            sequence._target = target;
            sequence._casterScale = caster.localScale;
            sequence._targetPosition = target.localPosition;
            sequence._start = root.InverseTransformPoint(caster.position) + new Vector3(45, 50, 0);
            sequence._end = root.InverseTransformPoint(target.position);
            sequence._powered = powered;
            sequence.Build(trace);
            sequence.Sample(0);
            return sequence;
        }

        private void Build(SpellTrace trace)
        {
            _group = gameObject.AddComponent<CanvasGroup>();
            _group.blocksRaycasts = false;
            _ribbon = Ui.SpriteImg(transform, "ElementBody", Glow());
            _core = Ui.SpriteImg(transform, "ElementCore", Glow());
            _ribbon.raycastTarget = _core.raycastTarget = false;
            int materialIndex = Kind == SkillVisualKind.EquationFlame ? 0 : Kind == SkillVisualKind.MakeTenWave ? 1 : 2;
            if (Materials[materialIndex] == null)
            {
                var texture = Resources.Load<Texture2D>("generated/Exploration/spell_materials");
                if (texture != null)
                {
                    float[] bottoms = { .65f, .34f, 0 }, heights = { .35f, .31f, .34f };
                    Materials[materialIndex] = Sprite.Create(texture,
                        new Rect(0, Mathf.Floor(bottoms[materialIndex] * texture.height), texture.width,
                            Mathf.Floor(heights[materialIndex] * texture.height)), Vector2.one * .5f, 100);
                }
            }
            _elementArt = Ui.SpriteImg(transform, "SpellMaterial", Materials[materialIndex]);
            _elementArt.raycastTarget = false;
            int count = Kind == SkillVisualKind.SymmetryBeam ? (trace?.Pattern?.Length ?? 3) * 2 : 20;
            if (trace != null && trace.Total > 0 && _powered && Kind != SkillVisualKind.SymmetryBeam)
                count = Mathf.Clamp(trace.Operation == '-' ? trace.A : trace.Total, 1, 20);
            _subtract = trace?.Operation == '-' && _powered;
            _groupA = trace != null && _powered ? (_subtract ? trace.Total : trace.A) : count / 2;
            _particles = new Image[count];
            for (int i = 0; i < count; i++)
            {
                Sprite sprite = Glow();
                _particles[i] = Ui.SpriteImg(transform, "Element-" + i, sprite);
                _particles[i].raycastTarget = false;
                _particles[i].preserveAspect = true;
                if (Kind == SkillVisualKind.SymmetryBeam)
                {
                    int half = count / 2;
                    int pair = i < half ? i : count - 1 - i;
                    var shape = trace?.Pattern != null ? trace.Pattern[pair].Shape : ShapeKind.Circle;
                    _particles[i].sprite = trace?.Pattern != null
                        ? PuzzleUi.PatternSpriteForSpell(trace.Pattern[pair]) : PuzzleUi.PatternSpriteForSpell(shape);
                    _particles[i].color = Color.white;
                }
            }
            if (Kind == SkillVisualKind.MakeTenWave)
            {
                int total = Mathf.Clamp(trace?.Total > 0 ? trace.Total : 10, 1, 20);
                _frame = new Image[total];
                for (int i = 0; i < total; i++)
                {
                    _frame[i] = Ui.SpriteImg(transform, "WaterCell-" + i, Glow());
                    _frame[i].raycastTarget = false;
                    _frame[i].rectTransform.sizeDelta = Vector2.one * 22;
                    _frame[i].rectTransform.anchoredPosition = _start + new Vector2((i % 5 - 2) * 29, 110 + (i / 5) * 27);
                }
            }
            if (trace != null && trace.Total > 0 && _powered && Kind != SkillVisualKind.SymmetryBeam)
            {
                var equation = Ui.Label(transform, "SpellEquation", trace.Equation, 34, Ui.Hex("#fff1be"));
                equation.rectTransform.anchoredPosition = _start + new Vector2(0, 250);
                equation.rectTransform.sizeDelta = new Vector2(330, 50);
            }
        }

        public static Sprite Glow()
        {
            if (_glow != null) return _glow;
            const int size = 64;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear };
            var pixels = new Color[size * size];
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float distance = Vector2.Distance(new Vector2(x, y), Vector2.one * 31.5f) / 31.5f;
                    pixels[y * size + x] = new Color(1, 1, 1, Mathf.Pow(Mathf.Clamp01(1 - distance), 1.3f));
                }
            texture.SetPixels(pixels);
            texture.Apply();
            _glow = Sprite.Create(texture, new Rect(0, 0, size, size), Vector2.one * .5f, 100);
            return _glow;
        }

        public void Sample(float seconds)
        {
            float charge = Mathf.Clamp01(seconds / .65f);
            float release = Mathf.Clamp01((seconds - .65f) / (ImpactTime - .65f));
            float aftermath = Mathf.Clamp01((seconds - ImpactTime) / (Duration - ImpactTime));
            _target.localPosition = _targetPosition;
            _group.alpha = 1 - aftermath;
            _caster.localScale = Vector3.Scale(_casterScale, new Vector3(1 + .035f * Mathf.Sin(charge * Mathf.PI),
                1 - .065f * Mathf.Sin(charge * Mathf.PI), 1));
            Vector2 head = Vector2.Lerp(_start, _end, release);
            bool flame = Kind == SkillVisualKind.EquationFlame, water = Kind == SkillVisualKind.MakeTenWave;
            Color tint = flame ? Ui.Hex("#ff862e") : water ? Ui.Hex("#54d4db") : Ui.Hex("#a3efff");
            float strength = _powered ? 1 : .72f;
            Beam(_ribbon, _start, head, (flame ? 100 : water ? 165 : 55) * strength,
                tint, release > 0 ? 1 - aftermath : 0);
            Beam(_core, _start, head, (flame ? 32 : water ? 50 : 17) * strength,
                Color.white, release > 0 ? .8f * (1 - aftermath) : 0);
            if (Kind == SkillVisualKind.SymmetryBeam)
            {
                _elementArt.rectTransform.anchoredPosition = Vector2.Lerp(_start + Vector2.up * 130, _end, release);
                _elementArt.rectTransform.sizeDelta = new Vector2(410, 175) * (.3f + .7f * charge) * strength;
                _elementArt.color = new Color(1, 1, 1, .7f * charge);
            }
            else
            {
                Vector2 tail = flame ? _start : Vector2.Lerp(_start, head, .40f);
                Beam(_elementArt, tail, head, (flame ? 175 : 230) * strength,
                    Color.white, release > 0 ? 1 - aftermath : 0);
            }

            for (int i = 0; i < _particles.Length; i++)
            {
                Image particle = _particles[i];
                float u = i / (float)_particles.Length;
                float phase = u * Mathf.PI * 2;
                Vector2 position;
                float size;
                if (Kind == SkillVisualKind.SymmetryBeam)
                {
                    int half = _particles.Length / 2;
                    int pair = i < half ? i : _particles.Length - 1 - i;
                    float side = i < half ? -1 : 1;
                    Vector2 wing = new Vector2(side * (60 + pair * 35), 100 + pair * 33);
                    position = Vector2.Lerp(_start + wing * charge, _end + wing * .6f, release);
                    size = 38 * strength;
                    particle.rectTransform.localRotation = Quaternion.identity;
                }
                else if (seconds < .65f)
                {
                    // Two visible groups merge toward the casting focus before the spell travels.
                    float side = i < _groupA ? -1 : 1;
                    position = _start + new Vector2(side * (100 - charge * 65) + Mathf.Cos(phase) * 27,
                        65 + Mathf.Sin(phase) * 30);
                    size = flame ? 20 + 22 * charge : 23;
                    particle.color = i < _groupA ? tint : Color.Lerp(tint, Color.white, .55f);
                    if (_subtract && i >= _groupA)
                    {
                        position += Vector2.up * (charge * 90);
                        Color c = particle.color; c.a = 1 - charge; particle.color = c;
                    }
                }
                else
                {
                    float trail = Mathf.Clamp01(release - u * .20f);
                    position = Vector2.Lerp(_start, _end, trail);
                    position += new Vector2(Mathf.Cos(phase + seconds * 9) * (water ? 25 : 12),
                        Mathf.Sin(phase + seconds * 11) * (water ? 50 : 25));
                    if (aftermath > 0)
                        position = _end + new Vector2(Mathf.Cos(phase) * (flame ? 70 : 125),
                            flame ? 110 * u : -60 * u) * Mathf.Sqrt(aftermath);
                    size = (flame ? 54 : 38) * strength * (1 - aftermath * .5f);
                    particle.color = Color.Lerp(tint, Color.white, u * .4f);
                    if (_subtract && i >= _groupA) particle.color = Color.clear;
                }
                particle.rectTransform.anchoredPosition = position;
                particle.rectTransform.sizeDelta = Vector2.one * size;
            }
            if (_frame != null)
                for (int i = 0; i < _frame.Length; i++)
                    _frame[i].color = new Color(i < _groupA ? .3f : .85f, .94f, 1,
                        charge >= (i + 1f) / _frame.Length ? 1 : .18f);
            if (aftermath > 0)
            {
                float recoil = Mathf.Sin(aftermath * Mathf.PI) * strength;
                _target.localPosition = _targetPosition + new Vector3(flame ? 10 : water ? 28 : 0,
                    Kind == SkillVisualKind.SymmetryBeam ? 12 : -6, 0) * recoil;
            }
        }

        public IEnumerator Play(Action impact)
        {
            bool landed = false;
            var audio = gameObject.AddComponent<AudioSource>();
            audio.spatialBlend = 0;
            if (Sfx.Enabled) audio.PlayOneShot(Sound(Kind), .48f * Sfx.Volume);
            try
            {
                for (float time = 0; time < Duration; time += Time.deltaTime)
                {
                    Sample(time);
                    if (!landed && time >= ImpactTime) { landed = true; impact(); }
                    yield return null;
                }
                if (!landed) impact();
            }
            finally { Cancel(); }
        }

        public void Cancel()
        {
            Restore();
            if (Application.isPlaying) Destroy(gameObject);
            else DestroyImmediate(gameObject);
        }

        private static void Beam(Image image, Vector2 from, Vector2 to, float width, Color tint, float alpha)
        {
            Vector2 delta = to - from;
            image.rectTransform.anchoredPosition = (from + to) * .5f;
            image.rectTransform.sizeDelta = new Vector2(delta.magnitude * 1.2f, width);
            image.rectTransform.localRotation = Quaternion.Euler(0, 0, Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg);
            tint.a = alpha;
            image.color = tint;
        }

        private void Restore()
        {
            if (_caster != null) _caster.localScale = _casterScale;
            if (_target != null) _target.localPosition = _targetPosition;
        }
        private void OnDestroy() => Restore();

        private static AudioClip Sound(SkillVisualKind kind)
        {
            int index = kind == SkillVisualKind.EquationFlame ? 0 : kind == SkillVisualKind.MakeTenWave ? 1 : 2;
            if (Clips[index] != null) return Clips[index];
            const int rate = 22050;
            var data = new float[(int)(Duration * rate)];
            uint seed = 23;
            float previous = 0;
            for (int i = 0; i < data.Length; i++)
            {
                float t = i / (float)rate;
                seed = seed * 1664525 + 1013904223;
                float noise = (seed % 65536) / 32768f - 1;
                previous = previous * .9f + noise * .1f;
                float envelope = Mathf.Sin(Mathf.PI * t / Duration);
                float impact = Mathf.Exp(-Mathf.Max(0, t - ImpactTime) * 12) * (t >= ImpactTime ? 1 : 0);
                float value = index == 0 ? noise * .13f + Mathf.Sin(t * 310) * impact * .6f :
                    index == 1 ? previous * .9f + Mathf.Sin(t * (700 - t * 130)) * .14f :
                    (Mathf.Sin(t * 2764) + Mathf.Sin(t * 4147)) * .12f;
                data[i] = value * envelope * .6f;
            }
            Clips[index] = AudioClip.Create("NumeriaSpell-" + kind, data.Length, 1, rate, false);
            Clips[index].SetData(data, 0);
            return Clips[index];
        }
    }

}
