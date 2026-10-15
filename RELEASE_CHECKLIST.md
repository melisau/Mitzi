# Mitzi — Google Play yayın listesi

Bu liste, kodda yapılamayan ve Unity Editor / Play Console'da elle yapılması gereken adımları sırayla toplar. Oynanış testleri için [QA_CHECKLIST.md](QA_CHECKLIST.md) dosyasına bak.

## 1. Unity sürümü (zorunlu)

Google Play, 31 Ağustos 2026'dan beri yeni uygulama ve güncellemelerde **Android 16 (API 36)** hedefi istiyor. Unity 2022.3'te API 36 hedeflemek için **2022.3.62f1 veya daha yeni** bir 2022.3 LTS sürümü gerekir.

1. Unity Hub → Installs → en yeni **2022.3 LTS** sürümünü **Android Build Support** (OpenJDK + Android SDK & NDK) modülüyle kur.
2. Projeyi bu sürümle aç (Hub'da sürümü değiştir). Yükseltme uyarısını onayla.
3. Edit → Preferences → External Tools'ta SDK/NDK/JDK'nın "Installed with Unity" olarak işaretli olduğunu kontrol et. Derlemede `build-tools` hatası alırsan Android Studio SDK Manager'dan **Build-Tools 36** paketini kur.

## 2. Player Settings (repoda ayarlandı — kontrol et)

Project Settings → Player → Android:

| Ayar | Değer |
| --- | --- |
| Scripting Backend | IL2CPP |
| Target Architectures | ARMv7 + **ARM64** |
| Minimum API Level | 23 |
| Target API Level | 36 |
| Package Name | `com.mitzicat.game` (Android ve iOS) — **Play'e ilk yüklemeden sonra değiştirilemez.** "Override Default Package Name" açık olmalı; kapalıysa Unity adı Company Name'den türetir. |
| Version / Bundle Version Code | Her yüklemede Bundle Version Code'u 1 artır. |

## 3. Elle yapılacaklar

- [ ] **İkon:** 1024×1024 PNG hazırla → Player → Icon → Android'de Adaptive (ön plan + arka plan) ve Legacy ikonları ata.
- [ ] **İmza anahtarı:** Player → Publishing Settings → Keystore Manager → yeni keystore oluştur. Dosyayı ve şifreleri **repoya koymadan** güvenli bir yerde sakla (kaybolursa güncelleme yayınlanamaz; Play App Signing açık olsa bile yükleme anahtarı gerekir).
- [ ] **Splash:** Unity Personal'da Unity logosu zorunludur; istersen kendi logonu ekleyebilirsin.
- [ ] **Derleme:** File → Build Settings → Android → **Build App Bundle (Google Play)** işaretli → Build. Çıktı `.aab` olmalı.
- [ ] Önce **Development Build** ile gerçek telefonda QA listesini çalıştır, ardından işaretsiz release derlemesini al.

## 4. Play Console

- [ ] Geliştirici hesabı (tek seferlik 25 USD). **Kişisel hesaplarda** üretime çıkmadan önce kapalı testte belirli sayıda test kullanıcısının belirli bir süre test etmesi gerekiyor; güncel koşulu Play Console'da kontrol et.
- [ ] Mağaza girişi (TR + EN): kısa açıklama (80 karakter), uzun açıklama, 512×512 ikon, 1024×500 öne çıkan görsel, en az 2 telefon ekran görüntüsü (yatay).
- [ ] Gizlilik politikası URL'si.
- [ ] İçerik derecelendirme anketi, hedef kitle ve içerik, Data Safety formu, reklam beyanı ("Uygulama reklam içeriyor").

## 5. Oyun içi dil

Oyun ilk açılışta cihaz dili Türkçeyse Türkçe, değilse İngilizce başlar. Ayarlar → Dil'den değiştirilebilir; değişince sahne yeniden yüklenir. Yeni metin eklerken `GameText.L("Türkçe", "English")` kullan.
