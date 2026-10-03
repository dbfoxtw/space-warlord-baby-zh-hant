using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using HarmonyLib;
using I2.Loc;
using MelonLoader;
using MelonLoader.Utils;
using TMPro;
using UnityEngine;

[assembly: MelonInfo(typeof(SwbZhHant.ZhHantMod), "Space Warlord Baby 繁體中文", "1.0.1", "dbfoxtw")]
[assembly: MelonGame("Strange Scaffold", "Space Warlord Baby Trading Simulator")]
[assembly: HarmonyDontPatchAll] // 翻譯資料載入後才手動掛上攔截

namespace SwbZhHant
{
    /// <summary>
    /// 遊戲只有英文：I2 語言表載入時把英文欄換成譯文；寫死在場景與程式碼裡的英文在顯示時換掉。
    /// 不改遊戲檔與存檔。存檔會存下顯示中的嬰兒名字與事件文字，所以舊存檔的英文也在顯示時換掉。
    /// 字型見 SetupFonts 與 StmRebuildPrefix 的說明。
    /// </summary>
    public class ZhHantMod : MelonMod
    {
        internal static Translator Tr;
        internal static MelonLogger.Instance Log;
        /// <summary>UserData\ZhHant\debug 存在時開啟：記錄沒翻到的英文（misses.tsv）、字型缺字自我測試。</summary>
        internal static bool DebugMode;
        const string SilverName = "Silver";
        const string SilverSdfName = "Silver SDF";
        /// <summary>STM 裡會換成中文像素字型的字型：主字型 Silver、標題用的 Astrolab（99 字，沒有中文）。</summary>
        static readonly HashSet<string> ReplacedFonts = new HashSet<string> { SilverName, "Astrolab" };
        /// <summary>
        /// 遊戲介面先畫在約 640×360 的低解析度畫面再放大，STM 的 size 18 文字 1 em＝18 個畫面像素。
        /// mod 的像素字型 1 em＝18 像素（tools/make_font.py），字型像素和畫面像素一對一；
        /// 點陣化大小（quality）也要是 18 的整數倍，否則點陣化時就掉像素。
        /// </summary>
        const int PixelEm = 18;
        /// <summary>Silver 的 1 em＝19 像素：在 size 18 一定會掉像素，沒有中文字型包時只能盡量讓點陣化對齊。</summary>
        const int SilverEm = 19;
        /// <summary>缺字時的系統字型。Unity 動態字型缺字時依序找 fontNames 裡的字型。</summary>
        const string FallbackOsFont = "Microsoft JhengHei";
        static string _dataDir;
        /// <summary>
        /// UserData\ZhHant\pixel.bundle 裡的字型（俐方體11號改成 Silver 像素格，見 tools/make_font.py、make_font_bundle.py）；
        /// 沒有就用 Silver。
        /// </summary>
        static Font _pixelFont;
        static bool _pixelFontTried;
        TMP_FontAsset _tmpFallback;
        bool _fontsReady, _fontTested;

        public override void OnInitializeMelon()
        {
            Log = LoggerInstance;
            _dataDir = Path.Combine(MelonEnvironment.UserDataDirectory, "ZhHant");
            DebugMode = File.Exists(Path.Combine(_dataDir, "debug"));
            if (DebugMode) File.Delete(Path.Combine(_dataDir, "misses.tsv")); // 每次啟動重新累積
            Tr = Translator.Load(_dataDir, Log, DebugMode);
            Log.Msg($"載入翻譯資料：I2 譯文 {Tr.TermCount} 條、寫死文字整句 {Tr.ExactCount} 條、規則 {Tr.RuleCount} 條{(DebugMode ? "；除錯模式" : "")}");

            // I2Languages 在第一次需要翻譯時才註冊（LocalizationManager.AddSource），在那之後立刻換成譯文
            Patch(AccessTools.Method(typeof(LocalizationManager), "AddSource"), postfix: nameof(AddSourcePostfix));
            foreach (var s in LocalizationManager.Sources) Tr.ApplyToSource(s);

            // 存檔：寫入前把顯示文字換回英文、讀檔後換回中文（見 SaveText）
            Patch(AccessTools.Method(typeof(SaveSystem), nameof(SaveSystem.EncryptDecrypt)),
                prefix: nameof(SaveEncryptPrefix), postfix: nameof(SaveDecryptPostfix));

            // SuperTextMesh：所有 Rebuild 多載都走到這個，場景裡序列化的文字也會經過
            Patch(AccessTools.Method(typeof(SuperTextMesh), nameof(SuperTextMesh.Rebuild), new[] { typeof(float), typeof(bool), typeof(bool) }),
                prefix: nameof(StmRebuildPrefix));
            // 中文字的頂點對齊像素格（見 SnapVerts）：三個方法分別算好讀完、讀到一半、還沒讀的頂點，SetMesh 再交給畫面
            Patch(AccessTools.Method(typeof(SuperTextMesh), "UpdateMesh"), postfix: nameof(StmEndVertsPostfix));
            Patch(AccessTools.Method(typeof(SuperTextMesh), "UpdateDrawnMesh"), postfix: nameof(StmMidVertsPostfix));
            Patch(AccessTools.Method(typeof(SuperTextMesh), "UpdatePreReadMesh"), postfix: nameof(StmStartVertsPostfix));
            // 標題描邊在執行時被改色（見 EffectColorPrefix）
            Patch(AccessTools.PropertySetter(typeof(UnityEngine.UI.Shadow), nameof(UnityEngine.UI.Shadow.effectColor)),
                prefix: nameof(EffectColorPrefix));

            int n = 0;
            Patch(AccessTools.PropertySetter(typeof(TMP_Text), nameof(TMP_Text.text)), prefix: nameof(FirstArgPrefix));
            n++;
            foreach (var m in typeof(TMP_Text).GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly))
            {
                var ps = m.GetParameters();
                if (m.Name != "SetText" || ps.Length == 0 || ps[0].ParameterType != typeof(string)) continue;
                Patch(m, prefix: nameof(FirstArgPrefix));
                n++;
            }
            Patch(AccessTools.PropertySetter(typeof(UnityEngine.UI.Text), nameof(UnityEngine.UI.Text.text)), prefix: nameof(UguiTextPrefix));
            n++;
            Log.Msg($"已攔截 SuperTextMesh.Rebuild 與 TMP／UGUI 的 {n} 個文字設定方法");
        }

        void Patch(MethodInfo target, string prefix = null, string postfix = null)
        {
            if (target == null) { Log.Error($"找不到要攔截的方法（{prefix ?? postfix}）"); return; }
            HarmonyMethod Hm(string name) => name == null ? null : new HarmonyMethod(typeof(ZhHantMod).GetMethod(name, BindingFlags.Static | BindingFlags.NonPublic));
            HarmonyInstance.Patch(target, Hm(prefix), Hm(postfix));
        }

        static void AddSourcePostfix(LanguageSourceData Source)
        {
            try { Tr.ApplyToSource(Source); }
            catch (Exception e) { Log.Error($"套用 I2 譯文失敗：{e}"); }
        }

        /// <summary>
        /// SaveSystem.EncryptDecrypt(data, loading)：存檔時 data 是 JSON（加密前），讀檔時傳回值是 JSON（解密後）。
        /// 轉換失敗一律維持原樣，絕不擋住存讀檔。
        /// </summary>
        static void SaveEncryptPrefix(ref string data, bool loading)
        {
            if (loading || string.IsNullOrEmpty(data) || !Tr.SaveMapsReady) return;
            try
            {
                data = SaveText.Convert(data, Tr.SaveToEnglish, out int n);
                if (DebugMode && n > 0) Log.Msg($"存檔：{n} 個欄位換回英文");
            }
            catch (Exception e) { Log.Error($"存檔轉換失敗，照原樣存檔：{e}"); }
        }

        static void SaveDecryptPostfix(ref string __result, bool loading)
        {
            if (!loading || string.IsNullOrEmpty(__result)) return;
            try
            {
                // 讀檔可能早於第一次翻譯（語言表還沒註冊）：照遊戲自己第一次取譯文時的做法先初始化 I2
                if (!Tr.SaveMapsReady) LocalizationManager.InitializeIfNeeded();
                if (!Tr.SaveMapsReady) { Log.Warning("讀檔時語言表還沒換成譯文，存檔裡的英文改在顯示時翻譯"); return; }
                __result = SaveText.Convert(__result, Tr.SaveToChinese, out int n);
                Log.Msg($"讀檔：{n} 個欄位換成中文");
            }
            catch (Exception e) { Log.Error($"讀檔轉換失敗，照原樣讀檔：{e}"); }
        }

        /// <summary>
        /// 第一次需要時才載入（要在 Unity 主執行緒、引擎初始化後）。
        /// 不能用 new Font(路徑)：那樣建的字型只有 TMP 的字型引擎讀得到檔案，Unity 傳統字型系統（STM 用的）
        /// 一個字都畫不出來（2026-10-03 實測，中文整個消失）。所以字型包成 AssetBundle，載入內含字型資料的 Font 物件。
        /// 載入後先試畫一個字，畫不出來就不用，免得中文消失。
        /// </summary>
        static Font PixelFont()
        {
            if (_pixelFontTried) return _pixelFont;
            _pixelFontTried = true;
            var path = Path.Combine(_dataDir, "pixel.bundle");
            if (!File.Exists(path)) { Log.Msg("沒有 pixel.bundle，中文用 Silver 本身的漢字"); return null; }
            try
            {
                var bundle = AssetBundle.LoadFromFile(path); // 不卸載：字型要用到遊戲結束
                if (bundle == null) { Log.Error($"{path} 載入失敗（AssetBundle.LoadFromFile 傳回 null），改用 Silver"); return null; }
                var f = bundle.LoadAllAssets<Font>().FirstOrDefault();
                if (f == null) { Log.Error($"{path} 裡沒有字型，改用 Silver"); return null; }
                f.RequestCharactersInTexture("中", PixelEm);
                if (!f.GetCharacterInfo('中', out _, PixelEm))
                {
                    Log.Error($"{f.name} 畫不出字，改用 Silver");
                    return null;
                }
                _pixelFont = f;
                Log.Msg($"中文像素字型：{f.name}（{path}，dynamic={f.dynamic}、fontNames {string.Join("、", f.fontNames ?? Array.Empty<string>())}）");
            }
            catch (Exception e) { Log.Error($"載入 pixel.bundle 失敗，改用 Silver：{e}"); }
            return _pixelFont;
        }

        static readonly AccessTools.FieldRef<SuperTextMesh, Vector3[]> EndVerts = AccessTools.FieldRefAccess<SuperTextMesh, Vector3[]>("endVerts");
        static readonly AccessTools.FieldRef<SuperTextMesh, Vector3[]> MidVerts = AccessTools.FieldRefAccess<SuperTextMesh, Vector3[]>("midVerts");
        static readonly AccessTools.FieldRef<SuperTextMesh, Vector3[]> StartVerts = AccessTools.FieldRefAccess<SuperTextMesh, Vector3[]>("startVerts");

        static void StmEndVertsPostfix(SuperTextMesh __instance) => SnapVerts(__instance, EndVerts(__instance));
        static void StmMidVertsPostfix(SuperTextMesh __instance) => SnapVerts(__instance, MidVerts(__instance));
        static void StmStartVertsPostfix(SuperTextMesh __instance) => SnapVerts(__instance, StartVerts(__instance));

        /// <summary>
        /// 含中文的介面 STM：頂點換算到根 Canvas 座標（＝畫面像素：Pixel Camera 正交大小 180、640×360 的 RenderTexture、
        /// Canvas 在原點，所以 Canvas 的整數座標就是像素邊界）後取整數，再換回本地座標。
        /// 字的四邊形和字型貼圖都是整數像素（size 是 quality 的整數比，StmRebuildPrefix 已經對齊），只要頂點落在像素邊界，
        /// 貼圖就一列不差地畫出來；落在小數像素上，材質的 PIXELSNAP 會把上下緣各自四捨五入，字可能少一列。
        /// 2026-10-03 星球趨勢說明（行距 0.8、垂直置中、掛 STMPixelSnap）「升」少了長橫：
        /// - STM 產生頂點時整段再加 (lineSpacing − 1) × size（0.8 × 18 → −3.6 像素）與置中的 anchorOffset；
        /// - 遊戲的 STMPixelSnap 再依文字框寬高平移一段小數（它的 snapping 值是開發者對英文逐一手調的，0.25、0.826…）；
        /// - 面板打開的動畫停下時，文字物件本身也可能在小數位置。
        /// 只在本地座標取整數不夠（第二次修正實測無效），所以每次算頂點時用當下的位置換算。
        /// 實測（第九次，除錯紀錄已拿掉）：網格本身正確（四邊形高 13＝貼圖高 13、縮放 1），但滑入動畫中的文字原點在 504.127、499.9 這種位置；
        /// 改成 Canvas 座標取整數後，逐列比對截圖「升」的 11 列都在。
        /// 掛 STMPixelSnap 的文字每一格都會重算頂點（它讓 STM 進入動畫狀態），面板滑動停下後也會對齊。
        /// 只動 RectTransform 的 STM：3D 場景裡的 STM 單位是公尺。原版英文不動，維持遊戲原本的樣子。
        /// </summary>
        static void SnapVerts(SuperTextMesh stm, Vector3[] verts)
        {
            try
            {
                if (verts == null || verts.Length == 0 || !stm.uiMode || !Translator.HasCjk(stm._text)) return;
                var root = stm.t.GetComponentInParent<Canvas>()?.rootCanvas;
                var toCanvas = root != null ? root.transform.worldToLocalMatrix * stm.t.localToWorldMatrix : Matrix4x4.identity;
                var toLocal = toCanvas.inverse;
                for (int i = 0; i < verts.Length; i++)
                {
                    var c = toCanvas.MultiplyPoint3x4(verts[i]);
                    c.x = Mathf.Floor(c.x + 0.5f);
                    c.y = Mathf.Floor(c.y + 0.5f);
                    verts[i] = toLocal.MultiplyPoint3x4(c);
                }
            }
            catch (Exception e) { Log.Error($"STM 頂點對齊失敗：{e.Message}"); }
        }

        /// <summary>
        /// 含中文的 STM：
        /// - 中文沒有空白，STM 預設只在空白處換行，逐字斷行時還會在行尾補「-」。改成逐字換行（breakText）、
        ///   不補連字號；標點禁則用 STM 內建的。
        /// - 字型是 Silver／Astrolab 時換成 pixel.bundle 的字型（有的話），行高和 Silver 幾乎相同，字級與版面不用動。
        /// - quality 改成字型 em 的整數倍（取最接近原值的倍數）：一般文字 18→18，標題等原本用高 quality 的 64→72
        ///   （保留高解析度給描邊之類的效果）。重複呼叫結果不變。
        /// </summary>
        static void StmRebuildPrefix(SuperTextMesh __instance)
        {
            try
            {
                var t = __instance._text;
                var r = Tr.Translate(t);
                if (r != t) __instance._text = r;
                if (!Translator.HasCjk(r)) return;
                __instance.breakText = true;
                __instance.insertHyphens = false;
                var font = __instance.font;
                if (font == null) return;
                var pixel = PixelFont();
                if (pixel != null && font != pixel && ReplacedFonts.Contains(font.name))
                {
                    __instance.font = font = pixel;
                    // 標題字級是 20、24、14：不是 18 的倍數時每個字型像素不等寬（小於 18 還會掉像素），對齊到 18 的倍數
                    if (__instance.size >= 12 && __instance.size % PixelEm != 0)
                        __instance.size = PixelEm * Math.Max(1, (int)Math.Round(__instance.size / PixelEm));
                    SwapOutlineColors(__instance);
                }
                int em = font == pixel ? PixelEm : font.name == SilverName ? SilverEm : 0;
                if (em > 0)
                {
                    __instance.autoQuality = false;
                    __instance.quality = em * Math.Max(1, (int)Math.Round(__instance.quality / (double)em));
                }
            }
            catch (Exception e) { Log.Error($"STM 翻譯失敗：{e.Message}"); }
        }

        /// <summary>
        /// 標題（21 個，都是 Astrolab）的設計是「深色字＋UGUI Outline 的彩色描邊」，描邊只往四個斜角偏 1 像素。
        /// Astrolab 筆畫粗，看得到描邊；中文像素字筆畫只有 1 像素，斜角的描邊零零落落，整個標題變成深色字
        /// （2026-10-03 第四次測試：「關於本遊戲」幾乎看不到）。字比描邊暗時對調兩個顏色，變成彩色字＋深色描邊。
        /// 只在換字型的那一次做（之後字型已經換掉，不會再進來），所以不會來回對調。
        /// 對調過的描邊記在 SwappedOutlines：遊戲之後在執行時改描邊顏色（劇情結局依獎盃改成銅／銀／金、
        /// 放空評價的跳字），由 EffectColorPrefix 把新顏色改給字、描邊維持深色。
        /// </summary>
        static void SwapOutlineColors(SuperTextMesh stm)
        {
            var outline = stm.GetComponent<UnityEngine.UI.Outline>();
            if (outline == null || !outline.enabled) return;
            Color text = stm.color, edge = outline.effectColor;
            if (Luma(text) >= Luma(edge)) return;
            stm.color = new Color(edge.r, edge.g, edge.b, text.a);
            outline.effectColor = new Color(text.r, text.g, text.b, edge.a);
            SwappedOutlines.Remove(outline);
            SwappedOutlines.Add(outline, new SwappedOutline { Stm = stm, Dark = outline.effectColor });
        }

        class SwappedOutline
        {
            public SuperTextMesh Stm;
            public Color Dark;
        }

        /// <summary>弱參照：場景卸載、物件銷毀後自動釋放。</summary>
        static readonly ConditionalWeakTable<UnityEngine.UI.Shadow, SwappedOutline> SwappedOutlines =
            new ConditionalWeakTable<UnityEngine.UI.Shadow, SwappedOutline>();

        /// <summary>
        /// 對調過顏色的標題，遊戲在執行時把描邊改成亮色：原版的意思是「深色字＋這個顏色的描邊」，
        /// 對中文像素字就改成「這個顏色的字＋深色描邊」（2026-10-03 實機：劇情結局的「劇情完成」被改成銀色描邊後糊成一團）。
        /// </summary>
        static void EffectColorPrefix(UnityEngine.UI.Shadow __instance, ref Color value)
        {
            try
            {
                if (!SwappedOutlines.TryGetValue(__instance, out var info) || info.Stm == null) return;
                if (Luma(value) <= Luma(info.Dark) + 0.01f) return;
                var a = info.Stm.color.a;
                info.Stm.color = new Color(value.r, value.g, value.b, a);
                value = new Color(info.Dark.r, info.Dark.g, info.Dark.b, value.a);
                info.Stm.Rebuild();
            }
            catch (Exception e) { Log.Error($"標題描邊顏色調整失敗：{e.Message}"); }
        }

        static float Luma(Color c) => 0.299f * c.r + 0.587f * c.g + 0.114f * c.b;

        /// <summary>TMP、UGUI 的文字設定方法：第一個參數就是要顯示的文字。</summary>
        static void FirstArgPrefix(ref string __0)
        {
            try { __0 = Tr.Translate(__0); }
            catch (Exception e) { Log.Error($"翻譯失敗：{e.Message}"); }
        }

        /// <summary>
        /// UGUI Text：翻譯；含中文、字型是 Silver／Astrolab 時換成中文像素字型（和 STM 一樣，字級對齊 18 的倍數）。
        /// 沒換的話中文用 Silver 自帶的漢字：19 像素格畫在 18 的字級上，掉像素又窄
        /// （2026-10-03 收藏嬰兒年齡下方的「歲／個月」擠成一團）。
        /// </summary>
        static void UguiTextPrefix(UnityEngine.UI.Text __instance, ref string __0)
        {
            try
            {
                __0 = Tr.Translate(__0);
                if (!Translator.HasCjk(__0)) return;
                var font = __instance.font;
                var pixel = PixelFont();
                if (font == null || pixel == null || font == pixel || !ReplacedFonts.Contains(font.name)) return;
                __instance.font = pixel;
                if (__instance.fontSize >= 12 && __instance.fontSize % PixelEm != 0)
                    __instance.fontSize = PixelEm * Math.Max(1, (int)Math.Round(__instance.fontSize / (double)PixelEm));
            }
            catch (Exception e) { Log.Error($"UGUI 翻譯失敗：{e.Message}"); }
        }

        public override void OnSceneWasInitialized(int buildIndex, string sceneName)
        {
            SetupFonts();
            // 場景與預製物件裡序列化好的 TMP／UGUI 文字不會經過 setter
            int changed = 0;
            foreach (var t in Resources.FindObjectsOfTypeAll<TMP_Text>())
            {
                var s = t.text;
                if (string.IsNullOrEmpty(s)) continue;
                var r = Tr.Translate(s);
                if (r != s) { t.text = r; changed++; }
            }
            foreach (var t in Resources.FindObjectsOfTypeAll<UnityEngine.UI.Text>())
            {
                var s = t.text;
                if (string.IsNullOrEmpty(s)) continue;
                var r = Tr.Translate(s);
                if (r != s) { t.text = r; changed++; }
            }
            if (changed > 0) Log.Msg($"場景 {sceneName}：換掉 {changed} 個 TMP／UGUI 的寫死文字");
            if (DebugMode && !_fontTested && _fontsReady) FontSelfTest();
        }

        /// <summary>
        /// - Silver 缺字時讓 Unity 從系統的微軟正黑體補（fontNames）。
        /// - TMP 的 Silver SDF 是只有 95 字的靜態字型：從中文像素字型（沒有就用 Silver）建動態字型，插到全域 fallback 最前面。
        /// </summary>
        void SetupFonts()
        {
            if (_fontsReady) return;
            var silver = Resources.FindObjectsOfTypeAll<Font>().FirstOrDefault(f => f.name == SilverName);
            var silverSdf = Resources.FindObjectsOfTypeAll<TMP_FontAsset>().FirstOrDefault(f => f.name == SilverSdfName);
            if (silver == null || silverSdf == null) return; // 還沒載入，下一個場景再試
            _fontsReady = true;

            var names = (silver.fontNames ?? Array.Empty<string>()).ToList();
            if (!names.Contains(FallbackOsFont))
            {
                names.Add(FallbackOsFont);
                silver.fontNames = names.ToArray();
            }

            var source = PixelFont() ?? silver;
            // 取樣大小＝1 em 的像素數：mod 字型 18，Silver 沿用 Silver SDF 的 19
            int pointSize = source == silver ? (int)silverSdf.faceInfo.pointSize : PixelEm;
            try
            {
                _tmpFallback = TMP_FontAsset.CreateFontAsset(source, pointSize, silverSdf.atlasPadding,
                    silverSdf.atlasRenderMode, 2048, 2048, AtlasPopulationMode.Dynamic, true);
                if (_tmpFallback == null) throw new Exception("CreateFontAsset 傳回 null（字型檔沒有包含字型資料？）");
                _tmpFallback.name = "ZhHant TMP fallback";
                _tmpFallback.hideFlags = HideFlags.DontUnloadUnusedAsset;
                var global = TMP_Settings.fallbackFontAssets;
                if (global == null) Log.Error("TMP_Settings.fallbackFontAssets 是 null，TMP 文字無法顯示中文");
                else
                {
                    global.Insert(0, _tmpFallback);
                    Log.Msg($"TMP：從 {source.name} 建立動態字型（取樣 {pointSize}、padding {silverSdf.atlasPadding}、{silverSdf.atlasRenderMode}），加入全域 fallback");
                }
            }
            catch (Exception e) { Log.Error($"建立 TMP 中文 fallback 失敗：{e}"); }
        }

        /// <summary>
        /// 確認譯文用到的每個字在兩種文字元件都產生得出來（不用等到遊戲裡剛好出現那句話）。
        /// STM：字型檔本身沒有的字（Font.HasCharacter 不算系統字型補的），看 Unity 能不能從 fontNames 補上。
        /// TMP：試著加進動態圖集。
        /// </summary>
        void FontSelfTest()
        {
            _fontTested = true;
            var font = PixelFont() ?? Resources.FindObjectsOfTypeAll<Font>().FirstOrDefault(f => f.name == SilverName);
            var chars = new string(Tr.Chars.OrderBy(c => c).ToArray());
            var notInFont = new List<char>();
            var noGlyph = new List<char>();
            int em = font == PixelFont() ? PixelEm : SilverEm;
            font.RequestCharactersInTexture(chars, em);
            foreach (var c in chars)
            {
                if (font.HasCharacter(c)) continue;
                notInFont.Add(c);
                if (!font.GetCharacterInfo(c, out _, em)) noGlyph.Add(c);
            }
            Log.Msg($"字型自我測試（{font.name}）：譯文用到 {chars.Length} 個非 ASCII 字；字型沒有 {notInFont.Count} 個：{new string(notInFont.ToArray())}");
            Log.Msg($"  其中 Unity 也產生不出字形的 {noGlyph.Count} 個：{new string(noGlyph.ToArray())}");
            if (_tmpFallback != null)
            {
                _tmpFallback.TryAddCharacters(chars, out string missing);
                Log.Msg($"  TMP 動態字型加不進去的 {missing?.Length ?? 0} 個：{missing}");
            }
        }
    }
}
