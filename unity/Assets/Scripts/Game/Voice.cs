using System.Collections;
using UnityEngine;

namespace Numeria.Game
{
    /// <summary>预烘焙语音播放:按台词文本查 Resources/Voice 下的 wav 顺序播放。</summary>
    public class Voice : MonoBehaviour
    {
        /// <summary>全局语音开关(菜单设置,持久化在 Progress.VoiceEnabled)。</summary>
        public static bool Enabled = true;

        private static int _nextVoiceId;
        private AudioSource _audio;
        private Coroutine _routine;
        private int _voiceId;

        private void Awake()
        {
            _voiceId = ++_nextVoiceId;
            _audio = gameObject.AddComponent<AudioSource>();
        }

        public void Say(params string[] lines)
        {
            if (!Enabled) return;
            if (_routine != null)
            {
                StopCoroutine(_routine);
                _audio.Stop();
                Music.SetVoiceActive(_voiceId, false);
            }
            _routine = StartCoroutine(SayRoutine(lines));
        }

        private IEnumerator SayRoutine(string[] lines)
        {
            _audio.Stop();
            Music.SetVoiceActive(_voiceId, true);
            foreach (string line in lines)
            {
                var clip = Resources.Load<AudioClip>("Voice/" + VoiceKeys.Sanitize(line));
                if (clip == null) continue;
                _audio.clip = clip;
                _audio.Play();
                yield return new WaitForSeconds(clip.length + 0.1f);
            }
            Music.SetVoiceActive(_voiceId, false);
            _routine = null;
        }

        private void OnDestroy()
        {
            Music.SetVoiceActive(_voiceId, false);
        }
    }
}
