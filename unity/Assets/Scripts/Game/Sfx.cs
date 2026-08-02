using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Numeria.Game
{
    /// <summary>短促游戏音效。与语音和背景音乐使用独立 AudioSource，互不打断。</summary>
    public enum SfxCue
    {
        Click,
        Correct,
        SoftMiss,
        Hit,
        ShieldBreak,
        Catch,
        Chest,
        LevelUp,
        Victory,
        Evolution
    }

    public static class Sfx
    {
        private static readonly Dictionary<SfxCue, string> Paths = new Dictionary<SfxCue, string>
        {
            { SfxCue.Click, "Sfx/ui-click" },
            { SfxCue.Correct, "Sfx/correct" },
            { SfxCue.SoftMiss, "Sfx/soft-miss" },
            { SfxCue.Hit, "Sfx/hit" },
            { SfxCue.ShieldBreak, "Sfx/shield-break" },
            { SfxCue.Catch, "Sfx/catch" },
            { SfxCue.Chest, "Sfx/chest-open" },
            { SfxCue.LevelUp, "Sfx/level-up" },
            { SfxCue.Victory, "Sfx/victory" },
            { SfxCue.Evolution, "Sfx/evolution" }
        };

        private static readonly HashSet<SfxCue> MissingWarnings = new HashSet<SfxCue>();
        private static AudioSource _source;

        public static bool Enabled { get; set; } = true;
        public static float Volume { get; set; } = 0.58f;

        /// <summary>公开资源路径，便于编辑器检查和未来替换音色而不改调用点。</summary>
        public static string ResourcePath(SfxCue cue) => Paths[cue];

        public static void Play(SfxCue cue, float volumeScale = 1f)
        {
            if (!Enabled) return;
            var clip = Resources.Load<AudioClip>(ResourcePath(cue));
            if (clip == null)
            {
                if (MissingWarnings.Add(cue)) Debug.LogWarning($"Missing SFX resource: {ResourcePath(cue)}");
                return;
            }

            EnsureSource().PlayOneShot(clip, Mathf.Clamp01(Volume * volumeScale));
        }

        /// <summary>给程序化按钮接入统一点击音，不改变按钮自己的业务回调顺序。</summary>
        public static Button WireClick(Button button)
        {
            button.onClick.AddListener(() => Play(SfxCue.Click));
            return button;
        }

        private static AudioSource EnsureSource()
        {
            if (_source != null) return _source;
            var go = new GameObject("Numeria SFX");
            Object.DontDestroyOnLoad(go);
            _source = go.AddComponent<AudioSource>();
            _source.playOnAwake = false;
            _source.loop = false;
            _source.spatialBlend = 0f;
            _source.ignoreListenerPause = true;
            return _source;
        }
    }
}
