r"""從俐方體11號（Cubic 11，OFL 1.1）產生 mod 用的像素字型：1 em＝18 像素，對齊遊戲介面的像素。

用法：python tools/make_font.py [--src fonts\candidates\Cubic_11.ttf] [--out fonts\zhhant-pixel.ttf]

為什麼這樣改：
- 遊戲介面先畫在約 640×360 的低解析度畫面再放大，STM 的 size 18 文字 1 em 就是 18 個畫面像素。
  字型的 1 個像素要剛好等於 1 個畫面像素，否則會整行整列掉像素（2026-10-03 第三次實機測試：
  改成 Silver 的 19 像素格時，19 格塞進 18 像素，「日」中間那橫不見）。遊戲自己的 Silver 也是 19 格，英文同樣會掉，只是不明顯。
- 俐方體是每像素 100 單位、1 em＝12 像素。只把 unitsPerEm 改成 1800，座標完全不縮放，em 就變成 18 像素。
- 上下緣取整數像素：ascent 11、descent 9，行高 20 像素（Silver 在 size 18 是 19.9）。基線位置是整數像素，字才不會跨像素。
- 俐方體的基線在 y＝-1 像素（A 從 -1 到 8），整體上移 1 像素，讓英數坐在基線上（大寫高 9 像素）。
- 補「‧」（U+2027，人名間隔號）：俐方體沒有，借用「・」（U+30FB）的字形。
- OFL 規定修改後不能沿用保留名稱（Cubic、俐方體），所以改名，授權改用 OFL 並附上原授權檔。
"""
from __future__ import annotations

import argparse
from pathlib import Path

from fontTools.ttLib import TTFont

ROOT = Path(__file__).resolve().parents[1]
FAMILY = "SWB ZhHant Pixel"
UPM = 1800  # 18 像素 × 100 單位
ASCENT, DESCENT = 1100, -900  # 11＋9 像素，行高 20 像素
SHIFT_Y = 100  # 俐方體的基線在 -1 像素


def build(src: Path, out: Path) -> None:
    f = TTFont(src)
    if "glyf" not in f:
        raise SystemExit("只支援 TrueType 外框（glyf）的字型")
    glyf = f["glyf"]
    for name in f.getGlyphOrder():
        g = glyf[name]
        if g.isComposite() or g.numberOfContours <= 0:
            continue  # 組合字形的元件已經移過；空字形沒有座標
        g.coordinates.translate((0, SHIFT_Y))
        g.recalcBounds(glyf)
    f["head"].unitsPerEm = UPM
    hhea, os2 = f["hhea"], f["OS/2"]
    hhea.ascent, hhea.descent, hhea.lineGap = ASCENT, DESCENT, 0
    os2.sTypoAscender, os2.sTypoDescender, os2.sTypoLineGap = ASCENT, DESCENT, 0
    os2.usWinAscent, os2.usWinDescent = ASCENT, -DESCENT

    cmap = f.getBestCmap()
    if 0x2027 not in cmap and 0x30FB in cmap:
        for t in f["cmap"].tables:
            if t.isUnicode():
                t.cmap[0x2027] = cmap[0x30FB]

    # 改名（OFL 保留名稱），版權與授權欄位保留
    name = f["name"]
    for rec in list(name.names):
        if rec.nameID in (1, 3, 4, 6, 16, 17, 21, 22):
            name.removeNames(nameID=rec.nameID)
    name.setName(FAMILY, 1, 3, 1, 0x409)
    name.setName("Regular", 2, 3, 1, 0x409)
    name.setName(f"{FAMILY} Regular", 3, 3, 1, 0x409)
    name.setName(FAMILY, 4, 3, 1, 0x409)
    name.setName(FAMILY.replace(" ", ""), 6, 3, 1, 0x409)
    name.setName("Space Warlord Baby 繁中 mod 用：俐方體11號（Cubic 11）改成 1 em＝18 像素。",
                 10, 3, 1, 0x409)
    out.parent.mkdir(parents=True, exist_ok=True)
    f.save(out)
    write_license(src, out)
    print(f"{out}（{out.stat().st_size:,} bytes）")


def write_license(src: Path, out: Path) -> None:
    """授權檔：說明修改內容＋原字型的著作權聲明＋OFL 全文。
    俐方體的 OFL.txt 開頭沒有著作權行（只寫在字型檔的 name ID 0），OFL 要求附上，所以從字型檔抄過來。"""
    lic = src.with_name(src.stem + ".OFL.txt")
    if not lic.is_file():
        return
    copyright_lines = [ln for ln in TTFont(src)["name"].getDebugName(0).splitlines() if ln.startswith("Copyright")]
    out.with_suffix(".LICENSE.txt").write_text(
        f"{FAMILY} 是俐方體11號（Cubic 11，https://github.com/ACh-K/Cubic-11）的修改版，\n"
        "依 SIL Open Font License 1.1 發布。修改內容：em 改成 18 像素、上下緣與基線調整、補 U+2027。\n"
        "原字型的保留名稱為 Cubic、俐方體，修改版不使用這些名稱。\n\n"
        + "".join(ln + "\n" for ln in copyright_lines) + "\n" + lic.read_text(encoding="utf-8"),
        encoding="utf-8")


def main() -> None:
    ap = argparse.ArgumentParser()
    ap.add_argument("--src", type=Path, default=ROOT / "fonts" / "candidates" / "Cubic_11.ttf")
    ap.add_argument("--out", type=Path, default=ROOT / "fonts" / "zhhant-pixel.ttf")
    args = ap.parse_args()
    build(args.src, args.out)


if __name__ == "__main__":
    main()
