# Mitzi oyun tasarımı ve yayın planı

**Proje:** Mitzi — kod içindeki adı Paw Path  
**Belge tarihi:** 15 Ekim 2026  
**Belgenin amacı:** Oyunun amacını, mevcut özelliklerini, teknik yapısını, tamamlanan çalışmaları ve yayın öncesi yapılacak işleri birlikte tanımlamak.

Mitzi, evde kedi bakımı ve dekorasyonu ile yandan görünüşlü platform bölümlerini birleştiren mobil bir oyundur. Oyuncu kedileriyle ilgilenir, bir kediyi seçerek yolculuğa çıkar, engelleri aşar ve kazandığı puanlarla evini geliştirir. Çizerek yol oluşturma ve doğrudan tuşlarla oynama, aynı oyuna iki farklı yaklaşım sunar.

Mevcut proje oynanabilir sistemler içeriyor. Genel yayına geçiş için öncelik; mobil yayın araçlarını tamamlamak, çarpışma ve görsel hizalamayı sağlamlaştırmak, gerçek cihazlarda oynanışı doğrulamak ve ilk kullanım deneyimini anlaşılır hale getirmektir. Yeni içerik önerileri bu zorunlu çalışmalardan sonra ele alınmalıdır.

## 1 Oyunun amacı ve oyuncuya sunduğu deneyim

### Temel amaç

Oyuncunun amacı bölümlerdeki çıkışa ulaşmak, yeni kediler kazanmak ve kedilerin yaşadığı evi geliştirmektir. Ev bölümü ilerleme arasında bir bakım ve dinlenme alanı oluşturur. Platform bölümü ise hareket, zamanlama ve çizim yoluyla problem çözme sunar.

Oyunun duygusal odağı kedilerle bağ kurmaktır. Kedinin seçilebilir olması, bakım değerlerinin ayrı tutulması, ev eşyalarıyla etkileşim ve yeni kedilerin kurtarılması bu odağı destekler. Bu nedenle kedilerin görünüşü, hareketi ve eşyalarla doğru temas etmesi doğrudan oyun kalitesini etkiler.

### Hedef deneyim

Kedi seven ve kısa mobil oturumlarda ilerlemek isteyen oyuncular için sıcak, anlaşılır bir deneyim hedeflenir. Kolay mod yeni oyuncuya ulaşılabilir bir başlangıç sunmalıdır. Orta ve zor modlar daha hızlı ve daha fazla köpekle beceri isteyen bir seçenek oluşturur. Hedef yaş aralığı ve mağaza yaş derecelendirmesi yayın hazırlığında kesinleştirilmelidir.

### Ana oyun döngüsü

1. Oyuncu evde kedisini seçer ve bakım durumuna bakar.
2. Mama, su ve sevgi değerlerini gerekli düzeye getirir.
3. Oynama biçimini ve mekan temasını seçer.
4. Çizim modunda yolu hazırlar; tuşlu modda doğrudan hareket eder.
5. Çukurları ve engelleri aşar, kuş ödüllerini toplar ve çıkışa ulaşır.
6. Bölüm ödülü kazanır; uygun ilerleme eşiklerinde yeni bir kedi açılır.
7. Eve döner, puanlarını dükkanda harcar ve kedileriyle etkileşir.

## 2 Oyuncu akışı ve ekranlar

### Ev ekranı

Ev, oyunun ana merkezidir. Seçili kedi, bakım kontrolleri, oyun seçimi, dükkan, ev düzenleme, ayarlar, mevcut dükkan bakiyesi ve çıkış işlemleri bu merkezden erişilir. Kediler evin içinde hareket eder. Bir kediye dokunup onu seçtikten sonra zemine veya etkileşimli bir eşyaya dokunmak, o kediyi hedefe yönlendirir.

Ev eşyaları kedilerin yürüyüşünü fiziksel olarak kapatmamalıdır. Kedilerin hedefe ulaşması, yemek kabının önünde doğru noktaya gelmesi ve kedi ağacının merkezinde etkileşime başlaması temel kullanılabilirlik koşullarıdır. Eşyaların önünden veya arkasından görsel geçiş ayrıca tutarlı sıralama gerektirir.

### Oyun seçimi

Oyuncu çizerek veya tuşlarla oynama seçeneğini açar; ardından Sokak, Orman, Cadde veya Neo Teknoloji temasını seçer. Zorluk ve görüntü boyutu tercihleri ayrı ayarlardır. Bu tercihlerin yeniden açılışta korunması yerel ayarlar üzerinden sağlanır.

### Bölüm sonu

Başarı ekranı bölüm ilerlemesini ve ödülü gösterir. `complete_screen.jpg` başarı arka planı olarak kullanılır; kedi görselleri ekranın parçası olmaya devam eder. Başarısızlık ekranında `failed_screen_cat.jpg` kullanılır. Başarısızlıkta yeniden deneme ve eve dönüş akışları bulunur. Uygun bölüm eşiklerinde kedi kurtarma ekranı açılır.

## 3 Oynama biçimleri

### Çizerek oynama

Kedi çizim hazırlığı sırasında hareket etmez. Oyuncu normal yol, zıplatan yol ve buz yüzeyi araçlarını kullanarak fiziksel yollar oluşturur; silgiyle çizgileri kaldırabilir. Eski kırmızı tehlike çizgisi oyuncunun mevcut çizim akışından çıkarılmıştır.

Oyun başladıktan sonra kedi alçak kutulu geçitlere yaklaşınca otomatik eğilip altından geçer; çizerek oynama modunda eğilme girdisi zorunlu değildir. Doğrudan kontrol modunda aynı geçide oyuncu eğilme girdisiyle girer; girdiyi tavan altında bırakmak kediyi aniden dikleştirmez, güvenli çıkışa kadar eğik profil korunur. Bu mod ayrımı ve diğer bölümlerdeki doğrulama `CROUCH_CONTROL_RULES_QA.md` içinde belgelenmiştir.

Bölümün tamamını önceden görebilmek için önceki ve sonraki çizim görünümüne geçiş bulunur. Bunlar aynı bölümün kamera pencereleridir; yeni sahne veya yeni bütçe değildir. İlk `floor(görünüm sayısı / 2)` pencere birlikte bölüm bütçesinin en fazla yarısını harcayabilir; sonraki pencerelerde toplam kalan kullanılabilir. Bu nedenle dört/beş pencereli akışta ilk pencerede bu yarı tüketilirse ikinci pencerede çizilemez, üçüncüde ayrılmış kalan yarı açılır. Numaralı yeni bölümde ve yeniden denemede yollar temizlenip tam bütçe atanır. Ayrıntılı kural ve 1 → 2 → 3 doğrulaması: `DRAWING_BUDGET_RULES_QA.md`.

Fırça tamamen tükenince oyun başlar. Oyuncu bütçeyi tüketmek istemezse Oyunu Başlat düğmesiyle erken başlayabilir. Başlangıçta çizim kapatılır, palet ve mürekkep göstergesi gizlenir, ZIPLA kontrolü görünür. Kedi otomatik ileri hareketini sürdürürken oyuncu zıplayarak köpekten kaçınabilir. Çift zıplama mantığı ortak hareket kodunu kullanır.

Köpeğin üstüne aşağı doğru inerken temas etmek köpeği puf efektiyle ortadan kaldırır ve kediyi hafifçe sektirir. Köpeğe yandan yürüyerek temas etmek başarısızlıktır. Bu kural çizim ve tuşlu modlarda ortaktır. Bu davranış kodda uygulanmıştır; mobil dokunma zamanlaması cihazda sınanmalıdır.

### Tuşlarla oynama

Oyuncu sol, sağ, ZIPLA ve EĞİL tuşlarıyla kediyi kontrol eder. Klavyede A ve D veya yön tuşları hareket; Space, W ve yukarı tuşu zıplama; S, aşağı tuşu ve Control eğilme için kullanılır. Mobil tuşlar büyütülmüş ve aralarında boşluk bırakılmıştır.

Zıplama, ikinci zıplama, eğilme, zemin kontrolü ve animasyon durumları `CatController` üzerinden yönetilir. Ekrandaki kedi boyutu değişse de görsel ayak noktası ile fiziksel temas noktasının aynı yüzeye oturması gerekir.

## 4 Bölümler ve mekanlar

### Temalar

| Tema | Görsel yaklaşım | İçerik |
| --- | --- | --- |
| Sokak | Sıcak ev cepheleri ve taşlı toprak zemin | Çukur, taş tümsek, kuş, koşan köpek, bazı bölümlerde ağaç |
| Orman | Doğal toprak, kök ve yosun dokusu | Orman dolgusu, yosunlu tümsek, çukur, kuş, köpek ve ağaç |
| Cadde | Kent zemini ve cadde dekorları | Cadde alt dolgusu, temaya uygun engeller ve çukur dekorları |
| Neo Teknoloji | Teknolojik arka plan ve neon vurgular | Robotik köpek, robotik kuş ve dört teknolojik engel görseli |

Neo temasında `cyber_neo.jpg`, `cyber_ground_tile.png`, robotik köpek yürüyüş kareleri, `robotic_bird.png` ve `neo_techno_object` serisi kullanılır. Neon vurgular engellerin atlanması gerektiğini anlatmayı amaçlar. Mevcut kurulumda Neo temasına tırmanılabilir ağaç verilmez.

### Bölüm üretimi

İlk beş bölüm başlangıç düzenlerini kullanır. Sonraki bölümlerde çukur ve tümsek yerleşimi bölüm numarasına bağlı deterministik üretimle çeşitlenir. Aynı bölüm yeniden oynandığında temel düzenin korunması beklenir. Bölümler önceki kısa yapıya göre uzatılmıştır.

Deterministik üretim rastgele görünüm sağlar; tek başına bütün bölümlerin geçilebilir olduğunu kanıtlamaz. Çukur genişliği, iniş alanı, köpek yaklaşımı, çizim bütçesi ve engellerin birlikte oluşturduğu zorluk ayrıca kontrol edilmelidir. Geometri kontrolü 1–100. bölümler için mevcut olsa da gerçek oynanış kontrolü farklı bir iştir.

### Zorluk

| Ayar | Mevcut köpek davranışı | Tasarım hedefi |
| --- | --- | --- |
| Kolay | Bir köpek ve daha düşük hız hedeflenir | İlk kez oynayanın öğrenebilmesi |
| Orta | Yerleşime göre iki köpeğe kadar ve daha yüksek hız | Temel kontrolleri öğrenmiş oyuncu |
| Zor | Yerleşime göre üç köpeğe kadar ve en yüksek hız | Daha sık zamanlama ve kaçınma gereksinimi |

Köpek sayısı geçerli doğma konumu bulunmasına bağlıdır; kodda yedek doğma mantığı vardır. Zorluk seçiminin etkisi esas olarak köpek hızı ve sayısı üzerindedir. Çukurların, bakım koşullarının ve bütün diğer mekaniklerin ayrı ayrı kolaylaştırıldığı varsayılmamalıdır.

## 5 Engeller ödüller ve tırmanma

### Zemin ve çukurlar

Zemin görseli, alt dolgu ve fiziksel collider ayrı sorumluluklar taşır. Çukur gerçek yürünebilir yüzeyin kesildiği boşluktur. Dekoratif çukur kenarı veya konteynır kendiliğinden yürünebilir yüzey değildir. Tümsekler platform sınırları içinde kalmalı, collider görünen tepeyle eşleşmelidir.

Sokakta yeni terracotta yol, ormanda `forest_ground_underfill`, caddede `city_road_underfill` yaklaşımı kullanılır. Zemin köşeleri çalışma anında oluşturulan maskelerle hafifçe yumuşatılır. Önceki üst üste binme sorunları nedeniyle çukur kenarlarında farklı denemeler yapılmış, istenmeyen denemeler geri alınmıştır. Asset eklenmesi tek başına onun aktif oyunda kullanıldığı anlamına gelmez.

### Kuşlar

Kuşlar zarar veren engel yerine toplanabilir ödüldür. Kedi kuşa dokunduğunda ortak dükkan bakiyesine 3 puan eklenir. Kuşlar X ekseninde ilerler, küçük bir dikey salınım yapar ve alan dışına çıkınca kaldırılır. Aynı kuşu sürekli aynı noktada yeniden doğurma döngüsü kullanılmaz. Ödül göstergesinin küçük ekranlarda okunması test edilmelidir.

### Köpekler

Normal köpekte 14 koşu karesi, robotik köpekte 9 yürüme karesi için içerik hazırlanmıştır. Köpek yakınından geçen kediyi algılayıp uygun koşulda iki saniye bekleyerek yön değiştirir. Çukura geldiğinde bazı köpekler atlayabilir; diğerleri kenarda bekleyip geri döner. Köpeklerin doğrudan boşluk üzerinde koşmaması hedeflenmiştir.

Üstten iniş ve yandan temas ayrımı hız, collider ve dikey konuma bağlıdır. Puf efekti ve sekme uygulanmış olsa da özellikle çukur kenarında, ikinci zıplamada ve köpek zıplarken temas sınanmalıdır. Görsel patilerin yere oturması ile fiziksel trigger sınırı ayrı kontrollerdir.

### Bölüm ağacı

Ağacın etkileşim alanına girmek yalnızca tırmanma menziline girmektir. Sağ ve sol hareket ağacın yanından geçmeyi sağlamalıdır. Tırmanma, menzilde yerde dururken ZIPLA/sağ pati yukarı/W/yukarı ok/Space ile başlar; yeşil Tırman işaretine dokunmak yardımcı bir seçenektir. Tüm tırmanma menzili ile çukur kenarı arasında en az 3,5 dünya birimi boş yaklaşma alanı ayrılır; geniş zemin yoksa ağaç o yerleşimde atlanır. Böylece çukurda zıplama girdisi tırmanma menziliyle çakışmaz. Kedi önce yatay hizalanır, ardından X sabit tutularak Y ekseninde yükselir. Tepede normal duruşa geçer, ağaç başına bir kez +5 alışveriş puanı alır ve hareket komutu gelene kadar bekleyebilir.

Mevcut kod, ağaç içeriği olan temalarda her dört bölümden birinde ağacı atlar; diğer bölümlerde geçerli bir konum seçer. Dolayısıyla önceki yaklaşık yüzde 60 önerisi mevcut uygulamanın kesin oranı değildir. Konum bölüm numarasından üretildiği için tekrar denemede değişmemelidir.

## 6 Kediler bakım ve ev etkileşimleri

### Kedi koleksiyonu

Başlangıç kedisi Mitzi'dir. Varsayılan kadro Mitzi, Pamuk, Kömür, Tarçın, Ada ve Moka olarak tanımlanmıştır. Yeni kediler beş bölüm aralıklı kurtarma ilerlemesiyle açılır; gerçek eşikler katalog verisinden okunur. Geliştiriciye ait yalnız Mitzi görünümü normal ilerlemeden ayrı bir test tercihidir.

Her kedinin kimliği, adı, portresi, başlangıç görseli, animasyon referansları ve açılma bilgileri `CatDefinition` verisinde tutulur. Diğer kediler için tüm hareket ve ev animasyonlarının Mitzi kadar tamamlandığı kabul edilmemelidir. Eksik kareler yanlış kedi görünümüyle doldurulmamalı; gerektiğinde kedinin kendi sabit görseli kullanılmalıdır.

### Bireysel bakım ve ortak para

Her kedinin açlık, su ve sevgi değeri 0–100 aralığında bağımsız tutulur. Varsayılan kurala göre her 30 dakikada açlık 2, su 3 ve sevgi 1 azalır. Çevrimdışı düşüş her dönüşte en fazla sekiz saat için uygulanır. Seçili kedinin üç ihtiyacının da en az 20 olması bölüm girişi için gerekir.

Kedinin bireysel sevgisi ile ortak Sevgi veya dükkan puanı farklı değerlerdir. Dükkan tüm kediler için ortak bakiyeyi kullanır. Mama ve su, günlük ödül alınmış olsa bile ilgili ihtiyacı doldurabilir. Günlük ödül kısıtı bakımın tekrar yapılmasını engellememelidir.

Varsayılan ödüller mama için 15, su için 20, dinlendirme için 30; günlük okşama puanı sınırı kedi başına 100'dür. Bölüm tamamlama 10, kuş toplama 3 puan verir. Bu sayılar uygulama değerleridir; dengeli ekonomi oldukları henüz kullanıcı testiyle kanıtlanmış değildir.

### Eşyalarla etkileşim

- **Mama kabı:** Seçili kedi kaba yürür; Mitzi için dört yemek karesiyle animasyon oynar. Beslenme yalnız ilgili kedinin açlığını doldurmalıdır. Ağız ile mama kabı arasındaki mesafe, farklı eşya ölçeklerinde sınanmalıdır.
- **Su kabı:** Seçili kedinin su ihtiyacını karşılar. Bütün kediler için ayrı içme animasyonu bulunduğu varsayılmamalıdır.
- **Kedi ağacı:** Kedi eşyanın yanına ve merkezine gelir, az miktarda yükselip iner. Evdeki bu kısa etkileşim, bölüm ağacındaki tam yükseklik tırmanışından ayrıdır.
- **Kedi kumu:** `new_cat_sand.png` eşya görseli ve giriş/çıkış kareleri birlikte çalışır. Animasyon tek kullanımda bir kez tamamlanmalı; döngü halinde tekrar girip çıkmamalıdır. Eşya aynalandığında bütün kareler aynı yönde aynalanmalıdır.
- **Yatak ve dinlenme:** Uyku ve dinlenme pozları kullanılır. Gövde opak, dış arka plan transparan olmalıdır.
- **Oyuncak ve dekor:** Fare oyuncağı dahil ürünler katalog üzerinden sunulur. Her dekorun özel animasyonu veya ayrı mekanik işlevi olduğu varsayılmamalıdır.

Dükkan görünümü dört sütuna geçirilmiştir. Satın alma, sahip olma ve yerleştirme kayıtları bulunur. Ev düzenlemede eşyalar sürüklenebilir ve yönleri çevrilebilir. Eşya animasyonlarının ölçeği, pivotu ve yönü statik eşya ile birlikte değerlendirilmelidir.

## 7 Teknik mimari

| Alan | Ana dosyalar | Sorumluluk |
| --- | --- | --- |
| Oyun akışı | GameFlow, RuntimeBootstrap | Ev, hazırlık, oyun, bilgi, duraklama ve sonuç ekranları |
| Bölümler | LevelManager, LevelCourseBuilder, GoalTrigger | Yerleşim, tema, doğma, hedef ve ilerleme |
| Kedi hareketi | CatController, MobileControlButton | Fizik, zıplama, eğilme, tırmanma ve mobil giriş |
| Çizim | LineDraw, PathSurface | Fırça bütçesi, çizgi görselleri, collider ve silme |
| Ev | CatHouseManager, CatHomeBehaviour, FurnitureInteraction | Kedi seçimi, hedefe yürüme ve eşya etkileşimi |
| Bakım ve ekonomi | CatNeedsSystem, CozyEconomyManager | Bireysel ihtiyaçlar, günlük ödüller ve ortak bakiye |
| Veri ve kayıt | CatDefinition, ShopItemDefinition, LevelDefinition, SaveService | İçerik tanımları ve yerel ilerleme |
| Arayüz ve ses | HudView, SafeAreaFitter, CozyAudioManager | Ekranlar, güvenli alan ve müzik/efekt ayarları |

Proje Unity 2022.3.50f1 ve Built-in Render Pipeline kullanır. Ana build sahnesi `Assets/Scenes/PawPath.unity` olarak tanımlıdır. Ev ve bölüm akışı tek ana sahnede kök nesnelerin etkinleştirilmesiyle yönetilir. `HubTest.unity` geliştirme sahnesidir.

Oyun durumları MainMenu, InfoOverlay, DrawingPreparation, Gameplay, Pause, Victory, GameOver ve Rescue olarak ayrılmıştır. Hazırlık ve sonuç durumlarında zaman ölçeği sıfırlanır. Bunun mobil uygulamanın arka plana alınmasıyla otomatik olarak aynı şey olduğu varsayılmamalıdır.

ScriptableObject içerik yapısı vardır; buna rağmen hareket hızı, ödül, bazı yerleşim ve görsel ölçüler kodda kalmaktadır. Bütün değerlerin veri dosyalarına taşındığı söylenemez. Özellikle büyük RuntimeBootstrap ve LevelCourseBuilder sınıflarının sorumlulukları zamanla ayrılmalıdır.

## 8 Kayıt ve veri güvenilirliği

İlerleme tek bir yerel JSON üzerinden PlayerPrefs içinde saklanır. Kayıt şeması sürüm 2'dir. Son sağlam kayıt için yedek, eski biçimden geçiş, bozuk ham veriyi koruma ve daha yeni kayıt sürümünü eski uygulamanın ezmesini önleme mekanizmaları uygulanmıştır.

JSON; bölüm ilerlemesi, ortak bakiye, seçili ve açılmış kediler, satın alınan/yerleştirilen eşyalar ve bireysel ihtiyaçları içerir. Dışa aktarma ve içe aktarma API'leri bulunur. Ancak oyuncunun kullanabileceği aktarım ekranı ve cihazlar arası senkronizasyon yoktur.

Mobilya konumları ve yönleri, görüntü boyutu, zorluk ve bazı günlük bakım bilgileri ayrı PlayerPrefs anahtarlarında tutulur. JSON dışa aktarımı bütün bu tercihleri taşımaz. Aynı cihazdaki yedek, uygulama kaldırıldığında veya cihaz kaybedildiğinde erişilebilir bulut yedeği anlamına gelmez.

Yayın öncesinde kayıt kapsamı belgelenmeli, mobilya ve tercihlerin sürümlü kayıt kapsamına alınması değerlendirilmelidir. Oyuncu ilerlemesini kaybettiğinde anlaşılır hata/geri yükleme davranışı gereklidir. Kayıt biçimi değişiklikleri mevcut oyuncunun verisini koruyarak yapılmalıdır.

## 9 Tamamlanan çalışmalar ve doğrulama durumu

| Çalışma | Mevcut durum | Kalan doğrulama |
| --- | --- | --- |
| Çizim hazırlığında kediyi bekletme | Uygulandı | İlk açılış ve yeniden deneme |
| Çizim alanı geçişi ve erken başlatma | Uygulandı | Kamera sınırları ve fırça paylaşımı |
| Çizim sonrası ZIPLA kontrolü | Uygulandı | Gerçek mobil dokunma |
| İki modda köpeğe üstten basma ve puf | Uygulandı | Kenar temasları ve köpek zıplarken çarpışma |
| Zorluk ve görüntü boyutu seçenekleri | Uygulandı | Küçük telefon ve tablet yerleşimi |
| Bireysel bakım ve zamanla azalma | Uygulandı | Gün değişimi ve uzun çevrimdışı oturum |
| Mama, ağaç ve kum etkileşimleri | Uygulandı | Seçili kedi, yön, ölçek ve temas hizası |
| Tema ve robotik içerik | Uygulandı | Her temada okunabilirlik ve geçilebilirlik |
| Kayıt yedeği ve sürüm geçişi | Uygulandı | Gerçek uygulama güncelleme senaryosu |
| Geometri, hedef, bakım ve kayıt kontrolleri | Editör komutları mevcut | Oynanış ve cihaz testlerinin yerini tutmaz |
| ARM64, IL2CPP ve API 36 ayarları | Uygulandı | Yeni araçlarla gerçek AAB üretimi |
| İmzalı AAB oluşturma menüsü | Kod derlemesi doğrulandı | Araç zinciri ve imzalama kurulumu |

Geçmişte düzeltme uygulanmış olması, ilgili animasyon veya hizalamanın bütün cihazlarda sorunsuz olduğu anlamına gelmez. Bu yüzden yukarıdaki durumlar uygulama ile doğrulamayı ayrı gösterir.

## 10 Yayın öncesi zorunlu işler

### P0 Android yayın paketini üretmek

Proje ARM64, IL2CPP, Target API 36, görünür sürüm 1.0.0 ve Android sürüm kodu 1 için hazırlanmıştır. Unity menüsüne Prepare Release Settings, Validate Release Setup ve Build Signed AAB komutları eklenmiştir. Geliştirme, hata ayıklama ve profiler bağlantısı yayın derlemesinde kapatılır. Anahtar ve parolalar ortam değişkenlerinden alınır; Git'e kaydedilmez.

Kurulu Unity 2022.3.50f1 eski olduğu için ön kontrol yayın derlemesini durdurur. Bu kurulumun SDK'sında 33, 34 ve 35 platformları vardır; API 36 yoktur. İmzalama anahtarı tanımlı değildir. Uyumluluğu doğrulanmış güncel Unity yaması, Android araçları ve yükleme anahtarı gereklidir. Unity 2022.3.56 ve sonrası 16 KB desteği için alt sınır olarak kontrol edilir; seçilen yamanın API 36 ile uyumu ve gerçek native kütüphaneler ayrıca doğrulanmalıdır.

Tamamlanma ölçütü: İmzalı AAB üretilmiş, manifestte paket kimliği/SDK/sürüm doğrulanmış, ARM64 native kütüphaneleri 16 KB açısından kontrol edilmiş ve Play iç testinde gerçek cihaza kurulmuş olmalıdır. Ayrıntılar `ANDROID_RELEASE.md` dosyasındadır. Daha önce Play'e yükleme yapıldıysa sürüm kodu 1 yerine önceki bütün kodlardan büyük bir değer kullanılmalıdır.

### P0 Mobil duraklatma ve devam akışı

Telefon görüşmesi, bildirim, ekran kilidi veya uygulama değiştirme sırasında oyun güvenli şekilde durmalıdır. Mevcut bakım sistemi odak dönüşünü işler; ana oyun akışında aynı kapsamda uygulama duraklatma yönetimi bulunmuyor.

Çizim erken bitirildiğinde kalan mürekkep, çizimin hâlâ sürüp sürmediğini belirlemek için tek başına yeterli değildir. Mevcut `DrawingStillInProgress` kontrolü mürekkep miktarına bakar. Duraklatmadan dönüşte çizim kapalıyken hazırlık durumuna dönme riski vardır. Hazırlığın tamamlanma durumu ayrı tutulmalıdır.

Tamamlanma ölçütü: Çizim öncesi, çizim sonrası ve tuşlu oyunda uygulama arka plana alınıp geri getirildiğinde kedi kendiliğinden kaybetmemeli, kontroller kilitlenmemeli ve doğru oyun durumu geri yüklenmelidir.

### P0 Zemin ve çarpışma güvenilirliği

Kedinin görünür ayak hizası bütün karelerde sprite alt boşluğunun yaklaşık yüzde 10 olduğu varsayımıyla ayarlanır. Görsellerdeki boşluk farklı olduğunda gömülme veya havada durma oluşabilir. Ortak ayak noktası, tutarlı pivot veya kareye özel ölçüm hazırlanmalıdır.

Kapıdan geçişte Y sınırı, çukurdan düşme, tümsek kenarı, çizilmiş yol ve köpeğe üstten iniş birlikte sınanmalıdır. Tamamlanma ölçütü: Temaların tamamında seçilen örnek bölümlerde görünür yüzey ile fiziksel temas uyumlu olmalı; kapı altından düşen kedi kazanmamalıdır.

### P0 Cihaz üzerinde test kaydı

En az bir daha düşük kapasiteli ve bir güncel Android telefonda; iOS yayınlanacaksa iPhone'da test kaydı tutulmalıdır. 16:9, uzun telefon oranı ve 4:3 tablet görünümü kontrol edilmelidir. Test kaydı cihaz, işletim sistemi, oyun sürümü, senaryo, sonuç ve hata kanıtını içermelidir.

Tamamlanma ölçütü: Bölüm başlatma, iki mod, başarısızlık/başarı, eve dönüş, alışveriş, kedi seçimi ve kayıt yükleme senaryolarında yayın engelleyen açık hata kalmamalıdır.

## 11 Kalite ve içerik için sonraki işler

### P1 Performans

Zemin köşeleri için her parçada oluşturulan 512×512 RGBA32 maske yaklaşık 1 MiB ham doku verisi taşır. Paylaşımlı maske, önceden üretilmiş varyantlar veya daha küçük çözünürlük değerlendirilmelidir. Geçici piksel dizisi ve üretim maliyeti ayrıca ölçülmelidir. Temizlik kodu bulunması, bölüm geçişinde hiç takılma olmadığını kanıtlamaz.

Çizim materyali artık paylaşımlı kullanılır ve ömrü yönetilir; önceki her çizgide yeni materyal oluşturma eleştirisi güncel haliyle tekrarlanmamalıdır. Buna rağmen 50 çizme/silme işlemini birkaç tur tekrarlayan profil testi gereklidir. Bellek, materyal sayısı, GC tahsisi, kare süresi ve uygulama açılışı ölçülmelidir. Hedef kare hızı cihaz grubuna göre belirlenmelidir.

### P1 Ekonomi ve bakım dengesi

Kuş, bölüm, günlük bakım ve oyuncak ödüllerinin eşya fiyatlarıyla ilişkisi hesaplanmalıdır. Oyuncak etkileşiminde tekrarlanabilir 5 puan ödülü bulunur; günlük bakım sınırlıyken bu kaynağın bakiyeyi sınırsız hızla artırıp artırmadığı test edilmelidir. İkinci kediye ulaşma ve ilk anlamlı satın almayı yapma süreleri oyuncu deneyiminde ölçülmelidir.

Sevgi ihtiyacının düşük, günlük okşama limitinin dolu olduğu durumda oyuncunun bakım kilidinden çıkabildiği doğrulanmalıdır. Günlük para ödülü ile ihtiyacı düzeltme işlemi gerektiğinde ayrı tasarlanmalıdır. Para birimi adı ve bireysel sevgi göstergesi bütün ekranlarda tutarlı kullanılmalıdır.

### P1 İlk kullanım ve erişilebilirlik

İlk bölüm oyuncuya çizgi çizme, sonraki alanı görme, erken başlatma ve ZIPLA kullanımını adım adım öğretmelidir. Köpeğe üstten basma ve güvenli konumdaki yeşil işaretli gövdede zıplama tuşuyla tırmanma görsel olarak anlatılmalıdır. Evde önce kedi sonra eşya seçme akışı da açıklanmalıdır.

Yazı ve tuşlar büyük görüntü ayarında taşmamalı, çentik altında kalmamalı ve yeterli kontrasta sahip olmalıdır. Engel anlatımı yalnız renge dayanmamalıdır. Türkçe metinler birden fazla dosyada sabit yazılmıştır; uluslararası yayın hedeflenirse bütün arayüz için gerçek dil seçimi ve İngilizce içerik gerekir.

### P1 Animasyon ve sanat bütünlüğü

Diğer kedilerin yürüme, bekleme, zıplama ve uyku setleri tamamlanmalıdır. Mama, kedi ağacı ve kum animasyonlarının eşya ile aynı ölçek/yönü paylaşması gerekir. Tüm PNG'lerde gövde opak, dış alan gerçek alfa transparan olmalıdır; dama deseni resme gömülmemelidir. Kareler arasında renk, ışık, ayak tabanı ve gövde ölçeği tutarlı tutulmalıdır.

### P2 Yayın sonrası içerik önerileri

Yeni mekanlar, daha fazla kedi, ek mobilya ve görevler mevcut sistemler doğrulandıktan sonra planlanabilir. Bölüm seçme/tekrar oynama ekranı, başarı hedefleri ve kısa günlük görevler uzun vadeli ilerlemeyi destekleyebilir. Bulut kayıt ve cihazlar arası aktarım değerlendirilebilir. Bunlar mevcut tamamlanmış özellikler değildir.

Reklam, uygulama içi satın alma veya ücretli satış modeli henüz ürün kararı olarak netleştirilmelidir. Sırf yayınlamak için reklam SDK'sı eklemek zorunlu değildir. Seçilecek model bakım ve bölüm akışını kesintiye uğratmamalıdır.

## 12 Yayın yol haritası ve kabul ölçütleri

| Aşama | Yapılacak iş | Aşamayı bitiren sonuç |
| --- | --- | --- |
| 1 Teknik hazırlık | Unity yaması, API 36, Android araçları ve upload anahtarı | Doğrulanabilir imzalı AAB |
| 2 Oynanış güvenilirliği | Duraklatma, çizim devamı, ayak hizası ve temas kuralları | Kritik senaryolarda takılma ve yanlış sonuç yok |
| 3 Kapalı beta | Gerçek cihaz testleri ve yeni oyuncu denemesi | Tekrarlanabilir hata listesi ve kapanmış yayın engelleri |
| 4 Kalite | Performans, denge, öğretici ve animasyonlar | Hedef cihazlarda kabul edilen akıcılık ve okunabilirlik |
| 5 Mağaza hazırlığı | İkon, ekran görüntüsü, açıklama, gizlilik ve destek | Mağaza gönderim dosyaları ve beyanları hazır |
| 6 Kademeli yayın | Küçük oyuncu grubundan başlayarak sürüm dağıtımı | Çökme, kayıt kaybı ve temel akış sorunları izleniyor |

Süre tahmini, cihaz testlerinin bulacağı sorunlara ve araç zinciri geçişine bağlıdır. Bu belge sabit bir yayın tarihi taahhüt etmez. Öncelikler P0 yayın engeli, P1 kalite ve güvenilirlik, P2 sonraki geliştirme olarak kullanılır.

### Mağaza teslim listesi

- Uygulama kimliği ve sürüm kodlarının korunması; upload anahtarının güvenli yedeği.
- İkon, doğru ekran görüntüleri, kısa/uzun açıklama ve destek iletişimi.
- Kullanılan görsel ve müziklerin ticari kullanım izinlerinin kayıt altına alınması.
- Gizlilik politikası, veri güvenliği beyanları, yaş ve hedef kitle bilgilerinin gerçek SDK kullanımına göre doldurulması.
- Android iç test paketi; iOS hedefleniyorsa imzalama, uygun Xcode/SDK ve TestFlight paketi.
- Çökme bildirimleri ve kullanıcı geri bildirimi için bir takip yöntemi.

### Asgari yayın kabulü

Oyuncu yeni kayıtla açılıştan başlayarak kedisini besleyebilmeli, iki modda bir bölümü bitirebilmeli, başarısız olup yeniden deneyebilmeli, ödül kazanabilmeli, eşya alıp yerleştirebilmeli ve uygulamayı yeniden açtığında ilerlemesini korumalıdır. Bu yolculuk gerçek telefonda, uygulama arka plana alınarak ve farklı ekran oranlarında tekrar edilebilmelidir.

## 13 Kaynak dosyalar ve belgeyi güncelleme

Bu belge 15 Ekim 2026 tarihindeki yerel proje durumunu özetler. Kodda uygulama bulunması, bütün cihazlarda test edildiği anlamına gelmez. Editör derlemesi ile gerçek Android paket doğrulaması ayrı sonuçlardır.

Başlıca proje kaynakları:

- `README.md` ve `QA_CHECKLIST.md`: proje kurulumu ve mevcut manuel test planı.
- `ANDROID_RELEASE.md` ve `Assets/_Game/Editor/AndroidReleaseBuilder.cs`: yayın ayarları ve araç/anahtar ön kontrolleri.
- `ProjectSettings/ProjectSettings.asset` ve `ProjectSettings/ProjectVersion.txt`: platform ayarları ve Unity sürümü.
- `Assets/_Game/Scripts/Core/GameFlow.cs`: durum geçişleri ve hazırlık/oyun akışı.
- `Assets/_Game/Scripts/Core/SaveService.cs`: kayıt şeması, yedek ve geçiş.
- `Assets/_Game/Scripts/Cat/CatController.cs` ve `CatNeedsSystem.cs`: hareket, ayak hizası ve bireysel bakım.
- `Assets/_Game/Scripts/Levels/LevelCourseBuilder.cs`, `DogObstacle.cs`, `ClimbableTree.cs`, `FlyingBirdCollectable.cs`: bölüm, engel ve ödül davranışları.
- `Assets/_Game/Scripts/Hub/FurnitureInteraction.cs` ve `DraggableFurniture.cs`: ev animasyonları ve yerleşim kayıtları.
- `Assets/Editor` altındaki geometri, hedef, bakım ve kayıt doğrulama komutları.

Her önemli sürümde bu belgenin tarihi, tamamlanan işler tablosu, açık P0 maddeleri ve cihaz test sonuçları güncellenmelidir. Eski öneriler aktif kodla karşılaştırılmadan yeniden eksik listesine eklenmemelidir.
