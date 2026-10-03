r"""從原始碼建置 mod 並安裝到遊戲。

事前準備：
1. 安裝 MelonLoader 0.7.3（https://github.com/LavaGang/MelonLoader）：把 MelonLoader.x64.zip 解壓到遊戲資料夾
   （編譯要參考 MelonLoader\net35 與遊戲 Managed\ 裡的組件）。
2. .NET SDK 6 以上、Python 3.10 以上（安裝只用到標準函式庫；產生字型才要 pip install -r requirements.txt）。

用法：
  python tools/install.py build        編譯 mod（不動遊戲）
  python tools/install.py install      編譯後複製到遊戲的 Mods\ 與 UserData\ZhHant\
  python tools/install.py uninstall    移除 mod（MelonLoader 本身不動）
都可加 --game "遊戲資料夾"；install 可加 --debug（記錄沒翻到的英文到 UserData\ZhHant\misses.tsv、字型缺字自我測試）。

翻譯資料在 data\（i2.tsv、hardcoded.tsv）。中文像素字型包 fonts\zhhant-pixel.bundle 要自己產生
（tools/make_font.py → tools/make_font_bundle.py，見 fonts/README.md）；沒有字型包時中文改用系統字型，會比較糊。
"""
from __future__ import annotations

import argparse
import shutil
import subprocess
import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent))
from swb import game as G  # noqa: E402

ROOT = Path(__file__).resolve().parents[1]
CSPROJ = ROOT / "mod" / "SpaceWarlordBabyZhHant.csproj"
DLL = ROOT / "mod" / "bin" / "Release" / "SpaceWarlordBabyZhHant.dll"
DATA = ROOT / "data"
DATA_FILES = ["i2.tsv", "hardcoded.tsv"]
FONT = ROOT / "fonts" / "zhhant-pixel.bundle"
FONT_LICENSE = ROOT / "fonts" / "zhhant-pixel.LICENSE.txt"


def build_dll(game: Path) -> None:
    if not (game / "MelonLoader" / "net35" / "MelonLoader.dll").is_file():
        raise SystemExit("找不到 MelonLoader\\net35\\MelonLoader.dll：請先把 MelonLoader 0.7.3 解壓到遊戲資料夾。")
    dotnet = shutil.which("dotnet") or r"C:\Program Files\dotnet\dotnet.exe"
    subprocess.run([dotnet, "build", str(CSPROJ), "-c", "Release", "-nologo", "-v", "q", f"-p:GameDir={game}"],
                   check=True)
    print(f"建置完成：{DLL}")


def install_files(game: Path, debug: bool = False) -> None:
    if G.is_game_running():
        raise SystemExit("遊戲正在執行，請先關閉。")
    (game / "Mods").mkdir(exist_ok=True)
    shutil.copy2(DLL, game / "Mods" / DLL.name)
    dest = game / "UserData" / "ZhHant"
    dest.mkdir(parents=True, exist_ok=True)
    for name in DATA_FILES:
        shutil.copy2(DATA / name, dest / name)
    (dest / "misses.tsv").unlink(missing_ok=True)  # 每個版本重新累積
    (dest / "debug").unlink(missing_ok=True)
    if debug:
        (dest / "debug").write_text("這個檔存在時，mod 會記錄沒翻到的英文（misses.tsv）並做字型自我測試。\n", encoding="utf-8")
    if FONT.is_file():
        shutil.copy2(FONT, dest / "pixel.bundle")
        if FONT_LICENSE.is_file():
            shutil.copy2(FONT_LICENSE, dest / "pixel-font-LICENSE.txt")
        print(f"中文像素字型：{FONT.name}")
    else:
        print(f"沒有 {FONT}，中文會改用系統字型（產生方式見 fonts/README.md）")
    print(f"已安裝到 {game}\\Mods 與 {dest}{'（除錯模式）' if debug else ''}")


def uninstall(game: Path) -> None:
    if G.is_game_running():
        raise SystemExit("遊戲正在執行，請先關閉。")
    (game / "Mods" / DLL.name).unlink(missing_ok=True)
    shutil.rmtree(game / "UserData" / "ZhHant", ignore_errors=True)
    print("已移除 mod（MelonLoader 本身仍在；要完全移除請刪除 version.dll、MelonLoader、Mods、Plugins、UserData、UserLibs）。")


def main() -> None:
    ap = argparse.ArgumentParser()
    ap.add_argument("action", choices=["build", "install", "uninstall"])
    ap.add_argument("--game")
    ap.add_argument("--debug", action="store_true", help="除錯模式（見上）")
    args = ap.parse_args()
    game = G.find_game_dir(args.game)
    if args.action == "uninstall":
        uninstall(game)
        return
    if args.action == "install" and G.is_game_running():
        raise SystemExit("遊戲正在執行，請先關閉。")
    build_dll(game)
    if args.action == "install":
        install_files(game, args.debug)


if __name__ == "__main__":
    main()
