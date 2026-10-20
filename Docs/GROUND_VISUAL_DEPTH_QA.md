# Zeminin havada platform gibi görünmesini giderme — 2026-10-20

Kullanıcının 12. bölüm Orman ekranında gördüğü alt boşluk, yürüyüş yüksekliğinin
değişmesinden değil, 1,25 birimlik ince platform görselinin arka plan üzerinde
sonlanmasından kaynaklanıyordu. Son istek doğrultusunda Sokak ve Orman zeminleri
artık havada asılı kısa levhalar yerine ekranın altına uzanan toprak kütleleridir.

## Değişiklik

- Mevcut çim/taş yüzeyinin konumu ve orman şeffaf-pay düzeltmesi korunur.
- Her gerçek yol parçasının altına kendi yatay sınırları içinde, en az y=-8'e
  uzanan RoadSoilColumn eklenir. Kaynak dokuda üst yüzeyle örtüşmenin başladığı
  satırdan aşağısı kullanılır; dikey karo tam sayıya tamamlanarak üst satırın
  kayması ve belirgin birleşim çizgisi önlenir. Çim şapka dolguya kopyalanmaz.
- Görsel toprak üst yüzeyin alt kesitiyle örtüşür; arada açık bir çizgi bırakmaz.
  Çizim sırası 0, üst yüzey 1, Abyss -1'dir. Koyu düz panel yerine mevcut toprak
  dokusu kullanılır. İnce levhanın altındaki eski elips gölgesi kaldırılır.
- Yeni dolgunun collider'ı yoktur. Yürüyüş yüzeyi, çukur konum/genişliği,
  zıplama, ağaç yerleşimi ve çizim bütçesi değiştirilmez.
- Her kolon yalnız ait olduğu platformun genişliğindedir; çukurun içine veya
  iki platform arasına dolgu yapılmaz. Cadde/Neo mevcut zemin sistemini kullanır.

## Doğrulama

Assets/Editor/GroundVisualDepthValidation.cs yalnız izole MitziValidation
projesinde çalışır. Sokak/Orman; 1, 12, 21. bölümler; 16:9, 19,5:9, 4:3
kameralar kontrol edilir. Yol ve kolon yatay sınırları bire bir karşılaştırılır;
alt sınırın kamera altında olduğu, yürüyüş collider'ının değişmediği ve boşlukların
kapatılmadığı doğrulanır. 12. bölüm görüntüsü bitiş tarafındaki oyun kamerasından
alınır; bu kullanıcının bildirdiği ekran konumuyla eşleşir.

Sonuç: 18 yerleşim/kamera senaryosu, 126 toprak kolonu ve 78 açık çukur kontrolü
geçti (`GROUND_VISUAL_DEPTH_PASS`). Sokak/Orman 12. bölüm bitiş görüntülerinde
toprak dokusunun ekran altına kadar sürdüğü, yüzey birleşiminde açık çizgi olmadığı
ve çukurların görsel olarak açık kaldığı kontrol edildi. Collider yüksekliği
ve boyutu aynı kaldı.
