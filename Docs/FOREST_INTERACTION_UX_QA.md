# Ormanda eğilme ve tırmanma ayrımı — 2026-10-20

## Tasarım kararı

İki farklı ağaç işlevi korunur. Yatay, alçak dalı olan CrouchPassage bir geçiş
engelidir; tırmanılmaz. Ayrı ClimbableTree dik gövdesinden tırmanılabilen, köpekten
korunma noktasıdır. Her ağacı tırmanılabilir yapmak mevcut geçiş tasarımını değiştirir.

Oyuncu yakınken düşük geçitte **mavi aşağı ok + Altından geç**, tırmanma ağacında
**yeşil yukarı ok + Tırman** gösterilir. Rengin yanında yön ve eylem sözcüğü de
ayrımı anlatır. İşaretler fizik collider'ı eklemez, boyutları ağaç görselinin
ölçeğinden bağımsızdır. Yeşil tırmanma işareti artık ayrı bir UI dokunma hedefidir;
mavi geçiş işareti yalnız yönlendiricidir. Başka bir etkileşimde veya Build/duraklatma
durumunda tırmanma başlatılmaz; aktif tırmanışta işaret gizlenir.

Gövde menzilinde yerde dururken yeşil **Tırman** işaretine de dokunulabilir;
bu yardımcı bir seçenek olarak kalır. Son kullanıcı kararıyla asıl
tırmanma girdisi yine sağ pati yukarı / ZIPLA / W / yukarı ok / Space'tir.
Çukur yaklaşımında aynı girdinin yanlışlıkla tırmanmaması için ağacın bütün
etkileşim menzili ile çukur arasında en az 3,5 birim boş koşu alanı bırakılır.
Uygun geniş zemin yoksa ağaç o yerleşime eklenmez. Tırmanış boyunca ve
tepede köpek kediyi yakalayamaz; tepede yeniden yukarı ile zıplayarak ayrılabilir.
Çizerek oynamada da gövdede aynı zıplama/tırmanma girdisi kullanılır. Alçak geçitte bu
modun mevcut otomatik eğilmesi korunur; doğrudan kontrolde sağ pati aşağı tutulur.

## Bulunan nedenler ve düzeltmeler

- Eski ders yalnız “ağacın yanında” diyordu; iki görselin farklı işlevini belirtmiyordu.
  TR/EN/DE dersleri yeni renk/yön işaretleriyle ve eylemlerle eşleştirildi. Eğilme ve
  tırmanma animasyonlarına aynı yön işaretleri eklendi.
- Köpek mesajı tırmanma ağacı olmayan bölümlerde de ağaca çıkmayı öneriyordu.
  Artık yalnız etkin ClimbableTree varsa işaretli ağacı önerir; yoksa köpeğin üstüne
  zıplama anlatılır. Yerleşimde her dördüncü bölümde ağaç bulunmaması korunur.
- Kedi ayak ve üst gövde olmak üzere birden fazla collider kullanıyor. Bunlardan
  birinin trigger'dan çıkması diğer temas sürerken tırmanmayı iptal edebiliyordu.
  Temaslar ayrı takip edilir; kalan temas tırmanma menzilini korur. Devre dışı
  collider'lar ve kapanan ağaç temizlenir. Build girdisi tırmanmaya dönüşmez.

## Doğrulama

Assets/Editor/ForestInteractionUxValidation.cs izole MitziValidation projesinde:

- Orman varlıklarıyla 1–20. bölümler, üç zorluk ve iki oyun modu: 120 yerleşim.
- Gövdenin solunda/ortasında/sağında üç yaklaşım; gerçek giriş menzili, yukarı
  girdisinin tüketilmesi, yükselme, tepede korunma ve zıplayarak ayrılma.
- Bir collider çıkarken diğerinin içeride kalması, Build durumunda tırmanmama,
  ağaçsız bölümde yanlış öneri verilmemesi, mavi/yeşil işaretlerin ayrılığı.
- TR/EN/DE ders metinlerinin alana sığması; oyun kamerasından geçit, tırmanma
  ağacı ve ders görüntüleri. Oyun kamerasının 4,45 ortografik boyutu kullanılır.

Etkileşim sonucu: **120 yerleşim, 270 tırmanma ve 30 ağaçsız bölüm önerisi geçti**;
üç dilde metin sığma kontrolü de geçti (`FOREST_INTERACTION_UX_PASS`).
Ayrıca önceki ağaç-zemin doğrulaması tekrar geçti: 300 yerleşim, 225 ağaç,
sekiz yükseklik/tümsek durumu, üç tırmanış ve 16:9 / 19,5:9 kamera
(`FOREST_TREE_GROUNDING_VALIDATION_PASS`).

Bu kontroller otomatik sahne/etkileşim doğrulamasıdır; tüm bölümleri fiziksel bir
telefonda baştan sona oynama veya kullanıcılarla yeniden anlaşılabilirlik testi
yerine geçmez. Gerçek cihazda mavi geçide yaklaşma/eğilme, yeşil gövdeye gelip
tırmanma, köpeğin geçmesini bekleme ve ağaçtan ayrılıp bitişe ilerleme akışı ayrıca
oynanmalıdır.
