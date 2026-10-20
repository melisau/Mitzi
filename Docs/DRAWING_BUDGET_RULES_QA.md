# Yol çizme bütçesi: sahne, görünüm ve bölüm — 2026-10-20

## Karar: bildirilen davranış tasarım gereği

Bildirimdeki 1 → 2 → 3, hazırlıktaki sağ/sol oklarla gezilen kamera görünümleriyse
ilk görünüm grubundaki hakkın tükenip üçüncü görünümde yeniden çizilebilmesi
mevcut tasarımla uyumludur. Yeni hak üretilmez; bölüm bütçesinin önceden ayrılmış
ikinci yarısı kullanılabilir olur. Bu karar için bütçe çalışma kodu değiştirilmedi.

Dayanaklar: mevcut `Mitzi_Oyun_Tasarimi_ve_Yayin_Plani_TR.md` çizerek oynama
bölümü yarım bütçe ayrımını anlatıyor; `LineDraw.AvailableInkForCurrentView` ve
`SideScrollCamera.IsSecondPreparationHalf` aynı kuralı uyguluyor.

## Kavram eşleştirmesi

- **Unity sahnesi:** etkin build listesinde tek sahne var: Assets/Scenes/PawPath.unity.
  Çizim oklarına basmak sahne yüklemez.
- **Çizim görünümü:** aynı bölümün kamera penceresi. HUD'daki `1 / 4` veya `1 / 5`
  gibi sayaç bu pencereleri gösterir; bölüm numarası veya yeni bütçe değildir.
- **Bölüm:** LevelManager'ın numaralı parkuru; başlatılması veya yeniden denenmesi
  yeni bir çizim denemesidir ve bütçeyi sıfırlar.
- **Tema/mekân:** Sokak, Orman vb. seçilen varlık grubudur; kamera sayacıyla aynı
  kavram değildir. Bölüm kurulurken tema/zorluk seçimi uygulanır.

## Bütçe ve açılma kuralı

Harcama çizginin girilen segment uzunluğu üzerinden dünya birimiyle hesaplanır;
çizgi adedi veya ekrandaki piksel uzunluğu değildir. Araçlar aynı havuzu kullanır.

Tam bölüm bütçesi `B = LevelDefinition.inkBudget × 2 × zorluk katsayısı`.
Katsayı kolayda 1,2; ortada 1; zorda 0,9. Tanım bulunmazsa LevelManager mevcut
FallbackLevel hesabını kullanır. Bütçe her bölümde tanımdan tekrar hesaplanır.

N görünüm varsa sıfır tabanlı kamera indeksi `floor(N / 2)` değerinden küçükken
ilk gruptadır. Bu grubun toplam harcama sınırı B/2'dir; **her görünüm için ayrı
B/2 verilmez**. Sonraki grup kalan toplam bütçeyi kullanabilir. İlk gruptan
harcanmayan miktar sonraki grupta kullanılabilir; ikinci gruptaki harcama ilk
grubun sınırını yükseltmez. Geri/ileri gezinmek hak üretmez.

Kontrol edilen yatay ekranlarda dört veya beş görünüm bulunur. İlk görünümde
ilk grubun B/2 hakkı tamamen tüketilirse:

| Geçiş | Toplam kalan | O görünümde çizilebilir | Sıfırlama |
| --- | --- | --- | --- |
| Bölüm başlangıcı, görünüm 1 | B | B/2 | Tam bütçe atanır; eski yollar silinir |
| Görünüm 1'de B/2 harcandı | B/2 | 0 | Yok |
| Görünüm 1 → 2 | B/2 | 0 | Yok; aynı ilk grup |
| Görünüm 2 → 3 | B/2 | B/2 | Yok; ayrılan ikinci yarı açılır |
| Görünüm 3 → 2 → 1 | Değişmez | İlk grubun kalan sınırı | Yok |

Örnek: B=40 ise ilk görünümde 20 kullanıldıktan sonra görünüm 2'de 0, görünüm
3'te 20 kullanılabilir. Toplam hak 40'ı aşmaz. “Üçüncü görünümde yenilenir” genel
bir sabit kural değildir: görünüm sayısı ekran oranına ve parkurun kamera aralığına
bağlıdır; grup sınırı yukarıdaki formülle belirlenir.

## Sıfırlama ve taşıma

| Eylem | Beklenen sonuç |
| --- | --- |
| Aynı bölümde kamera görünümü değiştir | Yollar ve iki grubun harcamaları korunur |
| Duraklat/devam et | Hak korunur |
| Yol sil | Gerçekte harcanan uzunluk ve ait olduğu ilk-grup payı iade edilir |
| Bütçenin tamamını tüket | Hazırlık biter, oyun başlar; çizim kapanır |
| Erken Başlat | Kalan hakla birlikte çizim kapanır; yeni bölüme birikmez |
| Aynı bölümü yeniden dene | Tam B atanır, ilk görünüm açılır, eski yollar silinir |
| Numaralı bölüm 1 → 2 → 3 | Her bölüm kendi tam B değeriyle başlar; tüketim taşınmaz |
| Eve dön, tekrar bölüme gir | Eski yollar temizlenir; bölüm girişinde tam B atanır |

LineDraw bütçesi/çizgileri oyuncu kaydında kalıcı stok olarak saklanmaz.

## Göstergeyi yorumlama

DrawingInkMeter toplam kalan `InkLeft / InkMax` oranını gösterir. Bu oran ilk
grupta o anda kullanılabilir `AvailableInkForCurrentView()` ile aynı şey değildir:
örneğin göstergede B/2 kalırken ilk grupta çizim hakkı 0 olabilir. Kamera sayacı da
yalnız görünüm numarasıdır. Mevcut fark bir tasarım/UX açıklık konusu olarak
kaydedildi; bu doğrulama kapsamında gösterge veya harcama sistemi değiştirilmedi.

## Tekrarlanabilir kontrol

Assets/Editor/DrawingBudgetTransitionValidation.cs yalnızca izole MitziValidation
projesinde çalışır. Gerçek LevelManager.BeginCurrentLevel, GameFlow yeniden deneme
ve SideScrollCamera.MovePreparationView yollarını kullanır.

- 1280×720, 1920×1080, 2340×1080, 2560×1600, 1024×768.
- Üç zorlukta numaralı bölüm 1 → 2 → 3 ve her birinde görünüm 1 → 2 → 3.
- İlk grubun tükenmesi, geri gezinme, silme iadesi, duraklatma, tam tüketim,
  yeniden deneme ve erken başlatma.
- Ek ekran oranlarında grup sınırının sabit görünüm numarası olmadığı kontrolü.
- Sayısal kanıt `transitions.csv` içinde bütçe, görünüm sayısı ve kullanılabilir
  hak olarak kaydedilir. Bu bir otomatik bütçe/akış kontrolüdür; cihazda baştan
  sona oynama testi değildir.

Sonuç: **45/45 bölüm-ekran-zorluk senaryosu ve 23 görünüm-grubu kontrolü geçti**
(`DRAWING_BUDGET_TRANSITION_PASS`). Örneğin orta zorlukta bölüm 1 için gerçek
tam bütçe 34,4; ilk görünümde 17,2 kullanıldığında görünüm 2'de kullanılabilir
hak 0, görünüm 3'te 17,2. Numaralı bölüm 2 tam 36,8 ve bölüm 3 tam 39,2 ile
başladı. Yeniden deneme, silme, geri gezinme, duraklatma ve erken başlatma
kontrolleri de geçti.

Görev sonucu **tasarım gereği** olarak değerlendirilebilir. Harici görev takip
aracının durumu bu çalışma sırasında değiştirilmedi.

## Kullanıcı isteğiyle yeniden inceleme — 2026-10-20

Son parkur/zemin değişikliklerinden sonra güncel LineDraw, SideScrollCamera,
GameFlow ve LevelManager kaynakları izole doğrulama projesine tekrar kopyalandı.
Aynı gerçek görünüm ve bölüm geçiş testi yeniden çalıştırıldı: **45/45 senaryo
ve 23 görünüm grubu kontrolü tekrar geçti** (`DRAWING_BUDGET_TRANSITION_PASS`).
Tek Unity sahnesi kullanıldığı ve mürekkep göstergesinin görünümde kullanılabilir
payı değil toplam kalan bütçeyi çizdiği de kaynakta yeniden kontrol edildi.
Bütçe yönetiminde bu yeniden inceleme için kod değişikliği yapılmadı.
