# 中文像素字型

發布版附的中文字型是 **SWB ZhHant Pixel**：俐方體11號（Cubic 11）的修改版，1 em 改成 18 像素，對齊遊戲介面的像素（遊戲先畫在約 640×360 的低解析度畫面再放大，size 18 的文字 1 em＝18 個畫面像素）。授權是 SIL Open Font License 1.1（不在本 repo 的 MIT 授權範圍內）。

字型檔不放在 repo 裡，從原始碼建置時自己產生：

1. 從 [Cubic-11 repo 的 tag v1.500](https://github.com/ACh-K/Cubic-11/tree/v1.500) 下載 `fonts/ttf/Cubic_11.ttf`（2,773,732 bytes）與根目錄的授權檔 `OFL.txt`，放到 `fonts/candidates/`，授權檔改名成 `Cubic_11.OFL.txt`。
2. `pip install -r requirements.txt`
3. `python tools/make_font.py`：產生 `fonts/zhhant-pixel.ttf` 與授權檔 `fonts/zhhant-pixel.LICENSE.txt`。
4. `python tools/make_font_bundle.py`：把字型包成 AssetBundle `fonts/zhhant-pixel.bundle`（要讀遊戲檔，只讀不寫）。

為什麼要包成 AssetBundle：遊戲的文字幾乎都用 SuperTextMesh，它用 Unity 傳統字型（`UnityEngine.Font`）。執行時用 `new Font(路徑)` 建的字型沒有字型資料，傳統字型系統畫不出字，所以要一個內含字型資料的 Font 物件，只能從 AssetBundle 載入。細節見兩支腳本開頭的說明。
