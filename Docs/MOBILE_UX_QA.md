# Build başlatma ve sevme titreşimi — 2026-10-20

## Seçilen davranış

Build/çizim hazırlığında Başlat düğmesi HUD'ın mevcut SafeArea kapsayıcısının
sağ alt köşesindedir. Normal çap 160, büyük kontrol ayarında 176 Canvas birimidir.
Sağdan 52, alttan 44 birim boşluk bırakır. Oyun 1920×1080 referanslı CanvasScaler
kullandığından bu değerler fiziksel piksel değildir. Sistem güvenli alanı iki yatay
ekran yönünde de uygulanır. Fırçalar alt ortada kalır. Başlatıldıktan sonra düğme
gizlenir ve oyun kontrollerine yer açılır. Kontrol boyutu ayarı bu düğmeyi de günceller.

Sevme ödül aralığı (0,55 saniye) değiştirilmedi. Uzun genel Handheld.Vibrate çağrısı
yerine aşağıdaki tek kısa sistem geri bildirimi kullanılır:

- Android: View.performHapticFeedback(CLOCK_TICK, 0). Sistem dokunsal ayarlarına
  uyar, ek VIBRATE izni istemez. Desteklenmeyen cihazda uzun titreşime dönüşmez.
- iOS 13+: UIImpactFeedbackStyleSoft ve 0,25 intensity; iOS 10–12: Light.
  Yerel çağrı ana iş parçacığında, uygulama aktifken çalışır.
- Tüm kediler ortak, ölçeklenmemiş zamana göre en az 1,5 saniyelik aralık kullanır.
  Parmağı kaldırmak, yeniden sevmek veya kedi değiştirmek aralığı sıfırlamaz.
  Engellenen çağrılar kuyruğa alınmaz. Sürekli sevmede mevcut ödül zamanlamasıyla
  yaklaşık 1,65 saniyede bir geri bildirim beklenir.

Milisaniye ve fiziksel motor şiddetini işletim sistemi/cihaz belirler; sabit süre
veya Android'de sayısal genlik garanti edilmez. Masaüstünde titreşim yoktur.

API kaynakları: [Android dokunsal geri bildirim](https://developer.android.com/develop/ui/views/haptics/haptic-feedback),
[Apple UIImpactFeedbackGenerator](https://developer.apple.com/documentation/uikit/uiimpactfeedbackgenerator).

## Tekrarlanabilir otomatik doğrulama

Assets/Editor/MobileUxValidation.cs yalnızca productName=MitziValidation olan
izole projede çalışır; gerçek oyuncu kayıtlarına dokunmaz.

- 1280×720, 1920×1080, 2340×1080, 2560×1600, 1024×768; normal/büyük kontroller;
  çentiksiz, sol çentik, sağ çentik ve iki yandan güvenli alan: 40 yerleşim.
- Güvenli alan içinde kalma, sağ alt erişim şeridi, diğer etkin düğmelerle
  çakışmama, gerçek UI raycast ile dokunma hedefi, merkez çizim alanı kontrolü.
- Başlat tıklamasının Gameplay'e geçmesi, çizimi kapatması ve düğmeyi gizlemesi.
- 10 saniyeye dağıtılmış 1000 titreşim isteği; beklenen 7 kabul, aralık en az
  1,5 saniye. İki kedi arasında 10 hızlı sev/bırak işlemi aralığı aşamaz.
- CompileAndroid yöntemi Android oyuncu C# kodunu ayrı çıktı klasörüne derler.
  Bu, JNI çağrılarının gerçek cihazda çalıştığını veya iOS yerel derlemesini doğrulamaz.

Sonuç: 40/40 yerleşim, dokunma/başlatma ve 1000 istekli tekrar sınırı kontrolleri
geçti (MOBILE_UX_VALIDATION_PASS). Android oyuncu C# derlemesi 14 assembly ile
geçti (MOBILE_UX_ANDROID_SCRIPTS_PASS). 1280×720 ve 2340×1080 oyun kamerası
çıktıları görsel olarak kontrol edildi; düğme alt sağda, fırça grubundan ayrı.

## Fiziksel cihaz doğrulaması — bekliyor

Erişilebilir, kararlı bir mobil cihaz bağlantısı olmadığından ergonomi ve titreşimin
hissedilen rahatlığı henüz doğrulanmadı. ADB listesinde kısa süre görünen cihaz,
model sorgusunda bağlantıyı kapattı ve sonraki sorguda bulunamadı.
Windows ortamında iOS yerel köprü derlemesi de yapılamadı.
Önceki miyav örneği için verilen hoparlör/kulaklık onayı bu testleri kapsamaz.

Güncel oyun derlemesi desteklenen Android ve iOS cihazlarda kurulunca:

1. Her iki yatay yönde normal tutuşla sağ başparmakla Başlat'a bas; çentik ve
   gezinme çubuğunu, normal/büyük kontrol ayarlarını ve fırça erişimini kontrol et.
2. Tek sevme, beş hızlı sev/bırak ve 10 saniye sürekli sevme dene; titreşimler
   kısa/hafif hissedilmeli, uzamamalı veya üst üste binmemeli.
3. Kediler arasında hızlı geçişi, arka plana alıp geri dönmeyi ve sistem dokunsal
   geri bildirim ayarını kontrol et. Desteksiz cihazda hata olmamalı.
4. Cihaz modeli, işletim sistemi, oyun sürümü ve öznel rahatlık sonucunu kaydet.
