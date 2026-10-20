# Ağaç tepesi ödülü ve parkurun alt katmanı — 2026-10-20

> Sonraki ekran görüntüsüyle yüzey üstündeki koyu şerit doğrulandı ve düzeltildi:
> FOREST_SURFACE_TREE_SPACING_QA.md güncel karardır. Aşağıdaki ilk Abyss inceleme
> değerleri tarihsel kayıttır; güncel üst alfa 0, üst sınır yüzeyin 0,28 birim altıdır.

## Ağaç ödülü ve etiket

Her yeni üretilen tırmanılabilir ağaç, tepe noktasında para simgesi ve **+5** taşır.
Rozetin arkasında dolu bir panel yoktur; hafif altın parıltı ve gölgeli açık yazı
kullanılır. Ödül, kedinin tırmanışı tamamlayıp o ağacın tepesine oturduğu ilk aktif
oyun güncellemesinde verilir. Gövdeye dokunmak, yukarı girdisini vermek veya
tırmanışın ortasında olmak yeterli değildir. Ödül toplandığında simge kaybolur ve
mevcut alışveriş puanı hesabına +5 eklenir; HUD mevcut puan değişimi olayıyla yenilenir.

Aynı ağaca tekrar çıkmak, beklemek veya ağacı kapatıp açmak yeniden puan vermez.
Yeni bölüm/yeniden deneme parkuru yeniden oluşturduğunda, kuş ödüllerinde olduğu
gibi yeni denemenin ağacında yeni bir ödül bulunur. Eğilme ağacına ödül eklenmez.
Build ve duraklatma durumlarında puan yazılmaz. Tırmanış sırasında gövde temasının
bitmesi, tepeye varış ödülünü engellemez.

**Tırman** yazısının koyu hap biçimindeki arka planı kaldırıldı. Açık renkli yazı,
küçük koyu metin gölgesi ve yeşil yukarı ok kaldı. Sonraki kullanıcı isteğiyle
mavi “Altından geç” işaretinin paneli de kaldırılıp aynı yazı gölgesi uygulandı.
Tırmanma dersinin TR/EN/DE metninde de tepeye ulaşmanın +5 puan kazandırdığı
belirtilir; metin sığma kontrolleri tekrar çalıştırılır.

## Parkurun altında görünen koyu katman: karar ve işlev

İncelenen görünümün ana kaynağı LevelCourseBuilder.CreateAbyss tarafından her
parkur kurulumunda üretilen **GeneratedCourse/Abyss** nesnesidir. Kaynak kodundaki
açıklama, ince platformun altının sıcak bir karanlığa dönüşmesini ve çukurların
“düşülecek yer” olarak okunmasını amaçladığını açıkça belirtir. Bu nedenle
görünür olması **tasarım gereği** olarak belgelendi; katman kaldırılmadı ve
parkurun alt görünümünü değiştiren bir düzeltme yapılmadı.

- Kullanım: yeni terracotta yol içeren Sokak ve forest_ground_underfill içeren
  Orman. Cadde ve Neo Teknoloji aynı Abyss nesnesini kullanmaz.
- Görsel: diskten yüklenen eksik bir doku değildir. Çalışma anında üretilen
  2×128 RGBA gradyan, RGB (0,10; 0,067; 0,047), yani siyaha yakın sıcak kahverengi.
  Alfa üstte 0,62, aşağıda 0,98'e yükselir; arka plan kısmen görünmeye devam eder.
- Konum: y=-8'den yürünebilir yüzeyin 0,02 birim altına kadar uzanır. Normal
  ortografik kameranın alt sınırının altında biter; kamera içinde biten bir panel
  değildir. Yol parçalarının altını ve aralarındaki çukurları koyulaştırır.
- Çizim sırası: arka plan -5, Abyss -1, platform görseli 1; platformları örtmez.
- Fizik: Collider2D ve Rigidbody2D içermez. Düşme/kurtarma kararını bu görsel
  vermez; CatController bölümün fallY sınırını ayrı kontrol eder.

Yakındaki diğer koyu görseller aynı şey değildir: **RoadSlabShadow** ince yolun
altındaki siyah, %55 alfa elips gölgesidir (sıra 0). **RoadUnderfill** ve
**CityRoadUnderfill**, tema ve yol varlığına göre platformların alt kesitini
doldurur; gerekli durumda tanımlanmış düz renk dolgusu kullanılır. Bunların da
amacı ve sırası kodda belirgindir; incelemede eksik Sprite veya yanlış sıra tespit
edilmedikçe silinmezler.

## Doğrulama yöntemi

Assets/Editor/TreeRewardAndUnderlayValidation.cs yalnız MitziValidation adlı
izole projede çalışır. Ödülün mevcut ekonomi yazım yolunu kullanabilmesi için
izole kayıtta puan artışını doğrular; test sonunda puan ve birincil/yedek kayıt
anahtarları eski hâline döndürülür. Gerçek oyuncu kaydı kullanılmaz.

- İki oyun modu, üç zorluk: ilk tırmanışta +5; Build, yarım tırmanış, duraklatma,
  tekrar tırmanış ve kapat/aç ile fazladan puan verilmemesi.
- Her ağaçta 100 ek güncelleme; ödülün bir kez kaybolması ve tek HUD olayı.
- Dört tema × bölüm 1/10/21 × 16:9, 19,5:9, 4:3: alt katmanın varlığı, gradyanı,
  fizik bileşeni olmaması, kamera sınırları, yol/arka plan sırası ve eksik sprite.
- Oyun kamerasından ağaç ödülü ve her temanın alt parkur görünümü kaydedilir.
  Orman için Abyss geçici kapatılarak yalnız doğrulama görüntüsü alınır, hemen
  yeniden açılır; üretim kodundaki davranış değiştirilmez.

Sonuç: altı ağaçta toplam +30 puan doğru ödendi; 600 ek güncelleme ve tekrar
tırmanma/etkinleştirme ile ilave puan verilmedi. 36 tema/bölüm/ekran oranı
yerleşimi, bunların 18'inde Abyss kontrolleri geçti
(`TREE_REWARD_UNDERLAY_PASS`). Dört temanın kamera çıktıları ve katmansız Orman
karşılaştırması incelendi. Son değişikliklerden sonra önceki etkileşim doğrulaması
da tekrar geçti: 120 yerleşim, 270 tırmanış, 30 ağaçsız mesaj ve üç dilde ders
kontrolü (`FOREST_INTERACTION_UX_PASS`).

Bu belge kaynak kodu ve otomatik sahne/kamera doğrulamasını kapsar; gerçek
telefonda yeni ödülün oynanarak toplanması ayrıca doğrulanmalıdır.
