# Yol Çizen Pati (Paw Path)

Doodle Road tarzı, kedi karakterli cozy mobil oyun. Parmakla yol çizersin; kedi fizikle o yolda yürür. Düşünce yanmaz, pembe baloncukla doğduğu yere döner. Her 5 bölümde yeni bir kedi eve davet edilir. Ev koleksiyon ve okşama merkezidir.

Hedef motor: **Unity 2022.3 LTS + 2D (Built-in)**. Sprite shader'ları pembe çıkmasın diye ilk prototip URP zorunlu değil; URP'ye geçince LineDraw materyalini Universal 2D'ye çevirmen yeterli.

## Neden bu mimari

- Tek sahne, durum makinesi (`GameFlow`): mobilde sahne yükü ve bellek sıçraması olmaz.
- Kedi / ürün / bölüm verisi `ScriptableObject`; kayıt `PlayerPrefs` JSON. Tasarımcı asset değiştirir, kayıt kimlik tutar.
- `CatalogFactory` asset yokken bile prototipi ayağa kaldırır. Grafik gelince sprite alanlarını doldurman yeter.
- Çizim `LineRenderer + EdgeCollider2D`: Doodle Road’daki “çizdiğin şey zemin olur” hissi, ekstra navmesh olmadan.

## Unity’de açılış (zorunlu)

1. Unity Hub → Add → bu klasör (`Mitzi`).
2. 2D (URP) şablonu değil, **mevcut proje** olarak aç (Packages zaten URP referansı içerir). İlk import birkaç dakika sürebilir.
3. Menü: **Paw Path → Build Starter Scene**
4. `Assets/Scenes/PawPath.unity` açılır. Play.

Editörde fare ile çiz, telefonda parmak. Hub’da kediye basılı tutarak okşa (Sevgi Puanı), kısa dokunuşla oynanacak kediyi seç. **Yola Çık** ile bölüm.

Kayıt sıfırlamak: **Paw Path → Reset Save Data**

## Sistemler

| Script | Görev |
|---|---|
| `LineDraw` | Fırça, mürekkep bütçesi, collider yol |
| `CatController` | Rigidbody2D yürüyüş, baloncukla dönüş |
| `CatPettingSystem` | Mırıldama, kalp, titreşim, Love Points |
| `LevelManager` + `CatUnlockService` | Bölüm ve her 5. seviyede kedi |
| `CozyEconomyManager` | Sevgi puanı ve dükkan |
| `CatHouseManager` | Koleksiyon spawn + seçim |
| `RuntimeBootstrap` | Boş sahnede oynanır iskelet |

## Grafik ve ses ekleme

- `Assets/_Game/Art/Prompts/MIDJOURNEY.md` — karakter, ev, mevsim, UI promptları
- `Assets/_Game/Audio/Docs/SOUND_DESIGN.md` — loop ve SFX talimatı
- Sprite’ları `CatDefinition` / `ShopItemDefinition` alanlarına sürükle
- Klipleri `CozyAudioManager` Inspector’ına ata

## Kediler (her 5 bölüm)

Mitzi (açık) → Pamuk (5) → Kömür (10) → Tarçın (15) → Ada (20) → Moka (25)

## Not

İlk Play’de daire placeholder sprite’lar görünür; bu bilinçli. Pastel renkler kedi ırkını ayırır. Asıl art pipeline prompt dosyalarındadır.
