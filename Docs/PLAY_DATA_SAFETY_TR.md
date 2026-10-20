# Mitzi — yayın ve veri güvenliği çalışma notları

20 Ekim güncellemesi: Hesap doğrulandı; reklamlı 1.0.0 (1) dahili testte etkin. Veri Güvenliği formu Console'da kaydedildi; bu, Google inceleme onayı değildir. Kullanıcı mevcut oyunu koruyarak yalnız 6–8 hedef grubunu seçti; IARC/PEGI sonucu 7+. Aşağıdaki eski hazırlık notları tarihsel bağlamdır.

## 20 Ekim Console beyanı

- Google Mobile Ads Android 25.5.0 için yayımlanmış SDK veri açıklaması ve projedeki başlatma akışı esas alındı; fiziksel cihaz ağ yakalaması yapılmadı.
- Dört veri türü hem toplanan hem paylaşılan olarak seçildi: yaklaşık konum (IP üzerinden), uygulama işlemleri, teşhisler, cihaz veya diğer kimlikler.
- Her tür için analiz, reklam veya pazarlama, sahtekarlığı önleme/güvenlik/kanunlara uygunluk amaçları seçildi. Kişiselleştirme veya geliştirici iletişimleri seçilmedi.
- Kısa süreli işleme Hayır; kullanıcı tarafından kapatılamayan toplama. Reklam izlemek isteğe bağlı olsa da SDK açılışta başlatılıp reklam önceden yüklendiğinden veri toplama isteğe bağlı olarak beyan edilmedi.
- Aktarımda şifreleme Evet (SDK TLS belgesi). Uygulamada hesap oluşturma/giriş yok. Geliştiricinin sunduğu uzaktan veri silme talep yöntemi yok; 90 günde silme sözü verilmedi. Yerel kayıt Android depolama ayarlarıyla silinebilir.
- Play Aile Politikası taahhüdü Evet; bağımsız güvenlik denetimi iddia edilmedi. AD_ID izninin kaldırılması SDK'nın tüm tanımlayıcıları işlemediği anlamına gelmez.
- Kullanıcının yerel ilerlemesi, birlikte paketlenen Mitzi fotoğrafları ve ses dosyaları kullanıcı yüklemesi değildir; bunlar için ek veri toplama seçilmedi.

Kaynak: [Google Mobile Ads Unity veri açıklaması](https://developers.google.com/admob/unity/privacy/play-data-disclosure), [Play Veri Güvenliği tanımları](https://support.google.com/googleplay/android-developer/answer/10787469?hl=tr).

18 Ekim 2026. Bu bir Play Console başvurusu veya uygunluk sertifikası değildir. Beyanları geliştirici, son paketin davranışını inceleyerek tamamlar.

## İncelenen yapı

- Android paket: `com.mitzicat.game`; Unity 2022.3.62f3; IL2CPP; ARM64; hedef API 36; minimum API 24.
- Hesap, sohbet, kullanıcı içeriği, uygulama içi satın alma, kendi sunucumuza ilerleme gönderimi veya ayrı analytics paketi bulunmadı.
- Oyun kaydı ve ayarlar cihazdaki PlayerPrefs içinde; işletim sistemi yedekleri ayrı yönetilir.
- Google Mobile Ads Unity 11.6.0; Android Maven bağımlılığı `com.google.android.gms:play-services-ads:25.5.0`; UMP 4.0.0. Mediation adaptörü yok. Families listesi/sürümleri son paket için ayrıca kontrol edilir.
- GMA Child yaş işlemi + G reklam içeriği; AD_ID izni manifestten çıkarılır. Reklam kimliği izninin çıkarılması bütün cihaz tanımlayıcılarını veya SDK veri işlemesini ortadan kaldırmaz.
- Gerçek Android uygulama ve ödüllü reklam kimlikleri kullanıcı tarafından sağlandı ve projeye eklendi. İmzalı cihaz test adayı gerçek reklamları kapalı tutar. Son paket üzerinde ağ davranışı cihazla doğrulanmadan 'veri toplanmıyor' seçeneği kullanılmamalıdır.

## Veri türleri — son reklam yapılandırmasıyla doğrulanacak

| Alan | İnceleme |
|---|---|
| Yaklaşık konum | SDK'nın IP adresinden tahmin edebildiği genel konum; uygulama GPS izni istemiyor. |
| Uygulama etkinliği | Reklam gösterimi/tıklama/video izleme ve SDK uygulama etkileşimi. |
| Uygulama bilgisi/performans | SDK tanılama, açılış ve performans bilgileri. |
| Cihaz veya diğer kimlikler | AD_ID çıkarılmış olsa da App Set ID gibi tanımlayıcıların kullanımını doğrulayın. |
| Amaçlar | Google SDK belgesine göre reklam, analiz ve kötüye kullanım önleme; nihai ayarlara göre yanıtlanır. |
| Aktarım şifreleme | Google SDK verileri için TLS belgeleniyor. Destek e-postası ayrı hizmettir. |
| Silme | Yerel veriler Android uygulama depolamasından silinir; yedekler ayrıca yönetilir. Google verisinin silinmesi/saklanması Google politikasına tabidir. |

## Yayın öncesinde geliştiricide kalan işler

- Gizlilik politikası kullanıcı onayıyla yayımlandı ve giriş gerektirmeden HTTP 200 ile doğrulandı: https://melisau.github.io/Mitzi/privacy.html . Uygulamada çevrimdışı metin de bulunur. Reklam yapılandırması değişince metni yeniden karşılaştırın.
- AdMob hesabı ve uygulama/reklam birimi oluşturma: `ADMOB_SETUP_TR.md`.
- Play Console hedef yaş, içerik derecesi, reklam içerir, veri güvenliği ve gizlilik adresi alanlarını tamamlayın. Gerekli geliştirici/hesap doğrulamasını yapın.
- Android telefonda kurulum, çevrimdışı ilk açılış, kayıt, tutorial, tüm temalar, reklam kapalı/açık durumları ve yaşam döngüsünü test edin. Android 16 telefonda kurulum ve ilk bölüm öğreticisi açılış kontrolü başarılı; kapsamlı oynanış testi bekliyor.
- 16 KB emulator/cihazda açılışı doğrulayın. ELF LOAD ve GNU_RELRO segmentleri, APK ZIP hizalaması/yerel kütüphane sıkıştırması ve AAB yapı/signature raporunu birlikte değerlendirin.
- Anahtarı ve parolalarını güvenli yedekleyin. Windows şifreli XML yalnız aynı kullanıcı/makineyle çözülebilir; bu dosya tek başına taşınabilir parola yedeği değildir. Anahtar repoya konmaz.

Kaynaklar:
[SDK veri açıklaması](https://developers.google.com/admob/unity/privacy/play-data-disclosure), [Play veri güvenliği](https://support.google.com/googleplay/android-developer/answer/10787469), [Families](https://support.google.com/googleplay/android-developer/answer/9893335), [16 KB doğrulama](https://developer.android.com/guide/practices/page-sizes).

## Geliştirici adı ve gizlilik metni

Kullanıcının tercihiyle gizlilik metnindeki ad-soyad kaldırıldı; uygulama Mitzi adıyla tanımlanır ve mevcut gizlilik iletişim adresi korunur. Google Play gizlilik politikası uygulama adını kullanmaya izin verir. Bireysel Play Console hesabının mağaza profilinde yasal ad, ülke ve geliştirici e-postasının ayrıca gösterilmesi farklı bir zorunluluktur. Gizlilik sayfası kullanıcı onayıyla yayımlanmıştır: https://melisau.github.io/Mitzi/privacy.html .

Kaynaklar: https://support.google.com/googleplay/android-developer/answer/10144311 ve https://support.google.com/googleplay/android-developer/answer/13628312
