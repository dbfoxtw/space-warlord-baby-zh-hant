using System;
using System.Text;
using System.Text.RegularExpressions;

namespace SwbZhHant
{
    /// <summary>
    /// 存檔裡的顯示文字：遊戲存檔時把幾個欄位存成「當下顯示的文字」，裝了 mod 就會存成中文，
    /// 拔掉 mod 後原版字型顯示不出來（2026-10-03 解開使用者的存檔確認）。
    /// 所以存檔前把這些欄位的 I2 譯文換回英文，讀檔後再換回中文：遊戲執行時看到的和沒轉換時一樣，硬碟上的存檔和原版相同。
    ///
    /// 只動這幾個欄位（都是 I2 語言表的整句，沒有代入名字或數字）：
    /// - scenarioCharacterName：每日情境的玩家稱號
    /// - babyName、cameraRigDetails 的 name：收藏的嬰兒名字（後者只拿來當 GameObject 名稱）
    /// - lifeEventString：收藏嬰兒的人生事件
    /// 不能整份取代：speciesReference、cameraRigDetails 的 species 這類參照（例如 Robot）剛好也是語言表的英文，換成中文遊戲就找不到了。
    /// 這個類別不碰 Unity，方便在遊戲外測試。
    /// </summary>
    internal static class SaveText
    {
        static readonly Regex Field = new Regex(
            "(?<head>\"(?:scenarioCharacterName|babyName|lifeEventString)\"\\s*:\\s*|\"cameraRigDetails\"\\s*:\\s*\\{\\s*\"name\"\\s*:\\s*)" +
            "\"(?<val>(?:[^\"\\\\]|\\\\.)*)\"",
            RegexOptions.Compiled);

        /// <summary>把 JSON 裡那幾個欄位的值用 map 換掉；查不到（map 傳回原值）的保持原樣，一個位元組都不動。</summary>
        public static string Convert(string json, Func<string, string> map, out int changed)
        {
            int n = 0;
            var result = Field.Replace(json, m =>
            {
                var value = Unescape(m.Groups["val"].Value);
                var mapped = map(value);
                if (mapped == null || mapped == value) return m.Value;
                n++;
                return m.Groups["head"].Value + "\"" + Escape(mapped) + "\"";
            });
            changed = n;
            return result;
        }

        static string Unescape(string s)
        {
            if (s.IndexOf('\\') < 0) return s;
            var sb = new StringBuilder(s.Length);
            for (int i = 0; i < s.Length; i++)
            {
                char c = s[i];
                if (c != '\\' || i + 1 >= s.Length) { sb.Append(c); continue; }
                char e = s[++i];
                switch (e)
                {
                    case 'n': sb.Append('\n'); break;
                    case 'r': sb.Append('\r'); break;
                    case 't': sb.Append('\t'); break;
                    case 'b': sb.Append('\b'); break;
                    case 'f': sb.Append('\f'); break;
                    case 'u':
                        if (i + 4 < s.Length && int.TryParse(s.Substring(i + 1, 4), System.Globalization.NumberStyles.HexNumber, null, out int code))
                        {
                            sb.Append((char)code);
                            i += 4;
                        }
                        else sb.Append("\\u");
                        break;
                    default: sb.Append(e); break; // \" \\ \/
                }
            }
            return sb.ToString();
        }

        static string Escape(string s)
        {
            var sb = new StringBuilder(s.Length + 8);
            foreach (char c in s)
            {
                switch (c)
                {
                    case '"': sb.Append("\\\""); break;
                    case '\\': sb.Append("\\\\"); break;
                    case '\n': sb.Append("\\n"); break;
                    case '\r': sb.Append("\\r"); break;
                    case '\t': sb.Append("\\t"); break;
                    default:
                        if (c < 0x20) sb.Append("\\u").Append(((int)c).ToString("x4"));
                        else sb.Append(c);
                        break;
                }
            }
            return sb.ToString();
        }
    }
}
