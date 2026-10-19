# Mitzi (Paw Path)

Unity **2022.3.62f3**, Built-in Render Pipeline ile geliştirilen, yatay ilerlemeli 2D mobil kedi oyunu. Çizerek veya tuşlarla oynayın; kedileri kurtarın, evde bakım yapın ve iki odayı döşeyin. Proje geliştirme aşamasındadır; fiziksel cihaz ve mağaza yayın doğrulaması henüz tamamlanmamıştır.

## Oynanış

- **Çizerek Oyna:** normal, zıplatıcı, tehlikeli ve buzlu yollar çizin; silgiyi kullanın. Koşu sırasında yukarı okla zıplayın, aşağı oku basılı tutarak eğilin.
- **Tuşlarla Oyna:** ekrandaki yön, zıplama ve eğilme düğmeleri; Editor'da klavye desteği.
- Cadde, orman, şehir ve neo teknoloji mekanları; çukurlar, tümsekler, ağaçlar ve eğilerek geçilen engeller. Şehirde asfalt yol ve açık servis kanalları kullanılır.
- Köpeğe temas kaybettirir; üstüne zıplayarak köpeği uzaklaştırabilirsiniz. Kuş toplamak alışveriş puanı kazandırır.
- Mitzi başlangıç kedisidir. Pamuk, Kömür, Tarçın, Ada ve Moka sırasıyla 7, 20, 30, 40 ve 50. bölüm sonunda kurtarılır. Kurtarılacak kedi bitiş kapısının yanında bekler.
- İlk oynayışta her kontrol biçimi için kısa yazılı, animasyonlu öğretici gösterilir. Türkçe, İngilizce ve Almanca desteklenir.

Otomatik zorluk her mekânda ayrı ilerler: ilk 10 bölüm kolay, 11–20 orta, 21. bölümden itibaren zor. Köpek sayısı/hızı, çukur ve tümsek yoğunluğu ile çizim bütçesi buna göre değişir. Oyna menüsünden Otomatik, Kolay, Orta veya Zor seçilebilir; elle seçilen seviye değiştirilene kadar korunur. Eski kayıtlarda mekân geçmişi tutulmadığından yeni mekân sayaçları sıfırdan başlar; genel ilerleme, bütçe ve kurtarılan kediler korunur.

## Kedi evi ve ekonomi

İki oda arasında geçiş yapın. Kediler bağımsız dolaşır; bir kediyi seçip yere veya etkileşimli eşyaya dokunarak yönlendirebilirsiniz. Mama, su, uyku, sevme/mırlama, kum kabı ve kedi ağacı etkileşimleri bulunur.

Dükkandan aynı üründen birden fazla alınabilir; yerleştirme, taşıma, çevirme, depolama ve satış desteklenir. Her başarılı alımdan sonra aynı ürünün sonraki fiyatı **×3** artar (40 → 120 → 360). Satış fiyat geçmişini sıfırlamaz. Duvar kağıdı, dükkanın açıldığı odaya uygulanır; odalar farklı görünebilir. Yeni kayıtta mavi oda varsayılandır. Alışveriş sonrasında liste konumu korunur. Eşyalar zemin/duvar sınırları içinde tutulur; yeni eşyalar boş yerlere ayrık yerleşir, oda doluysa depoda saklanır.

Alışveriş puanı ve kedinin sevgi ihtiyacı ayrıdır. Bölüm ödülü 5, kuş ödülü 3, yeni kedi ödülü 10 puandır. Tamamlanan ödüllü reklam 5 alışveriş puanı verir; günlük sınır 3'tür. Bakım ve sevme ödülleri günlük sınırlandırılmıştır. Test bütçesi ve tüm kedileri açma düğmeleri kaldırılmıştır.

İlerleme, envanter, satın alma geçmişi, oda düzeni ve kedi ihtiyaçları `PlayerPrefs` içinde sürümlü JSON (şema 5) ve son sağlam yedekle tutulur. Açlık, su ve sevgi zamanla azalır; çevrimdışı ihtiyaç hesabı sekiz saatle sınırlıdır.

## Projeyi açma

1. Unity Hub'dan bu klasörü mevcut proje olarak **2022.3.62f3** ile açın.
2. `Assets/Scenes/PawPath.unity` sahnesini açıp Play'e basın.
3. Mevcut projede başlangıç sahnesini yeniden oluşturmayın. **Paw Path > Build Starter Scene** yalnızca boş proje içindir ve mevcut katalog/sahneyi korumak için çalışmayı reddeder.

Ayarlar; görünüm boyutu, müzik/efekt, dil, öğretici ve oyundan çıkış içerir.

### İlk oynayışı test etme

**Ayarlar > İlk Oynayış Testi > Sıfırla ve Başlat** ile ilk kurulum deneyimine dönün. Onay ekranında Vazgeç hiçbir veriyi silmez. Onay; bu uygulamanın tüm `PlayerPrefs` verisini, ilerlemeyi, puanları, eşyaları, günlük limitleri, ayarları ve öğretici geçmişini siler; sahneyi yeniden açar. Yalnızca Mitzi ve 50 alışveriş puanı ile başlanır; iki oyun biçiminin öğreticileri yeniden gösterilir.

Bu geçici düğme yalnızca **Unity Editor veya Development Build** sürümlerinde derlenir; normal yayın sürümünde görünmez. Kullanılınca mevcut test ilerlemesi kalıcı olarak silinir.

## Kaynak yapısı

| Klasör | İçerik |
| --- | --- |
| `Assets/_Game/Scripts/Core` | Oyun akışı, arayüz kurulumu, kamera ve kayıt |
| `Assets/_Game/Scripts/Cat`, `Gameplay`, `Levels` | Animasyon, kontroller, bölüm ve engeller |
| `Assets/_Game/Scripts/Hub`, `UI`, `Economy` | Odalar, bakım, eşya düzenleme ve dükkan |
| `Assets/_Game/Scripts/Drawing` | Çizim ve fiziksel yol araçları |
| `Assets/_Game/Art` | Oyunda kullanılan görseller |
| `Assets/_Game/Content` | Kedi, eşya ve katalog tanımları |
| `Assets/_Game/Editor` | İçerik oluşturucular ve izole doğrulamalar |
| `tools` | Görsel hazırlama araçları |

Eski kullanılmayan piksel hayvanlar ve referanssız varlıklar temizlenmiştir. Yerel kaynak videoları, ara çıktılar, test günlükleri ve kurtarma kopyaları Git'e dahil edilmez. Sanat yaklaşımı için [ART_STYLE](docs/ART_STYLE.md).

## Doğrulama ve yayın durumu

18 Ekim 2026 güncellemesinde izole `MitziValidation` projesinde Unity derleme ve doğrulama çalışmaları geçti: hayvan animasyonları, tümsek/köpek hareketi, zıplama, eğilme fiziği, öğreticiler, envanter/fiyat artışı, oda bazlı duvar kağıdı, sabit kedi sıralaması, dükkan kaydırma konumu, şehir servis kanalı ve ilk oynayış sıfırlama.

Editor doğrulamalarını gerçek oyuncu kaydında çalıştırmayın; bazıları test verisi oluşturur veya siler. Ayrı proje kopyası ve `MitziValidation` ürün adı kullanın. Bunlar fiziksel cihaz testinin yerine geçmez. [QA_CHECKLIST.md](QA_CHECKLIST.md) cihaz ve oynanış kontrollerini içerir.

Android paket hazırlığı için [ANDROID_RELEASE.md](ANDROID_RELEASE.md). İmzalı cihaz testi AAB/APK paketleri üretildi; imza, ARM64/API 36, yerel kütüphane hizalaması ve APK paket hizalama kontrolleri geçti. Gerçek Android telefona kurulum ve ilk bölüm öğreticisinin açılış kontrolü başarılı; kapsamlı oynanış ve 16 KB cihaz testi bekliyor. Pati simgesi ve oyun içi gizlilik metni eklendi. Cihaz testi paketinde reklamlar kapalıdır; AdMob hesap/uygulama incelemesi, veri güvenliği ve çocuklara yönelik mağaza beyanları yayın öncesinde tamamlanmalıdır. [Gizlilik politikası](https://melisau.github.io/Mitzi/privacy.html) yayımlandı; gerçek Android reklam kimlikleri eklendi, ancak canlı reklamlar kapalı.

AdMob Android uygulama/ödüllü birim kimlikleri yapılandırıldı. [Geliştirici sitesi](https://melisau.github.io/) ve [app-ads.txt](https://melisau.github.io/app-ads.txt) yayımlandı; AdMob hesap/uygulama incelemesi ve mağaza bağlantısı henüz tamamlanmış sayılmaz.

19 Ekim 2026: Başlangıç çukurları fiziksel olarak 1,75–1,90 birime genişletildi; giriş platformları ve çukur görselleri aynı açıklıklardan oluşturulur. Değişken bölümlerde kolay 1,75–1,90, orta 2,00–2,30 ve zor 2,30–2,60 birim aralıkları kullanılır. İzole doğrulamada dört mekanda 144 yerleşim ve 332 fizik denemesi geçti: normal/büyük kedi, normal/hızlı yürüyüşte çukura düşme, kolay çukurlarda tek zıplama/köprü, ileri çukurlarda çift zıplama.

Evde boş alandan sola/sağa kaydırarak odalar arasında geçilebilir; ok düğmeleri de kullanılabilir. UI, kedi ve eşya üzerinden başlayan hareketler ile eşya düzenleme modu kaydırma başlatmaz. Kaydırmayı bırakmak, seçili kediye yürüme komutu vermez.

Köpek bacak animasyonu sabit 16 FPS yerine ilerleme hızına göre 8–12 FPS çalışır. Yerinde dururken duruş görseli, çukur sıçramasında sabit atlayış pozu kullanılır; düşman hareket hızları değiştirilmez.

Tuşlarla oynama modunda iki pati joysticki kullanılır: solda sağ/sol hareket, sağda yukarı itme ile zıplama ve aşağıda tutma ile eğilme. Sağ joystick merkezden tekrar yukarı itilince ikinci zıplama istenir; yukarıda tutmak otomatik tekrar üretmez. İki kontrol farklı parmakları ayrı izler; ekran dışına sürükleme, bırakma, duraklatma ve odak kaybı girişleri temizler. Çizerek oynarken yalnız sağdaki aynı zıpla/eğil joysticki gösterilir; çizim hazırlığında gizlenir. Her iki modun öğreticisi joystick hareketini gösterir ve eski öğreticiyi görmüş oyuncuya da yeni kontrol anlatımı açılır.

Evde uyuyan kediye yürüme hedefi verildiğinde uyanış kareleri yerinde tamamlanır; kedi kıvrılmış halde kaymadan yürümeye başlar. Oda tıklaması ve eşyalara doğrudan yürüme aynı beklemeyi kullanır.

Çizim koşusunda eğilme engelinin yanına zıplayarak çarpınca otomatik ileri baskı kesilir; kedi yerçekimiyle girişe iner ve eğilerek devam eder. Dikey çarpışma yüzeyleri zemin sayılmaz.

Evde seçili kedi dört yönlü pati joystickiyle zeminin sınırları içinde yürütülebilir; kediye kısa dokunmak seçimi değiştirir ve joystick üstünde adını gösterir. Yakındaki mama/su kabı veya yatak için ayrı bir kullan düğmesi çıkar. İşlem elle başlatılır; bakım sürerken joystick gizlenir, başka odaya yürürken kamera odayı takip eder. Uyuyan kedi önce yerinde uyanır; eşyaların içinden yürüyemez, yatağın içinden dışarı çıkabilir.
