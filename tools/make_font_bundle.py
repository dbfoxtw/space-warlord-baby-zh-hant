r"""把中文像素字型打包成 AssetBundle，給 mod 在執行時載入（只讀遊戲檔）。

用法：python tools/make_font_bundle.py [--game "遊戲資料夾"] [--font fonts\zhhant-pixel.ttf] [--out fonts\zhhant-pixel.bundle]

為什麼要打包：遊戲的文字幾乎都是 SuperTextMesh，它用 Unity 傳統字型（UnityEngine.Font）。
執行時用 new Font(路徑) 建的字型沒有字型資料，傳統字型系統一個字都畫不出來（實測，見 docs/MelonLoader方案.md），
所以要一個「內含字型資料」的 Font 物件，只能從 AssetBundle 載入。沒有 Unity 編輯器，這裡用 UnityPy 自己組：

- 容器：遊戲 StreamingAssets 裡同版本（2022.3.62f2）打包、有 typetree 的 Addressables 包。只留 AssetBundle 物件，
  其他物件、MonoScript 參照全部移除；內部檔名（CAB-…）與包名改掉，避免和遊戲自己的包撞名而載入失敗。
- 內容：從 sharedassets0.assets 複製 Silver 的 Font、它的 Font Material、Font Texture（動態字型的空貼圖），
  Font 的字型資料換成 zhhant-pixel.ttf、改名；匯入時算好的行距、ascent、descent 依新字型的 hhea 重算
  （Unity 用 m_FontSize 當基準換算）。
- 型別：typetree 用 UnityPy 內建的 Unity 型別資料庫，型別雜湊抄 sharedassets0.assets 的（同一版 Unity）。
- Material 的 shader（GUI/Text Shader）在 Unity 內建資源，外部參照照抄 sharedassets0.assets 的那一筆。
"""
from __future__ import annotations

import argparse
import copy
import hashlib
import sys
from pathlib import Path

import UnityPy
from fontTools.ttLib import TTFont
from UnityPy.enums import ClassIDType
from UnityPy.helpers.Tpk import get_typetree_node

ROOT = Path(__file__).resolve().parents[1]
sys.path.insert(0, str(ROOT / "tools"))
from swb import game as G  # noqa: E402

BUNDLE_NAME = "swb-zhhant-pixel"
FONT_NAME = "SWB ZhHant Pixel"
CONTAINER_PATH = "assets/swbzhhant/swb zhhant pixel.ttf"
FONT, MATERIAL, TEXTURE = 128, 21, 28
IDS = {FONT: 2, MATERIAL: 3, TEXTURE: 4}  # AssetBundle 物件是 1


def engine_node(class_id: int, version):
    """UnityPy 型別資料庫的節點少了寫檔要的 m_TypeFlags、m_RefTypeHash。
    和遊戲包裡 Unity 自己寫的 AssetBundle 型別逐節點比對過，只差這兩個：Array 節點的 m_TypeFlags 是 1，其他 0。"""
    node = get_typetree_node(class_id, version)
    for n in node.traverse():
        n.m_TypeFlags = 1 if n.m_Type == "Array" else 0
        n.m_RefTypeHash = 0
    return node


def pick_template(data_dir: Path) -> Path:
    """同版 Unity 打包、有 typetree 的 Addressables 包，挑最小的。"""
    aa = data_dir / "StreamingAssets" / "aa" / "StandaloneWindows64"
    for p in sorted(aa.glob("*.bundle"), key=lambda p: p.stat().st_size):
        sf = next(iter(UnityPy.load(str(p)).file.files.values()))
        if sf.unity_version == G.UNITY_VERSION and sf._enable_type_tree:
            return p
    raise SystemExit(f"找不到 Unity {G.UNITY_VERSION} 打包、含 typetree 的 Addressables 包")


def build(game: Path, font_path: Path, out: Path) -> None:
    data_dir = G.data_dir(game)
    template = pick_template(data_dir)
    env = UnityPy.load(str(template))
    bf = env.file
    old_cab, sf = next(iter(bf.files.items()))
    src = UnityPy.load(str(data_dir / "sharedassets0.assets")).file

    silver = next(o for o in src.objects.values() if o.class_id == FONT and o.peek_name() == "Silver")
    font_tree = silver.read_typetree()
    mat_obj = src.objects[font_tree["m_DefaultMaterial"]["m_PathID"]]
    tex_obj = src.objects[font_tree["m_Texture"]["m_PathID"]]
    mat_tree = mat_obj.read_typetree()
    tex_raw = tex_obj.get_raw_data()

    # 型別：保留 AssetBundle，加上 Font／Material／Texture2D
    ab_type = next(t for t in sf.types if t.class_id == ClassIDType.AssetBundle)
    types = [ab_type]
    for cid in (FONT, MATERIAL, TEXTURE):
        t = copy.copy(ab_type)
        t.class_id = cid
        t.is_stripped_type = False
        t.script_type_index = -1
        t.script_id = None
        t.old_type_hash = next(x.old_type_hash for x in src.types if x.class_id == cid)
        t.node = engine_node(cid, sf.version)
        t.type_dependencies = ()
        types.append(t)
    sf.types = types
    sf.script_types = []
    sf.ref_types = []

    # 外部參照：只留 Unity 內建資源（Material 的 shader）
    shader_ext = src.externals[mat_tree["m_Shader"]["m_FileID"] - 1]
    sf.externals = [shader_ext]

    ab = next(o for o in sf.objects.values() if o.class_id == ClassIDType.AssetBundle)
    ab_id = ab.path_id
    ab.type_id = 0  # 型別表重排過，AssetBundle 現在是第 0 個

    def new_object(cid: int):
        o = copy.copy(ab)
        o.path_id = IDS[cid]
        o.type_id = types.index(next(t for t in types if t.class_id == cid))
        o.serialized_type = types[o.type_id]
        o.class_id = cid
        o.type = ClassIDType(cid)
        return o

    font_obj, mat_new, tex_new = new_object(FONT), new_object(MATERIAL), new_object(TEXTURE)
    sf.objects = {ab_id: ab, font_obj.path_id: font_obj, mat_new.path_id: mat_new, tex_new.path_id: tex_new}

    tt = TTFont(font_path)
    upm, hhea = tt["head"].unitsPerEm, tt["hhea"]
    size = font_tree["m_FontSize"]
    font_tree["m_Ascent"] = hhea.ascent / upm * size
    font_tree["m_Descent"] = hhea.descent / upm * size
    font_tree["m_LineSpacing"] = (hhea.ascent - hhea.descent + hhea.lineGap) / upm * size
    font_tree["m_Name"] = FONT_NAME
    font_tree["m_FontData"] = font_path.read_bytes()
    font_tree["m_FontNames"] = [FONT_NAME]
    font_tree["m_FallbackFonts"] = []
    font_tree["m_DefaultMaterial"] = {"m_FileID": 0, "m_PathID": IDS[MATERIAL]}
    font_tree["m_Texture"] = {"m_FileID": 0, "m_PathID": IDS[TEXTURE]}
    font_obj.save_typetree(font_tree, types[1].node)

    mat_tree["m_Shader"] = {"m_FileID": 1, "m_PathID": mat_tree["m_Shader"]["m_PathID"]}
    mat_tree["m_SavedProperties"]["m_TexEnvs"] = [
        (k, {**v, "m_Texture": {"m_FileID": 0, "m_PathID": IDS[TEXTURE]}}) for k, v in mat_tree["m_SavedProperties"]["m_TexEnvs"]]
    mat_new.save_typetree(mat_tree, types[2].node)
    tex_new.set_raw_data(tex_raw)

    ab_tree = ab.read_typetree()
    preload = [{"m_FileID": 0, "m_PathID": IDS[c]} for c in (FONT, MATERIAL, TEXTURE)]
    ab_tree["m_Name"] = ab_tree["m_AssetBundleName"] = BUNDLE_NAME
    ab_tree["m_PreloadTable"] = preload
    ab_tree["m_Container"] = [(CONTAINER_PATH, {"preloadIndex": 0, "preloadSize": len(preload), "asset": preload[0]})]
    ab_tree["m_Dependencies"] = []
    ab.save_typetree(ab_tree, ab_type.node)

    new_cab = "CAB-" + hashlib.md5(BUNDLE_NAME.encode()).hexdigest()
    bf.files = {new_cab: sf}
    sf.name = new_cab
    out.parent.mkdir(parents=True, exist_ok=True)
    out.write_bytes(bf.save(packer="lz4"))
    print(f"{out}（{out.stat().st_size:,} bytes；容器 {template.name}，{old_cab} → {new_cab}）")


def verify(out: Path, font_path: Path) -> None:
    """重新讀一次：物件、型別、字型資料都要對。"""
    env = UnityPy.load(str(out))
    sf = next(iter(env.file.files.values()))
    objs = {o.path_id: o for o in sf.objects.values()}
    font = objs[IDS[FONT]].read_typetree()
    assert font["m_Name"] == FONT_NAME and bytes(font["m_FontData"]) == font_path.read_bytes()
    mat = objs[IDS[MATERIAL]].read_typetree()
    ab = next(o for o in objs.values() if o.class_id == ClassIDType.AssetBundle).read_typetree()
    print("驗證：", [(o.path_id, o.type.name) for o in objs.values()], "externals", [e.path for e in sf.externals],
          "shader", mat["m_Shader"], "container", ab["m_Container"])


def main() -> None:
    ap = argparse.ArgumentParser()
    ap.add_argument("--game")
    ap.add_argument("--font", type=Path, default=ROOT / "fonts" / "zhhant-pixel.ttf")
    ap.add_argument("--out", type=Path, default=ROOT / "fonts" / "zhhant-pixel.bundle")
    args = ap.parse_args()
    build(G.find_game_dir(args.game), args.font, args.out)
    verify(args.out, args.font)


if __name__ == "__main__":
    main()
