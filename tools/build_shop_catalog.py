"""Kedi evi mağaza kataloğunu (ShopItemDefinition asset'leri + katalog listesi) üretir.
Görseller tools/harmonize_furniture.py ile Assets/_Game/Art/Furniture altında oluşturulur.
Kullanım: python3 tools/build_shop_catalog.py   (repo kökünden, idempotent)"""
import os, re, uuid, glob

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
ART = "Assets/_Game/Art/Furniture"
CONTENT = "Assets/_Game/Content"
SPRITE_META_TEMPLATE = "Assets/_Game/Art/Furniture/bed_cocoa.png.meta"
ITEM_SCRIPT_GUID = "5453fd3994f158f44848f3bb7b9c08e9"
os.chdir(ROOT)

def guid_of(path):
    m = re.search(r"guid: ([0-9a-f]{32})", open(path + ".meta", encoding="utf-8").read())
    return m.group(1)

def ensure_sprite_meta(path, max_size=1024):
    """Yeni görsele Sprite (tek) içe aktarma ayarlarıyla .meta yazar; mevcutsa GUID'i korur."""
    meta = path + ".meta"
    g = guid_of(path) if os.path.exists(meta) else uuid.uuid4().hex
    t = open(SPRITE_META_TEMPLATE, encoding="utf-8").read()
    t = re.sub(r"guid: [0-9a-f]{32}", "guid: " + g, t, count=1)
    t = re.sub(r"(\n\s*maxTextureSize:) \d+", rf"\1 {max_size}", t)
    open(meta, "w", encoding="utf-8").write(t)
    return g

def ensure_folder_meta(path):
    if not os.path.exists(path + ".meta"):
        open(path + ".meta", "w", encoding="utf-8").write(
            f"fileFormatVersion: 2\nguid: {uuid.uuid4().hex}\nfolderAsset: yes\nDefaultImporter:\n  externalObjects: {{}}\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n")

def yaml_str(s):
    return '"' + s.replace("\\", "\\\\").replace('"', '\\"') + '"'

# Yuva numaraları (FurnitureSlotType): Rug0 Bed1 Bowl2 Wallpaper3 Poster4 Water5 Sand6 Tree7 Toy8
# (asset, id, sprite, slot, cost, TR ad, TR açıklama, base: etkileşim/kareleri kopyalanacak mevcut asset)
ITEMS = [
    ("item_rug", "item_rug", "rug_caramel", 0, 30, "Karamel Halı", "Krem ve karamel çizgili, yumuşacık yuvarlak halı.", None),
    ("item_rug_terracotta", "rug_terracotta", "rug_terracotta", 0, 40, "Kiremit Halı", "Sıcak kiremit tonlarında, güneşte uzanmalık halı.", "item_rug"),
    ("item_rug_sage", "rug_sage", "rug_sage", 0, 40, "Ada Çayı Halı", "Ada çayı yeşili çizgilerle serin ve huzurlu bir halı.", "item_rug"),
    ("item_cat_bed", "item_cat_bed", "bed_cocoa", 1, 40, "Kakao Kedi Yatağı", "Kıvrılabileceği sıcacık bir yatak.", None),
    ("item_cat_bed_sage", "cat_bed_sage", "bed_sage", 1, 55, "Ada Çayı Kedi Yatağı", "Yeşil kumaşlı, peluş astarlı yumuşak yatak.", "item_cat_bed"),
    ("item_cat_bed_rose", "cat_bed_rose", "bed_rose", 1, 55, "Gül Kurusu Kedi Yatağı", "Gül kurusu kumaşlı, şekerleme için ideal yatak.", "item_cat_bed"),
    ("item_food_bowl", "food_bowl", "bowl_steel", 2, 20, "Gri Taş Mama Kabı", "Taş görünümlü, sağlam ve kolay temizlenen mama kabı.", None),
    ("item_food_bowl_copper", "food_bowl_copper", "bowl_copper", 2, 30, "Bakır Mama Kabı", "Güneşte parlayan sıcak bakır renkli mama kabı.", "item_food_bowl"),
    ("item_food_bowl_cream", "food_bowl_cream", "bowl_cream", 2, 30, "Krem Emaye Mama Kabı", "Krem rengi emaye, ev sıcaklığında bir mama kabı.", "item_food_bowl"),
    ("item_cat_poster", "cat_poster", "poster_mitzi_sofa", 4, 30, "Mitzi Posteri: Kanepe", "Mitzi'nin kanepede poz verdiği çerçeveli fotoğraf.", None),
    ("item_cat_poster_curious", "cat_poster_curious", "poster_mitzi_curious", 4, 35, "Mitzi Posteri: Meraklı", "Başını yana eğmiş meraklı Mitzi, çerçevede.", "item_cat_poster"),
    ("item_cat_poster_hello", "cat_poster_hello", "poster_mitzi_hello", 4, 35, "Mitzi Posteri: Merhaba", "Kameraya selam veren Mitzi, çerçevede.", "item_cat_poster"),
    ("item_cat_water", "cat_water", "water_cream", 5, 25, "Krem Su Kabı", "Kedilerin su içmek için kullanacağı seramik kap.", None),
    ("item_cat_water_sage", "cat_water_sage", "water_sage", 5, 30, "Ada Çayı Su Kabı", "Ada çayı yeşili seramik su kabı.", "item_cat_water"),
    ("item_cat_water_rose", "cat_water_rose", "water_rose", 5, 30, "Gül Kurusu Su Kabı", "Gül kurusu seramik su kabı.", "item_cat_water"),
    ("item_cat_sand", "cat_sand", "sand_cottage", 6, 35, "Kulübe Kum Kabı", "Ahşap kulübe şeklinde, kapılı ve rahat bir kum kabı.", None),
    ("item_cat_tree", "cat_tree", "tree_taupe", 7, 80, "Taupe Kedi Ağacı", "Tırmanmak, dinlenmek ve evi seyretmek için.", None),
    ("item_cat_tree_walnut", "cat_tree_walnut", "tree_walnut", 7, 95, "Ceviz Kedi Ağacı", "Koyu ceviz tonlarında, sisal direkli kedi ağacı.", "item_cat_tree"),
    ("item_cat_tree_cream", "cat_tree_cream", "tree_cream", 7, 95, "Krem Kedi Ağacı", "Açık krem kaplamalı, ferah görünümlü kedi ağacı.", "item_cat_tree"),
    ("item_cat_toy_mouse", "cat_toy_mouse", "toy_gray", 8, 30, "Gri Oyuncak Fare", "Mitzi'nin peşinden koşup patileriyle oynayabileceği yumuşak fare.", None),
    ("item_cat_toy_mouse_cream", "cat_toy_mouse_cream", "toy_cream", 8, 35, "Krem Oyuncak Fare", "Krem peluş, pembe kulaklı oyuncak fare.", "item_cat_toy_mouse"),
    ("item_cat_toy_mouse_caramel", "cat_toy_mouse_caramel", "toy_caramel", 8, 35, "Karamel Oyuncak Fare", "Karamel renkli, kovalaması çok eğlenceli fare.", "item_cat_toy_mouse"),
    ("item_wall_sage", "wall_sage", "wall_sage.jpg", 3, 60, "Ada Çayı Duvar", "Odanın duvarlarını ada çayı yeşiline boyar.", None),
    ("item_wall_rose", "wall_rose", "wall_rose.jpg", 3, 60, "Gül Kurusu Duvar", "Odanın duvarlarını gül kurusuna boyar.", None),
    ("item_wall_sky", "wall_sky", "wall_sky.jpg", 3, 60, "Gökyüzü Duvar", "Odanın duvarlarını açık gökyüzü mavisine boyar.", None),
]

# Yalnız ilk sürümde var olan asset'lerde tutulacak alanlar (etkileşim, kareler).
def item_yaml(name, id_, sprite_guid, slot, cost, tr_name, tr_desc, base_text):
    interaction = "0"
    frames = "  interactionFrames: []\n"
    scale = "1"
    if base_text:
        m = re.search(r"\n  interactionType: (.*)", base_text); interaction = m.group(1) if m else "0"
        m = re.search(r"\n  interactionFrames:(?: \[\])?\n((?:  - .*\n)*)", base_text)
        if m and m.group(1):
            frames = "  interactionFrames:\n" + m.group(1)
        m = re.search(r"\n  interactionVisualScale: (.*)", base_text); scale = m.group(1) if m else "1"
    return f"""%YAML 1.1
%TAG !u! tag:unity3d.com,2011:
--- !u!114 &11400000
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: 0}}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {{fileID: 11500000, guid: {ITEM_SCRIPT_GUID}, type: 3}}
  m_Name: {name}
  m_EditorClassIdentifier: 
  id: {id_}
  displayName: {yaml_str(tr_name)}
  description: {yaml_str(tr_desc)}
  lovePointCost: {cost}
  icon: {{fileID: 21300000, guid: {sprite_guid}, type: 3}}
  placedSprite: {{fileID: 21300000, guid: {sprite_guid}, type: 3}}
  slotType: {slot}
  interactionType: {interaction}
  interactionAnimation: {{fileID: 0}}
{frames}  interactionVisualScale: {scale}
"""

def asset_meta(path):
    meta = path + ".meta"
    if os.path.exists(meta):
        return guid_of(path)
    g = uuid.uuid4().hex
    open(meta, "w", encoding="utf-8").write(
        f"fileFormatVersion: 2\nguid: {g}\nNativeFormatImporter:\n  externalObjects: {{}}\n  mainObjectFileID: 11400000\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n")
    return g

def main():
    ensure_folder_meta(ART)
    # Orijinal (ilk sürüm) asset'leri, etkileşim alanlarını kopyalamak için önce oku.
    originals = {os.path.basename(p)[:-6]: open(p, encoding="utf-8").read() for p in glob.glob(f"{CONTENT}/item_*.asset")}
    guids = []
    for name, id_, sprite, slot, cost, tr_name, tr_desc, base in ITEMS:
        file = sprite if "." in sprite else sprite + ".png"
        sg = ensure_sprite_meta(f"{ART}/{file}", 2048 if file.endswith(".jpg") else 1024)
        base_text = originals.get(base or name)
        path = f"{CONTENT}/{name}.asset"
        open(path, "w", encoding="utf-8").write(item_yaml(name, id_, sg, slot, cost, tr_name, tr_desc, base_text))
        guids.append(asset_meta(path))
    cat = f"{CONTENT}/PawPathCatalog.asset"
    t = open(cat, encoding="utf-8").read()
    lst = "  shopItems:\n" + "".join(f"  - {{fileID: 11400000, guid: {g}, type: 2}}\n" for g in guids)
    t = re.sub(r"  shopItems:\n(?:  - .*\n)*", lst, t, count=1)
    open(cat, "w", encoding="utf-8").write(t)
    print(len(guids), "eşya")

if __name__ == "__main__":
    main()
