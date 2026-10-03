using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using I2.Loc;
using MelonLoader;

namespace SwbZhHant
{
    /// <summary>
    /// 翻譯資料（UserData\ZhHant\）與兩種套用方式：
    /// 1. I2 語言表載入時，把英文欄換成譯文（i2.tsv，以 term 為鍵，附英文原文的雜湊）。
    /// 2. 顯示文字時查表（英文→中文）：I2 換掉的英文（舊存檔存的是英文）、hardcoded.tsv 的寫死文字與正規表示式。
    /// TSV 跳脫（只有 \n、\r、\t，反斜線不跳脫）與雜湊規則和 tools/swb/tsv.py 相同，兩邊要一起改。
    /// </summary>
    internal class Translator
    {
        readonly MelonLogger.Instance _log;
        readonly bool _debug;
        readonly Dictionary<string, (uint hash, string zh)> _terms = new Dictionary<string, (uint, string)>(StringComparer.Ordinal);
        /// <summary>整句對照：英文（去頭尾空白）→中文。I2 的部分在語言表載入時才建立。</summary>
        readonly Dictionary<string, string> _exact = new Dictionary<string, string>(StringComparer.Ordinal);
        readonly List<(string trigger, Regex regex, string replacement)> _rules = new List<(string, Regex, string)>();
        readonly HashSet<LanguageSourceData> _applied = new HashSet<LanguageSourceData>();
        readonly HashSet<string> _misses = new HashSet<string>(StringComparer.Ordinal);
        readonly string _missPath;
        static readonly Regex Tag = new Regex("<[^>]*>", RegexOptions.Compiled);
        static readonly Regex Letters = new Regex("[A-Za-z]{3,}", RegexOptions.Compiled);

        public int TermCount => _terms.Count;
        public int RuleCount => _rules.Count;
        public int ExactCount => _exact.Count;
        /// <summary>所有譯文用到的字（除錯模式的字型自我測試用）。</summary>
        public readonly HashSet<char> Chars = new HashSet<char>();

        Translator(MelonLogger.Instance log, bool debug, string dir)
        {
            _log = log;
            _debug = debug;
            _missPath = Path.Combine(dir, "misses.tsv");
        }

        public static Translator Load(string dir, MelonLogger.Instance log, bool debug)
        {
            var tr = new Translator(log, debug, dir);
            var i2 = Path.Combine(dir, "i2.tsv");
            if (File.Exists(i2))
                foreach (var c in ReadRows(i2))
                {
                    if (c.Length < 3) continue;
                    tr._terms[c[0]] = (Convert.ToUInt32(c[1], 16), c[2]);
                    tr.AddChars(c[2]);
                }
            else log.Warning($"找不到 {i2}，I2 語言表不會翻譯");
            var hard = Path.Combine(dir, "hardcoded.tsv");
            if (File.Exists(hard))
                foreach (var c in ReadRows(hard))
                {
                    if (c.Length == 2) tr._exact[c[0].Trim()] = c[1];
                    else if (c.Length >= 3) tr._rules.Add((c[0], new Regex(c[1], RegexOptions.Compiled), c[2]));
                    tr.AddChars(c[c.Length - 1]);
                }
            return tr;
        }

        void AddChars(string s)
        {
            foreach (var ch in Tag.Replace(s, "")) if (ch > 0x7F) Chars.Add(ch);
        }

        /// <summary>把語言表的英文欄換成譯文。英文和翻譯時不同（遊戲改版）的條目跳過，維持英文。</summary>
        public void ApplyToSource(LanguageSourceData src)
        {
            if (src == null || !_applied.Add(src)) return;
            int en = -1;
            for (int i = 0; i < src.mLanguages.Count; i++)
                if (src.mLanguages[i].Code == "en" || src.mLanguages[i].Name == "English") { en = i; break; }
            if (en < 0) { _log.Warning("語言表沒有英文欄，略過"); return; }
            int applied = 0, missing = 0, changed = 0;
            var changedTerms = new List<string>();
            foreach (var t in src.mTerms)
            {
                if (t?.Languages == null || en >= t.Languages.Length) continue;
                var orig = t.Languages[en];
                if (!_terms.TryGetValue(t.Term, out var e)) { missing++; continue; }
                if (Fnv1a(orig ?? "") != e.hash) { changed++; changedTerms.Add(t.Term); continue; }
                t.Languages[en] = e.zh;
                var key = (orig ?? "").Trim();
                if (key.Length > 0 && !_exact.ContainsKey(key)) _exact[key] = e.zh;
                applied++;
            }
            _log.Msg($"I2 語言表：套用 {applied} 條、沒有譯文 {missing} 條（維持英文）、原文已變動 {changed} 條（暫停套用）");
            if (changed > 0) _log.Warning("原文已變動、暫停套用：" + string.Join("、", changedTerms.GetRange(0, Math.Min(20, changedTerms.Count))));
        }

        /// <summary>
        /// 顯示前的翻譯。已含中文的字串也要跑規則：程式碼拼接的句子裡，星球名等片段已經由 I2 換成中文。
        /// 查不到時原樣傳回，除錯模式把像英文的字串記到 misses.tsv。
        /// </summary>
        public string Translate(string s)
        {
            if (string.IsNullOrEmpty(s)) return s;
            var key = s.Trim();
            if (_exact.TryGetValue(key, out var zh))
                return key.Length == s.Length ? zh : s.Replace(key, zh);
            var r = s;
            foreach (var (trigger, regex, replacement) in _rules)
                if (r.IndexOf(trigger, StringComparison.Ordinal) >= 0) r = regex.Replace(r, replacement);
            if (_debug && r == s) RecordMiss(s);
            return r;
        }

        void RecordMiss(string s)
        {
            if (!_debug || !Letters.IsMatch(Tag.Replace(s, "")) || !_misses.Add(s)) return;
            try { File.AppendAllText(_missPath, Escape(s) + "\n", Encoding.UTF8); }
            catch (Exception e) { _log.Warning($"寫入 misses.tsv 失敗：{e.Message}"); }
        }

        public static bool HasCjk(string s)
        {
            foreach (var ch in s)
                if ((ch >= 0x3000 && ch <= 0x9FFF) || (ch >= 0xFF00 && ch <= 0xFFEF)) return true;
            return false;
        }

        public static uint Fnv1a(string s)
        {
            uint h = 0x811C9DC5;
            foreach (var b in Encoding.UTF8.GetBytes(s)) h = (h ^ b) * 0x01000193;
            return h;
        }

        static IEnumerable<string[]> ReadRows(string path)
        {
            foreach (var line in File.ReadAllLines(path, Encoding.UTF8))
            {
                if (line.Length == 0 || line[0] == '#') continue;
                var cells = line.Split('\t');
                for (int i = 0; i < cells.Length; i++) cells[i] = Unescape(cells[i]);
                yield return cells;
            }
        }

        static string Unescape(string s)
        {
            if (s.IndexOf('\\') < 0) return s;
            var sb = new StringBuilder(s.Length);
            for (int i = 0; i < s.Length; i++)
            {
                if (s[i] == '\\' && i + 1 < s.Length)
                {
                    switch (s[i + 1])
                    {
                        case 'n': sb.Append('\n'); i++; continue;
                        case 'r': sb.Append('\r'); i++; continue;
                        case 't': sb.Append('\t'); i++; continue;
                    }
                }
                sb.Append(s[i]);
            }
            return sb.ToString();
        }

        static string Escape(string s) => s.Replace("\n", "\\n").Replace("\r", "\\r").Replace("\t", "\\t");
    }
}
