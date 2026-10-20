# Dev çukur, ağaç tanıtımları ve sıradaki kedi hedefi

## Parkur kararı

- İlk beş bölümde mevcut küçük çukurlar korunur. İlk dev çukur 6. bölümde, ardından her üç bölümde bir oluşturulur (6, 9, 12, ...).
- Her uygun bölümde tek raflı çukur vardır. Bölüm ve zorluk tohumuyla rastgele 1, 3 veya 4 raf seçilir. Raflar arası merkez mesafesi 1,60–1,82 birimdir; çukur genişliği bu mesafenin (raf sayısı + 1) katıdır. Dolayısıyla tek raflı çukur daha kısa, dört raflı olan daha geniştir. Rafsız normal çukurların genişliği korunur.
- Konum bölüm numarasına göre 19,4 / 29,4 arasında değişir; yeniden denemede değişmez. Yakındaki küçük çukurlar kaldırılır, arada dar ve kazara oluşmuş zemin kalmaz.
- Raf yükseklikleri rastgele değişir. İki uçtaki raflar zeminden 0,30–0,60, iç raflar 0,30–0,95 birim yüksektedir. Komşu raflar aynı hizada tutulmaz. Raflar arasındaki yatay açıklık en az 0,45 birimdir. Üstten basılır, alttan yukarı zıplarken ve yanlardan çarpışma takılması olmaz. Raflar fiziksel olarak sabittir; bölüm tekrarlandığında sayı, aralık ve yükseklikler değişmez.
- Sokakta taş, ormanda çim/toprak, caddede asfalt/bordür, teknolojik mekanda metal zemin görseli kullanılır. Teknolojik görselin şeffaf kenarları kırpılır.
- Raflar ana yol sayılmaz: altlarına yere kadar toprak doldurulmaz, üzerlerine otomatik köpek/ağaç/tümsek yerleştirilmez. Dev çukurun tamamı bu yerleşimler için boşluk kabul edilir.
- İki oyun modu da aynı rafları kullanır. Geçiş çizim hakkı harcamadan zıplayarak yapılabilir; çizim ve bütçe kuralları değiştirilmemiştir.
- Köpekler, tek raflı olanlar dahil, raflı çukurları atlayamaz. Yalnızca normal, en fazla 2,60 birimlik dar çukurlarda mevcut atlama/geri dönüş kararını uygularlar.

## Ağaçları tanıtma

- Ayarlar → Nasıl oynanır ekranında iki ayrı animasyonlu ağaç sayfası vardır. Mavi aşağı ok, altından geçilen ve tırmanılmayan ağacı; yeşil yukarı ok, tırmanılan ağacı gösterir.
- Çizerek oynama girişinde otomatik eğilme, doğrudan kontrolde sağ pati aşağı girdisi anlatılır. Tırmanış için gövdede yukarı/zıplama ve tepede +5 ödülü gösterilir.
- Genel rehber 12 sayfadır; çizerek oynama girişi 8, doğrudan kontrol girişi 7 sayfadır. Giriş dersleri yeni sürüm anahtarıyla mevcut oyunculara bir kez tekrar gösterilir.
- Türkçe, İngilizce ve Almanca açıklamalar eklenmiştir.

## Gizli kedi hedefi

- Ev ekranında bakiyenin yanında 166×76 birimlik küçük bir rozet gösterilir: soru işaretli anonim kedi silueti, hedef bayrağı ve yalnızca bölüm numarası. Cümle kullanılmaz. Rozet bakım kartının üstüne gelmez; dokunma girdilerini yakalamaz. Kedinin kimliği, portresi ve sonraki hedeflerin listesi gösterilmez.
- Başlangıç hedefi 7. bölümdür. İlk kedi koleksiyona alındığında sıradaki hedef 20 olur. Diğer hedefler de katalogdan, sahip olunan kedilere göre sırayla hesaplanır; UI içinde sabit bir ödül listesi tutulmaz.
- Bir bölümü tamamlamış olmak tek başına hedefi ilerletmez: kedi gerçekten açılmış olmalıdır. Tüm kediler alındığında rozet gizlenir.
- Katalog sırası karışık olsa bile hem gösterilen hedef hem kurtarma servisi en erken açılmamış kilometre taşını seçer. Eski kayıtta atlanan bir kedi varsa sırada önce o vardır.
- Hedef kartı oynanış, Build ve duraklatma ekranlarında gizlenir.

## Doğrulama

İzole `MitziValidation` Unity projesinde `ProgressionAndGiantPitValidation.Run` kullanılır. Asıl oyuncu kaydı kullanılmaz. Kontrol kapsamı: dört tema, üç zorluk, 1–30. bölümler; iki hareket modu, iki boyut ve iki yönde gerçek CatController / Physics2D ile her rafa basarak geçiş; yedi kedi kilometre taşı, erken açma ve kimlik gizleme; üç dilde 18 ağaç tanıtım sayfası ve metin taşması.

Sonuçlar ve oyun kamerası görüntüleri çalışma artefaktlarındaki `progression-giant-pit` klasörüne yazılır. Fiziksel telefon üzerinde bu yeni parkurun oynanış değerlendirmesi ayrıca yapılmalıdır.

Rastgele raf güncellemesinin güncel sonucu (`varied-shelves-ghost` artefakt klasörü): 360 düzen, 108 raflı çukur ve 864 tam geçiş başarılı; 1/3/4 raf çeşitleri, farklı yükseklikler ve aynı bölüm tekrarının değişmemesi doğrulandı. Dört tema, üç zorluk, iki hareket modu, iki boyut ve iki yönden tüm raflara basılarak geçildi. Dar çukurlar ayrıca kontrol edildi: 381 normal çukur, raf sayısına göre genişliği ayarlanmış 27 raflı çukur. Aşağıdaki ilk sonuçlar önceki sabit üç raflı sürümün geçmiş kaydıdır.

2026-10-20 sonucu: `PROGRESSION_GIANT_PIT_PASS catMilestones=7 guidePages=18 layouts=360 giantPits=108 completeCrossings=96`. Sokak, orman, cadde ve teknoloji rafları oyun kamerasından incelendi. Kedi hedef kartının kontrastı ve rehberde tırmanılan ağacın kök hizası görsel kontrolde düzeltildi; son kaynaklarla doğrulama tekrar geçti.

Küçük rozet güncellemesi: `RunCompactGoalAndGapRules` 1280×720, 2340×1080 ve 1600×1200 ekranlarında bakiye/bakım kartıyla çakışma olmadığını, metnin yalnız numara olduğunu, 7 → 20 hedefinin kedi alındıktan sonra değiştiğini ve biten koleksiyonda rozetin gizlendiğini doğruladı. Çukur kuralı ayrıca üç zorlukta 1–30. bölümlerde kontrol edildi: 382 normal çukur ve her birinde üç raf bulunan 27 geniş çukur; rafsız geniş çukur yok. Çukur genişlik kodu bu görsel güncellemede değiştirilmedi.
