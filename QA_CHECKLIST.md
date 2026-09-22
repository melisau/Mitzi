# Mitzi kısa doğrulama listesi

Her değişiklikten sonra önce Unity'de **Mitzi > Validate Course Geometry** komutunu çalıştır. Bu komut 1–100. bölümlerde yol, çukur ve tümsek sınırlarını kontrol eder; aşağıdaki oynanış testlerinin yerine geçmez.

Testleri en az bir gerçek Android telefonda, iOS sürümü hazır olduğunda da bir iPhone'da yap. Kayıt testinden önce mevcut ilerlemenin ekran görüntüsünü al; **Reset Save Data** komutu mevcut kaydı siler.

| Senaryo | İşlem | Beklenen sonuç |
| --- | --- | --- |
| Çukura düşme | Bölümü başlat, bilgilendirmeyi kapat, çizgi çizmeden çukura yürü. Ardından **Yeniden** de. | Kedi oyun başında düşmez; gerçek çukura düşünce kaybetme ekranı bir kez açılır. Yeniden denemede kedi zeminde başlar, bölüm ilerlemesi artmaz. |
| Kapıya ulaşma | Çukuru çizerek/zıplayarak aş ve çıkışa yürü. | Kapının yanından geçip takılmaz; kazanma ekranı bir kez açılır, bölüm ve ödül bir kez kaydedilir. |
| Kapı altından düşme | Kedi kapının X konumunu zeminin altında veya kapının çok üstünde geçsin; ardından normal yükseklikte tekrar dene. **Mitzi > Validate Goal Bounds** komutunu da çalıştır. | Yalnız X çizgisini geçmek kazanma sayılmaz; kapı yüksekliği dışında kedi kazanmaz. Normal girişte kazanma bir kez tetiklenir. |
| Kedi seçimi ve eşya | Evde iki kedi varken birini seç, mama kabına; sonra kedi ağacına/kuma dokun. Aradaki bir zemine de dokun. | Yalnız seçilen kedi ilgili eşyaya kadar yürür ve animasyonu orada yapar. Diğer kedi engel olmaz; zemine dokununca seçilen kedi eşyaların arasından hedefe yürür. Mama sonrası yalnız ilgili kedinin ihtiyacı değişir. |
| Kayıt yükleme | Bölüm, sevgi puanı, seçili kedi, kedi ihtiyaçları ve yerleştirilmiş eşyalardan örnek değerleri not et. Oyunu tamamen kapatıp yeniden aç. | Not edilen değerler korunur; bölüm başa dönmez, satın alınan eşya kaybolmaz. |
| Yedek ve sürüm geçişi | Önce **Mitzi > Validate Save Format** komutunu çalıştır. Test kopyasında ana kayıt JSON'unu bozup oyunu yeniden aç; yedek geri yüklemesini denetle. | Eski kayıt yeni şemaya geçer, sağlam yedek ilerlemeyi kurtarır. Her iki kayıt da bozuksa ham JSON `.corrupt` anahtarında korunur. Canlı kaydı test için bozma. |
| Kediye özel bakım | İki kedinin ihtiyaç değerlerini karşılaştır. Birini besle/sula/okşa; diğer kediyi seçip oyuna girmeyi dene. Bir süre sonra uygulamayı kapatıp aç. | Yalnız bakım yapılan kedinin ilgili değeri yükselir. Değerler zamanla yavaş düşer; çevrimdışı düşüş en fazla 8 saatliktir. Bölüm girişi ortak Sevgi puanına değil seçili kedinin üç ihtiyacının da en az 20 olmasına bağlıdır. Günlük ödül dolsa bile mama/su ihtiyacı yeniden doldurulabilir. |
| Bireysel sevgi göstergesi | Evde iki farklı kediyi sırayla okşa; her kedinin üzerindeki ve okşama bilgisindeki sayıyı karşılaştır. | Gösterilen 0–100 sevgi değeri dokunulan kediye aittir. HUD'daki **Dükkan Puanı** ortak para birimidir; kedi sevgisiyle aynı gösterge değildir. |
| Ekran oranları ve dokunma | Aynı ev ve oyun ekranını 16:9, 19.5:9 ve tablet 4:3 yatay görünümde kontrol et; gerçek telefonda dokunma tuşlarını kullan. | Mama/su/uyu, oyun seçimi, ses ve **Eve Dön** tuşları görünür ve basılabilir; tuşlar üst üste binmez, güvenli alan/çentik altında kalmaz. Zemin altında veya çukur kenarında boş/taşmış parça görünmez. |
| Çizim bellek/profil | Android Development Build'de Unity Profiler'ı bağla. Aynı bölümde 50 çizgi çizip silme işlemini üç kez tekrarla; CPU Timeline ve Memory/Material sayısını başlangıç ve her tur sonunda kaydet. | `PawPath.LineDraw.BeginStroke`, `AppendPoint` ve `Erase` süreleri kontrol edilir. Her çizgide yeni materyal oluşmaz; silme sonrası materyal sayısı turdan tura sürekli artmaz. Kare takılması varsa süre/GC tahsisi ekran görüntüsüyle kaydedilir. |

Zemin testi için sokak, orman ve cadde temalarının her birinde **1, 6 ve 20. bölümleri** ayrıca gözle kontrol et. Özellikle tümseğin yol dışına taşmamasına, çukurun gerçekten açık kalmasına ve kuş toplarken zeminin anlık değişmemesine bak.

Test kaydı: tarih, Unity sürümü, cihaz/model, işletim sistemi, oyun sürümü, geçen/kalan senaryolar ve hata ekran görüntüsü.

Not: Bu listedeki oynanış ve cihaz testleri henüz otomatik değildir; editör geometri doğrulaması yalnızca yerleşim verisini denetler.
