# Kutu yüklü geçitte eğilme kontrolü — 2026-10-20

## Karar: çizerek oynamada otomatik eğilme tasarım gereği

1. bölümün ilk kutulu geçidine yaklaşınca kedinin oyuncu eğilme girdisi vermeden
eğilip geçmesi **çizerek oynama modunda beklenen davranıştır**. Doğrudan kontrol
modunda aynı engelin girişinde oyuncunun eğilmesi gerekir. Engel iki modda aynı
CrouchPassage geometrisini kullanır; giriş kontrolünün sahibi farklıdır.

Bu ayrım yalnız mevcut davranıştan çıkarılmadı: çalışma ağacındaki hata
düzeltmelerinden önceki Git `f1b7a1f` sürümünde de CatController.FixedUpdate,
`GameplayMode.IsDrawing` dalında `CrouchPassage.NeedsCrouch(this, .45f)` ile
otomatik eğilmeyi açıkça uyguluyor; UpdateDirectControl ise girdiyi bekliyor.
Önceki orman etkileşimi tasarım doğrulaması da iki modun bu ayrımını belgeliyor.
Bu görev için hareket, çarpışma veya girdi kodu değiştirilmedi.

## Beklenen kontrol ve geçiş

| Durum | Çizerek oynama | Doğrudan kontrol |
| --- | --- | --- |
| Geçide yaklaşma | Girdi olmadan uygun yükseklik/mesafede eğilir | Yaklaşmak tek başına eğilme başlatmaz |
| Giriş | Otomatik yürür ve eğilerek girer | Sağa/sola hareket ile birlikte sağ pati aşağı tutulur |
| Geçidin altı | Güvenli profil korunur, yavaş yürür | Eğilme girdisi bırakılsa da tavan altında dikilmez |
| Güvenli çıkış | Yakın geçit ve eğilme girdisi yoksa doğrulur | Girdi bırakılmış ve tavan temizlenmişse doğrulur |
| Geçit yakınında zıplama | Güvenlik alanında zıplama tüketilir, konum atlaması yapmamalıdır | Aynı tavan güvenliği uygulanır |

Doğrudan kontrol için klavyede S, aşağı ok veya Control; mobilde sağ patinin
aşağı yönü kullanılır. Çizerek oynamada açık alanda oyuncu yine aşağı girdisi
verebilir; kutulu geçidi geçmek için bu girdi zorunlu değildir.

“Otomatik eğilme” ile “eğik kalmayı güvenli biçimde sürdürme” aynı karar değildir:
doğrudan kontrolde oyuncu girişte eğilmeyi başlatır; içeride bırakınca dikilmeyi
engelleyen koruma, zorunlu bir girdi tutma veya sıkışma cezası oluşturmaz.
Çizim hazırlığı/Build sırasında kedi hareket ettirilmez; bu geçiş davranışı oyun
başladıktan sonraki hareket içindir.

## Diğer bölümlerde tutarlılık

LevelCourseBuilder.BuildCrouchChallenge aynı bileşeni bölüm numarasına göre
seçilen geçerli bir yol konumuna yerleştirir. Bölüm numarasına özel bir kontrol
istisnası yoktur. Açıklanan mod kuralı bu engelin bulunduğu tüm bölümlere uygulanır.
Orman gibi başka temalardaki alçak dal geçitleri de bu bileşeni kullanır;
ClimbableTree ise ayrı bir tırmanma etkileşimidir.

## Tekrarlanabilir doğrulama

Assets/Editor/CrouchControlRuleValidation.cs yalnızca izole MitziValidation
projesinde çalışır; gerçek oyuncunun kaydı kullanılmaz.

- Sokak varlıklarıyla 1–100. bölümler, üç zorluk, normal/büyük kedi görünümü.
- Gerçek oluşturulan geçitte girdisiz otomatik yaklaşma/altında kalma/çıkış;
  doğrudan kontrolde girdisiz giriş, girdili eğilme, içeride girdiyi bırakma,
  güvenli çıkışta doğrulma kontrolleri.
- 1, 2, 6, 7, 25, 100. bölümlerden alınan gerçek engel geometrisinin sürekli bir
  test zeminine kopyasıyla iki yönde tam fizik geçişi. Bu düzenek sonraki çukurun
  sonucunu eğilme kontrolünden ayırır. Kediler ayakta başlar; çizerek oynama
  senaryosunda hiçbir eğilme girdisi veya önceden eğik poz atanmaz.
- Aynı fizik geometrisinde doğrudan kontrol ve eğilme girdisi olmadan girişte
  durma; girdili geçişte tavanın altında bırakınca güvenli eğik kalma.
- Testler hareketin amaçlanan mod kuralını kontrol eder; gerçek telefonda tüm
  bölümün baştan sona oynanması yerine geçmez.

Sonuç: **600 yerleşim tarandı; geçidin bulunduğu 504 yerleşimde 3.528 kontrol
geçti.** Uygun konum olmadığı için geçit üretilmeyen 96 yerleşim kontrol
senaryosuna dahil edilmedi. Kolay zorlukta 1. bölümün geçidi iki boyutta da vardı.
İki modda, iki yönde **48 tam fizik geçişi** başarıyla tamamlandı; doğrudan
kontrolde eğilme girdisi olmadan **24 denemede girişte durma** doğrulandı
(`CROUCH_CONTROL_RULE_PASS`). Hareket kodunda değişiklik yapılmadı.

Görev **tasarım gereği** açıklamasıyla kapatılabilir. Harici takip aracındaki
görev durumu bu çalışma sırasında değiştirilmedi.
