export function createSpeaker() {
  if (typeof speechSynthesis === 'undefined') return () => {};
  return text => {
    speechSynthesis.cancel();
    const u = new SpeechSynthesisUtterance(text);
    u.lang = 'en-US';
    u.rate = 0.85;
    u.pitch = 1.1;
    speechSynthesis.speak(u);
  };
}
