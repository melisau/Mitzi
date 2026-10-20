# Son karar: ortak zıplama/tırmanma, güvenli yerleşim, yüzey hizası — 2026-10-20

Kullanıcı son mesajında ayrı tırmanma tuşu istemediğini netleştirdi: **zıplama
tuşu gövdede tırmandırmalı; ağaç ve çukur yan yana yerleştirilmemeli**. Önceki
E veya yalnız ↑ kullanma denemesi bu kararla geçersizdir.

## Tırmanma ve çukur yaklaşımı

- Mobil ZIPLA/sağ pati yukarı, klavyede ↑/W/Space gövde menzilinde ve yerdeyken
  tırmanma başlatır. Sadece başarılı tırmanışta zıplama isteği tüketilir.
- Menzil dışında aynı girdiler zıplama olarak CatController'a kalır. Havada,
  Build'de ve duraklatmada kendiliğinden tırmanma başlatılmaz.
- Ağacın gövde trigger'ı ve en büyük kedi yaklaşma payının **dışından itibaren**
  her çukur kenarına en az **3,5 dünya birimi** boş zemin ayrılır. Bu yalnız
  ağacın merkezinden ölçülen bir mesafe değildir. Uygulama hem yolun başına hem
  sonuna aynı payı uygular.
- Alçak geçitlerden 12 birimlik eski güvenlik ayrımı korunur. Bu koşulları
  karşılayan yeterince uzun zemin yoksa ağaç o bölüm yerleşiminde üretilmez;
  çukur yanına zorla yerleştirilmez. Ağaçsız yerleşimde köpek mesajı tırmanma önermez.
- Yeşil işarete dokunarak tırmanmak yardımcı seçenek olarak kalır; zorunlu değildir.
  TR/EN/DE ders metni ve animasyon ortak zıplama tuşunu gösterir. +5 tepe ödülü sürer.

## Havada görünen eğilme ağacı ve zemin

Yeni ekran görüntüsündeki ağaç, ClimbableTree değil, “Altından geç” yazılı
orman CrouchPassage görselidir. İnce platforma kaynak olan 500×500
forest_ground_underfill.png'nin ilk 18 pikselinde şeffaf/seyrek çim payı vardır.
Eski ince platform kesiti bu payı da içerdiği için görünen çim collider'dan
yaklaşık 0,18 birim aşağıda başlıyor; collider'a hizalı kedi/ağaç kökleri ayrık
görünüyordu.

Orman ince zemin sprite kesitinde bu %3,6 üst pay hariç tutuldu. Görsel çim
yüzeyi artık fiziksel yürüyüş yüzeyine oturur. Geometri, çukur genişlikleri,
yürüyüş yüksekliği ve eğilme açıklığı değiştirilmez. Bu aynı düzeltmeyle hem
eğilme ağacının kökleri hem tırmanma ağacı hem kedinin patileri görünür zemine
hizalanır. Sokak/Cadde/Neo kaynak kesitlerine bu orman payı uygulanmaz.

## Koyu Abyss sınırı

Katman derinlik amacıyla korunur. Eski üst satır %62 opak olduğu ve collider'ın
yalnız 0,02 birim altında başladığı için çimin şeffaf üst payında düz koyu bir
şerit görünüyordu. Üst sınır yüzeyden 0,28 birim aşağı alındı; üst alfa 0'dan
başlar ve SmoothStep ile aşağıda 0,98'e ulaşır. Böylece yüzey üzerinde keskin
dikdörtgen sınır oluşmaz. Alt sınır -8, arka/yol sırası ve collider olmaması korunur.

İki eylem etiketi de panelsiz açık yazı ve küçük gölge kullanır. “Altından geç”
yazısının arka planı geri eklenmez.

## Doğrulama

TreeJumpSeparationValidation güncel kurala göre yeniden yazıldı. İzole
MitziValidation projesinde 1–100. bölümler, üç zorluk, normal/büyük görünüm taranır.
Her ağacın tüm çukurlara güvenli mesafesi ölçülür. İki oyun modunda çukurun iki
kenarında gerçek zemin yüksekliğinden zıplama; gövdede mobil ve klavye isteğiyle
tırmanma doğrulanır. Düşük tavan güvenlik bölgesindeki zıplamalar ayrı eğilme
kuralı olduğundan çukur testinden ayrılır. Orman kesitinin 18px payı dışladığı,
Abyss üst satırının tam şeffaf ve üst sınırın yüzey altında olduğu kontrol edilir.

Ödül/alt katman ve orman etkileşim testleri de güncel kuralla çalıştırılır. Kamera
çıktıları gerçek oyun ölçeğinde alınır. Bu otomatik testler fiziksel telefonda
tam oynanışın ve ergonominin doğrulandığı anlamına gelmez.

Sonuç: 600 yerleşimde 249 güvenli ağaç, 1.116 ağaç/çukur mesafesi, gerçek yerel
zemin yüksekliğinden 4.280 çukur kenarı zıplaması ve 996 gövdede tırmanma girdisi
kontrolü geçti (`TREE_GAP_FINAL_RULE_PASS`). İki görünüm boyutu ve üç zorluk
kapsandı. Uygun boşluk bulunmayan yerlerde ağaç atlandı. Güncel ödül/alt katman
doğrulaması 6 ağaç ve 36 tema/bölüm/ekran görünümüyle; etkileşim doğrulaması ise
120 yerleşim, 162 tırmanış, 66 ağaçsız bölüm önerisi ve üç dilde ders kontrolüyle
geçti. Güncel orman kamera görüntüsünde çim/kök temasının ve üstte keskin siyah
sınırın giderildiği görsel olarak kontrol edildi.
