// Web Speech 封装。浏览器(尤其 iOS Safari)要求在真实用户手势的调用栈内
// 同步调用过 speak 才会解锁 TTS 引擎,此后异步调用才被允许——
// 所以在首次 pointerdown 时用一句静音 utterance 解锁,解锁前的最后一句话
// 先挂起,解锁后立即补播。
export function createSpeaker() {
  if (typeof speechSynthesis === 'undefined') return () => {};

  let unlocked = false;
  let pendingText = null;

  const sayNow = text => {
    const u = new SpeechSynthesisUtterance(text);
    u.lang = 'en-US';
    u.rate = 0.85;
    u.pitch = 1.1;
    speechSynthesis.speak(u);
  };

  document.addEventListener('pointerdown', () => {
    unlocked = true;
    const blank = new SpeechSynthesisUtterance('');
    blank.volume = 0;
    speechSynthesis.speak(blank); // 手势栈内同步调用,解锁引擎
    if (pendingText) { sayNow(pendingText); pendingText = null; }
  }, { once: true, capture: true });

  return text => {
    if (!unlocked) { pendingText = text; return; }
    // 只在有话在说时打断;cancel 后立即 speak 会被 Chrome 静默丢弃,延迟规避
    if (speechSynthesis.speaking || speechSynthesis.pending) {
      speechSynthesis.cancel();
      setTimeout(() => sayNow(text), 50);
    } else {
      sayNow(text);
    }
  };
}
