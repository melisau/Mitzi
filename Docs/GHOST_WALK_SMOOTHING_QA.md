# Ghost yürüyüşünün ara geçişleri

19 Ekim 2026. Boza'nın beğenilen yürüyüşü ve kaynak görselleri değiştirilmedi.

Ghost'un sekiz yürüyüş pozu arasındaki sert geçişler için yalnız Ghost'a uygulanan bir çizim katmanı eklendi. Önceki ve sonraki pozdaki pati hareketleri önceden hesaplanan iki yönlü hareket vektörleriyle eşleştirilir; ara poz her görüntü karesinde çizilir. Sadece saydamlıkla üst üste bindirme kullanılmaz. Gövdenin üst kısmı sabit kalır. Sprite boyutu, karakter konumu ve fizik çarpışmaları değişmez.

`GhostWalkSmoother` hem oynanan kediye hem evdeki Ghost'a bağlanır. Sadece yan yürüyüşte çalışır; ön/arka görünüm, zıplama, bakım, duraklatma ve kurtarma sarılması sırasında normal çizime dönülür. Boza veya diğer kedilere geçince devreden çıkar. Döngünün sonundan ilk poza geçiş de aynı yöntemle işlenir. Veri kaynak pozlarla uyuşmazsa normal sprite çizimi kullanılır.

Hareket verisi `tools/build_ghost_motion_vectors.py` ile kaynak PNG'ler okunarak hesaplandı; bu işlem sprite piksellerini değiştirmez. Yeniden üretmek için Python, NumPy, Pillow ve OpenCV gerekir. Çalışma zamanı OpenCV gerektirmez. Üretilen `Resources/Motion/GhostWalk.bytes` dosyası sürüm kontrolündedir. Kaynak sprite dilimleri veya pivotları değiştirilirse veri yeniden üretilmelidir.

`GhostWalkSmoothingValidation.Run`, izole `MitziValidation` projesinde başarılı: gerçek shader çıktısında ara pati konumu, üst gövde opaklığı, sekiz geçişin hareket verisi, döngü birleşimi, fiziksel hizalama, ön/arka görünümün ve sarılmanın kaynak çizimi gizleyebilmesi, zıplamaya dönüş ve Boza'nın etkilenmemesi kontrol edildi. Shader derlemesi doğrulandı. Render önizlemeleri ve loglar yerel, ignore edilen `AnimalArtSources-20261018` klasöründedir.

Yeni Android paketinin fiziksel telefonda akıcılık değerlendirmesi henüz yapılmadı. Ara hareketlerin görsel niteliği ve cihaz performansı telefonda da değerlendirilmelidir.
