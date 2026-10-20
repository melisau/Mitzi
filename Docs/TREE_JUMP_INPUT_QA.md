# Zıplama / tırmanma ayrımı ve geçit etiketi — 2026-10-20

> Bu belge önceki ayrı giriş denemesinin kaydıdır. Sonraki açık kullanıcı kararı
> zıplama tuşunun gövdede tırmandırması ve ağaçların çukurlardan uzak tutulmasıdır.
> Güncel kural ve görsel düzeltme: FOREST_SURFACE_TREE_SPACING_QA.md. Aşağıdaki
> ayrı E/↑ tuşu ayrımına ilişkin sonuçlar güncel kontrol kuralı değildir.

## Sorun ve yeni davranış

ClimbableTree önce JumpQueued ve W/yukarı ok girdisini CatController'dan önce
okuyup tırmanmaya dönüştürüyordu. Ekran görüntüsündeki 5. bölüm örneğinde olduğu
gibi gövde menzili çukur yaklaşımıyla kesiştiğinde oyuncunun atlama isteği kayboluyordu.

- Sağ pati yukarı, mobil ZIPLA ve klavyede W/Space her konumda zıplama
  girdisi olarak kalır; mevcut tavan güvenliği ve çift zıplama kuralları korunur.
- Tırmanma gövde menzilinde, yerde dururken **yeşil Tırman işaretine dokunarak**
  veya klavyede **↑ (yukarı ok)** ile başlatılır. Sonraki kullanıcı isteğiyle E
  yerine yukarı ok bağlandı. Yukarı ok menzil dışında eski zıplama davranışını
  korur; çukur atlayışında Space/W veya mobil ZIPLA kullanılmalıdır. Erken/uzak,
  havadayken, Build veya duraklatma
  sırasında yapılan istek saklanmaz; daha sonra kendiliğinden tırmanılmaz.
- Aynı anda zıplama ve tırmanma istenirse zıplama önceliklidir.
- Yeşil yazı/ok üzerinde 2×1,1 dünya birimlik şeffaf World Space Canvas UI hedefi
  vardır. Collider eklemez; uygun değilken UI raycast hedefi kapatılır.
- Eski gövde temasının çıkış bildirimi gecikse bile güncel temas sınırları kontrol
  edilir; başka konumdan ağaca çekilme yapılmaz.
- TR/EN/DE dersler ve animasyon artık sağ patiyi yukarı itmeyi değil, ağacın
  yeşil işaretine dokunmayı gösterir. Tepede +5 ödül ve ayrılma davranışı korunur.

**Altından geç** yazısının koyu hap arka planı kaldırıldı. İki etiket de açık
yazı + küçük koyu gölge kullanır; mavi aşağı / yeşil yukarı ok ayrımı korunur.
Mavi işaret yönlendirmedir, yeni bir dokunma eylemi değildir.

## Doğrulama

Assets/Editor/TreeJumpSeparationValidation.cs izole MitziValidation projesinde
1, 5, 21 ve 25. bölümlerde, iki mod ve üç zorlukta üç yaklaşma konumunu dener.
  Mobil ve klavye zıplama kuyruğu ayrı kontrol edilir. CatController.FixedUpdate
sonrasında kedinin yukarı hız aldığı ve tırmanmadığı doğrulanır. Ayrı düğme gerçek
EventSystem UI tıklamasıyla denenir; 1280×720, 2340×1080, 1024×768 ve 720×1280
pencerelerde raycast hedefi kontrol edilir. Sonuncusu üretim yönü desteği iddiası
değil, dar pencere için ek kontroldür.

Ayrıca havadaki/uzaktaki istekler, duraklatma, eş zamanlı girdiler, collider
eklenmemesi ve panelin iki etikette de bulunmaması kontrol edilir. Mevcut ağaç
ödülü ve orman etkileşim testleri yeni açık tırmanma eylemiyle tekrar çalışır.

Bu otomatik sahne/girdi doğrulamasıdır; gerçek telefondaki dokunma rahatlığı ve
çukur atlayışı ayrıca oynanarak doğrulanmalıdır.

Sonuç: **144 zıplama**, **24 ayrı UI tırmanma tıklaması**, dört ekran biçiminde
iki mod için **8 raycast** kontrolü geçti (`TREE_JUMP_SEPARATION_PASS`). +5 ödül
testi altı ağaçta birer ödeme ve 600 tekrarda ek ödeme olmamasıyla tekrar geçti.
Orman etkileşimi kontrolü de 120 yerleşim, 270 açık tırmanma isteği ve üç dilde
ders sığmasıyla geçti. Oyun kamerasında “Altından geç” panelinin kaldırıldığı ve
tırmanma dersinin yeni dokunmayı anlattığı görsel olarak kontrol edildi.
