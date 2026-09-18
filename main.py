import os
from dotenv import load_dotenv
from litellm import completion

# .env dosyasındaki değişkenleri yükle
load_dotenv()

print("====================================================")
print(" 🎮 Mitzi Unity Proje Asistanı Başlatıldı!         ")
print(" Proje dizini otomatik tarama modunda aktif.        ")
print(" Çıkmak için 'çıkış' yazabilirsiniz.                ")
print("====================================================\n")

# Projedeki önemli dosyaları listeleyen yardımcı fonksiyon
def proje_haritasini_cikar():
    dosya_haritasi = []
    # Sadece Assets, Packages ve ProjectSettings klasörlerine odaklanalım
    hedef_klasorler = ['Assets', 'Packages', 'ProjectSettings']
    
    for root, dirs, files in os.walk('.'):
        # Gereksiz kütüphane veya gizli klasörleri atla
        if any(ignored in root for ignored in ['.git', '.cursor', '__pycache__', 'Library', 'Temp', 'Logs', 'obj']):
            continue
            
        for file in files:
            if file.endswith(('.cs', '.json', '.txt', '.asmdef')):
                rel_path = os.path.relpath(os.path.join(root, file), '.')
                dosya_haritasi.append(rel_path)
    return dosya_haritasi

def mitzi_unity_asistan():
    if not os.getenv("GEMINI_API_KEY"):
        print("Hata: GEMINI_API_KEY bulunamadı! .env dosyanızı kontrol edin.")
        return

    # Proje dosyalarını tarayıp listeliyoruz
    mevcut_dosyalar = proje_haritasini_cikar()
    dosya_listesi_str = "\n".join(mevcut_dosyalar[:40]) # İlk 40 dosyayı sisteme besle
    
    # Mitzi'ye projenin bir Unity projesi olduğunu öğretiyoruz
    konusma_gecmisi = [
        {
            "role": "system", 
            "content": f"Sen bir Unity/C# uzmanı ve Mitzi projesinin yapay zeka asistanısın. "
                       f"Projenin kök dizinindeki bazı önemli dosyalar şunlardır:\n{dosya_listesi_str}\n"
                       f"Kullanıcı bir C# scripti veya oyun mekaniği sorduğunda, bu yapıya uygun profesyonel çözümler üret."
        }
    ]

    while True:
        try:
            kullanici_girdisi = input("\nSiz: ")
            
            if kullanici_girdisi.lower() in ['çıkış', 'exit', 'quit']:
                print("\nMitzi kapatılıyor. İyi çalışmalar, bol kodlu günler!")
                break
                
            if not kullanici_girdisi.strip():
                continue

            # Kullanıcı girdi kontrolü: Eğer bir kod dosyasını incelemek isterse içeriğini oku
            for dosya_yolu in mevcut_dosyalar:
                if kullanici_girdisi.lower() in dosya_yolu.lower() and ("oku" in kullanici_girdisi or "incele" in kullanici_girdisi):
                    try:
                        with open(dosya_yolu, 'r', encoding='utf-8') as f:
                            kod_icerigi = f.read()
                        kullanici_girdisi += f"\n\n[Sistem Notu: Bahsedilen {dosya_yolu} dosyasının içeriği aşağıdadır]:\n```csharp\n{kod_icerigi}\n```"
                        print(f"🔄 {dosya_yolu} içeriği okunarak hafızaya eklendi...")
                        break
                    except Exception:
                        pass

            konusma_gecmisi.append({"role": "user", "content": kullanici_girdisi})

            # Yapay zekaya gönder
            response = completion(
                model="gemini/gemini-3.6-flash", 
                messages=konusma_gecmisi
            )
            
            mitzi_yaniti = response.choices.message.content
            print(f"\nMitzi: {mitzi_yaniti}")
            konusma_gecmisi.append({"role": "assistant", "content": mitzi_yaniti})

        except Exception as e:
            print(f"\nBir hata oluştu: {e}")
            break

if __name__ == "__main__":
    mitzi_unity_asistan()
