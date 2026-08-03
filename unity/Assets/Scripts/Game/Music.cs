using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Numeria.Game
{
    /// <summary>由地图和战斗状态驱动的 Dynamic Music 曲目。</summary>
    public enum MusicMood
    {
        None,
        Forest,
        Mountains,
        Sky,
        Battle,
        Boss,
        Evolution
    }

    public static class Music
    {
        private static readonly Dictionary<MusicMood, string> Paths = new Dictionary<MusicMood, string>
        {
            { MusicMood.Forest, "Music/Jukebox/forest" },
            { MusicMood.Mountains, "Music/Jukebox/mountains" },
            { MusicMood.Sky, "Music/Jukebox/sky" },
            { MusicMood.Battle, "Music/Jukebox/battle" },
            { MusicMood.Boss, "Music/Jukebox/boss" },
            { MusicMood.Evolution, "Music/Jukebox/evolution" }
        };

        private static MusicPlayer _player;
        private static bool _enabled = true;

        public static bool Enabled
        {
            get => _enabled;
            set
            {
                _enabled = value;
                if (_player != null) _player.SetEnabled(value);
            }
        }

        public static float Volume { get; set; } = 0.28f;
        public static float VoiceDuckVolume { get; set; } = 0.09f;

        public static string ResourcePath(MusicMood mood) =>
            mood == MusicMood.None ? null : Paths[mood];

        public static void PlayMap(string mapId)
        {
            switch (mapId)
            {
                case "mountains": Play(MusicMood.Mountains); break;
                case "sky": Play(MusicMood.Sky); break;
                default: Play(MusicMood.Forest); break;
            }
        }

        public static void Play(MusicMood mood)
        {
            EnsurePlayer().Play(mood);
        }

        /// <summary>Voice 以实例 id 登记，多个语音源重叠时不会过早恢复音乐音量。</summary>
        public static void SetVoiceActive(int voiceId, bool active)
        {
            EnsurePlayer().SetVoiceActive(voiceId, active);
        }

        private static MusicPlayer EnsurePlayer()
        {
            if (_player != null) return _player;
            var go = new GameObject("Numeria Music");
            Object.DontDestroyOnLoad(go);
            _player = go.AddComponent<MusicPlayer>();
            _player.SetEnabled(_enabled);
            return _player;
        }
    }

    /// <summary>双通道无缝交叉淡化；曲目缺失时安静降级，不阻塞游戏。</summary>
    internal sealed class MusicPlayer : MonoBehaviour
    {
        private readonly HashSet<int> _activeVoices = new HashSet<int>();
        private readonly HashSet<MusicMood> _missingWarnings = new HashSet<MusicMood>();
        private AudioSource[] _channels;
        private int _activeChannel;
        private MusicMood _mood = MusicMood.None;
        private Coroutine _fade;
        private bool _enabled = true;

        private void Awake()
        {
            _channels = new[] { MakeChannel(), MakeChannel() };
        }

        public void SetEnabled(bool enabled)
        {
            _enabled = enabled;
            if (_channels == null) return;
            if (!enabled)
            {
                foreach (var channel in _channels) channel.Pause();
            }
            else
            {
                foreach (var channel in _channels)
                    if (channel.clip != null && !channel.isPlaying) channel.Play();
                ApplyTargetVolume();
            }
        }

        public void Play(MusicMood mood)
        {
            if (_channels == null || mood == _mood) return;
            _mood = mood;

            AudioClip clip = mood == MusicMood.None ? null : Resources.Load<AudioClip>(Music.ResourcePath(mood));
            if (mood != MusicMood.None && clip == null && _missingWarnings.Add(mood))
                Debug.LogWarning($"Dynamic Music is not installed: {Music.ResourcePath(mood)}");

            int nextIndex = 1 - _activeChannel;
            var from = _channels[_activeChannel];
            var to = _channels[nextIndex];
            to.Stop();
            to.clip = clip;
            to.volume = 0f;
            to.loop = true;
            if (clip != null && _enabled) to.Play();

            if (_fade != null) StopCoroutine(_fade);
            _fade = StartCoroutine(CrossFade(from, to, 0.9f));
            _activeChannel = nextIndex;
        }

        public void SetVoiceActive(int voiceId, bool active)
        {
            if (active) _activeVoices.Add(voiceId);
            else _activeVoices.Remove(voiceId);
            ApplyTargetVolume();
        }

        private AudioSource MakeChannel()
        {
            var source = gameObject.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.loop = true;
            source.spatialBlend = 0f;
            source.ignoreListenerPause = true;
            return source;
        }

        private float TargetVolume => _activeVoices.Count > 0 ? Music.VoiceDuckVolume : Music.Volume;

        private void ApplyTargetVolume()
        {
            if (_channels == null || !_enabled) return;
            var active = _channels[_activeChannel];
            if (active.clip != null) active.volume = TargetVolume;
        }

        private IEnumerator CrossFade(AudioSource from, AudioSource to, float duration)
        {
            float fromStart = from.volume;
            float time = 0f;
            while (time < duration)
            {
                time += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(time / duration);
                from.volume = Mathf.Lerp(fromStart, 0f, t);
                to.volume = Mathf.Lerp(0f, _enabled ? TargetVolume : 0f, t);
                yield return null;
            }
            from.Stop();
            from.clip = null;
            to.volume = _enabled ? TargetVolume : 0f;
            _fade = null;
        }
    }
}
