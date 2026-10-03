r"""打包發布用的壓縮檔（GitHub Releases）。玩家把壓縮檔解壓到遊戲資料夾即可（要先裝好 MelonLoader 0.7.3）。

壓縮檔結構：
  Mods\SpaceWarlordBabyZhHant.dll
  UserData\ZhHant\   i2.tsv、hardcoded.tsv（翻譯資料）、pixel.bundle 與 pixel-font-LICENSE.txt（中文像素字型與它的授權）、
                     README.txt、LICENSE.txt（MIT）

用法：python tools/package.py [--game 遊戲資料夾] [--font-dir 字型資料夾] [--out 輸出資料夾]
  字型資料夾預設 fonts\，裡面要有 zhhant-pixel.bundle 與 zhhant-pixel.LICENSE.txt（產生方式見 fonts/README.md）。
  發布版一定要附字型與它的授權，缺一個就中止。
"""
from __future__ import annotations

import argparse
import re
import shutil
import sys
import zipfile
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent))
import install  # noqa: E402
from swb import game as G  # noqa: E402

ROOT = install.ROOT


def mod_version() -> str:
    src = (ROOT / "mod" / "src" / "ZhHantMod.cs").read_text(encoding="utf-8")
    m = re.search(r'MelonInfo\(.*?"(\d+\.\d+\.\d+)"', src)
    if not m:
        raise SystemExit("讀不到 mod 版本（ZhHantMod.cs 的 MelonInfo）")
    return m.group(1)


def assemble(font_dir: Path, out_dir: Path) -> Path:
    """把編譯好的 mod、翻譯資料、字型組成壓縮檔，傳回壓縮檔路徑。"""
    font, font_license = font_dir / "zhhant-pixel.bundle", font_dir / "zhhant-pixel.LICENSE.txt"
    for p in (font, font_license):
        if not p.is_file():
            raise SystemExit(f"找不到 {p}：發布版要附中文像素字型與它的授權（見 fonts/README.md）")
    version = mod_version()
    stage = ROOT / "build" / "package"
    if stage.exists():
        shutil.rmtree(stage)
    (stage / "Mods").mkdir(parents=True)
    shutil.copy2(install.DLL, stage / "Mods" / install.DLL.name)
    z = stage / "UserData" / "ZhHant"
    z.mkdir(parents=True)
    for name in install.DATA_FILES:
        shutil.copy2(install.DATA / name, z / name)
    shutil.copy2(font, z / "pixel.bundle")
    shutil.copy2(font_license, z / "pixel-font-LICENSE.txt")
    readme = (ROOT / "package" / "README.txt").read_text(encoding="utf-8").replace("{version}", version)
    (z / "README.txt").write_text(readme, encoding="utf-8", newline="\r\n")
    (z / "LICENSE.txt").write_text((ROOT / "LICENSE").read_text(encoding="utf-8"), encoding="utf-8", newline="\r\n")

    out_dir.mkdir(parents=True, exist_ok=True)
    out = out_dir / f"SpaceWarlordBaby-ZhHant-{version}.zip"
    out.unlink(missing_ok=True)
    files = sorted(p for p in stage.rglob("*") if p.is_file())
    with zipfile.ZipFile(out, "w", zipfile.ZIP_DEFLATED, compresslevel=9) as zf:
        for p in files:
            zf.write(p, p.relative_to(stage).as_posix())
    print(f"已打包 {len(files)} 個檔案（{out.stat().st_size / 1e6:.1f} MB）：{out}")
    for p in files:
        print(f"  {p.relative_to(stage).as_posix()}")
    return out


def main() -> None:
    ap = argparse.ArgumentParser()
    ap.add_argument("--game")
    ap.add_argument("--font-dir", type=Path, default=ROOT / "fonts")
    ap.add_argument("--out", type=Path, default=ROOT / "build")
    args = ap.parse_args()
    install.build_dll(G.find_game_dir(args.game))
    assemble(args.font_dir, args.out)


if __name__ == "__main__":
    main()
