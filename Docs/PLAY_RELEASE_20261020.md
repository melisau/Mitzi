# Mitzi — Google Play hazırlığı, 20 Ekim 2026

## Sürüm kararı

Kullanıcı önce reklamsız sürümün GitHub'a gönderilmesini, ardından isteğe bağlı ödüllü reklamlı yayın hazırlığını istedi. Reklamsız kaynak: `56e8030b4be5b99ad55d8d1bba8f71efac76ef1c` (`main`). Reklamlı sürümde tamamlanan reklam başına 5 alışveriş puanı, günlük 3 reklam sınırı korunur. Banner, zorunlu geçiş reklamı ve gerçek parayla satın alma yoktur.

`advertisingEnabled=true`; geliştirme sürümleri test reklamı, normal Android sürümü mevcut gerçek AdMob birimini kullanır. SDK başlatılmadan önce `AgeRestrictedTreatment.Child` ve `MaxAdContentRating.G` uygulanır. Android AD_ID izni manifestten çıkarılır. Google Mobile Ads Unity 11.6.0 / Android 25.5.0 kullanılır. Reklam sunumu veya inceleme onayı, yalnız bu ayarlardan çıkarılamaz.

## Console'da doğrulanan durum

- Kimlik ve telefon doğrulaması tamamlandı.
- Kullanıcının ayrıca onayladığı Play imzalama şartları ve gerekli beyanlarla **Mitzi** uygulama kaydı oluşturuldu: uygulama kimliği `4975980074103680735`, paket `com.mitzicat.game`, Türkçe, ücretsiz oyun.
- Üretime erişim kapalı: Console en az 12 kişinin kesintisiz 14 gün kapalı teste kayıtlı kalmasını istiyor. Henüz kapalı teste kayıtlı test kullanıcısı yok; kullanıcı listeyi henüz hazırlamadığını bildirdi. Bu süre başlamış sayılmaz.
- AdMob hesabı onaylı. Mitzi uygulaması **İnceleme gerekli / Sınırlı reklam sunumu / Mağaza ekle** durumunda; Play mağaza bağlantısı sonrasında inceleme tamamlanmalıdır.
- Gizlilik URL'si, reklam içerir: Evet, oyun için oturum açma gerekmiyor, resmi kurum/finans/sağlık özelliği yok beyanları kaydedildi. Kaydetmek incelemeye gönderildiği veya yayımlandığı anlamına gelmez.
- Gündelik oyun kategorisi, destek e-postası ve HTTPS geliştirici sitesi kaydedildi. Mağaza açıklaması, simge, özellik grafiği ve iki ekran görüntüsü de kaydedildi; mağaza girişi **İncelenmek üzere göndermeye hazır** durumunda. Henüz incelemeye gönderilmedi.
- İmzalı reklamlı sürüm **1.0.0 (1)** dahili testte yayımlandı ve Console **Dahili test kullanıcıları tarafından kullanılabilir / Etkin** durumunu gösterdi. Yalnız geliştirici hesabı `Mitzi - Geliştirici Dahili Test` listesine eklendi. Katılım: https://play.google.com/apps/internaltest/4701649389664604841 . Bu kanal kapalı testin 12 kişi / 14 gün şartını karşılamaz.
- İlk inceleme tamamlanana kadar uygulama adı `com.mitzicat.game (unreviewed)` olarak görünebilir. Play'in isteğe bağlı kod gösterme dosyası ve yerel hata ayıklama sembolleri uyarıları kaldı; paket hatası bulunmadı.
- IARC sözleşmesinin ayrıca kabulü için kullanıcıya onay soruldu; yanıt gelmeden sözleşme işaretlenmez. IARC, hedef yaş ve veri güvenliği sonlandırılmadı.

## Yayın metinleri

Ad: **Mitzi**

Kısa açıklama:

> Mitzi ile yollar çiz, engelleri aş, yeni kediler kurtar ve evini dekore et!

Tam açıklama:

> Mitzi ve kedi arkadaşlarıyla küçük bir maceraya çık!
>
> Çizerek Oyna modunda kedinin ilerlemesi için yollar hazırla. Tuşlarla Oyna modunda zıpla, eğil ve parkuru kendi kontrollerinle tamamla. Sokak, orman, cadde ve neo teknoloji mekanlarında çukurları aş, uygun ağaçlara tırman ve kuşları toplayarak oyun içi alışveriş puanı kazan.
>
> Bölümlerde ilerledikçe yeni kedi arkadaşlarını kurtar. Evde kedilerin mama, su, uyku ve sevgi ihtiyaçlarıyla ilgilen; iki odayı oyun içinde kazandığın puanlarla döşe.
>
> • İki farklı oynama biçimi
> • Dört mekan ve farklı engeller
> • Kurtarılabilen kedi arkadaşları
> • Kedi bakımı ve ev dekorasyonu
> • Türkçe, İngilizce ve Almanca
> • Seçilebilir zorluk ve görünüm boyutu
>
> Oyun isteğe bağlı ödüllü reklam içerir. Tamamlanan reklam 5 alışveriş puanı verir; günde en fazla 3 kez izlenebilir. Oyun içi alışveriş gerçek parayla yapılmaz. İlerleme cihazda saklanır; oyun için üyelik gerekmez. Reklamlar ve ebeveyn kontrolüyle açılan Instagram bağlantısı internet gerektirir.

İletişim: `melisauyar5225@gmail.com`

Geliştirici sitesi: https://melisau.github.io/

Gizlilik: https://melisau.github.io/Mitzi/privacy.html

## Kalanlar

İmzalı yayın AAB'si üretildi: `Builds/Android/PlayRelease-20261020/Mitzi-1.0.0-1.aab` (162.048.635 bayt). `verify_android_release.py` ile bundletool yapısı, AAB imzası, API 36, ARM64/IL2CPP, AD_ID izninin bulunmaması ve dört yerel kütüphanenin LOAD/RELRO 16 KB statik kontrolleri geçti (`static_checks_pass=true`). Rapor aynı klasördedir. Gerçek 16 KB ortam testi bu sonuçtan çıkarılamaz. Rapordaki eski `production_admob` / `public_privacy_policy` varsayılanları dış servisleri sorgulamaz; güncel hesap/politika durumu yukarıda doğrudan Console üzerinden belgelenmiştir.

1. Son Play paketinin gerçek cihazda açılış/ödüllü reklam ve ödül akışı; 16 KB ortamda çalışma testi. Bu çalışmada ADB'ye bağlı cihaz bulunmadı.
2. Hedef kitle (önceden seçilen 5 yaş ve altı + 6–8), IARC ve reklam SDK'sına uygun veri güvenliği. Çocuk yaş grupları IARC tamamlanana kadar seçilemiyor; hedef yaşı bunu aşmak için büyütmeyin.
3. En az 12 gerçek test kullanıcısının Google hesapları; kapalı test yayını ve katılım bağlantısı.
4. Kesintisiz 14 gün sonrasında gerçek test sonuçlarıyla üretim erişimi başvurusu. İnceleme onayları beklenir; sonuç uydurulmaz.

## Mağaza görselleri

`PlayStoreCapture.Run` yalnız `MitziValidation` adlı izole projede çalışır; test kaydını sıfırlar. Unity'de 1920×1080 ev ekranı ve 512×512 simge çıktısı üretildi. 1024×500 özellik grafiği, oyunun mevcut arka planı ve Mitzi sprite'ı üzerine Unity UI yazısı eklenerek oluşturuldu. Yeni yapay zeka görseli üretilmedi. Çıktılar `Builds/Store/20261020/` içindedir; ayrıca mevcut Mitzi telefonu ekran görüntüsü kullanıldı. Bunlar gerçek cihaz performans testi değildir.

Mağazaya yüklenen simge, özellik grafiği ve ekran görüntüleri üretilmiş oyun sanatı içerdiği için ilgili AI öğe etiketleri işaretlendi. Telefon ekranındaki dört kullanıcı fotoğrafı ve profil fotoğrafı değiştirilmedi; etiket ekran kompozisyonundaki oyun sanatıyla ilgilidir.

Kaynaklar: [Kişisel hesap test şartları](https://support.google.com/googleplay/android-developer/answer/14151465?hl=tr), [SDK veri açıklaması](https://developers.google.com/admob/unity/privacy/play-data-disclosure), [Yaş işlemi](https://developers.google.com/admob/unity/targeting).
