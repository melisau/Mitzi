"""
Kedi evi eşyalarını odanın (home_background_gemini.jpg) ışığına uydurur ve renk varyantları üretir.

Kullanım (repo kökünden):  python3 tools/harmonize_furniture.py
Kaynak görseller tools/furniture_src/ altında saklanır (orijinaller); çıktılar Assets/ içine yazılır.
Yeni AI görselleri gelince kaynağı değiştirip betiği yeniden çalıştırmak yeterli.
"""
import colorsys, os, shutil
import numpy as np
from PIL import Image, ImageFilter, ImageDraw

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
SRC = os.path.join(ROOT, "tools", "furniture_src")
OUT = os.path.join(ROOT, "Assets", "_Game", "Art", "Furniture")

# --- temel işlemler -----------------------------------------------------------

def load(name):
    return Image.open(os.path.join(SRC, name)).convert("RGBA")

def _main_run(counts):
    """Boş satır/sütunlarla ayrılmış bloklardan en büyüğünü seçer (kopuk lekeleri atar)."""
    nz = np.where(counts > 0)[0]
    best, start, prev = None, nz[0], nz[0]
    for k in list(nz[1:]) + [None]:
        if k is None or k != prev + 1:
            area = counts[start:prev + 1].sum()
            if best is None or area > best[0]:
                best = (area, start, prev)
            if k is not None:
                start = k
        if k is not None:
            prev = k
    return best[1], best[2]

def trim(im, pad=6):
    a = np.array(im)[:, :, 3] > 8
    y0, y1 = _main_run(a.sum(1))
    x0, x1 = _main_run(a[y0:y1 + 1].sum(0))
    box = (max(0, x0 - pad), max(0, y0 - pad), min(im.width, x1 + pad + 1), min(im.height, y1 + pad + 1))
    return im.crop(box)

def fit_max(im, max_side=900):
    s = max_side / max(im.size)
    return im.resize((round(im.width * s), round(im.height * s)), Image.LANCZOS) if s < 1 else im

def warm_grade(im, warmth=1.0, sat=0.92, contrast=0.93, lift=0.035, side_light=0.07):
    """Odanın gün batımı ışığı: sıcak beyaz dengesi, hafif düşük kontrast, kahve tonlu gölgeler,
    soldaki pencereden gelen ışık için soldan sağa çok hafif aydınlık geçişi."""
    arr = np.asarray(im).astype(np.float32) / 255.0
    rgb, a = arr[..., :3], arr[..., 3:]
    lum = (rgb @ np.array([0.299, 0.587, 0.114], np.float32))[..., None]
    rgb = lum + (rgb - lum) * sat
    rgb = 0.5 + (rgb - 0.5) * contrast
    rgb = rgb * np.array([1.0 + 0.06 * warmth, 1.0 + 0.01 * warmth, 1.0 - 0.10 * warmth], np.float32)
    rgb = rgb + lift * np.array([1.0, 0.78, 0.55], np.float32) * (1 - rgb)
    w = rgb.shape[1]
    ramp = np.linspace(1 + side_light, 1 - side_light * 0.6, w, dtype=np.float32)[None, :, None]
    rgb = np.clip(rgb * ramp, 0, 1)
    return Image.fromarray((np.concatenate([rgb, a], -1) * 255).astype(np.uint8), "RGBA")

def contact_shadow(im, width_ratio=0.86, height_ratio=0.13, opacity=0.42, blur=0.035, lift_px=None):
    """Zemine oturtmak için yumuşak, hafif sağa kaymış (ışık soldan) elips gölge ekler."""
    w, h = im.size
    sh_w, sh_h = int(w * width_ratio), max(6, int(w * height_ratio))
    extra = sh_h // 2 + 4
    canvas = Image.new("RGBA", (w, h + extra), (0, 0, 0, 0))
    shadow = Image.new("L", canvas.size, 0)
    d = ImageDraw.Draw(shadow)
    cx = w * 0.5 + w * 0.04
    base = h - (lift_px if lift_px is not None else int(sh_h * 0.35))
    d.ellipse([cx - sh_w / 2, base - sh_h / 2, cx + sh_w / 2, base + sh_h / 2], fill=int(255 * opacity))
    shadow = shadow.filter(ImageFilter.GaussianBlur(max(2, w * blur)))
    tint = Image.new("RGBA", canvas.size, (58, 34, 18, 0)); tint.putalpha(shadow)
    canvas.alpha_composite(tint)
    canvas.alpha_composite(im, (0, 0))
    return canvas

def wall_shadow(im, offset=(10, 12), opacity=0.35, blur=10):
    w, h = im.size
    canvas = Image.new("RGBA", (w + offset[0] + blur * 2, h + offset[1] + blur * 2), (0, 0, 0, 0))
    a = im.split()[3].point(lambda v: int(v * opacity))
    sh = Image.new("RGBA", im.size, (50, 30, 15, 0)); sh.putalpha(a)
    layer = Image.new("RGBA", canvas.size, (0, 0, 0, 0)); layer.paste(sh, (blur + offset[0], blur + offset[1]))
    canvas.alpha_composite(layer.filter(ImageFilter.GaussianBlur(blur)))
    canvas.alpha_composite(im, (blur, blur))
    return canvas

def colorize(im, mask, dark, light, strength=1.0, gamma=1.0):
    """Maskeli bölgeyi parlaklığını koruyarak iki ton arasında yeniden renklendirir (kumaş, plastik)."""
    arr = np.asarray(im).astype(np.float32) / 255.0
    rgb = arr[..., :3]
    lum = np.clip((rgb @ np.array([0.299, 0.587, 0.114], np.float32)) ** gamma, 0, 1)[..., None]
    d, l = np.array(dark, np.float32) / 255, np.array(light, np.float32) / 255
    new = d + (l - d) * lum
    m = (mask.astype(np.float32) * strength)[..., None]
    arr[..., :3] = rgb * (1 - m) + new * m
    return Image.fromarray((np.clip(arr, 0, 1) * 255).astype(np.uint8), "RGBA")

def hsv(im):
    arr = np.asarray(im).astype(np.float32) / 255.0
    r, g, b = arr[..., 0], arr[..., 1], arr[..., 2]
    mx, mn = arr[..., :3].max(-1), arr[..., :3].min(-1)
    s = np.where(mx > 0, (mx - mn) / np.maximum(mx, 1e-6), 0)
    return mx, s, arr[..., 3]

def soften(mask, radius=2):
    m = Image.fromarray((mask * 255).astype(np.uint8)).filter(ImageFilter.GaussianBlur(radius))
    return np.asarray(m).astype(np.float32) / 255

def save(im, name):
    os.makedirs(OUT, exist_ok=True)
    im.save(os.path.join(OUT, name + ".png"), optimize=True)

# --- eşyalar ------------------------------------------------------------------

def raw_item(src):
    return fit_max(trim(load(src)))

def floor_item(name, src, **grade):
    return warm_grade(raw_item(src), **grade)

def variant(raw, mask, dark, light, strength=0.9, gamma=1.0, **grade):
    """Maskeyi ve renklendirmeyi ham görselde yapar, ardından odanın ışığına uydurur."""
    return warm_grade(colorize(raw, mask, dark, light, strength, gamma), **grade)

def build():
    # Yatak: dış kumaş koyu (siyah) -> kakao, ada yeşili, gül kurusu.
    bed = raw_item("prop_cat_bed.png")
    v, sat, a = hsv(bed)
    outer = soften(((v < 0.42) & (a > 0.5)).astype(np.float32), 3)
    save(contact_shadow(variant(bed, outer, (52, 40, 34), (150, 120, 98), 0.85)), "bed_cocoa")
    save(contact_shadow(variant(bed, outer, (58, 74, 56), (160, 182, 150))), "bed_sage")
    save(contact_shadow(variant(bed, outer, (96, 56, 58), (214, 160, 156))), "bed_rose")

    # Halı: zebra deseni -> sıcak tonlarda çizgili halı (kahve/krem, kiremit, ada yeşili).
    rug = trim(load("prop_cat_rug.png"))
    rug = fit_max(rug)
    _, _, a = hsv(rug)
    full = soften((a > 0.05).astype(np.float32), 1)
    save(warm_grade(colorize(rug, full, (92, 62, 40), (246, 230, 205)), side_light=0.04), "rug_caramel")
    save(warm_grade(colorize(rug, full, (128, 62, 40), (240, 206, 176)), side_light=0.04), "rug_terracotta")
    save(warm_grade(colorize(rug, full, (70, 86, 64), (226, 232, 206)), side_light=0.04), "rug_sage")

    # Mama kabı: çelik -> sıcak çelik, bakır, krem emaye (mama taneleri korunur).
    bowl = raw_item("prop_cat_bowl.png")
    v, sat, a = hsv(bowl)
    metal = soften(((sat < 0.30) & (a > 0.5)).astype(np.float32), 2)
    save(contact_shadow(warm_grade(bowl, sat=0.95)), "bowl_steel")
    save(contact_shadow(variant(bowl, metal, (98, 50, 28), (250, 190, 146), 0.95, gamma=0.9)), "bowl_copper")
    save(contact_shadow(variant(bowl, metal, (156, 134, 110), (253, 246, 232), 0.95, gamma=0.8)), "bowl_cream")

    # Su kabı: seramik krem + ada yeşili + gül kurusu.
    water = raw_item("cat_water.png")
    v, sat, a = hsv(water)
    ceramic = soften(((v > 0.45) & (a > 0.5)).astype(np.float32), 2)
    save(contact_shadow(warm_grade(water)), "water_cream")
    save(contact_shadow(variant(water, ceramic, (84, 104, 82), (214, 228, 204), 0.85, gamma=0.9)), "water_sage")
    save(contact_shadow(variant(water, ceramic, (126, 82, 82), (240, 208, 200), 0.85, gamma=0.9)), "water_rose")

    # Kedi ağacı: gri-bej halı kaplama -> taupe (orijinal), ceviz, krem; sisal ip korunur.
    tree = raw_item("cat_tree.png")
    arr = np.asarray(tree).astype(np.float32) / 255
    a = arr[..., 3]
    rope = (arr[..., 0] - arr[..., 2]) > 0.10          # sisal ip daha sarı/turuncu
    carpet = soften(((a > 0.5) & ~rope).astype(np.float32), 1)
    save(contact_shadow(warm_grade(tree), 0.9, 0.10), "tree_taupe")
    whole = soften((a > 0.5).astype(np.float32), 1)   # ip dahil: lekesiz, tek tip kaplama
    save(contact_shadow(variant(tree, carpet, (56, 36, 24), (170, 124, 88), 0.92), 0.9, 0.10), "tree_walnut")
    save(contact_shadow(variant(tree, whole, (128, 116, 100), (248, 242, 230), 0.95, gamma=0.75), 0.9, 0.10), "tree_cream")

    # Oyuncak fare: gri + krem + karamel (pembe kulak/burun ve göz korunur).
    toy = raw_item("cat_toy_mouse.png")
    arr = np.asarray(toy).astype(np.float32) / 255
    v, sat, a = hsv(toy)
    pinkish = (arr[..., 0] - arr[..., 2]) > 0.12
    fur = soften(((v > 0.16) & (a > 0.5) & ~pinkish).astype(np.float32), 2)
    save(contact_shadow(warm_grade(toy), 0.7, 0.16), "toy_gray")
    save(contact_shadow(variant(toy, fur, (150, 132, 112), (252, 244, 230), 0.92), 0.7, 0.16), "toy_cream")
    save(contact_shadow(variant(toy, fur, (104, 60, 32), (236, 182, 122), 0.92), 0.7, 0.16), "toy_caramel")

    # Kum kabı: animasyon kareleri bu modeli içerdiği için tek model; yalnız ışık uyumu.
    save(contact_shadow(floor_item("sand", "new_cat_sand.png", sat=0.85), 0.9, 0.11), "sand_white")

    # Kum kabı animasyon kareleri kabın kendisini içerir; yerleştirilen kabla aynı ışığı alsınlar.
    for frame_name in ("cat_sand_out.png", "cat_sand_in.png", "cat_sand_in2.png"):
        graded = warm_grade(load(frame_name), sat=0.85)
        graded.save(os.path.join(ROOT, "Assets", frame_name), optimize=True)

    # Posterler: aynı çerçeveye Mitzi'nin üç farklı fotoğrafı.
    frame = trim(load("cat_poster.png"), pad=0)
    fw, fh = frame.size
    fa = np.asarray(frame)
    # İç fotoğraf alanı: çerçevenin iç kenarı (çerçeve kalınlığı ~%11)
    bx, by = int(fw * 0.115), int(fh * 0.085)
    inner = (bx, by, fw - bx, fh - by)
    save(wall_shadow(warm_grade(frame, side_light=0.03)), "poster_mitzi_sofa")
    for nm, photo, focus in [("poster_mitzi_curious", "completion_mitzi_face_two.png", (0.0, 0.0, 1.0, 0.75)),
                             ("poster_mitzi_hello", "completion_mitzi_face_big.png", (0.0, 0.0, 1.0, 0.55))]:
        p = load(photo)
        pw, ph = p.size
        crop = p.crop((int(pw * focus[0]), int(ph * focus[1]), int(pw * focus[2]), int(ph * focus[3])))
        bg = Image.new("RGBA", crop.size, (238, 226, 208, 255)); bg.alpha_composite(crop)
        iw, ih = inner[2] - inner[0], inner[3] - inner[1]
        s = max(iw / bg.width, ih / bg.height)
        bg = bg.resize((round(bg.width * s), round(bg.height * s)), Image.LANCZOS)
        ox, oy = (bg.width - iw) // 2, 0
        bg = bg.crop((ox, oy, ox + iw, oy + ih))
        poster = frame.copy(); poster.paste(bg, inner[:2])
        save(wall_shadow(warm_grade(poster, side_light=0.03)), nm)

    # Duvar renkleri: odanın sıva duvarını maskeleyip yeniden renklendirir (pencere, bitkiler, zemin korunur).
    room = Image.open(os.path.join(SRC, "home_background_gemini.jpg")).convert("RGBA")
    arr = np.asarray(room).astype(np.float32) / 255
    h = arr.shape[0]
    v, s, a = hsv(room)
    rr, gg, bb = arr[..., 0], arr[..., 1], arr[..., 2]
    hue_ok = (rr >= gg) & (gg >= bb * 0.95) & (gg > rr * 0.55)   # bej/turuncu aralığı (bitkiler yeşil, dışarıda)
    wall = (s < 0.66) & (v > 0.36) & hue_ok
    ys = np.arange(h)[:, None] / h
    wall &= np.broadcast_to(ys < 0.605, wall.shape)        # süpürgeliğin üstü
    # pencere çerçevesi ve bitkiler: daha doygun/koyu; ayrıca sol pencere bloğunu dışarıda tut
    xs = np.arange(arr.shape[1])[None, :] / arr.shape[1]
    window = (xs > 0.085) & (xs < 0.425) & (ys < 0.52)
    wall &= ~np.broadcast_to(window, wall.shape)
    m = soften(wall.astype(np.float32), 3)
    for nm, dark, light in [("wall_sage", (96, 112, 88), (236, 238, 212)),
                            ("wall_rose", (128, 88, 84), (250, 226, 214)),
                            ("wall_sky", (88, 104, 118), (232, 236, 236))]:
        out = colorize(room, m, dark, light, 0.85, gamma=0.95).convert("RGB")
        os.makedirs(OUT, exist_ok=True)
        out.save(os.path.join(OUT, nm + ".jpg"), quality=90)

if __name__ == "__main__":
    build()
    print("tamam:", sorted(os.listdir(OUT)))
