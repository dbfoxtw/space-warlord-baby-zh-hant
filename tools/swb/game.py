"""遊戲路徑與常數。遊戲資料夾依序找：--game、環境變數 SWB_GAME_DIR、Steam 的 appmanifest。"""
from __future__ import annotations

import os
import re
import subprocess
from pathlib import Path

APP_ID = "3642000"
EXE_NAME = "Space Warlord Baby Trading Simulator.exe"
DATA_DIR_NAME = "Space Warlord Baby Trading Simulator_Data"
UNITY_VERSION = "2022.3.62f2"


def _steam_library_dirs() -> list[Path]:
    roots: list[Path] = []
    try:
        import winreg

        with winreg.OpenKey(winreg.HKEY_CURRENT_USER, r"Software\Valve\Steam") as k:
            roots.append(Path(winreg.QueryValueEx(k, "SteamPath")[0]))
    except OSError:
        pass
    roots.append(Path(r"C:\Program Files (x86)\Steam"))
    libs: list[Path] = []
    for root in roots:
        vdf = root / "steamapps" / "libraryfolders.vdf"
        if not vdf.is_file():
            continue
        libs.append(root)
        for m in re.finditer(r'"path"\s+"([^"]+)"', vdf.read_text(encoding="utf-8", errors="replace")):
            libs.append(Path(m.group(1).replace("\\\\", "\\")))
    return libs


def find_game_dir(explicit: str | None = None) -> Path:
    candidates = [Path(explicit)] if explicit else []
    if os.environ.get("SWB_GAME_DIR"):
        candidates.append(Path(os.environ["SWB_GAME_DIR"]))
    for lib in _steam_library_dirs():
        acf = lib / "steamapps" / f"appmanifest_{APP_ID}.acf"
        if acf.is_file():
            m = re.search(r'"installdir"\s+"([^"]+)"', acf.read_text(encoding="utf-8", errors="replace"))
            if m:
                candidates.append(lib / "steamapps" / "common" / m.group(1))
    for c in candidates:
        if (c / EXE_NAME).is_file():
            return c
    raise SystemExit('找不到遊戲資料夾，請用 --game 指定，例如：--game "D:\\SteamLibrary\\steamapps\\common\\Space Warlord Baby Trading Simulator"')


def data_dir(game: Path) -> Path:
    return game / DATA_DIR_NAME


def is_game_running() -> bool:
    try:
        out = subprocess.run(["tasklist", "/FI", f"IMAGENAME eq {EXE_NAME}", "/FO", "CSV", "/NH"],
                             capture_output=True, text=True, errors="replace").stdout
    except OSError:
        return False
    return EXE_NAME.lower() in out.lower()


def build_id(game: Path) -> str | None:
    """Steam 的 buildid，用來記錄 text/en 是從哪一版抽的。"""
    acf = game.parent.parent / f"appmanifest_{APP_ID}.acf"
    if not acf.is_file():
        return None
    m = re.search(r'"buildid"\s+"(\d+)"', acf.read_text(encoding="utf-8", errors="replace"))
    return m.group(1) if m else None
