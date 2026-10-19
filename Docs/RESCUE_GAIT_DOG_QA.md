# Kurtarma sarılması, Ghost/Boza yürüyüşü ve köpek geçitleri

19 Ekim 2026, Unity 2022.3.62f3. Kontroller izole `MitziValidation` ürününde yapıldı; açık kullanıcı projesi kapatılmadı ve gerçek oyuncu kaydı değiştirilmedi.

- `RescueGreetingValidation.Run`: PASS, 56 farklı kurtaran/kurtarılan kedi çifti. Her çift kendi kürkünü kullanır, yüz yüze döner, aynı zeminde sarılır, bir kez davet ekranına geçer. Büyük kedi küçük arkadaşına eğilir; kalpler yükselir. Oyun ve girişler kısa sahne boyunca durur; süre ölçeklenmemiş zamanla ilerler. Gerçek 7. bölümde Ghost kurtarma ve çift ödül engeli doğrulandı.
- Ghost ve Boza eski 12 kare/0,5 saniyelik yürüyüş yerine 8 kare/0,8 saniye (10 FPS) çevrime geçti. Yeni pozlar gerçek video yürüyüşlerinden hareket referansı aldı. Her çevrim tek ölçekte oynar; baş ve pati zemini sprite pivotuyla kayıtlanır. Eski altı kedinin yürüyüşü korunur.
- Ghost normal gövde boyu yüzde 22 küçültüldü. Yeni 2×2 zıplama atlasında ön patiler tamdır. Kazanma portresi kullanıcı yüz fotoğrafından referanslandı; sol göz sarı-yeşil, sağ göz mavi. Portre ve zıplama bağları, görseller ve gerçek Unity çıktıları incelendi.
- Köpeklerin geçit içinde başlaması, her iki yönden koşarak yaklaşması ve havadaki çukur atlayışı doğrulandı. Durma noktası burun payı dahil engelin dışındadır. Durduktan sonra geri dönebilir. Büyük adımda karşısındaki en yakın geçit seçilir.
- Yeni kurtarma/mekan açılış testleri ve önceki ev hareket/bakım, uyku/uyanma ve kedi animasyon doğrulamaları geçti. Sarılma bitmeden mekan açılış kartı görünmez.

Loglar: `AnimalArtSources-20261018/unity-rescue-ghost-dog-final.log`, `unity-dog-passage-final.log`, `unity-dog-meeting-guard.log`. Sonuncusu sarılma sırasında gecikmiş tehlike temasının başarılı kurtarmayı kaybetme ekranına çevirmediğini de doğrular.

Görsel üretimi, dosyalar ve istemler: [RESCUE_HUG_ART_PROMPTS.md](RESCUE_HUG_ART_PROMPTS.md). Kaynak PNG'ler korunur; dilimleme ve hizalama Unity importer metadatasıdır. Son görsel değerlendirme gerçek telefonda yapılmalıdır; bu oturumda yeni paketle fiziksel cihaz oynanış testi yapılmadı.

Son imzalı APK: `Builds/Android/Rescue-Gait-Dog-Final-20261019/Mitzi-1.0.0-1-device-test.apk` (160.279.113 bayt). Unity Android derlemesi çıkış kodu 0; imza/yükleme sertifikası eşleşmesi, ZIP 16 KB hizalaması ve dört ARM64 kütüphanenin ELF/RELRO kontrolleri geçti. Ayrıntılar paketin yanındaki `apk-verification.json` dosyasındadır. Paket uygulama kaynak commit'i `1a84326`; bu APK GitHub'a eklenmedi, canlı reklamlar kapalıdır.
