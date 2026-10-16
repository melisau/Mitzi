"""
Her kedi için kendi rengiyle kum kabı animasyon kareleri üretir.

Eski kareler (Assets/cat_sand_*.png) kabın içinde yalnızca gri Mitzi'yi gösteriyordu; bu
yüzden Kömür, Tarçın vb. kum kabına girince griye dönüyordu. Bu betik aynı kulübe
render'ını (tools/furniture_src/rendered/sand_wood*.png) kullanır, kedinin kendi
görsellerini (eat_01 ve sit_01) yerleştirir ve harmonize_furniture.py ile aynı kırpma,
renk ve gölgeyi uygular. Böylece kap kareye geçerken zıplamaz.

Kullanım (repo kökünden):  python3 tools/build_litter_frames.py
Çıktı: Assets/_Game/Art/Cats/<kedi>/<kedi>_litter_{out,in2,in}.png
"""
import math, os, sys
import numpy as np
from PIL import Image, ImageDraw, ImageFilter

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import render_furniture as rf          # noqa: E402
import harmonize_furniture as hf       # noqa: E402

CATS = ["mitzi", "pamuk", "komur", "tarcin", "ada", "moka"]
KW = dict(extent=1.12, center=(0, 0.74, 0.1), size=760)
RENDERED = os.path.join(rf.ROOT, "tools", "furniture_src", "rendered")


def cat_sprite(cat, pose, height_px):
    return rf._sprite(f"Assets/_Game/Art/Cats/{cat}/{cat}_{pose}.png", height_px)


def compose(cat):
    back = Image.open(os.path.join(RENDERED, "sand_wood_back.png")).convert("RGBA")
    front = Image.open(os.path.join(RENDERED, "sand_wood_front.png")).convert("RGBA")
    full = Image.open(os.path.join(RENDERED, "sand_wood.png")).convert("RGBA")
    D = 0.52
    dx, dy, ppu = rf.project((0, 0.42, D), **KW)      # kapı merkezi
    door_r = 0.34 * ppu
    _, mat_y, _ = rf.project((0.0, 0.03, D + 0.16), **KW)

    door_mask = Image.new("L", back.size, 0)
    ImageDraw.Draw(door_mask).ellipse(
        [dx - door_r * 1.02, dy - door_r * 1.02, dx + door_r * 1.02, dy + door_r * 1.02], fill=255)

    def inside(img, pos):
        layer = Image.new("RGBA", back.size, (0, 0, 0, 0))
        layer.alpha_composite(img, pos)
        a = np.minimum(np.asarray(layer.split()[3]), np.asarray(door_mask)).astype(np.uint8)
        layer.putalpha(Image.fromarray(a))
        return layer

    # 1) Giriş: kedi paspasın yanında, başını kapıya uzatıp kokluyor (tamamen önde).
    f1 = full.copy()
    sniff = cat_sprite(cat, "eat_01", door_r * 1.9)
    x = int(dx - sniff.width * 0.78)
    y = int(mat_y - sniff.height * 0.97)
    f1.alpha_composite(sniff, (max(0, x), y))

    # 2) İçeride: kapıdan oturmuş kedinin başı ve göğsü görünüyor.
    sit = cat_sprite(cat, "sit_01", door_r * 3.0)
    # sit_01 sağa bakar; baş görselin sağ üst kısmında.
    head_x = sit.width * 0.80
    head_y = sit.height * 0.17
    f2 = back.copy()
    f2.alpha_composite(inside(sit, (int(dx - head_x), int(dy - door_r * 0.55 - head_y))))
    f2.alpha_composite(front)

    # 3) Kumda: kedi çömelmiş, kapının altında yalnız başının üstü görünüyor; kum tozu uçuşuyor.
    f3 = back.copy()
    f3.alpha_composite(inside(sit, (int(dx - head_x), int(dy + door_r * 0.42 - head_y))))
    f3.alpha_composite(front)
    rng = np.random.default_rng(7)
    dust = Image.new("RGBA", f3.size, (0, 0, 0, 0))
    d = ImageDraw.Draw(dust)
    for _ in range(26):
        ang = rng.uniform(math.pi * 0.15, math.pi * 0.85)
        r = rng.uniform(0.2, 1.25) * door_r
        px = dx + math.cos(ang) * r * 1.3
        py = dy + door_r * 0.55 - math.sin(ang) * r
        rad = rng.uniform(2.5, 7.5)
        d.ellipse([px - rad, py - rad, px + rad, py + rad], fill=(232, 214, 182, int(rng.uniform(150, 230))))
    f3.alpha_composite(dust.filter(ImageFilter.GaussianBlur(1.2)))
    return f1, f2, f3


def union_box():
    """harmonize_furniture.py ile aynı kırpma kutusu (kap görseli değişmesin)."""
    union = None
    for f in ["sand_wood", "cat_sand_out", "cat_sand_in2", "cat_sand_in"]:
        a = np.asarray(Image.open(os.path.join(RENDERED, f + ".png")).convert("RGBA"))[:, :, 3] > 8
        ys, xs = np.where(a)
        box = (xs.min(), ys.min(), xs.max() + 1, ys.max() + 1)
        union = box if union is None else (min(union[0], box[0]), min(union[1], box[1]),
                                           max(union[2], box[2]), max(union[3], box[3]))
    pad = 6
    return (max(0, union[0] - pad), max(0, union[1] - pad), union[2] + pad, union[3] + pad)


def main(cats):
    box = union_box()
    for cat in cats:
        for suffix, im in zip(("litter_out", "litter_in2", "litter_in"), compose(cat)):
            out = hf.contact_shadow(hf.warm_grade(hf.fit_max(im.crop(box)), warmth=0.6, sat=1.0,
                                                  contrast=0.97, lift=0.02), 0.82, 0.10)
            path = os.path.join(rf.ROOT, "Assets", "_Game", "Art", "Cats", cat, f"{cat}_{suffix}.png")
            out.save(path, optimize=True)
            print("kare:", os.path.relpath(path, rf.ROOT))


if __name__ == "__main__":
    main(sys.argv[1:] or CATS)
