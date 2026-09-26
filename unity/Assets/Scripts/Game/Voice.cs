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
        private bool _ducking;

        private void Awake()
        {
            _voiceId = ++_nextVoiceId;
            _audio = gameObject.AddComponent<AudioSource>();
        }

        public void Say(params string[] lines)
        {
            if (!Enabled) { Stop(); return; }
            Replay(lines);
        }

        /// <summary>A deliberate Listen tap works even when automatic narration is off.</summary>
        public void Replay(params string[] lines)
        {
            Stop();
            _routine = StartCoroutine(SayRoutine(lines));
        }

        public void Stop()
        {
            if (_routine != null)
            {
                StopCoroutine(_routine);
                _routine=null;
            }
            if(_audio!=null)_audio.Stop();
            if(_ducking){Music.SetVoiceActive(_voiceId, false);_ducking=false;}
        }

        private IEnumerator SayRoutine(string[] lines)
        {
            _audio.Stop();
            Music.SetVoiceActive(_voiceId, true);
            _ducking=true;
            foreach (string line in lines)
            {
                var clip = Resources.Load<AudioClip>("Voice/" + VoiceKeys.Sanitize(line));
                if (clip == null) continue;
                _audio.clip = clip;
                _audio.Play();
                yield return new WaitForSeconds(clip.length + 0.1f);
            }
            Music.SetVoiceActive(_voiceId, false);
            _ducking=false;
            _routine = null;
        }

        private void OnDestroy()
        {
            Stop();
        }
    }
}
