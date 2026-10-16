"""
Kedi evi eşyalarını sıfırdan, kodla 3D render eder (telifsiz, özgün görseller).

Küçük bir numpy SDF ışın izleyici: ortografik kamera (odanın zeminiyle aynı, ~18° yukarıdan),
soldaki pencereden gelen sıcak güneş, sıcak tavan dolgusu, yumuşak gölge ve ortam kapatma (AO).
Çıktı: tools/furniture_src/rendered/<ad>.png (şeffaf arka plan). Ardından
tools/harmonize_furniture.py bunları oyuna hazırlar (gölge, ışık uyumu).

Kullanım: python3 tools/render_furniture.py [ad ...]
"""
import math, os, sys
import numpy as np
from PIL import Image

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
OUT = os.path.join(ROOT, "tools", "furniture_src", "rendered")
F = np.float32

# ---------------------------------------------------------------- vektör yardımcıları
def v3(x, y, z): return np.array([x, y, z], F)
def length(p): return np.sqrt((p * p).sum(-1))
def normalize(v): return v / np.maximum(length(v)[..., None], 1e-6)
def clamp(x, a, b): return np.minimum(np.maximum(x, a), b)
def mix(a, b, t): return a + (b - a) * t
def smoothstep(a, b, x):
    t = clamp((x - a) / (b - a), 0, 1); return t * t * (3 - 2 * t)

# ---------------------------------------------------------------- gürültü
def _hash(ix, iy, iz):
    h = (ix * 374761393 + iy * 668265263 + iz * 2147483647) & 0xFFFFFFFF
    h = ((h ^ (h >> 13)) * 1274126177) & 0xFFFFFFFF
    return ((h ^ (h >> 16)) & 0xFFFF).astype(F) / F(65535.0)

def vnoise(p):
    pf = np.floor(p); f = p - pf
    i = pf.astype(np.int64)
    u = f * f * (3 - 2 * f)
    n = 0
    for dx in (0, 1):
        for dy in (0, 1):
            for dz in (0, 1):
                w = (u[..., 0] if dx else 1 - u[..., 0]) * (u[..., 1] if dy else 1 - u[..., 1]) * (u[..., 2] if dz else 1 - u[..., 2])
                n = n + w * _hash(i[..., 0] + dx, i[..., 1] + dy, i[..., 2] + dz)
    return n

def fbm(p, oct=4):
    s, a, tot = 0, 0.5, 0
    for _ in range(oct):
        s = s + a * vnoise(p); tot += a; p = p * 2.03 + 1.7; a *= 0.5
    return s / tot

def cells(p):
    """Voronoi: en yakın hücre noktasına uzaklık (mama taneleri, sisal)."""
    pf = np.floor(p); best = np.full(p.shape[:-1], 9.0, F); bid = np.zeros(p.shape[:-1], F)
    i = pf.astype(np.int64)
    for dx in (-1, 0, 1):
        for dy in (-1, 0, 1):
            for dz in (-1, 0, 1):
                cx, cy, cz = i[..., 0] + dx, i[..., 1] + dy, i[..., 2] + dz
                o = np.stack([_hash(cx, cy, cz), _hash(cy, cz, cx), _hash(cz, cx, cy)], -1)
                d = length(np.stack([cx, cy, cz], -1).astype(F) + o - p)
                m = d < best; best = np.where(m, d, best); bid = np.where(m, _hash(cx, cz, cy), bid)
    return best, bid

# ---------------------------------------------------------------- SDF ilkelleri
def sd_sphere(p, r): return length(p) - r
def sd_ellipsoid(p, r):
    k0 = length(p / r); k1 = length(p / (r * r)); return k0 * (k0 - 1) / np.maximum(k1, 1e-6)
def sd_rbox(p, b, r):
    q = np.abs(p) - (b - r); return length(np.maximum(q, 0)) + np.minimum(q.max(-1), 0) - r
def sd_cyl(p, r, h, rr=0.0):
    """Dikey (y) silindir, yarıçap r, yarı yükseklik h, kenar yuvarlatma rr."""
    d = np.stack([length(p[..., [0, 2]]) - (r - rr), np.abs(p[..., 1]) - (h - rr)], -1)
    return np.minimum(d.max(-1), 0) + length(np.maximum(d, 0)) - rr
def sd_cyl_z(p, r, h, rr=0.0): return sd_cyl(p[..., [0, 2, 1]], r, h, rr)
def sd_torus(p, R, r):
    q = np.stack([length(p[..., [0, 2]]) - R, p[..., 1]], -1); return length(q) - r
def sd_capsule(p, a, b, r):
    pa, ba = p - a, b - a; h = clamp((pa * ba).sum(-1) / (ba * ba).sum(), 0, 1)
    return length(pa - ba * h[..., None]) - r
def smin(a, b, k):
    h = clamp(0.5 + 0.5 * (b - a) / k, 0, 1); return mix(b, a, h) - k * h * (1 - h)
def sub(a, b): return np.maximum(a, -b)
def tr(p, x, y, z): return p - v3(x, y, z)

class Scene:
    """Birleşik SDF: parçalar (d, malzeme_id) olarak eklenir; en yakın kazanır."""
    def __init__(self): self.parts = []
    def add(self, fn, mat): self.parts.append((fn, mat)); return self
    def eval(self, p):
        d = np.full(p.shape[:-1], 1e9, F); m = np.zeros(p.shape[:-1], np.int32)
        for fn, mat in self.parts:
            di = fn(p); c = di < d; d = np.where(c, di, d); m = np.where(c, mat, m)
        return d, m
    def dist(self, p): return self.eval(p)[0]

# ---------------------------------------------------------------- render
SUN = normalize(v3(-0.62, 0.72, 0.45))       # soldan, önden, yukarıdan
SUN_COL = v3(1.0, 0.86, 0.66) * 1.25
SKY_COL = v3(0.62, 0.55, 0.48)
BOUNCE_COL = v3(0.42, 0.28, 0.17)              # ahşap zeminden yansıyan sıcak ışık

def render(scene, materials, size=720, extent=1.2, center=(0, 0.45, 0), elev=18, max_steps=140, ss=1):
    W = H = size * ss
    e = math.radians(elev)
    fwd = v3(0, -math.sin(e), -math.cos(e)); right = v3(1, 0, 0); up = np.cross(right, fwd)
    u = (np.arange(W, dtype=F) + 0.5) / W * 2 - 1; vv = 1 - (np.arange(H, dtype=F) + 0.5) / H * 2
    U, V = np.meshgrid(u * extent, vv * extent)
    origin = v3(*center) - fwd * 6
    ro = origin + U[..., None] * right + V[..., None] * up
    ro = ro.reshape(-1, 3); rd = np.broadcast_to(fwd, ro.shape)
    t = np.zeros(len(ro), F); hit = np.zeros(len(ro), bool); alive = np.ones(len(ro), bool)
    for _ in range(max_steps):
        idx = np.where(alive)[0]
        if len(idx) == 0: break
        p = ro[idx] + rd[idx] * t[idx, None]
        d = scene.dist(p)
        t[idx] += d * 0.9
        h = d < 1e-3; hit[idx[h]] = True
        dead = h | (t[idx] > 14); alive[idx[dead]] = False
    col = np.zeros((len(ro), 3), F); alpha = hit.astype(F)
    idx = np.where(hit)[0]
    if len(idx):
        p = ro[idx] + rd[idx] * t[idx, None]
        _, mid = scene.eval(p)
        e2 = 1e-3
        n = normalize(np.stack([scene.dist(p + v3(e2, 0, 0)) - scene.dist(p - v3(e2, 0, 0)),
                                scene.dist(p + v3(0, e2, 0)) - scene.dist(p - v3(0, e2, 0)),
                                scene.dist(p + v3(0, 0, e2)) - scene.dist(p - v3(0, 0, e2))], -1))
        # malzeme
        alb = np.zeros((len(idx), 3), F); spec = np.zeros(len(idx), F); shin = np.full(len(idx), 16, F)
        metal = np.zeros(len(idx), F); bump = np.zeros((len(idx), 3), F)
        for k, fn in materials.items():
            m = mid == k
            if not m.any(): continue
            r = fn(p[m], n[m])
            alb[m] = r["albedo"]; spec[m] = r.get("spec", 0.05); shin[m] = r.get("shine", 16)
            metal[m] = r.get("metal", 0);
            if "bump" in r: bump[m] = r["bump"]
        n = normalize(n + bump)
        # yumuşak gölge
        sh = np.ones(len(idx), F); tt = np.full(len(idx), 0.02, F); sp = p + n * 0.004
        for _ in range(40):
            d = scene.dist(sp + SUN * tt[:, None])
            sh = np.minimum(sh, clamp(10 * d / tt, 0, 1)); tt += clamp(d, 0.01, 0.12)
        sh = clamp(sh, 0, 1) ** 1.3
        # ortam kapatma
        ao = np.ones(len(idx), F)
        for i in range(1, 6):
            hh = 0.035 * i; ao -= (hh - scene.dist(p + n * hh)) * (1.6 / 2 ** i)
        ao = clamp(ao, 0.25, 1)
        ndl = clamp((n * SUN).sum(-1), 0, 1)
        sky = 0.5 + 0.5 * n[:, 1]
        bounce = clamp(-n[:, 1] * 0.6 + 0.4, 0, 1) * 0.6
        diff = ndl[:, None] * sh[:, None] * SUN_COL + sky[:, None] * SKY_COL * ao[:, None] + bounce[:, None] * BOUNCE_COL * ao[:, None]
        vdir = -rd[idx]; hv = normalize(SUN + vdir)
        sp_ = (clamp((n * hv).sum(-1), 0, 1) ** shin) * sh * spec
        tint = mix(np.ones_like(alb), alb, metal[:, None])
        # metal: çevre yansıması (sıcak oda: üstte açık, altta ahşap)
        refl = rd[idx] - 2 * (rd[idx] * n).sum(-1)[:, None] * n
        env = mix(v3(0.36, 0.22, 0.13), v3(0.95, 0.86, 0.74), smoothstep(-0.3, 0.5, refl[:, 1])[:, None])
        env = env + v3(1.0, 0.85, 0.6) * (clamp((refl * SUN).sum(-1), 0, 1) ** 24)[:, None] * 1.6
        base = alb * diff * (1 - metal[:, None] * 0.75) + metal[:, None] * env * alb * ao[:, None] * (0.6 + 0.6 * sh[:, None])
        c = base + sp_[:, None] * SUN_COL * tint
        # filmik ton eşleme + gama
        c = c / (1 + c * 0.35)
        col[idx] = clamp(c, 0, 1) ** (1 / 2.2)
    img = np.concatenate([col, alpha[:, None]], -1).reshape(H, W, 4)
    im = Image.fromarray((img * 255).astype(np.uint8), "RGBA")
    if ss > 1: im = im.resize((size, size), Image.LANCZOS)
    return im

def project(pt, size=720, extent=1.2, center=(0, 0.45, 0), elev=18, **_):
    """Dünya noktasını render görüntüsündeki piksele çevirir (kedi karelerini yerleştirmek için)."""
    e = math.radians(elev)
    fwd = v3(0, -math.sin(e), -math.cos(e)); right = v3(1, 0, 0); up = np.cross(right, fwd)
    d = np.array(pt, F) - np.array(center, F)
    x, y = float((d * right).sum()), float((d * up).sum())
    return ((x / extent + 1) / 2 * size, (1 - y / extent) / 2 * size, size / (2 * extent))

# ---------------------------------------------------------------- malzemeler
def solid(c, spec=0.06, shine=16, metal=0):
    c = np.array(c, F) / 255
    return lambda p, n: {"albedo": np.broadcast_to(c, p.shape).copy(), "spec": spec, "shine": shine, "metal": metal}

def fabric(c, scale=60, var=0.10, spec=0.03):
    c = np.array(c, F) / 255
    def f(p, n):
        g = fbm(p * scale, 3); w = 0.5 + 0.5 * np.sin(p[..., 0] * scale * 3) * np.sin(p[..., 1] * scale * 3)
        a = c * (1 - var + var * 2 * g)[..., None] * (0.96 + 0.06 * w)[..., None]
        return {"albedo": a, "spec": spec, "shine": 6, "bump": (np.stack([g, g * 0, g], -1) - 0.5) * 0.08}
    return f

def plush(c, scale=90, var=0.16):
    c = np.array(c, F) / 255
    def f(p, n):
        g = fbm(p * scale, 4)
        b = np.stack([fbm(p * scale + 3.1, 2), fbm(p * scale + 7.7, 2), fbm(p * scale + 5.3, 2)], -1) - 0.5
        return {"albedo": c * (1 - var + var * 2 * g)[..., None], "spec": 0.02, "shine": 4, "bump": b * 0.45}
    return f

def wood(c1, c2, scale=8, spec=0.12):
    c1 = np.array(c1, F) / 255; c2 = np.array(c2, F) / 255
    def f(p, n):
        r = p[..., 1] * scale * 1.0 + fbm(p * v3(2, 12, 2), 3) * 3.0 + p[..., 0] * 0.6
        ring = 0.5 + 0.5 * np.sin(r * 6.28)
        g = fbm(p * v3(40, 4, 40), 3)
        a = mix(c1, c2, (ring * 0.7 + g * 0.3)[..., None])
        return {"albedo": a, "spec": spec, "shine": 30}
    return f

def wood_planks(c1, c2):
    c1 = np.array(c1, F) / 255; c2 = np.array(c2, F) / 255
    def f(p, n):
        g = fbm(p * v3(3, 3, 30), 3) * 0.6 + fbm(p * v3(30, 30, 3), 3) * 0.4
        stripes = 0.5 + 0.5 * np.sin((p[..., 0] + p[..., 2]) * 3 + g * 6)
        return {"albedo": mix(c1, c2, (stripes * 0.6 + g * 0.4)[..., None]), "spec": 0.1, "shine": 28}
    return f

def sisal(c, scale=55):
    c = np.array(c, F) / 255
    def f(p, n):
        ang = np.arctan2(p[..., 2], p[..., 0])
        s = 0.5 + 0.5 * np.sin(p[..., 1] * scale * 6.28 + ang * 0.0)
        g = fbm(p * 120, 2)
        a = c * (0.72 + 0.22 * s + 0.12 * g)[..., None]
        return {"albedo": a, "spec": 0.04, "shine": 8, "bump": np.stack([np.zeros_like(s), (s - 0.5) * 0.5, np.zeros_like(s)], -1)}
    return f

def ceramic(c, glaze=0.55):
    c = np.array(c, F) / 255
    def f(p, n):
        g = fbm(p * 18, 3)
        return {"albedo": c * (0.95 + 0.08 * g)[..., None], "spec": glaze, "shine": 90}
    return f

def metal(c, rough=40):
    c = np.array(c, F) / 255
    def f(p, n):
        brushed = fbm(p * v3(400, 8, 400), 2)
        return {"albedo": c * (0.92 + 0.1 * brushed)[..., None], "spec": 0.9, "shine": rough, "metal": 1.0}
    return f

def kibble(c):
    c = np.array(c, F) / 255
    def f(p, n):
        d, idc = cells(p * 14)
        shade = 0.72 + 0.5 * idc
        edge = smoothstep(0.25, 0.55, d)
        a = c * (shade * (1 - 0.55 * edge))[..., None]
        return {"albedo": a, "spec": 0.12, "shine": 20}
    return f

def water_mat(p, n):
    g = fbm(p * 10, 2)
    a = mix(v3(0.30, 0.42, 0.46), v3(0.62, 0.70, 0.70), g[..., None])
    return {"albedo": a, "spec": 1.2, "shine": 140}

# ---------------------------------------------------------------- modeller
def bowl_scene(r_out=0.62, r_top=0.70, h=0.30, wall=0.06, base_ring=True, fill="kibble"):
    """Konik kenarlı kap. Alt yüzey y=0."""
    def shell(p):
        y = p[..., 1]
        rr = r_out + (r_top - r_out) * clamp(y / h, 0, 1)
        q = np.stack([length(p[..., [0, 2]]) - rr, np.abs(y - h / 2) - h / 2], -1)
        outer = np.minimum(q.max(-1), 0) + length(np.maximum(q, 0)) - 0.025
        rin = rr - wall
        q2 = np.stack([length(p[..., [0, 2]]) - rin, np.abs(y - (h / 2 + wall)) - h / 2], -1)
        inner = np.minimum(q2.max(-1), 0) + length(np.maximum(q2, 0))
        lip = sd_torus(tr(p, 0, h, 0), r_top - wall * 0.5, wall * 0.62)
        return smin(sub(outer, inner), lip, 0.02)
    s = Scene().add(shell, 1)
    if fill == "kibble":
        def pile(p):
            d0, _ = cells(p * 14)
            rr = length(p[..., [0, 2]])
            top = h * 0.92 + 0.08 * (1 - (rr / r_top) ** 2) + (0.5 - d0) * 0.05
            disc = np.maximum(rr - (r_out + (r_top - r_out) * 0.85 - wall), p[..., 1] - top)
            return np.maximum(disc, -p[..., 1] + wall)
        s.add(pile, 2)
    elif fill == "water":
        s.add(lambda p: np.maximum(sd_cyl(tr(p, 0, h * 0.5, 0), r_top - wall * 1.4, h * 0.36), -1e9), 2)
    return s

def bed_scene():
    def rim(p):
        q = p * v3(1, 1, 1.32)
        ring = sd_torus(tr(q, 0, 0.22, 0), 0.84, 0.15) * 0.86
        return ring
    def back(p):   # arka tarafta yükselen sırtlık (yatak hissi)
        q = p * v3(1, 1, 1.32)
        r = sd_torus(tr(q, 0, 0.32, -0.04), 0.84, 0.15) * 0.86
        return np.maximum(r, p[..., 2] + 0.05)
    def base(p): return sd_ellipsoid(tr(p, 0, 0.07, 0), v3(1.02, 0.08, 0.76))
    def cushion(p):
        q = tr(p, 0, 0.22, 0.03)
        e = sd_ellipsoid(q, v3(0.72, 0.14, 0.52))
        return e - 0.012 * fbm(p * 30, 2)
    return Scene().add(lambda p: smin(rim(p), back(p), 0.08), 1).add(base, 2).add(cushion, 3)

def rug_scene(r=1.0):
    def rug(p): return sd_cyl(tr(p, 0, 0.02, 0), r, 0.02, 0.012)
    return Scene().add(rug, 1)

def tree_scene():
    s = Scene()
    s.add(lambda p: sd_rbox(tr(p, 0, 0.05, 0), v3(0.62, 0.05, 0.42), 0.03), 1)            # taban
    s.add(lambda p: sd_cyl(tr(p, -0.30, 0.75, -0.05), 0.085, 0.70), 2)                     # direk 1
    s.add(lambda p: sd_cyl(tr(p, 0.32, 0.50, 0.08), 0.085, 0.45), 2)                       # direk 2
    s.add(lambda p: sd_cyl(tr(p, 0.05, 1.20, -0.08), 0.075, 0.42), 2)                      # direk 3
    s.add(lambda p: sd_rbox(tr(p, 0.20, 0.97, 0.05), v3(0.36, 0.035, 0.28), 0.03), 1)      # orta platform
    s.add(lambda p: sd_rbox(tr(p, 0.02, 1.64, -0.06), v3(0.34, 0.035, 0.26), 0.03), 1)    # üst platform
    s.add(lambda p: sub(sd_torus(tr(p, 0.02, 1.70, -0.06), 0.26, 0.06), -(p[..., 1] - 1.70)), 1)  # üst yatak kenarı
    # yuvarlak kovuk (cubby): yatay silindir, önü açık
    def cubby(p):
        q = tr(p, -0.30, 0.52, 0.10)
        outer = sd_cyl_z(q, 0.26, 0.24, 0.04)
        inner = sd_cyl_z(tr(q, 0, 0, 0.06), 0.18, 0.26)
        return sub(outer, inner)
    s.add(cubby, 1)
    return s

def toy_scene():
    s = Scene()
    s.add(lambda p: sd_ellipsoid(tr(p, 0, 0.26, 0), v3(0.42, 0.26, 0.30)), 1)                 # gövde
    s.add(lambda p: sd_ellipsoid(tr(p, 0.36, 0.30, 0), v3(0.12, 0.13, 0.13)), 1)             # baş-burun yumuşatma
    s.add(lambda p: sd_ellipsoid(tr(p, 0.18, 0.50, 0.14), v3(0.10, 0.11, 0.025)), 2)         # kulak sağ
    s.add(lambda p: sd_ellipsoid(tr(p, 0.18, 0.50, -0.14), v3(0.10, 0.11, 0.025)), 2)        # kulak sol
    s.add(lambda p: sd_sphere(tr(p, 0.47, 0.30, 0), 0.04), 3)                               # burun
    s.add(lambda p: sd_sphere(tr(p, 0.36, 0.38, 0.11), 0.03), 4)                            # göz
    s.add(lambda p: sd_sphere(tr(p, 0.36, 0.38, -0.11), 0.03), 4)
    # kuyruk: kıvrık kapsül zinciri
    pts = [v3(-0.40, 0.16, 0), v3(-0.60, 0.10, 0.08), v3(-0.78, 0.07, 0.02), v3(-0.92, 0.10, -0.10)]
    def tail(p):
        d = np.full(p.shape[:-1], 1e9, F)
        for a, b in zip(pts[:-1], pts[1:]): d = np.minimum(d, sd_capsule(p, a, b, 0.018))
        return d
    s.add(tail, 5)
    return s

def litter_scene(front=True, front_only=False):
    """Küçük ahşap kulübe şeklinde kapalı kum kabı.
    front=False: ön duvar yok (iç kısım); front_only=True: yalnız ön duvar + çatı + basamak (kapı deliği boş)."""
    s = Scene()
    W, D, Hh = 0.72, 0.52, 0.52
    def body(p):
        q = tr(p, 0, Hh, 0)
        box = sd_rbox(q, v3(W, Hh, D), 0.04)
        hollow = sd_rbox(tr(q, 0, 0.05, 0), v3(W - 0.06, Hh, D - 0.06), 0.02)
        shell = sub(box, hollow)
        door = sd_cyl_z(tr(p, 0, 0.42, D), 0.34, 0.2)
        shell = sub(shell, door)
        if not front:
            shell = np.maximum(shell, p[..., 2] - (D - 0.07))   # ön duvarı kes
        if front_only:
            shell = np.maximum(shell, -(p[..., 2] - (D - 0.07)))  # yalnız ön duvar
        return shell
    s.add(body, 1)
    def roof(p):
        q = tr(p, 0, 2 * Hh - 0.02, 0)
        outer = np.maximum(np.maximum(np.abs(q[..., 0]) * 0.66 + q[..., 1] - 0.56, -q[..., 1] - 0.02), np.abs(q[..., 2]) - (D + 0.09))
        inner = np.maximum(np.abs(q[..., 0]) * 0.66 + q[..., 1] - 0.47, -q[..., 1] - 0.5)
        return sub(outer, inner) - 0.008
    s.add(roof, 2)
    def gable(p):   # çatı altı üçgen alınlık (ön ve arka)
        q = tr(p, 0, 2 * Hh, 0)
        tri = np.maximum(np.abs(q[..., 0]) * 0.66 + q[..., 1] - 0.47, -q[..., 1] - 0.04)
        slabz = np.abs(q[..., 2]) - D
        g = np.maximum(tri, np.maximum(slabz, -(np.abs(q[..., 2]) - (D - 0.05))))
        if not front: g = np.maximum(g, q[..., 2])
        if front_only: g = np.maximum(g, -q[..., 2])
        return g
    s.add(gable, 1)
    s.add(lambda p: sd_rbox(tr(p, 0, 0.03, D + 0.10), v3(0.42, 0.03, 0.12), 0.02), 3)   # basamak/paspas
    if not front:
        s.add(lambda p: sd_rbox(tr(p, 0, 0.10, 0), v3(W - 0.07, 0.04, D - 0.07), 0.01), 4)  # kum
    return s

# ---------------------------------------------------------------- eşya tanımları
PALETTE = {
    "steel": metal((150, 156, 166), 90), "copper": metal((232, 140, 92), 50),
    "cream_ceramic": ceramic((243, 233, 214)), "sage_ceramic": ceramic((168, 190, 160)),
    "rose_ceramic": ceramic((222, 168, 160)), "kibble": kibble((140, 82, 44)),
}

def items():
    it = {}
    # Mama kabı
    it["bowl_steel"] = (lambda: bowl_scene(), {1: PALETTE["steel"], 2: PALETTE["kibble"]}, dict(extent=0.85, center=(0, 0.2, 0)))
    it["bowl_copper"] = (lambda: bowl_scene(), {1: PALETTE["copper"], 2: PALETTE["kibble"]}, dict(extent=0.85, center=(0, 0.2, 0)))
    it["bowl_cream"] = (lambda: bowl_scene(r_out=0.58, r_top=0.66, h=0.34, wall=0.08), {1: PALETTE["cream_ceramic"], 2: PALETTE["kibble"]}, dict(extent=0.85, center=(0, 0.2, 0)))
    # Su kabı (geniş seramik)
    for nm, mat in (("water_cream", "cream_ceramic"), ("water_sage", "sage_ceramic"), ("water_rose", "rose_ceramic")):
        it[nm] = (lambda: bowl_scene(r_out=0.55, r_top=0.72, h=0.30, wall=0.08, fill="water"), {1: PALETTE[mat], 2: water_mat}, dict(extent=0.85, center=(0, 0.2, 0)))
    # Yatak
    for nm, col in (("bed_cocoa", (122, 84, 60)), ("bed_sage", (132, 156, 120)), ("bed_rose", (196, 132, 128))):
        it[nm] = (bed_scene, {1: fabric(col), 2: solid((150, 108, 70)), 3: plush((246, 238, 224))}, dict(extent=1.25, center=(0, 0.25, 0)))
    # Halı (konsantrik örgü desen)
    def rug_mat(c1, c2):
        c1 = np.array(c1, F) / 255; c2 = np.array(c2, F) / 255; c3 = (c1 * 0.55 + c2 * 0.45)
        def f(p, n):
            r = length(p[..., [0, 2]]) * 1.0
            ang = np.arctan2(p[..., 2], p[..., 0])
            warp = r + 0.03 * fbm(p * 5, 2)
            k = np.floor(warp * 7.0)
            band = (k % 3)
            braid = 0.5 + 0.5 * np.sin(ang * (40 + k * 18) + warp * 90)
            g = fbm(p * 90, 3)
            col = np.where((band == 0)[..., None], c1, np.where((band == 1)[..., None], c2, c3))
            a = col * (0.80 + 0.22 * braid + 0.10 * g)[..., None]
            return {"albedo": a, "spec": 0.02, "shine": 5, "bump": np.stack([(braid - 0.5) * 0.35, braid * 0, (g - 0.5) * 0.3], -1)}
        return f
    for nm, c1, c2 in (("rug_caramel", (214, 170, 120), (246, 232, 208)), ("rug_terracotta", (190, 104, 72), (240, 214, 186)), ("rug_sage", (140, 160, 124), (232, 232, 210))):
        it[nm] = (rug_scene, {1: rug_mat(c1, c2)}, dict(extent=1.15, center=(0, 0.0, 0), elev=18))
    # Kedi ağacı
    for nm, c in (("tree_taupe", (112, 94, 78)), ("tree_walnut", (120, 82, 58)), ("tree_cream", (236, 226, 208))):
        it[nm] = (tree_scene, {1: plush(c, 60, 0.12), 2: sisal((206, 170, 118))}, dict(extent=1.05, center=(0, 0.95, 0), size=760))
    # Oyuncak fare
    for nm, c in (("toy_gray", (150, 146, 142)), ("toy_cream", (238, 228, 210)), ("toy_caramel", (190, 128, 78))):
        it[nm] = (toy_scene, {1: plush(c, 110, 0.14), 2: solid((236, 160, 160)), 3: solid((222, 132, 140), 0.2), 4: solid((30, 24, 22), 0.8, 80), 5: solid((90, 70, 60))},
                  dict(extent=1.0, center=(-0.2, 0.25, 0)))
    # Kum kabı
    sand_mats = {1: wood_planks((214, 176, 128), (188, 146, 98)), 2: wood((150, 96, 62), (118, 72, 44)), 3: plush((150, 160, 132), 60, 0.1), 4: plush((226, 210, 180), 160, 0.25)}
    it["sand_wood"] = (lambda: litter_scene(True), sand_mats, dict(extent=1.12, center=(0, 0.74, 0.1), size=760))
    it["sand_wood_front"] = (lambda: litter_scene(True, True), sand_mats, dict(extent=1.12, center=(0, 0.74, 0.1), size=760))
    it["sand_wood_back"] = (lambda: litter_scene(False), sand_mats, dict(extent=1.12, center=(0, 0.74, 0.1), size=760))
    return it

def _sprite(path, height_px, flip=False):
    im = Image.open(os.path.join(ROOT, path)).convert("RGBA")
    im = im.crop(im.getbbox())
    if flip: im = im.transpose(Image.FLIP_LEFT_RIGHT)
    s = height_px / im.height
    return im.resize((max(1, round(im.width * s)), max(1, round(height_px))), Image.LANCZOS)

def compose_litter_frames():
    """Kum kabı animasyon kareleri: arka (iç) katman -> kedi -> ön duvar.
    Kareler kabın kendisiyle aynı tuvali kullanır; oyunda kap zıplamadan kareye geçer."""
    kw = dict(extent=1.12, center=(0, 0.74, 0.1), size=760)
    back = Image.open(os.path.join(OUT, "sand_wood_back.png"))
    front = Image.open(os.path.join(OUT, "sand_wood_front.png"))
    full = Image.open(os.path.join(OUT, "sand_wood.png"))
    D = 0.52
    dx, dy, ppu = project((0, 0.42, D), **kw)          # kapı merkezi
    door_r = 0.34 * ppu
    # 1) Giriş: Mitzi paspasın üstünde, kapıya doğru eğilmiş (tamamen önde)
    f1 = full.copy()
    cat = _sprite("Assets/_Game/Art/Cats/mitzi_crouch.png", door_r * 1.55)
    mx, my, _ = project((0.0, 0.03, D + 0.16), **kw)
    # baş kapının sol kenarına uzanıyor: içeri girmek üzere
    f1.alpha_composite(cat, (int(dx - cat.width * 0.88), int(my - cat.height * 0.95)))
    # 2) İçeride: Mitzi kapıdan dışarı bakıyor
    from PIL import ImageDraw as _D
    door_mask = Image.new("L", back.size, 0)
    _D.Draw(door_mask).ellipse([dx - door_r * 1.02, dy - door_r * 1.02, dx + door_r * 1.02, dy + door_r * 1.02], fill=255)
    def inside(img, pos):
        layer = Image.new("RGBA", back.size, (0, 0, 0, 0)); layer.alpha_composite(img, pos)
        a = Image.fromarray(np.minimum(np.asarray(layer.split()[3]), np.asarray(door_mask)).astype(np.uint8))
        layer.putalpha(a); return layer
    f2 = back.copy()
    face = _sprite("Assets/_Game/Art/completion_mitzi_face_big.png", door_r * 2.1)
    f2.alpha_composite(inside(face, (int(dx - face.width / 2), int(dy - door_r * 0.85))))
    f2.alpha_composite(front)
    # 3) Kumda: yalnız kulak uçları görünüyor, kapıdan kum tozu uçuşuyor
    f3 = back.copy()
    face2 = _sprite("Assets/_Game/Art/completion_mitzi_face_two.png", door_r * 2.0)
    f3.alpha_composite(inside(face2, (int(dx - face2.width / 2), int(dy + door_r * 0.15))))
    f3.alpha_composite(front)
    rng = np.random.default_rng(7)
    dust = Image.new("RGBA", f3.size, (0, 0, 0, 0))
    from PIL import ImageDraw, ImageFilter
    d = ImageDraw.Draw(dust)
    for _ in range(26):
        ang = rng.uniform(math.pi * 0.15, math.pi * 0.85); r = rng.uniform(0.2, 1.25) * door_r
        x = dx + math.cos(ang) * r * 1.3; y = dy + door_r * 0.55 - math.sin(ang) * r
        rad = rng.uniform(2.5, 7.5)
        d.ellipse([x - rad, y - rad, x + rad, y + rad], fill=(232, 214, 182, int(rng.uniform(150, 230))))
    f3.alpha_composite(dust.filter(ImageFilter.GaussianBlur(1.2)))
    for nm, im in (("cat_sand_out", f1), ("cat_sand_in2", f2), ("cat_sand_in", f3)):
        im.save(os.path.join(OUT, nm + ".png"))
        print("kare:", nm)

def main(names):
    os.makedirs(OUT, exist_ok=True)
    allit = items()
    for nm in names or allit.keys():
        sc, mats, kw = allit[nm]
        kw = dict(kw); size = kw.pop("size", 720)
        quick = os.environ.get("QUICK")
        im = render(sc(), mats, size=280 if quick else size, ss=1 if quick else 2, **kw)
        im.save(os.path.join(OUT, nm + ".png"))
        print("render:", nm)

if __name__ == "__main__":
    args = sys.argv[1:]
    if args == ["frames"]:
        compose_litter_frames()
    else:
        main(args)
        if not args:
            compose_litter_frames()
