# Space Warlord Baby Trading Simulator 繁體中文化 mod（非官方）

*Space Warlord Baby Trading Simulator* 的繁體中文（台灣用語）翻譯 mod，使用 MelonLoader。非官方製作。

對應遊戲版本：2026.21.a（主選單左下角）

下載：本 repo 的 [Releases](https://github.com/dbfoxtw/space-warlord-baby-zh-hant/releases)。版本紀錄見 [CHANGELOG.md](CHANGELOG.md)。

翻譯是用 AI 翻譯、校對的，如果有翻不順的地方歡迎回報。這個 mod 還沒有玩遍所有劇情，如果遇到沒翻到的英文或任何 bug，也歡迎到 [Issues](https://github.com/dbfoxtw/space-warlord-baby-zh-hant/issues) 回報（附上截圖最好）。

> Unofficial Traditional Chinese (Taiwan) translation mod for *Space Warlord Baby Trading Simulator*, built on MelonLoader. See [English](#english) below.

## 這是什麼

- 遊戲只有英文。這個 mod 把劇情、星球與嬰兒的人生事件、顧問台詞、介面翻成繁體中文（台灣用語），裝好就是中文，不用另外設定。
- 附中文像素字型（俐方體11號的修改版），配合遊戲低解析度畫面的字級，中文清楚不糊。
- 語音指令仍然只能用英文（遊戲只辨識英文），說明文字會保留英文指令。

## 安裝

要先裝 MelonLoader（讓遊戲能載入 mod 的工具），再裝這個 mod。MelonLoader 只要裝一次。

**找到遊戲資料夾**：Steam 遊戲庫 → 在 Space Warlord Baby Trading Simulator 上按右鍵 → 管理 → 瀏覽本機檔案。開啟的資料夾裡有 `Space Warlord Baby Trading Simulator.exe`，以下說的「遊戲資料夾」都是這裡。

**第一步：安裝 MelonLoader 0.7.3**

1. 到 [MelonLoader v0.7.3 的下載頁](https://github.com/LavaGang/MelonLoader/releases/tag/v0.7.3)，在 Assets 底下下載 **`MelonLoader.x64.zip`**（不是 x86，也不是 Installer）。
2. 把 `MelonLoader.x64.zip` 的內容**解壓縮到遊戲資料夾**（和 `Space Warlord Baby Trading Simulator.exe` 同一層）。解壓後，遊戲資料夾裡會多出 `version.dll` 與 `MelonLoader` 資料夾。

**第二步：安裝這個 mod**

1. 從 [Releases](https://github.com/dbfoxtw/space-warlord-baby-zh-hant/releases) 下載最新版的 `SpaceWarlordBaby-ZhHant-<版本>.zip`（Assets 底下）。
2. 關閉遊戲，把壓縮檔裡的 `Mods` 與 `UserData` 資料夾**解壓縮到遊戲資料夾**，資料夾合併即可。
3. 開遊戲。會先出現 MelonLoader 的黑色視窗，接著遊戲就會顯示中文。

更新 mod 時，只要重做第二步（解壓覆蓋）。

## 移除

刪除遊戲資料夾裡的 `Mods\SpaceWarlordBabyZhHant.dll` 與 `UserData\ZhHant` 資料夾，就回到原版英文，存檔可以直接沿用（v1.0.0 會把玩家稱號與收藏的嬰兒以中文存檔；用 v1.0.1 以後的版本再存一次檔，就會換回英文）。MelonLoader 本身要另外移除：刪除 `version.dll`、`MelonLoader`、`Mods`、`Plugins`、`UserData`、`UserLibs`。

## 運作方式

- 遊戲是 Unity Mono 版本，mod 透過 MelonLoader（Harmony）掛上攔截。遊戲檔不修改，拔掉 mod 就回到原版英文。
- **存檔**：遊戲有幾個欄位存的是畫面上的文字（每日情境的玩家稱號、收藏嬰兒的名字與人生事件）。mod 存檔時把它們換回英文、讀檔時再換成中文，所以存檔內容和原版相同。
- **語言表**：遊戲的文字大多在 I2 Localization 的語言表裡。語言表載入時，mod 把英文欄換成譯文（以條目名稱對應）。
- **寫死的文字**：少數介面文字寫死在場景或程式裡，mod 在文字要顯示時（SuperTextMesh、TextMeshPro、UGUI Text）查表翻譯。程式拼出來的句子（例如天數、接上金額的標籤）用正規表示式翻譯。
- **遊戲更新**：每條譯文都記著翻譯時英文原文的雜湊。遊戲更新改了某條英文時，mod 會跳過那條、維持英文，避免顯示過時的譯文；新增的文字也會是英文，等 mod 更新。
- **字型與換行**：中文改用像素字型，標題的深色字配彩色描邊改成彩色字配深色描邊（中文筆畫細，原本的配色看不清楚），並調整換行規則，讓中文逐字換行、標點不出現在行首。

## 這個 repo 的內容

| 路徑 | 內容 |
|---|---|
| `mod/` | mod 原始碼（C#） |
| `data/i2.tsv` | 語言表的譯文：條目名稱、英文原文的雜湊、譯文（不含英文原文） |
| `data/hardcoded.tsv` | 寫死文字的翻譯：英文介面文字對中文，以及程式拼接句子的正規表示式 |
| `tools/` | 建置、安裝、打包，以及產生中文像素字型的工具 |
| `fonts/README.md` | 中文像素字型的來源與產生方式 |

## 從原始碼建置（開發者）

一般玩家請看上面的「安裝」，不需要建置。

需要 Windows、Python 3.10 以上、.NET SDK 6 以上。

1. 把 MelonLoader 0.7.3 解壓到遊戲資料夾（編譯時要參考 `MelonLoader\net35` 的組件）。
2. 照 [fonts/README.md](fonts/README.md) 產生中文像素字型包（沒有字型包時，中文會改用系統字型，比較糊）。
3. 關閉遊戲，執行 `install.bat`：建置後安裝到遊戲的 `Mods\` 與 `UserData\ZhHant\`。移除用 `uninstall.bat`。
   - 遊戲不在 Steam 預設位置時，加上 `--game "遊戲資料夾"`。
   - 除錯模式：加上 `--debug`，會把沒翻到的英文記到 `UserData\ZhHant\misses.tsv`，並做字型缺字自我測試。
4. 只建置、不安裝：`python tools/install.py build`。打包發布用的壓縮檔：`python tools/package.py`。

原始碼註解提到的 `docs/` 是開發用的文件，沒有放在這個 repo。

## 已知問題與衝突

- 製作群名單維持英文。
- 其他翻譯 mod，以及同樣修改 SuperTextMesh、TextMeshPro 文字或字型的 mod。

## 授權

這個 repo 的程式碼與翻譯資料（`data/`）以 [MIT 授權](LICENSE)釋出。

遊戲的文字、名稱與素材，權利屬於 Strange Scaffold，不在授權範圍內。第三方元件：

- 中文像素字型（發布版）：SWB ZhHant Pixel，是俐方體11號（[Cubic 11](https://github.com/ACh-K/Cubic-11)，ACh 與 Cubic 11 Project Authors，含 M+ BITMAP FONTS）的修改版，SIL Open Font License 1.1，不在 MIT 授權範圍內。發布檔附上授權文字。
- MelonLoader：Apache License 2.0；Harmony：MIT（執行時由 MelonLoader 提供，不隨本 mod 散布）。

## English

An unofficial mod that translates *Space Warlord Baby Trading Simulator* into Traditional Chinese (Taiwan). Supported game version: 2026.21.a (shown at the bottom left of the main menu). Download from this repository's [Releases](https://github.com/dbfoxtw/space-warlord-baby-zh-hant/releases).

The translation was done and proofread with AI. If any line reads awkwardly, please report it. Not every campaign has been played through with the mod yet, so reports of untranslated English text or bugs are welcome via [Issues](https://github.com/dbfoxtw/space-warlord-baby-zh-hant/issues) (screenshots help).

**Install**

Find the game folder: in your Steam library, right-click Space Warlord Baby Trading Simulator → Manage → Browse local files. It contains `Space Warlord Baby Trading Simulator.exe`.

1. Install MelonLoader 0.7.3 (once): from the [v0.7.3 release page](https://github.com/LavaGang/MelonLoader/releases/tag/v0.7.3), download **`MelonLoader.x64.zip`** (not x86, not the Installer) and **extract it into the game folder**, next to `Space Warlord Baby Trading Simulator.exe`. You should now see `version.dll` and a `MelonLoader` folder there.
2. Close the game, download the latest `SpaceWarlordBaby-ZhHant-<version>.zip` from [Releases](https://github.com/dbfoxtw/space-warlord-baby-zh-hant/releases) (under Assets), and extract its `Mods` and `UserData` folders into the game folder, merging with the existing folders.
3. Start the game. A MelonLoader console window opens first, then the game shows Chinese text. No setting is needed.

To update the mod, just repeat step 2.

**Uninstall:** delete `Mods\SpaceWarlordBabyZhHant.dll` and the `UserData\ZhHant` folder. Saves work with or without the mod (v1.0.0 saved the scenario title and bookmarked babies in Chinese; saving once with v1.0.1 or later turns them back into English).

**Notes**

- Voice commands still only work in English (the game only recognizes English); their descriptions keep the English commands.
- The credits stay in English.
- Known conflicts: other translation mods, and mods that change SuperTextMesh or TextMeshPro text or fonts.
- How it works: the game is a Unity Mono build, hooked through MelonLoader (Harmony). When the I2 Localization language table loads, the mod replaces its English column with the translation, matched by term name. Each translation stores a hash of the English text it was made from; if a game update changes that text, the entry stays in English instead of showing an outdated translation. A few strings hard-coded in scenes or code are translated right before display. Game files are never modified. The few save fields that store displayed text (scenario title, bookmarked babies) are written in English and translated back on load, so saves stay identical to the unmodded format.
- Chinese text uses a pixel font made from Cubic 11, sized to the game's low-resolution UI.
- Build from source (developers only): extract MelonLoader 0.7.3 into the game folder, generate the font bundle as described in [fonts/README.md](fonts/README.md), then run `install.bat` (requires Python 3.10+ and the .NET SDK 6+).
- License: MIT for the code and translation data in this repository. The pixel font (SWB ZhHant Pixel, a modified Cubic 11) is under the SIL Open Font License 1.1. The game's text, names, and assets belong to Strange Scaffold.
