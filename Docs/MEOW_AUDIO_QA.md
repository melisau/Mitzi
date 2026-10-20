# Miyav ses dengesi — 20 Ekim 2026

Dosya seviyesi farkı doğrulandı: üç kısa miyav aynı `Sfx` kazancı (0,8) ile
çalınıyordu; `PlayOneShot` peş peşe gelen miyavları ayrıca üst üste bindirebiliyordu.
Kaynak dosyalar arasındaki ölçülen fark 14,51 LU idi.

| Dosya | Önce LUFS-I | Önce dBTP | Sonra LUFS-I | Sonra dBTP |
| --- | ---: | ---: | ---: | ---: |
| cat_meow2.wav | -19,04 | -10,98 | -21,99 | -13,94 |
| cat_meow3.wav | -16,31 | -4,63 | -22,00 | -13,33 |
| cat_meow4.wav | -4,53 | +0,14 | -22,02 | -17,33 |

Önceki üçüncü dosya monoydu; diğer ikisi stereoydu. Sonuçların hepsi 48 kHz,
stereo, 16 bit PCM olarak aynı kanal düzeninde ölçüldü. Süreler korundu
(yeniden örneklemede en fazla bir 48 kHz örneği fark). Varyantlar arasındaki
son fark 0,03 LU. Hedef -22 LUFS-I; izin verilen true peak tavanı -6 dBTP.
Yalnız sabit kazanç ve gerekli örnekleme/kanal dönüşümü uygulandı; sıkıştırıcı,
limiter, EQ, hız/perde değişimi ve kırpma kullanılmadı.

Unity içe aktarma: PCM, örnekleme hızını koru, Force To Mono kapalı, Normalize
kapalı. Unity'den `AudioClip.GetData` ile dışarı alınan örneklerde LUFS ve dBTP
değerleri yukarıdaki sonuçlarla eşleşti. Normalize seçeneği Unity'de Force To
Mono işlemiyle ilgilidir; algılanan ses yüksekliği eşitlemesi yerine geçmez.
[Unity 2022.3 Audio Clip belgesi](https://docs.unity3d.com/2022.3/Documentation/Manual/class-AudioClip.html).

`CozyAudioManager` miyavları 0,8 kazançlı, iki boyutlu, sabit perdeli ayrı bir
AudioSource üzerinden tek ses olarak çalar. Devam eden miyav ve bitişinden
sonraki 120 ms sırasında gelen ek istekler atlanır; kuyruk oluşturulmaz.
Diğer efektler kesilmez. Sessize alma, tekrar açma ve nesnenin devre dışı
bırakılması bu kanalı da kapsar. Üç saniyeden uzun kayıtlar hâlâ reddedilir.

## Otomatik doğrulama

- `python tools/normalize_meows.py`: kaynak seviye, true peak, ortak kanal/hız
  ve süre denetimi. Dosyaları değiştirmek için açıkça `--apply` gerekir.
- Ölçüm: FFmpeg `loudnorm` EBU R128/LUFS-I ve true peak analizi.
  [FFmpeg filtresi](https://ffmpeg.org/ffmpeg-filters.html#loudnorm).
- Yalnız izole `MitziValidation` kopyasında `MeowAudioValidation.Run`: üç
  içe aktarılmış klip, 300 hızlı çağrı, duraklatılmış arayüzde tek ses koruması,
  efekt sessizi, tekrar açma, devre dışı bırakma ve uzun kayıt yedeği geçti.
- Miyav + ev müziği + mırıldama + arayüz sesi ve miyav + orman müziği +
  arayüz/adım sesi için farklı başlangıç kaymalarıyla 72 çevrimdışı karışım
  kontrol edildi. Dört kat örneklenmiş en yüksek karışım tepesi -3,64 dBFS;
  bu örneklerde taşma yok. Bu kontrol gerçek cihazın son çıkış kaydı değildir.

Yeni dosyalarda tam ölçeğe ulaşan örnek yoktur. `cat_meow4.wav` özgün kaydında
54 tam ölçek örneği vardı. Kazancı azaltmak önceden kaybolmuş dalga biçimini
geri getirmez; kalan işitsel bozulma varsa cihazda dinleme sırasında saptanmalıdır.

## Gerçek cihaz doğrulaması

20 Ekim 2026: Kullanıcı `meows-balanced.wav` örneğini gerçek cihaz hoparlörü
ve kulaklıkla aynı ses ayarında dinleyerek **"İkisinde de dengeli ve temiz"**
sonucunu bildirdi. Örnek dosyanın iki çıkışta algılanan seviye/bozulma kontrolü
onaylandı. Cihaz modeli ve kulaklık türü bildirilmedi.

ADB bağlı cihaz listesi boştu. Bu onay güncel oyun derlemesinin cihaz üzerinde
denendiği anlamına gelmez; aşağıdaki oyun içi eş zamanlı olay ve sessiz denemeleri
henüz cihaz üzerinde doğrulanmadı.

1. Aynı telefon ses ayarında üç miyavı sırayla en az iki kez dinle; varyant
   geçişlerinde belirgin sıçrama ve çatlama/cızırtı olmadığını değerlendir.
2. Telefon hoparlörü ve kulaklık için ayrı ayrı tekrarla; oynatıcıdaki otomatik
   ses eşitlemesini/normalleştirmeyi kapat veya ayarını kayda geçir.
3. Güncel oyun derlemesinde kedi seçimi ve ihtiyaç bildirimini art arda tetikle;
   miyavlar üst üste binmemeli, diğer efektler kesilmemeli.
4. Müzik ve mırıldama açıkken dinle; efekt sessizi kapat/aç durumunu kontrol et.
5. Cihaz/model, işletim sistemi, derleme, hoparlör/kulaklık türü, ses ayarı ve
   her iki dinlemenin sonucunu kaydet. Gerekirse varyantı belirt.
