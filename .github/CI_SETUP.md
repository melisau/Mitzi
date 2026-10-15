# Otomatik derleme (GitHub Actions) kurulumu

`.github/workflows/unity-ci.yml` her PR'da projeyi GitHub'ın sunucusunda Unity ile açar ve derler. Bunun için Unity lisansının **bir kez** GitHub'a tanıtılması gerekir. Bilgiler GitHub'da şifreli saklanır; loglarda ve kodda görünmez.

## 1. Unity lisans dosyasını bul

1. Unity Hub'ı aç, Unity hesabınla giriş yap.
2. **Preferences (Tercihler) → Licenses → Add → "Get a free personal license"** ile lisansı ekle. (Lisans zaten görünüyor olsa bile bu adımı yap; bazen dosya oluşmuyor.)
3. Lisans dosyasını bul:
   - **Windows:** `C:\ProgramData\Unity\Unity_lic.ulf` (ProgramData gizli klasördür; Dosya Gezgini adres çubuğuna yapıştır.)
   - **macOS:** `/Library/Application Support/Unity/Unity_lic.ulf`
4. Dosyayı Not Defteri / TextEdit ile aç ve **tüm içeriği** kopyala.

## 2. GitHub'a secret olarak ekle

GitHub'da repo → **Settings → Secrets and variables → Actions → New repository secret**. Şu üçünü ekle:

| Name | Value |
| --- | --- |
| `UNITY_LICENSE` | `.ulf` dosyasının tüm içeriği |
| `UNITY_EMAIL` | Unity hesabının e-postası |
| `UNITY_PASSWORD` | Unity hesabının şifresi |

## 3. Çalıştır

- PR'lar otomatik derlenir. Sonuç PR sayfasının altında **Unity CI / Derleme kontrolü** olarak görünür.
- Elle çalıştırmak için: **Actions → Unity CI → Run workflow**. "Android .aab" işaretliyse derleme bittikten sonra çalışma sayfasının altındaki **Artifacts** bölümünden `.aab` indirilebilir.
- İlk çalışma Unity imajını indirdiği için uzun sürer (derleme kontrolü ~15–25 dk, Android ~40–60 dk). Sonrakiler önbellek sayesinde hızlanır.

## 4. (Sonra) Play'e yüklenebilir imzalı .aab

Keystore oluşturduktan sonra (bkz. `RELEASE_CHECKLIST.md`) şu secret'ları da ekle:

| Name | Value |
| --- | --- |
| `ANDROID_KEYSTORE_BASE64` | Keystore dosyasının base64 hali (Windows PowerShell: `[Convert]::ToBase64String([IO.File]::ReadAllBytes("mitzi.keystore"))`, macOS: `base64 -i mitzi.keystore`) |
| `ANDROID_KEYSTORE_PASS` | Keystore şifresi |
| `ANDROID_KEYALIAS_NAME` | Anahtar (alias) adı |
| `ANDROID_KEYALIAS_PASS` | Anahtar şifresi |

Bunlar yoksa `.aab` debug imzasıyla üretilir: telefonda test edilebilir ama Play'e yüklenemez.
