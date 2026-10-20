# Köpek çukur kuralı ve yeni eşya yerleşimi

## Köpekler

Normal çukurlarda üst atlama sınırı 2,60 dünya birimidir. Mevcut deterministik atlama / durup geri dönme davranışı bu sınırın içinde korunur. Raflı çukur türü ayrıca köpeğe iletilir; raf sayısı 1, 3 veya 4 olsa da köpek o çukuru atlayamaz. Kenarda 0,65 saniye bekleyip geri döner. Önceki dar atlayışın inişi bir sonraki çukurun durma payına denk gelirse de yeni çukur atlanmaz; güvenli kenara çekilip geri dönülür.

`DogGapAndFurnitureValidation.Run` 72 karar senaryosunda dar / geniş / raflı tür izni, eski Configure çağrıları için boyut sınırı, iki yön, atlama ve geri dönüş seçimini kontrol eder. Dar atlayışların inişi ve bekleme sonrası geri dönüş kontrol edilir. Gerçek kurucunun ürettiği 47 köpeğin çukur izinleri de doğrulanır.

## Yeni eşyalar

İlk boş yer araması eskiden en yukarıdaki geçerli konumdan başladığı için eşyalar süpürgeliğin hemen önünde görünüyordu. İlk aday artık eşyanın pivotundan bağımsız gerçek görsel alt kenarını `HomeRooms.FloorMaxY` (-2,75) hizasına getirir; bu, kedi ağacı için önceki -2,00 civarı hizadan yaklaşık 0,75 birim aşağıdadır. Yer doluysa en yakın geçerli satır ve sütunlar denenir; çakışma ve oda sınırı kuralları korunur.

Posterler duvarda kalır; halılar bütünüyle zemine sığdırılır. Kaydedilmiş ve oyuncunun elle taşıdığı geçerli yerleşimler yeniden sıralanmaz. Değişiklik yeni eşya eklenirken veya geçersiz konum yeniden yerleştirilirken uygulanır. Daha önce kaydedilen eşya sürüklenerek aşağı taşınabilir.

İki odada sekiz eşya türüyle 16 ilk yerleştirme, tekrar Refresh, elle taşıma sonrası kayıt ve poster/halı sınırları kontrol edilir. Kamera görüntüsü `varied-shelves-ghost/furniture-lower-start.png` dosyasına kaydedilir. Asıl oyuncu kaydı kullanılmaz.
