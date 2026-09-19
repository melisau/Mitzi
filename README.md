# Yol Çizen Pati (Paw Path)

Doodle Road tarzı, kedi karakterli cozy mobil oyun[cite: 1]. Parmakla yol çizersin; kedi fizikle o yolda yürür[cite: 1]. Düşünce yanmaz, pembe baloncukla doğduğu yere döner ancak ceza olarak enerji/sevgi puanı düşer[cite: 1]. Her 5 bölümde yeni bir kedi eve davet edilir[cite: 1]. Ev; koleksiyon, bakım (mama/su/uyku) ve okşama merkezidir[cite: 1].

Hedef motor: **Unity 2022.3 LTS + 2D (Built-in)**[cite: 1]. Sprite shader'ları pembe çıkmasın diye ilk prototip URP zorunlu değil; URP'ye geçince LineDraw materyalini Universal 2D'ye çevirmen yeterli[cite: 1].

## Neden bu mimari

- Tek sahne, durum makinesi (`GameFlow`): mobilde sahne yükü ve bellek sıçraması olmaz[cite: 1].
- Kedi / ürün / bölüm verisi `ScriptableObject`; kayıt `PlayerPrefs` JSON[cite: 1]. Tasarımcı asset değiştirir, kayıt kimlik tutar[cite: 1].
- `CatalogFactory` asset yokken bile prototipi ayağa kaldırır[cite: 1]. Grafik gelince sprite alanlarını doldurman yeter[cite: 1].
- Çizim `LineRenderer + EdgeCollider2D`: Doodle Road’daki “çizdiğin şey zemin olur” hissi, ekstra navmesh olmadan[cite: 1].

## Oyun içi Mekanikler & Bakım Sistemi

### 🐾 Bakım ve Enerji Yönetimi (`CatNeedsSystem`)
- **Bölüm Giriş Koşulu:** Kedinin bölüme başlayabilmesi için en az **20 Enerji/Sevgi Puanı** olması gerekir. Puan yetersizse kedi yorgun görünür ve "Yola Çık" butonu kilitlenir.
- **Günlük Okşama Limiti (Cap):** Okşayarak mırıldatma ile günde en fazla **100 Puan** kazanılabilir. Limit dolduğunda okşama efekti çalışır ancak puan artmaz.
- **Ev Etkileşimleri (Bakım Puanları):**
  - 🍲 **Mama Kabı:** Mamasını tazelemek **+15 Puan**
  - 🥛 **Su Kabı:** Suyunu doldurmak **+20 Puan**
  - 💤 **Yatak:** Uykuda dinlendirmek **+30 Puan**
- **Düşme Cezası:** Kedi uçurumdan veya zemin dışına düştüğünde pembe baloncukla başa döner ve **-10 Puan** düşer.

### 🛍️ Ekonomi ve Dükkan
- **Tekil Satın Alma Güvenliği:** Her mobilya/aksesuar sadece 1 kez satın alınabilir. Satın alınan ürünler dükkanda "SAT" durumuna geçer ve geri satıldığında maliyetin %50'si iade edilir.

## Unity’de açılış (zorunlu)

1. Unity Hub → Add → bu klasör (`Mitzi`)[cite: 1].
2. 2D (URP) şablonu değil, **mevcut proje** olarak aç (Packages zaten URP referansı içerir)[cite: 1]. İlk import birkaç dakika sürebilir[cite: 1].
3. Menü: **Paw Path → Build Starter Scene**[cite: 1]
4. `Assets/Scenes/PawPath.unity` açılır[cite: 1]. Play.

Editörde fare ile çiz, telefonda parmak[cite: 1]. Hub’da kediye basılı tutarak okşa (Sevgi Puanı), kısa dokunuşla oynanacak kediyi seç[cite: 1]. **Yola Çık** ile bölüm[cite: 1].

Kayıt sıfırlamak: **Paw Path → Reset Save Data**[cite: 1]

## Sistemler

| Script | Görev |
|---|---|
| `LineDraw` | Fırça, mürekkep bütçesi, collider yol[cite: 1] |
| `CatController` | Rigidbody2D yürüyüş, baloncukla dönüş ve düşüş puan cezası[cite: 1] |
| `CatPettingSystem` | Mırıldama, kalp, titreşim ve günlük limitli okşama puanı[cite: 1] |
| `CatNeedsSystem` | Günlük puan limitleri, mama/su/uyku bakımı ve giriş enerjisi kontrolü |
| `LevelManager` + `CatUnlockService` | Bölüm ve her 5. seviyede kedi[cite: 1] |
| `CozyEconomyManager` | Sevgi puanı, dükkan ve tekil satın alma kontrolü[cite: 1] |
| `HubFurnitureView` | Evdeki mobilya slotlarının dinamik görsel güncellenmesi |
| `CatHouseManager` | Koleksiyon spawn + seçim[cite: 1] |
| `RuntimeBootstrap` | Boş sahnede oynanır iskelet[cite: 1] |

## Grafik ve ses ekleme

- `Assets/_Game/Art/Prompts/MIDJOURNEY.md` — karakter, ev, mevsim, UI promptları[cite: 1]
- `Assets/_Game/Audio/Docs/SOUND_DESIGN.md` — loop ve SFX talimatı[cite: 1]
- Sprite’ları `CatDefinition` / `ShopItemDefinition` alanlarına sürükle[cite: 1]
- Klipleri `CozyAudioManager` Inspector’ına ata[cite: 1]

## Kediler (her 5 bölüm)

Mitzi (açık) → Pamuk (5) → Kömür (10) → Tarçın (15) → Ada (20) → Moka (25)[cite: 1]

## Not

İlk Play’de daire placeholder sprite’lar görünür; bu bilinçli[cite: 1]. Pastel renkler kedi ırkını ayırır[cite: 1]. Asıl art pipeline prompt dosyalarındadır[cite: 1].