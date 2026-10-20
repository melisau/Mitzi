# Bölüm 30, köpek sesi/boyutu ve Mitzi'nin telefonu

## Bölüm 30 ve kutulu geçit güvenliği

Kutulu geçidin çıkışı ile çukur arasındaki eski 0,7 birimlik pay, kedinin eğilme bölgesinden çıkıp zıplaması için yetersiz kalabiliyordu. Yerleşim artık iki tarafta da en az 3,2 birim kesintisiz zemin ister. Giriş bölümlerinin üst üste gelen yol collider'ları kesintisiz zemin olarak birleştirilerek değerlendirilir; ilk bölümdeki geçit tanıtımı korunur.

Köpeklerin geçit sınırlarına ayırdığı güvenli paya 2 birim eklenmiştir. Yeni köpek yerleri bu koridora giremez. Dar bir çukurdan atlama kararı, inişin başka bir çukura veya geçidin korunan alanına denk gelmemesini de denetler. Her köpek aynı çukuru yalnızca bir kez atlar; böylece iki engel arasında sürekli atlayıp geri dönme döngüsü oluşmaz. Köpeklerin sadece normal dar çukurlardan atlayabilme kuralı korunur.

İzole Unity doğrulaması: 30. bölümde kolay/orta/zor × normal/büyük gösterim × çizerek/doğrudan kontrol = 12 baştan sona otomatik oyun geçişi başarılı. Bunlar gerçek CatController, köpek hareketi ve Physics2D üzerinden zıplama/eğilme girdileriyle, yol geometrisini değiştirmeden çalıştırıldı. 16 uzun köpek devriyesinde aynı atlayışın tekrarlanmaması kontrol edildi. Ek olarak 600 geçit yerleşimi, 48 fiziksel altından geçiş, 24 doğrudan kontrolde girdisiz durma ve 72 köpek çukur senaryosu geçti. Bu otomatik doğrulama, gerçek telefonda zorluk hissi değerlendirmesinin yerine geçmez.

## Köpek sesleri

- Normal köpek: kullanıcının `dog-bark-419014.mp3` dosyasından 1,032 saniyelik havlama.
- Robot köpek: `my-dog-the-robot.mp3` dosyasının ilk 2,5 saniyesi. Başlangıç ve bitişte kısa fade uygulanır.
- Kaynak indirmeler korunmuştur. `tools/prepare_dog_barks.py` mono 48 kHz PCM16 WAV üretir; kaynak kazancı -6 dB azaltılır. Çıktıların ölçülen tepe değerleri yaklaşık 0,429 ve 0,499; eklenmiş kırpılma yoktur.
- Oyun içi ayrı tek ses kanalı kazancı 0,20'dir. Aynı anda yalnızca bir köpek sesi duyulur; klip sonundan sonra en az 6 saniye sessizlik vardır.
- Ses yalnızca etkin oyun sırasında ve köpek oyun kamerasının görüşündeyken çalar. Köpek ekran dışına çıktığında, duraklatıldığında, hazırlık/ev ekranına geçildiğinde veya efektler kapatıldığında kesilir. Kameradaki yatay konuma göre hafif stereo yön verilir.
- Normal ve robot ses seçimleri, 2–3 saniye sınırı, düşük kazanç, ekran dışı/hazırlık/ev/duraklatma sessizliği ve eş zamanlı çalmama Unity'de doğrulandı. Fiziksel hoparlör/kulaklıkla bu yeni sesler henüz doğrulanmadı.

Dosyalar: `Assets/_Game/Resources/Audio/DogBark.wav`, `Assets/_Game/Resources/Audio/RobotDogBark.wav`.

## Robot köpek ölçeği

Önceki 1,95 birimlik tam görsel genişliği yerine 3,6 × CatScale kullanılır. Çarpışma kutusu ve çukur/yerleşim güvenlik payları bu genişlikle birlikte güncellenir. Idle ve yürüyüş sprite'larının farklı şeffaf ayak boşlukları ayrı hizalanır. Normal ve büyük gösterimde tüm katalog kedilerinin alfa üzerinden ölçülen görünen yüksekliğiyle karşılaştırılmıştır; robot köpek daha kısa değildir. Bu iki boyutta kamera karşılaştırmaları artefaktlara kaydedilir.

## Mitzi'nin telefonu

Kullanıcının seçimi: profil kartı ve Instagram'da aç düğmesi. Evde, bakiye/kedi hedefinin yanındaki renkli, Mitzi yüzlü ve isim etiketli 88 birimlik telefon simgesi 500×860 referans birimli mini ekranı açar. Kartta **Mitzi Tosuncuk**, **@egyptianmaumitzi** ve kullanıcının gönderdiği dört gerçek fotoğraftan 2×2 albüm vardır. Canlı akış, takipçi sayısı veya web oturumu taklit edilmez.

“Instagram'da aç” düğmesi önce yetişkinlere yönelik rastgele iki basamaklı çarpma sorusu açar. Yanlış/boş cevap bağlantıyı açmaz; yeni soru verir. Doğru cevap yalnızca ayrı bir “Instagram'da aç” adımını etkinleştirir. Oyuncu bu ikinci düğmeye basarsa işletim sistemine `https://www.instagram.com/egyptianmaumitzi/` bağlantısı gönderilir. İptal, telefonu kapatma veya evden çıkma onayı sıfırlar. Bu, uygulama giriş/SDK entegrasyonu gerektirmez. Telefon simgesi oyun/hazırlık/duraklatmada gizlenir; açık kart kapanır. Profil kimliğinin gizli kedi hedefiyle ilişkisi yoktur.

Türkçe/İngilizce/Almanca, 1280×720 / 2340×1080 / 1600×1200 ekran boyutları; metin taşması, aç/kapat, doğru hesap adresi, kedi hedefiyle çakışmama ve ev dışına geçişte kapanma doğrulanır. Test, harici Instagram bağlantısını kendiliğinden başlatmaz.

Güncel sonuç: `MITZI_PHONE_PASS localizedScreens=9 account=egyptianmaumitzi realPhotos=4 sharedAtlasMax=1024 lazyLoad=true parentGate=wrong-correct-cancel-close-state-passed externalUrl=validated-not-launched`. Profil ve ana ekran simgesi kamera görüntülerinden incelendi. Görseller ve test günlükleri `mitzi-phone-album` artefakt klasöründedir.

Fotoğraf kaynağı, kullanıcının 2026-10-20'de eklediği `codex-clipboard-1ff23c26-7a91-4593-9a31-3304f62bbbf6.png` ekran görüntüsüdür. Dosya byte düzeyinde değiştirilmeden `Assets/_Game/Resources/MitziPhone/mitzi-instagram-photos.png` olarak saklanır. Unity sprite metaverisi dört kare küçük görseli seçer; yüzler üretilmez/değiştirilmez. Tek atlasın ithalat üst sınırı 1024, mipmap kapalı, CPU okunabilirliği kapalı ve sıkıştırma açıktır. Telefon ilk açılışında kaynaklar yüklenir, tekrar açılışlarda aynı sprite referansları kullanılır. Editor Profiler ölçümü atlas için 2.394.792 bayttır (~2,28 MiB; hedef cihazda biçime göre değişebilir). Fiziksel mobil cihazda FPS ölçümü yapılmamıştır.

Ebeveyn kapısı eklenmiştir; mağaza hedef yaş/kategori seçimi ve yayındaki uygunluğu ayrıca değerlendirilmelidir. Referanslar: https://developer.apple.com/app-store/review/guidelines/#kids-category ve https://support.google.com/googleplay/android-developer/answer/9893335 . Bu teknik doğrulama mağaza onayı garantisi değildir.
