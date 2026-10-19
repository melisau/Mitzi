# Ghost, Boza, ev yönleri ve mekan ilerlemesi — 19 Ekim 2026

İzole proje: `D:/Projects/Mitzi-AnimalValidation-20261018`, ürün adı `MitziValidation`. Kullanıcının açık Unity projesi kapatılmadı, gerçek oyuncu kaydı sıfırlanmadı.

`NewCatsAndAreasValidation.Run` başarıyla tamamlandı (Unity 2022.3.62f3, çıkış kodu 0).

- Sekiz kedi, her birinde 12 yürüme/tırmanma karesi, 11 Animator durumu; boş kare veya eksik durum yok.
- Ghost ve Boza kendilerine ait yürüme, duruş, zıplama, eğilme, uyuma, yeme, içme, tırmanma ve evde önden/arkadan yürüme görsellerini kullanır. Atlaslar ve gerçek Unity önizlemeleri incelendi; Boza eğilme satırı 5 hücre olarak kaydedilerek komşu kedinin kesilmiş parçası düzeltildi.
- Evde aşağı hareket önden, yukarı hareket arkadan gösterilir; durunca son yön korunur. Yana hareket, bakım ve uyku kendi animasyonlarına döner. Bölüm oynanışına ev yönleri sızmaz.
- Yeni kurtarma sırası: Ghost 7, Boza 20, Tarçın 30, Ada 40, Moka 50, Pamuk 60, Kömür 70. Erken açılma ve çift kurtarma engellenir. Yeni kimlikler kayda yazılır, eski kayıttaki Pamuk korunur.
- Mekan eşikleri 0/9/10/19/20/29/30/99 seviyelerinde doğrulandı. Yeni oyunda yalnız Sokak açıktır. Orman 10, Cadde 20, Neo Teknoloji 30 tamamlanınca açılır.
- Gerçek bölüm tamamlama akışında 10/20/30 bildirimleri, kedi kurtarma ekranıyla çakışma, mevcut mekanda kalma, yeni mekanı seçme, eski mekana dönüş ve aynı bitirişte ikinci bildirimin engellenmesi doğrulandı.
- Önceki evde manuel kontrol, uyandırma, ayar, ilk oynayış, hareket/yön ve zorluk doğrulamaları da geçti.

Log: `AnimalArtSources-20261018/unity-new-cats-areas-verified.log`. Önizlemeler aynı klasörün izole kopyasında `home-direction-preview.png`, `ghost-boza-preview.png`, `area-unlock-preview.png`.

Üretim istemleri ve varlık konumları: [NEW_CATS_ART_PROMPTS.md](NEW_CATS_ART_PROMPTS.md). Fotoğraf referansları GitHub'a eklenmedi.

Bu doğrulama fiziksel telefonda yeni sürümle elle oynama testinin yerine geçmez. Canlı reklamlar kapalı kalır.
