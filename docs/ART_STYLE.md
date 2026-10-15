# Mitzi görsel stil rehberi ve AI prompt seti

Oyunun tarzı **sıcak gerçekçi**: gerçek Mitzi fotoğrafları ve Gemini ile üretilmiş, gün ışığı alan odadan (`Assets/_Game/Art/home_background_gemini.jpg`) yola çıkar. Yeni her görsel bu odaya aynı ışıkta konmuş gibi görünmeli.

> Eski `MIDJOURNEY.md` (pastel/çizgi film tarzı) bu rehberle değiştirildi; o tarz artık kullanılmıyor.

## Kurallar

| Konu | Kural |
| --- | --- |
| Işık | Soldan, pencereden gelen **altın saat (golden hour) güneşi**; sıcak beyaz, yumuşak gölgeler. Stüdyo/beyaz kutu ışığı yok. |
| Açı | Tüm zemin eşyaları **önden, hafif yukarıdan (yaklaşık 15°)** — odadaki zeminle aynı perspektif. |
| Renk | Krem, bal, karamel, ceviz, ada çayı yeşili, gül kurusu, açık gökyüzü mavisi. Siyah, neon ve soğuk gri yok. |
| Malzeme | Doğal: ahşap, seramik, keten, yün, sisal, peluş. Parlak plastik ve logolu ürün yok. |
| Zemin | **Düz, saf beyaz arka plan**, eşyanın altında gölge olmasın (gölgeyi oyun ekler). Tek nesne, kenarlardan biraz boşluk. |
| İçerik | Yazı, logo, marka, filigran, insan, kedi yok (kediler ayrı görsellerde). |
| Boyut | En az 1024×1024 (duvarlar/odalar 16:9, en az 1920×1080). |

### Her promptun sonuna eklenecek stil cümlesi

```
photorealistic product photo for a cozy cottage cat game, warm golden-hour sunlight coming from the left, soft natural shadows, warm color palette of cream, honey, caramel and walnut, front view from slightly above, single object centered, isolated on a plain pure white background, no shadow on the ground, no text, no logo, no people, no animals
```

## Prompt seti (her yer için 3 seçenek)

Görseli üretince bana yükle; arka planı temizleyip, ışığı odaya uydurup, gölgesini ekleyip oyuna koyarım (`tools/harmonize_furniture.py`). Dosya adını tablodaki gibi verirsen hangi eşyaya ait olduğunu otomatik bilirim.

### Yatak (`bed_*`)
- `bed_cocoa` — round cat bed with a cocoa brown linen outer rim and a fluffy cream sherpa cushion, [stil]
- `bed_sage` — round cat bed with sage green linen rim and cream plush cushion, [stil]
- `bed_rose` — oval cat bed with dusty rose velvet rim and cream faux-fur cushion, [stil]

### Halı (`rug_*`)
- `rug_caramel` — round hand-woven jute and cream wool rug with soft caramel stripes, seen at a low angle so it looks like an oval lying on the floor, [stil]
- `rug_terracotta` — round shaggy rug in warm terracotta and cream, low angle oval view, [stil]
- `rug_sage` — round braided rag rug in sage green and cream, low angle oval view, [stil]

### Mama kabı (`bowl_*`) — dolu, kuru mama ile
- `bowl_steel` — brushed stainless steel cat food bowl filled with dry kibble, [stil]
- `bowl_copper` — hammered copper cat food bowl on a small wooden stand, filled with dry kibble, [stil]
- `bowl_cream` — cream glazed stoneware cat food bowl with a hand-painted paw print, filled with dry kibble, [stil]

### Su kabı (`water_*`) — su dolu
- `water_cream` — wide cream ceramic cat water bowl filled with clear water, [stil]
- `water_sage` — sage green ceramic cat water bowl filled with clear water, [stil]
- `water_rose` — dusty rose ceramic cat water fountain bowl filled with clear water, [stil]

### Kum kabı (`sand_*`)
> Kum kabı animasyon kareleri (`cat_sand_in`, `cat_sand_in2`, `cat_sand_out`) kabın kendisini içerir. Yeni kum kabı modeli eklemek için o üç karenin de aynı kapla yeniden üretilmesi gerekir; bu yüzden şimdilik tek model var.
- `sand_wood` — covered wooden cat litter box shaped like a small cottage with a round door opening, light oak wood, [stil]
  - Kareler: aynı kum kabı, (1) gri tabby kedi kapıdan içeri giriyor, (2) kedi içeride oturuyor, (3) kedi kapıdan çıkıyor — her biri ayrı görsel.

### Kedi ağacı (`tree_*`)
- `tree_taupe` — three-level cat tree with taupe carpet platforms, sisal rope scratching posts and a round cubby, [stil]
- `tree_walnut` — modern cat tree made of walnut wood platforms with cream cushions and sisal posts, [stil]
- `tree_cream` — cat tree with cream boucle platforms, a hammock and sisal posts, [stil]

### Oyuncak (`toy_*`)
- `toy_gray` — small soft gray plush toy mouse with pink ears and a short string tail, [stil]
- `toy_cream` — small soft cream plush toy mouse with pink ears and a short string tail, [stil]
- `toy_caramel` — small soft caramel brown plush toy mouse with cream ears and a short string tail, [stil]

### Poster (`poster_*`)
Posterlerde **gerçek Mitzi fotoğrafları** kullan (oyunun kalbi o). Çerçeve için:
- `frame_oak` — empty light oak wooden picture frame, portrait orientation, plain white center area, [stil]
- `frame_gold` — empty antique gold picture frame, portrait orientation, plain white center area, [stil]

### Duvar / oda (`room_*`) — 16:9
Odayla birebir aynı kompozisyon (sol tarafta bahçeye bakan pencere, sağda asılı saksılar, ahşap zemin, süpürgelik), yalnız duvar değişsin:
```
empty cozy cottage living room, large wooden window on the left looking out to a sunny garden with pink flowers and a white picket fence, hanging plants in woven baskets on the right, warm wooden plank floor, wooden baseboard, wide 16:9 view, no furniture, no animals, no people, photorealistic, warm golden-hour sunlight, [DUVAR]
```
- `room_sage` — [DUVAR] = walls painted soft sage green limewash
- `room_rose` — [DUVAR] = walls painted dusty rose limewash
- `room_wallpaper` — [DUVAR] = walls covered with cream wallpaper with a small botanical leaf pattern

## Lisans

Oyunda kaynağı bilinmeyen görsel kalmamalı. İnternetten bulunmuş ürün/stok fotoğraflarıyla yayınlamak telif şikayeti ve Play'den kaldırılma riski taşır. Aşağıdakilerin kaynağı belirsiz; yukarıdaki promptlarla üretilen kendi görsellerinle değiştirilmeli:

| Dosya | Durum |
| --- | --- |
| `prop_cat_bed.png`, `prop_cat_bowl.png`, `prop_cat_rug.png` | Ürün fotoğrafına benziyor → değiştir |
| `cat_water.png`, `new_cat_sand.png` (+ `cat_sand_*` kareleri), `cat_tree.png`, `cat_toy_mouse.png` | Ürün fotoğrafına benziyor → değiştir |
| `Assets/_Game/home_wallpaper.png`, `home_wallpaper2.png`, `shop_wallpaper.png` | Stok fotoğrafına benziyor; oyunda kullanılmıyorsa sil |
| `home_background_gemini.jpg` | Gemini ile üretildi → kullanılabilir |
| `cat_poster.png`, `completion_mitzi_*` | Kendi Mitzi fotoğrafların ise sorun yok |

AI ile ürettiğin görsellerin kullanım koşullarını (ör. Gemini'nin ticari kullanım şartlarını) da bir kez kontrol et.
