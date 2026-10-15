"""
Kedi evi eşyalarını odanın (home_background_gemini.jpg) ışığına uydurur ve renk varyantları üretir.

Kullanım (repo kökünden):  python3 tools/harmonize_furniture.py
Zemin eşyaları önce tools/render_furniture.py ile render edilir (tools/furniture_src/rendered/);
posterler Mitzi fotoğraflarından, duvarlar Gemini odasından üretilir. Çıktılar Assets/ içine yazılır.
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
    # Zemin eşyaları: tools/render_furniture.py ile sıfırdan render edilen özgün modeller.
    # (Eski internet kaynaklı ürün fotoğrafları artık kullanılmıyor.)
    shadows = {"bed": (0.92, 0.12), "bowl": (0.9, 0.14), "water": (0.9, 0.14), "tree": (0.9, 0.10), "toy": (0.75, 0.16)}
    for name in ("bed_cocoa", "bed_sage", "bed_rose", "bowl_steel", "bowl_copper", "bowl_cream",
                 "water_cream", "water_sage", "water_rose", "tree_taupe", "tree_walnut", "tree_cream",
                 "toy_gray", "toy_cream", "toy_caramel"):
        im = warm_grade(fit_max(trim(load("rendered/" + name + ".png"))), warmth=0.6, sat=1.0, contrast=0.97, lift=0.02)
        wr, hr = shadows[name.split("_")[0]]
        save(contact_shadow(im, wr, hr), name)
    for name in ("rug_caramel", "rug_terracotta", "rug_sage"):
        im = warm_grade(fit_max(trim(load("rendered/" + name + ".png"))), warmth=0.6, sat=1.0, contrast=0.97, lift=0.02, side_light=0.04)
        save(im, name)

    # Kum kabı (ahşap kulübe) ve animasyon kareleri: aynı kırpma kutusu, aynı ışık ve gölge;
    # böylece oyunda kap kareye geçerken zıplamaz.
    frames = ["sand_wood", "cat_sand_out", "cat_sand_in2", "cat_sand_in"]
    ims = [load("rendered/" + f + ".png") for f in frames]
    union = None
    for im in ims:
        b = trim(im).getbbox()
        a = np.asarray(im)[:, :, 3] > 8
        ys, xs = np.where(a)
        box = (xs.min(), ys.min(), xs.max() + 1, ys.max() + 1)
        union = box if union is None else (min(union[0], box[0]), min(union[1], box[1]), max(union[2], box[2]), max(union[3], box[3]))
    pad = 6
    union = (max(0, union[0] - pad), max(0, union[1] - pad), union[2] + pad, union[3] + pad)
    for f, im in zip(frames, ims):
        out = contact_shadow(warm_grade(fit_max(im.crop(union)), warmth=0.6, sat=1.0, contrast=0.97, lift=0.02), 0.82, 0.10)
        if f == "sand_wood":
            save(out, "sand_cottage")
        else:
            out.save(os.path.join(ROOT, "Assets", f + ".png"), optimize=True)

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
