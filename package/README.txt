Space Warlord Baby Trading Simulator 繁體中文化 mod（非官方） v{version}
=====================================================================

把遊戲翻譯成繁體中文（台灣用語）。非官方製作。
對應遊戲版本：2026.21.a（主選單左下角）

翻譯是用 AI 翻譯、校對的，如果有翻不順的地方歡迎回報。
這個 mod 還沒有玩遍所有劇情，如果遇到沒翻到的英文或任何 bug 也歡迎回報
（https://github.com/dbfoxtw/space-warlord-baby-zh-hant/issues ，附上截圖最好）。

【安裝】
找遊戲資料夾：Steam 遊戲庫 → 在遊戲上按右鍵 → 管理 → 瀏覽本機檔案
（裡面有 Space Warlord Baby Trading Simulator.exe）。

1. 安裝 MelonLoader 0.7.3（只要裝一次）：
   到 https://github.com/LavaGang/MelonLoader/releases/tag/v0.7.3 ，在 Assets 底下下載
   MelonLoader.x64.zip（不是 x86，也不是 Installer），把它的內容解壓縮到遊戲資料夾
   （和 Space Warlord Baby Trading Simulator.exe 同一層）。解壓後，遊戲資料夾裡會多出 version.dll 與 MelonLoader 資料夾。
2. 關閉遊戲，把這個壓縮檔裡的 Mods 與 UserData 資料夾，解壓縮到遊戲資料夾，資料夾合併即可。
3. 開遊戲。會先出現 MelonLoader 的黑色視窗，接著遊戲就會顯示中文，不用另外設定。
更新 mod 時，只要重做第 2 步（解壓覆蓋）。

【移除】
刪除遊戲資料夾裡的 Mods\SpaceWarlordBabyZhHant.dll 與 UserData\ZhHant 資料夾，就回到原版英文，存檔可以直接沿用。
（v1.0.0 會把玩家稱號與收藏的嬰兒以中文存檔；用 v1.0.1 以後的版本再存一次檔，就會換回英文。）
MelonLoader 本身要另外移除：刪除 version.dll、MelonLoader、Mods、Plugins、UserData、UserLibs。

【說明】
- 不修改遊戲檔。存檔裡的玩家稱號、收藏的嬰兒會存成英文、讀檔時再換成中文，存檔內容和原版相同。拔掉 mod 就回到原版英文。
- 語音指令仍然只能用英文（遊戲只辨識英文），說明文字會保留英文指令。
- 製作群名單維持英文。
- 遊戲更新後，改過的英文會維持英文、不顯示過時的譯文；新增的文字也是英文，等 mod 更新。
- 已知衝突：其他翻譯 mod，以及同樣修改 SuperTextMesh、TextMeshPro 文字或字型的 mod。

【授權與致謝】
- 本 mod：MIT 授權（LICENSE.txt）。
  原始碼：https://github.com/dbfoxtw/space-warlord-baby-zh-hant
- 中文像素字型 pixel.bundle：SWB ZhHant Pixel，是俐方體11號（Cubic 11，https://github.com/ACh-K/Cubic-11）的修改版，
  SIL Open Font License 1.1（pixel-font-LICENSE.txt），不在 MIT 授權範圍內。
- MelonLoader（https://github.com/LavaGang/MelonLoader），Apache License 2.0
- 遊戲的文字、名稱與素材，權利屬於 Strange Scaffold。


Space Warlord Baby Trading Simulator Traditional Chinese mod (unofficial) v{version}
-----------------------------------------------------------------------------------

Translates the game into Traditional Chinese (Taiwan). Unofficial.
Supported game version: 2026.21.a (bottom left of the main menu)

The translation was done and proofread with AI. If any line reads awkwardly, please report it.
Not every campaign has been played through with the mod yet. Reports of untranslated English text
or bugs are welcome (https://github.com/dbfoxtw/space-warlord-baby-zh-hant/issues, screenshots help).

Install
Find the game folder: Steam library → right-click the game → Manage → Browse local files
(it contains Space Warlord Baby Trading Simulator.exe).

1. Install MelonLoader 0.7.3 (once): from https://github.com/LavaGang/MelonLoader/releases/tag/v0.7.3
   download MelonLoader.x64.zip (not x86, not the Installer) and extract it into the game folder,
   next to Space Warlord Baby Trading Simulator.exe. You should now see version.dll and a MelonLoader folder there.
2. Close the game and extract the Mods and UserData folders from this archive into the game folder,
   merging with the existing folders.
3. Start the game. A MelonLoader console window opens first, then the game shows Chinese text. No setting is needed.
To update the mod, just repeat step 2.

Uninstall
Delete Mods\SpaceWarlordBabyZhHant.dll and the UserData\ZhHant folder. Saves work with or without the mod (saves made with v1.0.0 store the scenario title and
bookmarked babies in Chinese; saving once with v1.0.1 or later turns them back into English).

Notes
- Game files are never modified. Save fields that store displayed text are written in English, so saves stay unmodded.
- Voice commands still only work in English (the game only recognizes English).
- The credits stay in English.
- After a game update, changed English lines stay in English instead of showing outdated translations.
- Known conflicts: other translation mods, and mods that change SuperTextMesh or TextMeshPro text or fonts.

License
- This mod: MIT (LICENSE.txt). Source: https://github.com/dbfoxtw/space-warlord-baby-zh-hant
- Chinese pixel font pixel.bundle: SWB ZhHant Pixel, a modified Cubic 11 (https://github.com/ACh-K/Cubic-11),
  SIL Open Font License 1.1 (pixel-font-LICENSE.txt), not covered by the MIT license.
- MelonLoader (https://github.com/LavaGang/MelonLoader), Apache License 2.0
- The game's text, names, and assets belong to Strange Scaffold.
