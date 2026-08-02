using System.Text;

namespace Numeria.Game
{
    /// <summary>
    /// 台词文本 → 语音文件名。必须与 tools/bake-voice.sh 的 key 规则一致:
    /// 小写、非字母数字的连续段折叠为单个 '-'、去掉首尾 '-'。
    /// </summary>
    public static class VoiceKeys
    {
        public static string Sanitize(string text)
        {
            var sb = new StringBuilder(text.Length);
            bool lastDash = true; // 抑制开头的 '-'
            foreach (char raw in text)
            {
                char c = char.ToLowerInvariant(raw);
                if (c >= 'a' && c <= 'z' || c >= '0' && c <= '9')
                {
                    sb.Append(c);
                    lastDash = false;
                }
                else if (!lastDash)
                {
                    sb.Append('-');
                    lastDash = true;
                }
            }
            while (sb.Length > 0 && sb[sb.Length - 1] == '-') sb.Length--;
            return sb.ToString();
        }
    }
}
