# Mitzi — Play Console beyan hazırlığı

19 Ekim 2026. Kullanıcının seçtiği hedef kitle: **5 yaş ve altı + 6–8 yaş**. Bu belge hazırlanmıştır; Console'a kaydedilmiş bir başvuru değildir.

## Console'daki durum

Hesaba giriş yapıldı. Henüz uygulama taslağı yok. `Uygulama oluştur` düğmesi **“Uygulama oluşturmak için hesap doğrulama işlemlerini tamamlayın”** mesajını gösteriyor. Kimlik incelemesi ve sonraki telefon doğrulaması tamamlanınca uygulama oluşturulabilir. Mevcut engel nedeniyle aşağıdaki alanlar Console'a gönderilmedi.

## Hazır bilgiler

| Alan | Mitzi için bilgi |
|---|---|
| Uygulama adı / türü | Mitzi / Oyun |
| Dil | Türkçe |
| Gizlilik politikası | https://melisau.github.io/Mitzi/privacy.html |
| Gizlilik iletişimi | melisauyar5225@gmail.com |
| Hedef yaş | 5 yaş ve altı, 6–8 yaş; kullanıcı onayladı |
| Hesap / oturum açma | Yok; inceleme için kullanıcı adı veya parola gerekmiyor |
| Kullanıcı içeriği, sohbet, kullanıcılar arası iletişim | Yok |
| Gerçek para ile uygulama içi satın alma | Yok; eşya alışverişi oyun içinde kazanılan puanla |
| Kumar / gerçek para ödülü | Yok |
| Kamera, mikrofon, kişiler, GPS verisi | Oyun bu verileri istemiyor |
| Oyun ilerlemesi ve ayarlar | Cihazda PlayerPrefs; geliştiricinin sunucusuna aktarım bulunmadı |
| Unity Analytics / bulut tanılama | Proje ayarlarında kapalı |

Gizlilik sayfası giriş gerektirmeden tarayıcıda açılarak doğrulandı. Oyun içinde Ayarlar > Gizlilik altında çevrimdışı metin ve aynı HTTPS sayfasını açan `Web'de aç` düğmesi bulunur. İlk yayın paketi bu düğme eklendikten sonra üretilmelidir; önceki APK'lar bu değişikliği içermez.

İzole Unity doğrulaması başarılı: gizlilik ekranı, kaydırma ve kapatma; HTTPS web düğmesinin kullanılabilirliği; güncel pati simgesi; reklamsız AAB/export önkoşullarının kabul edilmesi; gizlilik URL'si yokken veya reklam açıkken gerçek birim kimliği eksikken derlemenin engellenmesi. Log: `AnimalArtSources-20261018/unity-play-privacy-final-validation.log`. Yeni AAB veya APK bu çalışma kapsamında üretilmedi.

## Reklam beyanı

Mevcut `ReleaseConfiguration.advertisingEnabled=false`; normal sürümde yönetilen reklam başlatma ve yükleme yolu çalışmıyor. Paket Google Mobile Ads SDK'sını, Android `play-services-ads:25.5.0` ve UMP bağımlılıklarını içeriyor. İlk sürümün reklamsız mı, isteğe bağlı ödüllü reklamlı mı olacağı kullanıcıya soruldu; seçim henüz yanıtlanmadı.

- Ödüllü reklam etkin yayımlanırsa **Reklam içerir: Evet**. İsteğe bağlı olması bu yanıtı değiştirmez.
- Reklamsız yayımlanırsa **Reklam içerir: Hayır**, ancak önce son pakette reklam sunulmadığı doğrulanmalı. Bu seçenek tek başına veri toplanmadığı anlamına gelmez.
- Reklamlı sürümde çocuklara uygun SDK sürümü, Families koşulları ve reklam içeriği doğrulanmalı. Mevcut kod Child işlemi ve G sınırı uygular; AD_ID manifestten çıkarılmıştır. Bunlar tek başına bütün veri işlemeyi ortadan kaldırmaz.

## Veri güvenliği — henüz kesinleştirilmedi

GMA SDK dokümanı varsayılan olarak IP adresi, uygulama/reklam etkileşimleri, tanılama ve cihaz/hesap tanımlayıcılarının toplanıp paylaşıldığını belirtir. Bu nedenle SDK paketteyken yalnız reklam düğmesinin kapalı olmasına dayanarak **“Veri toplanmıyor/paylaşılmıyor” seçilmez**.

Reklamlı paket için kontrol edilecek türler: yaklaşık konum (IP'den tahmin), uygulama etkileşimleri, uygulama performansı/tanılama, cihaz veya diğer kimlikler. SDK belgesindeki amaçlar reklam, analiz ve kötüye kullanım önlemedir; aktarım TLS ile şifrelidir. Her alanın zorunluluk, geçici işleme, toplama/paylaşma ve silme yanıtı son yapılandırmaya göre ayrıca belirlenir; tümüne otomatik aynı yanıt verilmez.

Reklamsız pakette SDK'nın bulunması/başlangıç davranışı ayrıca incelenmeli veya reklamsız dağıtımda SDK bağımlılıkları dışlanıp son paket doğrulanmalıdır. Hangi profil seçilirse gizlilik metni ve veri güvenliği formu onunla tutarlı hâle getirilmeli. İşletim sistemi yedekleri ve destek e-postası, oyun içi hesap/bulut kayıt sistemiyle karıştırılmaz. Oyun hesabı olmadığından hesap silme mekanizması yoktur; yerel kayıt Android uygulama depolamasından silinir.

## İçerik derecelendirmesi

Anket türü Oyun; iletişim e-postası yukarıdaki adres. Kan, grafik yaralanma, cinsel içerik, küfür, uyuşturucu veya kumar işlevi bulunmadı. Ancak **köpeğe yakalanma, çukura düşme, tehlikeli bölgelere temas ve bölüm başarısızlığı** vardır. Şiddet/korku/tehdit sorularına bütün başlıkları otomatik “Hayır” seçerek yanıt verilmemelidir. Kesin sorular Console anketi açılınca mevcut görüntü, ses ve oynanışa göre cevaplanır. Resmî bölgesel yaş derecesini IARC üretir; PEGI 3 veya başka bir sonuç önceden atanmaz.

5 yaş ve altı seçimi kullanıcının tasarım hedefidir; uygunluk onayı değildir. Küçük çocukların hızlı tepki ve yol çizme gereksinimi, köpek/tehlike sahneleri, metin bağımlılığı ve başarısızlık geri bildirimiyle oynayabildiği test edilmelidir. Google'ın rehberi bu yaşta hızlı tepki ve tehlikedeki karakterlerin uygun olmayabileceğini belirtir. Gerekirse oyunun kolaylığı ve sunumu bu hedefe uyarlanır.

## Kimlik onayından sonra sıra

1. Telefon dahil kalan hesap görevlerini tamamla ve Mitzi uygulama taslağını oluştur.
2. Gizlilik URL'sini ve uygulama erişimi bilgisini kaydet.
3. Kullanıcının reklam kararına ve doğrulanmış son pakete göre reklam/veri güvenliği beyanlarını tamamla.
4. İçerik derecelendirme anketini ve seçilmiş hedef kitle alanlarını doldur; özetlerini kontrol et.
5. Doğru beyanlarla eşleşen imzalı AAB'yi yükle ve kapalı teste geç.

Kaynaklar: [Yayın hazırlığı](https://support.google.com/googleplay/android-developer/answer/9859455?hl=tr), [Hedef yaş](https://support.google.com/googleplay/android-developer/answer/9867159?hl=tr), [Veri güvenliği](https://support.google.com/googleplay/android-developer/answer/10787469?hl=tr), [AdMob veri açıklaması](https://developers.google.com/admob/unity/privacy/play-data-disclosure), [İçerik derecelendirmesi](https://support.google.com/googleplay/android-developer/answer/9898843).
