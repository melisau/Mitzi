# Ses tasarımı — Yol Çizen Pati

Hepsi yumuşak, yakın mikrofon, sıkıştırma hafif (ratio 2:1, threshold -24 dB). Peak -6 dBFS. Mobil hoparlör için 80 Hz altını kes.

## 1. Purr loop (ASMR)

Kayıt: gerçek kedi mırıldaması veya katmanlı “rolled r” + göğüs rezonansı (80–160 Hz).  
Süre: 8–12 sn. Audacity:

1. Normalize -3 dB
2. Effect → Noise Reduction (hafif)
3. Başı ve sonu 200 ms çapraz dinle; tıkırtı varsa Truncate Silence değil, Equalization ile 2 kHz üstünü -3 dB
4. Effect → Repeat veya elle kopyala: son 300 ms ile ilk 300 ms Crossfade Clips
5. Export WAV 48 kHz mono, Unity’de Load Type = Compressed In Memory, Loop açık

Inspector: `CozyAudioManager.defaultPurr`

## 2. Ambiyans

**Yaz:** kuşlar uzak, yaprak hışırtısı, rüzgar %20. 30–60 sn loop. LPF 10 kHz.  
**Sonbahar:** daha kuru yaprak, az kuş.  
**Kış:** şömine çıtırtısı (kayıt veya kırık odun + düşük gürültü) + camdan yağmur/kar. Şömineyi mid, yağmuru side.

Unity: `summerAmbience` / `autumnAmbience` / `winterAmbience`, loop, volume 0.3–0.4

## 3. Müzik (60–70 BPM, Lo-Fi)

Akustik gitar + yumuşak piyano, davul yok veya sadece fırça. Telifsiz kaynak önerileri (lisansı kendi hesabınla doğrula):

- Pixabay / “lofi acoustic piano 70 bpm cozy”
- FreePD / Kevin MacLeod tarzı warm acoustic (isim ve lisans dosyaya)
- Kendin: Am pentatonik, 4 bar, vinyl hissi çok az (Audacity Filter Curve EQ + hafif wow yok)

Unity: Music AudioSource, loop, 0.25–0.30

## 4. SFX

| Cue | His | Audacity |
|---|---|---|
| Fırça | yumuşak pastel tebeşir | kâğıt sürtme, fade in/out 30 ms, 0.4 sn |
| Mama kabı | seramik + mama taneleri | iki katman, 0.6 sn |
| Baloncuk | pembe pop, tehditkâr değil | sinüs 420 Hz kısa + air puff, pitch +2 |
| Mırıldama one-shot | kısa onay | purr’den 0.5 sn dilim |

## 5. Dokunma titreşimi

Kod: `Handheld.Vibrate()` okşama tikinde. iOS’ta uzun titreşim kaba gelir; ileride native plugin ile 20 ms haptic önerilir.
