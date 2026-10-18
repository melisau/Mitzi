# Mitzi — AdMob kurulumu

Bu hesap geliştiriciye ait olmalıdır. Kimlik, ödeme ve vergi bilgilerini yalnız geliştirici Google arayüzünde tamamlar; bu bilgileri sohbet veya GitHub'a yazmayın.

1. [AdMob](https://admob.google.com/) üzerinden Google hesabınızla kaydolun ve gerekli hesap doğrulamalarını tamamlayın.
2. Uygulamalar > Uygulama ekle > Android. Henüz mağazada değilse yayımlanmamış uygulama seçeneğini kullanın. Adı Mitzi; Android paket adı **com.mitzicat.game**.
3. Uygulama ayarlarından `ca-app-pub-…~…` biçimindeki **uygulama kimliğini** kopyalayın.
4. Reklam birimleri > Ekle > **Ödüllü** seçin. Ödüllü geçiş reklamı, banner veya uygulama açılış reklamı seçmeyin. Birimin adı `Mitzi_Shopping_Reward`; ödül 5 oyun içi alışveriş puanı. Oluşan `ca-app-pub-…/…` kimliğini kopyalayın.
5. Çocuklara yönelik en sıkı içerik/kişiselleştirme ayarlarını uygulayın; G içerik sınırını koruyun. Mediation/reklam ağı eklemeyin; eklenen her SDK ayrıca denetlenmelidir. Etkileşimli reklamların küçük çocuklar açısından uygunluğunu değerlendirin.
6. Unity'de `Assets/_Game/Resources/ReleaseConfiguration.asset` dosyasına Android App Id ve Android Rewarded Ad Unit Id değerlerini yazın. Bunlar parola değildir. Geliştirme sürümleri test birimi kullanır; normal sürüm eksik gerçek kimliği test kimliğiyle doldurmaz.
7. Gizlilik metnini son reklam davranışıyla karşılaştırıp onaylayın; halka açık HTTPS adresini Privacy Policy Url alanına yazın. Ancak bundan sonra Privacy Policy Reviewed ve Advertising Enabled alanlarını etkinleştirin.
8. **Mitzi > Android > Validate Release Setup** ardından **Build Signed AAB** çalıştırın. Araç Google Mobile Ads uygulama ayarını gerçek kimlikle eşleştirir; eksik politika/gerçek kimlik varsa mağaza derlemesini reddeder.
9. Play kaydı açılınca uygulamayı AdMob'a bağlayın; AdMob uygulama incelemesi ve gerekiyorsa app-ads.txt/site doğrulamasını tamamlayın. Kimliklerin bulunması reklam yayınının/onayın/gelirin başladığı anlamına gelmez.

## Çocuk ve mağaza beyanları

Mevcut oyun küçük çocukları da hedefler; tasarım görüşü 4–5 yaş için kolay modla başlamak yönündedir. Play Console'daki **5 yaş ve altı** hedef grubunu geliştirici değerlendirmelidir. 6–8 yaş da pazarlama hedefiyse bu grup ayrıca seçilir; hedeflenmeyen yaşları erişim artırmak için seçmeyin. IARC içerik derecesi, yaş hedefi ve oynanış zorluğu farklı şeylerdir.

Kod bütün reklam isteklerinde Child yaş işlemi ve G içerik sınırlaması kullanır. Android AD_ID izni kaldırılır. Bu ayarlar hiçbir teknik veri toplanmadığını veya mağaza onayını tek başına kanıtlamaz.

## Resmî kaynaklar

- [AdMob uygulaması ekleme](https://support.google.com/admob/answer/9989980)
- [Ödüllü reklam birimi oluşturma](https://support.google.com/admob/answer/7311747)
- [Unity reklam hedefleme/yaş işlemi](https://developers.google.com/admob/unity/targeting)
- [Google Play Families](https://support.google.com/googleplay/android-developer/answer/9893335)
- [Families SDK programı](https://support.google.com/googleplay/android-developer/answer/12955712)

## Kurulumda karşılaşılabilecek ekranlar

**Ödeme ayarları tamamlanmadı:** Bu ücret ödeme talebi değildir. AdMob kazancı alacak kişi veya kuruluşu doğrulamak için ödeme profili ister. Kendi adına açılan hesapta Bireysel seçilir; gerçek ad ve doğru ödeme adresi yalnız Google ekranına girilir. Banka hesabı ekleme, kazancın yatırılmasına ilişkin sonraki ödeme adımıdır. Kayıt/aylık AdMob kullanım ücreti yoktur. Play Console geliştirici kaydı ayrı olup bir defalık 25 ABD dolarıdır.

Gizlilik bağlantısı hazır: https://melisau.github.io/Mitzi/privacy.html . Bu GitHub Pages sitesinin gh-pages dalında yalnız gizlilik sayfası bulunur. Kaynak güncellenirse yayımlanan kopya da güncellenmelidir.

app-ads.txt için AdMob'un verdiği yayıncı satırı gerekir. Google, geliştirici web sitesinin alan adı kökünü tarar; proje altındaki `/Mitzi/app-ads.txt` tek başına yeterli değildir. Yayıncı kimliğiyle dosya https://melisau.github.io/app-ads.txt adresinde yayımlandı ve HTTP 200 ile doğrulandı. Play Console geliştirici web sitesi alanı https://melisau.github.io/ olmalıdır. AdMob taraması/onayı henüz doğrulanmadı.

## Play Console için mevcut uygulamaya göre hazırlık

- Gizlilik politikası: yukarıdaki HTTPS adresi.
- Uygulama erişimi: giriş/üyelik yok; tüm işlevler için hesap gerekmez.
- Reklam içerir: gerçek reklamlı son sürüm için Evet; kapalı test adayı ile son yayın davranışını karıştırmayın.
- Hedef kitle: 4–5 yaş hedefi için 5 yaş ve altı grubunu değerlendirin; 6–8 yaş da hedefleniyorsa ayrıca seçilir. Nihai seçimi geliştirici yapar.
- İçerik derecelendirmesi: kedi-köpek çarpışmaları, köpeğin üzerine atlama ve kaybetme dahil gerçek oynanışa göre IARC sorularını yanıtlayın; derecelendirmeyi sistem verir.
- Veri güvenliği: Google SDK veri tablosunu son reklam davranışıyla eşleştirin. Reklam kimliği izni kaldırıldı diye bütün veriler için Hayır demeyin. Ayrıntılar `PLAY_DATA_SAFETY_TR.md`.
- Bu notlar form hazırlığıdır; Play Console'da gönderim yapılmamıştır.

Kaynaklar: [AdMob kayıt ve ödeme profili](https://support.google.com/admob/answer/7356219), [Play kayıt ücreti](https://support.google.com/googleplay/android-developer/answer/6112435).

Android uygulama kimliği ve ödüllü reklam birimi kullanıcıdan alınıp ReleaseConfiguration ve Google Mobile Ads uygulama ayarına eklendi. Reklamlar hesap/uygulama incelemesi ve son çocuk/veri güvenliği kontrolü tamamlanana kadar kapalıdır. Kimlikler reklam yayını veya gelir onayı değildir.
